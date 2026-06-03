# 008 — M5：拾取 + 背包

- 状态：已确认（RPG-MVP-PLAN M5）

## 目标
地图上有可拾取物品，玩家触碰即入背包；背包是可单测的纯逻辑容器。

## 范围
- `Inventory`（纯 C#）：按 id 堆叠、容量(槽位)限制、TryAdd/Remove/CountOf/Total。
- `InventoryHolder`（玩家组件）：持有背包 + 变化事件。
- `Pickup`：触发器物品，玩家进入即加入背包并销毁。
- GameRoot：玩家挂 InventoryHolder；生成 4 个黄色金币拾取物。
- 不做：背包 UI（M8）、物品使用/装备、掉落表。

## 验收（可自动化）
- [ ] 编译 Success
- [ ] EditMode：堆叠、容量拒新允堆、移除释放槽、非法输入拒绝
- [ ] DoD DONE
- [ ] （人工）按 Play：走过黄色方块即消失，控制台显示拾取与背包总数
