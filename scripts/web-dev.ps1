$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$bffProject = Join-Path $repoRoot 'src/Web/Web.Bff/Web.Bff.csproj'
$uiPath = Join-Path $repoRoot 'src/Web/web-ui'

Start-Process pwsh -ArgumentList @('-NoExit', '-Command', "dotnet run --project '$bffProject'")
Start-Process pwsh -ArgumentList @('-NoExit', '-Command', "Set-Location '$uiPath'; npm ci; npm run dev")

Write-Host 'Started Web.Bff and Vite dev server in separate PowerShell windows.'
