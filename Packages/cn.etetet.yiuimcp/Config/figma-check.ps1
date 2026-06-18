#requires -version 5.1
<#
.SYNOPSIS
  One-shot Figma sync-status check across all panels (data-driven, via lastModified).
  Lists which panels are up-to-date / stale-ready / cache-windowed / node-gone.
  With -Sync, auto re-syncs only the panels that are stale AND whose /nodes cache has refreshed.

.DESCRIPTION
  Core logic in scripts/figma_check.py (uses curl, inherits system proxy for Figma).
  -Sync consumes the JSON "ready" list and calls figma.ps1 -NoVerify for each.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\figma-check.ps1
  powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\figma-check.ps1 -Sync

.NOTES
  ASCII-only comments (PS5.1 would mis-read a non-BOM file with Chinese). Figma over system proxy;
  local Unity (build step under -Sync) bypassed via NO_PROXY.
#>
param(
  [switch]$Sync
)
$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
# Local-proxy bypass for the build step (figma.ps1); Figma itself still uses system proxy.
$env:NO_PROXY = "127.0.0.1,localhost"; $env:no_proxy = "127.0.0.1,localhost"

Push-Location $Root
try {
  Write-Host "==== Figma sync-status check ===="
  & python "scripts/figma_check.py"

  if ($Sync) {
    $raw = & python "scripts/figma_check.py" --json
    $list = @($raw | ConvertFrom-Json)
    if (-not $list -or $list.Count -eq 0) {
      Write-Host "`n[sync] nothing to sync (no stale-ready panel)."
      return
    }
    Write-Host "`n==== auto-sync $($list.Count) stale-ready panel(s) ===="
    foreach ($it in $list) {
      Write-Host "`n-- syncing $($it.panel) ($($it.node)) --"
      & "$PSScriptRoot\figma.ps1" -Node $it.node -Panel $it.panel -FileKey $it.fileKey -NoVerify
    }
    Write-Host "`n==== auto-sync done; re-checking ===="
    & python "scripts/figma_check.py"
  }
} finally { Pop-Location }
