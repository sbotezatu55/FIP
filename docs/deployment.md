# FIP deployment guide

## What the workflows do

build-master.yml runs on pushes to master and on manual dispatch using a GitHub-hosted Windows runner. It restores and builds the .NET solution in Release configuration, runs the existing .NET tests, installs Angular dependencies with npm ci, and builds the Angular application with the production configuration. It never writes to IIS.

deploy-fip-demo.yml is manual-only. It always checks out the current fip-demo branch explicitly, regardless of the branch selected to open the workflow. It uses the self-hosted Windows runner labels self-hosted, Windows, X64, and fip-demo.

The deployment builds both applications before it touches IIS. It publishes the API and stages the Angular output, validates the staged files, takes only the API child application offline, stops only the `ATS.FIP.API` app pool until API files are copied, synchronizes managed frontend files without deleting unrelated persistent data, preserves API production configuration and persistent files, brings the API back online, and verifies the public frontend and API URLs.

Database migrations are not run by the deployment workflow. Configure the production connection through IIS/server environment configuration or the existing server-managed production settings.

## Repository paths

- API project: src/Hosts/Fip.Api/Fip.Api.csproj
- Angular project: apps/fip-web
- Angular configuration: apps/fip-web/angular.json
- Angular package manifest: apps/fip-web/package.json
- Angular production output: apps/fip-web/dist/fip-web/browser
- The Angular production build uses a browser subdirectory.

## IIS layout

The existing IIS site is expected to be:

- Hostname: fip.aviatechsystems.com
- Parent site: ATS.FIP
- Frontend directory: C:\inetpub\wwwroot\ATS.FIP\WEB
- Frontend app pool: ATS.FIP
- API child application alias: API
- API directory: C:\inetpub\wwwroot\ATS.FIP\API
- API app pool: ATS.FIP.API

The workflow only deploys to the two configured directories. It stops and starts only the `ATS.FIP.API` app pool; unrelated IIS applications are not affected. The API `app_offline.htm` mechanism and the stopped app pool are used to release application files during the copy.

The runner account must have read/execute access to the repository and write/modify access to both deployment directories and `C:\inetpub\backups\ATS.FIP`. It also needs permission to run dotnet, node, npm, and robocopy. Administrator rights are not required by the workflow itself, but IIS administrators may need to grant the runner service account filesystem permissions and ensure the API app pool can read the deployed files and production configuration.

## Routing and Angular IIS configuration

The frontend calls relative URLs such as /api/flights. The Angular public/web.config is copied into the production output by the Angular build.

Its first rewrite rule maps public /api/... requests to the IIS child application at /API/.... The controller keeps its existing api/flights route and also exposes a flights route alias. This lets the child application receive /flights without producing /api/api/flights, while local development through the Angular proxy continues to use /api/flights.

The SPA fallback rewrites non-file, non-directory routes such as /flights/123 to index.html. API requests are excluded from that fallback. IIS URL Rewrite must be installed on the server.

## Required server software

The deployment runner/server needs:

- .NET 10 SDK for CI and the .NET 10 ASP.NET Core Hosting Bundle on the IIS server.
- Node.js 22.x and npm for the deployment runner.
- IIS with ASP.NET Core Module V2 installed by the Hosting Bundle.
- IIS URL Rewrite installed for Angular SPA fallback and API child-application routing.
- The existing IIS site and child application configuration described above.

The API production connection string must be supplied outside the repository. Do not put database credentials in workflow YAML.

## Normal release flow

1. Develop and commit changes on master.
2. Push to master and confirm build-master.yml succeeds.
3. Merge master into fip-demo locally or through the repository's normal review process.
4. Push the updated fip-demo branch.
5. In GitHub Actions, open deploy-fip-demo.yml and select Run workflow. The workflow checks out fip-demo itself and does not deploy from master.
6. Confirm the workflow's frontend and API verification steps succeed.

The deployment workflow is intentionally not triggered by a push.

## Verification

After deployment, verify:

- https://fip.aviatechsystems.com/ returns the Angular application.
- Refreshing https://fip.aviatechsystems.com/flights/123 returns the Angular application instead of a 404.
- https://fip.aviatechsystems.com/api/flights returns an HTTP 200 response.
- Browser developer tools show frontend calls to /api/..., not /api/api/....
- The API child application remains mapped to /API and its generated ASP.NET Core web.config remains in C:\inetpub\wwwroot\ATS.FIP\API.

## Rollback

Before modifying IIS content, the workflow creates a timestamped file backup at:

- `C:\inetpub\backups\ATS.FIP\<yyyyMMdd-HHmmss>-<run-id>\WEB`
- `C:\inetpub\backups\ATS.FIP\<yyyyMMdd-HHmmss>-<run-id>\API`

If deployment or either post-deployment health check fails, the workflow automatically restores the backed-up WEB and API files. It reports copy failures separately from health-check failures and reports rollback failures separately from the original deployment failure. The API app pool is stopped during recovery and restarted before rollback verification; a final recovery path also attempts to restart it and remove `app_offline.htm` if an earlier step fails.

The frontend rollback restores the previous static-file manifest. The frontend deployment only removes files previously recorded by that manifest, so unrelated persistent files are not treated as obsolete. API production configuration files matching `appsettings*.json`, uploaded files, and other files not removed by the workflow are preserved.

For a manual rollback, identify a known-good `fip-demo` commit and run `deploy-fip-demo.yml` manually from that branch state. The workflow does not run database migrations and cannot automatically reverse database changes; database changes must be rolled back separately. File backups also do not restore changes made outside the backed-up WEB and API directories.

## Troubleshooting

- Runner not selected: confirm the runner is online and has all four labels: self-hosted, Windows, X64, and fip-demo.
- Frontend build fails: run npm ci and npm run build -- --configuration production from apps/fip-web.
- API build fails: install the .NET 10 SDK and run dotnet restore src/Hosts/Fip.Api/Fip.Api.csproj followed by dotnet build src/Hosts/Fip.Api/Fip.Api.csproj -c Release.
- Angular route refresh returns 404: verify IIS URL Rewrite is installed and the deployed frontend web.config exists in the WEB directory.
- /api is handled by Angular: verify the API child application alias is exactly API and that the frontend web.config API rewrite rule appears before the SPA fallback rule.
- API returns 500: verify the API app pool identity can read the production configuration and connect to SQL Server. The workflow does not run migrations or print connection strings.
- Copy fails with access denied or ERROR 32: grant the runner service account Modify permission on the WEB and API directories, ensure the API app pool can be taken offline through `app_offline.htm`, and inspect the workflow diagnostics for separate staging-source and IIS-destination lock checks.