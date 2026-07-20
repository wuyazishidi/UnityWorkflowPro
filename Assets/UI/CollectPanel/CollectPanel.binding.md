# CollectPanel — UI 绑定描述

- prefab：`Assets/UI/CollectPanel/CollectPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `ring12`, `ring3`, `ring5`, `ring7`, `round12`, `round3`, `round5`, `round7`；图集 `Assets/UI/CollectPanel/CollectPanel.spriteatlas`；needsTmpEssentials = `true`

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
| `taskIDText` | Text | 任务： | `Container/ContainerFill_Image/Container_1/Text/TaskID_Text` |  |
| `text7` | Text | \| | `Container/ContainerFill_Image/Container_1/Text_1/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `strategyText` | Text | 口述策略：仅当出错 | `Container/ContainerFill_Image/Container_1/Text_2/Strategy_Text` |  |
| `stateText` | Text | 准备中 | `Container/ContainerFill_Image/Container_1/TextFill_Image/State_Text` |  |
| `text8` | Text | \| | `Container/ContainerFill_Image/Container_1/Text_3/|_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text9` | Text | 左手柄 | `Container/Container/ContainerFill_Image/LeftController/Green_ImageFill_Image/左手柄_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text10` | Text | 左手柄 | `Container/Container/ContainerFill_Image/LeftController/Red_ImageFill_Image/左手柄_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text11` | Text | 右手柄 | `Container/Container/ContainerFill_Image/RightController/Green_ImageFill_Image/右手柄_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text12` | Text | 右 手柄 | `Container/Container/ContainerFill_Image/RightController/Red_ImageFill_Image/右 手柄_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text13` | Text | 相机 | `Container/Container/ContainerFill_Image/Camera/Green_ImageFill_Image/相机_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text14` | Text | 相机 | `Container/Container/ContainerFill_Image/Camera/Red_ImageFill_Image/相机_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text15` | Text | 手 | `Container/Container/ContainerFill_Image/Hand/Green_ImageFill_Image/手_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text16` | Text | 手 | `Container/Container/ContainerFill_Image/Hand/Red_ImageFill_Image/手_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text17` | Text | 腕手适配度 | `Container/Container/ContainerFill_Image/Hand_Wrist_MatchRate/Green_ImageFill_Image/腕手适配度_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text18` | Text | 腕手适配度 | `Container/Container/ContainerFill_Image/Hand_Wrist_MatchRate/Red_ImageFill_Image/腕手适配度_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text19` | Text | 追踪器 5/5 | `Container/Container/ContainerFill_Image/Tracker/Green_ImageFill_Image/追踪器 5/5_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text20` | Text | 追踪器5/5 | `Container/Container/ContainerFill_Image/Tracker/Red_ImageFill_Image/追踪器5/5_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text21` | Text | 网络 | `Container/Container/ContainerFill_Image/Wifi/Green_Image/Text/网络_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text22` | Text | 网络 | `Container/Container/ContainerFill_Image/Wifi/Red_Image/Text/网络_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text23` | Text | 音频 | `Container/Container/ContainerFill_Image/Audio/Green_Image/Text/音频_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text24` | Text | 音频 | `Container/Container/ContainerFill_Image/Audio/Red_Image/Text/音频_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `stateMessageText` | Text | 准备中（点「结束采集」取消） | `Container/Container/Container/Container/Container/Paragraph/StateMessage_Text` |  |
| `text25` | Text | 手势识别切换成功 | `Container/Container/Container/Container/Container_1/Hand/Green_HandStateFill_Image/Container_1/Inlinecontent/手势识别切换成功_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text26` | Text | 手势识别切换成功 | `Container/Container/Container/Container/Container_1/Hand/Green_HandStateFill_Image/Container_1/Container/Text/手势识别切换成功_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text27` | Text | 准备完成 | `Container/Container/Container/Container/Container_1/Hand/Green_HandStateFill_Image/Text/TextFill_Image/准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text28` | Text | 手势识别未成功 | `Container/Container/Container/Container/Container_1/Hand/Red_HandStateFill_Image/Container_1/Inlinecontent/手势识别未成功_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text29` | Text | 请将手保持在Pico可视范围内，若长时间未切换，请检查交互方式是否为手柄和手势识别自由切换模式 | `Container/Container/Container/Container/Container_1/Hand/Red_HandStateFill_Image/Container_1/Container/Text/请将手保持在Pico可视范围内，若长时间未切换，请检查交互方式是否为手柄和手势识别自由切换模式_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text30` | Text | 未准备完成 | `Container/Container/Container/Container/Container_1/Hand/Red_HandStateFill_Image/Text/TextFill_Image/未准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text31` | Text | 相机初始化成功 | `Container/Container/Container/Container/Container_1/Camera/Green_CameraStateFill_Image/Container_1/Inlinecontent/相机初始化成功_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text32` | Text | 相机初始化成功，请保持 | `Container/Container/Container/Container/Container_1/Camera/Green_CameraStateFill_Image/Container_1/Container/Text/相机初始化成功，请保持_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text33` | Text | 准备完成 | `Container/Container/Container/Container/Container_1/Camera/Green_CameraStateFill_Image/Text/TextFill_Image/准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text34` | Text | 相机未初始化成功 | `Container/Container/Container/Container/Container_1/Camera/Red_CameraStateFill_Image/Container_1/Inlinecontent/相机未初始化成功_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text35` | Text | 等待一段时间，若长时间不能初始化成功，尝试重启Pico后重试 | `Container/Container/Container/Container/Container_1/Camera/Red_CameraStateFill_Image/Container_1/Container/Text/等待一段时间，若长时间不能初始化成功，尝试重启Pico后重试_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text36` | Text | 未准备完成 | `Container/Container/Container/Container/Container_1/Camera/Red_CameraStateFill_Image/Text/TextFill_Image/未准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text37` | Text | 追踪器识别校准成功 | `Container/Container/Container/Container/Container_1/Tracker/Green_TrackerStateFill_Image/Container_1/Inlinecontent/追踪器识别校准成功_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text38` | Text | 追踪器识别校准成功，请保持 | `Container/Container/Container/Container/Container_1/Tracker/Green_TrackerStateFill_Image/Container_1/Container/Text/追踪器识别校准成功，请保持_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text39` | Text | 准备完成 | `Container/Container/Container/Container/Container_1/Tracker/Green_TrackerStateFill_Image/Text/TextFill_Image/准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text40` | Text | 追踪器未就位 | `Container/Container/Container/Container/Container_1/Tracker/Red_TrackerStateFill_Image/Container/Inlinecontent/追踪器未就位_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text41` | Text | 请检查追踪器是否配对成功，如果配对成功且长时间未就位，请重新校准 | `Container/Container/Container/Container/Container_1/Tracker/Red_TrackerStateFill_Image/Container/Container/Text/请检查追踪器是否配对成功，如果配对成功且长时间未就位，请重新校准_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text42` | Text | 未准备完成 | `Container/Container/Container/Container/Container_1/Tracker/Red_TrackerStateFill_Image/Text/TextFill_Image/未准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text43` | Text | 腕手匹配度达标 | `Container/Container/Container/Container/Container_1/Hand_Wrist_MatchRate/Green_Hand_Wrist_MatchRateFill_Image/Container_1/Inlinecontent/腕手匹配度达标_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text44` | Text | 腕手匹配度符合要求，请保持 | `Container/Container/Container/Container/Container_1/Hand_Wrist_MatchRate/Green_Hand_Wrist_MatchRateFill_Image/Container_1/Container/Text/腕手匹配度符合要求，请保持_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text45` | Text | 准备完成 | `Container/Container/Container/Container/Container_1/Hand_Wrist_MatchRate/Green_Hand_Wrist_MatchRateFill_Image/Text/TextFill_Image/准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text46` | Text | 腕手匹配度未达标 | `Container/Container/Container/Container/Container_1/Hand_Wrist_MatchRate/Red_Hand_Wrist_MatchRateFill_Image/Container/Inlinecontent/腕手匹配度未达标_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text47` | Text | 请保持手在Pico可视范围内，并检查追踪器是否配对成功，如果配对成功且长时间未就位，请重新校准后重试 | `Container/Container/Container/Container/Container_1/Hand_Wrist_MatchRate/Red_Hand_Wrist_MatchRateFill_Image/Container/Container/Text/请保持手在Pico可视范围内，并检查追踪器是否配对成功，如果配对成功且长时间未就位，请重新校准后重试_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text48` | Text | 未准备完成 | `Container/Container/Container/Container/Container_1/Hand_Wrist_MatchRate/Red_Hand_Wrist_MatchRateFill_Image/Text/TextFill_Image/未准备完成_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `contentText` | Text | "id": 1001,  "package_id": 7,  "title": "厨房取放物体",   "task_description": "...", "quota_per_user": 20,  "quota_per_user_scene": 10,    "teaching_video_url": "http://.../demo.mp4",  "reference_img_urls": ["http://.../1.jpg"],   "initial_state": "...",  "collector_initial_pose": "...", "collection_mode": "free",   "narration_policy": "say_then_do",  "sop_steps": ["步骤1","步骤2"],     "lighting": "...",    "clutter_level": "...",   | `Container/Container/Container/Container/Container_2/Paragraph/content_Text` |  |
| `messageText` | Text | head:0 | `Container/Container/Container/Container/ContainerFill_Image_2/Container/Text/Message_Text` |  |
| `collectStateBtn` | Button | 准备中 | `Container/Container/Container/Container/Container_3/CollectState_Btn` |  |
| `endCollectBtn` | Button | 任务结束 | `Container/Container/Container/Container/Container_3/EndCollect_Btn` |  |
| `text49` | Text | 使用说明： | `Container/Container/Container/Container/ContainerFill_Image_3/Paragraph/使用说明：_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `text50` | Text | 放下手柄，手放进视野停约3秒由手柄模式切换成手势模式，等待除手柄指示外全绿，语音说：任务开始，识别后开始采集,进入采集中后根据口述策略进行任务描述;结束前说：任务结束，会弹出选择标签面板，再选相对应的标签，然后重新开始 | `Container/Container/Container/Container/ContainerFill_Image_3/Paragraph_1/手柄进视野停约 2 秒 → 双下手柄，双手张开 2~3 秒至状态灯全绿点「开始准备」保持手柄约 1_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `offlineReturnBtn` | Button | 返回 | `Container/Container_1/OfflineReturn_Btn` |  |
| `returnBtn` | Button | 返回选择任务 | `Container/Container_1/Return_Btn` |  |
| `endRecordBtn` | Button | 任务结束 | `Container/Container_1/EndRecord_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
>
> 有 50 个元素 key 为自动兜底（Figma 名非 ASCII）。如需稳定绑定，建议在 Figma 侧改用 `Start`/`User` 之类 ASCII 名（翻译器会补 `_Btn`/`_InputField`）后重发布。
