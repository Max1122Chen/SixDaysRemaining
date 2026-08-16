# COMB-F10 特质卡系统（三特质收口）

## 元信息

- **ID:** `COMB-F10`
- **类型:** `Feature`
- **状态:** `Review`（半开放待验收；EditMode 齐，完整 Play 等人设）
- **负责人:** `Max`
- **分支：** `main`（自 `feat/combat` 合入）
- **最后更新：** `2026-08-16`
- **相关：** `SHLT-F02`、`COMB-F05`、`FEATURE_REGISTRY.md`

## TL;DR

把战斗特质从 **名字子串解锁** 收成 **`defId` 解锁**；本 feat **只做已定义的三特质**（英雄 / 护士 / 小贼）并补齐边界与 EditMode。  
**不做**新人物特质、不做牌库化。

**验收说明：** 护士/小贼依赖事件入住或 Debug TakeIn；人物设定未齐前 **不要求完整 Play 闭环**，feat 完成后标 **半开放 Review**。

---

## 范围

### In

| # | 交付 |
|---|------|
| 1 | `UnlockSurvivorDefId`；退役 `UnlockNameFragments` / `IsOwnedByNames` |
| 2 | `GetOwnedTraits(aliveDefIds)`；Shelter 只传存活实例 |
| 3 | AppFlow 出征 + TraitBar + Shelter 详情同源 |
| 4 | 三特质行为可测：英雄 ManualOnce；护士 RoundEnd Heal；小贼 TurnStart 伤+偷意图 |
| 5 | 满手偷牌：意图已偷走、**不入**手牌；**PlayerTurnStart 特质先于抽牌** |
| 6 | EditMode；文档标半开放 Review |

### Out

- 幼童 / 政治家 / 运动员特质
- 特质进牌库 / CardDef 1006+
- 完整 Play（等人设/入住路径齐）

---

## Design

### 解锁

| Trait | StartsOwned | UnlockSurvivorDefId |
|-------|-------------|---------------------|
| Hero | true | — |
| Nurse | false | `nurse` |
| Thief | false | `thief` |

存活 = `status` 非 Dead / Left。

### 小贼满手与时机

- `StealRandomAction` 先清空意图；`AddToHand` 失败则牌不进手。
- `EndRound` / 开局：`TriggerTraits(PlayerTurnStart)` **先于** `OnPlayerTurnStart` 抽牌，以便回合间隙手牌有空位时能偷入手。

---

## 验证

### Edit Mode（本 feat 必达）

- [x] 默认 roster → 仅英雄
- [x] TakeIn nurse/thief → 对应特质；Expel → 失去
- [x] 英雄 once；护士 RoundEnd；小贼伤害+偷

### Play（半开放）

- [ ] Debug TakeIn 护士/小贼后战斗槽点亮（手测可选）
- [ ] 正式入住线完整测 → 等人设齐

**完成后状态：`Review`（半开放待验收）**

---

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-08-16 | 定稿并实现；半开放待验收 |
