param(
    [bool]$Force = $False,
    [bool]$NoWait = $True,
    [int]$MaxAttempts = 3
)

$ErrorActionPreference = "Stop"

# ===== 清代理：本脚本只连本地 Unity(回环 socket)，绝不该走代理 =====
# 本机系统代理(Clash 等)会拦/篡改到 127.0.0.1 的连接 → RPC "响应超时" 或 Editor.log
# 报 "message header is corrupted"。NO_PROXY 对 node 原始 socket 不一定生效，故直接清空。
$env:HTTP_PROXY = ""; $env:HTTPS_PROXY = ""; $env:ALL_PROXY = ""
$env:http_proxy = ""; $env:https_proxy = ""; $env:all_proxy = ""
$env:NO_PROXY = "127.0.0.1,localhost,::1"; $env:no_proxy = "127.0.0.1,localhost,::1"

$UTO_PATH = Join-Path $PSScriptRoot "..\UTO"

# 从 .port 文件读取 Unity MCP 端口
$UNITY_MCP_PORT = 3212
$portFile = Join-Path $UTO_PATH ".port"
if (Test-Path $portFile) {
    $UNITY_MCP_PORT = [int](Get-Content $portFile -Raw).Trim()
}

# UTO HTTP 端口 = Unity 端口 + 1
$UTO_HTTP_PORT = $UNITY_MCP_PORT + 1

Write-Host "========================================"
Write-Host "Unity 智能编译 (Force: $Force)"
Write-Host "========================================"
Write-Host "Unity MCP 端口: $UNITY_MCP_PORT"
Write-Host "UTO HTTP 端口: $UTO_HTTP_PORT"
Write-Host ""

# 清掉占用 UTO HTTP 端口的旧进程（残留 UTO 会与新连接抢端口）
function Stop-StaleUto {
    try {
        $conns = Get-NetTCPConnection -LocalPort $UTO_HTTP_PORT -ErrorAction SilentlyContinue
        $killed = $false
        foreach ($c in $conns) {
            Stop-Process -Id $c.OwningProcess -Force -ErrorAction SilentlyContinue
            $killed = $true
        }
        if ($killed) { Start-Sleep -Seconds 1 }
    } catch {}
}

# 跑一次完整编译流程：起 UTO → batch(StopPlayMode/TriggerCompile/GetCompileResult) → 关 UTO。
# 返回 @{ Ok=[bool]; Text=[string] }；Ok 以输出含 "Success, No errors!" 为准（RPC 卡死时不含）。
function Invoke-CompileOnce {
    Stop-StaleUto

    Write-Host "启动 UTO HTTP Server..."
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "node"
    $psi.Arguments = "build/index.js --http"
    $psi.WorkingDirectory = $UTO_PATH
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $utoProcess = [System.Diagnostics.Process]::Start($psi)

    # 等待 UTO 就绪
    $ready = $false
    for ($i = 0; $i -lt 20; $i++) {
        try {
            $health = Invoke-RestMethod -Uri "http://localhost:$UTO_HTTP_PORT/health" -TimeoutSec 2
            if ($health -and $health.status -eq "ok") {
                $ready = $true
                Write-Host "UTO 已就绪" -ForegroundColor Green
                if ($health.heartbeatReady) { Write-Host "心跳检测已启动" -ForegroundColor Green }
                break
            }
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }

    if (-not $ready) {
        Write-Host "UTO 启动超时" -ForegroundColor Red
        if ($utoProcess -and -not $utoProcess.HasExited) { try { $utoProcess.Kill() } catch {} }
        return @{ Ok = $false; Text = "UTO 启动超时" }
    }

    Write-Host ""

    $tools = @(
        @{ name = "StopPlayMode"; arguments = @{} },
        @{ name = "TriggerCompile"; arguments = @{ Force = $Force } },
        @{ name = "GetCompileResult"; arguments = @{} }
    )
    $body = @{ tools = $tools } | ConvertTo-Json -Depth 10 -Compress

    Write-Host "执行编译流程..."
    Write-Host "  1. 退出 PlayMode（如果在运行）"
    Write-Host "  2. 触发编译"
    Write-Host "  3. 获取编译结果"
    Write-Host ""

    $sb = New-Object System.Text.StringBuilder
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:$UTO_HTTP_PORT/batch" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 600
    } catch {
        $msg = "调用失败: $($_.Exception.Message)"
        Write-Host $msg -ForegroundColor Red
        if ($utoProcess -and -not $utoProcess.HasExited) {
            try { $utoProcess.Kill(); [void]$utoProcess.WaitForExit(3000) } catch {}
        }
        return @{ Ok = $false; Text = $msg }
    }

    Write-Host "========================================"
    [void]$sb.AppendLine("========================================")
    if ($response.success) {
        Write-Host "编译流程完成!" -ForegroundColor Green
        Write-Host "总耗时: $($response.totalDurationSeconds) 秒" -ForegroundColor Cyan
        Write-Host ""
        [void]$sb.AppendLine("编译流程完成!")
        foreach ($result in $response.results) {
            $stepDurationText = ""
            if ($null -ne $result.duration -and "$($result.duration)" -ne "") {
                $stepDurationText = " ($($result.duration) ms)"
            }
            Write-Host "✓ $($result.tool)$stepDurationText" -ForegroundColor Green
            [void]$sb.AppendLine("OK $($result.tool)$stepDurationText")
            if ($result.result) {
                Write-Host "  $($result.result)" -ForegroundColor Gray
                [void]$sb.AppendLine("  $($result.result)")
            }
        }
    } else {
        Write-Host "编译流程失败!" -ForegroundColor Red
        Write-Host "总耗时: $($response.totalDurationSeconds) 秒" -ForegroundColor Cyan
        Write-Host "失败位置: 第 $($response.failedAt + 1) 个工具"
        Write-Host "错误: $($response.error)"
        Write-Host ""
        [void]$sb.AppendLine("编译流程失败! 错误: $($response.error)")
        foreach ($result in $response.results) {
            $stepDurationText = ""
            if ($null -ne $result.duration -and "$($result.duration)" -ne "") {
                $stepDurationText = " ($($result.duration) ms)"
            }
            if ($result.success) {
                Write-Host "✓ $($result.tool)$stepDurationText" -ForegroundColor Green
                [void]$sb.AppendLine("OK $($result.tool)$stepDurationText")
            } else {
                Write-Host "✗ $($result.tool)$stepDurationText" -ForegroundColor Red
                [void]$sb.AppendLine("FAIL $($result.tool)$stepDurationText")
                if ($result.error) {
                    Write-Host "  错误: $($result.error)" -ForegroundColor Red
                    [void]$sb.AppendLine("  错误: $($result.error)")
                }
            }
            if ($result.result) {
                Write-Host "  $($result.result)" -ForegroundColor Gray
                [void]$sb.AppendLine("  $($result.result)")
            }
        }
    }
    Write-Host "========================================"

    # 收尾：关 UTO 进程
    if ($utoProcess -and -not $utoProcess.HasExited) {
        Write-Host "关闭 UTO 进程..." -ForegroundColor Yellow
        try { $utoProcess.Kill(); [void]$utoProcess.WaitForExit(3000) } catch {}
    }

    $text = $sb.ToString()
    return @{ Ok = ($text -match "Success, No errors!"); Text = $text }
}

# ===== 重试循环：RPC 卡死/超时(常见于域重载后或代理拦截)时，清理重连再试 =====
# 每次重试前等几秒让 Unity 主线程 settle（域重载/大图导入会短暂占住主线程致 RPC 超时）。
$final = @{ Ok = $false; Text = "" }
for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
    if ($attempt -gt 1) {
        Write-Host ""
        Write-Host "RPC 未成功，等待 Unity 就绪后自动重连重试 ($attempt/$MaxAttempts) ..." -ForegroundColor Yellow
        Start-Sleep -Seconds 4
    }
    $final = Invoke-CompileOnce
    if ($final.Ok) { break }
}

if (-not $final.Ok) {
    Write-Host ""
    Write-Host "编译闸门：$MaxAttempts 次尝试仍未拿到编译结果。" -ForegroundColor Red
    Write-Host "（Unity 是否打开/挂死？若 MCP 卡死，点一下编辑器窗口触发重编译可重置 MCP server。）" -ForegroundColor Yellow
    if (-not $NoWait) {
        Write-Host "按任意键退出..."
        [Console]::ReadKey($true) | Out-Null
        Stop-Process -Id $PID
    }
    exit 1
}

if (-not $NoWait) {
    Write-Host "按任意键退出..."
    [Console]::ReadKey($true) | Out-Null
    Stop-Process -Id $PID
}
exit 0
