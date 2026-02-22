$ErrorActionPreference = "Stop"

$envFile = "deploy/docker/.env"
if (-not (Test-Path $envFile)) {
  throw "Missing $envFile. Copy deploy/docker/.env.example to deploy/docker/.env and set local secrets."
}

Write-Host "Validating compose configuration..."
docker compose `
  -f deploy/docker/docker-compose.yml `
  -f deploy/docker/docker-compose.apps.yml `
  --env-file $envFile `
  config | Out-Null

Write-Host "Starting infra services..."
docker compose -f deploy/docker/docker-compose.yml --env-file $envFile up -d

Write-Host "Building app images..."
docker compose `
  -f deploy/docker/docker-compose.yml `
  -f deploy/docker/docker-compose.apps.yml `
  --env-file $envFile `
  build catalog-api ordering-api gateway-api integration-worker catalog-migrator ordering-migrator

Write-Host "Running Catalog migrations..."
docker compose -f deploy/docker/docker-compose.yml -f deploy/docker/docker-compose.apps.yml --env-file $envFile run --rm catalog-migrator

Write-Host "Running Ordering migrations..."
docker compose -f deploy/docker/docker-compose.yml -f deploy/docker/docker-compose.apps.yml --env-file $envFile run --rm ordering-migrator

Write-Host "Starting app services..."
docker compose -f deploy/docker/docker-compose.yml -f deploy/docker/docker-compose.apps.yml --env-file $envFile up -d catalog-api ordering-api gateway-api integration-worker

Write-Host "Infra + app stack is up (migrations applied)."
