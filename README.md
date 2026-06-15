# Architect Portfolio: Meabrod Station

A multi-service, cloud-native portfolio project demonstrating senior-level knowledge of software architecture patterns, inter-service communication, and Azure infrastructure. The project is themed around a science fiction space station called **Meabrod Station**, where incoming ships are logged, assessed for risk by an onboard AI, and triaged by specialist crew.

Each application is deliberately built using a different architectural pattern and, where appropriate, a different technology stack. The goal is to demonstrate that architectural decisions are driven by the problem at hand rather than personal habit.

---

## The Big Picture

```
App 1                        App 2                          App 3
StationShipManifestLogger    StationAI                      StationTriage (WIP)
.NET 10 / Vertical Slice     .NET 10 / Hexagonal            Python / Django
Azure App Service (F1)       Azure App Service              Azure App Service
                             Azure Functions (Flex)
        │                           │                              │
        │   ship-manifest-queue     │   risk-assessment-queue      │
        └──────────────────────────►│──────────────────────────────►│
             Azure Storage Queue         Azure Storage Queue
```

Ships arrive at the station, their manifests are logged by App 1, the AI assesses risk in App 2, and specialist crew are notified for triage in App 3. All three apps communicate asynchronously via Azure Storage Queues and none of them know about each other directly.

---

## App 1: StationShipManifestLogger

**Pattern:** Vertical Slice Architecture  
**Stack:** .NET 10, ASP.NET Core, MediatR, Entity Framework Core, SQLite  
**Hosting:** Azure App Service (F1 Free Tier, Linux)

### What it does

Accepts incoming ship manifest reports via a REST API. Each manifest contains the ship name, callsign, captain name, cargo items, and passenger list. The manifest is logged to a SQLite database and a message is published to an Azure Storage Queue for downstream processing.

### Why Vertical Slice?

Vertical Slice Architecture organizes code by feature rather than by layer. All logic for a given feature (the controller, the command, the handler, the data access) lives in a single file. This makes the codebase easy to navigate for small, focused applications.

App 1 has a single responsibility with minimal business logic. Vertical Slice is the right fit because the overhead of layered architecture would add complexity without adding value. The pattern intentionally avoids shared abstractions. The queue publisher lives inside the Docking feature slice because it belongs to that feature's concerns, not to shared infrastructure.

### Key design decisions

**MediatR** is used to decouple the HTTP layer from the handler, keeping the controller thin and the handler focused on its single responsibility.

**SQLite in production** was a deliberate cost-driven decision. Azure SQL Serverless, while serverless, still accrues compute costs during active development. For an audit log that no one queries directly through a UI, SQLite on the App Service `/home/` persistent storage is a perfectly adequate and zero-cost alternative. The tradeoff (no concurrent write safety, no cloud-native HA) is acceptable at portfolio scale and would be a clear upgrade path in a production system.



---

## App 2: StationAI

**Pattern:** Hexagonal Architecture (Ports and Adapters)  
**Stack:** .NET 10, ASP.NET Core, Azure Functions (Flex Consumption, isolated worker), Azure Blob Storage, Google Gemini API  
**Hosting:** Azure App Service + Azure Functions (Flex Consumption, Linux)

### What it does

App 2 is the station's onboard AI system, named ARIA. It has two entry points.

A **REST API** (`StationAI`) that exposes the current universe rules (editable prompt context) and allows them to be updated. If no rules have been saved yet, the GET endpoint returns a default message indicating no intel is available.

An **Azure Function** (`StationAI.Functions`) that triggers on messages arriving in `ship-manifest-queue`. It retrieves the current rules, builds a prompt, calls the Gemini LLM, and parses the structured response into a `RiskAssessment` containing scores for Biohazard, Chemical Hazard, and Security Hazard (each 0-10) plus a plain-language recommendation. The assessment is then published to `risk-assessment-queue` for App 3 to consume.

### Why Hexagonal?

Hexagonal Architecture (also known as Ports and Adapters) is chosen when the primary architectural concern is the volatility of external dependencies rather than the complexity of internal business logic. App 2's core logic is straightforward: build a prompt, call an LLM, parse the result. What is volatile is the LLM provider.

Google Gemini is used today because it offers a free tier sufficient for portfolio traffic. The primary model is `gemini-3.1-flash-lite` with `gemini-2.5-flash-lite` as a rate limit fallback. If Gemini removes the free tier, changes its API, or a better option emerges, the only change required is writing a new outbound adapter that implements `ILargeLanguageModelService`. The core, the rules repository, the queue publisher, and every inbound adapter remain completely untouched. The architecture makes the swap a single-file change by design.

The same principle applies to rules storage. Rules are stored as a plain text blob in Azure Blob Storage, which is a simple and zero-cost solution for a single overwritable string. If the requirements evolved to support versioned rule history or multi-tenant rules, the `IRulesRepository` interface stays the same and only the adapter changes.

### Ports and Adapters structure

**Core** (business logic, no external dependencies):
- `ILargeLanguageModelService`: outbound port for LLM calls
- `IRulesRepository`: outbound port for rules persistence
- `RiskAssessmentService`: assembles the prompt and orchestrates the assessment
- `RiskAssessment`, `ShipManifest`: core domain models

**Outbound Adapters** (the core calls these):
- `GeminiAdapter`: implements `ILargeLanguageModelService`, calls Google Gemini API, generates a JSON schema from the `RiskAssessment` type via reflection to enforce structured output
- `RulesBlobStorageAdapter`: implements `IRulesRepository`, reads and writes a single blob in Azure Blob Storage

**Inbound Adapters** (these call the core):
- `UniverseRulesController`: HTTP endpoints for GET/PUT on the current rules
- `ShipManifestFunction`: Azure Queue trigger that deserializes the manifest, calls `RiskAssessmentService`, and publishes the result
- `RiskAssessmentQueuePublisher`: publishes the completed assessment to `risk-assessment-queue`

### The prompt architecture

The system prompt is structured in three deliberate sections.

**Part 1: Fixed core directive.** ARIA's identity, scoring instructions, and output contract are hardcoded and never change. This section is not exposed to the user and cannot be overridden.

**Part 2: Volatile universe intel (the editable rules).** This section is loaded from Blob Storage at runtime and is editable through the UI. It contains station-specific intelligence such as banned cargo, persons of interest, and active threat advisories. A prompt injection warning is embedded in this section. If a user attempts to override ARIA's core directives through the rules field, ARIA is instructed to treat the attempt itself as a security risk and include it in the assessment.

**Part 3: The manifest payload.** The incoming ship data, serialized as JSON.

This structure ensures that the output contract (the JSON shape that App 3 depends on) can never be broken by user-edited rules, while still giving full flexibility over the tactical intelligence ARIA uses to make assessments.



---

## Inter-Service Communication: Event-Driven Architecture

All three applications communicate via **Azure Storage Queues**, implementing an event-driven, asynchronous messaging pattern.

**App 1** publishes a `ManifestReportCommand` to `ship-manifest-queue` after logging a manifest. It has no knowledge of App 2.

**App 2's Function** consumes `ship-manifest-queue`, processes the manifest, and publishes a combined `{ Manifest, Assessment }` payload to `risk-assessment-queue`. It has no knowledge of App 1 or App 3.

**App 3** (in progress) will consume `risk-assessment-queue`. It has no knowledge of App 1 or App 2.

This decoupling means any service can be redeployed, scaled, or replaced independently without affecting the others. The system is also resilient to downstream failures. If App 2 is unavailable, messages queue up and are processed when it recovers rather than causing App 1 to fail.

The pattern used is **event-carried state transfer**. Each message contains the full state needed by the consumer rather than just a notification ID that requires a follow-up API call. This keeps the services truly independent.

---

## Infrastructure

All Azure resources live in a single resource group: `SpaceStation-RG`.

**Azure App Service (F1 Free Tier)** hosts App 1 and App 2's REST APIs on Linux.

**Azure Functions (Flex Consumption)** hosts App 2's queue trigger. Flex Consumption is chosen over the standard Consumption plan because it provides better cold-start performance and native .NET 10 support.

**Azure Storage Account (`stationairules`)** serves two purposes. It is the backing store for Azure Functions (required by the runtime) and it hosts the application-level queues and the rules blob container.

**SQLite on App Service persistent storage** replaces Azure SQL for App 1, eliminating database costs entirely for a workload that does not require a cloud-native relational database.

Infrastructure for App 1 is defined as code using **Bicep** (`infra/main.bicep`), demonstrating familiarity with Azure IaC tooling.

---

## App 3: StationTriage *(Coming Soon)*

The third application in the portfolio is currently in development. It will consume risk assessments published by App 2 and provide role-based triage views for station crew, allowing specialist teams to see only the ships relevant to their domain and respond accordingly.



---

## Running Locally

### Prerequisites
- .NET 10 SDK
- Azure Functions Core Tools v4
- Azurite (Azure Storage emulator): `npm install -g azurite`
- A Google Gemini API key

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
  }
}
```

`GOOGLE_API_KEY` must be set as a system environment variable.

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

---

## Architectural Decision Summary

| App | Pattern | Why |
|-----|---------|-----|
| StationShipManifestLogger | Vertical Slice | Single responsibility, minimal business logic, feature cohesion over layer separation |
| StationAI | Hexagonal (Ports and Adapters) | High external dependency volatility, LLM provider and storage are both likely to change |
| StationTriage | TBD | Coming soon |

| Concern | Decision | Rationale |
|---------|----------|-----------|
| Inter-service messaging | Azure Storage Queues | Zero cost at portfolio scale, sufficient reliability, demonstrates async event-driven patterns |
| LLM provider | Google Gemini (free tier) | Zero cost, 500 req/day sufficient for portfolio traffic, swappable via adapter |
| App 1 database | SQLite | Zero cost, adequate for append-only audit log with no UI query requirements |
| Rules storage | Azure Blob Storage | Single overwritable value, zero cost, eliminates unnecessary database dependency |
| Hosting | Azure App Service F1 + Functions Flex Consumption | Zero fixed cost, scales to zero, sufficient for portfolio traffic |
