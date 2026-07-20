# SelectPanel — UI 绑定描述

- prefab：`Assets/UI/SelectPanel/SelectPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `ring11`, `ring12`, `ring6`, `round11`, `round16`, `round6`；图集 `Assets/UI/SelectPanel/SelectPanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `taskIDText` | Text | 任务#2 | `Container_Image/Container/TaskID_Text` |  |
| `taskDescriptionText` | Text | 场景：书房         状态：assigned        已传0段 | `Container_Image/Container/Text/TaskDescription_Text` |  |
| `returnBtn` | Button | 返回 | `Container_1/Return_Btn` |  |
| `taskDetailText` | Text | "id": 1001,  "package_id": 7,  "title": "厨房取放物体",   "task_description": "...", "quota_per_user": 20,  "quota_per_user_scene": 10,    "teaching_video_url": "http://.../demo.mp4",  "reference_img_urls": ["http://.../1.jpg"],   "initial_state": "...",  "collector_initial_pose": "...", "collection_mode": "free",   "narration_policy": "say_then_do",  "sop_steps": ["步骤1","步骤2"],     "lighting": "...",    "clutter_level": "...",   | `ContainerFill_Image/Container/Paragraph/TaskDetail_Text` |  |
| `continueCollectBtn` | Button | 继续采集 | `ContainerFill_Image/Container_1/ContinueCollect_Btn` |  |
| `abandonBtn` | Button | 放弃任务 | `ContainerFill_Image/Container_1/Abandon_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
