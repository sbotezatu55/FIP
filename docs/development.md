# Development

## Prerequisites

- .NET 10 SDK or compatible preview SDK used by the repository.
- A Git client.
- An editor or IDE with C# and .NET support.

The current environment builds with a .NET 10 preview SDK and emits `NETSDK1057` informational messages about the preview runtime.

## Clone and restore

```powershell
git clone <repository-url>
cd FIP
dotnet restore FIP.sln
```

The repository does not currently include a README-provided external service or database setup requirement.

## Build

Build the complete solution:

```powershell
dotnet build FIP.sln
```

Build an individual project when iterating:

```powershell
dotnet build src/Core/Fip.Application/Fip.Application.csproj
dotnet build src/Infrastructure/Fip.Infrastructure/Fip.Infrastructure.csproj
```

## Run tests

Run all solution tests:

```powershell
dotnet test FIP.sln
```

Run focused tests:

```powershell
dotnet test tests/Fip.Application.Tests/Fip.Application.Tests.csproj
dotnet test tests/Fip.Infrastructure.Tests/Fip.Infrastructure.Tests.csproj
```

The current test suite uses xUnit. Application and Infrastructure tests use temporary data for focused OpenSky paths. The application integration suite also runs the real TRA051 fixture at `data/samples/opensky/TRA051_B738_2018-05-30.json` through the complete import, normalization, validation, reconstruction, event-detection, summary, and persistence-boundary workflow. Domain and Integration test projects currently contain placeholder tests.

## Run hosts and tools

### Running FIP locally from Visual Studio

For the normal development workflow:

1. Open `FIP.sln` in Visual Studio.
2. Set `Fip.Api` as the startup project.
3. Press `F5`.

The API runs under the Visual Studio debugger, and ASP.NET Core SPA Proxy automatically starts the Angular development server with `npm start` from `apps/fip-web`. Visual Studio first opens the API bootstrap URL at `http://localhost:5271`; SPA Proxy then starts Angular and redirects the browser to `http://localhost:4200`. Angular keeps hot reload/live rebuild enabled, and its existing `/api` development proxy forwards requests to the API at `http://localhost:5271`.

The first run requires the .NET and Node.js prerequisites below, plus `npm install` once from `apps/fip-web` if `node_modules` is not present. Stopping the Visual Studio debugging session stops the SPA Proxy-launched frontend process with the session.

### Manual host startup

Run the API manually when Visual Studio is not being used:

```powershell
dotnet run --project src/Hosts/Fip.Api/Fip.Api.csproj
```

Run the Angular frontend manually for troubleshooting in a second terminal:

```powershell
cd apps/fip-web
npm install
npm start
```

The Angular development server proxies `/api/...` requests to the local API at `http://localhost:5271`. The frontend communicates with `Fip.Api` through HTTP/JSON; it does not reference .NET projects directly.

Run the Worker, CLI, or DatabaseMigrator using their project paths if needed. They are currently scaffolds: the Worker completes its background operation immediately, while the CLI and migrator print that they are not configured.

## Configuration

API configuration is in:

- `src/Hosts/Fip.Api/appsettings.json`
- `src/Hosts/Fip.Api/appsettings.Development.json`
- `src/Hosts/Fip.Api/Properties/launchSettings.json`

Worker configuration is in:

- `src/Hosts/Fip.Worker/appsettings.json`
- `src/Hosts/Fip.Worker/appsettings.Development.json`

Current settings cover logging, allowed hosts, launch URLs, and environment selection. There are no database, authentication, or external OpenSky settings.

## Frontend architecture

The Angular application is located at `apps/fip-web` and provides a reusable dark aerospace/telemetry application shell, an Index landing page, a live Recent Flights section loaded from `GET /api/flights`, a full Flights list, and a Flight Detail dashboard loaded from `GET /api/flights/{id}` and `GET /api/flights/{id}/summary`:

```text
Angular FIP Web
      |
      | HTTP / JSON
      v
    Fip.Api
      |
      v
Fip.Application
      |
      v
 Fip.Domain
```

The frontend uses standalone components, Angular Router, SCSS, strict TypeScript, and the standalone `provideHttpClient()` provider. Global FIP CSS variables and shared classes define the dark surfaces, typography, borders, controls, panels, tables, focus states, and green/cyan accent system. The root shell owns navigation, footer, and the `RouterOutlet`; public `/features` and `/technology` pages explain product capabilities and engineering architecture respectively, while `/privacy`, `/terms`, and `/contact` provide informational pages. The landing page's Recent Flights component sorts the API response by start time and displays the four newest records, linking each row to `/flights/{id}`. The reusable `FipIconComponent` renders the repository-owned, current-color SVG assets from `public/icons/fip/`; it accepts an icon name, pixel size, and optional accessible label. The hero uses the transparent `public/images/airframe-transparent.png` aircraft asset; the original supplied PNG remains in `public/images/airframe.png`. The Flights feature uses typed API services and the development proxy for its flight, detail, summary, telemetry, event, and import requests. The `/flights/import` page uploads OpenSky JSON files as multipart form data to `POST /api/flights/import`; parsing and reconstruction remain backend responsibilities. The Flight Detail page currently renders the telemetry trajectory with Leaflet `1.9.4` and `@types/leaflet` `1.9.22`, using an OpenStreetMap-compatible tile layer with visible attribution and no API key for development. It also renders altitude, groundspeed, and vertical-rate profiles with Chart.js `4.5.1`, using the same elapsed flight-time X-axis, responsive sizing, decimation, and tooltip conventions; all three charts reuse the same telemetry request. Vertical-rate values remain signed in feet per minute with a visible zero reference. Detected flight events are displayed in a chronological timeline from `GET /api/flights/{id}/events`.

The import page reports real HTTP upload progress when the browser provides total byte information. Once upload completes, it shows backend processing as one truthful phase because the synchronous API does not expose internal stage progress.

## Sample data

The OpenSky integration fixture is `data/samples/opensky/TRA051_B738_2018-05-30.json`. The importer accepts a caller-supplied file path and does not depend on a fixed sample location.

## Development conventions observed

- Nullable reference types and implicit usings are enabled across projects.
- Projects target `net10.0`.
- File-scoped namespaces are used throughout the handwritten C# code.
- DI is exposed through project-level `DependencyInjection` extension classes.
- The solution favors feature-oriented folders in Application and source-specific folders for OpenSky import code.
- External DTOs remain outside Domain; normalized telemetry resides in Domain.
- Tests are separated by architectural area.
- No CQRS, MediatR, AutoMapper, EF Core, or database provider is currently used.
