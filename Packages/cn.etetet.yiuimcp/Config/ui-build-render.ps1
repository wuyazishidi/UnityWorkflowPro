#requires -version 5.1
<#
.SYNOPSIS
  健壮地 BuildUIFromSpec(+可选 RenderCanvasToPng)：每步重试到出现 SUCCESS 为止。
  专治大纹理导入/冷渲染导致的「连接被服务器关闭」——导入/首帧未就绪时必失败，就绪后立刻成功。

.EXAMPLE
  # 只构建
  powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\ui-build-render.ps1 `
    -Spec Assets/UI/Login/Login.json -Prefab Assets/UI/Login/Login.prefab
  # 构建并渲染核对（渲染产物自行删除即可）
  powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\ui-build-render.ps1 `
    -Spec Assets/UI/Login/Login.json -Prefab Assets/UI/Login/Login.prefab `
    -Png Assets/UI/Login/_render.png -Width 1106 -Height 778 -Bg "#03060E"

.NOTES
  Unity 须打开且健康。底层调 invoke-uto-tool.ps1。
#>
param(
  [Parameter(Mandatory=$true)][string]$Spec,
  [Parameter(Mandatory=$true)][string]$Prefab,
  [string]$Png = "",
  [int]$Width = 1106,
  [int]$Height = 778,
  [string]$Bg = "#03060E",
  [int]$Retries = 50,
  [int]$RefreshTimes = 3,
  [bool]$Atlas = $true,
  [switch]$Verify
)
# 不能用 Stop：invoke-uto-tool 内部健康检查重试会产生错误记录，Stop 会干扰 *>&1 捕获导致永远匹配不到 SUCCESS
$ErrorActionPreference = "Continue"
$Invoke = Join-Path $PSScriptRoot "invoke-uto-tool.ps1"

# 成功判定用「产物文件是否被刷新」这个确定性信号，而非解析 stdout——
# invoke-uto-tool 的 "SUCCESS:" 走 Write-Host 信息流，在函数+循环+& 复用上下文里捕获不稳定。
function Invoke-Retry([string]$Tool, [hashtable]$P, [string]$Label, [string]$OutFile) {
  $json = ($P | ConvertTo-Json -Compress)
  $b64  = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
  $before = if (Test-Path $OutFile) { (Get-Item $OutFile).LastWriteTimeUtc } else { [datetime]::MinValue }
  for ($i = 1; $i -le $Retries; $i++) {
    & $Invoke -Tool $Tool -ParamsBase64 $b64 -NoWait $true *>$null
    Start-Sleep -Milliseconds 150
    if ((Test-Path $OutFile) -and ((Get-Item $OutFile).LastWriteTimeUtc -gt $before)) {
      Write-Host "[$Label] OK after $i"; return $true
    }
  }
  Write-Host "[$Label] FAILED after $Retries tries (no fresh $OutFile)"; return $false
}

# 先刷新资源库，确保 figma-pull 新写入的 PNG 被导入为 Sprite（小图导入极快）。
# Refresh 无产物文件可做新鲜度判定，故只 fire 几次 + 间隔，靠后续 build 重试兜底。
$rb64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes('{"menuPath":"Assets/Refresh"}'))
for ($i = 1; $i -le $RefreshTimes; $i++) {
  & $Invoke -Tool "ExecuteMenu" -ParamsBase64 $rb64 -NoWait $true *>$null
  Start-Sleep -Milliseconds 600
}
Write-Host "[refresh] fired $RefreshTimes x"

# 给该面板 Icons 目录打一张独立 SpriteAtlas（V1 Always Enabled 会在进入 Play/构建时自动合批 → 降 DC）。
# 路径从 -Prefab 推导：同目录的 Icons/ 与 <PrefabName>.spriteatlas。
if ($Atlas) {
  $panelDir  = (Split-Path $Prefab -Parent) -replace '\\', '/'
  $panelName = [IO.Path]::GetFileNameWithoutExtension($Prefab)
  $icons     = "$panelDir/Icons"
  $atlasPath = "$panelDir/$panelName.spriteatlas"
  if (Test-Path $icons) {
    if (-not (Invoke-Retry "PackPanelAtlas" @{ spriteFolder = $icons; outputAtlasPath = $atlasPath } "atlas" $atlasPath)) {
      Write-Host "[atlas] WARN: 打图集失败（不阻断构建）"
    }
  } else {
    Write-Host "[atlas] 跳过：无 $icons"
  }
}

if (-not (Invoke-Retry "BuildUIFromSpec" @{ specPath = $Spec; outputPrefabPath = $Prefab } "build" $Prefab)) { exit 1 }

# 渲染降级（spec 004 Phase 1）：常态只构建、不渲染（摘掉最不稳的冷渲染环节）。
# 仅 -Verify $true 时出核对图 _render.png，并与 .figma/truth.png 算分区域 MAE。
if ($Verify) {
  if (-not $Png) { $Png = ((Split-Path $Prefab -Parent) -replace '\\', '/') + "/_render.png" }
  if (-not (Invoke-Retry "RenderCanvasToPng" @{ prefabPath = $Prefab; outputPngPath = $Png; width = $Width; height = $Height; backgroundColor = $Bg } "render" $Png)) { exit 1 }
  $truth = ((Split-Path $Prefab -Parent) -replace '\\', '/') + "/.figma/truth.png"
  if (Test-Path $truth) {
    $py = (Get-Command python -ErrorAction SilentlyContinue).Source
    if (-not $py) { $py = (Get-Command py -ErrorAction SilentlyContinue).Source }
    $diff = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\scripts\ui_diff.py")).Path
    if ($py -and (Test-Path $diff)) { & $py $diff $Png $truth }
  }
}
Write-Host "ALL DONE"
