$ErrorActionPreference = "Stop"

$envFile = "deploy/docker/.env"
if (-not (Test-Path $envFile)) {
  throw "Missing $envFile. Copy deploy/docker/.env.example to deploy/docker/.env and set local secrets."
}

docker compose -f deploy/docker/docker-compose.yml -f deploy/docker/docker-compose.apps.yml --env-file $envFile up -d --build

Write-Host "Infra + app stack is up."
