# COMB-F03 PlayerCombatComponent + 小丑牌式卡组/手牌

## 元信息

- **ID:** `COMB-F03`
- **类型:** `Feature`
- **状态:** `Planned`（已批准；选牌模型已修订；**整链设计完前暂缓实现**）
- **负责人:** `Max`
- **最后更新：** `2026-07-30`（恢复完整数据结构定义；选 5 + Commit）
- **分支：** `feat/combat-cards`（实现阶段：F01+F02 合并后再拉）
- **相关：** `COMB-F01`、`COMB-F02`、`COMB-F05`、`COMB-feat-chain.md`、`designer-feedback-2026-07-29.md`、产品 PDF（底栏可选牌数量）

## TL;DR

定义 **`PlayerCombatComponent`（继承 `CombatComponent`）**，持有玩家侧牌库/手牌运行时；卡牌为**数据**（C# 静态表），效果映射到**有限行为原语**（无 GA/GE、无费用）。  
每回合从手牌选出 **恰好 5 张**（可撤销），**Commit** 时按**选中顺序**一次性结算并回**牌库底**；未选中留手；回合开始补至手牌上限 **8**。  
**出牌/选牌 API 只挂在 Player 上**；日后 UI 直调 Player，**不**经 `CombatManager`。  
本 feat 不含敌人、Manager、Flee。

---

## 范围

### In

- `PlayerCombatComponent : CombatComponent`
- `DeckRuntime`（或等价）：`drawPile` + `hand` + `selection`，无弃牌堆
- `CardDef` / `EffectOp` / `EffectTarget` / `EffectSpec` + **`CombatEffectExecutor`**
- C# **静态卡表**（最小白牌，便于 Edit Mode）
- 选牌 / 撤销 / Commit（必须 5 张）
- Edit Mode 测试

### Out

- **敌人** / `EnemyCombatComponent` / 敌方手牌
- `CombatSession` / `CombatManager`、Flee、回合末 `SetBlock(0)`（F04/F05）
- 费用 / 能量
- GA / GE / Tag
- Canvas UI、大门、路线
- 黑化牌、问号怪、腐蚀驱动换牌
- 敌人意图展示

---

## 已拍板

| # | 决策 |
|---|------|
| 1 | 手牌上限 = **8**（产品设计文档底栏可选牌数量） |
| 2 | 每回合 Commit 必须选出 **恰好 5 张** |
| 3 | Commit 前可 **撤销/重选** |
| 4 | 结算顺序 = **选中顺序** |
| 5 | 无费用；支持一张牌 **多效果**（`effects[]` 顺序执行） |
| 6 | 卡表先用 **C# 静态表**；执行器名 **`CombatEffectExecutor`** |
| 7 | **打牌能力在 Player**；UI 直调；Manager 不提供选牌/出牌 API |
| 8 | 敌人不在本 feat 范围 |

---

## 设计

### 1) 类型关系

```text
CombatComponentBase          // F01
  └─ CombatComponent         // F02
        └─ PlayerCombatComponent   // F03
              ├─ CombatAttributeSet（继承自 F02）
              ├─ DeckRuntime
              └─ 使用 CombatEffectExecutor + 静态 CardCatalog
```

`PlayerCombatComponent` 职责：

- 继承 F02 全部伤害/格挡 API
- 持有并驱动本玩家的 `DeckRuntime`（含选中序列）
- 提供选牌 / Commit 入口；`CommitPlay` 的 `enemyTarget` 为通用 `CombatComponent`（测试或 F05 Session 主敌）

不负责：是否轮到玩家、清 Block、胜负、Flee。

### 2) 牌区模型（小丑牌式 / 设计师反馈）

```text
drawPile[]     // 约定：index 0 为牌库顶（抽牌取 0）
hand[]         // 手牌，上限 HandLimit = 8
selection[]    // 本回合已选中的牌引用，有序；Commit 时必须 Count == CommitCount
// 无 discardPile
```

| 时机 | 行为 |
|------|------|
| 战斗开局 | 用 seed 洗牌 → `drawPile`；`DrawUntilHandLimit()` |
| 回合开始 `OnPlayerTurnStart` | `ClearSelection()`；`DrawUntilHandLimit()`（已满不抽） |
| 选牌 | `SelectFromHand(handIndex)` → 加入 `selection` 末尾；已满 5 则失败 |
| 撤销 | `DeselectAt(selectionIndex)` 或 `ClearSelection()` |
| **CommitPlay** | 仅当 `selection.Count == 5`：按序执行每张 `Effects` → 移出手牌 → **追加到 `drawPile` 末尾（库底）**；清空 selection |
| 未选中 | 留在 `hand`，不移动 |

牌库抽空仍需抽牌：首版 **不抽**（日志/测试可断言）；因打出回库底，正常循环下库不应长期为空。

常量：

```csharp
public const int HandLimit = 8;
public const int CommitCount = 5;
```

运行时牌实例：手牌/选中/库中持有同一运行时对象（内含 `CardDef` 引用）；首版卡无额外运行时状态字段。

### 3) 卡牌数据（无 GA/GE）

以下类型均放在 **`CardDef.cs`**（同文件，不拆）：

```csharp
public enum EffectOp
{
    DealDamage = 0,   // SetDamage(amount) + DealDamage(target)
    GainBlock = 1,    // source.GainBlock(amount)
    Draw = 2          // source 侧 DeckRuntime 再抽 count（受 HandLimit 约束）
}

public enum EffectTarget
{
    Self = 0,
    Enemy = 1         // 需要调用方传入的敌方 CombatComponent
}

public struct EffectSpec
{
    public EffectOp Op;
    public float Amount;       // 伤害/格挡数值；Draw 时表示张数
    public EffectTarget Target;
}

public class CardDef
{
    public string Id;
    public string DisplayName;
    public EffectSpec[] Effects;   // 顺序执行；允许为空数组（不推荐）
}
```

**执行器** `CombatEffectExecutor`（本 feat 即定此名，供 F04 敌人行为复用）：

```csharp
// F03 出牌用重载：显式传入敌方目标
void Execute(IReadOnlyList<EffectSpec> effects, CombatComponent source, CombatComponent enemyTarget)
```

- 对每个 `EffectSpec`：按 `Target` 解析受体（Self→source，Enemy→enemyTarget）
- `DealDamage`：必须走 F02：`source.SetDamage(amount)` → `source.DealDamage(enemyTarget)`（若 Target 为 Self 的伤害首版可不支持或禁止）
- `GainBlock`：仅 Self
- `Draw`：调 `source` 为 `PlayerCombatComponent` 时的 `Deck.Draw(count)`

`CommitPlay` 对 selection 中每张卡依次：`Execute(card.Def.Effects, this, enemyTarget)`，再将该卡回库底。  
F04 将为同一执行器增加 `Execute(effects, source, CombatSession session)` 重载（经 Session 解析目标）。

扩展新效果 = 加 `EffectOp` + 执行器分支 + 测试；不加任意脚本。

### 4) 静态卡表（种子，可改数值）

示例（实现时可微调，设计锁定「有攻击 / 有防御 / 可多效果」）：

| Id | 名称 | Effects |
|----|------|---------|
| `strike` | 打击 | `DealDamage(6) → Enemy` |
| `defend` | 防御 | `GainBlock(5) → Self` |
| `bash` | 痛击 | `DealDamage(4) → Enemy`，`GainBlock(2) → Self`（多效果样例） |

基础卡组构成（开局实例化多份引用同一 `CardDef`）：例如 4×strike + 4×defend + 2×bash（**总数须 ≥ 8** 以便开局满手，且可稳定选出 5 张；具体张数实现时可常量配置，设计要求：**可配置的静态列表**）。

`CardCatalog`：提供上述 `CardDef` 单例/静态只读表，以及默认 starter 列表构建方法。

### 5) DeckRuntime（数据结构）

```csharp
public class DeckRuntime
{
    // 内部：List 或等价
    // drawPile, hand, selection

    public IReadOnlyList<CardInstance> DrawPile { get; }
    public IReadOnlyList<CardInstance> Hand { get; }
    public IReadOnlyList<CardInstance> Selection { get; }

    public void Shuffle(int seed);
    public void DrawUntilHandLimit(int handLimit);
    public bool TrySelectFromHand(int handIndex, int commitCount);
    public bool TryDeselectAt(int selectionIndex);
    public void ClearSelection();
    // Commit 时由 Player 驱动：取出 selection 快照、移出手牌、逐张结算、AddLast 回库底
}

public class CardInstance
{
    public CardDef Def;
    // 首版无其它运行时字段
}
```

`CardInstance` 可与 `DeckRuntime` 同文件或放在 `CardDef.cs` 旁；**不要**为 `EffectOp` 等再拆文件。

### 6) PlayerCombatComponent API

```text
void SetupDeck(IReadOnlyList<CardDef> starterCards, int seed)
  // 装入 drawPile（生成 CardInstance），Shuffle(seed)，DrawUntilHandLimit()

void OnPlayerTurnStart()
  // ClearSelection + DrawUntilHandLimit
  // 不清 Block（清 Block 属 F05 Manager）

bool SelectFromHand(int handIndex)
  // 成功选入 selection 末尾；已在 selection、非法 index、已满 CommitCount → false

bool DeselectAt(int selectionIndex)
void ClearSelection()

bool CommitPlay(CombatComponent enemyTarget)
  // selection.Count != CommitCount → false
  // 按 selection 顺序：Execute(Def.Effects) + 该实例移出 hand + AddLast(drawPile)
  // 清空 selection；返回 true

DeckRuntime Deck { get; }
// 或直接暴露 Hand / Selection / DrawPile 只读视图
```

**无** 单张即时 `PlayCard(handIndex)` API（已由选 5 + Commit 取代）。

### 7) 与 UI / Manager 的边界

```text
UI / 输入 / EditMode 测试:
  player.SelectFromHand(...)
  player.ClearSelection() / DeselectAt(...)
  player.CommitPlay(enemy)           // 直调 Player

CombatManager（F05）:
  不提供 PlayCard / Select
  玩家 Commit 成功之后：由 UI 或测试再调 manager.NotifyPlayerCommitted()
  然后 Manager：SetBlock(0) → 敌方回合 → …
```

打牌是玩家能力；Manager 只编排时间轴。

### 8) 文件布局（实现时）

少拆文件：效果相关类型与 `CardDef` 同文件。

```text
Assets/Scripts/Combat/
  Framework/                   // F01
  CombatAttributeSet.cs        // F02
  CombatComponent.cs           // F02
  PlayerCombatComponent.cs     // F03
  Cards/
    CardDef.cs                 // EffectOp / EffectTarget / EffectSpec / CardDef
                               // （可选同文件：CardInstance）
    CardCatalog.cs             // 静态卡表 + 默认 starter
    CombatEffectExecutor.cs    // 共享执行器（F03 引入；F04 扩展 Session 重载）
    DeckRuntime.cs             // drawPile / hand / selection
Assets/Tests/EditMode/
  PlayerCombatCardTests.cs     // 含牌库流转与选牌 Commit；不必再拆
```

### 9) 与 F04 / F05 的接缝

| 后续 | F03 已提供 |
|------|------------|
| F05 回合开始调 `OnPlayerTurnStart()` | 抽牌 + 清 selection |
| UI/测试直调 Select / CommitPlay | 出牌结算 + 回库底 |
| F05 `NotifyPlayerCommitted` | 不在此 |
| F05 清 Block / Flee | 不在此 |
| F04 Session 主敌作为 `CommitPlay` 参数 | 只依赖 `CombatComponent` 形参 |

---

## 测试策略（Edit Mode）

1. `SetupDeck` 后 `hand.Count == 8`（starter ≥ 8）；`HandLimit == 8`，`CommitCount == 5`。  
2. 未满 5 张时 `CommitPlay` 失败；满 5 成功。  
3. Commit 后：5 张在库底；未选中留在手牌；selection 为空。  
4. 按选中顺序结算（先选 strike 再 defend → 先伤后挡）。  
5. `ClearSelection` / `DeselectAt` 后可重选再 Commit。  
6. `OnPlayerTurnStart` 补手至 8。  
7. 多效果牌 `bash`：敌受伤且己方 Block 增加。  
8. 无费用字段；不出现 EnemyCombat / Manager 类型依赖。

---

## 实现步骤（整链批准后）

| 步骤 | 内容 |
|------|------|
| S1 | `CardDef.cs`（含 Effect 类型）+ 静态 `CardCatalog` |
| S2 | `DeckRuntime`（洗/抽/选中/回底） |
| S3 | `CombatEffectExecutor` |
| S4 | `PlayerCombatComponent`（Select / Commit） |
| S5 | Edit Mode 全绿 |

---

## 验收清单

- [ ] `PlayerCombatComponent` + 选 5 Commit 流转就位
- [ ] 手牌上限 8；CommitCount 5；可撤销；按选中顺序
- [ ] `CardDef` / `EffectOp` / `EffectSpec` 完整；静态卡表；多效果可跑
- [ ] API 仅在 Player；Edit Mode 全绿；效果只走 F02 原语 + Draw

## 依赖

- `COMB-F01`、`COMB-F02` 已实现  
- 后续：`COMB-F04` / `COMB-F05`
