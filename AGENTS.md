\# Project Instructions for AI Coding Agents



\## Code Comments

Do not add comments that restate what the code does. Only comment:

\- Non-obvious business logic or "why" decisions a future reader couldn't infer from the code itself

\- Workarounds for known platform quirks (e.g., Azure-specific gotchas)



Do not add comments like "// fetch the data" above a fetch call, or docblocks that just repeat the function signature in prose.



\## Before Writing Code

Before implementing anything, read the existing code in the relevant app(s) first:

\- Identify the architectural pattern already in use (Vertical Slice, Hexagonal, Django MTV, etc.)

\- Identify existing naming conventions, file organization, and idioms

\- Follow what's already there, even if you'd personally default to something else



Do not introduce a new pattern, library, or convention without flagging it as a deliberate decision and explaining why the existing approach didn't fit.



\## Problem-Solving Approach

When you hit an obstacle, do not reach for the fastest workaround that makes the error go away. Instead:

1\. Understand why the obstacle is occurring

2\. Check if the obstacle indicates a misunderstanding of the existing architecture

3\. Solve it in a way consistent with the existing patterns in this repo



If you genuinely believe a quick fix is the right call (e.g., a true one-off edge case), say so explicitly and explain the tradeoff rather than silently taking the shortcut.



\## React Architecture (StationDashboard specifically)

Organize the codebase using a \*\*feature-folder structure\*\* (also called "feature-based" or "vertical slice" organization in frontend contexts) — group files by domain feature, not by file type. Avoid the common `components/`, `hooks/`, `types/`, `services/` top-level split.



Rules:

\- Each feature owns its own components, hooks, and types in one folder.

\- No cross-feature imports except through a shared/common layer.

\- Data fetching lives in custom hooks, not inside components.

\- No global state management library unless a feature genuinely requires cross-feature shared state — polling plus local component/hook state is sufficient for this app's scope.

\- Keep components presentational where possible; push fetching/polling logic into hooks.



Use your judgment on naming and exact folder layout — the principle (feature cohesion over type-based grouping) matters more than matching any specific example structure.



\## Scope Boundaries

\- Do not modify `.github/workflows/deploy.yml`

\- Do not create or modify any `.bicep` files

\- Do not touch `infra/` at all

\- Stay within `src/StationDashboard/` for new code; only touch backend apps (StationShipManifestLogger, StationAI, StationTriage) for CORS configuration changes, and nothing else in those apps

