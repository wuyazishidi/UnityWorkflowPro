# 001 — 角色平面移动（PlayerController）

- 状态：已确认
- 负责人：示例（用于跑通工作流）
- 关联：首个示例 spec，验证「规约→实现→编译→验证」闭环

## 1. 目标（Why）

提供一个最小、可独立编译验证的角色移动组件，作为工作流的样板功能。

## 2. 范围（Scope）

- 包含：一个挂在角色 GameObject 上的 `PlayerController` MonoBehaviour，读取水平/垂直输入，按可配置速度在 XZ 平面移动。
- **不包含**：跳跃、物理碰撞、动画、相机跟随、网络。这些另开 spec。

## 3. 设计与接口（What）

运行时脚本，目录 `Assets/Scripts/Player/PlayerController.cs`，命名空间 `Game.Player`。

```csharp
namespace Game.Player
{
    public class PlayerController : UnityEngine.MonoBehaviour
    {
        [UnityEngine.SerializeField] private float _moveSpeed = 5f; // 单位/秒，>=0
        // Update(): 读取 Input.GetAxis("Horizontal"/"Vertical")，
        //           在世界坐标 XZ 平面按 _moveSpeed * Time.deltaTime 平移。
        // 移动向量长度做归一化上限，避免斜向加速。
    }
}
```

- 输入：Unity 传统输入轴 `Horizontal` / `Vertical`（工程默认存在）。
- 依赖：仅 UnityEngine，无第三方、无 Odin。
- 约束：`_moveSpeed` 经 `[SerializeField]` 暴露在 Inspector，私有字段。

## 4. 约束（Constraints）

- 性能：`Update` 内零 GC 分配（不 new、不闭包）。
- 命名 / 目录：遵循 `CLAUDE.md` 第 3 节（PascalCase 类型、`_camelCase` 私有字段、运行时代码在 `Assets/Scripts/`）。
- 禁止：触碰 `ProjectSettings/`、`Packages/`。

## 5. 验收标准（Acceptance — 必须可验证）

- [x] 编译通过：`compile-unity-flow.ps1 -Force 0 -NoWait 1` → `Compilation Complete / Result: Success, No errors!`（3.94s）
- [x] 控制台无报错：上述 `GetCompileResult` 返回 `No errors!`；`PlayerController` 已编入 `Assembly-CSharp.dll`
- [x] 脚本可作为组件挂载（无编译期错误，类型存在于 `Game` 程序集）
- [x] 自动化测试：`Assets/Tests/EditMode/PlayerControllerTests.cs` 5 个用例覆盖移动数学，`[YIUIMCP-TESTS] result=PASS passed=5 failed=0`

> 验证经过：起初 MCP 命令一直 5 分钟超时报“Unity 未就绪”，排查根因为本机系统代理拦截了 UTO 到本地 Unity 的 axios 心跳；在 `UTO/src` 加 `axios.defaults.proxy=false` 并重编译后，原生闸门 3.94s 成功。后续整合 Unity Test Framework，移动逻辑抽为纯函数 `ComputeDisplacement` 并加 5 个 EditMode 测试，经 `scripts/dod.ps1` 一键门禁验证：编译 Success + 测试 PASS = DONE。

## 6. 备注 / 决策记录

- 选用传统 Input Manager 而非新版 Input System，避免引入额外包依赖，保证开箱即编译。
- 移动放在 `Update` + `Time.deltaTime`，本示例不接物理（无 Rigidbody），后续若加碰撞再改 `FixedUpdate` + `Rigidbody.MovePosition`，届时更新本 spec。
