# FIP Repository Instructions

## Project overview

Flight Intelligence Platform is a .NET solution organized into Domain, Application, Infrastructure, Persistence, host, building-block, and test projects.

## General guidance

- Inspect existing projects, references, files, and conventions before making changes.
- Preserve existing intentional code and user changes.
- Do not recreate the solution, `src/`, `tests/`, existing projects, or `.csproj` files.
- Keep changes focused on the requested task; avoid unrelated refactoring.
- Use file-scoped namespaces, nullable reference types, and sealed classes where appropriate.
- Prefer simple, explicit implementations over speculative abstractions.

## Architecture

- Keep domain models source-independent and free of infrastructure, serialization, persistence, and OpenSky-specific dependencies.
- Keep external data-source DTOs and source-specific mapping logic outside the Domain project.
- Keep application services focused on orchestration and cohesive use cases.
- Place concrete file, network, and persistence implementations in Infrastructure or the appropriate outer layer.
- Do not add EF Core, database entities, migrations, repositories, APIs, or authentication unless explicitly requested.
- Do not introduce CQRS, MediatR, commands, queries, handlers, or validators unless explicitly requested.

## Editing and validation

- Use the existing dependency-injection registration patterns.
- Add focused tests in the existing appropriate test project when behavior is introduced.
- Prefer temporary test data over fragile repository-relative paths.
- After changes, build affected projects, run relevant tests, and build `FIP.sln` when practical.
- Report files created or modified, test/build results, warnings, assumptions, and any remaining issues.



## Documentation Maintenance

Documentation under `/docs` and the root `README.md` are part of the project and must remain synchronized with the implementation.

Whenever making a code change, determine whether the change affects existing documentation.

Update the relevant documentation when a change affects:

* solution or project structure
* architecture or dependencies
* domain entities or business rules
* database schema or persistence
* flight-data import or normalization
* telemetry models
* API endpoints or contracts
* configuration
* build or development instructions
* externally visible functionality

Do not update documentation simply because source code formatting or internal implementation details changed.

The source code is the source of truth.

Never document functionality as implemented unless it actually exists in the repository.

When completing a task, include a brief `Documentation impact` section in the final summary:

* `Updated` — list documentation files changed
* `No documentation impact` — if no update was necessary

