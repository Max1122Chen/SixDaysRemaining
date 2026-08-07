# COMB-F08 战斗内容数据驱动（JSON CardDef + Encounter）

## 元信息

- **ID:** `COMB-F08`
- **类型:** `Feature`
- **状态:** `Review`（Play 已验数值变化；待合 main）
- **负责人:** `Max`
- **最后更新：** `2026-08-07`
- **分支：** `feat/combat`（不新开 `feat/combat-data-driven`）
- **相关：**
  - 前序：`COMB-F06`（`ICardLibrary` / `IEncounterLibrary` / `CombatContent`）
  - 并存：`COMB-F07` Corrupted（规则仍在代码；本 feat 不参数化）
  - 产品：`docs/designs/六日英雄，卡牌.xlsx`（导出管线 **Out**）

## TL;DR

把战斗卡牌与日遭遇的**内容真相**从 C# 种子改为 **`StreamingAssets/Combat` JSON**，经现有 Library 接口注入。  
DTO → Def；**加载或校验失败一律抛异常硬失败**（禁止静默 fallback），避免设计师以为 JSON 生效实际却在跑旧种子。  
Edit Mode 测试通过 **`ResetForTests` 注入内存库** 或指向测试用合法 JSON，不依赖「缺文件自动 Seed」。  
**不做** Excel 导出、热重载、Corrupted 进 JSON。在 **`feat/combat`** 上实现。

---

## 已拍板

| # | 议题 | 决议 |
|---|------|------|
| P1 | 路径 | `StreamingAssets/Combat/` |
| P2 | 文件 | `cards.json` + `encounters.json`（含 dayMap）+ `starter.json`（首版做） |
| P3 | 解析 | DTO + 转换；Unity `JsonUtility` + 包装根对象；不用第三方包（首版） |
| P4 | 失败策略 | **硬失败**：抛 `Exception`（或项目统一内容异常类型）；**禁止** fallback 到 C# Seed |
| P5 | 权威 | **仅 JSON** 为运行时内容源 |
| P6 | C# Seed | 删除或降为「仅测试/导出辅助」；正式 `Ensure()` **不再** Seed 兜底 |
| P7 | Corrupted | 仍 `CorruptedRules` 代码常量 |
| P8 | 分支 | **`feat/combat`**；不新开细碎 feat 分支 |
| P9 | 测试 | `ResetForTests` 注入；或临时合法 JSON fixture；坏 JSON 测试 **Assert.Throws** |

---

## 现状、目标与差距

| | |
|--|--|
| **当前** | `CombatContent.Ensure` → InMemory Seed |
| **目标** | 设计师改 JSON；错了立刻炸，绝不能悄悄用旧表 |
| **差距** | 无文件、无 Loader、无硬失败校验 |

---

## 范围

### In

1. Schema + 与现行种子一致的基准 JSON 入仓  
2. DTO + Loader + 校验  
3. `CombatContent.Ensure`：**只**从 StreamingAssets 加载；失败 throw  
4. `starter.json` 驱动开局份数  
5. Edit Mode：成功加载；坏/缺文件 **Throws**；`ResetForTests` 仍可用  
6. 文档 / registry 同步  

### Out

- Excel → JSON 工具、热重载、Corrupted / 奖励表进 JSON  
- 静默 fallback、Release 与 Editor 两套失败策略  

---

## 设计

### 加载流（硬失败）

```text
StreamingAssets/Combat/
  cards.json
  encounters.json
  starter.json

CombatContent.Ensure():
  read + parse cards.json      → fail → throw
  validate cards               → fail → throw
  read + parse encounters.json → fail → throw
  validate encounters vs cards → fail → throw
  read + parse starter.json    → fail → throw
  validate starter cardIds     → fail → throw
  fill InMemory*Library
  ready = true
```

异常消息须含：**文件名、字段/Id、原因**（便于设计师对照 Console）。

### 为何仍填 InMemory

接口与 `ResetForTests` 不变；JSON 只是填充来源。正式路径无 Seed 分支。

### 测试策略（无 fallback 后）

| 场景 | 做法 |
|------|------|
| 现有战斗单测 | `ResetForTests` 注入内存库（不读盘） |
| JSON 契约 | 读仓库内基准 StreamingAssets，或 `Assets/.../TestFixtures` |
| 坏 JSON | 指向临时坏文件 / 解析 API，`Assert.Throws` |

### 路径

```csharp
Path.Combine(Application.streamingAssetsPath, "Combat", "cards.json")
```

Editor / Standalone：`File.ReadAllText`。移动端读法见 `TD-006`（未完成前移动包会硬失败——可接受，促补齐）。

---

## JSON Schema（设计师可读）

### `cards.json`

```json
{
  "cards": [
    {
      "id": 1000,
      "displayName": "剑意",
      "description": "",
      "tags": ["Attack"],
      "canBlacken": true,
      "effects": [
        { "op": "DealDamage", "amount": 5, "amountSecondary": 0, "target": "Enemy" }
      ]
    },
    {
      "id": 2100,
      "displayName": "攻击蓄力",
      "description": "无行动。预示之后将有强力攻击。",
      "tags": ["Charge", "Intent"],
      "canBlacken": false,
      "effects": []
    }
  ]
}
```

| 字段 | 允许值 |
|------|--------|
| `tags[]` | `Attack`, `Defend`, `Combo`, `Special`, `Sleep`, `Charge`, `Intent`（可多选） |
| `op` | `DealDamage`, `GainBlock`, `Draw`, `Heal`, `AddCorruption`, `RemoveCorruption`, `DealDamagePlusAttackCount`, `GainBlockRandom` |
| `target` | `Self`, `Enemy` |

号段：玩家 `1000+`；意图 `2000+`；空槽 `0` 不进表。

### `encounters.json`

```json
{
  "encounters": [
    {
      "id": 1,
      "displayName": "小怪01",
      "maxHp": 35,
      "damageBonus": 0,
      "roundPlans": [
        { "slots": [2204, 0, 2090, 2205, 2304] }
      ]
    }
  ],
  "dayMap": [
    { "day": 1, "encounterId": 1 },
    { "day": 2, "encounterId": 2 },
    { "day": 3, "encounterId": 4 },
    { "day": 4, "encounterId": 3 },
    { "day": 5, "encounterId": 5 },
    { "day": 6, "encounterId": 3 }
  ]
}
```

- `roundPlans`：JsonUtility 不支持锯齿数组，用 `{ "slots": [5 ints] }`  
- 每行 `slots` **长度必须为 5**；`0` = 空槽  
- `dayMap` **必须覆盖 day 1–6**；强化怪 Id=`4`/`5`（与 `EncounterIds` 一致）  

### `starter.json`

```json
{
  "copies": [
    { "cardId": 1000, "count": 4 },
    { "cardId": 1001, "count": 2 },
    { "cardId": 1002, "count": 2 },
    { "cardId": 1003, "count": 3 },
    { "cardId": 1004, "count": 3 },
    { "cardId": 1005, "count": 2 }
  ]
}
```

缺文件或未知 `cardId` → throw。

---

## 校验规则（全部 throw）

| 规则 | 行为 |
|------|------|
| 文件缺失 / 读失败 | throw |
| JSON 解析异常 | throw |
| `cards` 空 | throw |
| 重复 card/encounter id | throw |
| 未知 tags / op / target | throw |
| `roundPlans` 行长 ≠ 5 | throw |
| plan 中非 0 id 不在 card 表 | throw |
| `dayMap` 缺 day 1–6 | throw |
| starter 引用未知 cardId | throw |
| `canBlacken` 缺省 | 默认 `true`；意图建议显式 `false` |

---

## 与 F06 / F07 接缝

| 组件 | F08 改动 |
|------|----------|
| Library 接口 | 不改签名 |
| `CombatContent.Ensure` | 只走 JSON；失败 throw |
| `Seed*` | 移除出正式路径；可留作一次性导出脚本或删 |
| `ResetForTests` | 保留，供单测 |
| `CorruptedRules` | 不动 |

---

## 建议切片（均在 `feat/combat`）

| Slice | 内容 |
|-------|------|
| S01 | 导出基准 JSON 入 `StreamingAssets/Combat/` |
| S02 | DTO + Loader + 校验（失败 throw） |
| S03 | `Ensure` / starter 接线；去掉正式 Seed 兜底 |
| S04 | 测试：ResetForTests 绿；坏 JSON Throws；Play 改数冒烟 |

---

## 验证

- 合法 JSON：`Get(1000)` 等与设计一致  
- 缺文件 / 坏 Id / 行长错误 → **抛异常**，不加载任何半套表  
- `ResetForTests` 下旧战斗测试仍绿  
- Play：改剑意 JSON 伤害，进战可见  

## 验收清单

- [ ] 明确开工后实现  
- [ ] 基准 JSON 入库且为唯一运行时内容源  
- [ ] 任意加载/校验失败 → Exception，无 fallback  
- [ ] starter.json 生效  
- [ ] 业务仍只经 Library；无 Excel 工具 / 热重载  
- [ ] 测试绿；在 `feat/combat` 提交  

---

## 审批收口

| 项 | 决议 |
|----|------|
| 失败策略 | **硬失败（Exception）**，不要 fallback |
| starter.json | **首版做** |
| 分支 | **`feat/combat`**，不新开 |
