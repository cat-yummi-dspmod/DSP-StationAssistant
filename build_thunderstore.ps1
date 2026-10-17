# Thunderstore package: StationAssistant.dll + manifest + README + CHANGELOG + icon

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot
$ModName = 'StationAssistant'
$Dll = Join-Path $Root "bin\Release\net472\$ModName.dll"
$Zip = Join-Path $Root "bin\Release\$ModName.zip"

if (-not (Test-Path $Dll)) {
    Write-Host "Building $ModName..." -ForegroundColor Cyan
    dotnet build (Join-Path $Root "$ModName.csproj") -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Build successful!" -ForegroundColor Green
}

if (-not (Test-Path $Dll)) {
    Write-Host "DLL not found: $Dll" -ForegroundColor Red
    exit 1
}

$Temp = Join-Path $Root 'thunderstore_package'
if (Test-Path $Temp) { Remove-Item $Temp -Recurse -Force }
New-Item -ItemType Directory -Path $Temp | Out-Null

Copy-Item (Join-Path $Root 'manifest.json') $Temp
Copy-Item (Join-Path $Root 'README.md') $Temp
Copy-Item (Join-Path $Root 'CHANGELOG.md') $Temp
Copy-Item (Join-Path $Root 'icon.png') $Temp
Copy-Item $Dll $Temp

New-Item -ItemType Directory -Force -Path (Split-Path $Zip) | Out-Null
if (Test-Path $Zip) { Remove-Item $Zip -Force }
Compress-Archive -Path (Join-Path $Temp '*') -DestinationPath $Zip -Force
Remove-Item $Temp -Recurse -Force
Write-Host "Package created: $Zip" -ForegroundColor Green
