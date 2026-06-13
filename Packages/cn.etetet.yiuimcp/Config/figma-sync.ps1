#requires -version 5.1
<#
.SYNOPSIS
  一条命令同步 Figma 设计到 UGUI：拉取+导资源+生成草稿(figma-pull) → Refresh+构建+渲染(ui-build-render)。
  给个 node-id 即可；Unity 须打开。

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\figma-sync.ps1 -Node 20:387 -Panel Login

.NOTES
  产物：Assets/UI/<Panel>/<Panel>.prefab（由自动草稿构建）+ _render.png（核对图）。
  渲染分辨率自动取草稿的 referenceWidth/Height。复杂设计若草稿需微调：先只跑 figma-pull，
  改 <Panel>.draft.json 另存为 <Panel>.json，再单独跑 ui-build-render。
#>
param(
  [Parameter(Mandatory=$true)][string]$Node,
  [string]$Panel = "Login",
  [string]$Token = "",
  [string]$FileKey = "",
  [string]$Bg = "#03060E"
)
$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$pull = Join-Path $PSScriptRoot "figma-pull.ps1"
$bren = Join-Path $PSScriptRoot "ui-build-render.ps1"

# 1) 拉取 + 导资源 + 生成草稿（Unity 无需开）
# 用哈希表 splat（传命名参数）；数组 splat 会按位置错绑导致 -Token 变空。
$pullParams = @{ Node = $Node; Panel = $Panel }
if ($Token)   { $pullParams.Token = $Token }
if ($FileKey) { $pullParams.FileKey = $FileKey }
& $pull @pullParams
if ($LASTEXITCODE -ne 0) { Write-Error "figma-pull failed"; exit 1 }

# 2) 渲染分辨率取草稿的参考尺寸（草稿含中文，须按 UTF8 读，否则 PS5.1 默认 GB2312 误读会让 ConvertFrom-Json 崩）
$draft = Join-Path $Root "Assets/UI/$Panel/$Panel.draft.json"
$txt   = Get-Content $draft -Raw -Encoding UTF8
$w = [int]([regex]::Match($txt, '"referenceWidth":\s*(\d+)').Groups[1].Value)
$h = [int]([regex]::Match($txt, '"referenceHeight":\s*(\d+)').Groups[1].Value)
if ($w -le 0 -or $h -le 0) { Write-Error "cannot read referenceWidth/Height from $draft"; exit 1 }

# 3) Refresh + 构建 + 渲染（Unity 须开）
& $bren -Spec "Assets/UI/$Panel/$Panel.draft.json" `
        -Prefab "Assets/UI/$Panel/$Panel.prefab" `
        -Png "Assets/UI/$Panel/_render.png" -Width $w -Height $h -Bg $Bg
if ($LASTEXITCODE -ne 0) { Write-Error "ui-build-render failed"; exit 1 }

Write-Host "=== synced: Assets/UI/$Panel/$Panel.prefab ($w x $h). 核对 _render.png vs .figma/truth.png ==="
