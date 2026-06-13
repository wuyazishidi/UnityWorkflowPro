# 002 — UISpec 声明式契约与构建引擎（Spec→Prefab）

- 状态：已实现并在用（构建引擎稳定，作为 Figma 主路的后端；字段已扩展见 004）
- 负责人：Jinwanpeng
- 关联：上游入口见 `specs/004-figma-first-lossless-spec.md`（Figma→UISpec，唯一入口）；产物在 `Assets/Scripts/UI/`、`Assets/Editor/UI/`、`Assets/UI/`

## 1. 目标（Why）

定义"一个面板 = 一份声明式 UISpec(JSON)"的契约，以及把 UISpec **确定性**建成可复用 UGUI 预制体的纯逻辑 + 编辑器工具。坐标数学/颜色/对齐/校验/建树全为纯函数、可 EditMode 测、headless 可跑。

UISpec **由上游生成**（Figma → UISpec，见 004）；本 spec 只管 **"UISpec → Prefab"** 这一后端。它是整条 UI 管线稳定不变的核心。

## 2. 范围（Scope）

- 包含：声明式 JSON Spec（schema 见 `UISpec.cs`，v2 字段见 004）；纯函数核心(坐标数学/颜色/对齐/校验/建树)；编辑器导入器(Spec→prefab)；图标导入后处理；共享根 UIRoot.prefab；MCP 原子工具 `BuildUIFromSpec`、`RenderCanvasToPng`、`PackPanelAtlas`、`GenRoundedSprite`。
- 不包含：上游"如何得到 UISpec"（那是 004 的 Figma 翻译）；多分辨率响应式自适应（004 Phase 4）；SDF shape 渲染（004 Phase 3）。

## 3. 设计与接口（What）

- 坐标：UISpec 像素、左上原点；参考分辨率取面板（卡片）尺寸。左上锚定 `anchorMin=anchorMax=(0,1)`、`pivot=(0,1)`、`sizeDelta=(w,h)`、`anchoredPosition=(x-px, -(y-py))`；`stretch-full` 拉伸填满，`center` 固定尺寸居中。
- 纯核心（`Game` 程序集，`Assets/Scripts/UI/`，可 EditMode 测）：`UISpec`(DTO)、`UISpecMath`、`ColorUtil`、`AlignmentMap`、`UISpecValidator`、`UISpecJson`、`UIHierarchyBuilder`(经 `IUIAssetResolver` 解耦资源)、`UIVertexGradient`(顶点色渐变，v2)。
- 编辑器（`Assets/Editor/UI/`，编进 Assembly-CSharp-Editor，无需 asmdef）：`UIBuilder`(资源加载/9-slice border 同步/`SaveAsPrefabAsset` 存无 Canvas 面板 prefab)、`UIIconPostprocessor`(导入分流，见 004)、`UIRootBuilder`、`[YIUIMCPTools]` 工具。
- 交付物：`Assets/UI/<Panel>/<Panel>.prefab`(每面板，可复用) + 共享 `UIRoot.prefab`。

### 原子工具
- `BuildUIFromSpec(specPath, outputPrefabPath)` → 生成/覆盖面板 prefab；失败返回校验错误列表。
- `RenderCanvasToPng(prefabPath, outputPngPath, width, height, backgroundColor?)` → 专用正交相机 + RenderTexture 渲染到精确分辨率 PNG（用于可选 QA 核对，见 004 `-Verify`）。
- `PackPanelAtlas`、`GenRoundedSprite`：见 CLAUDE.md §4。

## 4. 约束（Constraints）

- 纯逻辑必须在 `Game`（测试程序集只引用 `Game`）。`Game` 与测试 asmdef 已加 `UnityEngine.UI`、`Unity.TextMeshPro`。
- 不改 `Packages/cn.etetet.yiuimcp/**`。
- 渲染前对 TMP 调 `ForceMeshUpdate()`（Dynamic SDF 按需栅格化）。
- 向后兼容 `schemaVersion: 1`（旧 spec 仍能 build）。

## 5. 验收标准（Acceptance）

- [x] 编译通过；EditMode 测试全绿（`UISpecTests`）。
- [x] `BuildUIFromSpec` + `RenderCanvasToPng` 经 MCP 头less 跑通，渲染图与 Spec 坐标一致。
- [x] CJK 字体：MiSans Dynamic SDF 设为默认，中文正常渲染。
- [x] Figma 主路（004）产出的 spec 能一键 build，`-Verify` 渲染与 Figma 真值区域 MAE < 阈值。

## 6. 怎么玩（运行期）

把 `<Panel>.prefab` 实例化到 `UIRoot.prefab`(或任意参考分辨率 Canvas) 下即原样呈现。

## 7. 备注 / 决策记录

- MCP 包无 asmdef、类型在 Assembly-CSharp-Editor → 新工具放 `Assets/Editor/UI/` 零 asmdef 接线即可见 MCP 基类与 `Game` 纯逻辑。
- 面板 prefab 不内嵌 Canvas（可复用）；渲染工具临时自建 Canvas+相机，不污染 prefab。
- **历史**：本管线最初为"扁平 PNG 效果图 → 看图估算 UISpec → 像素 diff 迭代收敛"而建（已废弃）。现以 Figma 为唯一入口（004），UISpec 由 Figma 精确数据生成，不再人工看图估算；本 spec 收敛为纯构建引擎。
