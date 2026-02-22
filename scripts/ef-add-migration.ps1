param(
  [Parameter(Mandatory = $true)]
  [string]$Name
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Name)) {
  throw "Migration name is required. Example: ./scripts/ef-add-migration.ps1 InitialCreate"
}

dotnet tool restore

dotnet ef migrations add $Name --project src/Services/Catalog/Catalog.Infrastructure --startup-project src/Services/Catalog/Catalog.Api

dotnet ef migrations add $Name --project src/Services/Ordering/Ordering.Infrastructure --startup-project src/Services/Ordering/Ordering.Api

Write-Host "Added migration '$Name' to Catalog and Ordering."
