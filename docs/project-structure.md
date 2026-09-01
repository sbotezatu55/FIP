# Project structure

## Repository tree

```text
.
├── FIP.sln
├── AGENTS.md
├── README.md
├── docs/
├── src/
│   ├── BuildingBlocks/
│   │   ├── Fip.Application.Abstractions/
│   │   ├── Fip.Infrastructure.Abstractions/
│   │   └── Fip.SharedKernel/
│   ├── Core/
│   │   ├── Fip.Application/
│   │   └── Fip.Domain/
│   ├── Hosts/
│   │   ├── Fip.Api/
│   │   ├── Fip.DatabaseMigrator/
│   │   └── Fip.Worker/
│   ├── Infrastructure/
│   │   ├── Fip.Identity/
│   │   ├── Fip.Infrastructure/
│   │   └── Fip.Persistence/
│   └── Tools/
│       └── Fip.Cli/
└── tests/
    ├── Fip.Application.Tests/
    ├── Fip.Domain.Tests/
    ├── Fip.Infrastructure.Tests/
    └── Fip.IntegrationTests/
```

## Production projects

| Project | Purpose and current contents | Project dependencies |
|---|---|---|
| `Fip.Application.Abstractions` | Contracts for aircraft, flight, and telemetry repositories; unit of work; file storage; and date-time access. | None |
| `Fip.Infrastructure.Abstractions` | Placeholder infrastructure contract `IInfrastructureService`. | None |
| `Fip.SharedKernel` | `Entity` base class with a generated `Guid`, empty `ValueObject` base class, and reusable geographic distance calculation. | None |
| `Fip.Domain` | Source-independent domain model. Currently contains `Fip.Domain.Flights.Telemetry.FlightTelemetryPoint`. | `Fip.SharedKernel` |
| `Fip.Application` | Feature-oriented service/DTO shells and OpenSky import contract, DTO, and mapper. | `Fip.Domain`, `Fip.SharedKernel`, `Fip.Application.Abstractions`; DI abstractions package |
| `Fip.Api` | ASP.NET Core host composition root. Calls all four DI extension methods and starts the application. | Application, Infrastructure, Persistence, Identity |
| `Fip.DatabaseMigrator` | Executable host that applies pending EF Core migrations using the configured SQL Server connection. | Persistence |
| `Fip.Worker` | .NET Worker host with a `BackgroundService` whose work currently completes immediately. | Application, Infrastructure, Persistence; hosting package |
| `Fip.Identity` | Identity DI composition root with no current registrations. | Application and Infrastructure abstractions; DI abstractions package |
| `Fip.Infrastructure` | Concrete OpenSky JSON importer and Infrastructure DI registration. | Application, Application abstractions, Infrastructure abstractions; DI abstractions package |
| `Fip.Persistence` | Persistence DI composition root with no current database implementation. | Domain, Application abstractions, Infrastructure abstractions; DI abstractions package |
| `Fip.Cli` | Executable scaffold that reports the CLI is not configured. | Application, Infrastructure |

## Test projects

| Project | Current contents | Project dependency |
|---|---|---|
| `Fip.Application.Tests` | OpenSky mapper tests plus a placeholder test. | `Fip.Application` |
| `Fip.Domain.Tests` | Placeholder test only. | `Fip.Domain` |
| `Fip.Infrastructure.Tests` | OpenSky JSON importer tests plus a placeholder test. | `Fip.Infrastructure` |
| `Fip.IntegrationTests` | Placeholder test only. | `Fip.Api` |

All test projects use xUnit, Microsoft.NET.Test.Sdk, xUnit Visual Studio integration, and coverlet collector. All projects target `net10.0` with nullable reference types enabled.

## NuGet dependencies

The explicit package references currently present are:

| Package | Version | Projects |
|---|---:|---|
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `9.0.2` | Application, Infrastructure, Identity, Persistence |
| `Microsoft.Extensions.Hosting` | `10.0.9` | Worker |
| `coverlet.collector` | `6.0.4` | All test projects |
| `Microsoft.NET.Test.Sdk` | `17.14.1` | All test projects |
| `xunit` | `2.9.3` | All test projects |
| `xunit.runner.visualstudio` | `3.1.4` | All test projects |

The Web and Worker SDKs also provide their framework-level dependencies. EF Core 10.0.9 and the SQL Server provider are explicitly referenced by `Fip.Persistence`; the migrator explicitly references EF Core and hosting packages. AutoMapper, MediatR, and OpenAPI are not referenced.

## Important namespaces and folders

- `Fip.Domain.Flights.Telemetry` — normalized telemetry domain type.
- `Fip.SharedKernel.Geography` — reusable geographic distance abstraction and Haversine implementation.
- `Fip.Application.Flights.Import.OpenSky` — OpenSky DTO, import contract, and mapper.
- `Fip.Infrastructure.Flights.Import.OpenSky` — concrete OpenSky JSON file importer.
- `Fip.Application.Aircraft`, `.Flights`, `.Telemetry`, `.FlightEvents`, and `.Imports` — initial feature-oriented application shells.
- `Fip.Application.Abstractions.Persistence`, `.Storage`, and `.Time` — future-facing abstraction locations.
