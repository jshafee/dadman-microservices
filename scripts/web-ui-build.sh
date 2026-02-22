#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

cd "${repo_root}/src/Web/web-ui"
npm ci
npm run build
rm -rf ../Web.Bff/wwwroot/*
if [[ -d dist ]]; then
  cp -R dist/* ../Web.Bff/wwwroot/
elif [[ -d build ]]; then
  cp -R build/* ../Web.Bff/wwwroot/
else
  echo "No dist/ or build/ output found after UI build." >&2
  exit 1
fi

echo "Web UI built and copied into src/Web/Web.Bff/wwwroot"
