#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"
log_file="$(mktemp)"

cleanup() {
  if [[ -n "${BFF_PID:-}" ]] && kill -0 "${BFF_PID}" 2>/dev/null; then
    kill "${BFF_PID}" 2>/dev/null || true
    wait "${BFF_PID}" 2>/dev/null || true
  fi
  rm -f "${log_file}"
}
trap cleanup EXIT INT TERM

cd "${repo_root}"
dotnet build src/Web/Web.Bff/Web.Bff.csproj >/dev/null
dotnet run --project src/Web/Web.Bff --no-build >"${log_file}" 2>&1 &
BFF_PID=$!

ready=0
for _ in {1..30}; do
  if curl -fsS http://localhost:5087/health >/dev/null 2>&1; then
    ready=1
    break
  fi
  sleep 1
done

if [[ "${ready}" != "1" ]]; then
  echo "Web.Bff did not become ready at http://localhost:5087/health"
  echo "--- Web.Bff log ---"
  cat "${log_file}"
  exit 1
fi

root_status="$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5087/)"
if [[ "${root_status}" != "200" ]]; then
  echo "Expected GET / to return 200 but got ${root_status}"
  echo "--- Web.Bff log ---"
  cat "${log_file}"
  exit 1
fi

me_status="$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5087/bff/me)"
if [[ "${me_status}" != "401" ]]; then
  echo "Expected GET /bff/me to return 401 before login but got ${me_status}"
  echo "--- Web.Bff log ---"
  cat "${log_file}"
  exit 1
fi

echo "Smoke checks passed: GET / => 200, GET /bff/me => 401"
