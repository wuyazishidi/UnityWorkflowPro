# 004 — M1：2D 玩家移动 + 相机跟随

- 状态：已确认（RPG-MVP-PLAN M1）

## 目标
2D 俯视角玩家可用方向键/WASD 在 XY 平面移动，相机平滑跟随。开发者按 Play 即可见（运行时自动搭场景，无需手动布线）。

## 范围
- `PlayerMovement2D`：读输入→纯函数算位移→移动 transform。
- `CameraFollow2D`：相机平滑跟随目标。
- `GameRoot`：`[RuntimeInitializeOnLoadMethod]` 运行时生成玩家（占位方块精灵）+ 配置正交相机 + 挂跟随。
- 不做：碰撞/边界（M2）、动画、CC0 美术（先占位）。

## 接口
```csharp
// 纯函数（可 EditMode 单测）
PlayerMovement2D.ComputeFrameMove(Vector2 input, float speed, float dt) -> Vector2  // 归一化上限1 * speed*dt
CameraFollow2D.ComputeFollowPosition(Vector3 target, float zDepth) -> Vector3       // (target.x,target.y,zDepth)
```

## 验收（可自动化）
- [ ] 编译 Success
- [ ] EditMode：移动归一化/缩放、零输入不动、相机定位 z 锁定
- [ ] DoD DONE
- [ ] （人工）按 Play：绿色方块随 WASD 移动、相机跟随
