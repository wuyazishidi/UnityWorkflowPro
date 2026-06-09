# 002 — 从效果图(纯位图)拼像素级 UGUI

- 状态：进行中（M1 纯核心 + M2 导入器 + M3 原子工具 = 已实现并验证；M0 CJK 字体 / M4 真实面板闭环 待办）
- 负责人：Jinwanpeng
- 关联：完整方案见 `.claude/plans/ui-from-mockup.md`；产物在 `Assets/Scripts/UI/`、`Assets/Editor/UI/`、`Assets/UI/`

## 1. 目标（Why）

给一张效果图(扁平 PNG/JPG，无图层) + 一组图标，由 AI 看图估算 → 写 JSON Spec → 一键生成可复用 UGUI 预制体，再用确定性渲染截图与效果图做像素级 diff、迭代收敛。

## 2. 范围（Scope）

- 包含：声明式 JSON Spec；纯函数核心(坐标数学/颜色/对齐/校验/建树)；编辑器导入器(Spec→prefab)；图标导入后处理；共享根 UIRoot.prefab；两个 MCP 原子工具 `BuildUIFromSpec`、`RenderCanvasToPng`。
- 不包含(v1)：多分辨率自适应、DI 封装、热更下载、SpriteAtlas、Shadow/Outline/渐变等特效（列 v1.1）。

## 3. 设计与接口（What）

- 坐标：效果图像素、左上原点；参考分辨率 1920×1080。左上锚定 `anchorMin=anchorMax=(0,1)`、`pivot=(0,1)`、`sizeDelta=(w,h)`、`anchoredPosition=(x-px, -(y-py))`；`stretch-full` 拉伸填满。
- 纯核心（`Game` 程序集，`Assets/Scripts/UI/`，可 EditMode 测）：`UISpec`(DTO)、`UISpecMath`、`ColorUtil`、`AlignmentMap`、`UISpecValidator`、`UISpecJson`、`UIHierarchyBuilder`(经 `IUIAssetResolver` 解耦资源)。
- 编辑器（`Assets/Editor/UI/`，编进 Assembly-CSharp-Editor，无需 asmdef）：`UIBuilder`(资源加载/9-slice border 同步/`SaveAsPrefabAsset` 存无 Canvas 面板 prefab)、`UIIconPostprocessor`、`UIRootBuilder`、两个 `[YIUIMCPTools]` 工具。
- 交付物：`Assets/UI/Prefabs/<Panel>.prefab`(每面板，可复用) + 共享 `UIRoot.prefab`。

### 原子工具
- `BuildUIFromSpec(specPath, outputPrefabPath)` → 生成/覆盖面板 prefab；失败返回校验错误列表。
- `RenderCanvasToPng(prefabPath, outputPngPath, width=1920, height=1080, backgroundColor?)` → 专用正交相机 + RenderTexture 渲染到精确分辨率 PNG。

## 4. 约束（Constraints）

- 纯逻辑必须在 `Game`（测试程序集只引用 `Game`）。`Game` 与测试 asmdef 已加 `UnityEngine.UI`、`Unity.TextMeshPro`。
- 不改 `Packages/cn.etetet.yiuimcp/**`。
- 渲染前对 TMP 调 `ForceMeshUpdate()`（Dynamic SDF 按需栅格化）。

## 5. 验收标准（Acceptance）

- [x] 编译通过；EditMode 测试全绿（`UISpecTests`，23 passed）。
- [x] `BuildUIFromSpec` + `RenderCanvasToPng` 经 MCP 头less 跑通，渲染图与 Spec 坐标一致（已用冒烟面板核验）。
- [ ] M0：放入 CJK 字体并生成 Dynamic SDF TMP 资源、设为默认（中文渲染）。
- [ ] M4：拿真实效果图迭代到像素级吻合，产出一个可复用面板 prefab。

## 6. 怎么玩（运行期）

把 `<Panel>.prefab` 实例化到 `UIRoot.prefab`(或任意 1920×1080 参考分辨率 Canvas) 下即原样呈现。中文需先完成 M0 字体。

## 7. 备注 / 决策记录

- MCP 包无 asmdef、类型在 Assembly-CSharp-Editor → 新工具放 `Assets/Editor/UI/` 零 asmdef 接线即可见 MCP 基类与 `Game` 纯逻辑。
- 面板 prefab 不内嵌 Canvas（可复用）；渲染工具临时自建 Canvas+相机，不污染 prefab。
