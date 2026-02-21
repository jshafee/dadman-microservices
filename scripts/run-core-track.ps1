$ErrorActionPreference = "Stop"

$envFile = "deploy/docker/.env"
if (Test-Path $envFile) {
  Get-Content $envFile |
    Where-Object { $_ -and -not $_.Trim().StartsWith("#") } |
    ForEach-Object {
      $parts = $_ -split "=", 2
      if ($parts.Length -eq 2) {
        [System.Environment]::SetEnvironmentVariable($parts[0].Trim(), $parts[1].Trim())
      }
    }
}

if ($env:MSSQL_SA_PASSWORD -or $env:SA_PASSWORD) {
  $saPassword = if ($env:MSSQL_SA_PASSWORD) { $env:MSSQL_SA_PASSWORD } else { $env:SA_PASSWORD }
  [System.Environment]::SetEnvironmentVariable("ConnectionStrings__CatalogDb", "Server=localhost,1433;Database=CatalogDb;User Id=sa;Password=$saPassword;TrustServerCertificate=true")
  [System.Environment]::SetEnvironmentVariable("ConnectionStrings__OrderingDb", "Server=localhost,1433;Database=OrderingDb;User Id=sa;Password=$saPassword;TrustServerCertificate=true")
}

if ($env:RABBITMQ_DEFAULT_USER) { [System.Environment]::SetEnvironmentVariable("RabbitMq__Username", $env:RABBITMQ_DEFAULT_USER) }
if ($env:RABBITMQ_DEFAULT_PASS) { [System.Environment]::SetEnvironmentVariable("RabbitMq__Password", $env:RABBITMQ_DEFAULT_PASS) }
[System.Environment]::SetEnvironmentVariable("RabbitMq__Host", "localhost")

Start-Process pwsh -ArgumentList '-NoExit','-Command','dotnet run --project src/Services/Catalog/Catalog.Api --launch-profile http'
Start-Sleep -Seconds 1
Start-Process pwsh -ArgumentList '-NoExit','-Command','dotnet run --project src/Services/Ordering/Ordering.Api --launch-profile http'
Start-Sleep -Seconds 1
Start-Process pwsh -ArgumentList '-NoExit','-Command','dotnet run --project src/Gateway/Gateway.Api --launch-profile http'
Start-Sleep -Seconds 1
Start-Process pwsh -ArgumentList '-NoExit','-Command','dotnet run --project src/Workers/Integration/Integration.Worker'

Write-Host "Started Catalog (5101), Ordering (5102), Gateway (5100), Worker."
