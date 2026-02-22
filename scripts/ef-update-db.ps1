$ErrorActionPreference = "Stop"

dotnet tool restore

if (-not $env:ConnectionStrings__CatalogDb) {
  throw "Set ConnectionStrings__CatalogDb before running this script."
}

if (-not $env:ConnectionStrings__OrderingDb) {
  throw "Set ConnectionStrings__OrderingDb before running this script."
}

dotnet ef database update --project src/Services/Catalog/Catalog.Infrastructure --startup-project src/Services/Catalog/Catalog.Api

dotnet ef database update --project src/Services/Ordering/Ordering.Infrastructure --startup-project src/Services/Ordering/Ordering.Api

Write-Host "Applied Catalog and Ordering migrations."
