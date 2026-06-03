# Stop hook：硬闸门。若本轮改过 Assets 下 .cs（存在 .dod-needed 标记），结束前强制跑 DoD。
#   - 全绿 -> 清标记，放行结束
#   - 红（编译/测试失败）-> 输出 decision=block，阻断结束，把失败回灌给 Agent 继续修
#   - Unity 不可用（未打开/超时）-> 优雅放行（不死锁），保留标记并提示
# 注意：Claude Code 的 hooks 在会话启动时加载，本文件改动需新会话才生效。
$ErrorActionPreference = 'SilentlyContinue'
[Console]::In.ReadToEnd() | Out-Null   # 读掉 stdin 的 Stop 事件 JSON

$root = $env:CLAUDE_PROJECT_DIR
if (-not $root) { $root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path }
$marker = Join-Path $root '.claude\.dod-needed'

# 没有脏标记 = 本轮没改代码 -> 直接放行
if (-not (Test-Path $marker)) { exit 0 }

# 跑 DoD
$dod = Join-Path $root 'scripts\dod.ps1'
$out = & powershell -NoProfile -ExecutionPolicy Bypass -File $dod 2>&1 | Out-String

$green = ($out -match 'Success, No errors!') -and ($out -match 'result=PASS') -and ($out -match 'failed=0')
$unityDown = ($out -match 'Unity 未就绪') -or ($out -notmatch 'GetCompileResult')

if ($green) {
    Remove-Item $marker -Force
    exit 0   # 放行
}

if ($unityDown) {
    # 无法运行门禁（Unity 未打开等）：不死锁，放行但提示；保留标记下次再验
    [Console]::Error.WriteLine('[DoD 闸门] 改动了 .cs 但无法运行门禁（Unity 未打开?）。已放行本次结束，DoD 尚未验证，请在 Unity 打开后运行 /dod。')
    exit 0
}

# 红：阻断结束，把失败节选回灌给 Agent
$tail = ($out -split "`n" | Select-Object -Last 30) -join "`n"
$reason = "DoD 门禁未通过（constitution 第三条：编译/测试不过不得结束）。请修复后再结束。门禁输出（末尾节选）：`n$tail"
(@{ decision = 'block'; reason = $reason } | ConvertTo-Json -Compress)
exit 0
