# PostToolUse hook：当 AI 编辑了 Assets 下的 C# 脚本，提醒它必须走编译闸门。
# 非阻塞：把提醒作为 additionalContext 回灌给 Claude，不打断流程。
# 输入：stdin 收到 Claude Code 的 PostToolUse 事件 JSON。
$ErrorActionPreference = 'SilentlyContinue'

$raw = [Console]::In.ReadToEnd()
if (-not $raw) { exit 0 }

try { $evt = $raw | ConvertFrom-Json } catch { exit 0 }

$path = $evt.tool_input.file_path
if (-not $path) { exit 0 }

# 只对 Assets 下的 .cs 触发；忽略包内/生成代码。
$isCs = $path -match '\.cs$'
$inAssets = $path -match '[\\/]Assets[\\/]'
if (-not ($isCs -and $inAssets)) { exit 0 }

# 置"脏标记"：本会话改过 Assets 下 .cs，结束时 Stop 闸门据此强制跑 DoD。
try { New-Item -ItemType File -Path (Join-Path $PSScriptRoot '..\.dod-needed') -Force | Out-Null } catch {}

$msg = @'
[工作流闸门] 你刚修改了 Assets 下的 C# 脚本。完成本轮前必须：
1) 运行编译：powershell -ExecutionPolicy Bypass -Command "& '.\Packages\cn.etetet.yiuimcp\Config\compile-unity-flow.ps1' -Force 0 -NoWait 1"
2) 检查报错：powershell -ExecutionPolicy Bypass -Command "& '.\Packages\cn.etetet.yiuimcp\Config\get_console_error.ps1' -NoWait 1"
编译未通过不得声称完成；若 Unity 未打开/.port 缺失，请明确标注“待编译验证”为未完成。
'@

$out = @{
  hookSpecificOutput = @{
    hookEventName    = 'PostToolUse'
    additionalContext = $msg
  }
}
$out | ConvertTo-Json -Compress -Depth 5
exit 0
