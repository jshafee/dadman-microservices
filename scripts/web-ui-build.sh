#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"
ui_path="${repo_root}/src/Web/web-ui"
bff_wwwroot="${repo_root}/src/Web/Web.Bff/wwwroot"

pushd "${ui_path}" >/dev/null
npm ci
npm run build
popd >/dev/null

rm -rf "${bff_wwwroot}"
mkdir -p "${bff_wwwroot}"
cp -R "${ui_path}/dist/." "${bff_wwwroot}/"

echo "Web UI built and copied to ${bff_wwwroot}"
