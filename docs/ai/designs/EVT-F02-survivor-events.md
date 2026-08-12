# EVT-F02 幸存者特殊事件（SurvivorEventProvider）

## 元信息

- **ID:** `EVT-F02`
- **类型:** `Feature`
- **状态:** `Review`（已实现，待 Play 验收）
- **负责人:** `Max`
- **最后更新：** `2026-08-12`
- **分支：** 继续 `feat/events`（与 F01 同域；未经要求不另开分支）
- **产品源：** `docs/designs/人物模板2.0.pdf`、`docs/designs/六日英雄—技术演示文档2.0.pdf`（突发事件段：住户强制剧情优先）
- **相关：** `EVT-F01`、`SHLT-F02`、`FEATURE_REGISTRY.md`

## TL;DR

在 F01 的同质 `GameEventDef` + **全日共享 cap=3** 之上，落地 **`SurvivorEventProvider`**：  
**当且仅当**对应 `defId` 的幸存者 **在庇护所 roster 内且未 Dead/Left** 时，才把该条专属事件投入候选；**优先于**随机池，仍与其它事件 **共用同一日额度**。  
内容继续放在 **单一 `events.json`**；样例对齐 **人物模板 2.0**（幼童抛石头、政治家敲门入住）。  
长线副作用（禁出征、常驻腐蚀、战败结局 E 等）分 slice 落地，本 design 先锁调度与内容骨架。

---

## 已拍板（2026-08-12）

| # | 议题 | 决定 |
|---|------|------|
| 1 | **日额度** | **所有事件共用** F01 的 `MaxEventsPerDay = 3`（跨 AfterTriumph / BeforeDayEnd / BeforeDepart 共享剩余额度） |
| 2 | **争额度策略** | **Option A**：SurvivorProvider 先收集，RandomPool 填剩余；专属与随机 **不拆池** |
| 3 | **内容文件** | **单文件** `StreamingAssets/Events/events.json`（F02 不拆 `survivor_events.json`） |
| 4 | **触发条件** | 幸存者专属事件 **当且仅当** 所需 `defId` **在庇护所内**（roster 中存在且 `status` 非 `Dead` / `Left`） |
| 5 | **样例来源** | 以 **`docs/designs/人物模板2.0.pdf`** 为准（见下「产品样例映射」） |
| 6 | **实现分支** | 审阅通过后继续在 **`feat/events`** 实现（默认） |
| 7 | **实现深度** | **幼童抛石头整套**（D2/D3 事件、陪玩→次日禁出征、婉拒→赌气/计数、连续拒两次→D4 偷粮离开）纳入 F02 |
| 8 | **天数门控** | F02 扩展 **`requiredDayMin` / `requiredDayMax`**（闭区间）；单日事件 **min=max**；省略两字段表示 **任意天** |
| 9 | **未入住门控** | 扩展 **`requiredAbsentSurvivorIds`**（名单里 **不能** 出现这些 defId） |
| 10 | **占位事件** | **删除** `wanderer_plea`，**替换** 为 PDF 对齐的 `politician_knock_day3` |
| 11 | **幼童常驻 −8** | **本轮不做**（不归 F02；日后 SHLT/被动 feat） |

> F01 `BuildQuery` 已用 `OwnedSurvivorDefIds` 排除 Dead/Left；F02 文档用语「在庇护所内」与此一致。

### 术语说明（审阅 FAQ）

**「未入住」指什么？**  
指该身份的 `defId` **还不在** 庇护所 roster 里——玩家尚未通过 `TakeInSurvivor` 把此人收进庇护所。  
开局 `starter.json` 只有 `child`、`athlete`；**政治家不在开局名单里**，第一次出现应是 **第 3 天** 的敲门事件，选「让他进来」后才入住。

**「政治家 D3、未入住才触发」是什么意思？**  
这不是 Survivor 专属事件，而是 **随机/剧情入住事件**：第 3 天凯旋后弹窗，门外是政治家；只有 **庇护所里还没有 `politician` 这条人** 时才应出现（已收留了就不该再敲门求入住）。  
与幼童相反：幼童事件要求 `child` **已在** 庇护所内；政治家敲门要求 `politician` **还不在** 庇护所内。

**`wanderer_plea` 是什么？**  
EVT-F01 原型阶段写在 `events.json` 里的一条 **占位随机事件**（标题「流浪者求助」），选项「收留他」实际会 `TakeInSurvivor(politician)`。  
文案是流浪者，效果却是收政治家，且 **任意天、凯旋后** 都可能抽到——与 PDF「第 3 天贵气男士敲门、食物 +5 交换」不符。  
对齐产品时应 **删掉 `wanderer_plea`，换成 `politician_knock_day3`**（除非你希望保留为额外随机，会与 PDF 冲突）。

---

## 范围

### In

- 实现并注册 `SurvivorEventProvider`（`requiredSurvivorIds` 非空才收录）
- 与 `RandomPoolProvider` 的 **优先级 + 共享额度** 规则（见 Design）
- 在 **`events.json`** 追加 PDF 对齐的幸存者样例（幼童、政治家相关；见下）
- EditMode：有人/无人过滤；优先入队；额度占用；defId 精确匹配
- 文档 / Registry 更新

### Out（F02 不做或另 feat）

| 项 | 说明 | 归属 |
|----|------|------|
| 四池（腐蚀×人口）分池 | 技术演示文档四套池 | EVT-F03+ |
| 幼童 **常驻** 腐蚀 −8 / 日结「腐蚀 −8」 | 被动，非 popup | **本轮不做** → SHLT/被动 |
| 婉拒后赌气随机文案替换 | 依赖被动/UI | 与常驻同 feat 或延后 |
| 政治家 **战败即结局 E** | 结局钩子 | Ending / Combat |
| 运动员 / 护士 / 小贼专属 popup | PDF 未展开 | 等产品 |
| Debug `event.fire` | — | 可选 |

**F02 In（与上表区分）：** `SetFlag`（幼童拒两次）、陪玩→次日禁出征、删 `wanderer_plea`。

---

## 现状（F01 已具备）

| 能力 | 状态 |
|------|------|
| `GameEventDef` + fragment + JSON | ✅ |
| `IGameEventProvider` / `RandomPoolProvider` | ✅ |
| `requiredSurvivorIds` 过滤（RandomPool 内） | ✅ |
| `OwnedSurvivorDefIds` 排除 Dead/Left | ✅ |
| 三钩子 + 全日 cap=3 | ✅ |
| `SurvivorEventProvider` | ❌ 未实现、未注册 |
| JSON **按 day 过滤** | ❌ 无字段（F02 新增 `requiredDayMin`/`Max`） |
| `requiredAbsentSurvivorIds` | ❌ F02 新增 |
| Flag 执行 | ❌ 预留 op；F02 需落地 **SetFlag** 以支撑幼童拒两次 |

---

## Design

### Provider 架构（Option A — 已采纳）

```text
GameEventSubsystem.SetProviders([
  SurvivorEventProvider,   // 先
  RandomPoolProvider       // 后
])
```

`TryPrepareTrigger`：按 provider 顺序收集 → **id 去重** → 直到 `RemainingDailyBudget` 用尽。

对齐技术演示文档：**先**判定庇护所内住户强制剧情，**再**基本/分池随机事件。

### 触发规则（硬约束）

**幸存者专属事件入候选 ⟺ 同时满足：**

1. `def.Trigger == query.Trigger`
2. `requiredSurvivorIds` **非空**（SurvivorProvider **不收录**空数组 — 避免与 Random 重复扫库）
3. `requiredSurvivorIds` 中 **每一个** `defId` ∈ `query.OwnedSurvivorDefIds`
4. （F01 行为）`OwnedSurvivorDefIds` 仅含 roster 内 **非 Dead、非 Left** 的实例

**推论：**

- 未入住（从未 TakeIn）→ 永不触发
- 已 `Left` / `Dead` → 同日后续 trigger 不再触发
- `nurse` / `thief` 等后期入住身份：只有 TakeIn 之后才可能出现其专属事件（**接受**）

### 与 RandomPool 的分工

| 事件类型 | `requiredSurvivorIds` | 由谁收集 |
|----------|----------------------|----------|
| 纯随机 | 省略或 `[]` | RandomPool |
| 幸存者专属 | 至少一个 defId，如 `["child"]` | SurvivorProvider（Random 也可通过同一 filter，但 **id 去重** 只入队一次） |

**内容规范：** 幸存者专属条目 **必须** 写 `requiredSurvivorIds`；随机条目 **不写** 或写 `[]`。

### 优先级

| 类型 | `priority` 约定 |
|------|----------------|
| 幸存者专属（剧情） | `50`～`100`（幼童 D2/D3 产品写「突发事件里属第一位」→ 建议 `90`+） |
| 随机池 | `0`（现状） |

同 priority 按 `id` 字典序稳定排序（与 F01 RandomPool 一致）。

### 时机（内容层建议）

| Trigger | 用途 |
|---------|------|
| `AfterTriumph` | 凯旋后住户反应；**政治家敲门**（入住前，见下「非 Survivor 条目」） |
| `BeforeDayEnd` | 睡前倾诉、日结前异状 |
| `BeforeDepart` | 出征前谈话、幼童拦门等 |

代码 **不** 写死 defId↔trigger 绑定；上表仅为内容约定。

### 6) 出现条件字段（F01 现状 + F02 扩展）

**F01 已有（`GameEventDef` / JSON）：**

| 字段 | 含义 | 运行时过滤 |
|------|------|------------|
| `trigger` | 三钩子之一（AfterTriumph / BeforeDayEnd / BeforeDepart） | ✅ |
| `requiredSurvivorIds` | 名单里 **必须** 有这些 defId（Alive，非 Dead/Left） | ✅ RandomPool |
| `requiredFlags` | 必须已置位 | ❌ 无 Flag 存储时非空一律剔除 |
| `corruptionMin` / `corruptionMax` | 腐蚀区间 | ❌ F01 占位，**不过滤** |
| `populationMin` / `populationMax` | 人口区间 | ❌ F01 占位，**不过滤** |
| `priority` / `weight` | 排序与随机权重 | ✅（权重仅 Random 洗牌） |

**F01 没有「第几天」字段。** `GameEventQuery.Day` 会传入当前天数，但 def 侧无法声明「仅 D2～D3 出现」。

**F02 新增 — 天数时间区间（主模型）：**

与 `corruptionMin/Max`、`populationMin/Max` 同一套 **闭区间** 语义：

| JSON 字段 | 类型 | 含义 |
|-----------|------|------|
| `requiredDayMin` | int? | 最早出现日（含）；省略 = **无下界** |
| `requiredDayMax` | int? | 最晚出现日（含）；省略 = **无上界** |

**过滤：** `query.Day` 须满足 `dayMin ≤ Day ≤ dayMax`（仅一侧有值时只校验该侧）。

**内容约定：**

| 场景 | JSON 写法 |
|------|-----------|
| 任意天 | 省略 `requiredDayMin` / `requiredDayMax` |
| 仅第 2 天 | `requiredDayMin: 2`, `requiredDayMax: 2` |
| 第 2～4 天 | `requiredDayMin: 2`, `requiredDayMax: 4` |
| 第 3 天及以后 | `requiredDayMin: 3`（`max` 省略） |

Loader 校验：`min ≤ max`；`day` 须 ≥ 1。  
**不采用** 单独的 `requiredDay` 精确字段（避免与区间双轨）；单日一律 **min=max**。

**F02 新增 — 未入住：**

| JSON 字段 | 类型 | 含义 |
|-----------|------|------|
| `requiredAbsentSurvivorIds` | string[] | 名单里 **不能** 出现这些 defId（政治家敲门：`["politician"]`） |

**共享过滤** `EventRequirements.Passes(def, query)`（Survivor + Random 共用）：

```text
requiredSurvivorIds:       每一个 need ∈ OwnedSurvivorDefIds
requiredAbsentSurvivorIds: 每一个 absent ∉ OwnedSurvivorDefIds
requiredDayMin/Max:        Day 落在闭区间内（见上）
requiredFlags:             （F02 起有 Flag 存储后接入）
```

**与 `trigger` 的关系：** `trigger` 仍是「一天内哪个阶段」；`requiredDayMin/Max` 是「哪几天」。二者 **叠加**（例如 D3 的 `AfterTriumph` = 第 3 天凯旋后）。

---

## 产品样例映射（人物模板 2.0）

### 幼童 `child` — **幸存者专属**（F02 样例核心）

**前提：** 开局已在庇护所（starter）；`requiredSurvivorIds: ["child"]`。

| 产品 | Design 映射 |
|------|-------------|
| **D2、D3** 各弹一次「抛石头」 | `child_stone_day2` / `child_stone_day3`；`requiredDayMin/Max` 各为 2、3；`priority: 90` |
| 文案 | 「幼童攥着磨圆的石头……明天能不能陪我玩一会抛石头……」 |
| **选项 1** 留下陪玩 | 即时：腐蚀 −20（产品：当日常驻 −8 翻倍 → 单日 −12，合计 −20）；**长线：** 次日 **禁点大门出征**（Flow 门控，非 fragment） |
| **选项 2** 婉拒外出搜集 | 事件内记录拒选（`SetFlag`）；**常驻 −8 与赌气文案本轮不做** |
| **两次均拒** → **D4** 进庇护所 | 偷走 1 粮、`Left`、腐蚀 +20、不再返场 |

**F02 落地：** 腐蚀 ±、食物、`ExpelSurvivor`/`Left`、`SetFlag`、禁出征 Flow 门控。  
**本轮不做：** 幼童每日常驻腐蚀 −8、婉拒后随机事件文案替换。

### 政治家 `politician` — **入住事件 ≠ SurvivorProvider**

PDF：**第三天** 随机事件 — 敲门、带粮交换、选项入住或回绝。**此时政治家尚未在庇护所**，故 **不是** `requiredSurvivorIds: ["politician"]` 条目。

| 产品 | Design 映射 |
|------|-------------|
| D3 敲门 | `politician_knock_day3`：`requiredDayMin/Max: 3`；`requiredAbsentSurvivorIds: ["politician"]`；选项 1 食物 +5 + `TakeInSurvivor` |
| 初始状态 Hungry / 耐饿 1→濒 3 天 | SHLT-F02 已定义 |
| 任意战斗失败 → 结局 E | **Out** — 非 Event Provider |

> **占位清理（已拍板）：** 删除 F01 占位 `wanderer_plea`，由 `politician_knock_day3` 替代。

### 运动员 / 护士 / 小贼

`人物模板2.0.pdf` **未给出** 与幼童同级的专属突发事件文案。  
F02 **不编造** 占位剧情；待产品补稿后再加 `requiredSurvivorIds` 条目。  
（F01 的 `night_unrest` 驱赶运动员仍为 **随机** 事件，非 PDF 专属。）

---

## 内容草案（`events.json` 追加示意）

以下为审阅用骨架；实现时写入同一 `events.json` 的 `events` 数组。

### 1. 幼童 D2 — `child_stone_day2`

```json
{
  "id": "child_stone_day2",
  "title": "角落里的石子",
  "body": "幼童攥着磨圆的石头蹲在角落，举起石子盯着你，突然说：「明天能不能陪我玩一会抛石头？没人陪我玩，我好孤单……」",
  "trigger": "AfterTriumph",
  "priority": 90,
  "requiredDayMin": 2,
  "requiredDayMax": 2,
  "requiredSurvivorIds": ["child"],
  "options": [
    {
      "id": "stay_play",
      "label": "放下搜集计划，留下来陪他玩耍",
      "resultText": "你答应了他。今天晚些时候，庇护所里难得有了笑声。",
      "effects": [
        { "op": "CorruptionDelta", "amount": -20 }
      ]
    },
    {
      "id": "decline",
      "label": "婉拒，优先外出搜集物资",
      "resultText": "幼童没再说话，把石头攥得更紧了。",
      "effects": []
    }
  ]
}
```

### 2. 幼童 D3 — `child_stone_day3`

同结构，`id: child_stone_day3`，`requiredDayMin: 3`, `requiredDayMax: 3`。  
两次 `decline` 的 **D4 后果**：`child_stone_day4`（`requiredDayMin/Max: 4` + `requiredFlags` 拒两次）或 Flag 计数触发独立事件。

### 3. 政治家 D3 敲门 — `politician_knock_day3`（Random，非 Survivor）

```json
{
  "id": "politician_knock_day3",
  "title": "门外的交换",
  "body": "有一位风尘仆仆但依旧难掩贵气和高傲的男士前来敲门。他带着一些食物作为交换条件，希望可以进入庇护所得到你的帮助。",
  "trigger": "AfterTriumph",
  "priority": 10,
  "requiredDayMin": 3,
  "requiredDayMax": 3,
  "requiredAbsentSurvivorIds": ["politician"],
  "options": [
    {
      "id": "admit",
      "label": "虽然有点被他的势气冒犯到，但让他进来",
      "resultText": "他踏入庇护所，粮食堆上了货架。",
      "effects": [
        { "op": "FoodDelta", "amount": 5 },
        { "op": "TakeInSurvivor", "survivorDefId": "politician" }
      ]
    },
    {
      "id": "refuse",
      "label": "他看起来不是什么好应付的，回绝他的交换请求",
      "resultText": "你关上门。他在门外站了一会儿，转身消失在废墟里。",
      "effects": []
    }
  ]
}
```

---

## 实现缺口与 slice

| 缺口 | F02 处理 |
|------|----------|
| 无 `requiredDayMin/Max` | S2 扩展 schema + 共享 `EventRequirements` |
| 无 `requiredAbsentSurvivorIds` | S2 同上 |
| Flag 未执行 | S4 落地 `SetFlag` + 幼童拒两次 / D4 |
| 禁出征 | S5 Flow 门控（陪玩选项） |
| `wanderer_plea` | S5 **删除**，换 `politician_knock_day3` |
| 幼童常驻 −8 | **Out** 本轮 |
| `combat.skip` / `combat.sweep` 无效 | **TD-008**；EVT-F02 实现前或同会话修复 |

### 实现切片

| Slice | 内容 |
|-------|------|
| **S1** | `SurvivorEventProvider` + GameInstance 注册顺序 |
| **S2** | `requiredDayMin/Max` + `requiredAbsentSurvivorIds`；`EventRequirements`；`events.json` 幼童/政治家条目 |
| **S3** | EditMode：在/不在庇护所、absent、day、优先入队、共享额度 |
| **S4** | `SetFlag` 执行器 + 幼童拒选计数 + D4 `child_stone_day4` |
| **S5** | 陪玩 → 次日禁出征；删 `wanderer_plea`；Play 回归 |
| **S6** | Registry / ACTIVE_WORK / PROGRESS Done |

---

## 验收清单

- [x] `SurvivorEventProvider` 已注册且 **先于** RandomPool
- [x] `requiredSurvivorIds` 非空才进 SurvivorProvider
- [x] **仅** 所需 defId 在庇护所内（非 Dead/Left）时入候选
- [x] 与随机事件 **共用** 全日 3 额度；survivor 高 priority 时先占坑
- [x] `requiredDayMin/Max`、`requiredAbsentSurvivorIds` 过滤生效
- [x] `wanderer_plea` 已删除；`politician_knock_day3` 已入 `events.json`
- [ ] 幼童抛石头线（D2/D3/D4/禁出征/拒选）Play 验证；**不含**常驻 −8
- [ ] EditMode + Play 通过
- [ ] Registry / ACTIVE_WORK / PROGRESS 更新

---

## 审阅说明

**状态：** 代码已实现；请在 Unity 中按下方 Play 验收项手测。
