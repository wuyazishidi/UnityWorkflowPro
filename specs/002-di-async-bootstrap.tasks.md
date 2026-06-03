# 002 — DI 与异步初始化骨架 · 任务清单（Tasks）

## 任务
- [x] T1: manifest.json 加 VContainer/UniTask（git URL） — 验证：`Packages/manifest.json` 含两行
- [x] T2: 经 YIUIMCP `Resolve Packages` 触发 UPM 解析 — 验证：`Library/PackageCache` 出现 vcontainer/unitask
- [x] T3: `Game.asmdef` 增加 references `VContainer`/`UniTask` — 编译通过
- [x] T4: 写 `IGreetingService`/`GreetingService`（纯逻辑） — 编译 + 单测通过
- [x] T5: 写 `GameLifetimeScope` + `GameBootstrap`(IStartable + UniTask) — 编译通过
- [x] T6: 写 `GreetingServiceTests`（正常/空白/去空白） — result=PASS
- [x] T7: 跑 DoD 门禁 `scripts/dod.ps1` — DONE

## 完成定义（DoD）—— 全部达成
- [x] `compile-unity-flow.ps1` → Success
- [x] EditMode 测试 result=PASS passed=8 failed=0
- [x] 控制台无新增 error
- [x] spec 002 验收清单全部勾选
