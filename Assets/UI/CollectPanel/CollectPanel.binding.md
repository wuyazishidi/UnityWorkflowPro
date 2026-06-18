# CollectPanel — UI 绑定描述

- prefab：`Assets/UI/CollectPanel/CollectPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `ring12`, `ring16`, `round12`, `round16`, `round24`；图集 `Assets/UI/CollectPanel/CollectPanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `fPSText` | Text | FPS | `Container_Image/ContainerFill_Image/Container/Text/Text/FPS_Text` |  |
| `fpsText` | Text | 103 | `Container_Image/ContainerFill_Image/Container/Text/Text_1/fps_Text` |  |
| `text1` | Text | \| | `Container_Image/ContainerFill_Image/Container/Text/Text_2/Text/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text2` | Text | 内存 | `Container_Image/ContainerFill_Image/Container/Text_1/Text/内存_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `memoryText` | Text | 4/144M | `Container_Image/ContainerFill_Image/Container/Text_1/Text_1/Memory_Text` |  |
| `text3` | Text | \| | `Container_Image/ContainerFill_Image/Container/Text_1/Text_2/Text/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text4` | Text | 磁盘 | `Container_Image/ContainerFill_Image/Container/Text_2/Text/磁盘_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `capacityText` | Text | 211.7G | `Container_Image/ContainerFill_Image/Container/Text_2/Text_1/Capacity_Text` |  |
| `text5` | Text | \| | `Container_Image/ContainerFill_Image/Container/Text_2/Text_2/Text/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text6` | Text | 当前账号： | `Container_Image/ContainerFill_Image/Container/Text_3/Text/当前账号：_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `userNameText` | Text | collector01 | `Container_Image/ContainerFill_Image/Container/Text_3/Text_1/UserName_Text` |  |
| `taskIDText` | Text | 任务# ae1 \| a#1 | `Container_Image/ContainerFill_Image/Container_1/Text/TaskID_Text` |  |
| `text7` | Text | \| | `Container_Image/ContainerFill_Image/Container_1/Text_1/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `cameraStateText` | Text | 相机● | `Container_Image/ContainerFill_Image/Container_1/Text_2/CameraState_Text` |  |
| `text8` | Text | \| | `Container_Image/ContainerFill_Image/Container_1/Text_3/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `stateText` | Text | 准备中 | `Container_Image/ContainerFill_Image/Container_1/TextFill_Image/State_Text` |  |
| `text9` | Text | \| | `Container_Image/ContainerFill_Image/Container_1/Text_4/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `strategyText` | Text | 口述策略：仅当出端 | `Container_Image/ContainerFill_Image/Container_1/Text_5/Strategy_Text` |  |
| `text10` | Text | 左手柄 | `Container_Image/Container/ContainerFill_Image/green_left/ContainerFill_Image/左手柄_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text11` | Text | 左手柄 | `Container_Image/Container/ContainerFill_Image/green_left/ContainerFill_Image_1/左手柄_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text12` | Text | 右手柄 | `Container_Image/Container/ContainerFill_Image/green_left_1/ContainerFill_Image/右手柄_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text13` | Text | 右 手柄 | `Container_Image/Container/ContainerFill_Image/green_left_1/ContainerFill_Image_1/右 手柄_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text14` | Text | 相机 | `Container_Image/Container/ContainerFill_Image/green_left_2/ContainerFill_Image/相机_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text15` | Text | 相机 | `Container_Image/Container/ContainerFill_Image/green_left_2/ContainerFill_Image_1/相机_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text16` | Text | 手 | `Container_Image/Container/ContainerFill_Image/green_left_3/ContainerFill_Image/手_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text17` | Text | 手 | `Container_Image/Container/ContainerFill_Image/green_left_3/ContainerFill_Image_1/手_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text18` | Text | 腕手适配度 | `Container_Image/Container/ContainerFill_Image/green_left_4/ContainerFill_Image/腕手适配度_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text19` | Text | 腕手适配度 | `Container_Image/Container/ContainerFill_Image/green_left_4/ContainerFill_Image_1/腕手适配度_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text20` | Text | 追踪器 5/5 | `Container_Image/Container/ContainerFill_Image/green_left_5/ContainerFill_Image/追踪器 5/5_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text21` | Text | 追踪器5/5 | `Container_Image/Container/ContainerFill_Image/green_left_5/ContainerFill_Image_1/追踪器5/5_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text22` | Text | 网络 | `Container_Image/Container/ContainerFill_Image/green_right/Container/Text/网络_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text23` | Text | 网络 | `Container_Image/Container/ContainerFill_Image/green_right/Container_1/Text/网络_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text24` | Text | 音频 | `Container_Image/Container/ContainerFill_Image/green_right_1/Container/Text/音频_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text25` | Text | 音频 | `Container_Image/Container/ContainerFill_Image/green_right_1/Container_1/Text/音频_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `stateMessageText` | Text | 准备中（点「结束采集」取消） | `Container_Image/Container/Container/Container/Container/Paragraph/StateMessage_Text` |  |
| `text26` | Text | 手部未进入摄像头视野 | `Container_Image/Container/Container/Container/Container_1/Container/ContainerFill_Image/Container_1/Inlinecontent/手部未进入摄像头视野_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text27` | Text | 请将手放在摄像头前方约 50cm 处，掌心朝前保持静止 | `Container_Image/Container/Container/Container/Container_1/Container/ContainerFill_Image/Container_1/Container/Text/请将手放在摄像头前方约 50cm 处，掌心朝前保持静止_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text28` | Text | 准备完成 | `Container_Image/Container/Container/Container/Container_1/Container/ContainerFill_Image/Text/TextFill_Image/准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text29` | Text | 手势s识别模式未切换成功 | `Container_Image/Container/Container/Container/Container_1/Container/ContainerFill_Image_1/Container_1/Inlinecontent/手势s识别模式未切换成功_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text30` | Text | 等待模式切换为手势识别，请将手放在摄像头前方约 50cm 处，掌心朝前保持静止 | `Container_Image/Container/Container/Container/Container_1/Container/ContainerFill_Image_1/Container_1/Container/Text/等待模式切换为手势识别，请将手放在摄像头前方约 50cm 处，掌心朝前保持静止_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text31` | Text | 未准备完成 | `Container_Image/Container/Container/Container/Container_1/Container/ContainerFill_Image_1/Text/TextFill_Image/未准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text32` | Text | 手部未识别 | `Container_Image/Container/Container/Container/Container_1/Container_1/ContainerFill_Image/Container_1/Inlinecontent/手部未识别_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text33` | Text | 确保环境光线充足，避免手部被遮挡；张开五指，缓慢移入视野中心 | `Container_Image/Container/Container/Container/Container_1/Container_1/ContainerFill_Image/Container_1/Container/Text/确保环境光线充足，避免手部被遮挡；张开五指，缓慢移入视野中心_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text34` | Text | 准备完成 | `Container_Image/Container/Container/Container/Container_1/Container_1/ContainerFill_Image/Text/TextFill_Image/准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text35` | Text | 相机未初始化成功 | `Container_Image/Container/Container/Container/Container_1/Container_1/ContainerFill_Image_1/Container_1/Inlinecontent/相机未初始化成功_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text36` | Text | 等待一段时间，若长时间不能初始化成功，尝试重启Pico后重试 | `Container_Image/Container/Container/Container/Container_1/Container_1/ContainerFill_Image_1/Container_1/Container/Text/等待一段时间，若长时间不能初始化成功，尝试重启Pico后重试_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text37` | Text | 未准备完成 | `Container_Image/Container/Container/Container/Container_1/Container_1/ContainerFill_Image_1/Text/TextFill_Image/未准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text38` | Text | 追踪器 设备连接中… | `Container_Image/Container/Container/Container/Container_1/Container_2/ContainerFill_Image/Container_1/Inlinecontent/追踪器 设备连接中…_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text39` | Text | 检查 追踪器是否开机，蓝灯常亮表示已配对；可尝试重新校准，可尝试重启设备 | `Container_Image/Container/Container/Container/Container_1/Container_2/ContainerFill_Image/Container_1/Container/Text/检查 追踪器是否开机，蓝灯常亮表示已配对；可尝试重新校准，可尝试重启设备_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text40` | Text | 准备完成 | `Container_Image/Container/Container/Container/Container_1/Container_2/ContainerFill_Image/Text/TextFill_Image/准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text41` | Text | 追踪器未全部连接成功… | `Container_Image/Container/Container/Container/Container_1/Container_2/ContainerFill_Image_1/Container_1/Inlinecontent/追踪器未全部连接成功…_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text42` | Text | 检查追踪器 是否开机，是否配对成功；可重新校准，可尝试重启设备 | `Container_Image/Container/Container/Container/Container_1/Container_2/ContainerFill_Image_1/Container_1/Container/Text/检查追踪器 是否开机，是否配对成功；可重新校准，可尝试重启设备_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text43` | Text | 未准备完成 | `Container_Image/Container/Container/Container/Container_1/Container_2/ContainerFill_Image_1/Text/TextFill_Image/未准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text44` | Text | 音频设备就绪 | `Container_Image/Container/Container/Container/Container_1/Container_3/ContainerFill_Image/Container_1/Inlinecontent/音频设备就绪_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text45` | Text | 麦克风已连接，采样率 48kHz，电平正常，可开始采集 | `Container_Image/Container/Container/Container/Container_1/Container_3/ContainerFill_Image/Container_1/Container/Text/麦克风已连接，采样率 48kHz，电平正常，可开始采集_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text46` | Text | 准备完成 | `Container_Image/Container/Container/Container/Container_1/Container_3/ContainerFill_Image/Text/TextFill_Image/准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text47` | Text | 音频设备就绪 | `Container_Image/Container/Container/Container/Container_1/Container_3/ContainerFill_Image_1/Container_1/Inlinecontent/音频设备就绪_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text48` | Text | 麦克风已连接，采样率 48kHz，电平正常，可开始采集 | `Container_Image/Container/Container/Container/Container_1/Container_3/ContainerFill_Image_1/Container_1/Container/Text/麦克风已连接，采样率 48kHz，电平正常，可开始采集_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text49` | Text | 未准备完成 | `Container_Image/Container/Container/Container/Container_1/Container_3/ContainerFill_Image_1/Text/TextFill_Image/未准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `contentText` | Text | "id": 1001,  "package_id": 7,  "title": "厨房取放物体",   "task_description": "...", "quota_per_user": 20,  "quota_per_user_scene": 10,    "teaching_video_url": "http://.../demo.mp4",  "reference_img_urls": ["http://.../1.jpg"],   "initial_state": "...",  "collector_initial_pose": "...", "collection_mode": "free",   "narration_policy": "say_then_do",  "sop_steps": ["步骤1","步骤2"],     "lighting": "...",    "clutter_level": "...",   | `Container_Image/Container/Container/Container/Container_2/Paragraph/content_Text` |  |
| `messageText` | Text | head:0 | `Container_Image/Container/Container/Container/ContainerFill_Image_2/Container/Text/Message_Text` |  |
| `collectStateBtn` | Button | 准备中 | `Container_Image/Container/Container/Container/Container_3/CollectState_Btn` |  |
| `endCollectBtn` | Button | 结束采集 | `Container_Image/Container/Container/Container/Container_3/EndCollect_Btn` |  |
| `text50` | Text | 使用说明 | `Container_Image/Container/Container/Container/ContainerFill_Image_3/Paragraph/使用说明_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text51` | Text | 手柄进视野停约 2 秒 → 双下手柄，双手张开 2~3 秒至状态灯全绿 点「开始准备」保持手柄约 1 秒自动开始；结束后点「保存完成」再选 | `Container_Image/Container/Container/Container/ContainerFill_Image_3/Paragraph_1/手柄进视野停约 2 秒 → 双下手柄，双手张开 2~3 秒至状态灯全绿
点「开始准备」保持手柄约 1_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `returnBtn` | Button | 返回选择任务 | `Container_Image/Container/ContainerFill_Image_1/Return_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
>
> 有 51 个元素 key 为自动兜底（Figma 名非 ASCII）。如需稳定绑定，建议在 Figma 侧改用 `Start`/`User` 之类 ASCII 名（翻译器会补 `_Btn`/`_InputField`）后重发布。
