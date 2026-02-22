#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${ConnectionStrings__CatalogDb:-}" ]]; then
  echo "Set ConnectionStrings__CatalogDb before running this script."
  exit 1
fi

if [[ -z "${ConnectionStrings__OrderingDb:-}" ]]; then
  echo "Set ConnectionStrings__OrderingDb before running this script."
  exit 1
fi

dotnet tool restore

dotnet ef database update --project src/Services/Catalog/Catalog.Infrastructure --startup-project src/Services/Catalog/Catalog.Api

dotnet ef database update --project src/Services/Ordering/Ordering.Infrastructure --startup-project src/Services/Ordering/Ordering.Api

echo "Applied Catalog and Ordering migrations."
