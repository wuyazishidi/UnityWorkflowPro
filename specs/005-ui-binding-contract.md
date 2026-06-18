# 005 — UI 绑定交付契约（Prefab + binding 描述 + 发布命令）

- 状态：草稿
- 负责人：
- 关联：[.claude/plans/ui-binding-contract.md](../.claude/plans/ui-binding-contract.md)、spec 004（figma-first）

## 1. 目标（Why）

让消费方工程（YC-Ego）**只凭「Prefab + 一份绑定描述」就能给 UI 接事件**，与本工程**完全解耦**：
消费方不引用本工程任何代码/脚本/生成管线，只共享 `binding.json` 的**格式约定**。
本工程产出该描述并打成可移植交付包；UI 更新后重跑一条命令即可重新交付。

## 2. 范围（Scope）

- 包含：
  - 走**已构建 prefab 的真实 GameObject 树** → 产 `<Panel>.binding.json` + `<Panel>.binding.md`。
  - 一条发布命令 `/publish-ui`（构建 prefab → 产描述 → 打交付包）。
  - 纯逻辑（树遍历→元素表/key/类型）抽到 Game 程序集 + EditMode 测试。
- **不包含**（明确划界）：
  - **不碰 Figma→Prefab 生成路径**（图生成不变，本契约只读已生成的 prefab）。
  - **不在本工程实现消费方绑定**（YC-Ego 的 `PanelBinder`/`wiring.json` 在 YC-Ego 侧，另立计划）。
  - 不维护「key→功能」的操作逻辑（那是消费方的事，本工程只描述「有什么可绑组件」）。

## 3. 设计与接口（What）

### 3.1 契约格式 `<Panel>.binding.json`（解耦唯一接口）

```jsonc
{
  "schemaVersion": "1.0",
  "panel": "TaskDetailPanel",
  "prefab": "Assets/UI/TaskDetailPanel/TaskDetailPanel.prefab",
  "size": { "w": 1106, "h": 778 },
  "dependencies": {
    "font": "MiSans Medium SDF",
    "commonSprites": ["round16", "ring16"],
    "atlas": "Assets/UI/TaskDetailPanel/TaskDetailPanel.spriteatlas",
    "needsTmpEssentials": true
  },
  "elements": [
    { "key": "startBtn", "path": "Container/Footer/Start_Btn", "type": "Button",
      "text": "领取并开始任务", "keyAuto": false }
  ]
}
```

- `type` ∈ `Button | InputField | Toggle | Slider | Dropdown | Scrollbar | ScrollList | Text`。
- **命名后缀约定**：翻译器（`figma_sync.py` `_apply_type_suffixes`）给固定类型节点名加类型后缀——
  `Button→_Btn`、`Image→_Image`、`Text→_Text`、`InputField→_InputField`、`Dropdown→_Dropdown`。
  GameObject 名因此自带类型（如 `Return_Btn`/`FPS_Text`），`path` 也带后缀，消费方据此即可识别类型。
- `key`：稳定 ASCII。取 GameObject 名（含类型后缀）→ 若为 ASCII 标识符则驼峰化（`Return_Btn → returnBtn`，
  后缀进 key → 同名不同类型不撞键）；否则兜底 `<type><序号>`（如 `button1`）并置 `keyAuto:true`。
- `nameTypeMismatch:true`：名称类型后缀与实际组件不符（如 `_Btn` 却非 Button 组件）→ 暴露「交互元素未生成为对应组件」，在 `.md` 显式标注（坐实 Phase A 可绑性缺口）。
- `path`：从 panel 根（不含根）到该节点的 transform 路径，精确到最终 prefab 实树。
- 只列可绑/有意义元素（交互组件 + 独立动态文本）；装饰 Image/Container 不入表（`_Image` 后缀仅用于 prefab 命名，不进描述）。

### 3.2 纯逻辑（Game 程序集，可 EditMode 测）

```csharp
namespace Game.UI {
  public enum BindKind { Button, InputField, Toggle, Slider, Dropdown, Scrollbar, ScrollList, Text }
  public sealed class BindElement { public string key, path, text; public BindKind kind; public bool keyAuto; }
  public sealed class BindDoc { public string panel; public List<BindElement> elements; }
  public static class BindingDescriptorBuilder {
    // 走真实 GameObject 树 → 元素表（不触 AssetDatabase / 文件 IO）。
    public static BindDoc Build(GameObject root, string panel);
    public static string ToKey(string goName, BindKind kind, int autoIndex, out bool keyAuto);
  }
}
```

- 组件识别（优先级，取第一个命中）：`Button → TMP_InputField → TMP_Dropdown → Toggle → Slider → Scrollbar → ScrollRect(=ScrollList)`。
- 文本提取：Button 取其 `Label` 子节点 TMP 文案；Text 取自身；InputField 取 placeholder 文案。
- 「独立动态文本」：`TextMeshProUGUI` 且其 GameObject 名不属于 builder 内部子节点（`Label/Placeholder/Text/Item Label/...`）、其父链最近交互组件之外 → 计为 `Text`。

### 3.3 Editor 导出器 + MCP 工具

```csharp
// Assets/Editor/UI/BindingDescriptorExporter.cs
public static class BindingDescriptorExporter {
  // 载入 prefab → BindingDescriptorBuilder.Build → 解析依赖(字体/round*-ring*/atlas) → 写 json + md。
  public static (bool ok, string json, string md, List<string> errors) Export(string prefabPath);
}
// Assets/Editor/UI/YIUIMCPTools_ExportBindingDescriptor.cs  → [YIUIMCPTools("ExportBindingDescriptor", ...)]
```

- JSON 手写（StringBuilder，稳定字段序、人可 diff），不依赖 JsonUtility。
- 依赖解析：遍历 prefab 上 `TMP_Text.font` 取字体名；`Image.sprite` 名匹配 `round*/ring*` 收进 commonSprites；
  `Assets/UI/<Panel>/<Panel>.spriteatlas` 存在则填 atlas；`needsTmpEssentials` 恒 true。

### 3.4 发布命令 `/publish-ui [Panel|all]` 与 Skill `ui-publish`

`Packages/cn.etetet.yiuimcp/Config/publish-ui.ps1`：
1. （可选）`ui-build-render.ps1` 重建 prefab（路径不变）。
2. MCP 调 `ExportBindingDescriptor` → 产 `binding.json/.md`。
3. `tools/export-ui-package.ps1`：把 `prefab + Icons + atlas + binding.* + 共享依赖(MiSans/Common)` 连 `.meta`
   打成 `delivery/<Panel>.unitypackage`（GUID 保真）+ README。

## 4. 约束（Constraints）

- 命名/目录遵循 `CLAUDE.md` §3：运行时纯逻辑→`Assets/Scripts/UI/`，编辑器→`Assets/Editor/UI/`，测试→`Assets/Tests/EditMode/`。
- 不改 `ProjectSettings/`、`Packages/manifest.json`、Figma 翻译器（除非 Phase A 修「可绑性」另立任务）。
- 改动最小化；含中文 `.ps1` 存 UTF-8 BOM（PS5.1 约束）。
- 禁止：在描述里写「key→功能」映射（解耦红线）；裸拷 png（交付走 .unitypackage/连 .meta）。

## 5. 验收标准（Acceptance — 必须可验证）

- [ ] 编译通过：`compile-unity-flow.ps1 -Force 0 -NoWait 1` → Success
- [ ] 控制台无报错：`get_console_error.ps1 -NoWait 1`
- [ ] EditMode `failed=0`：`BindingDescriptorBuilder` 对内存树（Button+InputField+独立Text）产出
      预期 `key/path/kind/text`，装饰节点不入表，Button 的 `Label` 子节点不被当独立 Text。
- [ ] 任取 `TaskDetailPanel`：`ExportBindingDescriptor` 产出 `binding.json`，`path` 与 prefab 实树一致、
      `dependencies` 写全；底部按钮若未成 `Button` 则在 `.md` 显式标注（暴露 Phase A 缺口）。

## 6. 备注 / 决策记录

- 解耦红线：消费方只读 `binding.json + prefab`，零反向依赖；操作逻辑（key→动作）在消费方维护。
- v1 不写 `nodeId`（builder 未把 Figma nodeId 落到 GameObject；key 稳定性靠 Figma ASCII 命名约定 = Phase A）。
- key 命名默认：Figma 名驼峰化兜底（不另维护映射表）；交付走 `.unitypackage`。
</content>
