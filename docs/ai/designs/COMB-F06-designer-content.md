# COMB-F06 设计师内容落地（统一卡牌模型 + 效果扩展 + 日遭遇）

## 元信息

- **ID:** `COMB-F06`
- **类型:** `Feature`
- **状态:** `Planned`（审阅通过；可开分支实现）
- **负责人:** `Max`
- **最后更新：** `2026-08-07`（同质意图=卡牌；预留 Library 接口；JSON → F08）
- **分支（建议）：** `feat/combat-content`
- **相关：**
  - 产品：`docs/designs/六日英雄—技术演示文档2.0.pdf`、`docs/designs/六日英雄，卡牌.xlsx`
  - 前序：`COMB-F01`～`F05`、`CORE-F03`、`COMB-feat-chain.md`
  - 后续：`COMB-F08` JSON 数据驱动、`COMB-F07` Corrupted、`SHLT-F02` 特质

## TL;DR

落地设计师基础牌与六日小怪内容；**玩家出牌与敌人意图同质——都是 `CardDef`/`CardInstance`**，共用 `CombatEffectExecutor`（重构现有 `TurnAction` 内容路径）。  
扩展效果（组合/腐蚀/随机格挡/治疗等），修订空槽与消极腐蚀规则。  
**本轮仍用内存静态种子填表**，但必须留下 **`ICardLibrary` / 遭遇查询接口**（或等价），**禁止**把 `CardCatalog` 写成日后唯一入口；**JSON 读写留给 `COMB-F08`，本 feat 不实现。**  
攻击蓄力：结算无行动，意图预兆「强攻将至」。Corrupted / 特质 / ？怪 Out。

---

## 架构拍板：意图 = 卡牌

设计师新理念：敌人「意图」不是独立行为类型，而是 **与玩家相同的卡牌**。

```text
统一：
  CardDef (int Id, DisplayName, Tags, Effects[], CanBlacken, …)
  CardInstance → CardDef
  CombatEffectExecutor.Execute(card, source, context)

玩家回合 5 槽：CardInstance?[]   // null = 空槽
敌人回合 5 槽：CardInstance?[]   // 由遭遇方案的 cardId[] 解析；null = 空

废弃作为「内容真相」：
  TurnAction.Effects 与 EnemyActionKind 驱动结算（可留 UI 提示派生，或删除）
```

**空槽：** 双方均可 null，结算跳过。  
**休眠 / 攻击 N / 防御 N：** 各自是一张（或共享模板参数化的）`CardDef`，不是 `EnemyActionKind` 分支。  
**攻击蓄力：** 单独 `CardDef`（Id `2100`）。**结算无行动**（`Effects` 空）；**意图展示为预兆**——告知玩家之后将有强力攻击（非倍率/挂 buff）。  
**天 3/5 伤害 +1：** 遭遇级 `DamageBonus`（flat），在解析/结算攻击类效果时叠加——仍走卡牌效果管线，不另开敌人攻击 API。

### Id 号段（约定，非 enum）

| 段 | 用途 |
|----|------|
| `1000+` | 玩家可见/持有牌（剑意…缓释；特质预留） |
| `2000+` | 敌人常用意图牌（攻击 N、防御 N、休眠、空占位、攻击蓄力…） |
| 同一 `ICardLibrary` | 两端都 `Get(id)`；允许未来共享同 Id 若设计需要 |

---

## 新需求捕捉（相对现状）

### A. PDF 2.0 战斗增量（摘要，决议已收口）

空槽合法；放牌 &lt;3 → 本场结束腐蚀 +2（可累加）；补牌至手牌 8；非 Flee 固定腐蚀 +3；Flee 保留已产生局内腐蚀；Confirm 后不可回退；Corrupted → F07。

### B. 卡牌.xlsx + 同质意图

| # | 需求 | F06 |
|---|------|-----|
| B1–B5 | 基础 6 种玩家牌及效果 | **In**（内存种子 → Library） |
| B6 | 特质三张 | **Out** → SHLT-F02 |
| B7 | 小怪多方案 × 5 意图 | **In**：方案 = `int[5] cardIds`（0/哨兵=空） |
| B8 | 休眠 HP+10 | **In**：意图卡 `Heal` |
| B9 | 攻击蓄力 | **In**：无行动 + 意图预兆文案（强攻将至） |
| B10 | 玩家牌与敌人意图同质 | **In：重构建模** |
| B11 | 日后 JSON 调参 | **Out → F08**；本 feat **预留接口** |

---

## 范围

### In

1. **统一卡牌模型**  
   - `CardDef.Id: int`（≥1000 玩家段 / ≥2000 意图段）  
   - Tags、`CanBlacken`、`EffectSpec[]` 扩展（Heal / Corruption± / RandomBlock / 蓄力修正等）  
   - `CombatResolveContext`（本回合双方槽位快照、slot index、腐蚀累计、遭遇 DamageBonus）
2. **`ICardLibrary`（名称可调）**  
   - `bool TryGet(int id, out CardDef def)` / `CardDef Get(int id)`  
   - `IEnumerable` 或按需查询  
   - F06 提供 `InMemoryCardLibrary`（由现静态种子 `Register`），**调用方只依赖接口**  
   - **不**实现 JSON
3. **遭遇模型（为数据驱动做准备）**  
   - `EnemyEncounterDef`：`Id`、`DisplayName`、`MaxHp`、`DamageBonus`、`RoundPlans: int[][]`（每方案长度 5，元素为 cardId，空槽用 `0`）  
   - `IEncounterLibrary` + `InMemoryEncounterLibrary`（天 → encounter）  
   - `EnemyCombatComponent`：按回合取 plan，解析为 `CardInstance?[]`，结算调 Executor（与玩家同路径）
4. **拆除/降级** 以 `TurnAction`+`EnemyActionKind` 为内容主路径的逻辑；UI 意图展示改为读 `CardDef`（DisplayName/Tags/Effects 描述）
5. **出牌规则**：空槽、消极 +2、Confirm 锁定（同前拍板）
6. **接缝**：开战注入 Library + 按 day 取 Encounter；起始牌组从 Library 按 id×数量构建
7. **Edit Mode 测试**

### Out

- JSON / 文件加载（**COMB-F08**）  
- Excel 导出管线  
- 特质卡、黑化完整、？怪奖励  
- 大改五槽 UI 框架（仅适配同质意图展示与空槽）

---

## 设计

### Option A（推荐）：Library 接口 + 内存实现 + 同质结算

- 玩家 `ResolvePlayerSlot` / 敌人 `ResolveEnemySlot` 均：取槽上 `CardInstance` → `CombatEffectExecutor`  
- 遭遇与卡表结构 **直接可序列化**（字段 = 未来 JSON 形状），F08 只换 Loader  
- **好处：** 一次建模，避免 F08 再拆 `TurnAction`  
- **风险：** UI 需改意图绑定；旧 `EnemyPatternCatalog` 删除或适配为注册意图卡的辅助

### Option B：先填旧 TurnAction，F08 再同质化

- **不选：** 与设计师理念冲突，返工大。

### 1) 玩家牌种子（InMemory，经 Library）

| Id | 名称 | 数量 | 效果摘要 | CanBlacken |
|----|------|------|----------|------------|
| 1000 | 剑意 | 4 | 伤 5 | true |
| 1001 | 蓄力一击 | 2 | 伤 5 + 槽内每张 Attack +1 | true |
| 1002 | 血祭 | 2 | 伤 7；腐蚀 +5 | false |
| 1003 | 抵挡 | 3 | 格挡 +4 | — |
| 1004 | 庇佑 | 3 | 50% 格挡 2 或 7 | — |
| 1005 | 缓释 | 2 | 格挡 +3；腐蚀 −4 | — |

特质预留 1006–1008，本 feat 不注册实体。

### 2) 敌人意图牌种子（示例号段，实现时可微调）

| Id | 名称 | 效果 |
|----|------|------|
| 2000 | 空 | 无（或仅用 cardId=0 表示空，不注册） |
| 2001+ | 攻击 N / 防御 N | 按表出现的数值各建 Def，或参数化工厂注册 |
| 2090 | 休眠 | Heal 10 |
| 2100 | 攻击蓄力 | Effects 空；Description 预兆强攻 |

表内「攻击 4」「攻击5」等 → 注册对应 Id；遭遇 plan 只存 Id。

### 3) 日遭遇

| 天 | Encounter | MaxHp | DamageBonus | Plans |
|----|-----------|-------|-------------|-------|
| 1 | 小怪01 | 35 | 0 | 4×5 cardIds |
| 2 | 小怪02 | 42 | 0 | 3×5 |
| 3 | 小怪01 强化 | 50 | +1 | 同 01 |
| 4 | 小怪03 | 55 | 0 | 含 2100 占位 |
| 5 | 小怪02 强化 | 62 | +1 | 同 02 |
| 6 | 暂复用 03 | — | — | Q7 |

循环：`plans[(round-1) % planCount]`。

### 4) 出牌 / 腐蚀（拍板摘要）

Q1 补至 8；Q2 非 Flee +3；Q3 配卡可调 Confirm 锁定；Q4 缓释 −4；Q5 蓄力=无行动+意图预兆；Q6 消极每回合累加；Q8 蓄力计数含自身，血祭算 Attack。

---

## 卡牌 Id 说明

- **类型 `int`，禁止牌种 `enum`**  
- 玩家段自 **1000**；意图段建议 **2000+**  
- Catalog 常量可有（`public const int SwordIntent = 1000`），仅便测试，不是 enum  
- 真相来源：F06 = `InMemoryCardLibrary`；F08 = JSON → 同一接口  

---

## 与 F08 的边界

| F06 | F08 |
|-----|-----|
| 定义 `CardDef` / Encounter 可序列化形状 | 从 JSON 填充 Library |
| `InMemory*Library` | `Json*Library` 替换注册 |
| 业务只依赖接口 | 业务不改或少改 |

---

## 实现注意点

- 删除「必须满 5」双路径；更新 F03/F05 备注  
- 组合伤害读本回合玩家槽快照（含未结算槽）  
- 不要让 UI/Manager `switch (EnemyActionKind)` 结算数值  
- 旧 `strike`/`defend`/`bash` 字符串 id 移除  
- 攻击蓄力：仅预兆文案 + 空结算，禁止脑补倍率 buff  

## 验证

- Edit Mode：同质结算（敌我同一 Executor）、Library.Get、遭遇 plan 循环、蓄力无数值变化、空槽、消极腐蚀、玩家牌效果  
- Play：Day1 小怪01；意图条显示为卡名/效果描述；Day4 见「攻击蓄力（强攻将至）」  

## 验收清单

- [ ] 敌我槽位均以 Card 结算  
- [ ] 存在可替换的 Card/Encounter Library 接口；无 JSON 也可跑  
- [ ] 基础 16 张玩家牌 + 日遭遇与表一致（特质除外）  
- [ ] 攻击蓄力：无行动结算 + 意图预兆可读  
- [ ] 未实现 F08/F07/特质  

## 建议切片

| Slice | 内容 |
|-------|------|
| S01 | CardDef/Id/Effects/Context + ICardLibrary 内存实现 |
| S02 | 敌人意图同质化 + IEncounterLibrary + 拆 TurnAction 内容路径 |
| S03 | 玩家 6 种牌种子 + 测试 |
| S04 | 空槽/消极腐蚀/固定 +3 + F03 文档修订 |
| S05 | 日遭遇接缝 + 蓄力占位 + Play 冒烟 |
