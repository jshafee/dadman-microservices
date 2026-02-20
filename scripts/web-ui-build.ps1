$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$uiPath = Join-Path $repoRoot 'src/Web/web-ui'
$bffWwwroot = Join-Path $repoRoot 'src/Web/Web.Bff/wwwroot'

Push-Location $uiPath
try {
    npm ci
    npm run build
}
finally {
    Pop-Location
}

if (Test-Path $bffWwwroot) {
    Remove-Item -Recurse -Force $bffWwwroot
}

New-Item -ItemType Directory -Path $bffWwwroot | Out-Null
Copy-Item -Recurse -Force (Join-Path $uiPath 'dist/*') $bffWwwroot

Write-Host "Web UI built and copied to $bffWwwroot"
