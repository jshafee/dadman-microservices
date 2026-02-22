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
$composeFiles += @("-f", "deploy/docker/docker-compose.apps.yml")
if ($prodMode) {
  $composeFiles += @("-f", "deploy/docker/docker-compose.prod.yml")
}

docker compose @composeFiles --env-file $envFile down

Write-Host "Infra + app stack is down."
if ($prodMode) {
  Write-Host "Production overlay was active (docker-compose.prod.yml)."
}
