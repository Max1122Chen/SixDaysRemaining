# CORE-F10 设计师反馈修复包 02

## Meta
- **ID:** `CORE-F10`
- **类型:** `Feature`
- **状态:** `Review`（已实现；待 EditMode / 轻 Play）
- **负责人:** `Max`
- **最后更新：** `2026-08-23`
- **分支：** `main`（与 F09 同纪律，按 slice 直推）
- **相关：** `[Feature Registry](../FEATURE_REGISTRY.md)`、`CORE-F09`、`COMB-F11`、`END-F02`、`SHLT-F05`

## TL;DR

收口设计师第二轮数值与循环反馈：**5 天战斗 + 第 6 天终局**、敌人 HP 曲线、起始牌库比例、**昨日投喂 → 次日状态回升**、**日腐蚀 +10（战斗结算展示）**、回合奖励档 **4/3/1**、结局屏展示**可读的达成条件**。

开局食物 **1** 已在 CORE-F09 落地（`DefaultStartingFoodStock = 1`），本包仅回归确认。

---

## 设计师需求映射

| # | 需求摘要 | 主要落点 | 现状差距 |
|---|----------|----------|----------|
| 1 | 第 1–5 关敌人 HP：15 / 42 / 65 / 80 / 92 | `encounters.json` + `dayMap` | 当前 day1=35、day3/4/6 映射不一致 |
| 2 | 攻击牌各 +1、**庇佑** −1 | `starter.json` | 当前 18 张，未按新比例 |
| 3 | **第 6 天 = 结局日**，不再多打一天 | `GameplaySubsystem` / `AppFlowController` / `dayMap` | 当前第 6 天仍出征，第 7 天才 Ending |
| 4 | 昨日已投喂 → 次日状态向健康升一格 + 立绘刷新 | `ShelterManager` + `AppFlowController` | 仅有 `fedDefIds` 当日标记，无次日回升 |
| 5 | 开局食物 = 1 | `ShelterManager.DefaultStartingFoodStock` | **已满足**，测例回归即可 |
| 6 | 腐蚀按**天** +10，非按回合；战斗结算展示 | `CombatManager` / `SettlementView` 文案 | 逻辑已是 flat +10；需确认 UI 不说「每回合」；奖励档 `CorruptionDelta` 字段不再参与结算 |
| 7 | 1–2 回合同属第一档；食物奖励 **4 / 3 / 1** | `CombatRewardTable` | 食物档 5/3/2；需核对 `turnsElapsed` 边界 |
| 8 | 结局展示**判定理由**（腐蚀/人口等） | `endings.json` + `EndingView` / 图鉴 | 仅有 title/body，无条件摘要 |

---

## Scope

### In
- JSON：`encounters.json`（HP + 去掉 day 6 战斗映射）、`starter.json`
- 日循环：第 5 日凯旋后 `day=6` 直接 `Ending`，不进入第 6 日 `ExpeditionPrep`
- 庇护所：`fedYesterday` 快照 + 次日 `ImproveStatusOneStep`；分配食物当日不提前跳立绘（濒死例）
- 战斗：`CombatRewardTable` 食物 4/3/1；结算/ HUD 语义对齐「今日腐蚀」
- 结局：`endings.json` 增 `criteriaHint`（或等价字段）；`EndingEvaluator.ResolveCriteriaText`；`EndingView` 展示
- EditMode：日循环、投喂回升、奖励档、结局条件文案、牌库/遭遇 JSON 加载

### Out
- 新 UI 美术、结局多页/CG
- Excel 导出工具
- 腐蚀熔断 G、战败 E、人口 F 的触发规则改写（仅补展示文案）

---

## Design

### 1) 敌人 HP（COMB 内容）

按 **dayMap 第 1–5 天** 对应 encounter 的 `maxHp`：

| 游戏日 | encounterId（建议） | maxHp |
|--------|---------------------|-------|
| 1 | 1 | 15 |
| 2 | 2 | 42 |
| 3 | 3 | 65 |
| 4 | 4 | 80 |
| 5 | 5 | 92 |

**实现建议：** 5 条 encounter 各设上述 HP；`dayMap` 仅保留 day 1–5，**删除 day 6 战斗行**。行为表（`roundPlans`）可沿用现有 id 1–5 的计划，仅调 HP。

### 2) 起始牌库（starter.json）

| cardId | 名称 | 现 count | 新 count |
|--------|------|----------|----------|
| 1000 | 剑意 | 5 | **6** |
| 1001 | 蓄力一击 | 2 | **3** |
| 1002 | 血祭 | 3 | **4** |
| 1003 | 抵挡 | 3 | 3 |
| 1004 | 庇佑 | 3 | **2** |
| 1005 | 缓释 | 2 | 2 |

合计 **20** 张（EditMode 需同步）。

「攻击类每种 +1」= 1000/1001/1002；「防御类庇佑 −1」= 1004。

### 3) 第 6 天终局（日循环）

**目标时间线：**

```text
Day 1–5: ExpeditionPrep → Combat → TriumphReturn →（日结）→ 次日
Day 5 日结推进后: day=6 → GameplayPhase.Ending（无第 6 日战斗）
```

**代码改动要点：**
- `GameplaySubsystem.AdvancePhase`：`TriumphReturn` 分支 `day++` 后，若 `day >= MaxDay`（6）→ `ForceEnding(EndingIds.MaxDay)` 占位，**不再**回到 `ExpeditionPrep`
- `AdvanceDayWithoutCombat`：同样 `day >= MaxDay` 进 Ending
- `GameplayFlowTests.AfterSixthDayTriumph_EntersEnding` 改为：**5 次完整日出征后** day=6 且 Ending（不再期望 day=7）
- HUD / 事件：`state.day == 6` 时已在 Ending，需确认无「第 6 日出征」按钮路径

`MaxDay` **保持 6**（语义：共 6 个日历日，末日为结局日）。

### 4) 昨日投喂 → 次日状态回升（SHLT）

**规则（设计师例）：**
- 第 1 天濒死 NPC 被分配食物 → **当天立绘仍为濒死**
- 第 2 天开始时：若 **昨日已投喂** → 状态 **升一格**：`Dying → Hungry`，`Hungry → Healthy`；`Healthy` 不变
- 升格后 `ShelterPortraits.Load(..., status, day)` 自然换立绘；`ShelterView.Refresh` 在日初触发

**流程（推荐 Option A）：**

```text
OnDayEndContinue（日结面板点继续）:
  1. snapshot = copy(fedDefIds)     // 昨日谁被喂过
  2. AdvancePhase → day++
  3. if Ending: …
  4. Shelter.ApplyFedYesterdayRecovery(snapshot)
  5. fedDefIds.Clear(); foodAllocationDay = day
  6. EnterShelter BeforeDepart
```

**AllocateFood 调整：**
- 仅扣 `foodStock` 并记录当次分配单位数（`fedFoodAmounts`）
- **当日不修改 `hunger`、不刷新状态/立绘**
- 次日 `ApplyFedYesterdayRecovery`：**先** `hunger += 份量 × HungerPerFoodUnit`，**再** 状态升一格

**ProcessEndOfDay：**
- 保持现有饥饿衰减 / 死亡 / 被动 tick
- 不与「次日回升」重复：回升发生在 **day 切换后、新一天 BeforeDepart 前**，不在 day N 的 `ProcessEndOfDay` 里做

**Slice ID：** `CORE-F10-S2`（Shelter 投喂链）

### 5) 开局食物 = 1

已落地：`ShelterManager.DefaultStartingFoodStock = 1`。本包 DoD：`ShelterManagerTests.InitializeDefaultRoster_SetsTwoSurvivorsAndStartingFood` 保持绿。

### 6) 日腐蚀 +10（战斗结算展示）

**语义：** 每 **出征日** 固定 +10（`FlatCorruptionOnFinish = 10`），空槽另计 +3/格；在 **战斗结算屏** 展示，**不是**「每打一轮 +10」。

**核对/改动：**
- `CombatManager.Finish`：继续 `flat + passivePenalty*3`；**不**把 `CombatRewardTable.CorruptionDelta` 叠加入 `result.CorruptionDelta`
- `SettlementView`：文案改为「今日腐蚀 +N」/「本日腐蚀」（避免「回合」）
- 可选：奖励进度条只表示 **食物档位**，不暗示腐蚀随回合增加

### 7) 回合奖励档

`CombatRewardTable`：

| 档位 | 回合范围 | 食物（新） | 标签 |
|------|----------|------------|------|
| 1 | **1–2** | **4** | 速战 |
| 2 | 3–6 | **3** | 拉锯 |
| 3 | ≥7 | **1** | 鏖战 |

**边界：** `GetTier(1)` 与 `GetTier(2)` 均返回第一档（已支持 `MaxRounds=2`）。若 Play 仍只见 1 回合进档，查 `turnsElapsed` 计数是否在 `BeginRound`/`EndRound` 与 `Finish` 间 off-by-one。

**腐蚀：** 奖励档 **只调食物**，与日腐蚀 +10 解耦。

### 8) 结局判定理由展示（END 扩展）

在 `endings.json` 增加玩家向字段，例如 `criteriaHint`：

```json
{
  "id": "Ending.A",
  "title": "永恒的英雄",
  "body": "…",
  "criteriaHint": "达成条件：腐蚀度 ≤ 39，庇护所人数 ≥ 3",
  "trigger": "RunComplete",
  ...
}
```

**展示：**
- `EndingView`：title/body 下方增加一行 criteria（来自 JSON；无字段则 `EndingEvaluator` 由 def 字段 **自动生成** 兜底，如「腐蚀 81–100，人数 2」）
- `ShelterCodexView` 结局条目：可选同步 hint（防剧透可仅终局屏展示——**默认终局屏必显，图鉴不剧透**）

**各结局建议 hint（与 END-F02 条件对齐）：**

| ID | criteriaHint（草案） |
|----|---------------------|
| A | 腐蚀度 ≤ 39，人数 ≥ 3 |
| B | 腐蚀度 ≤ 39，人数 = 1 |
| C | 腐蚀度 41–80，人数 = 1 |
| D | 腐蚀度 41–80，人数 ≥ 3 |
| E | 战斗失败，且庇护所有「政治家」 |
| F | 庇护所人数 = 0 |
| G | 腐蚀度 ≥ 100 |
| H | 腐蚀度任意，人数 = 2 |
| I | 腐蚀度 ≥ 81 |
| MaxDay | 六日已到，未匹配其他分支 |

---

## 实现 Slice（建议顺序）

| Slice | 内容 | 风险 |
|-------|------|------|
| **CORE-F10-S1** | encounters HP + dayMap 去 day6；starter 比例 | 低 |
| **CORE-F10-S2** | 日循环 day6 Ending；GameplayFlowTests | 中（牵动 SAVE 节点文案） |
| **CORE-F10-S3** | 投喂次日回升 + AllocateFood 濒死当日不跳态 | 中 |
| **CORE-F10-S4** | CombatRewardTable 4/3/1 + 结算文案 | 低 |
| **CORE-F10-S5** | endings criteriaHint + EndingView | 低 |
| **CORE-F10-S6** | EditMode 全量回归 | — |

---

## 验证

### EditMode（必达）
- `GameplayFlowTests`：5 日出征后 day=6 Ending
- `ShelterManagerTests` / 新用例：`FedYesterday_DyingBecomesHungryNextDay`
- `CombatManagerTests` / `CombatContentJsonTests`：HP、牌库 20、奖励 4/3/1
- `EndingEvaluatorTests`：criteria 文案 lookup

### Play（抽检）
- Day1 濒死幼童：喂食 → 仍濒死立绘 → Day2 初变饥饿立绘
- Day5 战后日结 → 直接进结局，无 Day6 出征
- 1 回合与 2 回合胜利均为 4 食物
- 结局屏能看到「达成条件：…」

---

## 验收清单

- [x] S1–S6 实现
- [x] EditMode 全绿
- [ ] Play 抽检通过
- [x] `FEATURE_REGISTRY` / `ACTIVE_WORK` / `PROGRESS_LOG` 更新

---

## 开放问题（审阅时确认）

1. **第 3–5 关** 是否沿用现有 encounter 行为表，还是设计师另有出招表？（默认：仅改 HP）
2. **投喂回升** 与 **ProcessEndOfDay 饥饿衰减** 同处新一天：先回升再衰减，还是日结末尾才衰减？（默认：日切换后先回升，该日结束前仍跑 ProcessEndOfDay 衰减）
3. **结局条件** 是否在图鉴中展示？（默认：仅终局屏，避免剧透）
