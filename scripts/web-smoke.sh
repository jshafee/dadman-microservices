#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

url="${WEB_BFF_URL:-http://localhost:5087}"
project="${repo_root}/src/Web/Web.Bff/Web.Bff.csproj"
log_file="$(mktemp)"

cleanup() {
  if [[ -n "${BFF_PID:-}" ]] && kill -0 "${BFF_PID}" 2>/dev/null; then
    kill "${BFF_PID}" 2>/dev/null || true
    wait "${BFF_PID}" 2>/dev/null || true
  fi
  rm -f "${log_file}"
}
trap cleanup EXIT INT TERM

dotnet run --project "${project}" -c Release --no-build >"${log_file}" 2>&1 &
BFF_PID=$!

ready=0
for _ in {1..30}; do
  if curl -fsS "${url}/health" >/dev/null 2>&1; then
    ready=1
    break
  fi
  sleep 1
done

if [[ "${ready}" != "1" ]]; then
  echo "Web.Bff did not become ready at ${url}/health"
  echo "--- Web.Bff log ---"
  cat "${log_file}"
  exit 1
fi

root_status="$(curl -s -o /dev/null -w "%{http_code}" "${url}/" || true)"
if [[ "${root_status}" != "200" ]]; then
  echo "Expected GET / to return 200 but got ${root_status}"
  echo "--- Web.Bff log ---"
  cat "${log_file}"
  exit 1
fi

me_status="$(curl -s -o /dev/null -w "%{http_code}" "${url}/bff/me" || true)"
if [[ "${me_status}" != "401" ]]; then
  echo "Expected GET /bff/me to return 401 before login but got ${me_status}"
  echo "--- Web.Bff log ---"
  cat "${log_file}"
  exit 1
fi

echo "Smoke checks passed: GET / => 200, GET /bff/me => 401"
