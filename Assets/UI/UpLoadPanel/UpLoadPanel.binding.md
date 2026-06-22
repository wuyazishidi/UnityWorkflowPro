# UpLoadPanel — UI 绑定描述

- prefab：`Assets/UI/UpLoadPanel/UpLoadPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `round10`, `round16`, `round2`, `round7`；图集 `Assets/UI/UpLoadPanel/UpLoadPanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `text1` | Text | 数据上传 | `Paragraph/数据上传_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `customScrollList` | ScrollList |  | `CustomScrollList` |  |
| `buttonBtn` | Button | 继续采集 | `Container_1/Button_Btn` |  |
| `buttonBtn1` | Button | 去上传 | `Container_1/Button_Btn_1` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
>
> 有 1 个元素 key 为自动兜底（Figma 名非 ASCII）。如需稳定绑定，建议在 Figma 侧改用 `Start`/`User` 之类 ASCII 名（翻译器会补 `_Btn`/`_InputField`）后重发布。
