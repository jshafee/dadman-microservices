$ErrorActionPreference = 'Stop'

Push-Location "deploy/docker"
try {
    if (-not (Test-Path ".env")) {
        Write-Host "Missing deploy/docker/.env. Copy .env.example first:" -ForegroundColor Yellow
        Write-Host "  Copy-Item .env.example .env"
        exit 1
    }

    docker compose up -d
}
finally {
    Pop-Location
}
