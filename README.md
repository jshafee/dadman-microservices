# Dadman Microservices Backend

Backend-first .NET 10 microservices monorepo with a React SPA + Web BFF track.

## Repository Layout

- `src/BuildingBlocks` - shared backend building blocks
- `src/Gateway` - API gateway applications
- `src/Services` - domain microservices
- `src/Workers` - background workers
- `src/Bootstrap/Bootstrap.Api` - minimal bootstrap API placeholder
- `src/Web/Web.Bff` - cookie-based Web BFF hosting the SPA in production
- `src/Web/web-ui` - React + TypeScript + Vite SPA
- `tests/Bootstrap.UnitTests` - bootstrap unit tests
- `deploy/docker` - local infrastructure assets
- `scripts` - local scripts
- `docs/adr` - architecture decision records
- `.github/workflows` - CI workflows

## Prerequisites

- .NET SDK 10.0.103 (pinned by `global.json`)
- Node.js 20+
- Docker Desktop or Docker Engine with Compose plugin

## Run (bootstrap)

```bash
dotnet run --project src/Bootstrap/Bootstrap.Api
```

```bash
curl http://localhost:5087/health
```

## Run (Web BFF + UI)

Production-style (serve SPA from Web.Bff):

Windows:

PowerShell 5.1:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\web-ui-build.ps1
dotnet run --project src/Web/Web.Bff
```

PowerShell 7:

```powershell
pwsh .\scripts\web-ui-build.ps1
dotnet run --project src/Web/Web.Bff
```

Linux/macOS:

```bash
bash ./scripts/web-ui-build.sh
dotnet run --project src/Web/Web.Bff
```

Development (Vite proxy + BFF):

Windows (PowerShell):

```powershell
pwsh ./scripts/web-dev.ps1
```

Linux/macOS (bash):

```bash
bash ./scripts/web-dev.sh
```

Vite proxies `/bff/*` requests to `http://localhost:5087` (Vite default: `http://localhost:5173`, Web.Bff: `http://localhost:5087`).

## Web smoke check

Run a lightweight non-Playwright smoke verification:

```bash
bash ./scripts/web-smoke.sh
```

It verifies:
- `GET /` returns `200`
- `GET /bff/me` returns `401` before login

## Build and test

```bash
dotnet build -c Release
dotnet test -c Release
```

Web UI build:

```bash
cd src/Web/web-ui
npm ci
npm run build
```


## Build .NET service container images (reusable Dockerfile)

A single reusable Dockerfile is available at `deploy/docker/Dockerfile.dotnet`.

Example builds from repository root:

```bash
docker build -f deploy/docker/Dockerfile.dotnet --build-arg PROJECT=src/Services/Catalog/Catalog.Api/Catalog.Api.csproj -t dadman/catalog-api .
docker build -f deploy/docker/Dockerfile.dotnet --build-arg PROJECT=src/Services/Ordering/Ordering.Api/Ordering.Api.csproj -t dadman/ordering-api .
docker build -f deploy/docker/Dockerfile.dotnet --build-arg PROJECT=src/Gateway/Gateway.Api/Gateway.Api.csproj -t dadman/gateway-api .
docker build -f deploy/docker/Dockerfile.dotnet --build-arg PROJECT=src/Workers/Integration/Integration.Worker/Integration.Worker.csproj -t dadman/integration-worker .
```

Runtime notes:
- Container port is `8080` (`ASPNETCORE_URLS=http://+:8080`).
- Dockerfile uses `ENTRYPOINT ["dotnet"]`; set the service DLL via container command in compose/run.

## Infrastructure (docker compose)

Files under `deploy/docker`:
- `docker-compose.yml`
- `.env.example`
- `otel-collector-config.yml`
- `prometheus.yml`

1) Create a local env file (git-ignored) and set real secrets there:

```bash
cp deploy/docker/.env.example deploy/docker/.env
```

2) Start stack:

```bash
docker compose -f deploy/docker/docker-compose.yml --env-file deploy/docker/.env up -d
```

3) Stop stack:

```bash
docker compose -f deploy/docker/docker-compose.yml --env-file deploy/docker/.env down -v
```

Windows helpers:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\infra-up.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\infra-down.ps1
```

Linux/macOS helpers:

```bash
bash ./scripts/infra-up.sh
bash ./scripts/infra-down.sh
```


Full stack (infra + apps) with Docker Compose:

Windows (PowerShell):

```powershell
./scripts/apps-up.ps1
./scripts/apps-down.ps1
```

Linux/macOS (bash):

```bash
./scripts/apps-up.sh
./scripts/apps-down.sh
```

App ports when full stack is running:
- Gateway: `http://localhost:5100`
- Catalog API: `http://localhost:5101`
- Ordering API: `http://localhost:5102`

Quick smoke flow via gateway with a dev JWT:

```bash
TOKEN=$(dotnet run --project src/Tools/DevJwt -- \
  --issuer "$AUTH_ISSUER" \
  --audience "$AUTH_AUDIENCE" \
  --key "$AUTH_SIGNING_KEY" \
  --sub "dev-user" \
  --scopes "catalog.read,ordering.write" \
  --minutes 60)

curl -H "Authorization: Bearer $TOKEN" "http://localhost:5100/catalog/items?api-version=1.0"
```


Observability defaults in Docker stack:
- Prometheus scrapes the OTEL Collector exporter at `otel-collector:9464`.
- Grafana is pre-provisioned with:
  - Prometheus datasource (`http://prometheus:9090`)
  - Jaeger datasource (`http://jaeger:16686`)
  - a "Targets Up" dashboard based on Prometheus `up` and `scrape_duration_seconds` metrics.
- OTEL Collector health endpoint is available at `http://localhost:13133`.
- Jaeger traces appear after calling APIs (for example via Gateway routes) with OTEL export enabled.
- Logging uses Serilog + Seq as the primary log pipeline; OpenTelemetry logging export is intentionally not enabled to avoid duplicate log entries.


Seq usage:
- Open Seq at `http://localhost:5341`.
- Filter by service with properties such as `service.name = "gateway-api"`, `service.name = "catalog-api"`, `service.name = "ordering-api"`, or `service.name = "integration-worker"`.
- Correlation is attached via `CorrelationId` scope, and trace/span context is enriched when available.

Service URLs/ports:
- SQL Server: `localhost:1433`
- RabbitMQ AMQP: `localhost:5672`
- RabbitMQ Management: http://localhost:15672
- Seq: http://localhost:5341

Default local credentials are read from `deploy/docker/.env` (copied from `.env.example`).
Set real values only in `deploy/docker/.env` (git-ignored) or environment variables. Never commit real credentials. Required values: `SA_PASSWORD` (or `MSSQL_SA_PASSWORD`), `RABBITMQ_DEFAULT_USER`, `RABBITMQ_DEFAULT_PASS`, `SEQ_PASSWORD`, and `GRAFANA_ADMIN_PASSWORD`.
- OpenTelemetry Collector OTLP gRPC: `localhost:4317`
- OpenTelemetry Collector OTLP HTTP: `localhost:4318`
- Jaeger UI: http://localhost:16686
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000

## Microservices Core Track (Catalog + Ordering + Gateway + Worker)

Projects:
- `src/Services/Catalog/*` (`Api`, `Application`, `Domain`, `Infrastructure`, `Contracts`)
- `src/Services/Ordering/*` (`Api`, `Application`, `Domain`, `Infrastructure`, `Contracts`)
- `src/Gateway/Gateway.Api` (YARP reverse proxy)
- `src/Workers/Integration/Integration.Worker`

Key implementation notes:
- Catalog and Ordering each use a dedicated SQL Server database (`CatalogDb`, `OrderingDb`).
- Both services use `BuildingBlocks.ServiceDefaults` for health (`/health`), correlation, request logging, and OTLP OpenTelemetry export (traces + metrics).
- RabbitMQ is used for async integration via MassTransit.
- Reliability uses MassTransit EF transactional outbox/inbox (`AddEntityFrameworkOutbox`, `UseEntityFrameworkOutbox`).
- Service consumers use explicit MassTransit `ReceiveEndpoint(...)` queue names (no mixed auto-endpoint configuration) to avoid duplicate queues.
- Gateway routes:
  - `/catalog/*` -> `http://localhost:5101`
  - `/ordering/*` -> `http://localhost:5102`
- Gateway resiliency middlewares:
  - Rate limiting policy `fixed` (per authenticated user `sub` or client IP): default 120 requests/minute, queue 20.
  - Rate limiting policy `write` (stricter for `POST/PUT/PATCH/DELETE`): default 20 requests/minute, queue 5.
  - Output cache policy `catalog-get`: caches safe catalog GET/HEAD proxy responses for 15 seconds and varies by `api-version` query value.
  - `/health` remains outside proxy route policies (no route-level throttling/cache metadata applied).
- Tuning: adjust limits/TTL in `src/Gateway/Gateway.Api/Program.cs` (`AddRateLimiter` and `AddOutputCache`) and route-policy bindings in `src/Gateway/Gateway.Api/appsettings.json` metadata.
- Ordering write flow validates catalog item existence via a resilient outbound HTTP call from Ordering API to Catalog API before creating an order.

Run services (separate terminals):

```bash
dotnet run --project src/Services/Catalog/Catalog.Api --launch-profile http
dotnet run --project src/Services/Ordering/Ordering.Api --launch-profile http
dotnet run --project src/Gateway/Gateway.Api --launch-profile http
dotnet run --project src/Workers/Integration/Integration.Worker
```

Windows helper scripts:
- `scripts/run-core-track.ps1` starts Catalog, Ordering, Gateway, and Worker in separate PowerShell windows.
- `scripts/migrate-services.ps1` (legacy helper) loads `deploy/docker/.env` and applies EF Core updates for Catalog and Ordering.

Health checks:
- Catalog: `http://localhost:5101/health`
- Ordering: `http://localhost:5102/health`
- Gateway: `http://localhost:5100/health`


API versioning and OpenAPI (Catalog + Ordering):
- Endpoints use query-string API versioning via `api-version`.
- Examples:
  - `GET /catalog/items?api-version=1.0`
  - `GET /catalog/items?api-version=2.0`
  - `GET /ordering/orders?api-version=1.0`
- OpenAPI JSON is exposed in development at:
  - `/openapi/v1.json`
  - `/openapi/v2.json`
- OpenAPI endpoints are mapped with anonymous access so docs can be retrieved even when API auth is enabled.

Environment-variable first configuration (no secrets in repo):
- `ConnectionStrings__CatalogDb`
- `ConnectionStrings__OrderingDb`
- `RabbitMq__Host`
- `RabbitMq__Username`
- `RabbitMq__Password`
- `Auth__Issuer`
- `Auth__Audience`
- `Auth__SigningKey`
- `Services__Catalog__BaseUrl` (Ordering -> Catalog validation call target)
- `Services__Catalog__ServiceToken` (machine token used by Ordering for Catalog validation calls)

The checked-in service `appsettings.json` files use placeholder values only; override with environment variables (for example `RabbitMq__Password` and `ConnectionStrings__*`) in local/dev environments.

Auth baseline:
- Gateway, Catalog, and Ordering validate JWT bearer tokens from the `Auth` configuration section (`Issuer`, `Audience`, `SigningKey`).
- `Auth:SigningKey` in checked-in `appsettings.json` files is a placeholder; set a real value via environment variable (for example `Auth__SigningKey`) for real usage.
- Use `src/Tools/DevJwt` to generate local development JWTs.
- Example dev service token (scope `catalog.read`) for `Services__Catalog__ServiceToken`:

```bash
dotnet run --project src/Tools/DevJwt -- \
  --issuer "https://auth.local" \
  --audience "dadman-api" \
  --key "$Auth__SigningKey" \
  --sub "ordering-api" \
  --scopes "catalog.read" \
  --minutes 60
```


## EF Core migrations workflow (Catalog + Ordering)

Standardized migration tooling uses `dotnet-ef` from the local tool manifest (`.config/dotnet-tools.json`).

1) Restore local tools:

```bash
dotnet tool restore
```

2) Create a migration for both services:

Windows (PowerShell):

```powershell
./scripts/ef-add-migration.ps1 InitialCreate
```

Linux/macOS (bash):

```bash
./scripts/ef-add-migration.sh InitialCreate
```

3) Apply migrations to databases:

Set environment variables first (examples):

```bash
export ConnectionStrings__CatalogDb="Server=localhost,1433;Database=CatalogDb;User Id=sa;Password=<YOUR_PASSWORD>;TrustServerCertificate=true"
export ConnectionStrings__OrderingDb="Server=localhost,1433;Database=OrderingDb;User Id=sa;Password=<YOUR_PASSWORD>;TrustServerCertificate=true"
```

Then run:

Windows (PowerShell):

```powershell
./scripts/ef-update-db.ps1
```

Linux/macOS (bash):

```bash
./scripts/ef-update-db.sh
```

Docker compose usage:
- Put real secrets only in `deploy/docker/.env` (git-ignored).
- Export `ConnectionStrings__CatalogDb` and `ConnectionStrings__OrderingDb` from those values before running the update script.

Commit migration code to source control; never commit real passwords or secret values.

## CI

GitHub Actions runs on pull requests and pushes to `main`.

The workflow executes:

```bash
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
cd src/Web/web-ui && npm ci && npm run build
bash ./scripts/web-smoke.sh
```
