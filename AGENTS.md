# Repository Agent Guide

## Scope
These instructions apply to the entire repository.

## Purpose
Backend-only .NET 10 microservices monorepo bootstrap.

## Must-pass commands
- `dotnet build -c Release`
- `dotnet test -c Release`

## Rules
1. Prefer adding reusable code to `src/BuildingBlocks`.
2. Keep gateway code under `src/Gateway`, service code under `src/Services`, and worker code under `src/Workers`.
3. Keep package versions in `Directory.Packages.props`.
4. Keep shared MSBuild defaults in `Directory.Build.props`.
5. Update `README.md` whenever build or test commands change.
