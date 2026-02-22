#!/usr/bin/env bash
set -euo pipefail

name="${1:-}"
if [[ -z "${name}" ]]; then
  echo "Usage: ./scripts/ef-add-migration.sh <MigrationName>"
  exit 1
fi

dotnet tool restore

dotnet ef migrations add "${name}" --project src/Services/Catalog/Catalog.Infrastructure --startup-project src/Services/Catalog/Catalog.Api

dotnet ef migrations add "${name}" --project src/Services/Ordering/Ordering.Infrastructure --startup-project src/Services/Ordering/Ordering.Api

echo "Added migration '${name}' to Catalog and Ordering."
