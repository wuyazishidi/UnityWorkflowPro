# SelectModulePanel — UI 绑定描述

- prefab：`Assets/UI/SelectModulePanel/SelectModulePanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `ring6`, `ring7`, `round16`, `round6`, `round7`；图集 `Assets/UI/SelectModulePanel/SelectModulePanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `currentVersionText` | Text | 当前版本： | `Container/CurrentVersion_Text` |  |
| `logoutBtn` | Button | 退出登录 | `Container/Container/Logout_Btn` |  |
| `checkRefreshBtn` | Button | 检查更新 | `Container/Container/CheckRefresh_Btn` |  |
| `text1` | Text | 模块选择 | `Container/Paragraph/模块选择_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `collectBtn` | Button | 采集模块 | `Container/ContainerFill_Image/Collect_Btn` |  |
| `checkBtn` | Button | 审核模块 | `Container/ContainerFill_Image_1/Check_Btn` |  |
| `uploadBtn` | Button | 上传模块 | `Container/ContainerFill_Image_2/Upload_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
>
> 有 1 个元素 key 为自动兜底（Figma 名非 ASCII）。如需稳定绑定，建议在 Figma 侧改用 `Start`/`User` 之类 ASCII 名（翻译器会补 `_Btn`/`_InputField`）后重发布。
