# ScanPanel — UI 绑定描述

- prefab：`Assets/UI/ScanPanel/ScanPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `ring12`, `ring3`, `ring6`, `round3`, `round5`, `round6`, `round8`；图集 `Assets/UI/ScanPanel/ScanPanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `offlineCollectBtn` | Button | 离线采集 | `Container_Image/Container/Footer_Image_1/OfflineCollect_Btn` |  |
| `quitBtn` | Button | 退出程序 | `Container_Image/Container/Footer_Image_1/Quit_Btn` |  |
| `text1` | Text | 扫码采集面板 | `Container_Image/Container/Header/扫码采集面板_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `manualInputBtn` | Button | 手动输入 | `Container_Image/Container/Header/ManualInput_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
>
> 有 1 个元素 key 为自动兜底（Figma 名非 ASCII）。如需稳定绑定，建议在 Figma 侧改用 `Start`/`User` 之类 ASCII 名（翻译器会补 `_Btn`/`_InputField`）后重发布。
