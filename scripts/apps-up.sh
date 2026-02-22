#!/usr/bin/env bash
set -euo pipefail

if [[ ! -f deploy/docker/.env ]]; then
  echo "Missing deploy/docker/.env. Copy deploy/docker/.env.example to deploy/docker/.env and set local secrets."
  exit 1
fi

docker compose -f deploy/docker/docker-compose.yml -f deploy/docker/docker-compose.apps.yml --env-file deploy/docker/.env up -d --build

echo "Infra + app stack is up."
