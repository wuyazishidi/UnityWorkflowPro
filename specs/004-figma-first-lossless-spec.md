# 004 — 以 Figma 为主：UISpec 无损承接，卸下"看图估算"时代的架构债

- 状态：草稿（待确认方向后分阶段开工）
- 负责人：Jinwanpeng
- 关联：`specs/002`（UISpec 契约与构建引擎，后端）；`specs/003` 已废弃并入本规约（原 A/B 双入口 → 现 Figma 唯一入口，B 路移除）

## 1. 目标（Why）

管线最初是为 **"扁平 PNG → 肉眼估算 UISpec → 渲染出图与原图做像素 diff → 反复迭代收敛"** 而建——这套"估算 + 逼近"机制存在的唯一理由是**输入是一张没有结构数据的图**。

后来接入 Figma，REST API 直接给出**精确的几何 / 颜色 / 文字 / 层级 / 描边 / 渐变 / 透明度**。"估算 + 逼近"的前提消失了，但管线仍在用那套机制，于是它从"必要手段"变成**损耗源**：

- 把 Figma 的精确数据**压回一个有损中间层（UISpec）再去近似**——UISpec 当年只够描述"肉眼能从 PNG 看出的东西"，**没有 stroke / gradient / opacity / blend 概念**。于是 Figma 明确给的"描边 + 半透填充"只能用镂空环精灵去 hack、渐变被迫纯色近似 → **本质性色差**（见 002/本轮排查）。
- 每次同步都背着**最不稳的环节**：`RenderCanvasToPng` 冷渲染掉连接、重试多次——而几何/颜色既已精确，这条 diff 本应是"可选验证"而非"必经迭代"。
- **导入策略冲突**：`UIIconPostprocessor` 强制 Uncompressed 是为"小图标时代"调的，Figma 导出的整卡大图走同一策略 → 卡死 Unity 主线程（靠降采样 ≤1280 绕过）。
- **双真相源漂移**：手调的 `<Panel>.json` 与自动 `<Panel>.draft.json` 并存会漂移。
- **丢弃响应式信息**：UISpec 用"固定参考分辨率 + 左上绝对像素"（量截图的模型），Figma 的 constraints/auto-layout 被丢弃 → Prefab 钉死在单一分辨率。

**目标**：以 **Figma 为唯一真相源**，让 UISpec **无损承接** Figma 原语，把"估算 + 迭代逼近"机制从主路径上卸下；从源头消灭色差/渐变退化这一类 bug，并去掉链路里最不稳的环节。

## 2. 范围（Scope）

- **包含**：
  - UISpec 扩展为 v2：新增 `stroke` / `gradient` / `opacity` / `blend`、（承接但暂不实现的）`constraints` 字段，**向后兼容 v1**。
  - `UIHierarchyBuilder` 及配套纯逻辑（Validator/Json/Math）承接新字段。
  - spec 作为**纯生成物**：`figma_sync.py` 直接产出 `<Panel>.json`，废弃常态 `draft`，消灭双真相。
  - 渲染 diff **从迭代降级为可选 QA**（`-Verify` 开关 + 区域 MAE 报告）。
  - Figma 导出位图与共享小精灵的**导入策略分流**。
  - **语义组件映射**：按 Figma 节点命名把"看着像输入框/按钮的东西"映射成**真正的功能组件**（`TMP_InputField` / `Button` / 后续 Toggle/Slider/Dropdown），而非纯视觉 Image+Text。**生成的 UI 要和功能匹配**。
- **不包含**（明确划界）：
  - 不重写 002 的纯核心 / headless 解耦（沿用）。
  - 不在本期强制引入运行期矢量（SVG / `com.unity.vectorgraphics`）——列入 Phase 4 后续评估。
  - 不在本期完整实现多分辨率响应式布局——本期只保证 `constraints` 字段"承接不丢"，驱动 RectTransform 留 Phase 4。
  - 不改 `Packages/cn.etetet.yiuimcp/**` 内部。
  - **B 路（纯 PNG 看图估算闭环）已整体移除**：Figma 是唯一入口，UISpec 由 Figma 精确数据生成，不再人工看图估算（003 已废弃）。

## 3. 设计与接口（What）

### 3.1 UISpec v2 字段（向后兼容，旧字段保留）

```csharp
public class UINode {
    // …现有字段不变…
    public float? opacity;          // 节点整体不透明度 0-1（区别于 color 的 alpha）
    public UIStroke stroke;         // 描边（替代"镂空环精灵" hack）
    public UIGradient gradient;     // 渐变填充（替代纯色近似）
    public string blend;            // Normal/Multiply/Screen…（可选，先只认 Normal）
    public UIConstraints constraints; // 锚定意图：承接不丢，Phase 4 才驱动
}
public class UIStroke   { public string color; public float weight = 1f; public string align = "Inside"; }
public class UIGradient { public string type = "Linear"; public UIGradientStop[] stops; public float angle; }
public class UIGradientStop { public string color; public float pos; }
public class UIConstraints { public string horizontal = "Left"; public string vertical = "Top"; }
```

- `schemaVersion` → 2；解析器对缺失的新字段按 v1 行为处理（null = 老逻辑）。

### 3.2 渲染策略：SDF 优先，精灵回退

- builder 对"形状类节点"（圆角矩形 + 描边 + 渐变）优先走 **SDF UI 材质**（Phase 3）；无 shader 时**回退** round/ring 9-slice 精灵 + 顶点色渐变（Phase 2 即可用，不破坏 002）。
- 平滑过渡：Phase 2 用精灵承接新字段，Phase 3 再切 SDF，期间产物不变样。

### 3.3 真相源与 QA

- `figma_sync.py` 直接生成/覆盖 `<Panel>.json`（不再常态写 `draft`）。人工微调走 **"覆盖式重生成 + `git diff` 审阅"**，而非维护并行副本。
- `ui-build-render.ps1` 加 `-Verify`（默认 `false`）：仅在该开关下出 `_render.png` 并算与 `.figma/truth.png` 的**分区域 MAE**，超阈值告警；常态同步**不渲染**，摘掉最不稳环节。

### 3.4 导入分流

- Figma 导出位图：独立导入策略（按显示尺寸压缩、不强制 Uncompressed），从源头避免大图卡主线程。
- 共享小精灵（round/ring 等）：保持现状。

## 4. 约束（Constraints）

- 向后兼容 `schemaVersion: 1`：旧 spec 必须仍能 build（回归测试守住）。
- 纯逻辑仍在 `Game` 程序集、可 EditMode 测；遵循 `CLAUDE.md` 第 3 节命名/目录。
- SDF shader 改动不得破坏现有面板渲染（有回退路径）。
- 不引第三方包（SVG 留 Phase 4 单独评估）。
- 禁止：再用"镂空环精灵 / 纯色"去近似 Figma 已精确给出的 stroke/gradient（v2 后视为反模式）。

## 5. 验收标准（Acceptance — 必须可验证）

- [ ] 编译通过：`compile-unity-flow.ps1 -Force 0 -NoWait 1` → Success
- [ ] EditMode 全绿：新增 `stroke/gradient/opacity/constraints` 的解析与布局有测试；**v1 旧 spec 回归测试通过**。
- [ ] 取一个含**描边 + 半透 + 线性渐变**的 Figma 帧，`figma-sync` 一条命令产出 prefab，输入框/按钮区域与 Figma 的 MAE < 设定阈值（**根治本轮色差**）。
- [x] 常态同步不再默认渲染；`-Verify` 时才出比对图与 MAE 报告。（Phase 1）
- [x] 大图导入不再强制 Uncompressed（>1024px 走 Compressed+限 2048，不卡主线程）。（Phase 1）
- [x] 仓库中不再出现 `<Panel>.draft.json` 与 `<Panel>.json` 并存的双真相。（Phase 1）

## 6. 备注 / 决策记录

- **为何保留 UISpec 这一层**：它是可测的纯逻辑 + 构建契约，有价值；损耗来自"把它当作对 PNG 的肉眼近似手稿"这种**用法**，改成"Figma 的忠实生成投影"即可，不必废层。
- **SDF vs 精灵**：SDF 同时解决色差 / 导入 / DC，但需写 shader；故分阶段——先"字段无损 + 精灵承接"，再切 SDF，风险可控、随时可退。
- **B 路（纯 PNG 看图估算）已移除**：它为"无结构数据的扁平图"而生，是本 spec §1 所述损耗的根源；Figma 提供精确数据后无保留必要（003 已废弃）。

## 7. 分阶段改造计划

> 每阶段独立可交付、可回退。收益排序：Phase 1 最快（摘掉最不稳环节 + 消漂移），Phase 2/3 根治色差与纹理负担。

### Phase 0 — 对齐与隔离（不改运行行为）
- 落地本 spec；在 `figma_sync.py` 标注当前哪些是"近似"（镂空环 = 假 stroke、纯色 = 假 gradient），列为待替换点。
- **验收**：文档落地；现有面板照常 build，行为不变。

### Phase 1 — 真相源收敛 + 渲染降级 + 导入分流（高收益、低风险）✅ 已完成
- ✅ `figma-sync`/`figma-pull` 直接生成/覆盖 `<Panel>.json`，废弃常态 `draft`；微调走"重生成 + git diff 审阅"。
- ✅ `ui-build-render` 加 `-Verify`（switch，默认关）：仅该开关出 `_render.png` + 分区域 MAE 报告；常态不渲染。
- ✅ 导入分流：`UIIconPostprocessor` 按 PNG 宽度分流——>1024px 大图 Compressed+限 2048，小图标维持 Uncompressed。
- ✅ 附带修正：面板根收敛到**卡片**(非外层画板)，去掉画板空白，使渲染与卡片真值同框 → `-Verify` MAE 有意义（实测 81→13.9）。
- **验收（已验证）**：同步链路常态无渲染（无冷渲染重试）；大图导入 `textureCompression=Compressed` 不卡；仓库无 `.draft.json` 双真相。

### Phase 2 — UISpec v2 无损字段 + builder 承接（精灵实现）
- UISpec v2 加 `stroke/gradient/opacity/constraints`（承接不丢）；`UISpecValidator/UISpecJson/UISpecMath` 配套；`UIHierarchyBuilder` 用现有 round/ring 精灵 + 顶点色渐变**实现** stroke/opacity/线性渐变。
- `figma_sync` 直译这些字段，**停用**镂空环 / 纯色近似。
- EditMode 测试覆盖新字段解析与布局；v1 回归。
- **验收**：含描边 + 半透 + 线性渐变的面板 MAE < 阈值；色差类 bug 关闭。

### Phase 2.5 — 语义组件映射（功能匹配）
> 当前痛点：生成的 UI 只还原**外观**（输入框是 Image+占位符 Text），不还原**功能**（不是 `TMP_InputField`、密码框不是密码输入、眼睛不是 toggle）。Figma 节点命名已带语义（实测内层帧名为 `Text Input` / `Password Input` / `Button`），据此映射即可，无需猜。

- UISpec 新增 `InputField` 类型契约：背景（精灵+9-slice 或 SDF shape）作 targetGraphic、`placeholder`（文本+色）、`textColor`、`contentType`（Standard/Password/…）、可选 `characterLimit`。
- `UIHierarchyBuilder` 新增 `BuildInputField`：建 `TMP_InputField` + Text Area（Viewport）+ Text 组件 + Placeholder + Caret，正确接线 `textComponent`/`placeholder`/`targetGraphic`。
- `figma_sync.py` 命名映射：含 `Input`/`输入` 的填充+描边框 → `InputField`；名为 `Password*` → `contentType=Password`，并把其内的眼睛图标接 show/hide；名为 `Button` 且有居中文字 → `Button`。建立可扩展的"命名约定 → 组件"表（后续 Toggle/Slider/Dropdown 同法）。
- Validator 放行新类型；EditMode 测试覆盖命名映射与 InputField 接线。
- **验收**：Login 同步出的输入框是可输入的 `TMP_InputField`，密码框为 Password、可切换显隐；按钮可点击；无需手动在 Inspector 改组件。

### Phase 3 — SDF shape 渲染（去纹理）
- 写 SDF UI shader（圆角 + 描边 + 线性/径向渐变 + 可选软阴影）；builder 新增 `Shape` 走材质，精灵作回退。
- 替换 round*/ring* 用法；形状类元素**零纹理、矢量级清晰**。
- **验收**：圆角/描边/渐变不再依赖精灵；DC 不升；视觉与 Figma 一致。

### Phase 4（可选）— 矢量与响应式
- 矢量节点走 SVG（`com.unity.vectorgraphics`）或 Figma `geometry=paths` 生成 mesh。
- `constraints/auto-layout` 真正驱动 RectTransform 锚定 → 多分辨率自适应。
- **验收**：矢量图标清晰可缩放；面板在不同分辨率自适应。
