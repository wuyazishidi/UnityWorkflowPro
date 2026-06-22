# Tip — UI 绑定描述

- prefab：`Assets/UI/Tip/Tip.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `ring11`, `ring6`, `round11`, `round6`；图集 `Assets/UI/Tip/Tip.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `text1` | Text | 警告 | `ContainerFill_Image/Container/Text/警告_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `messageText` | Text | 追踪器断开连接，请检查是否正常工作，重新启动，校准 | `ContainerFill_Image/Container/Text_1/Message_Text` |  |
| `closeBtn` | Button | 关闭 | `ContainerFill_Image/Close_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
>
> 有 1 个元素 key 为自动兜底（Figma 名非 ASCII）。如需稳定绑定，建议在 Figma 侧改用 `Start`/`User` 之类 ASCII 名（翻译器会补 `_Btn`/`_InputField`）后重发布。
