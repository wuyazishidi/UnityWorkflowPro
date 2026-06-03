# 005 — M2：可探索地图 + 碰撞/边界

- 状态：已确认（RPG-MVP-PLAN M2）

## 目标
运行时生成一张矩形地图（地板 + 四周墙），玩家被墙体阻挡，可在内部自由探索。开发者按 Play 即见。

## 范围
- `MapBuilder`：纯函数 `GenerateBorderWalls`/`CellToWorld` + 运行时实例化地板与带 `BoxCollider2D` 的墙。
- 玩家改用 `Rigidbody2D`（gravityScale 0、冻结旋转）+ `BoxCollider2D`，`PlayerMovement2D` 经 `MovePosition` 移动 → 物理阻挡。
- `GameRoot` 进 Play 时构建 20×14 地图，玩家居中。
- 不做：多房间、地形多样性、Tilemap 资源（先程序化占位）。

## 验收（可自动化）
- [ ] 编译 Success
- [ ] EditMode：边界格数=周长公式、中心开放、CellToWorld 居中
- [ ] DoD DONE
- [ ] （人工）按 Play：玩家被四周墙挡住，地图内可走动
