# 007 — M4：玩家生死 + 重生

- 状态：已确认（RPG-MVP-PLAN M4）

## 目标
玩家 HP 归零时死亡（禁用控制、变灰），延迟后在出生点满血重生。

## 范围
- `PlayerLife`：监听 Health.OnDied → 禁用 PlayerMovement2D/Attacker、变灰、UniTask 延迟后回出生点满血重生。
- GameRoot 给玩家挂 PlayerLife。
- Health 组件 EditMode 测试（Configure/TakeDamage/OnDied/Heal）。
- 不做：死亡动画、存档惩罚、生命数限制。

## 验收（可自动化）
- [ ] 编译 Success
- [ ] EditMode：Health 满血配置、扣血钳0、OnDied 只触发一次、Heal 钳满且死后不回血
- [ ] DoD DONE
- [ ] （人工）按 Play：被敌人打死后玩家变灰、约2秒后回中心满血复活
