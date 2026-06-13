#requires -version 5.1
<#
.SYNOPSIS
  Figma -> UGUI 同步：给 node-id，一条命令拉取+导出资源+生成 UISpec 草稿+落地版式报告/合成图。
  外部活儿（HTTP/代理/导出/降采样/圆角/编码）全在这里做完；spec 微调与 build/render 交给 ui-build-render.ps1。

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File .\Packages\cn.etetet.yiuimcp\Config\figma-pull.ps1 -Node 20:387 -Panel Login

.NOTES
  token 解析顺序：-Token 参数 > 环境变量 FIGMA_TOKEN > 项目根 .figma-token 文件（已 gitignore）。
  token 需 file_content:read 作用域。详见 scripts/figma_sync.py 顶部注释。
#>
param(
  [Parameter(Mandatory=$true)][string]$Node,
  [string]$Panel = "Login",
  [string]$Token = "",
  [string]$FileKey = "",
  [int]$MaxBg = 1280,
  [int]$IconScale = 3
)
$ErrorActionPreference = "Stop"
# 项目根 = Config 上溯三级
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$Py   = Join-Path $Root "scripts\figma_sync.py"
if (-not (Test-Path $Py)) { Write-Error "missing $Py"; exit 1 }

if (-not $Token) { $Token = $env:FIGMA_TOKEN }
if (-not $Token) {
  $tf = Join-Path $Root ".figma-token"
  if (Test-Path $tf) { $Token = (Get-Content $tf -Raw).Trim() }
}
if (-not $Token) { Write-Error "no Figma token (-Token / FIGMA_TOKEN / .figma-token)"; exit 2 }

$python = (Get-Command python -ErrorAction SilentlyContinue).Source
if (-not $python) { $python = (Get-Command py -ErrorAction SilentlyContinue).Source }
if (-not $python) { Write-Error "python not found on PATH"; exit 3 }

# 不要用 $args（自动变量），改名 $pyArgs 避免 splat 调用时被污染
$pyArgs = @($Py, $Node, $Panel, "--token", $Token, "--maxbg", $MaxBg, "--iconscale", $IconScale)
if ($FileKey) { $pyArgs += @("--file", $FileKey) }
Push-Location $Root
try { & $python @pyArgs } finally { Pop-Location }
