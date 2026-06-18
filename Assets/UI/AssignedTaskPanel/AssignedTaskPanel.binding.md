# AssignedTaskPanel — UI 绑定描述

- prefab：`Assets/UI/AssignedTaskPanel/AssignedTaskPanel.prefab`
- 依赖：字体 `MiSans Medium SDF`；公共精灵 `ring12`, `round16`；图集 `Assets/UI/AssignedTaskPanel/AssignedTaskPanel.spriteatlas`；needsTmpEssentials = `true`

YC-Ego 据 `key` 绑事件（回退用 `path`），不接触 Figma。

| key | 类型 | 文案 | path | 备注 |
|-----|------|------|------|------|
| `text1` | Text | 我的已领任务 | `Container_Image/Container/我的已领任务_Text` | ⚠ 自动兜底键，建议在 Figma 用稳定 ASCII 名 |
| `countText` | Text | 共3个进行中任务，点击继续/审核 | `Container_Image/Container/Text/Count_Text` |  |
| `itemParent` | List |  | `ItemParent` | 📋 列表：运行时实例化 `TaskItem_Btn.prefab` 到本容器（见末尾说明） |
| `returnBtn` | Button | 返回 | `Container_1/Return_Btn` |  |

> 命名约定：可绑元素名带类型后缀（`_Btn`/`_InputField`/`_Dropdown`/`_Text`），由翻译器自动加，`key` 即由其驼峰化（如 `returnBtn`）。
>
> 有 1 个元素 key 为自动兜底（Figma 名非 ASCII）。如需稳定绑定，建议在 Figma 侧改用 `Start`/`User` 之类 ASCII 名（翻译器会补 `_Btn`/`_InputField`）后重发布。

## 列表（type=List）使用方法（给 YC-Ego）
命名重复的列表项已抽成**独立 prefab**；主 panel 的父容器只剩 LayoutGroup（item 已去掉）。运行时这样填充：

```csharp
// listEl = binding.json 里 type=="List" 的元素
var parent = root.transform.Find(listEl.path);                    // 列表父容器(已带 LayoutGroup)
var itemPrefab = Resources.Load<GameObject>("UI/<Panel>/" + listEl.itemPrefab); // 同目录 <itemPrefab>.prefab
foreach (var data in dataList) {
    var go = Object.Instantiate(itemPrefab, parent);              // LayoutGroup 自动排序、ContentSizeFitter 自动撑开
    // 用 data 填充 go 内部子元素（FindDeep 取其中的文本/按钮）
}
```
- `itemParent`（path `ItemParent`）→ item prefab `TaskItem_Btn.prefab`

> 父容器带 `UIListBinding` 组件（itemPrefab / vertical / spacing），运行时也可直接读它拿配置，无需解析 binding.json。
