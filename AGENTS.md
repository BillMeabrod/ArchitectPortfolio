# Project Instructions for AI Coding Agents

## Code Comments

Do not add comments that restate what the code does. Only comment:

- Non-obvious business logic or "why" decisions a future reader couldn't infer from the code itself
- Workarounds for known platform quirks or external system behavior

Do not add comments like `// fetch the data` above a fetch call, or docblocks that just repeat the function signature in prose.

## Before Writing Code

Before implementing anything, read the existing code in the relevant app first:

- Identify the architectural pattern already in use
- Identify existing naming conventions, file organization, and idioms
- Follow what's already there, even if you'd personally default to something else

Do not introduce a new pattern, library, or convention without flagging it as a deliberate decision and explaining why the existing approach didn't fit.

## Problem-Solving Approach

When you hit an obstacle, do not reach for the fastest workaround that makes the error go away. Instead:

1. Understand why the obstacle is occurring
2. Check if the obstacle indicates a misunderstanding of the existing architecture
3. Solve it in a way consistent with the existing patterns in this repo

If you genuinely believe a quick fix is the right call (e.g., a true one-off edge case), say so explicitly and explain the tradeoff rather than silently taking the shortcut.

## Architectural Consistency

Every app in this project uses a deliberate architectural pattern. Your job is to identify what that pattern is and follow it strictly — not to impose a different one.

When reading the codebase, ask:

- How is the code organized? By layer, by feature, by domain?
- Where does business logic live?
- Where does data access live?
- How do the layers communicate — direct calls, interfaces, events?
- What is explicitly kept out of each layer?

Once you understand the pattern, new code must fit it. A correct implementation in the wrong layer is still wrong.

## Layer Boundaries

Every architecture in this project enforces boundaries between layers. Identify what those boundaries are before writing anything.

Rules that apply regardless of which pattern is in use:

- Each layer has a defined responsibility. Do not bleed concerns across layers.
- If a layer communicates with another through an abstraction (interface, port, hook, etc.), do not bypass it with a direct reference.
- If you find yourself adding logic to a layer that doesn't own that concern, stop and find the correct layer.

## Controllers and Entry Points

Inbound entry points — HTTP controllers, queue triggers, event handlers — must be thin. Their only job is to receive input, delegate to the appropriate service or handler, and return a response.

**If you are writing logic in an entry point, stop.** Extract it to the layer that owns that concern.

This applies to:
- Validation logic
- Data mapping or transformation
- Orchestration of multiple operations
- Background or async work

HTTP input models may live alongside the controller as they are part of the HTTP contract, not the domain.

## Background Work and Service Lifetimes

When kicking off background work from a short-lived context (e.g., an HTTP request), never capture scoped dependencies directly. The scope they were created in will be disposed before the work completes.

Instead, create a new scope explicitly for the background work and resolve dependencies from it. Use the framework's scope factory for this — not a raw service provider reference.

## Adding New Functionality

Before adding anything new:

1. Identify which layer owns the concern you are implementing
2. Check whether an abstraction already exists for it — use it if so, extend it if needed
3. Implement in the correct layer
4. Wire up any new dependencies through the existing registration pattern in that app
5. Do not add a new layer, pattern, or abstraction without flagging it explicitly

## Scope Boundaries

- Do not modify `.github/workflows/deploy.yml`
- Do not create or modify any `.bicep` files
- Do not touch `infra/` at all
- Stay within `src/StationDashboard/` for new code; only touch backend apps (StationShipManifestLogger, StationAI, StationTriage) for CORS configuration changes, and nothing else in those apps

## Testing
This section should be followed for every new feature added. If tests are required, the PR should not be approved without them.
Do not aim for 100% test coverage. Aim for coverage of the logic that matters.

**Write tests when you add or modify:**
- Business logic in a service class
- Validation logic
- Logic that orchestrates multiple dependencies
- Fail-open or fail-closed behavior on exceptions

**Do not write tests for:**
- Controllers or entry points that are thin delegates with no logic to assert
- Adapters — these are infrastructure concerns that require real external services
- Constants, configuration classes, or data models with no behavior

Endpoint-level integration tests are acceptable when an endpoint contains meaningful behavior (for example filtering, sorting, or request-specific orchestration).

**Where tests live:**
- Read the existing test project structure before adding new test files
- Follow the naming and organization conventions already in place
- New test classes go in the test project for the app being modified

**What makes a good test:**
- Tests behavior, not implementation — assert on outcomes, not on which internal methods were called
- Uses mocks only for dependencies that cross a boundary (external service, database, queue)
- Is readable without needing to trace through the production code to understand what it is asserting
