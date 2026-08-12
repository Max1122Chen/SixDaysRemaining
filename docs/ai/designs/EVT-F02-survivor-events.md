# EVT-F02 幸存者特殊事件（SurvivorEventProvider）

## 元信息

- **ID:** `EVT-F02`
- **类型:** `Feature`
- **状态:** `Draft`（待审阅）
- **负责人:** `Max`
- **最后更新：** `2026-08-12`
- **分支：** 继续 `feat/events`（与 F01 同域；未经要求不另开分支）
- **相关：** `EVT-F01`、`SHLT-F02`、`TD-007`（已 Resolved）、`FEATURE_REGISTRY.md`

## TL;DR

在 F01 的同质 `GameEventDef` + 全日 cap=3 之上，落地 **`SurvivorEventProvider`**：  
仅当庇护所内存在对应 **`defId`** 时，才把该幸存者的专属事件投入候选。  
F02 先把 **调度规则、优先级、内容样例、与随机池争额度** 定清；不做四池算法、不做长线剧情编辑器。

---

## 范围

### In

- 实现并注册 `SurvivorEventProvider`
- JSON 支持幸存者专属事件（`requiredSurvivorIds` 已有；可加 `kind` / `survivorDefId` 显式字段若需要）
- 与 `RandomPoolProvider` 的 **争额度 / 优先级** 规则
- 至少覆盖当前 starter + 可入住身份的 **样例事件**（见下「内容最小集」）
- EditMode：有人/无人时候选过滤；额度占用；defId 精确匹配
- 文档 / Registry 更新

### Out

- 四池（腐蚀×人口）分池
- 完整每人多章剧情树 / 对话树
- Flag 运行时完整系统（若某事件需要，可继续用 F01 预留 op，但执行器仍可延后）
- Debug `event.fire`（可选后续）

---

## 现状（F01 已具备）

| 能力 | 状态 |
|------|------|
| `GameEventDef` + fragment + JSON | ✅ |
| `IGameEventProvider` / `RandomPoolProvider` | ✅ |
| `requiredSurvivorIds` 过滤 | ✅（RandomPool 已实现；无旗标系统时 `requiredFlags` 非空则剔除） |
| 三钩子 + 全日 cap=3 | ✅ |
| `SurvivorEventProvider` | ❌ 未实现、未注册 |

---

## Design

### Option A（推荐）

- `SurvivorEventProvider` 只收集：`trigger` 匹配 **且** `requiredSurvivorIds` 全部在住（Alive）  
- 与 Random 池并列注册；**先跑 Survivor provider，再跑 Random**（同一次 `TryPrepareTrigger`）  
- 入队顺序：先按 survivor 事件 `priority`，再填满剩余额度用 random  
- 好处：专属事件优先露脸，且仍受全日 3 上限约束  
- 风险：若专属事件过多，随机事件被挤掉——靠内容量与 priority 控制

### Option B

- 专属事件占用独立额度（例如每天最多 1 条 survivor + 2 条 random）  
- 否决倾向：破坏 F01「全日共享 3」拍板，规则变复杂

### Option C

- 专属事件只挂 `BeforeDepart`，随机只挂 `AfterTriumph`  
- 可作为 **内容约定**，但代码仍应允许任意 trigger，避免写死

**采纳：A + 内容约定（见拍板待确认）。**

---

## 调度细节

### 1) Provider 顺序

```text
GameEventSubsystem.SetProviders([
  SurvivorEventProvider,
  RandomPoolProvider
])
```

`TryPrepareTrigger` 已有逻辑：按 provider 顺序收集，去重 id，直到 `RemainingDailyBudget`。

### 2) 过滤

- `def.Trigger == query.Trigger`
- 每个 `requiredSurvivorIds[i]` 必须在 `OwnedSurvivorDefIds`（非 Dead/Left）
- `requiredSurvivorIds` **为空** → SurvivorProvider **不收录**（避免把纯随机事件再收一遍）
- RandomProvider：**不要求** `requiredSurvivorIds` 为空；若 JSON 给了 required，两边都可收集——应用 **id 去重**（F01 已有）

建议内容规范：

- 随机事件：`requiredSurvivorIds` 省略或 `[]`
- 幸存者事件：至少写一个 `requiredSurvivorIds: ["politician"]`

### 3) 优先级建议

| 类型 | priority 约定 |
|------|----------------|
| 幸存者专属 | `10`～`100`（剧情重要更高） |
| 随机池 | `0`（现状） |

同 priority 时按 `id` 稳定排序（F01 RandomPool 已类似）。

### 4) 时机建议（内容层）

| Trigger | 幸存者事件适合什么 |
|---------|-------------------|
| `BeforeDepart` | 出征前谈话、拦门、请求分配食物等 |
| `AfterTriumph` | 凯旋后反应、争执、庆功 |
| `BeforeDayEnd` | 睡前倾诉、夜半异状（少用） |

F02 **不强制**代码绑定时机；样例可混用。

### 5) 与入住/驱离

- 事件效果仍用 F01 fragment：`TakeInSurvivor` / `ExpelSurvivor`
- 专属事件若要求某人在场，驱离后同日后续 trigger 不再抽到该事件

---

## 内容最小集（建议）

当前身份：`child` / `athlete` / `politician` / `nurse` / `thief`（starter 常为 child+athlete）。

F02 建议先做 **每人 1 条** 可空触发的样例（可放 `events.json` 或 `survivor_events.json`）：

| defId | 建议 title | 建议 trigger |
|-------|------------|--------------|
| `child` | 幼童的噩梦 | BeforeDepart |
| `athlete` | 运动员要加训 | BeforeDepart |
| `politician` | 政治家的提案 | AfterTriumph |
| `nurse` | 护士夜巡 | BeforeDayEnd |
| `thief` | 小贼的小动作 | AfterTriumph |

（文案/数值实现时再填；design 只锁「每人至少可挂 1 条」的骨架。）

**加载布局选项：**

- **A1**：继续单一 `events.json`（简单）  
- **A2**：`events.json` + `survivor_events.json` 合并加载（内容分文件更清晰）

建议 **A2**，loader 扫目录下全部 `*.json` 或显式双文件。

---

## 实现切片

| Slice | 内容 |
|-------|------|
| S1 | `SurvivorEventProvider` + GameInstance 注册顺序 |
| S2 | JSON 拆分/扩展 + 5 条样例 |
| S3 | EditMode：过滤、优先入队、额度 |
| S4 | Play 抽测 + 文档 Done |

---

## 验收清单

- [ ] `SurvivorEventProvider` 已注册且优先于 Random
- [ ] 无对应幸存者时专属事件不出现
- [ ] 有对应幸存者时可入队并消耗全日额度
- [ ] 样例内容覆盖主要 defId
- [ ] EditMode + Play 通过
- [ ] Registry / ACTIVE_WORK / PROGRESS 更新

---

## 待你拍板

1. **争额度**：确认 Option A（survivor 优先占满共享 3）？  
2. **内容文件**：单文件 vs `survivor_events.json` 分文件？  
3. **样例深度**：F02 只做「每人 1 条骨架」，还是要可玩向完整文案数值？  
4. **nurse/thief**：未入住时事件永不触发——是否接受（仅 TakeIn 后才可能见到）？  
5. 审阅通过后是否 **继续在 `feat/events` 实现**（默认是）？
