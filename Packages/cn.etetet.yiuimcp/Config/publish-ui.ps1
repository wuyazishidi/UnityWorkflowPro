<#
.SYNOPSIS
  Publish one UI panel as a portable delivery package for the consumer project (YC-Ego).

.DESCRIPTION
  Pipeline (all via the YIUIMCP UTO bridge; Unity must be open):
    1. (optional, -Build) Rebuild prefab from <Panel>.json spec       -> BuildUIFromSpec
    2. Walk the built prefab tree -> <Panel>.binding.json + .md        -> ExportBindingDescriptor
    3. Pack prefab + binding + Icons + atlas + deps (.meta) -> .unitypackage -> ExportUiPackage
    4. Write delivery/<Panel>.README.md (contract note)

  Decoupling: the consumer only ever consumes the .unitypackage + binding.json;
  it never references this project's code. The binding.json schema is the only shared contract.

.EXAMPLE
  publish-ui.ps1 -Panel TaskDetailPanel
  publish-ui.ps1 -Panel TaskDetailPanel -Build -Out delivery/TaskDetailPanel.unitypackage
#>
param(
  [Parameter(Mandatory=$true)][string]$Panel,
  [string]$Out = "",
  [switch]$Build
)

$ErrorActionPreference = "Stop"
# Local UTO/Unity over loopback; Clash proxy intercepts 127.0.0.1 -> clear so the bridge isn't blocked.
$env:HTTP_PROXY=''; $env:HTTPS_PROXY=''; $env:ALL_PROXY=''; $env:NO_PROXY='127.0.0.1,localhost'

if (-not $Out) { $Out = "delivery/$Panel.unitypackage" }
$dir = "Assets/UI/$Panel"
$spec = "$dir/$Panel.json"
$prefab = "$dir/$Panel.prefab"

function Get-Stamp([string]$p) {
  if (Test-Path $p) { return (Get-Item $p).LastWriteTimeUtc }
  return [datetime]::MinValue
}

function Invoke-Tool([string]$tool, [hashtable]$params, [string]$expectFile) {
  # invoke-uto-tool.ps1 prints via Write-Host (no output stream) and doesn't set exit code on success,
  # so neither stdout parsing nor $LASTEXITCODE is reliable. Key off the produced artifact instead:
  # success = $expectFile is created/refreshed after the call.
  $json = ($params | ConvertTo-Json -Compress)
  $b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
  $before = Get-Stamp $expectFile
  for ($try = 1; $try -le 3; $try++) {
    # Consecutive UTO spin-ups can collide on the port; clear lingering node + settle first.
    Get-Process node -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    & "$PSScriptRoot\invoke-uto-tool.ps1" -Tool $tool -ParamsBase64 $b64 -NoWait 1 | Out-Null
    Start-Sleep -Seconds 1
    if ((Get-Stamp $expectFile) -gt $before) { return }
    if ($try -lt 3) { Write-Host "  ($tool) no fresh output, retry $try ..." }
  }
  throw "$tool produced no fresh artifact: $expectFile"
}

Write-Host "==== publish-ui: $Panel ===="

if ($Build) {
  if (-not (Test-Path $spec)) { throw "spec not found: $spec (need it to rebuild)" }
  Write-Host "[1/4] rebuild prefab from spec ..."
  Invoke-Tool "BuildUIFromSpec" @{ specPath = $spec; outputPrefabPath = $prefab } $prefab
} else {
  Write-Host "[1/4] skip rebuild (use existing prefab; pass -Build to rebuild from spec)"
}

Write-Host "[2/4] export binding descriptor ..."
Invoke-Tool "ExportBindingDescriptor" @{ prefabPath = $prefab } "$dir/$Panel.binding.json"

Write-Host "[3/4] export portable package ..."
if (Test-Path $Out) { Remove-Item $Out -Force }
Invoke-Tool "ExportUiPackage" @{ panel = $Panel; outputPath = $Out } $Out

Write-Host "[4/4] write delivery README ..."
$outDir = Split-Path $Out -Parent
if ($outDir -and -not (Test-Path $outDir)) { New-Item -ItemType Directory -Force $outDir | Out-Null }
$readme = Join-Path $outDir "$Panel.README.md"
$pkg = [IO.Path]::GetFileName($Out)
$readmeText = @'
# __PANEL__ — UI delivery package (for YC-Ego)

## Import
1. Import `__PKG__` into the YC-Ego project (exported with .meta -> GUID-faithful).
2. First time only: Window > TextMeshPro > Import TMP Essential Resources
   (binding.json sets needsTmpEssentials=true as a reminder).

## How to bind (read only `__PANEL__.binding.json` + the prefab; never reference this project)
Each bindable element has: `key` (stable handle), `path` (transform path inside the prefab, root excluded),
`type` (component kind), `text` (current text, for human cross-check).

Naming convention — every bindable node name carries a TYPE SUFFIX (also present in `path`):
`_Btn` (Button), `_InputField` (InputField), `_Dropdown` (Dropdown), `_Text` (Text).
`key` is that suffixed name camelized (e.g. `Return_Btn` -> `returnBtn`): it ENCODES the type and
never collides across types, so binding by `key` is stable and self-documenting.

Bind = look up by `key` -> locate via `path` -> GetComponent of `type` -> wire your event. e.g.
    root.transform.Find(e.path).GetComponent<Button>().onClick.AddListener(OnConfirm);

Flags to honor:
- `keyAuto:true`        the Figma node name was non-ASCII; `key` is a `<type><index>` fallback (unstable).
                        Rename the node in Figma to an ASCII name and re-publish for a stable `key`.
- `nameTypeMismatch:true` the node is named like an interactive (e.g. `_Btn`) but is NOT that component
                        (the interactive was not generated as its matching component). Do not bind it;
                        report back to the design/translator side to fix bindability.

Human-readable element table: `__PANEL__.binding.md`.

## Contract
Schema v1.0. Full definition: specs/005-ui-binding-contract.md (in the producer project).
The consumer depends only on the binding.json FORMAT — never on this project's code/pipeline.
'@
$readmeText = $readmeText -replace '__PANEL__', $Panel -replace '__PKG__', $pkg
$readmeText | Set-Content -Encoding UTF8 $readme

Write-Host "==== publish-ui done: $Out ===="
Write-Host "  binding: $dir/$Panel.binding.json (+ .md)"
Write-Host "  readme : $readme"
