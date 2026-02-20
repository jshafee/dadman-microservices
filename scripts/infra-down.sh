#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"

cd "${repo_root}/deploy/docker"

if [[ ! -f .env ]]; then
  echo "Missing deploy/docker/.env. Copy .env.example first:"
  echo "  cp .env.example .env"
  exit 1
fi

docker compose down -v
