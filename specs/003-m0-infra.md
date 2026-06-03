# 003 — M0 基建：PlayMode 测试脚手架 + 截图工具

- 状态：已确认（RPG-MVP-PLAN M0）
- 目标：为后续每个游戏功能提供"行为可测 + 可视审核证据"的底座。

## 范围
- PlayMode 测试程序集 `Game.Tests.PlayMode` + 一个冒烟测试（验证脚手架可用）。
- 截图工具菜单 `YIUIMCP/Capture Screenshot`：把 Game 视图截图写到 `Logs/screenshot.png`，供开发者审核。
- 不做：完整 PlayMode-via-CLI 自动跑（DoD 门禁继续用 编译 + EditMode；游戏核心逻辑按架构抽纯类，EditMode 即可覆盖大部分）。

## 验收（可自动化）
- [ ] 编译通过 `compile-unity-flow.ps1` → Success
- [ ] EditMode 套件仍全绿（PlayMode 脚手架不影响 EditMode 门禁）
- [ ] `YIUIMCP/Capture Screenshot` 菜单存在且可经 ExecuteMenu 调用

## 备注
- PlayMode 测试用于"行为/集成"验证，按需在编辑器手动跑；纯逻辑仍走 EditMode 自动门禁。
- 截图在 Play 模式下最有效；编辑期截 Game 视图作尽力而为。
