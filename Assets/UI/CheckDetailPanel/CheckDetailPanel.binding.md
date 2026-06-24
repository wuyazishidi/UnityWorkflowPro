# CheckDetailPanel — UI 绑定描述

- prefab：`Assets/UI/CheckDetailPanel/CheckDetailPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `round16`, `round7`；图集 `Assets/UI/CheckDetailPanel/CheckDetailPanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `checkDescriptionText` | Text | #1 桌面小工具归入收纳盒 | `Container/Container/Container/Paragraph/CheckDescription_Text` |  |
| `checkDetailText` | Text | "id": 1001,  "package_id": 7,  "title": "厨房取放物体",   "task_description": "...", "quota_per_user": 20,  "quota_per_user_scene": 10,    "teaching_video_url": "http://.../demo.mp4",  "reference_img_urls": ["http://.../1.jpg"],   "initial_state": "...",  "collector_initial_pose": "...", "collection_mode": "free",   "narration_policy": "say_then_do",  "sop_steps": ["步骤1","步骤2"],     "lighting": "...",    "clutter_level": "...",   | `Container/Container/Container/Container/Paragraph/CheckDetail_Text` |  |
| `assignedAndStartTaskBtn` | Button | 返回审核列表 | `Container/Container/Container_1/AssignedAndStartTask_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
