# UnityWorkflowPro — AI 开发工作流约定

> 本文件是本工程对 AI Agent 的“标准作业指令”。Claude Code 每次会话会自动读取。
> 所有 AI 驱动的开发都必须遵守这里定义的流程与规则，目标是让产出**可预期、可验证、可回滚**。

## 1. 工程概览

- 引擎：Unity 2022.3，C#（Assets 下脚本进入 `Assembly-CSharp` / `Assembly-CSharp-Editor`）
- 已集成 **YIUIMCP**（`Packages/cn.etetet.yiuimcp`）：提供 CLI-first 的 Unity 编排能力（编译、读日志、调菜单、Domain Reload 恢复）
- 可视化窗口依赖 **Odin Inspector**（已装于 `Assets/Plugins/Sirenix`）—— 不要移除
- UTO 编排层（Node）已 `npm install` + `npm run build`；端口写在 `Packages/cn.etetet.yiuimcp/UTO/.port`
- 自动化测试：Unity Test Framework，测试在 `Assets/Tests/EditMode/`（`Game.Tests.EditMode` 程序集），运行时代码在 `Game` 程序集
- **工作流总览见 [docs/WORKFLOW.md](docs/WORKFLOW.md)；不可违背的原则见 [specs/constitution.md](specs/constitution.md)**
- SDD 斜杠命令：`/spec-new`、`/spec-plan`、`/spec-tasks`、`/dod`、`/feature`（定义见 `.claude/commands/`）
- **自治流水线**：给一句需求即可——skill `unity-feature-workflow`（`.claude/skills/`，自动触发）或 `/feature <需求>` 跑完 规约→实现→DoD→审核包
- **硬闸门**：改了 `Assets/*.cs` 后，会话结束时 Stop hook 自动跑 `scripts/dod.ps1`，未通过则阻断结束（`.claude/hooks/dod-stop-gate.ps1`）

## 2. 强制开发流程（每个改动都要走完）

这是一条**线性、不可跳步**的流程。除非用户明确豁免，否则严格执行：

1. **理解 / 写 Spec** — 任何“功能级”改动，先在 `specs/` 下确认或新建 spec（见 `specs/_TEMPLATE.md`）。
   - 小型 bug 修复可跳过 spec，但必须在回复里用一句话说明改动意图与验收标准。
2. **实现** — 按 spec 的接口、命名、约束写代码。一次只解决一个明确问题，保持改动可审查。
3. **编译（硬闸门）** — 改动 C# 后，**必须**运行：
   ```powershell
   powershell -ExecutionPolicy Bypass -Command "& '.\Packages\cn.etetet.yiuimcp\Config\compile-unity-flow.ps1' -Force 0 -NoWait 1"
   ```
   要求 Unity 编辑器处于打开状态。期望返回 `Compilation Complete / Success`。
4. **验证** — 编译后读取结果/报错：
   ```powershell
   powershell -ExecutionPolicy Bypass -Command "& '.\Packages\cn.etetet.yiuimcp\Config\get_console_error.ps1' -NoWait 1"
   ```
   有错 → 回到第 2 步修复；**不允许在编译未通过的情况下声称完成**。
5. **测试（完成的定义）** — 跑 DoD 门禁（编译 + EditMode 测试）：`/dod`，或
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\scripts\dod.ps1
   ```
   涉及可测逻辑必须在 `Assets/Tests/EditMode/` 有覆盖（逻辑尽量抽纯函数）。**测试不过（failed>0）不得声称完成**。
6. **收尾** — 在回复中如实报告：做了什么、编译结果、测试 passed/failed、验证输出。失败就说失败并附输出。

> 若 Unity 未打开 / `.port` 不存在，不要伪造编译成功。应提示用户先打开 Unity，并把“待编译验证”明确标注为未完成。

> **环境关键点（实测，已修复）**：本机设了系统代理（`HTTP_PROXY/HTTPS_PROXY`，如 Clash 7897）。UTO 的 axios 默认会把到本地 Unity `127.0.0.1:3212/health` 的心跳也走代理 → 一直“Unity 未就绪”→ 5 分钟超时。**已在 `UTO/src/index.ts`、`heartbeat-manager.ts` 加 `axios.defaults.proxy = false;` 修复**，编译闸门现约 4 秒成功。若升级/重拷 UTO 包导致该改动丢失，症状会复现——重新加这两行并 `npm run build`。
> 其他前提：① Unity 编辑器已打开且未挂死（健康可直接 `curl http://127.0.0.1:3222/health` 验证返回 200 + serverId）；② **本工程端口已改为 3222**（写在 `UTO/.port`，UTO HTTP = 3223），因为 3212 被另一工程（PicoTest）占用；③ 健康编辑器即使不在前台也能正常处理 MCP 请求（“必须前台”是早期误判，真因是代理）。
> 当 MCP 命令仍异常、又需验证编译时，可用确定性等价手段：检查 `Library/ScriptAssemblies/Assembly-CSharp*.dll` 的重编译时间与目标类型是否在其中，并 grep Editor.log 的 `error CS`。

## 3. 编码与架构规则

> 这些是初始约定，请根据本工程实际情况持续补充（记录 AI 实际犯过的错，而不是一次写全）。

- **命名**：类型 PascalCase，私有字段 `_camelCase`，常量 UPPER_SNAKE。文件名与主类型同名。
- **目录**：运行时代码放 `Assets/Scripts/`，编辑器代码放 `Assets/Editor/` 或带 `Editor` asmdef。
- **不要**：随意改 `ProjectSettings/`、`Packages/manifest.json`、`Packages/cn.etetet.yiuimcp/**`，除非任务明确要求并说明理由。
- **序列化/反射密集**：优先 Odin 特性；避免破坏 Unity 序列化约定。
- **改动最小化**：贴合周边既有代码风格，不做无关重构。

## 4. YIUIMCP 命令速查（从工程根目录执行，Unity 须打开）

| 目的 | 命令 |
|------|------|
| 完整编译流程 | `compile-unity-flow.ps1 -Force 0 -NoWait 1` |
| 强制编译 | `compile-unity-flow.ps1 -Force 1 -NoWait 1` |
| 读控制台日志 | `get_console_log.ps1 -NoWait 1` |
| 读编译结果/报错 | `get_console_error.ps1 -NoWait 1` |
| 调任意工具 | `invoke-uto-tool.ps1 -Tool '<工具名>' -ParamsBase64 <base64> -NoWait 1` |

布尔参数命令行用 `1/0`。`invoke-uto-tool` 的常用工具：`Log` / `LogError` / `TriggerCompile` / `ExecuteMenu` / `GetConsoleLog` / `AssertConsoleContains`（详见 `Packages/cn.etetet.yiuimcp/Config/README.md`）。

调用示例（带参数）：
```powershell
$json = '{"menuPath":"Assets/Refresh"}'
$b64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($json))
powershell -ExecutionPolicy Bypass -Command "& '.\Packages\cn.etetet.yiuimcp\Config\invoke-uto-tool.ps1' -Tool 'ExecuteMenu' -ParamsBase64 $b64 -NoWait 1"
```

### Figma → UGUI 同步（给 node-id 一条命令）

| 目的 | 命令 |
|------|------|
| **一条命令同步**（拉取→导资源→草稿→Refresh→构建→渲染） | `figma-sync.ps1 -Node 20:387 -Panel Login` |
| 仅拉取：导资源 + 生成 UISpec 草稿 + 版式报告 + 合成图（Unity 无需开） | `figma-pull.ps1 -Node 20:387 -Panel Login` |
| 仅构建：自动 Refresh + 打面板图集 + 重试到产物刷新（Unity 须开） | `ui-build-render.ps1 -Spec <spec> -Prefab <prefab> [-Png <png> -Width -Height -Bg]` |

- **常规设计更新**：直接 `figma-sync.ps1 -Node <id>`，产出 `Assets/UI/<Panel>/<Panel>.prefab` + `_render.png`，核对 `_render.png` vs `.figma/truth.png` 即可。
- **草稿需微调时**（复杂改版）：先只跑 `figma-pull.ps1`，改 `<Panel>.draft.json` 另存为 `<Panel>.json`，再 `ui-build-render.ps1 -Spec <Panel>.json ...`。
- figma-pull 产物：`Icons/*.png`(背景自动降采样≤1280+按卡片圆角打 alpha)、`<Panel>.draft.json`(自动翻译的 UISpec：实色→Image、IMAGE→精灵、文字→Text、fill+stroke→外层描边+内层填充边环、Button、整卡背景→圆角精灵)、`.figma/layout.txt`、`.figma/truth.png`。
- **降 DC**：`ui-build-render` 默认会调 `PackPanelAtlas` 给 `Assets/UI/<Panel>/Icons/` 打一张 `<Panel>.spriteatlas`(图集路径从 prefab 路径推导)，工程 Sprite Packer = V1 Always Enabled，进入 Play/构建时自动合批 → 面板内所有 Image 共用一张纹理，加图标不增 DC。`-Atlas $false` 可关。（注意：TMP 文字用自己的字体图集，与精灵是两张纹理，z 序里图文交替仍会打断合批。）
- 前置：项目根 `.figma-token`(已 gitignore，需 `file_content:read` 作用域) 或环境变量 `FIGMA_TOKEN`；默认 file key 在 `scripts/figma_sync.py`，可 `-FileKey` 覆盖。核心逻辑在 `scripts/figma_sync.py`。

## 5. 扩展工作流（让 AI 越用越强）

- 新增 Unity 原子工具：见 `Packages/cn.etetet.yiuimcp/Docs/如何扩展Unity原子工具.md`
- 新增高聚合 CLI flow：在 `Packages/cn.etetet.yiuimcp/Config/` 下加 `*.ps1`，并在本文件第 4 节登记
  - 含中文的 `.ps1` **必须存为 UTF-8 BOM**（PS5.1 否则按 GB2312 解析，多字节字符会被误读成 `}` 等导致语法错误）
  - 外部活儿（HTTP/图像处理）放 `scripts/*.py`，由 ps1 薄封装调用（如 Figma 同步 = `figma-pull.ps1` → `scripts/figma_sync.py`）
- 新增业务流程规范：在 `specs/` 下新建 spec，并在需要时把“必须遵守”的点回填到本文件第 3 节
