#requires -version 5.1
<#
.SYNOPSIS
  Shared low-level entry for Figma->UGUI sync. Both the `figma-sync` Skill and the `/figma`
  slash command call THIS script. Handles: URL parsing, local-proxy bypass, node discovery,
  full sync (figma-sync.ps1, which auto-snapshots), and recovery-index regen.

.EXAMPLE
  # By URL (parses fileKey + node):
  powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\figma.ps1 -Url "https://www.figma.com/design/KEY/Name?node-id=20-388" -Panel LoginPanel
  # By node + panel (default file key from figma_sync.py):
  ...figma.ps1 -Node 20:388 -Panel LoginPanel
  # Discover frames when the node is unknown/stale:
  ...figma.ps1 -Discover -Panel UpLoad           # lists frames whose name contains "UpLoad"
  ...figma.ps1 -Discover -FileKey KEY            # lists all top frames of a file

.NOTES
  ASCII-only comments on purpose: a .ps1 with Chinese must be UTF-8 BOM under PS5.1; this file
  avoids that trap. Requires Unity open for the build step. Token via .figma-token / FIGMA_TOKEN.
#>
param(
  [string]$Url = "",
  [string]$Node = "",
  [string]$Panel = "",
  [string]$FileKey = "",
  [switch]$Discover,
  [switch]$NoVerify
)
$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path

# Local-proxy bypass: Clash intercepts 127.0.0.1 -> 502 / "UTO timeout". Figma (external) still uses proxy.
$env:NO_PROXY = "127.0.0.1,localhost"; $env:no_proxy = "127.0.0.1,localhost"

# Parse a Figma URL -> fileKey + node (only fill what wasn't passed explicitly).
if ($Url) {
  $mk = [regex]::Match($Url, "/(?:file|design)/([A-Za-z0-9]+)")
  if ($mk.Success -and -not $FileKey) { $FileKey = $mk.Groups[1].Value }
  $mn = [regex]::Match($Url, "node-id=([0-9]+[-:][0-9]+)")
  if ($mn.Success -and -not $Node) { $Node = $mn.Groups[1].Value }
}
if ($Node) { $Node = $Node -replace "-", ":" }

Push-Location $Root
try {
  $py = "python"

  # Discovery mode (or no node yet): list frames so the caller can pick a node-id.
  if ($Discover -or -not $Node) {
    Write-Host "[discover] listing frames (FileKey='$FileKey' default-if-empty, filter='$Panel')"
    & $py "scripts/figma_frames.py" $FileKey --kw $Panel
    Write-Host ""
    Write-Host "[discover] pick an id above, then: figma.ps1 -Node <id> -Panel $Panel"
    return
  }

  if (-not $Panel) { Write-Error "missing -Panel"; exit 2 }

  # Full sync (figma-sync.ps1 -> figma-pull + ui-build-render; figma_sync.py auto-writes figma/ snapshot).
  $sync = Join-Path $PSScriptRoot "figma-sync.ps1"
  $p = @{ Node = $Node; Panel = $Panel; Verify = (-not $NoVerify) }
  if ($FileKey) { $p.FileKey = $FileKey }
  & $sync @p
  if ($LASTEXITCODE -ne 0) { Write-Error "figma-sync failed"; exit 1 }

  # Regenerate the human recovery index from figma/*.meta.json.
  & $py "scripts/figma_index.py"
  Write-Host "[figma.ps1] done: synced $Panel ($Node) + index updated"
} finally { Pop-Location }
