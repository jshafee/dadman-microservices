$ErrorActionPreference = "Stop"

$envFile = "deploy/docker/.env"
if (-not (Test-Path $envFile)) {
  throw "Missing $envFile. Copy deploy/docker/.env.example to deploy/docker/.env and set local secrets."
}

Get-Content $envFile |
  Where-Object { $_ -and -not $_.Trim().StartsWith("#") } |
  ForEach-Object {
    $parts = $_ -split "=", 2
    if ($parts.Length -eq 2) {
      [System.Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim())
    }
  }

$saPassword = $env:SA_PASSWORD
if (-not $saPassword) {
  throw "Set SA_PASSWORD in deploy/docker/.env"
}

$env:ConnectionStrings__CatalogDb = "Server=localhost,1433;Database=CatalogDb;User Id=sa;Password=$saPassword;TrustServerCertificate=true"
$env:ConnectionStrings__OrderingDb = "Server=localhost,1433;Database=OrderingDb;User Id=sa;Password=$saPassword;TrustServerCertificate=true"
$env:RabbitMq__Host = if ($env:RabbitMq__Host) { $env:RabbitMq__Host } else { "localhost" }
$env:RabbitMq__Username = if ($env:RABBITMQ_DEFAULT_USER) { $env:RABBITMQ_DEFAULT_USER } else { "admin" }
$env:RabbitMq__Password = $env:RABBITMQ_DEFAULT_PASS

if (-not $env:RabbitMq__Password) {
  throw "Set RABBITMQ_DEFAULT_PASS in deploy/docker/.env"
}

dotnet tool restore

dotnet ef database update --project src/Services/Catalog/Catalog.Infrastructure --startup-project src/Services/Catalog/Catalog.Api
dotnet ef database update --project src/Services/Ordering/Ordering.Infrastructure --startup-project src/Services/Ordering/Ordering.Api

Write-Host "Catalog and Ordering migrations applied."
