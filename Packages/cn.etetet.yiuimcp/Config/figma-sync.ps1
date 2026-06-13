#requires -version 5.1
<#
.SYNOPSIS
  一条命令同步 Figma 设计到 UGUI：拉取+导资源+生成 spec(figma-pull) → Refresh+打图集+构建(ui-build-render)。
  给个 node-id 即可；Unity 须打开。常态只构建不渲染；要核对图加 -Verify $true。

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\figma-sync.ps1 -Node 20:387 -Panel Login
  # 带核对：-Verify $true（出 _render.png 并和 .figma/truth.png 算 MAE）

.NOTES
  产物：Assets/UI/<Panel>/<Panel>.json（Figma 忠实投影，覆盖式重生成，git diff 审阅）+ <Panel>.prefab。
  spec 是单一真相，不再有 .draft.json。复杂设计可手改 <Panel>.json，下次重生成用 git diff 看改动。
#>
param(
  [Parameter(Mandatory=$true)][string]$Node,
  [string]$Panel = "Login",
  [string]$Token = "",
  [string]$FileKey = "",
  [string]$Bg = "#03060E",
  [switch]$Verify
)
$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$pull = Join-Path $PSScriptRoot "figma-pull.ps1"
$bren = Join-Path $PSScriptRoot "ui-build-render.ps1"

# 1) 拉取 + 导资源 + 生成 spec（Unity 无需开）
# 用哈希表 splat（传命名参数）；数组 splat 会按位置错绑导致 -Token 变空。
$pullParams = @{ Node = $Node; Panel = $Panel }
if ($Token)   { $pullParams.Token = $Token }
if ($FileKey) { $pullParams.FileKey = $FileKey }
& $pull @pullParams
if ($LASTEXITCODE -ne 0) { Write-Error "figma-pull failed"; exit 1 }

# 2) 渲染分辨率取 spec 的参考尺寸（含中文，须按 UTF8 读，否则 PS5.1 默认 GB2312 误读会让解析崩）
$spec = Join-Path $Root "Assets/UI/$Panel/$Panel.json"
$txt  = Get-Content $spec -Raw -Encoding UTF8
$w = [int]([regex]::Match($txt, '"referenceWidth":\s*(\d+)').Groups[1].Value)
$h = [int]([regex]::Match($txt, '"referenceHeight":\s*(\d+)').Groups[1].Value)
if ($w -le 0 -or $h -le 0) { Write-Error "cannot read referenceWidth/Height from $spec"; exit 1 }

# 3) Refresh + 打图集 + 构建（Unity 须开）；常态不渲染，-Verify 才出核对图
& $bren -Spec "Assets/UI/$Panel/$Panel.json" `
        -Prefab "Assets/UI/$Panel/$Panel.prefab" `
        -Width $w -Height $h -Bg $Bg -Verify:$Verify
if ($LASTEXITCODE -ne 0) { Write-Error "ui-build-render failed"; exit 1 }

$msg = "=== synced: Assets/UI/$Panel/$Panel.prefab ($w x $h) ==="
if ($Verify) { $msg += " 核对 _render.png vs .figma/truth.png" }
Write-Host $msg
