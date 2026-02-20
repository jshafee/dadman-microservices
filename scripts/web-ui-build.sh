#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

cd "${repo_root}/src/Web/web-ui"
npm ci
npm run build
rm -rf ../Web.Bff/wwwroot/*
cp -R dist/* ../Web.Bff/wwwroot/

echo "Web UI built and copied into src/Web/Web.Bff/wwwroot"
