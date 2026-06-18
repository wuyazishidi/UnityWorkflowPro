# Figma → Unity Prefab 同步管线：完整分步与数据模型

> 本文逐步拆解「一张 Figma 设计图变成 Unity prefab」的全过程,标明**每一步产生哪些数据**、
> 各数据是**必须/缓存/临时**。基于代码实测(file:line),是本项目数据流的权威说明。

## 0. 一句话模型

**唯一真相在 Figma 云端**。本地一切都是从它派生的:一对指针(`fileKey`+`node`)→ 实时 API 拉取 →
翻译成 spec(`<Panel>.json`)→ 构建成 prefab。判断"本地是否最新"= 比对 `lastModified` 时间戳。

```
Figma 云端设计 (真源,不在仓库)
  │  fileKey + node  ← 唯一源指针,记录在 figma/<Panel>.meta.json
  ▼
[阶段一 PULL]  figma_sync.py   (不需要 Unity)
  ▼  产出: <Panel>.json(spec) + Icons/*.png + 快照 + truth.png
[阶段二 BUILD] ui-build-render.ps1  (需要 Unity)
  ▼  产出: <Panel>.prefab + <Panel>.spriteatlas
[交付 可选]    publish-ui.ps1
     产出: <Panel>.binding.json/.md + delivery/<Panel>.unitypackage
```

## 1. 编排层级(谁调谁)

| 层 | 脚本 | 职责 |
|---|---|---|
| 入口 | `figma.ps1` | 解析 URL→fileKey+node、设代理、调同步、回填 RECOVERY 索引 |
| 编排 | `figma-sync.ps1` | 串起 PULL + BUILD 两阶段 |
| 拉取 | `figma-pull.ps1` → `scripts/figma_sync.py` | 阶段一全部(薄封装 + Python 核心) |
| 构建 | `ui-build-render.ps1` | 阶段二(Refresh + 图集 + 构建 + 核对) |
| 交付 | `publish-ui.ps1` | 绑定描述 + 打包(非同步必经) |

- `figma.ps1:37-40` 从 URL 正则取 `fileKey`(`/design/([A-Za-z0-9]+)`)和 `node`(`node-id=...`)。
- `figma.ps1:63` 调 `figma-sync.ps1`;`:67` 调 `figma_index.py` 重写 `RECOVERY.md`。

## 2. 阶段一 PULL —— `scripts/figma_sync.py`(不需要 Unity)

| 步 | 操作 | 代码 | 产生的数据 | 类别 |
|---|---|---|---|---|
| 1.1 | **拉节点树**:`GET /files/{key}/nodes?ids={node}` | `figma_sync.py:237` | 内存 Figma 原始树 `doc` | — |
| 1.2 | 收集要导出的图(bg/icon/art/corner)+ 稳定命名 | `:264-296` | 命名表 | — |
| 1.3 | **导图**:`GET /images` 拿 URL → curl 下载;背景降采样+圆角 alpha;**同像素去重** | `:298-326` | `Assets/UI/<Panel>/Icons/*.png` | **必须** |
| 1.4 | **合成真值图**:渲染卡片节点 scale=2 | `:328-333` | `Assets/UI/<Panel>/.figma/truth.png` | 临时 |
| 1.5 | **翻译树→spec**:实色→Image、TEXT→Text、命名后缀→Button/InputField/Dropdown、fill+stroke→描边环;保留层级;字号归一化/字间距/换行后处理 | `build_node` `:335-730` | **`Assets/UI/<Panel>/<Panel>.json`(spec)** | **必须(核心真相)** |
| 1.6 | 清理孤儿图标(spec 不再引用的旧 PNG) | `:708+` | (删除) | — |
| 1.7 | 版式报告 | | `.figma/layout.txt` | 临时 |
| 1.8 | **写来源快照** `save_source_snapshot` | `:177-202` | `figma/<Panel>.nodes.json` + `figma/<Panel>.meta.json` | 缓存 |

**关键**:数据来自**实时 API**(`:237` 的 `get_json`→`curl`→api.figma.com),**不读**本地 `nodes.json`。
`nodes.json` 是只写不读的备份(`:200-201` 以 `"w"` 模式写)。

## 3. 阶段二 BUILD —— `ui-build-render.ps1`(需要 Unity)

| 步 | 操作 | 代码 | 产生的数据 | 类别 |
|---|---|---|---|---|
| 2.1 | **Refresh** ×3:导入新 PNG 为 Sprite | `:53-58` | (Unity 资产库刷新) | — |
| 2.2 | **打图集**:`PackPanelAtlas(Icons/)` | `:62-74` | `Assets/UI/<Panel>/<Panel>.spriteatlas` | **必须** |
| 2.3 | **构建 prefab**:`BuildUIFromSpec(spec, prefab)` 读 `<Panel>.json`(`UIBuilder.cs:39` `File.ReadAllText`)→ 建 GameObject 树 → `SaveAsPrefabAsset` | `:76` | **`Assets/UI/<Panel>/<Panel>.prefab`** | **必须(最终产物)** |
| 2.4 | **渲染核对**(仅 `-Verify`):`RenderCanvasToPng` + `ui_diff.py` 算 MAE | `:80-89` | `Assets/UI/<Panel>/_render.png` | 临时 |

- 成功判定靠**产物文件刷新时间戳**(`:40,44`),不解析 stdout —— 每步重试到产物刷新或达上限。
- **常态只构建不渲染**(`-Verify` 才出 `_render.png` + MAE)。

## 4. 交付层(可选)—— `publish-ui.ps1`

| 步 | 操作 | 产生的数据 | 类别 |
|---|---|---|---|
| 3.1 | (可选 `-Build`)重建 prefab | — | |
| 3.2 | `ExportBindingDescriptor` 走真实 prefab 树 | `<Panel>.binding.json` + `.binding.md` | **必须(交付契约)** |
| 3.3 | `ExportUiPackage` 连 .meta 打包 | `delivery/<Panel>.unitypackage` | 交付物 |

## 5. 数据总分类

### 必须(源/产物,入库,缺了管线断)
- **`figma/<Panel>.meta.json`** — 源指针 `fileKey`+`node`(决定 panel 来自哪个设计)+ `lastModified`(判最新的依据)
- **`Assets/UI/<Panel>/<Panel>.json`** — spec,Figma 的忠实投影,**直接构建 prefab 的真相**
- `Assets/UI/<Panel>/<Panel>.prefab` — 最终产物
- `Assets/UI/<Panel>/Icons/*.png`、`<Panel>.spriteatlas` — 精灵与图集
- `Assets/UI/<Panel>/<Panel>.binding.json` / `.binding.md` — 给消费方(YC-Ego)的绑定契约
- 共享:`Assets/UI/Common/*`(圆角/描边精灵)、`Assets/Fonts/MiSans*`(中文字体)

### 缓存(可由重新同步再生,入库但属备份)
- `figma/<Panel>.nodes.json` — Figma 原始树快照(只写不读;Figma 被清也能离线看设计)
- `figma/RECOVERY.md` — 人读来源索引(`figma_index.py` 从 meta.json 自动生成)

### 临时(gitignore,每次重建,可随意删)
- `Assets/UI/<Panel>/.figma/truth.png`、`layout.txt`
- `Assets/UI/<Panel>/_render.png`
- (`.gitignore`:`Assets/UI/**/.figma/`、`Assets/UI/**/_render.png`)

### 不在仓库
- Figma 云端的实时设计 = 唯一真源。仓库里全是它的派生物。

## 6. 判断"最新设计是否已同步进来"

**数据指标 = `lastModified`**(整个 Figma 文件的最后修改时间):
- `figma/<Panel>.meta.json` 的 `lastModified` = **我们上次同步时**该文件的修改时间(`figma_sync.py:190` 写入,取自 API 的 `data.get("lastModified")`)。
- Figma 当前 `lastModified` = `GET /files/{key}?depth=1` 实时返回。
- **相等 → 本地已是最新;Figma 更新 → 没同步进来,需重新同步。**

### ⚠️ 缓存窗口陷阱(实测)
Figma 刚改完文件后,`/nodes` 端点有**几分钟~几十分钟 CDN 缓存延迟**:此期间同步会拿到**旧版本**
(`meta.lastModified` 仍是旧值)。判断缓存是否刷新:`GET /files/{key}/nodes?ids=...` 的 `lastModified`
是否已追上 `/files?depth=1`。追上后再同步才拿得到最新。**Figma 改完别马上同步**。

## 7. 同步/恢复命令

```powershell
# 同步某面板(来源 fileKey+node 见 figma/RECOVERY.md 表)
.\Packages\cn.etetet.yiuimcp\Config\figma.ps1 -Node <node> -Panel <Panel> -FileKey <key>   # +-NoVerify 跳核对图
# 只重建 prefab(不打 Figma API,应用 builder 改动)
.\Packages\cn.etetet.yiuimcp\Config\ui-build-render.ps1 -Spec Assets/UI/<P>/<P>.json -Prefab Assets/UI/<P>/<P>.prefab
# 发现某文件当前顶层帧(拿新 node-id):见 figma/RECOVERY.md §3
```
