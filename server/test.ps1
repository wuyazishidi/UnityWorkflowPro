# Backend gate: build web project + run zero-dependency test runner. Exit 0 = PASS.
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
Write-Host "[server] dotnet build RpgServer (web) ..."
dotnet build (Join-Path $root "RpgServer\RpgServer.csproj") -c Release --nologo
if ($LASTEXITCODE -ne 0) { Write-Host "[server] BUILD FAILED"; exit 1 }
Write-Host "[server] running tests ..."
dotnet run --project (Join-Path $root "RpgServer.Tests") -c Release --nologo
exit $LASTEXITCODE
