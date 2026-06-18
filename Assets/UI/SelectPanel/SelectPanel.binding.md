# SelectPanel — UI 绑定描述

- prefab：`Assets/UI/SelectPanel/SelectPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `ring12`, `round12`, `round16`；图集 `Assets/UI/SelectPanel/SelectPanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `taskIDText` | Text | 任务#2 | `Container_Image/Container/TaskID_Text` |  |
| `taskDescriptionText` | Text | 场景：书房         状态：assigned        已传0段 | `Container_Image/Container/Text/TaskDescription_Text` |  |
| `returnBtn` | Button | 返回 | `Container_1/Return_Btn` |  |
| `continueCollectBtn` | Button | 继续采集 | `ContainerFill_Image/Container/ContinueCollect_Btn` |  |
| `uploadBtn` | Button | 上传 | `ContainerFill_Image/Container/Upload_Btn` |  |
| `abandonBtn` | Button | 放弃任务 | `ContainerFill_Image/Container/Abandon_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
