# TaskListPanel — UI 绑定描述

- prefab：`Assets/UI/TaskListPanel/TaskListPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `ring12`, `round12`, `round16`；图集 `Assets/UI/TaskListPanel/TaskListPanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `returnLoginBtn` | Button | 退出登录 | `Container_Image/ReturnLogin_Btn` |  |
| `text1` | Text | 任务列表 | `Container_Image/Container/任务列表_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `userNameText` | Text | （collector01） | `Container_Image/Container/UserName_Text` |  |
| `refreshBtn` | Button | 刷 新 | `Container_Image/Refresh_Btn` |  |
| `countMessageText` | Text | 共 28 个任务，第 1/5 页（点击领取） | `Text/CountMessage_Text` |  |
| `taskItemBtn` | Button | #1 将水倒入水杯 | `Container_1/TaskItem_Btn` |  |
| `taskItemBtn1` | Button | #1 将水倒入水杯 | `Container_1/TaskItem_Btn_1` |  |
| `taskItemBtn2` | Button | #1 将水倒入水杯 | `Container_1/TaskItem_Btn_2` |  |
| `taskItemBtn3` | Button | #1 将水倒入水杯 | `Container_1/TaskItem_Btn_3` |  |
| `taskItemBtn4` | Button | #1 将水倒入水杯 | `Container_1/TaskItem_Btn_4` |  |
| `taskItemBtn5` | Button | #1 将水倒入水杯 | `Container_1/TaskItem_Btn_5` |  |
| `taskItemBtn6` | Button | #1 将水倒入水杯 | `Container_1/TaskItem_Btn_6` |  |
| `taskItemBtn7` | Button | #1 将水倒入水杯 | `Container_1/TaskItem_Btn_7` |  |
| `prePageBtn` | Button | ← 上一页 | `Container_2/PrePage_Btn` |  |
| `pageTipText` | Text | 1 / 5 | `Container_2/Container_Image/PageTip_Text` |  |
| `nextPageBtn` | Button | 下一页 → | `Container_2/NextPage_Btn` |  |
| `assignedTaskBtn` | Button | 我的已领任务 / 恢复未传数据 | `Container_3/AssignedTask_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
>
> 有 1 个元素 key 为自动兜底（Figma 名非 ASCII）。如需稳定绑定，建议在 Figma 侧改用 `Start`/`User` 之类 ASCII 名（翻译器会补 `_Btn`/`_InputField`）后重发布。
