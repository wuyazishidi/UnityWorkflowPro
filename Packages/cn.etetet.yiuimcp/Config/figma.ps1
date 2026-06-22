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
  [switch]$NoVerify,
  [switch]$Deliver
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

  # -Deliver: after sync, publish (binding + .unitypackage) and copy into the consumer project (YC-Ego).
  # One command does Figma -> prefab -> binding -> YC-Ego. Skipped if no -Deliver or YC-Ego not found.
  if ($Deliver) {
    Write-Host "[deliver] publish + sync to YC-Ego ..."
    & "$PSScriptRoot\publish-ui.ps1" -Panel $Panel

    # Expected list-item prefab name from the spec (used to VERIFY the copy actually landed).
    $itemName = $null
    $specPath = Join-Path $Root "Assets/UI/$Panel/$Panel.json"
    if (Test-Path $specPath) {
      $spec = Get-Content $specPath -Raw
      if ($spec -match '"isItemTemplate"\s*:\s*true') {
        $mi = [regex]::Match($spec, '"itemPrefab"\s*:\s*"([^"]+)"')
        if ($mi.Success) { $itemName = $mi.Groups[1].Value }
      }
    }

    $ycego  = Join-Path $Root "..\YC-Ego"
    $syncUi = Join-Path $ycego "tools\sync-ui.ps1"
    if (Test-Path $syncUi) {
      Push-Location $ycego
      try { & $syncUi -Src $Root -Panels $Panel } finally { Pop-Location }

      # Item-prefab guarantee: sync-ui discovers item prefabs via Get-ChildItem, whose directory
      # enumeration can hit the Unity import delete/rewrite window right after a build and silently
      # miss the item (root cause of dropped TaskItem_Btn / Item_ImageFill in YC-Ego). So copy it
      # DETERMINISTICALLY here by its KNOWN name (from the spec) with Copy-Item, retrying until the
      # source is readable and the destination exists. This does not depend on directory listing.
      if ($itemName) {
        $srcItem = Join-Path $Root "Assets/UI/$Panel/$itemName.prefab"
        $dstDir  = Join-Path $ycego "Assets/Resources/UI/$Panel"
        $dstItem = Join-Path $dstDir "$itemName.prefab"
        $t = 0
        while (-not (Test-Path $dstItem) -and $t -lt 12) {
          if (Test-Path $srcItem) {
            Copy-Item $srcItem $dstDir -Force -ErrorAction SilentlyContinue
            if (Test-Path "$srcItem.meta") { Copy-Item "$srcItem.meta" $dstDir -Force -ErrorAction SilentlyContinue }
          }
          if (-not (Test-Path $dstItem)) { Start-Sleep -Seconds 2; $t++ }
        }
        if (Test-Path $dstItem) { Write-Host "[deliver] item prefab synced: $itemName.prefab (+$($t*2)s)" }
        else { Write-Host "[deliver] WARN item prefab still missing: $itemName.prefab (run sync-ui manually)" }
      }
      Write-Host "[deliver] done: $Panel -> YC-Ego/Assets/Resources/UI/$Panel"
    } else {
      Write-Host "[deliver] SKIP sync-ui (not found: $syncUi)"
    }
  }
} finally { Pop-Location }
