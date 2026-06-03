# 经 YIUIMCP 触发 EditMode 测试并读取结果（Unity 须打开本工程）。
# 退出码：0=全部通过；1=有失败或未取得结果。
param([int]$WaitSeconds = 8)
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root
$cfg = Join-Path $root "Packages\cn.etetet.yiuimcp\Config"
$resultFile = Join-Path $root "Logs\EditMode-test-results.txt"

# 清掉旧结果，避免读到上一次的
if (Test-Path $resultFile) { Remove-Item $resultFile -Force }

# 1) 触发测试
$json = '{"menuPath":"YIUIMCP/Run EditMode Tests"}'
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
Write-Host "[run-editmode-tests] 触发 YIUIMCP/Run EditMode Tests ..."
& powershell -ExecutionPolicy Bypass -Command "& '$cfg\invoke-uto-tool.ps1' -Tool 'ExecuteMenu' -ParamsBase64 $b64 -NoWait 1" | Out-Null

# 2) 轮询结果文件
$deadline = (Get-Date).AddSeconds($WaitSeconds + 20)
$line = $null
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    if (Test-Path $resultFile) { $line = (Get-Content $resultFile -Raw).Trim(); if ($line) { break } }
}

if (-not $line) {
    Write-Host "[run-editmode-tests] 未取得测试结果（Unity 是否打开/编译完成？）" -ForegroundColor Red
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
