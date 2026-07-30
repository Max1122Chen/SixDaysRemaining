# 外部参考：CardGameDemo（轻量 GAS / 卡牌规则机）

> **状态：** `Reference`  
> **用途：** 《六日英雄》战斗数值框架的设计参考，**不**直接拷贝整套实现。  
> **最后更新：** `2026-07-29`

## 仓库位置

| 项 | 路径 / URL |
|----|------------|
| 本地 | `D:\Dev\GitRepo\CardGameDemo` |
| 远端 | https://github.com/Max1122Chen/CardGameDemo.git |

## 优先阅读（对本项目最相关）

| 文档 / 代码 | 关注点 |
|-------------|--------|
| `docs/design/systems/attributes.md` | Attribute 分层、伤害/格挡元属性、倍率/修正/偏移 |
| `docs/design/systems/gameplay-framework.md` | Base/Current、Modifier 聚合、GE/GA、GFC（≈ ASC） |
| `docs/design/systems/combat.md` | Damage / DamageToTake 解耦、预计算 vs 结算 |
| `packages/core/src/gfc/` | `GameplayFrameworkComponent` 实现 |
| `packages/combat/src/combat-attributes.ts` | 战斗属性键集合 |

## 与《六日英雄》的关系

- CardGameDemo 是 **TypeScript 规则机**（控制台 / BattleOnly），《六日英雄》是 **Unity C# GameJam 原型**。
- 借鉴：**属性双值、元属性、受击/造成伤害解耦、共用 Combat 承载组件**。
- 暂不照搬：完整 Tag/GE/GA、Evaluation Stage Pipeline、装备驱动卡组、行动力 STS 循环（六日战斗更接近「小丑牌」式手牌流转，见战斗讨论）。

## 引用约定

实现 `COMB-F01`（ASC 骨架）与 `COMB-F02`（伤害管线）时，以本页为索引；具体裁剪写在对应 `docs/ai/designs/COMB-F0*.md`。
