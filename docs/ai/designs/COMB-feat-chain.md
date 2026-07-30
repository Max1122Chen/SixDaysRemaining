# COMB 设计链（先设计、后动工）

> **策略：** `COMB-F01`～`COMB-F05` **全部设计审阅通过后**，再按序开分支写代码。  
> **最后更新：** `2026-07-30`

## 总览

```text
COMB-F01  CombatComponentBase + AttributeSet     ← 代码已落（Review）
COMB-F02  CombatAttributeSet + CombatComponent   ← 代码已落（Review）
COMB-F03  PlayerCombat + 选 5 Commit               ← 代码已落（Review）
COMB-F04  EnemyCombat + 行为表 + 轻量 Session    ← 代码已落（Review）
COMB-F05  CombatManager + 编排 + 结算            ← 代码已落（Review）
（后续）Item 食物列表结算 / EnemyData / 入口 UI / 意图
```

## 已定关键跨 feat 约定

| 约定 | 归属 |
|------|------|
| Attribute 值在 AttributeSet 字段上 | F01 |
| Get/Set/OnChange；无 Modifier | F01 |
| DealDamage → 直接 TakeDamage | F02 |
| DamageMultiplier 在 SetDamage 时乘算 | F02 |
| 手牌上限 **8**；每回合选 **恰好 5** 张再 Commit；可撤销；按选中顺序 | F03 |
| 选牌/Commit **仅在 Player**；UI 直调；Manager **不**转发打牌 | F03 / F05 |
| 执行器 **`CombatEffectExecutor`** | F03 / F04 |
| 敌人行为表 loop；PatternDef 无 id/名；无意图展示 | F04 |
| `ExecuteTurn(CombatSession)`；轻量 Session | F04 |
| Manager：`NotifyPlayerCommitted` / `Flee` / 清 Block / Result | F05 |
| `CombatResult.FoodGained: int`（日后 Item 列表） | F05 |
| Manager 不直接改 foodStock；上层 DepositFood | F05 + SHLT |

## 状态

| ID | 状态 | 设计文档 |
|----|------|----------|
| F01 | Review（代码已落） | `COMB-F01-combat-component-base.md` |
| F02 | Review（代码已落） | `COMB-F02-combat-pipeline.md` |
| F03 | Review（代码已落） | `COMB-F03-player-cards.md` |
| F04 | Review（代码已落） | `COMB-F04-enemy-pattern.md` |
| F05 | Review（代码已落） | `COMB-F05-combat-manager.md` |

## 下一步

1. 本机 Unity Test Runner：Edit Mode 全绿  
2. prepare commit → 审阅 → commit（勿自动 merge `main`）
