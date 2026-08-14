# SHLT-F03 幸存者被动 + 人设闭环

## 元信息

- **ID:** `SHLT-F03`
- **类型:** `Feature`
- **状态:** `Done`（实现于 `main`；待 Play 手测）
- **负责人:** `Max`
- **最后更新：** `2026-08-13`
- **分支：** `main`
- **产品源：** `docs/designs/人物模板2.0.pdf`、`EVT-F02` 幼童/政治家样例映射
- **相关：** `SHLT-F02`、`EVT-F01`/`EVT-F02`、`CORE-F06`/`CORE-F07`、`END-F01`（依赖）、`COMB-F10`（后置）、`[Feature Registry](../FEATURE_REGISTRY.md)`

## TL;DR

在 **SHLT 域**补齐「幸存者人设」业务基础与样板内容：

1. **`PassiveEffect`**：`passives.json` + 运行实例 + 日结 tick（幼童 −8/日）
2. **结局最小模型**：仅 `GameState.endingId`（**无 EndingReason enum**）；`ForceEnding(string)`；`EndingView` 按 id 展示
3. **事件 fragment**：`GrantPassive` / `RevokePassive` / `ForceEnding`；**`JumpToEnding` 硬失败退役**（不映射）
4. **人设 content**：政治家 `Story.Politician.Refused` + 回访事件 D4–D6

**政治家「战败 → 结局 E」** → **`END-F01`**（本 feat Out）。

---

## 审阅拍板（2026-08-13）

| # | 议题 | 决定 |
|---|------|------|
| 1 | `JumpToEnding` | **B：硬失败退役**，JSON 只用 `ForceEnding` + `endingId` |
| 2 | Passive 存放 | Shelter 内 `ShelterPassiveService`，不新 asmdef |
| 3 | 幼童赌气 / BeforeDayEnd | Out |
| 4 | 政治家回访窗口 | D4–D6 + AfterTriumph |
| 5 | 结局标识 | **只留 `endingId` 字符串**，删除 `EndingReason` enum |
| 6 | 被动 tick 与熔断 | 走 `ApplyCorruption` |

---

## 范围

### In（已实现）

#### Phase 0 — Foundation

| # | 交付物 | 状态 |
|---|--------|------|
| F0-1 | `passives.json` + Loader + 硬校验 | ✅ |
| F0-2 | `ActivePassive` + `ShelterPassiveService` | ✅ |
| F0-3 | `ProcessEndOfDay` 末尾 PassiveTick | ✅ |
| F0-4 | `SurvivorDef.passiveIds[]` 入住自动 Grant | ✅ |
| F0-5 | `GameState.endingId` + `ForceEnding(string)` + `StartNewRun` 清空 | ✅ |
| F0-6 | `GrantPassive` / `RevokePassive` / `ForceEnding` fragment | ✅ |
| F0-7 | `JumpToEnding` loader 硬失败 | ✅ |
| F0-8 | EditMode 测试 | ✅ |

#### Phase 1 — Persona content

| # | 交付物 | 状态 |
|---|--------|------|
| P1-1 | `passive.child.corruption_daily`（−8/日） | ✅ |
| P1-2 | `Story.Politician.Refused` + refuse 选项 | ✅ |
| P1-3 | `politician_knock_revisit`（占位文案） | ✅ |
| P1-4 | `EndingView` 按 endingId（G/E/MaxDay/Debug） | ✅ |

#### Phase 2 — Out → END-F01

| 项 | 归属 |
|----|------|
| 政治家战败 → 结局 E | END-F01 |
| 幼童赌气文案 | 后续 |
| 特质牌 / META / SAVE | COMB-F10 / META / SAVE |

---

## Design（定稿摘要）

### 三层效应

- **Tag**：叙事/门禁（Story.* / State.*）
- **Passive**：持续规则（日结 tick）
- **Ending**：`endingId` 收束（Ending.G / Ending.E / Ending.MaxDay / Ending.Debug）

### Passive

- 定义：`StreamingAssets/Shelter/passives.json`
- 实例：`ShelterPassiveService`（Grant / Revoke / TickEndOfDay）
- 日结顺序：饱食度 → PassiveTick → cleanup 离场被动
- 幼童：`survivors.json` `passiveIds` 自动 Grant，不必事件 Grant

### Ending

```csharp
public bool ForceEnding(string endingId); // 写 phase=Ending + endingId；Ending.G 时 clamp 腐蚀
```

删除 `EndingReason`。腐蚀熔断 / 天数用尽 / Debug / 事件均写 string id。

### Fragment

| Op | 字段 |
|----|------|
| `GrantPassive` | `passiveId`, 可选 `survivorDefId` |
| `RevokePassive` | `passiveId` |
| `ForceEnding` | `endingId`（必填） |

---

## 验证

### Edit Mode

- [x] passives 未知 effect.type → 抛错
- [x] child 入住自动 Grant
- [x] 日结 corruption −8（幼童存活）
- [x] Expel/Dead 后被动移除
- [x] GrantPassive / ForceEnding fragment
- [x] JumpToEnding 硬失败
- [x] StartNewRun 清空 endingId

### Play（手测）

- [ ] 开局幼童：日结腐蚀 −8
- [ ] 幼童 D2/D3/D4 不退化
- [ ] 政治家拒收 → D4+ 回访
- [ ] 腐蚀熔断 / 第六天 → Ending 文案与 id 一致

---

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-08-12 | 初稿 |
| 2026-08-13 | 拍板 + 全量实现（endingId only；JumpToEnding 硬切） |
