# Definition of Done 门禁：编译闸门 + EditMode 测试（Unity 须打开本工程）。
# 退出码：0=全绿(DONE)；1=未完成(NOT DONE)。对照 specs/constitution.md 第三条。
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $root
$cfg = Join-Path $root "Packages\cn.etetet.yiuimcp\Config"

# ===== 清代理：本地 Unity MCP 走回环 socket，系统代理(Clash 等)会拦致 RPC 卡死 =====
# 设在此处会随 & powershell 子进程(编译/测试)继承，子脚本内也各自再清一次兜底。
$env:HTTP_PROXY = ""; $env:HTTPS_PROXY = ""; $env:ALL_PROXY = ""
$env:http_proxy = ""; $env:https_proxy = ""; $env:all_proxy = ""
$env:NO_PROXY = "127.0.0.1,localhost,::1"; $env:no_proxy = "127.0.0.1,localhost,::1"

Write-Host "==================== DoD 门禁 ===================="

# 1) 编译闸门
Write-Host "[1/2] 编译闸门 ..."
$compile = & powershell -ExecutionPolicy Bypass -Command "& '$cfg\compile-unity-flow.ps1' -Force 0 -NoWait 1" | Out-String
Write-Host $compile
$compileOk = ($compile -match "Success, No errors!")
if (-not $compileOk) {
    Write-Host "结果: NOT DONE（编译未通过）" -ForegroundColor Red
    exit 1
}

# 2) EditMode 测试
Write-Host "[2/2] EditMode 测试 ..."
& powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "run-editmode-tests.ps1")
$testsOk = ($LASTEXITCODE -eq 0)

Write-Host "================================================="
if ($compileOk -and $testsOk) {
    Write-Host "结果: DONE  (编译 Success + 测试 PASS)" -ForegroundColor Green
    # 全绿则清除脏标记，告知 Stop 闸门本轮已达 DoD
    Remove-Item (Join-Path $root ".claude\.dod-needed") -Force -ErrorAction SilentlyContinue
    exit 0
} else {
    Write-Host "结果: NOT DONE（测试未全通过）" -ForegroundColor Red
    exit 1
}
