# NNN — <功能名称>

- 状态：草稿 / 已确认 / 已实现 / 已废弃
- 负责人：
- 关联：<相关 spec / issue / PR>

## 1. 目标（Why）

<一句话说明这个功能要解决什么问题、为谁解决。>

## 2. 范围（Scope）

- 包含：
- **不包含**（明确划界，防止 AI 扩张）：

## 3. 设计与接口（What）

> 给出 AI 实现时必须遵守的精确契约：类型、方法签名、数据结构、关键流程。

```csharp
// 关键类型 / 接口 / 方法签名
```

- 数据结构 / 字段：
- 状态机 / 流程：
- 依赖（现有系统、第三方、Odin 等）：

## 4. 约束（Constraints）

- 性能：
- 平台 / 输入：
- 命名 / 目录：遵循 `CLAUDE.md` 第 3 节
- 禁止事项：

## 5. 验收标准（Acceptance — 必须可验证）

- [ ] 编译通过：`compile-unity-flow.ps1 -Force 0 -NoWait 1` → Success
- [ ] 控制台无报错：`get_console_error.ps1 -NoWait 1`
- [ ] <可观察的行为，例如：进入 Play 后角色可移动；某菜单命令执行成功>
- [ ] <如有自动化断言：`invoke-uto-tool.ps1 -Tool 'AssertConsoleContains' ...`>

## 6. 备注 / 决策记录

<实现过程中做出的关键取舍，便于后续会话沿用。>
