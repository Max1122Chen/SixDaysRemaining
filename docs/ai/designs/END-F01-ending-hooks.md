# END-F01 结局钩子 + EndingEvaluator

## 元信息

- **ID:** `END-F01`
- **类型:** `Feature`
- **状态:** `Done`（`main`；Play 战败 E 通过）
- **负责人:** `Max`
- **最后更新：** `2026-08-16`
- **依赖：** `SHLT-F03`（`endingId` / `ForceEnding(string)` / `EndingView`）
- **相关：** `COMB-F05`、`CORE-F05`、`FEATURE_REGISTRY.md`

## TL;DR

在战斗结束时跑轻量 **`EndingEvaluator`**：若 **战败** 且庇护所有 **存活政治家** → `ForceEnding("Ending.E")`，跳过凯旋结算。  
不扩 META 回顾、不改腐蚀熔断 G / 天数 MaxDay。

---

## 范围

### In

- `EndingEvaluator`：输入战斗结果 + roster → 可选 `endingId`
- 首条规则：`CombatOutcome.Lose` ∧ `politician` 存活在场 → `Ending.E`
- `AppFlowController.OnCombatFinished`：腐蚀熔断之后、结算 overlay 之前调用；命中则 `ForceEndingFlow`
- EditMode：有/无政治家、Lose/Win/Flee
- 文档 / Registry 更新

### Out

| 项 | 归属 |
|----|------|
| META 结局回顾 / 成就 | META-F01 |
| 更多结局规则表 JSON | 后续 |
| Flee / skip / sweep 特殊叙事 | 不触发 E（sweep 会把 Lose 变成 Win） |
| Ending 文案精修 | 现有 `EndingView` 占位即可 |

---

## Design

### 触发顺序（`OnCombatFinished`）

```text
1. RunEndedByCorruption → Ending.G（已有）
2. EndingEvaluator.TryResolve(result, shelter)
   └─ 命中 → ForceEndingFlow(endingId)；不进结算
3. AdvancePhase Combat→TriumphReturn
4. ShowSettlementOverlay
```

### 规则（首版硬编码）

| 条件 | endingId |
|------|----------|
| `Outcome == Lose` 且 `Shelter.IsSurvivorPresent("politician")` | `Ending.E` |
| 其它 | 不强制终局 |

### API

```csharp
// App 程序集（避免 Gameplay↔Shelter 环依赖）
public static class EndingEvaluator
{
    public static bool TryResolveCombatEnd(
        CombatResult result,
        ShelterManager shelter,
        out string endingId);
}
```

---

## 验证

- [x] EditMode：Lose + politician → Ending.E / phase Ending
- [x] EditMode：Lose 无政治家 → 不 ForceEnding
- [x] EditMode：Win / Flee + politician → 不 ForceEnding
- [x] Play：入住政治家后战败 → 结局 E（ForceEnding 正确触发）

---

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-08-16 | 初稿并开工（`main`） |
| 2026-08-16 | Play 通过；标 Done |
