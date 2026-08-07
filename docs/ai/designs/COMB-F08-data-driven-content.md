# COMB-F08 战斗内容数据驱动（JSON CardDef + Encounter）

## 元信息

- **ID:** `COMB-F08`
- **类型:** `Feature`
- **状态:** `Deferred`（已登记；**本轮不实现**；依赖 F06 先落地同质模型与加载接口）
- **负责人:** `Max`
- **最后更新：** `2026-08-07`
- **分支（建议）：** `feat/combat-data-driven`
- **相关：** `COMB-F06`（预留接口）、`COMB-F07` 黑化、产品 xlsx / 日后导出管线

## TL;DR

在 F06 已具备 **统一 `CardDef`（玩家牌 = 敌人意图）** 与 **`ICardLibrary` / `IEncounterLibrary`（或等价）** 的前提下，把内容从 C# 静态种子改为 **JSON（或等价文本）加载**，方便设计师调参；本 feat **不做**调参工具链本身，只做运行时加载与校验。

## 动机

- 设计师改数值不应改代码、等编译  
- 敌人血量、意图序列、牌效果与玩家共用同一套 Def 文件  
- F06 允许暂时用内存实现 Library；F08 换成文件实现，调用方不变  

## 范围（将来）

### In（预期）

- `StreamingAssets` 或 `Resources` 下的 cards / encounters JSON  
- 启动或开战时：`ICardLibrary.Load` / `IEncounterLibrary.Load`  
- 校验：未知 Id、空意图槽、重复 Id、缺字段 → 显性失败 Log  
- 文档：字段 schema + 与 xlsx 列对照  

### Out

- Excel→JSON 自动导出工具（可另 TECH feat）  
- 热重载编辑器扩展（可后置）  
- 黑化生成逻辑（F07）  

## 与 F06 的契约（F06 必须留下）

1. **禁止**业务代码直接依赖 `CardCatalog` 静态字段列表作为唯一真相；经 `ICardLibrary.Get(int id)` / `TryGet`  
2. **敌人每槽意图** = `CardDef`/`CardInstance`（或 null 空槽），与玩家出牌走同一 `CombatEffectExecutor`  
3. **遭遇**经 `IEncounterLibrary`（或 `EnemyEncounterDef` + provider）：`MaxHp`、强化、每回合 5 个 cardId  
4. F06 可用 `InMemoryCardLibrary` 填入与 JSON schema 同构的数据，便于 F08 无痛替换  

## 状态

等 F06 Review/Done 后再写完整 Design Spec 并改 Planned。
