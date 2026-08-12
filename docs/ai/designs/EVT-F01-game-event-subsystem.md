# EVT-F01 GameEventSubsystem + 同质事件模型

## 元信息

- **ID:** `EVT-F01`
- **类型:** `Feature`
- **状态:** `In Progress`（实现于 `feat/events`；待 Play 验收）
- **负责人:** `Max`
- **最后更新：** `2026-08-12`
- **分支：** `feat/events`（基于 `main` @ `52612ba`）
- **相关：** `CORE-F05`、`CORE-F04`、`SHLT-F02`、`COMB-F09`、`TD-007`、`FEATURE_REGISTRY.md`

## TL;DR

把塞在 `AppFlowController` 里的随机事件队列与效果落地，抽成独立域 **`Events/`** 的 **`GameEventSubsystem`**。  
随机事件统一为同质 **`GameEventDef` + fragment 效果**；内容进 **`StreamingAssets/Events/`**；UI 只展示与回传选项。  
F01 接上 **三个时机钩子**（含可空内容），并用 **全日共享上限 3** 约束全天事件总量。幸存者特有调度 / 四池算法留给后续。

---

## 已拍板（2026-08-12）

| # | 议题 | 决定 |
|---|------|------|
| 1 | 触发时机 | 三钩子：`AfterTriumph`、`BeforeDayEnd`、`BeforeDepart`（原误称 PrepStart，见下） |
| 2 | 每日上限 | **全天最多 3 个事件**，跨**全部时机共享**剩余额度 |
| 3 | 代码归属 | 独立 `Assets/Scripts/Events/` + `SixDaysRemaining.Events` |
| 4 | 分支 | `feat/events` |
| 5 | 首版 fragment | `FoodDelta` / `CorruptionDelta` / `TakeInSurvivor` / `ExpelSurvivor` / `JumpToEnding` |
| 6 | 日结顺序 | **先** `BeforeDayEnd` 事件链，**后** `ProcessEndOfDay` |
| 7 | 未实现 op | JSON 出现未落地的 fragment op → **加载硬失败** |

---

## 范围

### In

- 新建 `Assets/Scripts/Events/` + `SixDaysRemaining.Events.asmdef`
- `GameEventSubsystem`：收集、过滤、排队、ApplyOption、Next、广播；维护 **日额度**
- 数据模型：`GameEventDef` / `GameEventOptionDef` / `GameEventEffectFragment`（含四池预留字段）
- `StreamingAssets/Events/` JSON 加载与硬失败校验（对齐 COMB-F08 风格）
- 迁移现有 `RandomEventCatalog` 三则原型事件 → JSON；退役内存 Catalog
- `RandomEventView` → `GameEventView`（仍在 `UI/`）
- `AppFlowController` 瘦身：去掉 `pendingEvents` / 直接改 food·corruption·TakeIn·Expel；改为请求 subsystem + 订阅广播
- `PresentationManager` 委托参数改为 `GameEventDef` / 结果 DTO
- 时机钩子三处接线（见下）
- Edit Mode：加载、额度、fragment、熔断；Play：凯旋事件链 → 日结回归
- 退出 `TD-007`；视情况关闭 `TD-003`

### Out

- 完整四池（腐蚀×人口）分池算法
- `SurvivorEventProvider` 真实调度（**EVT-F02**）
- Flag 运行时系统完整落地（模型可预留 `SetFlag`/`ClearFlag`，F01 可不执行）
- `OverrideHungerDecay` 执行器
- 对话树 / 事件美术管线
- Debug `event.fire` / `event.queue`（可后续挂 CORE-F04）

---

## 现状、目标与差距（相对当前 main）

| | 现状 | 目标 |
|--|------|------|
| 队列 | `AppFlowController.pendingEvents` | `GameEventSubsystem` 拥有 |
| 效果 | `OnRandomEventChosen` 直接调 Gameplay/Shelter | fragment → 统一 Apply |
| 内容 | `RandomEventCatalog` 内存三事件 | `StreamingAssets/Events/*.json` |
| 时机 | 仅凯旋后 `PickSequence(..., 3)` | 三钩子 + **全日共享 cap=3** |
| UI | `RandomEventView` + Presentation 委托 | `GameEventView`；Presentation 只展示 |
| 程序集 | 逻辑散落 App/Gameplay | 独立 `SixDaysRemaining.Events` |

> 注：design 旧稿写「AppFlow 仍在 UI/」已过时。现状：`AppFlowController` 编译在 **App** 程序集、命名空间 `SixDaysRemaining.Gameplay`；展示经 **PresentationManager**。

---

## Design

### Option A（采纳）

- 独立 `Events` 域 + `GameEventSubsystem`
- 同质 def + fragment；Flow 只做时机与编排；Presentation 只做展示
- 好处：消 `TD-007`、可测、可扩 F02/四池
- 风险：多一个 asmdef，需理清引用方向（见下）

### Option B（未选）

- 仅搬队列、保留 `RandomEventOption` 硬编码字段  
- 否决原因：效果字段会继续膨胀，F02 仍要重做模型

---

## 架构与程序集

### 目录

```text
Assets/Scripts/Events/
  SixDaysRemaining.Events.asmdef
  GameEventSubsystem.cs
  GameEventDef.cs          // + Option / Fragment / enums
  IGameEventProvider.cs
  RandomPoolProvider.cs
  Content/
    EventContent.cs
    EventContentJsonLoader.cs
    EventContentDtos.cs

Assets/StreamingAssets/Events/
  events.json              // 或分文件；F01 一种布局即可

Assets/Scripts/UI/GameEventView.cs   // 由 RandomEventView 重命名
```

### 引用方向（禁止环）

```text
Combat
  ↑
Gameplay ← Shelter
  ↑          ↑
  └──── Events ────┘
          ↑
         App  → 持有 GameEventSubsystem，时机钩子
          ↑
     Debug / UI
```

- `Events` 引用：`Gameplay`、`Shelter`（应用 fragment；腐蚀走 `ApplyCorruption`/`ForceEnding`）
- `Events` **不**引用 `App` / `UI` / `Debug`
- `App` / `UI` / `Debug` / Tests 引用 `Events`
- `Gameplay` **不**引用 `Events`（避免与历史 App↔Gameplay 环同类问题）

### 持有关系

- `GameInstance`（或 Flow 绑定后）创建/持有 `GameEventSubsystem`
- 注入：`GameplaySubsystem`、`ShelterManager`、内容库、RNG seed 来源
- 每日开局 / `StartNewGame` / 日推进时 **ResetDailyBudget()**

---

## 设计细节

### 1) 触发时机与全日额度

```text
enum GameEventTrigger
{
  AfterTriumph = 0,   // 凯旋结算之后、日结之前
  BeforeDayEnd = 1,   // 紧挨 ProcessEndOfDay 之前
  BeforeDepart = 2    // 庇护所内、按下「出征」之前（对应 ExpeditionPrep）
}

const int MaxEventsPerDay = 3;
```

#### `BeforeDepart` 是什么（澄清）

旧稿里的 `PrepStart` 名字含糊。按日循环语义，它应对齐：

> **玩家已在庇护所界面、尚未出征** 的窗口（`GameplayPhase.ExpeditionPrep`）。

不是「抽象的一天开始瞬间」，而是 **出发前在庇护所内** 可插事件的时机——例如清晨谈事、分配前插曲、有人拦门等（F01 内容可暂空，钩子必须接上）。

**何时调用 `TryPrepareTrigger(BeforeDepart)`：**

| 场景 | 时机 |
|------|------|
| 新开局 day1 | 故事跳过 / 进入庇护所 UI **之后**、玩家可点出征 **之前**（可先 ShowShelter，再叠事件 overlay） |
| 日结后进入次日 | `AdvancePhase` → `ExpeditionPrep` 后：ShowShelter，再跑 `BeforeDepart`；事件链结束才允许出征交互（或出征按钮在事件结束前禁用） |
| 玩家点出征 | **不再**二次触发；出征走 `OnDepart` |

F01：**允许该 trigger 下挂内容**；若 JSON 暂无 `BeforeDepart` 事件，队列为空即可，但不要把该钩子理解成「仅空壳占位、语义未定」。

#### 额度语义

- 计数单位 = **实际入队并完成结算的事件个数**
- `TryPrepareTrigger(trigger)`：
  1. `remaining = MaxEventsPerDay - eventsConsumedToday`
  2. `remaining <= 0` → 空序列 + `EventSequenceFinished`
  3. 否则按该 trigger 收集候选，排序后最多取 `remaining` 条入队
  4. 每 Resolve 完一个事件，`eventsConsumedToday++`
- **新的一天**（day++ 进入 ExpeditionPrep，或 `StartNewRun`）`ResetDailyBudget()`

#### F01 内容预期

| Trigger | F01 内容 | 说明 |
|---------|----------|------|
| `AfterTriumph` | 迁入现有 3 则随机事件 | 主路径 |
| `BeforeDayEnd` | 可空 | 钩子接线；**先事件，后日结** |
| `BeforeDepart` | 可空，但语义已定 | 庇护所内、出征前；日后可加清晨事件 |

> 若 AfterTriumph 已用满 3，同日 `BeforeDayEnd` / `BeforeDepart` 只能空序列——刻意行为。

#### Flow 接线

```text
── 凯旋后 ──
OnSettlementContinue（入库 / 腐蚀后）
  → TryPrepareTrigger(AfterTriumph)
  → …事件链…
  → EventSequenceFinished
  → TryPrepareTrigger(BeforeDayEnd)
  → …事件链（常空）…
  → EventSequenceFinished
  → Shelter.ProcessEndOfDay + ShowDayEnd     // 先事件，后日结

── 日结继续 / 次日 ──
OnDayEndContinue
  → AdvancePhase（可能 day++ → ExpeditionPrep）
  → 若 Ending：ShowEnding；否则：
  → ResetDailyBudget（新一天时）
  → ShowShelter
  → TryPrepareTrigger(BeforeDepart)          // 庇护所内、出征前
  → …事件链（常空）…
  → 开放出征等庇护所交互

── 开局 ──
故事结束进庇护所
  → ResetDailyBudget
  → ShowShelter
  → TryPrepareTrigger(BeforeDepart)          // day1 同样跑
  → 开放出征
```

### 2) 子系统职责与广播

`GameEventSubsystem`：

- 内容加载（或接受已加载 `IEventLibrary`）
- Provider 收集 + trigger/条件过滤 + priority 排序
- 队列 + 日额度
- `ApplyOption(optionIndex)` → 跑 fragment → 返回 `GameEventResult`（含是否 Ending）
- `ContinueAfterResult()` / `Next()` 推进队列

广播（名称可微调，语义固定）：

```csharp
public event Action<GameEventDef> CurrentEventChanged;
public event Action<IReadOnlyList<GameEventDef>> EventQueuePrepared;
public event Action<GameEventResult> EventResolved;
public event Action EventSequenceFinished; // 含空序列
```

`AppFlowController`：

- 在时机点调用 `TryPrepareTrigger`
- 订阅广播 → 调 Presentation 委托（`ShowGameEventOverlay` 等）
- **不**保存 `pendingEvents`，**不**直接 `AddFood` / `TakeIn` / `Expel`

`PresentationManager` / `GameEventView`：

- 只展示 def / resultText；选项点击 → Flow → `subsystem.ApplyOption`

### 3) 同质事件模型

```text
GameEventDef
  id
  title
  body
  trigger                 // GameEventTrigger
  priority
  requiredSurvivorIds[]   // defId
  requiredFlags[]         // F01 可解析、过滤时若无 flag 系统则忽略或当未满足
  // 四池预留：
  poolId
  corruptionRange         // min/max 或可空
  populationRange
  weight
  options[]

GameEventOptionDef
  id
  label
  resultText
  effects[]               // GameEventEffectFragment

GameEventEffectFragment
  op                      // enum
  amount                  // int/float 按 op
  survivorDefId           // TakeIn / Expel
  flagId                  // 预留
  // …
```

**F01 必须执行的 op：**

| Op | 行为 |
|----|------|
| `FoodDelta` | `Gameplay.AddFood` |
| `CorruptionDelta` | `Gameplay.ApplyCorruption`；熔断 → Ending 信号 |
| `TakeInSurvivor` | `Shelter.TakeIn(defId)` |
| `ExpelSurvivor` | `Shelter.ExpelSurvivor(defId)`（禁止用显示名） |
| `JumpToEnding` | `Gameplay.ForceEnding(...)` + Ending 信号 |

**模型预留、F01 可不执行：** `SetFlag` / `ClearFlag` / `OverrideHungerDecay`  
JSON 若出现未实现 op：**加载期硬失败**（与 COMB-F08 一致，禁止静默丢效果）。

### 4) Provider

```text
IGameEventProvider
  IEnumerable<GameEventDef> Collect(GameEventQuery query);

RandomPoolProvider     // F01
SurvivorEventProvider  // F02 空壳或暂不注册
```

`GameEventQuery`：`trigger`、当前 day/corruption/population、剩余日额度、已入住 defId 集合、flags（可空）。

### 5) 与 defId

- TakeIn / Expel / requiredSurvivor **一律 defId**
- 迁移旧 Catalog 时：`politician` 等保持；错误的 `DriveAwayName: "一名不安分的幸存者"` 改为真实 `defId` 或删掉该效果并改文案

### 6) View

- `RandomEventView` → `GameEventView`（场景引用一并改）
- 仍最多 3 选项按钮（与现 UI 一致）
- 日结摘要若仍挂在同一面板，可保留 `ShowDayEnd`；与事件链分离清晰即可

### 7) 数据流（F01）

```text
AfterTriumph
  -> TryPrepareTrigger(AfterTriumph)  // ≤ remainingDaily
  -> View … ApplyOption … Next
  -> EventSequenceFinished
  -> TryPrepareTrigger(BeforeDayEnd)  // 常空
  -> EventSequenceFinished
  -> ProcessEndOfDay + ShowDayEnd     // 先事件，后日结

… 玩家继续 …
  -> AdvancePhase → ExpeditionPrep（新一天则 ResetDailyBudget）
  -> ShowShelter
  -> TryPrepareTrigger(BeforeDepart)  // 庇护所内、出征前；常空
  -> 开放出征
```

---

## 实现切片

| Slice | 内容 |
|-------|------|
| **S1** | `Events` asmdef + 模型 + `GameEventSubsystem`（队列、额度、广播）；空 Provider 接口 |
| **S2** | JSON loader + 三则事件迁移；退役 `RandomEventCatalog` |
| **S3** | Flow / Presentation / `GameEventView` 接线；三钩子；消 `pendingEvents` |
| **S4** | EditMode（额度跨 trigger、fragment、熔断）+ Play 回归 |
| **S5** | `TD-007` Resolved；`TD-003` 关闭或改写；Registry / ACTIVE_WORK / PROGRESS |

---

## 验证

- Unity Play：`MainScene`（非旧 SampleScene）
- Edit Mode：
  - JSON 加载失败 / 未知 fragment op → 硬报错
  - AfterTriumph 用满 3 后 BeforeDayEnd / BeforeDepart 无法再入队
  - fragment：food / corruption fuse / takein / expel(defId)
- 人工：
  - AppFlow 无直接 food/corruption/takein/expel
  - 凯旋 → 事件 →（空 BeforeDayEnd）→ 日结 → 次日庇护所 →（空 BeforeDepart）→ 可出征
  - asmdef 无环

## 验收清单

- [x] `Assets/Scripts/Events/` + `SixDaysRemaining.Events` 独立存在
- [x] `GameEventSubsystem` 拥有队列；AppFlow 无 `pendingEvents`
- [x] 三钩子已接线（含 `BeforeDepart`）；全日共享 `MaxEventsPerDay = 3`
- [x] 选项效果为 fragment；首版 5 种 op 可用；未知 op 加载硬失败
- [x] `StreamingAssets/Events/` 加载与校验
- [x] `GameEventView` 替换 `RandomEventView`
- [ ] EditMode + Play 回归通过（EditMode 已写；Play 待你验）
- [x] `TD-007` Resolved；进度日志 / Registry 已更新

---

## 二次审阅状态

| # | 项 | 状态 |
|---|-----|------|
| 1 | 先 `BeforeDayEnd`，后 `ProcessEndOfDay` | ✅ |
| 2 | `BeforeDepart` = 庇护所内出征前 | ✅ |
| 3 | 未实现 op → 加载硬失败 | ✅ |
| 4 | 授权实现 | ✅ 已在 `feat/events` 落地 |