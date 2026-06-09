# 计划：从效果图(纯位图) + 图标 → 在 Unity 中拼出像素级一致的 UGUI

## Context（为什么做、要解决什么）

用户要一套 UI 制作工作流：**给一张效果图(扁平 PNG/JPG，无图层数据) + 一组图标，由 AI（我）在 Unity 里拼出与效果图一致的 UGUI**。

已确认前提（用户选定）：
- 效果图是**纯位图**——没有图层/坐标，元素的位置/尺寸/颜色/文字必须由我**看图估算**，再迭代逼近。
- 技术栈 **UGUI**（Canvas + UnityEngine.UI.Image + TextMeshPro）。
- 保真度 **像素级 + 固定参考分辨率**（如 1920×1080），v1 不做多分辨率自适应。
- 效果图**含中文** → 必须先有 CJK 的 TextMeshPro 字体资源，否则中文显示方框。

工程现状（已勘探）：UI 空白——只有 `SampleScene`，零预制体、无 UI 框架/脚本、无图集/字体。栈可用：UGUI+TMP 3.0.6、Odin、VContainer、UniTask、YooAsset 2.3.19。自动化层 `cn.etetet.yiuimcp` 提供原子工具机制（`[YIUIMCPTools]` + `YIUIMCPBaseExecutor<T>`，主线程执行，PowerShell 调用），已验证其类型编译进 `Assembly-CSharp-Editor`。DoD 门禁 = 编译 + EditMode 测试。

**核心洞察**：纯位图没有真值坐标，所以**“构建→确定性渲染截图→我对比效果图→改数值→重建”的闭环是达成“像素级一致”的关键机制**，而非一次成型。工程已具备截图与 CLI 闭环的雏形。

## 推荐方案

**声明式 JSON UI-Spec + 纯函数核心(Game) + 编辑器导入器(Assets/Editor) + 2 个原子工具 + 确定性 RenderTexture 截图 + 视觉反馈闭环。**

**最终交付物 = 可复用的 UI 预制体(.prefab)**；render.png 只是迭代时用于对比的中间产物，不是交付物。

数据流：
```
我看效果图 → 写 panel.json(Spec) ──BuildUIFromSpec──▶ 生成/更新 <Panel>.prefab  ← 交付物
                                   └RenderCanvasToPng─▶ 精确 1920×1080 的 render.png（仅供 diff）
我对比 render.png vs 效果图 → 改 JSON 数值 → 重建 …（每个面板预计 2~4 轮收敛）
最终：<Panel>.prefab 与效果图像素级一致，可被实例化/复用
```
选 JSON-Spec 而非“逐个原子工具实时搭”：可复现、可 diff、是 SDD 的评审产物，解析/坐标数学可抽纯函数进 DoD；而 prefab 是这条流水线的稳定落地产物。

### 预制体结构与产物（一等交付物）
- **一次性共享根 `Assets/UI/Prefabs/UIRoot.prefab`**：`Canvas` + `CanvasScaler`(ScaleWithScreenSize, 1920×1080, match=0.5) + `GraphicRaycaster` + 场景内 `EventSystem`。所有面板在它下面实例化，统一像素坐标系。
- **每个面板一个独立预制体 `Assets/UI/Prefabs/<Panel>.prefab`**：根是**面板 RectTransform(不自带 Canvas)**，内部是按 Spec 建好的 Image/TMP/Button 树。这样面板**可复用**——挂到任意符合参考分辨率的 Canvas 下即原样呈现。
- **幂等**：`BuildUIFromSpec` 用 `PrefabUtility.SaveAsPrefabAsset` 覆盖式写同一路径，多轮迭代只更新这一个 prefab，GUID 稳定、引用不丢。
- **GUID/引用稳定**：精灵/字体按资源路径加载并写入 prefab 序列化引用；改 Spec 重建不破坏已建立的资源引用。
- `RenderCanvasToPng` 在临时场景里实例化 `UIRoot` + `<Panel>` 来出 diff 图，渲染完销毁，**不污染** prefab 本身（prefab 保持干净、无 Canvas）。
- 后续可演进为**嵌套预制体**（公共按钮/图标做成子 prefab，被面板引用），v1 先出扁平面板 prefab。

### 关键架构决策（已按真实工程校正 Plan 设计）
- **不新建 asmdef**。MCP 包无 asmdef，其类型在 `Assembly-CSharp-Editor`；`Assets/Editor/**` 散落脚本也编进同一程序集 → 新原子工具/导入器**直接可见** MCP 基类，且经 autoReference 可见 `Game` 纯逻辑。（这推翻了 Plan agent “建 Game.Editor.UI.asmdef 引用 MCP asmdef” 的方案——那个 asmdef 不存在。）
- **纯逻辑放 `Game` 程序集**（`Assets/Scripts/UI/`），因为 `Game.Tests.EditMode` 只引用 `Game`，可测逻辑必须在此。
- 需给 `Assets/Scripts/Game.asmdef` **增加引用 `UnityEngine.UI` 与 `Unity.TextMeshPro`**（供 `UIHierarchyBuilder` 建 Image/TMP 组件）；`Game.Tests.EditMode.asmdef` 同样加这两个引用，以便断言组件类型与 RectTransform 数值。
- 截图用**专用正交相机 + RenderTexture 渲染到精确分辨率**，不用 `ScreenCapture`（游戏视图尺寸不确定，无法可靠 diff）。

## Spec 格式（JSON，Newtonsoft 反序列化）

每面板一个文件。坐标统一为**效果图像素、左上原点**，与我看图所量一致；嵌套子节点也用**绝对像素**，由导入器换算到父局部。

```jsonc
{
  "schemaVersion": 1,
  "referenceWidth": 1920, "referenceHeight": 1080,
  "rootName": "LoginPanel",
  "root": {
    "name": "LoginPanel", "type": "Container",
    "rect": { "x":0,"y":0,"w":1920,"h":1080 }, "anchorPreset": "stretch-full",
    "children": [
      { "name":"Dialog","type":"Image","rect":{"x":710,"y":360,"w":500,"h":360},
        "sprite":"Assets/UI/Icons/panel_bg.png","imageType":"Sliced",
        "border":{"l":24,"t":24,"r":24,"b":24},"color":"#FFFFFF",
        "children":[
          { "name":"LoginButton","type":"Button","rect":{"x":810,"y":600,"w":300,"h":80},
            "sprite":"Assets/UI/Icons/btn_blue.png","imageType":"Sliced","border":{"l":16,"t":16,"r":16,"b":16},
            "text":{"content":"确认登录","fontAsset":"Assets/UI/Fonts/CJK SDF.asset","fontSize":32,"color":"#FFFFFF","alignment":"Center"} }
        ] }
    ]
  }
}
```
- `type`: `Container|Image|RawImage|Text|Button`（Button = Image + 子 TMP）。
- 颜色 `#RRGGBB[AA]`；`sprite` 按**资源路径**引用（避免重名歧义）；`children` 数组顺序 = 兄弟序 = 绘制层级（后者在上）。
- `anchorPreset` 默认 null = 左上像素映射；`"stretch-full"` 用于全屏背景。

## 坐标数学（纯函数边界 = 可 EditMode 测）

Canvas：`CanvasScaler` = ScaleWithScreenSize，referenceResolution=(1920,1080)，match=0.5 → 在参考分辨率下 1 单位 = 1 像素。

左上锚定约定（每个像素放置节点）：对效果图 rect `(x,y,w,h)`（左上、y 向下），父绝对左上 `(px,py)`：
```
anchorMin = anchorMax = (0,1);  pivot = (0,1)
sizeDelta = (w,h)
anchoredPosition = (x - px, -(y - py))
```
`stretch-full`：anchorMin=(0,0),anchorMax=(1,1),offsetMin=offsetMax=(0,0)。

**纯函数**（`Game`，仅用 Vector2/Color，无 GameObject）：`UISpecMath.TopLeftLayout/StretchFull`、`ColorUtil.ParseHex`、alignment 字串→`TextAlignmentOptions` 映射、`UISpecParser.Parse`(校验返回错误列表)。导入器只是把 `RectLayout` 套到真实 `RectTransform` 上的胶水。

## CJK 字体（v1 前置，非阻塞——给方案）

需要一个含中文的 `.ttf/.otf`。**推荐用开源 OFL 字体 Noto Sans SC / 思源黑体**（可自由嵌入，绕开授权阻塞）；用户也可放自有字体。
- 放 `Assets/UI/Fonts/`，用 TMP Font Asset Creator 生成，**Atlas 模式选 Dynamic SDF**（按需栅格化，任意中文字符都能渲染，不必预烘 2 万字）。
- 设为 TMP 默认字体；渲染前对每个 TMP 调 `ForceMeshUpdate()`/`TryAddCharacters` 确保字形已生成，否则首帧可能缺字影响 diff。

## 图标/精灵导入

- 图标放 `Assets/UI/Icons/`；`AssetPostprocessor`(`UIIconPostprocessor.cs`) 在 `OnPreprocessTexture` 设 `TextureImporterType.Sprite`、关 mipmap、Clamp、v1 高质量/不压缩。
- 9-slice：导入器遇 `imageType:"Sliced"` 时，按 Spec 的 `border` 同步该精灵 `TextureImporter.spriteBorder` 并 reimport（Spec 为单一真源）。
- **v1 暂不做 SpriteAtlas**（图集 padding/UV 会扰动像素 diff，先保真后优化）。

## 关键文件（路径）

纯核心（`Game`，`Assets/Scripts/UI/`）：`UISpec.cs`(DTO)、`UISpecParser.cs`、`UISpecMath.cs`、`ColorUtil.cs`、`UIHierarchyBuilder.cs`(用已加载的 sprite/font 建树，资源无关)。
编辑器（`Assets/Editor/UI/`，无 asmdef）：`UIBuilder.cs`(加载资源/同步 border/存 prefab，包装纯 builder)、`UIIconPostprocessor.cs`、`YIUIMCPTools_BuildUIFromSpec.cs`、`YIUIMCPTools_RenderCanvasToPng.cs`。
测试：`Assets/Tests/EditMode/UISpecTests.cs`。
改动：`Assets/Scripts/Game.asmdef` 与 `Game.Tests.EditMode.asmdef` 各加 `UnityEngine.UI`、`Unity.TextMeshPro` 引用。
产物目录：`Assets/UI/Fonts|Icons|Specs/`；**预制体产物 `Assets/UI/Prefabs/UIRoot.prefab`(共享根) + `Assets/UI/Prefabs/<Panel>.prefab`(每面板，交付物)**。规约：`specs/002-ui-from-mockup.md`。

### 两个新原子工具
- `BuildUIFromSpec`(specPath, outputPrefabPath) → 建 prefab；返回结构化结果(成功路径或校验错误列表)。比固定路径 ExecuteMenu 更适合多轮迭代。
- `RenderCanvasToPng`(prefabPath, outputPngPath, width=1920,height=1080,bg?) → 正交相机 + RenderTexture 渲染到精确分辨率、ReadPixels、EncodeToPNG，保证每次输出尺寸一致可 diff。

## Milestones

- **M0 字体前置**：放入 Noto Sans SC（或用户字体）→ 生成 Dynamic SDF TMP 资源 → 设默认。
- **M1 纯核心**：DTO/Parser/Math/ColorUtil/HierarchyBuilder + EditMode 测试 → DoD 绿。
- **M2 导入器 + 共享根**：`UIBuilder`(建面板树→`SaveAsPrefabAsset` 存**不带 Canvas 的面板 prefab**) + `UIIconPostprocessor`；一次性建好 `UIRoot.prefab`(Canvas+Scaler+Raycaster+EventSystem)。
- **M3 原子工具**：`BuildUIFromSpec`、`RenderCanvasToPng`（改完 .cs 走编译闸门）。
- **M4 单面板闭环（产出第一个交付 prefab）**：拿一张真实效果图，写 Spec → build(生成 `<Panel>.prefab`)→render→我对比→改→重建，迭代到像素级吻合，**锁定一个可复用的面板预制体**。
- **M5 泛化**：补节点类型与效果(Shadow/Outline/渐变)、第 2~3 个面板、可选 `ImageDiff` 叠加图工具。

## 验证（怎么测端到端）

1. DoD 门禁：`powershell -ExecutionPolicy Bypass -File .\scripts\dod.ps1`（编译 Success + `UISpecTests` 全绿）。
2. 工具链：`invoke-uto-tool.ps1 -Tool BuildUIFromSpec`（传 Spec/输出路径 base64）→ 期望返回 prefab 路径；再 `-Tool RenderCanvasToPng` → 产出 1920×1080 PNG。
3. 闭环：我读 `render.png` 与效果图对比，改 JSON 数值重跑，直至视觉吻合。
4. 运行核验：把 prefab 放进场景进 Play，中文正常、按钮可点。

## Top Risks

1. **看图估算有误**——首轮坐标/颜色必偏，靠 render→对比→改的闭环兜底（每面板 2~4 轮）；JSON 用绝对像素让改动最省。
2. **CJK 字体授权/体积**——用 OFL 字体规避授权；Dynamic SDF 避免烘全字集；渲染前补字形防缺字。
3. **9-slice/特效**——sliced 需同步 spriteBorder；Shadow/Outline/渐变 v1 schema 未含，列 v1.1；含这些特效的效果图在补齐前无法完全像素吻合。
4. **绘制层级**——`SetSiblingIndex` 必须严格匹配数组序（index 0 在底），用 EditMode 断言守住。
5. **Game 程序集耦合 UI/TMP**——需给 Game 与测试 asmdef 加引用，属预期内的受控改动。
