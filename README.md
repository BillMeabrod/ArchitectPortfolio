# Architect Portfolio: Meabrod Station

A multi-service, cloud-native portfolio project demonstrating senior-level knowledge of software architecture patterns, inter-service communication, and Azure infrastructure. The project is themed around a science fiction space station called **Meabrod Station**, where incoming ships are logged, assessed for risk by an onboard AI, triaged by specialist crew, and the whole process is observable through a unified dashboard.

Each backend application is deliberately built using a different architectural pattern and, where appropriate, a different technology stack. The goal is to demonstrate that architectural decisions are driven by the problem at hand rather than personal habit.

---

## Live

The dashboard is deployed and publicly reachable: **https://agreeable-moss-0ff2e0510.7.azurestaticapps.net**

Every backend runs on free or low-cost Azure tiers, which means a real cold start on the first request after idle. The dashboard surfaces this explicitly rather than hiding it behind a generic spinner. If a request takes more than a few seconds, you will see a message explaining that the backend may be waking up. This is not a bug, it is a deliberate tradeoff documented further in the App 4 section below.

This infrastructure is sized for portfolio review, not sustained traffic. If the demo is unresponsive, the most likely cause is a free-tier quota being exhausted rather than the app being broken.

---

## The Big Picture

```
App 1                        App 2                          App 3
StationShipManifestLogger    StationAI                      StationTriage
.NET 10 / Vertical Slice     .NET 10 / Hexagonal            Python / Django MTV
Azure App Service (F1)       Azure App Service              Azure App Service (F1)
                              Azure Functions (Flex)         Azure Functions (Flex)
        │                           │                              │
        │   ship-manifest-queue     │   risk-assessment-queue      │
        └──────────────────────────►│──────────────────────────────►│
             Azure Storage Queue         Azure Storage Queue        │
                                                                     ▼
                                                              Postgres (Neon)
                                                                     ▲
                                                                     │
                                                          ┌──────────┴──────────┐
                                                          │   App 4              │
                                                          │   StationDashboard   │
                                                          │   React / Vertical   │
                                                          │   Slice (frontend)   │
                                                          │   Azure Static Web   │
                                                          │   Apps               │
                                                          └───────────────────────┘
                                                          Reads/writes all three
                                                          backend APIs directly
```

Ships arrive at the station, their manifests are logged by App 1, ARIA, the station's onboard AI assesses risk in App 2, and specialist crew are notified for triage in App 3. All three backend apps communicate asynchronously via Azure Storage Queues, and none of them know about each other directly. App 4 is a dashboard that talks to all three backend APIs directly, giving a single place to submit manifests, tune ARIA's behavior, and work the triage queues.

---

## App 1: StationShipManifestLogger

**Pattern:** Vertical Slice Architecture
**Stack:** .NET 10, ASP.NET Core, MediatR, Entity Framework Core, SQLite
**Hosting:** Azure App Service (F1 Free Tier, Linux)

### What it does

Accepts incoming ship manifest reports via a REST API. Each manifest contains the ship name, callsign, captain name, cargo items, and passenger list. The manifest is logged to a SQLite database, and a message is published to an Azure Storage Queue for downstream processing.

### Why Vertical Slice?

Vertical Slice Architecture organizes code by feature rather than by layer. All logic for a given feature, the controller, the command, the handler, and the data access, lives in a single file. This makes the codebase easy to navigate for small, focused applications.

App 1 has a single responsibility with minimal business logic. Vertical Slice is the right fit because the overhead of layered architecture would add complexity without adding value. The pattern intentionally avoids shared abstractions. The queue publisher lives inside the Docking feature slice because it belongs to that feature's concerns, not to shared infrastructure.

### Key design decisions

**MediatR** is used to decouple the HTTP layer from the handler, keeping the controller thin and the handler focused on its single responsibility.

**SQLite in production** was a deliberate cost-driven decision. Azure SQL Serverless, while serverless, still accrues compute costs during active development. For an audit log that no one queries directly through a UI, SQLite on the App Service `/home/` persistent storage is a perfectly adequate and zero-cost alternative. The tradeoff (no concurrent write safety, no cloud-native HA) is acceptable at portfolio scale, and would be a clear upgrade path in a production system.

---

## App 2: StationAI

**Pattern:** Hexagonal Architecture (Ports and Adapters)
**Stack:** .NET 10, ASP.NET Core, Azure Functions (Flex Consumption, isolated worker), Azure Blob Storage, Google Gemini API
**Hosting:** Azure App Service + Azure Functions (Flex Consumption, Linux)

### What it does

App 2 is the station's onboard AI system, named ARIA. It has two entry points.

A **REST API** (`StationAI`) that exposes the current universe rules (editable prompt context) and allows them to be updated. If no rules have been saved yet, the GET endpoint returns a default fallback message.

An **Azure Function** (`StationAI.Functions`) that triggers on messages arriving in `ship-manifest-queue`. It retrieves the current rules, builds a prompt, calls the Gemini LLM, and parses the structured response into a `RiskAssessment` containing scores for Biohazard, Chemical Hazard, and Security Hazard (each 0-10), plus a plain-language recommendation. The assessment is then published to `risk-assessment-queue` for App 3 to consume.

### Why Hexagonal?

Hexagonal Architecture (also known as Ports and Adapters) is chosen when the primary architectural concern is the volatility of external dependencies rather than the complexity of internal business logic. App 2's core logic is straightforward: build a prompt, call an LLM, parse the result. What is volatile is the LLM provider.

Google Gemini is used today because it offers a free tier sufficient for portfolio traffic. The primary model is `gemini-3.1-flash-lite`, with `gemini-2.5-flash-lite` as a fallback for transient failures (rate limits, server errors, timeouts). If Gemini removes the free tier, changes its API, or a better option emerges, the only change required is writing a new outbound adapter that implements `ILargeLanguageModelService`. The core, the rules repository, the queue publisher, and every inbound adapter remain completely untouched. The architecture makes the swap a single-file change by design.

The same principle applies to rules storage. Rules are stored as a plain text blob in Azure Blob Storage, which is a simple and zero-cost solution for a single overwritable string. If the requirements evolved to support versioned rule history or multi-tenant rules, the `IRulesRepository` interface stays the same, and only the adapter changes.

### Ports and Adapters structure

**Core** (business logic, no external dependencies):
- `ILargeLanguageModelService`: outbound port for LLM calls
- `IRulesRepository`: outbound port for rules persistence
- `RiskAssessmentService`: assembles the prompt, calls the LLM, and validates the response, including a single retry if the result is malformed or contains out-of-range values
- `RiskAssessment`, `ShipManifest`: core domain models
- `AriaIdentity`: holds ARIA's fixed core directive and shared fallback text as constants, referenced by both the prompt builder and the rules API so they stay in sync with a single source of truth

**Outbound Adapters** (the core calls these):
- `GeminiAdapter`: implements `ILargeLanguageModelService`, calls Google Gemini API with a range-constrained response schema, generated via reflection from the `RiskAssessment` type's validation attributes, and falls back to a backup model on transient failures
- `RulesBlobStorageAdapter`: implements `IRulesRepository`, reads and writes a single blob in Azure Blob Storage

**Inbound Adapters** (these call the core):
- `UniverseRulesController`: HTTP endpoints for GET/PUT on the current rules, also exposing ARIA's fixed core directive for read-only inspection
- `ShipManifestFunction`: Azure Queue trigger that deserializes the manifest, calls `RiskAssessmentService`, and publishes the result
- `RiskAssessmentQueuePublisher`: publishes the completed assessment to `risk-assessment-queue`

### The prompt architecture

The system prompt is structured in three deliberate sections.

**Part 1: Fixed core directive.** ARIA's identity, scoring instructions, and output contract are hardcoded and never change. This section is exposed read-only through the dashboard's AI Console so a viewer can understand ARIA's core behavior, but it cannot be edited.

**Part 2: Volatile universe intel (the editable rules).** This section is loaded from Blob Storage at runtime and is editable through the UI. It contains station-specific intelligence such as banned cargo, persons of interest, and active threat advisories. A prompt injection warning is embedded in this section. If a user attempts to override ARIA's core directives through the rules field, ARIA is instructed to treat the attempt itself as a security risk and include it in the assessment.

**Part 3: The manifest payload.** The incoming ship data, serialized as JSON.

This structure ensures that the output contract (the JSON shape that App 3 depends on) can never be broken by user-edited rules, while still giving full flexibility over the tactical intelligence ARIA uses to make assessments.

### Validation, end to end

Hazard level scores are constrained at three independent layers, not just one. The Gemini request schema itself declares a minimum and maximum for each score, generated automatically from `[Range(0, 10)]` attributes on the `RiskAssessment` model. If Gemini still returns an out-of-range or malformed value despite the constrained schema (a real possibility, since structured output guarantees syntax, not semantics), `RiskAssessmentService` validates the result and retries once before failing loudly. Finally, Postgres itself enforces the same 0-10 range via `CheckConstraint`s on the `triage_shipassessment` table, so the constraint holds regardless of which process writes to that table.

---

## App 3: StationTriage

**Pattern:** Django MTV (Model-Template-View)
**Stack:** Python 3.14, Django 6.0.6, Postgres (Neon serverless), Azure Functions (Python v2)
**Hosting:** Azure App Service (F1 Free Tier, Linux) + Azure Functions (Flex Consumption)

### What it does

App 3 is the station's triage system. It has two entry points.

A **Django web app** that exposes role-based queue views, security, medical, and hazmat, each showing only the ships relevant to that specialist team, filtered by the corresponding hazard score and excluding already-resolved assessments. Each queue has a detail endpoint that returns the full assessment (recommendation, cargo manifest, passenger list) and accepts status updates as the ship moves through `NEW → IN_PROGRESS → RESOLVED`.

A standalone **Azure Function** that triggers on messages arriving in `risk-assessment-queue`, and inserts the combined manifest and risk assessment into Postgres as a new `ShipAssessment` row, ready for the queue views to pick up.

### Why Django MTV?

This app exists partly to prove a point the first two don't: that the architectural reasoning throughout this project isn't tied to one language or ecosystem. App 3 is deliberately built in Python, a different language from Apps 1 and 2, specifically to demonstrate range, not just depth in a single stack.

Django is the natural choice once Python is the language. It's the standard, widely-adopted framework for exactly this kind of application: CRUD-style views over a relational model, with real business logic and a real data layer. Using Django's own MTV convention, rather than working against the grain of the framework, keeps the codebase recognizable to anyone who knows Django, which matters for a project meant to be read and evaluated by other engineers.

### Key design decisions

**The Azure Function is deliberately framework-free.** Although the web app uses Django's ORM, the Function does not import Django at all. It writes directly to Postgres via `psycopg2`. Early iterations attempted to share Django's `ShipAssessment` model between the web app and the Function, which created a real deployment coupling problem: the Function's deployment package needed to include the entire Django project tree to satisfy `INSTALLED_APPS`, and any change to the web app's dependencies broke the Function's ability to start, since it loaded the same `INSTALLED_APPS` list regardless of whether it actually used those apps. Decoupling the Function into a standalone script with its own minimal dependency list (`azure-functions`, `psycopg2-binary`) eliminated this coupling entirely. The Function now has an honest, accurate dependency list, and the two processes evolve independently.

**Postgres over SQLite for this app specifically**, unlike App 1's SQLite choice. App 3's data is genuinely queried and filtered in different ways by three different consumers (the three role-based queues), and is written by two separate processes (the web app and the Function) that may run as multiple instances. SQLite's lack of robust concurrent-write support made it a poor fit here, whereas App 1's audit log is single-writer and append-only. Neon's serverless Postgres free tier keeps this at zero fixed cost while providing real concurrent access.

---

## App 4: StationDashboard

**Pattern:** Vertical Slice (feature-folder organization, frontend equivalent)
**Stack:** React 19, TypeScript, Vite, Tailwind CSS, React Router
**Hosting:** Azure Static Web Apps

### What it does

App 4 is a single dashboard that ties the other three apps together visually without merging their identities. It has three zones, each calling its corresponding backend's API directly: a manifest submission form (App 1), a control panel for editing ARIA's universe intel and viewing its fixed core directive (App 2), and a live, role-based triage board with drill-down detail pages (App 3).

### Why Vertical Slice for the frontend?

The same reasoning that justified Vertical Slice for App 1 applies here: a small application with no complex internal layering needs benefits from feature cohesion over type-based organization. The codebase is organized by feature (`manifests/`, `ai-console/`, `triage/`), each owning its own components, hooks, and types, rather than the common `components/` / `hooks/` / `types/` split that scatters a single feature's logic across multiple top-level folders. A `shared/` folder holds only what is genuinely cross-feature: the layout shell and the cold-start notice component.

### Key design decisions

**Each zone has a distinct visual identity**, not just a different accent color. The Manifest Logger zone reads as a blue-collar warehouse terminal, the AI Console as a literal command-line interface (complete with a blinking cursor and `>` prompt styling), and Triage as a clean space-station administration console. This is a deliberate choice to make the underlying architectural diversity of the three backend apps visible, not just documented. A viewer should be able to tell they've navigated to a different application without reading a label.

**No global state management library.** Each zone's data fetching lives in a dedicated hook (`useSubmitManifest`, `useUniverseRules`, `useTriageQueue`, `useTriageDetail`) using local component state and polling. Each zone is independent and nothing needs cross-zone shared state, so local state is sufficient here.

**Explicit handling of low-cost infrastructure tradeoffs.** Every backend in this project runs on free or low-cost Azure tiers, which means real cold-start latency on the first request after idle. Rather than hiding this behind a generic spinner, the dashboard explicitly tells the user what's happening ("this backend may be waking up, this can take up to a minute") after a 5-second threshold. This is an honest acknowledgment of the infrastructure's real tradeoffs rather than a polished facade over them.

**Stable identity for dynamic list rows.** The manifest form's cargo and passenger lists use a real, stable ID per row (generated once via `crypto.randomUUID()` at creation), not array index or row content, as the React key. This avoids the well-known class of bugs where adding, removing, or editing list items causes React to misattribute DOM state, focus, or input values between rows.

---

## Inter-Service Communication: Event-Driven Architecture

All three backend applications communicate via **Azure Storage Queues**, implementing an event-driven, asynchronous messaging pattern.

**App 1** publishes a `ManifestReportCommand` to `ship-manifest-queue` after logging a manifest. It has no knowledge of App 2.

**App 2's Function** consumes `ship-manifest-queue`, processes the manifest, and publishes a combined `{ Manifest, Assessment }` payload to `risk-assessment-queue`. It has no knowledge of App 1 or App 3.

**App 3's Function** consumes `risk-assessment-queue` and writes the combined manifest and assessment to Postgres. It has no knowledge of App 1 or App 2. **App 3's web app** independently reads from the same Postgres database to serve the role-based queue views. It has no direct relationship with the queues at all, only with the data the Function produced.

This decoupling means any service can be redeployed, scaled, or replaced independently without affecting the others. The system is also resilient to downstream failures. If App 2 is unavailable, messages queue up and are processed when it recovers rather than causing App 1 to fail.

The pattern used is **event-carried state transfer**. Each message contains the full state needed by the consumer rather than just a notification ID that requires a follow-up API call. This keeps the services truly independent.

**App 4** sits outside this event chain entirely. It does not publish or consume queue messages. It calls each backend's REST API directly, the same way any external client would.

---

## Infrastructure

All Azure resources live in a single resource group: `SpaceStation-RG`.

**Azure App Service (F1 Free Tier)** hosts App 1, App 2's REST API, and App 3's web app on Linux.

**Azure Functions (Flex Consumption)** hosts App 2's and App 3's queue triggers. Flex Consumption is chosen over the standard Consumption plan because it provides better cold-start performance and native .NET 10 and Python support. Each Function app uses its own dedicated Flex Consumption plan, since the plan type only supports one site per plan.

**Azure Storage Account (`spacestationstorage`)** serves multiple purposes across the project. It is the backing store for Azure Functions (required by the runtime), and it hosts the application-level queues, the rules blob container, and the deployment packages for both Function apps.

**Neon Postgres (serverless, free tier)** backs App 3, chosen for genuine multi-writer concurrency that SQLite cannot provide.

**Azure Static Web Apps (Free Tier)** hosts App 4, the dashboard.

**SQLite on App Service persistent storage** replaces Azure SQL for App 1, eliminating database costs entirely for a workload that does not require a cloud-native relational database.

Infrastructure for every app is defined as code using **Bicep** (`infra/main.bicep`, `infra/station-ai.bicep`, `infra/station-triage.bicep`, `infra/station-dashboard.bicep`), demonstrating familiarity with Azure IaC tooling.

---

## CI/CD

All four apps deploy via a single GitHub Actions workflow (`.github/workflows/deploy.yml`) triggered on push to `main`, with one job per app.

A few deliberate choices worth calling out:

**The dashboard's build runs explicitly, not implicitly.** Azure Static Web Apps' deploy action can build the app itself using an internal, auto-detecting builder, but that builder does not necessarily run the project's own `package.json` build script, which meant TypeScript errors could pass through to production undetected. The pipeline now runs `npm install && npm run build` (the project's real build script, including its `tsc -b` type-check gate) as its own explicit step, then deploys the already-built output with `skip_app_build: true`. A broken type stops the deploy, loudly, before anything ships.

**The build output is verified before it's deployed.** A short check confirms `dist/index.html` references a real compiled bundle (`/assets/...`) rather than raw source, catching a category of misconfiguration that a successful build alone cannot rule out.

---

## Running Locally

### Prerequisites
- .NET 10 SDK
- Python 3.13+ (3.14 recommended to match production)
- Node.js and npm
- Azure Functions Core Tools v4
- Azurite (Azure Storage emulator): `npm install -g azurite`
- A Google Gemini API key
- A Neon (or any Postgres) connection string

### App 1: StationShipManifestLogger

```bash
cd src/StationShipManifestLogger/StationShipManifestLogger
dotnet run
```

User secrets required:
```json
{
  "ConnectionStrings": {
    "AzureStorageConnection": "UseDevelopmentStorage=true"
  }
}
```

### App 2: StationAI (Web API)

```bash
cd src/StationAI/StationAI
dotnet run
```

User secrets required:
```json
{
  "ConnectionStrings": {
    "BlobStorageConnection": "UseDevelopmentStorage=true"
  },
  "GOOGLE_API_KEY": "your-gemini-api-key"
}
```

The deployed version supplies `GOOGLE_API_KEY` through an Azure App Service application setting instead.

### App 2: StationAI.Functions

Start Azurite first:
```bash
azurite
```

Then:
```bash
cd src/StationAI/StationAI.Functions
func start
```

`local.settings.json` required (not committed):
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "BlobStorageConnection": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "FUNCTIONS_EXTENSION_VERSION": "~4"
  }
}
```

### App 3: StationTriage (Web App)

```bash
cd src/StationTriage/station_triage
pip install -r requirements.txt
python manage.py migrate
python manage.py runserver
```

`.env` required (not committed):
```
DATABASE_URL=postgresql://user:password@host/dbname
DJANGO_SECRET_KEY=your-local-secret-key
DEBUG=True
```

### App 3: StationTriage Function

```bash
cd src/StationTriage/station_triage/functions
func start
```

`local.settings.json` required (not committed):
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "DATABASE_URL": "postgresql://user:password@host/dbname",
    "FUNCTIONS_WORKER_RUNTIME": "python"
  }
}
```

### App 4: StationDashboard

```bash
cd src/StationDashboard
npm install
cp .env.example .env
npm run dev
```

The `.env.example` values point at the deployed Azure APIs by default. Replace them with local URLs (for example `http://localhost:5000`) if running the backends locally too.

---

## Architectural Decision Summary

| App | Pattern | Why |
|-----|---------|-----|
| StationShipManifestLogger | Vertical Slice | Single responsibility, minimal business logic, feature cohesion over layer separation |
| StationAI | Hexagonal (Ports and Adapters) | High external dependency volatility, LLM provider and storage are both likely to change |
| StationTriage | Django MTV | Deliberately built in a second language to demonstrate range, with Django as the standard, recognizable choice for CRUD-style apps in that ecosystem |
| StationDashboard | Vertical Slice (frontend) | Same single-responsibility, low-complexity reasoning as App 1, applied to React's feature-folder convention |

| Concern | Decision | Rationale |
|---------|----------|-----------|
| Inter-service messaging | Azure Storage Queues | Zero cost at portfolio scale, sufficient reliability, demonstrates async event-driven patterns |
| LLM provider | Google Gemini (free tier) | Zero cost, 500 req/day sufficient for portfolio traffic, swappable via adapter |
| App 1 database | SQLite | Zero cost, adequate for append-only audit log with no UI query requirements |
| App 3 database | Postgres (Neon serverless) | Multiple concurrent readers and writers across web app and Function require real concurrency support, unlike App 1's single-writer log |
| Rules storage | Azure Blob Storage | Single overwritable value, zero cost, eliminates unnecessary database dependency |
| App 3 Function dependency model | Standalone (no Django import) | Avoids deployment coupling between the web app's and Function's dependency lists; the Function's `requirements.txt` accurately reflects only what it uses |
| Hazard level validation | Schema constraint + app-level retry + DB `CheckConstraint` | No single point of failure; an out-of-range value is rejected regardless of which layer or process attempts to write it |
| Hosting | Azure App Service F1 + Functions Flex Consumption + Static Web Apps | Zero fixed cost across every component, scales to zero, sufficient for portfolio traffic |
