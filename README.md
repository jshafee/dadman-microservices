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

## Infrastructure (docker compose)

Files under `deploy/docker`:
- `docker-compose.yml`
- `.env.example`
- `otel-collector-config.yml`
- `prometheus.yml`

1) Create a local env file:

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

Service URLs/ports:
- SQL Server: `localhost:1433`
- RabbitMQ AMQP: `localhost:5672`
- RabbitMQ Management: http://localhost:15672
- Seq: http://localhost:5341
- OpenTelemetry Collector OTLP gRPC: `localhost:4317`
- OpenTelemetry Collector OTLP HTTP: `localhost:4318`
- Jaeger UI: http://localhost:16686
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000

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
