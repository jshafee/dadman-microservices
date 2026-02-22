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
if [[ "$prod_mode" == "true" ]]; then
  compose_files+=(-f deploy/docker/docker-compose.prod.yml)
fi
compose_files+=(-f deploy/docker/docker-compose.apps.yml)

echo "Validating compose configuration..."
docker compose \
  "${compose_files[@]}" \
  --env-file deploy/docker/.env \
  config >/dev/null

echo "Starting infra services..."
docker compose "${compose_files[@]}" --env-file deploy/docker/.env up -d sqlserver rabbitmq seq otel-collector jaeger prometheus grafana redis

echo "Building app images..."
docker compose \
  "${compose_files[@]}" \
  --env-file deploy/docker/.env \
  build catalog-api ordering-api gateway-api integration-worker catalog-migrator ordering-migrator

echo "Running Catalog migrations..."
docker compose "${compose_files[@]}" --env-file deploy/docker/.env run --rm catalog-migrator

echo "Running Ordering migrations..."
docker compose "${compose_files[@]}" --env-file deploy/docker/.env run --rm ordering-migrator

echo "Starting app services..."
docker compose "${compose_files[@]}" --env-file deploy/docker/.env up -d catalog-api ordering-api gateway-api integration-worker

echo "Infra + app stack is up (migrations applied)."
if [[ "$prod_mode" == "true" ]]; then
  echo "Production overlay is active (docker-compose.prod.yml)."
fi
