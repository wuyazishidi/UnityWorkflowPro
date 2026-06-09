# 004 — UI Toolkit 工业 UI 小样（结论：不适配本 headless 工作流）

- 状态：已结论（spike 完成）
- 分支：`feature/industrial-uitoolkit`
- 关联：`Assets/UI/UITK/`、`Assets/Scripts/UI/UITKShot.cs`、`Assets/Editor/UI/SetupUITKShot.cs`

## 1. 目的
评估 UI Toolkit(UXML/USS) 是否更适合工业 UI（集中主题、圆角、flex 布局、中文），以你给的 SaaS 登录页为目标试做。

## 2. 做了什么
- **UXML**：`Assets/UI/UITK/LoginPanel.uxml`（登录页结构：卡片/Logo/标题/输入框/复选/按钮）。
- **USS**：`Assets/UI/UITK/IndustrialTheme.uss` —— **设计令牌**(`--bg/--card/--ink/--primary…`) + 组件类(`.card/.input/.btn-primary…`)，含 **border-radius 圆角**、flex 布局。这正是 UITK 对"和谐"的强项:集中改 token = 全局一致。
- **渲染脚手架**：`PanelSettings` + `UIDocument` + `UITKShot`(出图) + `SetupUITKShot`(一键搭场景)。

## 3. 关键结论：本 headless 工作流渲染不了 UITK
- 验证到位:UXML 树已加载、USS 已挂上(`childCount=1, styleSheets=1`)。
- **但运行时面板根本没跑布局**:`resolvedStyle` 全为 `NaN`、`cardBg` 透明 —— 内容不渲染。
- 两条路都失败:
  - **targetTexture 离屏**:后台编辑器里离屏 UITK 面板不刷新 → NaN。
  - **屏幕 + ScreenCapture**:MCP 驱动的编辑器在**后台无活动 game view**,UITK 拿不到屏幕尺寸驱动布局 → 同样 NaN/空白。
- 对比:**UGUI 的 `RenderCanvasToPng`(自带相机 + RenderTexture)首次就成**,不需要屏幕。

## 4. 决断
**本工作流的核心价值 = headless、AI 驱动、截图回环验证。UGUI 天然适配(显式相机+RT 出图),UITK 在后台编辑器里无法被驱动渲染/验证 → 直接打断核心闭环。**
- UITK 的主题/圆角/flex 优势是真的,但"自动出图验证"这条命脉它在当前环境走不通。
- 因此:**工业 UI 仍用 UGUI**(已能很好还原登录页),把唯一短板**圆角**用"生成的圆角矩形 9-slice 精灵"补上即可,成本远低于解决 UITK 的 headless 渲染。
- 仅当将来改为**前台/Play 模式或独立 Player 渲染**的出图链路时,再回头评估 UITK。

## 5. 备注
- UITK 设计本身没问题:在 Unity 里**打开 `LoginPanel.uxml`(UI Builder)即可实时预览**——只是没法接进我们的自动出图回环。
- 本分支保留 UITK 产物作为记录;主线工业 UI 走 UGUI + 圆角精灵。
