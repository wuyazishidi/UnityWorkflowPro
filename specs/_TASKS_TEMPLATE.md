# NNN — <功能名> · 任务清单（Tasks）

> 由 `/spec-tasks` 基于 plan 生成。每个任务**小到可独立实现并验证**，像给 AI 的 TDD 步骤。
> 勾选规则：编译过 + 相关测试过 才可勾。

## 任务
- [ ] T1: <最小可验证步骤> — 验证：<编译 / 某测试用例 / 断言>
- [ ] T2: ...
- [ ] T3: 为本功能补 EditMode 测试用例 `Assets/Tests/EditMode/*Tests.cs`
- [ ] T4: 运行 DoD 门禁（`/dod` 或 `scripts/dod.ps1`）全绿

## 完成定义（Definition of Done，对照 constitution 第三条）
- [ ] `compile-unity-flow.ps1` → Success
- [ ] EditMode 测试 result=PASS（failed=0）
- [ ] 控制台无新增 error
- [ ] 对应 spec 验收清单全部勾选
