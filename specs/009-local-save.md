# 009 — M6：本地存档读档

- 状态：已确认（RPG-MVP-PLAN M6）

## 目标
F5 存档、F9 读档，保存/恢复玩家位置、HP、背包。

## 范围
- `GameState`/`ItemStack`：可序列化存档数据。
- `SaveSystem`：Serialize/Deserialize（纯）+ Save/Load 文件读写。
- `SaveService`（玩家组件）：F5 收集→存 `persistentDataPath/save.json`；F9 读→应用。
- Health 加 `SetCurrent`、Inventory 加 `Clear`（读档恢复）。
- 不做：多存档槽、云存档（M7）、自动存档。

## 验收（可自动化）
- [ ] 编译 Success
- [ ] EditMode：标量往返、物品列表往返、空串/Null 反序列化为 null
- [ ] DoD DONE
- [ ] （人工）按 Play：移动+捡币后 F5，再移动/受伤后 F9 → 位置/HP/背包恢复
