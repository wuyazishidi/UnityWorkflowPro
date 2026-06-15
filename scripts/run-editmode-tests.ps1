# 经 YIUIMCP 触发 EditMode 测试并读取结果（Unity 须打开本工程）。
# 退出码：0=全部通过；1=有失败或未取得结果。
param([int]$WaitSeconds = 8, [int]$MaxAttempts = 3)
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root
$cfg = Join-Path $root "Packages\cn.etetet.yiuimcp\Config"
$resultFile = Join-Path $root "Logs\EditMode-test-results.txt"

# ===== 清代理：本地 Unity MCP 走回环 socket，系统代理(Clash 等)会拦致 RPC 卡死 =====
$env:HTTP_PROXY = ""; $env:HTTPS_PROXY = ""; $env:ALL_PROXY = ""
$env:http_proxy = ""; $env:https_proxy = ""; $env:all_proxy = ""
$env:NO_PROXY = "127.0.0.1,localhost,::1"; $env:no_proxy = "127.0.0.1,localhost,::1"

# 清掉旧结果，避免读到上一次的
if (Test-Path $resultFile) { Remove-Item $resultFile -Force }

$json = '{"menuPath":"YIUIMCP/Run EditMode Tests"}'
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))

# 触发 + 轮询，未取得结果(RPC 卡死/Unity 忙)时自动重连重试
$line = $null
for ($attempt = 1; $attempt -le $MaxAttempts -and -not $line; $attempt++) {
    if ($attempt -gt 1) {
        Write-Host "[run-editmode-tests] 未取得结果，等待后自动重试 ($attempt/$MaxAttempts) ..." -ForegroundColor Yellow
        Start-Sleep -Seconds 4
    }
    Write-Host "[run-editmode-tests] 触发 YIUIMCP/Run EditMode Tests ..."
    & powershell -ExecutionPolicy Bypass -Command "& '$cfg\invoke-uto-tool.ps1' -Tool 'ExecuteMenu' -ParamsBase64 $b64 -NoWait 1" | Out-Null

    $deadline = (Get-Date).AddSeconds($WaitSeconds + 20)
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        if (Test-Path $resultFile) { $line = (Get-Content $resultFile -Raw).Trim(); if ($line) { break } }
    }
}

if (-not $line) {
    Write-Host "[run-editmode-tests] $MaxAttempts 次仍未取得测试结果（Unity 是否打开/编译完成？MCP 卡死可点一下编辑器重置）。" -ForegroundColor Red
    exit 1
}

Write-Host "[run-editmode-tests] $line"
if ($line -match "failed=(\d+)") {
    $failed = [int]$Matches[1]
    if ($failed -eq 0 -and $line -match "result=PASS") {
        Write-Host "[run-editmode-tests] PASS" -ForegroundColor Green
        exit 0
    }
}
Write-Host "[run-editmode-tests] FAIL" -ForegroundColor Red
exit 1
