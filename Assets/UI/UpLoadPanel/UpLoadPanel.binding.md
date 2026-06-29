# UpLoadPanel — UI 绑定描述

- prefab：`Assets/UI/UpLoadPanel/UpLoadPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `round10`, `round16`, `round2`, `round7`；图集 `Assets/UI/UpLoadPanel/UpLoadPanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `titleText` | Text | 数据上传 | `Paragraph/Title_Text` |  |
| `tipText` | Text | 数据上传 | `Paragraph/Tip_Text` |  |
| `customScrollList` | ScrollList |  | `CustomScrollList` |  |
| `returnBtn` | Button | 返回 | `Container_1/Return_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
