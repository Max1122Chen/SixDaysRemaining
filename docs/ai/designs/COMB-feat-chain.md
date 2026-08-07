# COMB 设计链（先设计、后动工）

> **策略：** 战斗域在 `feat/combat` 上连续推进；小 Feature 不另拆细碎分支。  
> **最后更新：** `2026-08-07`

## 总览

```text
COMB-F01  CombatComponentBase + AttributeSet     ← Done
COMB-F02  CombatAttributeSet + CombatComponent   ← Done
COMB-F03  PlayerCombat + 选 5 Commit               ← Done
COMB-F04  EnemyCombat + 行为表 + 轻量 Session    ← Done
COMB-F05  CombatManager + 编排 + 结算            ← Done
COMB-F06  统一卡牌（玩家=意图）+ 内容内存种子 + Library 接口  ← Done
COMB-F07  Corrupted 伴生牌（≥40 / 动态倍率 / +8 / 100 熔断）  ← Done
COMB-F08  JSON 加载 Card/Encounter（硬失败；无 fallback）           ← Done（feat/combat 待合 main）
```

## 已定关键跨 feat 约定

| 约定 | 归属 |
|------|------|
| Attribute 值在 AttributeSet 字段上 | F01 |
| DealDamage → 直接 TakeDamage | F02 |
| 手牌上限 **8**；选牌在 Player；UI 直调 | F03 |
| Commit：**允许空槽**（F06 修订） | F03 → F06 |
| **玩家出牌与敌人意图同质 = CardDef** | **F06** |
| 业务经 **`ICardLibrary` / `IEncounterLibrary`** | **F06 → F08 JSON** |
| 卡牌 `int Id`（1000+ 玩家 / 2000+ 意图）；**禁止**牌种 enum | F06 |
| 攻击蓄力：无行动 + 意图预兆（非数值 buff） | F06 |
| Corrupted：伴生实例、无独立 Def；见 F07 | F07 |
| `corruption >= 100` → 任意来源立即整局结局 | F07 |
| 内容真相：JSON @ StreamingAssets；**加载失败 throw** | F08 |
| COMB 实现默认在 **`feat/combat`** | 分支纪律 |
| Manager：编排 / Flee / Result；不转发选牌 | F05 |

## 状态

| ID | 状态 | 设计文档 |
|----|------|----------|
| F01–F08 | **Done** | 各 COMB-F0x / F08 |

## 下一步（战斗域外）

1. `feat/combat`（F08）合 `main` + push  
2. `SHLT-F02` 讨论与 design  
3. 可选：Excel→JSON TECH；TD-004/005/006  
