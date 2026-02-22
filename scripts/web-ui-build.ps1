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

$distPath = Join-Path $uiPath 'dist'
$buildPath = Join-Path $uiPath 'build'
if (Test-Path $distPath) {
    Copy-Item -Recurse -Force (Join-Path $distPath '*') $bffWwwroot
}
elseif (Test-Path $buildPath) {
    Copy-Item -Recurse -Force (Join-Path $buildPath '*') $bffWwwroot
}
else {
    throw 'No dist/ or build/ output found after UI build.'
}

Write-Host "Web UI built and copied to $bffWwwroot"
