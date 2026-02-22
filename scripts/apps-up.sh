#!/usr/bin/env bash
set -euo pipefail

if [[ ! -f deploy/docker/.env ]]; then
  echo "Missing deploy/docker/.env. Copy deploy/docker/.env.example to deploy/docker/.env and set local secrets."
  exit 1
fi

echo "Validating compose configuration..."
docker compose \
  -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.apps.yml \
  --env-file deploy/docker/.env \
  config >/dev/null

echo "Starting infra services..."
docker compose -f deploy/docker/docker-compose.yml --env-file deploy/docker/.env up -d

echo "Building app images..."
docker compose \
  -f deploy/docker/docker-compose.yml \
  -f deploy/docker/docker-compose.apps.yml \
  --env-file deploy/docker/.env \
  build catalog-api ordering-api gateway-api integration-worker catalog-migrator ordering-migrator

echo "Running Catalog migrations..."
docker compose -f deploy/docker/docker-compose.yml -f deploy/docker/docker-compose.apps.yml --env-file deploy/docker/.env run --rm catalog-migrator

echo "Running Ordering migrations..."
docker compose -f deploy/docker/docker-compose.yml -f deploy/docker/docker-compose.apps.yml --env-file deploy/docker/.env run --rm ordering-migrator

echo "Starting app services..."
docker compose -f deploy/docker/docker-compose.yml -f deploy/docker/docker-compose.apps.yml --env-file deploy/docker/.env up -d catalog-api ordering-api gateway-api integration-worker

echo "Infra + app stack is up (migrations applied)."
