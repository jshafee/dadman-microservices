param(
  [switch]$Prod
)

$ErrorActionPreference = "Stop"

$envFile = "deploy/docker/.env"
if (-not (Test-Path $envFile)) {
  throw "Missing $envFile. Copy deploy/docker/.env.example to deploy/docker/.env and set local secrets."
}

$prodMode = $Prod -or ($env:COMPOSE_PROD -eq "true")
$composeFiles = @("-f", "deploy/docker/docker-compose.yml")
if ($prodMode) {
  $composeFiles += @("-f", "deploy/docker/docker-compose.prod.yml")
}
$composeFiles += @("-f", "deploy/docker/docker-compose.apps.yml")

Write-Host "Validating compose configuration..."
docker compose @composeFiles --env-file $envFile config | Out-Null

Write-Host "Starting infra services..."
docker compose @composeFiles --env-file $envFile up -d sqlserver rabbitmq seq otel-collector jaeger prometheus grafana redis

Write-Host "Building app images..."
docker compose @composeFiles --env-file $envFile build catalog-api ordering-api gateway-api integration-worker catalog-migrator ordering-migrator

Write-Host "Running Catalog migrations..."
docker compose @composeFiles --env-file $envFile run --rm catalog-migrator

Write-Host "Running Ordering migrations..."
docker compose @composeFiles --env-file $envFile run --rm ordering-migrator

Write-Host "Starting app services..."
docker compose @composeFiles --env-file $envFile up -d catalog-api ordering-api gateway-api integration-worker

Write-Host "Infra + app stack is up (migrations applied)."
if ($prodMode) {
  Write-Host "Production overlay is active (docker-compose.prod.yml)."
}
