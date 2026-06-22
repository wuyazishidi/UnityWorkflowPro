# CountdownPanel — UI 绑定描述

- prefab：`Assets/UI/CountdownPanel/CountdownPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `ring11`, `ring12`, `ring7`, `ring9`, `round11`, `round12`, `round7`, `round9`；图集 `Assets/UI/CountdownPanel/CountdownPanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `fPSText` | Text | FPS | `Container/ContainerFill_Image/Container/Text/Text/FPS_Text` |  |
| `fpsText` | Text | 103 | `Container/ContainerFill_Image/Container/Text/Text_1/fps_Text` |  |
| `text1` | Text | \| | `Container/ContainerFill_Image/Container/Text/Text_2/Text/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text2` | Text | 内存 | `Container/ContainerFill_Image/Container/Text_1/Text/内存_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `memoryText` | Text | 4/144M | `Container/ContainerFill_Image/Container/Text_1/Text_1/Memory_Text` |  |
| `text3` | Text | \| | `Container/ContainerFill_Image/Container/Text_1/Text_2/Text/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text4` | Text | 磁盘 | `Container/ContainerFill_Image/Container/Text_2/Text/磁盘_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `capacityText` | Text | 211.7G | `Container/ContainerFill_Image/Container/Text_2/Text_1/Capacity_Text` |  |
| `text5` | Text | \| | `Container/ContainerFill_Image/Container/Text_2/Text_2/Text/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text6` | Text | 当前账号： | `Container/ContainerFill_Image/Container/Text_3/Text/当前账号：_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `userNameText` | Text | collector01 | `Container/ContainerFill_Image/Container/Text_3/Text_1/UserName_Text` |  |
| `taskIDText` | Text | 任务# ae1 \| a#1 | `Container/ContainerFill_Image/Container_1/Text/TaskID_Text` |  |
| `text7` | Text | \| | `Container/ContainerFill_Image/Container_1/Text_1/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `cameraStateText` | Text | 相机● | `Container/ContainerFill_Image/Container_1/Text_2/CameraState_Text` |  |
| `text8` | Text | \| | `Container/ContainerFill_Image/Container_1/Text_3/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `stateText` | Text | 准备中 | `Container/ContainerFill_Image/Container_1/TextFill_Image/State_Text` |  |
| `text9` | Text | \| | `Container/ContainerFill_Image/Container_1/Text_4/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `strategyText` | Text | 口述策略：仅当出端 | `Container/ContainerFill_Image/Container_1/Text_5/Strategy_Text` |  |
| `countdownText` | Text | 准备中 3秒后开始采集 | `Container/Container/Container/Container/Container/Paragraph/Countdown_Text` |  |
| `text10` | Text | 注意事项 | `Container/Container/Container/Container/ContainerFill_Image/Container/Text/注意事项_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `countentText` | Text | 1.请在开始采集后，语音说：开始采集  在结束采集前，语音说：结束采集  2.请全程保持手部在Pico的可视范围内，距离Pico  大致0.6m左右  | `Container/Container/Container/Container/ContainerFill_Image/Container/Text_Image/Countent_Text` |  |
| `returnBtn` | Button | 返回选择任务 | `Container/Container/ContainerFill_Image/Return_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
>
> 有 10 个元素 key 为自动兜底（Figma 名非 ASCII）。如需稳定绑定，建议在 Figma 侧改用 `Start`/`User` 之类 ASCII 名（翻译器会补 `_Btn`/`_InputField`）后重发布。
