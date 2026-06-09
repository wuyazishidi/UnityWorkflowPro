# 003 — 让不懂 Unity 的美术也能编辑 UI（先兼容 A=Figma / B=平面图+AI）

- 状态：草稿（待 Figma 访问凭据后开工 A；B 已具备，待产品化）
- 负责人：Jinwanpeng
- 关联：建立在 `specs/002-ui-from-mockup.md` 的管线之上

## 1. 目标（Why）

美术不开 Unity、不写 JSON、不碰 Hierarchy/Inspector，也能产出并迭代 UI。
最难的解耦已完成（UI 描述 ↔ Unity 引擎分离，管线 headless）。本规约只解决"美术端入口"，**先同时兼容**：
- **A**：美术在 **Figma** 设计 → 自动转契约。
- **B**：美术给一张**平面图 PNG** → 现有"看图→Spec→渲染→对比"AI 闭环。

## 2. 共享契约（A、B 都必须产出这个，是唯一交汇点）

一个面板 = 一个目录，结构固定：
```
Assets/UI/<Panel>/
 ├ <Panel>.json          # UISpec（schema 见 specs/002 / Assets/Scripts/UI/UISpec.cs）—— 唯一交换格式
 ├ Icons/                # 该面板所有精灵（自动转 Sprite + 打独立图集）
 ├ Mockup/（可选）       # 原始效果图，留档/对比
 ├ <Panel>.prefab        # 产物（BuildUIFromSpec 生成）
 └ <Panel>.spriteatlas   # 产物（PackPanelAtlas 生成）
```
- **交换格式 = `UISpec` JSON**。A 和 B 都只需把设计变成这份 JSON + 填好 Icons，下游(`BuildUIFromSpec`→`PackPanelAtlas`→`RenderCanvasToPng`→DoD)全自动、已就绪。
- 根节点用 `anchorPreset:"center"`（固定尺寸居中，见 002 的坑）。
- 坐标：效果图/设计稿像素、左上原点；`referenceWidth/Height` = 面板设计尺寸。

## 3. 组件契约（A、B 共用的"设计套件"映射）

把 Figma 图层 / 平面图里的元素，统一映射到 UISpec 节点类型：

| 设计语义 | UISpec `type` | 约定（Figma 命名 / 标记） |
|---|---|---|
| 容器/分组 | Container | FRAME / GROUP |
| 图片块 | Image | RECTANGLE/FRAME 有图片填充 → 切图进 Icons；纯色填充 → `color` |
| 九宫格 | Image + `imageType:Sliced` + `border` | 图层名后缀 `#9slice(l,t,r,b)` 或组件约定 |
| 文本 | Text | TEXT 节点 → content/fontSize/color/alignment |
| 按钮 | Button | 命名前缀 `btn_` 或组件 = 背景图 + 子文本 |
| 复选框/单选 | Image(勾选态图) | 命名约定 |

> 这份映射是真正的工作量核心，与入口无关。先覆盖上面这几类，够搭大多数面板。

## 4. A：Figma → Spec 导出器

**机制（推荐）：Figma REST API（服务端、可 headless、我可直接驱动测试）**
- 读 `GET https://api.figma.com/v1/files/:key`（带 personal access token）→ 拿到文档节点树。
- walk 节点：用 `absoluteBoundingBox` 算 rect（相对所选 Frame 左上换算）、`fills` 取颜色、`characters/style` 取文本、按命名/填充判定 Image vs 纯色 vs 9-slice。
- 取图：`GET /v1/images/:key?ids=...&format=png` 批量导出需要切图的图层 → 落到 `Icons/`。
- 输出 `<Panel>.json` + `Icons/`，随后复用现有管线。
- 备选：Figma 插件（美术在 Figma 内点导出）——更"美术一键"，但要单独写/分发插件、维护成本高；**先做 REST 版**，需要时再加插件壳。

**需要你提供（才能建+测 A）**：
1. 一个 **Figma personal access token**（只读即可）。
2. 一个**样例 Figma 文件 URL + 要转的 Frame 名**（拿真实数据定映射规则）。
3. 美术可遵守的**轻命名约定**（按上表，迭代中细化）。

## 5. B：平面图 + AI 闭环（产品化）

核心已具备（本仓库已跑通 MainMenu / SettingsForm）。产品化 = 让美术能独立用：
- 约定投递目录：美术把 `mockup.png` 放进 `Assets/UI/<Panel>/Mockup/`，图标放 `Icons/`。
- 一键触发：跑 AI 闭环（看图→写 Spec→`BuildUIFromSpec`→`RenderCanvasToPng`→回传对比图），AI 在环精修到像素接近。
- 回传：把 `Logs/<Panel>_compare.png` 给美术看 → 改图/反馈 → 再来。
- 触发面：可用 文件夹监听 / 一条聊天指令 / 小网页 —— 因为管线本就 headless。

## 6. 里程碑

- **AB0 契约**：锁定本规约的目录/Spec/组件映射（本文件）。
- **AB1 B 产品化**：把现有闭环包装成"投递 mockup → 收对比图"的固定流程 + 一个 scaffold 工具（建面板目录骨架）。
- **AB2 A Figma 导出器**：REST 转换器（拿到 token+样例后），先覆盖 §3 的节点类型，跑通一个真实 Figma Frame → prefab。
- **AB3 收敛**：A、B 产出的 Spec 在同一管线下一致；补 9-slice/按钮/复选框等映射;（可选）Figma 插件壳、网页触发面。

## 7. 备注 / 决策

- 单一交换格式 = `UISpec` JSON，是 A/B 兼容的根本；任何新入口都只需"产出 Spec + Icons"。
- A 更精确(有坐标/文字/样式)、迭代少；B 通用零约定、需 AI 在环。两者互补：A 主路，B 兜底+精修。
