# LoginPanel — UI 绑定描述

- prefab：`Assets/UI/LoginPanel/LoginPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `ring10`, `ring16`, `round10`, `round16`；图集 `Assets/UI/LoginPanel/LoginPanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `text1` | Text | 源策未来 | `Logo/Paragraph/源策未来_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text2` | Text | 用户名 | `Form/Account/Container/用户名_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `containerInputInputField` | InputField | 请输入用户名 | `Form/Account/ContainerInput_InputField` |  |
| `text3` | Text | 密 码 | `Form/Password/Container/密 码_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `containerInputInputField` | InputField | 请输入密码 | `Form/Password/ContainerInput_InputField` |  |
| `text4` | Text | 动态码 | `Form/TOTP/Container/动态码_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `containerInputInputField` | InputField | 请输入动态码 | `Form/TOTP/ContainerInput_InputField` |  |
| `loginBtn` | Button | 登 录 | `Form/Container/Login_Btn` |  |
| `loginStateText` | Text | 登录中... | `Form/Container_1/Text/LoginState_Text` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
>
> 有 4 个元素 key 为自动兜底（Figma 名非 ASCII）。如需稳定绑定，建议在 Figma 侧改用 `Start`/`User` 之类 ASCII 名（翻译器会补 `_Btn`/`_InputField`）后重发布。
