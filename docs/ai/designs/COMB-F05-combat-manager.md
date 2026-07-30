# COMB-F05 CombatManager + 回合编排 + 结算

## 元信息

- **ID:** `COMB-F05`
- **类型:** `Feature`
- **状态:** `Planned`（已批准；**整链设计完，随 `feat/combat` 实现**）
- **负责人:** `Max`
- **最后更新：** `2026-07-30`（批准；选牌在 Player；含 Flee）
- **分支：** `feat/combat-manager`
- **相关：** `COMB-F01`～`F04`、`COMB-F03`（选 5 Commit）、`SHLT-F01`

## TL;DR

**`CombatManager`** 只做战斗编排与结算：开打、标记玩家/敌方回合、在玩家 **已 Commit / 已 Flee** 之后推进流程、双方回合末 **`SetBlock(0)`**、胜负/逃离判定、产出 **`CombatResult`**。  
**不**提供选牌/出牌 API——UI 与测试直接调 **`PlayerCombatComponent`**（选 5 → `CommitPlay`），再通知 Manager。  
`FoodGained` 暂为 **int**（日后 Item 列表）。支持 **Flee** 与 **BattleOnly**。

---

## 范围

### In

- `CombatManager` 回合状态机、清 Block、Win/Lose/**Flee**、`CombatResult`
- `NotifyPlayerCommitted` / `Flee`（或等价命名）
- BattleOnly
- 与 Shelter 回写接缝（上层 `DepositFood`）
- Edit Mode（直调 Player + Notify Manager）

### Out

- Item 类；大门 UI；意图展示；黑化牌；速度先手
- 在 Manager 上实现 Select/Commit/PlayCard

---

## 已拍板

| # | 决策 |
|---|------|
| 1 | 双方回合末 **`SetBlock(0)`** |
| 2 | **玩家先手** |
| 3 | 玩家侧：**选满 5 张 → Commit**（见 F03）；Manager **不调**打牌 |
| 4 | UI/输入 **直调 Player**；Commit 成功后再 **Notify** Manager |
| 5 | `FoodGained: int` 占位 |
| 6 | **Flee 本 feat 做** |
| 7 | BattleOnly 纳入 |

---

## 设计

### 1) 职责

```text
PlayerCombatComponent     // 选牌、CommitPlay、牌库
EnemyCombatComponent      // ExecuteTurn
CombatManager             // 何时轮到谁、清挡、Flee、Result
UI / Tests                // 直调 Player；再 Notify Manager
```

### 2) 回合状态机

```text
[StartCombat]
  → 组装 Session
  → PlayerTurn
  → player.OnPlayerTurnStart()

PlayerTurn:
  （输入层）player.Select* / CommitPlay(enemy)
  - Commit 成功后：输入层调用 manager.NotifyPlayerCommitted()
  - Notify 内：
      若已分出胜负 → FinishCombat
      否则 player.SetBlock(0) → EnemyTurn
  - 任意时刻（仅 PlayerTurn）：manager.Flee() → FinishCombat(Flee)

EnemyTurn:
  → enemy.ExecuteTurn(session)
  → 检查胜负
  → enemy.SetBlock(0)
  → 若仍存活 → player.OnPlayerTurnStart() → PlayerTurn
  → 否则 FinishCombat

FinishCombat:
  → 冻结（再 Commit/Flee/Notify 无效）
  → 填写 CombatResult
```

说明：Manager **不知道**选了哪 5 张；只在 Notify 时假定「本回合玩家出牌阶段已结束」。若 `CommitPlay` 失败，不应调用 Notify。

可选优化（非必须）：`CommitPlay` 内检测 HP 已决出胜负时仍由 Notify 统一 `FinishCombat`，避免 Player 依赖 Manager。

### 3) CombatManager API

```text
void StartCombat(CombatStartConfig config)
void StartBattleOnly(CombatStartConfig config)

void NotifyPlayerCommitted()   // 仅 PlayerTurn；推进清挡+敌方回合
bool Flee()                    // 仅 PlayerTurn；结束为 Flee

CombatSession Session { get; }
bool IsFinished { get; }
bool IsPlayerTurn { get; }     // 供 UI 判断是否允许选牌（可选）
CombatResult Result { get; }
```

**禁止：** `PlayCard` / `SelectFromHand` 出现在 Manager 上。

玩家能否选牌：UI 可查 `manager.IsPlayerTurn && !manager.IsFinished`；Player 本身可不强制绑 Manager（测试可在无 Manager 时单独测 Commit）。

### 4) Flee

```csharp
public enum CombatOutcome
{
    Win = 0,
    Lose = 1,
    Flee = 2
}
```

| Outcome | FoodGained | CorruptionDelta |
|---------|------------|-----------------|
| Win | 固定（如 3） | +3 |
| Lose | 0 | +3 |
| Flee | **0**（无食物） | +3（逃离不清除本场应计腐蚀；与「战斗结束固定腐蚀」对齐，首版 Flee 也 +3） |

Flee 不执行敌方剩余行动；立即 `FinishCombat`。

### 5) CombatResult

同前：`Outcome`、`FoodGained`（int，Item 迁移点）、`CorruptionDelta`、`TurnsElapsed`（可选）。  
Manager 不改 `GameState.foodStock`。

### 6) BattleOnly / 测试写法

```text
manager.StartBattleOnly(config)
player = manager.Session.Player
enemy = manager.Session.Enemies[0]

// 选 5 张
player.SelectFromHand(...)
player.CommitPlay(enemy)
manager.NotifyPlayerCommitted()

// 或
manager.Flee()
```

### 7) 文件布局

```text
CombatManager.cs
CombatResult.cs    // 可合并
CombatStartConfig.cs
CombatManagerTests.cs
```

---

## 测试策略

1. 选 5 → Commit → Notify → 敌方行动；玩家 Block 在 Notify 后为 0。  
2. 打到敌死 → Win + FoodGained + CorruptionDelta。  
3. 敌打死玩家 → Lose。  
4. `Flee` → Outcome.Flee，FoodGained=0。  
5. Manager 无选牌 API；结束后 Notify/Flee/Commit 无效。  
6. 不自动改外部 foodStock。

---

## 验收清单

- [ ] 编排完整；Flee 可用；FoodGained 为 int
- [ ] 打牌仅经 Player；Manager 仅 Notify/Flee/状态
- [ ] BattleOnly + Edit Mode 全绿

## 依赖

- F01～F04；`ShelterManager.DepositFood`  
- 后续：Item 列表结算、入口 UI
