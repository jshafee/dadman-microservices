# Dadman Microservices Backend

Backend-only .NET 10 microservices monorepo bootstrap.

## Repository Layout

- `src/BuildingBlocks` - shared backend building blocks
- `src/Gateway` - API gateway applications
- `src/Services` - domain microservices
- `src/Workers` - background workers
- `src/Bootstrap/Bootstrap.Api` - minimal bootstrap API placeholder
- `tests/Bootstrap.UnitTests` - bootstrap unit tests
- `deploy/docker` - local infrastructure assets
- `scripts` - local/CI scripts
- `docs/adr` - architecture decision records
- `.github/workflows` - CI workflows

## Prerequisites

- .NET SDK 10.0.103 (pinned by `global.json`)
- Docker Desktop or Docker Engine with Compose plugin

## Run (bootstrap)

```bash
dotnet run --project src/Bootstrap/Bootstrap.Api
```

```bash
curl http://localhost:5087/health
```

## Build and test

```bash
dotnet build -c Release
dotnet test -c Release
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

GitHub Actions runs on every pull request and on pushes to `main`.

The workflow executes:

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```
