# 计划：UI 交付契约 —— 本项目产「Prefab + 绑定描述」，YC-Ego 只吃 Prefab

> **职责划分(用户定调)**
> - **本项目 UnityWorkflowPro = 只做 UI**：Figma→Prefab，并**随 prefab 产出一份「绑定描述」**，
>   把 prefab 里有哪些可绑组件、叫什么、什么类型、在什么路径、当前文案，统统讲清楚。
> - **YC-Ego = 只处理 Prefab**：拿 prefab + 描述就能绑事件，**完全不接触 Figma**
>   (不需要 node-id / figma-sync / 设计来源)。命名与稳定性是**本项目内部的事**。
>
> 两边的唯一接口 = **绑定描述文件** `<Panel>.binding.json`(本项目自动生成、入库、随 prefab 交付)。

---

## 1. 为什么需要"绑定描述"

YC-Ego 要给 prefab 接事件,必须知道:**哪个组件是开始按钮、哪个是输入框、它在 prefab 里的路径**。
现状的痛点(实测 `TaskDetailPanel`):
- prefab 节点名 = Figma 文案,如 `任务编号：` / `1004` / `#1 桌面小工具归入收纳盒` —— **中文、随文案漂移、不可作绑定键**。
- 底部"返回列表 / 领取并开始任务"**没被翻译成 `Button`**(只是 Container+Text),YC-Ego 想 `onClick` 都没处接。

所以"做好描述"包含两件事,都在**本项目**侧:
1. **保证可绑**:该交互的元素真的生成成 `Button` / `TMP_InputField` / `Toggle` / ...(翻译器质量)。
2. **讲清楚**:产出 `<Panel>.binding.json`,给每个可绑元素一个**稳定键 + 路径 + 类型 + 文案**。

---

## 2. 契约格式：`<Panel>.binding.json`

随 prefab 生成于 `Assets/UI/<Panel>/<Panel>.binding.json`(入库)。示例:

```jsonc
{
  "panel": "TaskDetailPanel",
  "prefab": "Assets/UI/TaskDetailPanel/TaskDetailPanel.prefab",
  "size": { "w": 1106, "h": 778 },
  "dependencies": {                         // YC-Ego 需要一并就位的资产(见 §4)
    "font": "MiSans Medium SDF",
    "commonSprites": ["round16", "ring16"],
    "atlas": "Assets/UI/TaskDetailPanel/TaskDetailPanel.spriteatlas",
    "needsTmpEssentials": true
  },
  "elements": [                             // 仅列「可绑/有意义」的组件,装饰不列
    {
      "key": "btnStart",                    // 稳定键(本项目保证稳定) → YC-Ego 绑这个
      "path": "Container/Footer/BtnStart",  // prefab 内 transform 路径(精确)
      "type": "Button",
      "text": "领取并开始任务",
      "nodeId": "8:53"                       // Figma 节点 id(稳定锚,仅供本项目追溯,YC-Ego 可忽略)
    },
    {
      "key": "btnBack",
      "path": "Container/Footer/BtnBack",
      "type": "Button",
      "text": "返回列表"
    },
    {
      "key": "labelTaskTitle",
      "path": "Container/Body/Title",
      "type": "Text",
      "text": "#1 桌面小工具归入收纳盒"
    },
    {
      "key": "inputUser",
      "path": "Form/InputUser",
      "type": "InputField",
      "contentType": "Standard"
    }
  ]
}
```

设计要点:
- **`key` 是 YC-Ego 的绑定键**:稳定、ASCII、语义化。由本项目据节点角色生成/约定;Figma 文案改了,
  `key` 不变。YC-Ego 只认 `key`(回退用 `path`)。
- **`path` 精确到最终 prefab 层级**(含 builder 加的 `Label`/`Text Area` 等子节点)→ 必须**从已构建的 prefab 走树**生成,
  而非从 spec 推断(spec 树与 prefab 略有出入)。
- **只列可绑元素**(`Button`/`TMP_InputField`/`Toggle`/`Slider`/`Dropdown`/`ScrollList`,以及被标为"动态文本"的 `Text`),
  装饰性 Image/Container 不入表,描述保持精简可读。
- 另出一份 **`<Panel>.binding.md`**(人读版,表格列 key/类型/文案/路径),给 YC-Ego 开发当文档。

---

## 3. 本项目要做的(主体工作)

### Phase A — 翻译器补齐"可绑性"(质量前置)
没生成成正确组件,描述再好也绑不了。
- [ ] 复核 Button 识别:`TaskDetailPanel` 底部两按钮未成 `Button` → 修翻译器
      (`scripts/figma_sync.py` 的 Button 启发式;按命名/居中文字+填充框)或在 Figma 侧按约定命名。
- [ ] 核对 InputField/Toggle/... 在各面板是否如期生成。
- [ ] 约定:**需要绑定的元素在 Figma 用稳定 ASCII 名**(`BtnStart`/`InputUser`/`ListTasks`),
      此约定写进本项目 `CLAUDE.md` 第 4 节(Figma 命名规范)。

### Phase B — 绑定描述生成器(核心新功能)
- [ ] 在 prefab 构建后,**走一遍已构建 prefab 的 GameObject 树**,收集可绑组件 → 产 `<Panel>.binding.json` + `.md`。
      实现位置:`Assets/Editor/UI/` 新增 `BindingDescriptorExporter.cs`(C#,能拿到真实组件与路径);
      由 `ui-build-render.ps1` 在 build 成功后自动调用(MCP 工具或 menu)。
- [ ] `key` 生成规则:优先取 Figma 稳定名→驼峰化;否则按 `<type><序号>`(如 `button1`)兜底并在 `.md` 标注"建议改名"。
- [ ] 把 `dependencies` 写全(字体/公共精灵/atlas/是否需 TMP Essentials),供 YC-Ego 交付检查。
- [ ] 给生成器加 EditMode 测试(树遍历→元素表的纯逻辑)。

### Phase C — 可移植交付包
- [ ] 写 `tools/export-ui-package.ps1`:把 `<Panel>.prefab + Icons + atlas + binding.* +
      共享依赖(MiSans 字体、Assets/UI/Common/*)`连 `.meta` 打成一个交付目录/`.unitypackage`,
      供 YC-Ego 直接导入(GUID 保真)。
- [ ] 交付包内附 `README`:列依赖、TMP Essentials 提示、binding 契约说明。

### Phase D — 文档
- [ ] 本项目 `CLAUDE.md`:登记 binding 契约格式、生成器入口、Figma 命名规范。
- [ ] `specs/` 下补一份"UI 交付契约" spec(格式版本化,便于演进)。

---

## 4. YC-Ego 侧(很薄,且零 Figma)

> 仅供对接参考,不在本项目实施;放这里是为了说清契约怎么被消费。

1. 导入交付包(prefab + 依赖 + binding 描述);首次需 `Import TMP Essential Resources`(描述里 `needsTmpEssentials` 已提示)。
2. 写一个通用 `PanelBinder`:`Resources.Load`/引用 prefab → 读同名 `<Panel>.binding.json` →
   按 `key`→`path` 拿 `Button`/`TMP_InputField` 等 → 接 `onClick` 到 `RecordingSession.EnterPreparing/Stop`、
   `WizardController` 导航。
3. YC-Ego 全程**只认 `key` 和 prefab**,不知道 Figma 存在。

YC-Ego 的对接细节由 YC-Ego 侧另立计划;本项目只保证交付物(prefab + 描述 + 依赖)正确、稳定、自洽。

---

## 5. 兼容性约束(本项目交付时要在描述里讲明)

| # | 事项 | 描述里如何体现 |
|---|------|----------------|
| C1 | YC-Ego 无 TMP Essentials(包已装) | `dependencies.needsTmpEssentials = true` |
| C2 | 默认字体 MiSans 中文 SDF | `dependencies.font` + 交付包内含字体资产 |
| C3 | 公共精灵 round*/ring* | `dependencies.commonSprites` + 交付包内含 |
| C4 | 跨工程 GUID | 交付走 `.unitypackage`/连 `.meta` 目录,绝不裸拷 png |

---

## 6. 验证标准(DoD)

- 改了 C#(生成器/翻译器)→ 编译 `Success` + EditMode `failed=0`(本项目硬闸门)。
- 任取一面板(建议 `TaskDetailPanel`):
  - 底部按钮确为 `Button` 类型;
  - `<Panel>.binding.json` 列出全部可绑元素,`path` 与 prefab 实树一致,`key` 稳定;
  - 交付包导入一张白工程能正确显示(中文/圆角/不粉红),按 `key`→`path` 能取到组件。

---

## 7. 待确认

1. `key` 命名:由本项目据 Figma 名自动驼峰化即可,还是要我维护一份 `<Panel>` 的 key 映射表(更可控)？
2. 交付方式:`.unitypackage`(GUID 稳、手动)还是 目录+sync 脚本(可 CI)？(建议先 `.unitypackage` 跑通,再脚本化)
3. 是否现在就先做 **Phase A 翻译器补齐 + Phase B 描述生成器**(本项目能独立闭环、可验证),YC-Ego 侧等你这边稳定后再起？
