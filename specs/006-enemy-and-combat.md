# 006 — M3：敌人 + 近战战斗 + 伤害结算

- 状态：已确认（RPG-MVP-PLAN M3）

## 目标
场上有一个会追玩家的敌人；玩家可近战攻击击杀敌人；敌人接触玩家造成伤害。战斗数学全部可单测。

## 范围
- `CombatMath`（纯）：ApplyDamage(钳0)、IsDead、InRange(平方距离)、CanAttack(冷却)。
- `Health`：通用生命值，伤害走 CombatMath，死亡触发 OnDied。
- `Attacker`（玩家）：Fire1/空格，对半径内敌人造成伤害，受冷却。
- `EnemyChase`：追玩家；接触按冷却造成伤害。
- GameRoot 生成红色敌人(hp50)、玩家加 Health(hp100)+Attacker；敌人死亡销毁。
- 不做：玩家死亡/重生（M4）、多敌人波次、击退/特效。

## 验收（可自动化）
- [ ] 编译 Success
- [ ] EditMode：ApplyDamage 钳0/负伤害、IsDead、InRange 边界、CanAttack 冷却
- [ ] DoD DONE
- [ ] （人工）按 Play：红敌追来，空格攻击数次后敌人消失；被敌人贴身时玩家掉血
