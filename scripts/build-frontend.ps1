#!/usr/bin/env pwsh
# scripts\build-frontend.ps1
# Workaround for Vite/Rollup failing when project path contains '#' (e.g. D:\c#\)
# Usage: .\scripts\build-frontend.ps1  (run from repo root)

$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot | Split-Path -Parent
$frontendSrc  = Join-Path $projectRoot "frontend"
$tempBuild    = Join-Path $env:TEMP "mousou_frontend_build"
$distDest     = Join-Path $frontendSrc "dist"

Write-Host "📦  Copying frontend source to clean temp path..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $tempBuild -Force | Out-Null
robocopy $frontendSrc $tempBuild /E /XD node_modules dist .git /NFL /NJH /NJS /nc /ns | Out-Null

Write-Host "📦  Installing npm dependencies..." -ForegroundColor Cyan
Push-Location $tempBuild
try {
    npm.cmd install --prefer-offline 2>&1 | Select-Object -Last 5
    Write-Host "🔨  Building with Vite..." -ForegroundColor Cyan
    npx.cmd vite build
    if ($LASTEXITCODE -ne 0) { throw "Vite build failed" }
} finally {
    Pop-Location
}

Write-Host "📁  Copying dist back to project..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $distDest -Force | Out-Null
robocopy "$tempBuild\dist" $distDest /E /NFL /NJH /NJS | Out-Null

Write-Host "✅  Frontend build complete! Output: $distDest" -ForegroundColor Green
Write-Host "   $(Get-ChildItem $distDest -Recurse -File | Measure-Object -Property Length -Sum | Select-Object -ExpandProperty Sum | ForEach-Object { '{0:N0} bytes total' -f $_ })"
