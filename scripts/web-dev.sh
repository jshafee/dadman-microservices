#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

bff_project="${repo_root}/src/Web/Web.Bff/Web.Bff.csproj"
ui_path="${repo_root}/src/Web/web-ui"

cleanup() {
  if [[ -n "${BFF_PID:-}" ]] && kill -0 "${BFF_PID}" 2>/dev/null; then
    kill "${BFF_PID}" 2>/dev/null || true
  fi
  if [[ -n "${UI_PID:-}" ]] && kill -0 "${UI_PID}" 2>/dev/null; then
    kill "${UI_PID}" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

dotnet run --project "${bff_project}" &
BFF_PID=$!

(
  cd "${ui_path}"
  npm ci
  npm run dev
) &
UI_PID=$!

echo "Started Web.Bff (PID ${BFF_PID}) and Vite dev server (PID ${UI_PID}). Press Ctrl+C to stop."
wait
