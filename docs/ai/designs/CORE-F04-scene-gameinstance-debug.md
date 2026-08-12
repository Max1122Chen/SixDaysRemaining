# CORE-F04 Scene-owned GameInstance + Hybrid Debug

## 元信息

- **ID:** `CORE-F04`
- **类型:** `Refactor`
- **状态:** `Done`（2026-08-12 文档收口；实现已合 `main`）
- **负责人:** `Max`
- **最后更新：** `2026-08-12`
- **分支：** 当前在 `main` 上继续（原建议名 `feat/core-debug` 仅作备选）
- **相关：** `CORE-F03`、`COMB-F09`、`EVT-F01`、`FEATURE_REGISTRY.md`

## TL;DR

把运行入口从“Scene 预置 + 缺失时代码补建”的混合模式，收敛成 **Scene 预置 `GameInstance`** 的单一路径。  
同时引入一套 **Hybrid Debug**：Inspector 常驻参数 + 游戏内 `~` 控制台，支持命令输入、Tab 补全、候选下拉、即时回显。  
目标是让调试能力脱离临时脚本入口，成为系统级基础设施，方便后续重构事件、战斗和庇护所。

---

## 范围

### In

- `GameInstance` 必须由 Scene 预置；移除 `UiSceneBootstrap` 对 `GameObject("GameInstance")` 的兜底创建
- Debug 参数 Inspector 化：起始腐蚀、跳过战斗、玩家无敌、饥饿流失倍率等
- 游戏内 `~` 控制台：输入框、命令执行、日志回显、Tab 补全、候选列表向上预览
- Debug 命令注册表与执行上下文（读取 `GameInstance` / `GameplaySubsystem` / `ShelterManager` / `CombatManager`）
- 让 `AppFlowController` / `UiSceneBootstrap` / HUD 能安全感知 Debug 模式，但不直接保存 Debug 逻辑
- 明确 `AppFlowController` 暂保留在 `UI/` 目录；但必须剥离事件/战斗结算/腐蚀写入等业务逻辑，仅保留屏幕路由与输入转发
- **命令上下文门禁**（按是否开局 / 阶段过滤执行与候选）
- **补齐首批战斗/庇护所调试命令**，并真正接通 `skipCombat` / `playerInvincible`

### Out

- 正式存档系统
- 远程控制台 / 网络调试
- 第三方命令框架
- 发布版作弊保护（首版只做开发态入口）
- `event.*` 命令（留给 `EVT-F01` 后再接）

---

## 实现进度（截至 2026-08-11）

### 已完成

- [x] Scene 预置 `GameInstance`；`UiSceneBootstrap` 缺引用时硬失败
- [x] 目录收敛：`App/`（`GameInstance` / `DebugRunSettings`）+ `Debug/`（控制台 / registry / context）
- [x] `~` 控制台场景手搭（`DebugConsoleRoot` + `Window`），Inspector 绑定引用
- [x] 基础命令：`debug.help`、`run.corruption/day/food/phase`、`shelter.hungerDecay`
- [x] `startCorruption` / `hungerDecayOverride` / `enableConsole` 已接线

### 未完成（见下方「后续需求」）

- [ ] 命令上下文门禁 + 候选过滤
- [ ] `skipCombat` / `playerInvincible` 真正生效
- [ ] 扩展命令集（combat / shelter / debug.status）
- [ ] 关掉或废弃 runtime `BuildUi` 兜底（场景手搭已成为主路径）
- [ ] 文档 / Feature 状态收口

---

## 现状、目标与差距

- **当前行为：** `GameInstance` 已场景化；控制台可用，但命令少，且主菜单也可改腐蚀/食物/天数（无意义）。
- **目标行为：** Scene 中有且仅有一个可见的 `GameInstance`；Debug 选项在 Inspector 与控制台统一生效；命令按上下文门禁执行。
- **差距：** 门禁、跳战斗/无敌接线、命令扩容、验收文档同步。

---

## Design

### Option A（recommended）

- 描述：
  - 保留 `GameInstance` 作为 `MonoBehaviour` 真相源，但改为 **必须场景预置**
  - 新增 `DebugRunSettings`（可序列化类）作为 Inspector 配置容器
  - 新增 `DebugCommandConsole` / `DebugCommandRegistry` / `DebugCommandContext`
  - `AppFlowController` 保留在 `UI/`，仅保留“屏幕路由 + 输入转发”职责；事件与腐蚀写入走 subsystem/网关
  - 由 `UiSceneBootstrap` 负责“绑定现有对象”，不再负责“创建核心对象”
  - 命令注册时附带 `DebugCommandGate`；执行前校验，候选列表同步过滤
- 好处：
  - 调试参数一眼可见，可直接保存在场景
  - 控制台与 Inspector 共用一套状态，不会出现两套 debug 入口打架
  - 对后续 `GameEventSubsystem`、战斗规则调试最友好
- 风险：
  - 场景装配要求提高；空场景或缺引用会比现在更早失败

### Option B

- 描述：保留自动创建 `GameInstance`，只是在其上新增更多 Debug 字段和控制台
- 为什么没选：
  - 继续保留“启动路径不唯一”的问题
  - 你希望直接在 Scene 中调参数，这和 Option B 的代码兜底本质冲突

---

## 设计细节

### 1) 启动所有权

调整后（已落地）：
- `GameInstance` 必须预置在 Scene（`Assets/Scripts/App/`）
- `UiSceneBootstrap` 只允许绑定现有对象；缺 `GameInstance` 硬失败
- `AppFlowController` 暂保留在 `Assets/Scripts/UI/`
- Debug 控制台挂在 `Canvas/DebugConsoleRoot`，`panel` 必须是子物体 `Window`

### 2) Debug 数据面

```csharp
[Serializable]
public class DebugRunSettings
{
    public int startCorruption;
    public bool playerInvincible;
    public bool skipCombat;
    public int hungerDecayOverride;
    public bool enableConsole = true;
}
```

说明：
- `hungerDecayOverride <= 0` 视为“不覆盖默认规则”（已接线）
- `skipCombat`：**待接线**；点击出征后直接产出一份可配置结算结果，不进 `CombatView`
- `playerInvincible`：**待接线**；经战斗层显式分支，不偷偷改 HP

### 3) 控制台 UI（已落地约定）

- `~` 呼出/隐藏；Enter 执行；Esc 关闭；Tab 补全
- 场景手搭：`DebugConsoleRoot`（脚本常驻 Active）→ `Window`（`panel`，可关）
- InputField 必须用 **UI → TextMeshPro - Input Field** 创建（含 Text Area / Text / Placeholder）
- 深色底上 Text / Caret 用浅色，避免“打了字看不见”

### 4) 命令上下文对象

命令实现不直接静态抓全局，统一从 `DebugCommandContext` 取：
- `GameInstance`
- `GameplaySubsystem`
- `ShelterManager`
- `CombatManager`
- `ShowEnding` / `RefreshPresentation` 回调（避免 Debug 程序集直接依赖 UI 类型）

---

## 后续需求（已审批方向 — 2026-08-11）

> Max 拍板：主菜单**仅** `debug.help`；局内扩命令 + 上下文门禁。实现顺序仍建议 S1 → S2 → S3 → S4。

### 已拍板

| # | 决策 |
|---|------|
| 1 | **主菜单**：只允许 `debug.help`（及控制台开关本身）；其余命令一律 gate 拒绝 |
| 2 | **局内 status**：`debug.status` 仅在已开局时可用 |
| 3 | **局内 run**：支持设置腐蚀、食物（及现有 day 等，均须 `RunActive`） |
| 4 | **Shelter**：加人、弄走人、直接改某人饱食度 |
| 5 | **Combat**：无敌；按 `EffectOp` + 参数注入效果；直接赢/输；**扫荡模式**（永远赢得战斗） |
| 6 | **Flow**：可强行触发结局；可推进时间 |

### S1 — 命令上下文门禁

**动机：** 主菜单改腐蚀/食物无意义；Tab 候选应只显示当前能用的命令。

**Gate 定义：**

| Gate | 含义 | 判定 |
|------|------|------|
| `MenuOnly` | 主菜单 / 未开局 | 无有效 `GameState` 或未 `StartNewRun` |
| `RunActive` | 已开局且非 Ending | 已开局 ∧ phase ≠ Ending |
| `InShelter` | 庇护所可操作 | `RunActive` ∧ phase ∈ {ExpeditionPrep, TriumphReturn} ∧ 当前屏为 Shelter 相关（或仅看 phase，首版可只看 phase） |
| `InCombat` | 战斗中 | phase == Combat 且 CombatManager 局内活跃 |
| `NotMenu` | 非主菜单 | 已开局（与 `RunActive` 类似，Ending 单独处理） |

**主菜单白名单：** 仅 `debug.help`。

**行为：**
- 执行前校验 gate；失败回显：`当前在主菜单，仅支持 debug.help`
- `GetSuggestions` / Tab **只列出当前 gate 允许的命令**
- gate 真相源：优先 `Gameplay.State` + `CurrentPhase`；必要时 flow 注入只读 `IsRunStarted` / `IsOnMainMenu` 回调

---

### S2 — Debug 运行标志（Inspector + 命令共用）

扩展 `DebugRunSettings`（或同级 runtime 标志，Inspector 可设默认值）：

| 标志 | 含义 | 命令 |
|------|------|------|
| `playerInvincible` | 战中玩家不受致死/扣血（显式分支） | `combat.invincible on\|off` |
| `skipCombat` | 出征不进战斗界面，直接结算 | `combat.skip on\|off` |
| `combatSweep` | **扫荡模式**：一旦进入战斗逻辑，永远判胜（与 skip 不同：仍进 CombatView 或仍走 CombatManager，但 outcome 强制 Win） | `combat.sweep on\|off` |

**待实现时区分：**
- `skipCombat`：在 **出征入口** 拦截，不进 `CombatView`
- `combatSweep`：在 **CombatManager 结算** 拦截，强制 `CombatOutcome.Win`
- 二者可同时存在；扫荡更适合测多轮战斗 UI

跳战斗默认结算：**固定胜利 + 配置食物奖励**（常量或 Inspector 字段 `skipCombatFoodGained`），不做 mock 编辑器。

---

### S3 — 命令表（审批版）

命名空间不变：`debug.*` / `run.*` / `shelter.*` / `combat.*`

#### debug.*

| 命令 | Gate | 说明 |
|------|------|------|
| `debug.help [prefix]` | 主菜单 + 局内 | 主菜单只列 help；局内列当前 gate 可用命令 |
| `debug.status` | `RunActive` | 一行摘要：day / phase / food / corruption / population；可选第二段列出幸存者 name/defId/hunger/status |

#### run.*

| 命令 | Gate | 说明 |
|------|------|------|
| `run.corruption set <n>` | `RunActive` | 走 `Gameplay.SetCorruption`；100 时触发结局流 |
| `run.food set <n>` | `RunActive` | 直接设存量（新增；比仅 `add` 更符合调试） |
| `run.food add <n>` | `RunActive` | 已有 |
| `run.day set <n>` | `RunActive` | 已有；clamp 1..MaxDay |
| `run.day advance` | `RunActive` | **推进时间**：调用 `Gameplay.AdvancePhase()` 一次；若从 TriumphReturn 推进则含 day++ 与日结前逻辑边界需与 flow 对齐（见下方「推进时间」） |
| `run.day end` | `InShelter` | **强行日结**：`Shelter.ProcessEndOfDay()` + 刷新 UI；不自动 day++（或可选参数 `--advance`） |
| `run.ending force` | `RunActive` | **强行结局**：phase→Ending 或 corruption→100 + 调 `ShowEnding` |
| `run.phase set <...>` | **首版移除或 RunActive 且隐藏** | 主菜单不可用；低优先级，易破坏 flow，建议后续只做 `advance` 不做任意 set |

#### shelter.*

| 命令 | Gate | 说明 |
|------|------|------|
| `shelter.list` | `RunActive` | 列出幸存者：`defId` / `name` / `hunger` / `status`（调试 targeting） |
| `shelter.takein <defId>` | `InShelter` | 走 `TakeIn` |
| `shelter.expel <name\|defId>` | `InShelter` | 走 `Expel`（弄走，Left 状态） |
| `shelter.hunger add <target> <delta>` | `InShelter` | 改单人饱食度；target 匹配 name 或 defId；delta 可为负 |
| `shelter.hunger set <target> <n>` | `InShelter` | 直接设饱食度并 `UpdateSurvivorStatus` |
| `shelter.hungerDecay set <n>` | `RunActive` | 已有；改 `DailyHungerDecay` |

> **说明：** 「弄走」首版用 `expel`；若需「直接死亡」可后续加 `shelter.kill <target>`（设 Dead + personnelChanges）。

#### combat.*

| 命令 | Gate | 说明 |
|------|------|------|
| `combat.invincible on\|off` | `RunActive` | 改 `DebugRunSettings.playerInvincible` |
| `combat.skip on\|off` | `RunActive` | 改 `skipCombat` |
| `combat.sweep on\|off` | `RunActive` | 改 `combatSweep` |
| `combat.win` | `InCombat` | 立即 `Finish(Win, ...)` |
| `combat.lose` | `InCombat` | 立即 `Finish(Lose, ...)` |
| `combat.effect apply <Op> <amount> [target]` | `InCombat` | 构造 `EffectSpec`，经 `CombatEffectExecutor` 对 player/enemy 执行；`Op` 为枚举名（Tab 可补全 `DealDamage` 等）；`target` 默认 `Enemy`（对玩家施放时用 `Self`/`Enemy` 与现有 `EffectTarget` 一致） |

**EffectOp 首版支持：** `DealDamage`, `GainBlock`, `Heal`, `Draw`, `AddCorruption`, `RemoveCorruption`, `DealDamagePlusAttackCount`, `GainBlockRandom`（与 `CardDef.EffectOp` 同步）。

**参数示例：**
```
combat.effect apply DealDamage 12 Enemy
combat.effect apply Heal 5 Self
combat.effect apply AddCorruption 8 Self
```

---

### S4 — 「推进时间」语义（实现注意）

现有 `Gameplay.AdvancePhase()` 只做 **阶段状态机**（Prep→Combat→Triumph→day++→Prep），**不含** UI 切屏、随机事件、`ProcessEndOfDay`。

建议拆两条命令，避免一个命令干太多：

| 命令 | 行为 |
|------|------|
| `run.day advance` | 仅 `AdvancePhase()` + `RefreshPresentation`；从 TriumphReturn 推进时 day++ |
| `run.day end` | 在庇护所：执行 `ProcessEndOfDay()` + 弹出/刷新日结 UI（需 flow 回调 `ShowDayEnd` 或等价）；玩家仍点继续才 `AdvancePhase` |

若 Max 希望 **一条命令跳整天**：可再加 `run.day skip`（end + advance + 切屏），作为后续 slice。

---

### S5 — 实现建议（文档采纳项）

以下为实现时建议一并考虑（非必须首版全做）：

1. **`shelter.list`**：改 hunger/expel 前几乎必需，否则记不住 defId/name。
2. **`run.food set`**：与 `add` 成对，调资源更方便。
3. **目标解析**：shelter 命令统一 `target` = 精确 defId 优先，否则子串匹配 name；匹配失败返回可用列表。
4. **效果注入**：`combat.effect apply` 走现有 `CombatEffectExecutor` + 当前 `CombatResolveContext`，不另写伤害公式；第二参数 `AmountSecondary` 首版可省略或第 4 个可选参数。
5. **结算后刷新**：凡改 state 的命令统一调 `RefreshPresentation`；腐蚀满/强制结局调 `ShowEnding`。
6. **扫荡 vs 跳过**：文档与 Inspector 分两个 checkbox，避免语义混淆。
7. **`run.phase set` 从候选移除**：任意跳 phase 易卡 UI；用 `advance` / `day end` / `ending force` 代替。
8. **Edit Mode 测试**：gate 拒绝文案、effect 解析、takein/expel/hunger 参数；Play 测 sweep/skip/invincible。
9. **EVT 后再加**：`event.fire` / `event.queue`；`content.reload` 有热更管线再做。

---

### S6 — 收口与清理

- 场景手搭为唯一路径；`allowRuntimeBuildFallback` 保持 `false`
- 更新 `ACTIVE_WORK` / `FEATURE_REGISTRY` / `PROGRESS_LOG`
- CORE-F04 验收 → commit → 合 `main` → 开 `EVT-F01`

---

## AppFlowController 职责分析（2026-08-11）

### 设计意图（类注释）

> 「阶段面板路由与开战/结算胶水。UI 视图只调用这里的公开方法。」

即：**View 的事件入口 + 屏幕切换**，不应持有业务规则。

### 当前实际承担的 5 类职责

| 类别 | 方法 / 状态 | 是否应在 Flow |
|------|-------------|---------------|
| **① 纯 UI 路由** | `ShowStart/Shelter/Combat/Ending`、`SwitchScreen`、`ShowOverlay`、`CloseOverlay`、HUD 显隐 | **✅ 应该留** |
| **② 视图胶水** | 各 View 的 `Wire(flow)` 把按钮绑到 `OnXxx` | **✅ 应该留**（或将来换成事件总线） |
| **③ 日循环编排** | `OnDepart`（组 `CombatStartConfig` + `StartCombat`）、`OnCombatFinished`、`OnSettlementContinue`、`OnDayEndContinue` | **⚠️ 应下沉** → `RunFlowService` |
| **④ 随机事件调度** | `pendingEvents`、`RandomEventCatalog.PickSequence`、`ShowNextRandomEvent`、`OnRandomEventChosen` | **❌ 应迁出** → `EVT-F01` `GameEventSubsystem` |
| **⑤ 直接改业务状态** | `OnRandomEventChosen` 写 `foodStock`；`OnRunEndedByCorruption` 裸写 `currentPhase` | **❌ 反模式** → 走 `Gameplay` / `Shelter` API |

### 为什么「什么都掺一脚」

1. **原型期单点入口**：所有 View 都 `Wire(AppFlowController)`，跨屏逻辑只能堆在这里。
2. **还没有 RunFlow / Event 子系统**：出征、凯旋、日结、随机事件没有独立编排层，只能写在 UI MonoBehaviour 里。
3. **按钮 handler = 业务 handler**：例如 `OnDepart` 既切屏又 `AdvancePhase` 又拼战斗配置；`OnSettlementContinue` 既入库又抽事件又开 overlay。
4. **与 EVT-F01 重叠**：随机事件队列和选项副作用（TakeIn/Expel/food/corruption）本应是 `GameEventSubsystem` 的职责。

### 目标形态（Max 拍板 — 2026-08-11）

**AppFlowController 不应再管 UI 细节**；伙伴做 UI 时把切屏/HUD/Overlay 逻辑塞进 Flow 是历史包袱，需拆回。

建议拆成两层 + 委托通信（与 PlayableLoop「编排 vs 展示」一致）：

```
┌─────────────────────────────────────────────────────────┐
│  RunFlowController（或保留名 AppFlowController）         │
│  位置：App/ 或 Gameplay/（非 UI/）                       │
│  职责：日循环编排 — 纯 C# 或轻 MonoBehaviour              │
│  · 出征 / 凯旋 / 日结 / advance / skip / 终局            │
│  · 调 Gameplay / Shelter / Combat / GameEventSubsystem   │
│  · 不引用具体 View 类型                                  │
└───────────────────────┬─────────────────────────────────┘
                        │ 委托 / 事件（Presentation 边界）
                        ▼
┌─────────────────────────────────────────────────────────┐
│  UIManager（新，Assets/Scripts/UI/）                     │
│  职责：屏幕路由与呈现                                     │
│  · SwitchScreen / ShowOverlay / HUD 显隐与 Refresh       │
│  · 绑定 View 引用；View 按钮 → 调 RunFlow 公开方法        │
│  · 实现 RunFlow 注入的 presentation 委托                  │
└───────────────────────┬─────────────────────────────────┘
                        │
                        ▼
                   *View（只展示 + 回传用户输入）
```

**信息传递：用委托（首版），不反向依赖 View**

RunFlow 侧定义 presentation 契约，由 `UIManager` 在 `Wire` 时注入，例如：

```csharp
// RunFlow 构造或 Initialize 时注入
public Action ShowShelterScreen;
public Action ShowCombatScreen;
public Action<CombatResult> ShowSettlementOverlay;
public Action ShowEndingScreen;
public Action ShowDayEndOverlay;   // 带 personnelChanges 参数的可扩展 overload
public Action RefreshHud;
```

RunFlow 编排示例：

```csharp
public void ContinueAfterCombat(CombatResult result)
{
    if (result.RunEndedByCorruption) { ForceEnding(...); ShowEndingScreen?.Invoke(); return; }
    Gameplay.AdvancePhase();
    ShowSettlementOverlay?.Invoke(result);
}
```

View **不**直接改 `GameState`；按钮 → `RunFlow.OnXxx()` → 子系统 API → 必要时 `RefreshHud`。

**与现有命名的关系**

| 现名 | 去向 |
|------|------|
| `AppFlowController` UI 方法（`ShowShelter` 等） | → `UIManager` |
| `AppFlowController` 编排（`OnDepart`、`OnSettlementContinue`…） | → `RunFlowController` / `RunFlowService` |
| `DebugCommandContext.ShowEnding` | 注入 `RunFlow.ForceEnding` + presentation 委托，不绑 View |

**实施节奏（建议）**

- **CORE-F05**（独立 feat）：保留名 `AppFlowController` 迁出 `UI/`；新增 `PresentationManager`；委托链接。详见 `CORE-F05-appflow-presentation.md`。
- **CORE-F04**：Debug 命令挂 Flow 公开 API + presentation 委托；建议在 F05 边界清晰后收口 day end/skip。

**Debug 层**：只依赖 `RunFlow` + 子系统公开 API + presentation 回调；门禁读 `GameInstance.IsRunActive` / phase，不读「当前哪个 View active」（除非 UIManager 暴露只读 `CurrentScreenId` 供 gate 用）。

---

## 接口化审计（Debug 需求 × 业务层）

> **原则（Max）：** Debug 命令不得迫使业务层为 debug 妥协。  
> 命令应调用**已暴露的业务接口**；缺接口则在业务层补齐。

图例：**✅** 已有 · **⚠️** 弱/风险 · **❌** 缺接口 · **🔧** 待补 · **✓拍板** 已确认方案

### 拍板结论（2026-08-11）

| 项 | 决定 |
|----|------|
| 无敌 | **✓** `CombatManager` 内 `PlayerInvincible` flag；生效时玩家受击伤害 override 为 0（在 `TakeDamage` 路径或 CombatManager 统一拦截） |
| 扫荡 | **✓** `CombatManager` 内 `CombatSweep` flag；结算时强制 `CombatResult.Outcome = Win`（仍走 Finish 填 FoodGained 等，再 `OnCombatFinished`） |
| `run.day set` 到 MaxDay | **✓** `SetDay` 后若 day==MaxDay → 调 `ForceEnding(MaxDayReached)` |
| `shelter.hunger` | **✓** 正 delta 且要扣 stock 时走 `AllocateFood`；纯调试 adjust（不扣 stock）走 `AdjustSurvivorHunger`；命令可 `--allocate` 或子命令 `shelter.feed` |
| 无敌/扫荡配置来源 | Debug/Inspector 在 **出征或开战前** 写入 `CombatStartConfig` 或 `CombatManager` 运行时 flag；Combat **不引用** Debug 程序集 |
| `RunFlowService` | **✓** 采用；由 `AppFlowController` 持有并转发（不另开 MonoBehaviour） |

### 门禁 / 查询

| 需求 | 现状 | 结论 |
|------|------|------|
| 主菜单 vs 局内 | `GameInstance.Mode`（MainMenu/InGame）；`Shelter == null` 可作未开局信号 | **⚠️** 缺统一查询：`IsRunActive` / `CanExecuteRunCommands` |
| 阶段 / 战斗态 | `Gameplay.CurrentPhase`；`Combat.IsFinished` / `Session != null` | **⚠️** 缺 `IGameRunQuery` 一次性给出 gate 判定 |
| `debug.status` | 可读 `GameState` + `Shelter.Survivors` | **🔧** 建议 `Gameplay.GetRunSnapshot()` 或只读 DTO，避免 debug 拼字段 |

### run.*

| 命令 | 现状 | 结论 |
|------|------|------|
| `run.corruption set` | `Gameplay.SetCorruption` →  clamp + phase=Ending + 返回 fused | **✅** 现实现正确；debug 仅在 fused 时调 `ShowEnding` |
| `run.food add` | `Gameplay.AddFood` | **✅** |
| `run.food set` | 无；只能 add 或写 `State.foodStock` | **❌ 🔧** `Gameplay.SetFood(int)`（clamp ≥0，Sync 如有需要） |
| `run.day set` | `SetDay` 仅 clamp | **🔧 ✓** `SetDay` 后若 `day >= MaxDay` → `ForceEnding(MaxDayReached)` |
| `run.day advance` | `Gameplay.AdvancePhase()` 只改 phase/day，**不**切 UI、不日结 | **⚠️** 纯状态机 API 存在；与玩家流程脱节 |
| `run.day end` | `Shelter.ProcessEndOfDay()` 存在；日结 UI 在 `AppFlowController` 私有流 | **❌ 🔧** 需 flow 级 `BeginDayEnd()` / `IRunFlowCommands.EndDay()`，内部：ProcessEndOfDay + ShowDayEnd |
| `run.day skip` | 无 | **❌ 🔧** flow 级 `SkipToNextDay()`：日结 + AdvancePhase + 切屏（可跳过事件） |
| `run.ending force` | `SetCorruption(100)` 仅覆盖腐蚀熔断；`SetPhase(Ending)` 为裸写 | **❌ 🔧** `Gameplay.ForceEnding(EndingReason reason)`：统一设 phase=Ending；debug/自然到期共用 |
| `run.phase set` | `SetPhase` 裸 assignment | **❌ 反模式** — 建议 debug **不提供**；用 advance/end/skip/ending |

### shelter.*

| 命令 | 现状 | 结论 |
|------|------|------|
| `shelter.list` | `Shelter.Survivors` 只读 | **✅**（或 🔧 轻量 `FormatRosterLine()` 纯展示） |
| `shelter.takein` | `Shelter.TakeIn(defId)` | **✅** |
| `shelter.expel` | `Expel(nameHint)` 仅 **name**，无 defId | **⚠️ 🔧** `ExpelByDefId` 或 `TryResolveSurvivor(target)` + `Expel(Survivor)` |
| `shelter.hunger add/set/feed` | 无统一 API | **🔧 ✓** `AdjustSurvivorHunger(target, delta)`（不扣 stock）；`FeedSurvivor(target, foodUnits)` → 内部 `AllocateFood`（扣 stock + 涨 hunger） |
| `shelter.kill`（若做） | 无公开 Kill | **❌ 🔧** `MarkSurvivorDead(target)` 走与饥饿死亡相同 personnelChanges 路径 |
| `shelter.hungerDecay set` | 改 `Shelter.DailyHungerDecay` 属性 | **⚠️** 规则参数，可接受；更好收进 `Shelter.SetDailyHungerDecay(int)` |

### combat.*

| 命令 | 现状 | 结论 |
|------|------|------|
| `combat.win` / `combat.lose` | `CombatManager.Finish(...)` **private** | **❌ 🔧** `ForceOutcome(CombatOutcome)` 或 `ResolveCombat(CombatOutcome)` 公开，走完整结算字段 |
| `combat.effect apply` | `CombatEffectExecutor.Execute` 需 `CombatResolveContext` + 正确 `ApplyRunCorruption` 回调 | **❌ 🔧** `CombatManager.ApplyEffect(EffectSpec, sourceSide)` 封装当前 session/context，腐蚀走 `ICorruptionRunState` |
| `combat.invincible` | 无 | **🔧 ✓** `CombatManager.PlayerInvincible`（或 config 注入）；玩家 `TakeDamage` 有效伤害=0 |
| `combat.sweep` | 无 | **🔧 ✓** `CombatManager.CombatSweep`；`Finish` 前强制 outcome=Win |
| `combat.skip` | 无；出征在 `AppFlowController.OnDepart` | **❌ 🔧** `IGameRunFlow.TryStartExpedition()`：内部判断 skip→`ResolveSkippedCombat()` 产出 `CombatResult` 并 `OnCombatFinished` |

### debug / flow 编排

| 需求 | 现状 | 结论 |
|------|------|------|
| 腐蚀满→结局 UI | debug 调 `ShowEnding` 回调；自然路径 `OnRunEndedByCorruption` 还 **直接写** `State.currentPhase` | **⚠️** AppFlow 与 Gameplay 双写 phase；应收敛到 `ForceEnding` |
| 战斗结束→凯旋 | `AppFlowController.OnCombatFinished` | **✅** debug 的 win/lose 应调 `ForceOutcome` 后 **仍走** `OnCombatFinished`，不跳过 settlement |
| Refresh HUD | `RefreshPresentation` 回调 | **✅** 编排层职责，可保留 |

### 建议新增的业务层接口（讨论稿）

**GameplaySubsystem**
- `bool IsRunActive` / static query via GameInstance
- `SetFood(int)` 
- `ForceEnding(EndingReason reason)` — 腐蚀熔断 / 天数用尽 / debug 共用
- `RunSnapshot GetSnapshot()` — status 用

**ShelterManager**
- `TryGetSurvivor(string target, out Survivor)` — defId 优先再 name
- `AdjustSurvivorHunger(...)` / `SetSurvivorHunger(...)`
- `ExpelByDefId(string defId)` 或统一 `ExpelSurvivor(string target)`
- `MarkDead(string target)`（可选）

**CombatManager**
- `bool ForceOutcome(CombatOutcome outcome)` — 公开 Finish 语义
- `bool ApplyEffectInCurrentCombat(EffectSpec spec, EffectTarget side)`
- `bool PlayerInvincible { get; set; }` — 玩家受伤 override 0
- `bool CombatSweep { get; set; }` — 结算强制 Win
- 出征时由 `RunFlowService` / `CombatStartConfig` 从 DebugSettings 拷贝 flag（Combat 不引用 Debug 程序集）

**App/RunFlowService**（纯 C#，`AppFlowController` 持有）
- `TryStartExpedition()` — 含 skipCombat 分支
- `BeginDayEnd()` / `SkipToNextDay()`
- `ContinueAfterSettlement()` — 替代 `OnSettlementContinue` 内业务链（事件部分 EVT-F01 再接）

**Debug 层只做：** 解析命令 → gate 校验 → 调上述 API → 根据返回值调 `RefreshPresentation` / flow 回调。

### 当前实现中的反模式（应逐步删除）

1. `run.phase set` — 裸 `SetPhase`，易与 UI 脱节  
2. `HandleSetPhase` 在 debug 里直接 `ShowEnding` — 应改为 `ForceEnding` 返回结果驱动  
3. `AppFlowController.OnRunEndedByCorruption` 直接写 `State.currentPhase` — 应走 Gameplay 网关  
4. 若在 `CombatManager` 内读取 `DebugSettings` — **禁止**；只读 `PlayerInvincible` / `CombatSweep` 运行时 flag（由 flow/config 设置）

---

## 建议实现顺序

1. **业务接口补齐（讨论定稿后）** — Gameplay / Shelter / Combat / Flow 薄 API
2. **S1 门禁** + 主菜单仅 help
3. **S2 标志经 config 注入**（invincible / skip / sweep → CombatStartConfig / policy，非 Combat 引用 Debug）
4. **S3 命令表** — 全部走新接口
5. **S4 推进时间** — flow 命令对齐
6. **S6 文档收口 → commit**

---

## 验收清单（更新）

- [x] `GameInstance` 由 Scene 拥有，运行路径唯一
- [x] `~` 控制台具备输入、Tab 补全、候选预览、结果回显（基础版）
- [x] 主菜单仅 `debug.help`；局内命令带 gate
- [x] `debug.status`；`run.corruption/food`；`run.day advance` / `run.day end` / `run.ending force`
- [x] `shelter.list/takein/expel/hunger.*`
- [x] `combat.invincible/skip/sweep/win/lose/effect apply`
- [x] Play 回归抽测通过 + 命令/门禁已合 `main`（`run.day skip` 仍为后续可选）

