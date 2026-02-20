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

```powershell
pwsh ./scripts/web-ui-build.ps1
dotnet run --project src/Web/Web.Bff
```

Development (Vite proxy + BFF):

```powershell
pwsh ./scripts/web-dev.ps1
```

Vite proxies `/bff/*` requests to `http://localhost:5087`.

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

Windows PowerShell helpers:

```powershell
pwsh ./scripts/infra-up.ps1
pwsh ./scripts/infra-down.ps1
```

## CI

GitHub Actions runs on pull requests and pushes to `main`.

The workflow executes:

```bash
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
cd src/Web/web-ui && npm ci && npm run build
```
