#!/usr/bin/env bash
set -euo pipefail

prod_mode="false"
if [[ "${1:-}" == "--prod" ]] || [[ "${COMPOSE_PROD:-false}" == "true" ]]; then
  prod_mode="true"
fi

if [[ ! -f deploy/docker/.env ]]; then
  echo "Missing deploy/docker/.env. Copy deploy/docker/.env.example to deploy/docker/.env and set local secrets."
  exit 1
fi

compose_files=(-f deploy/docker/docker-compose.yml)
compose_files+=(-f deploy/docker/docker-compose.apps.yml)
if [[ "$prod_mode" == "true" ]]; then
  compose_files+=(-f deploy/docker/docker-compose.prod.yml)
fi

docker compose "${compose_files[@]}" --env-file deploy/docker/.env down

echo "Infra + app stack is down."
if [[ "$prod_mode" == "true" ]]; then
  echo "Production overlay was active (docker-compose.prod.yml)."
fi
