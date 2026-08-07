# COMB 设计链（先设计、后动工）

> **策略：** `COMB-F01`～`COMB-F05` **全部设计审阅通过后**，再按序开分支写代码。  
> **最后更新：** `2026-08-07`

## 总览

```text
COMB-F01  CombatComponentBase + AttributeSet     ← 代码已落（Review）
COMB-F02  CombatAttributeSet + CombatComponent   ← 代码已落（Review）
COMB-F03  PlayerCombat + 选 5 Commit               ← 代码已落（Review）
COMB-F04  EnemyCombat + 行为表 + 轻量 Session    ← 代码已落（Review）
COMB-F05  CombatManager + 编排 + 结算            ← 代码已落（Review）
COMB-F06  统一卡牌（玩家=意图）+ 内容内存种子 + Library 接口  ← **Planned**
COMB-F08  JSON 加载 Card/Encounter（替换 InMemory）           ← Deferred
COMB-F07  黑化 / SHLT-F02 特质 / EVT …
```

## 已定关键跨 feat 约定

| 约定 | 归属 |
|------|------|
| Attribute 值在 AttributeSet 字段上 | F01 |
| DealDamage → 直接 TakeDamage | F02 |
| 手牌上限 **8**；选牌在 Player；UI 直调 | F03 |
| Commit：**允许空槽**（F06 修订） | F03 → F06 |
| **玩家出牌与敌人意图同质 = CardDef** | **F06** |
| 业务经 **`ICardLibrary` / `IEncounterLibrary`**；禁死绑唯一 Catalog | **F06 预留 → F08 换 JSON** |
| 卡牌 `int Id`（1000+ 玩家 / 2000+ 意图）；**禁止**牌种 enum | F06 |
| 攻击蓄力：无行动 + 意图预兆（非数值 buff） | F06 |
| Manager：编排 / Flee / Result；不转发选牌 | F05 |

## 状态

| ID | 状态 | 设计文档 |
|----|------|----------|
| F01–F05 | Review（代码已落） | 各 COMB-F0x |
| F06 | **Planned** | `COMB-F06-designer-content.md` |
| F07 | Deferred | — |
| F08 | **Deferred**（已登记） | `COMB-F08-data-driven-content.md` |

## 下一步

1. 实现 F06（同质建模 + 内存 Library + 内容）  
2. F08：JSON 替换 InMemory（F06 Done 后）  
