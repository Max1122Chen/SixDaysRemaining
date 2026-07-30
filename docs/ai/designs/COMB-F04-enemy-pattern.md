# COMB-F04 EnemyCombat + 行为表 + 轻量 CombatSession

## 元信息

- **ID:** `COMB-F04`
- **类型:** `Feature`
- **状态:** `Planned`（已批准；**COMB 设计链完成前暂缓实现**）
- **负责人:** `Max`
- **最后更新：** `2026-07-30`（批准；无意图展示；整链完再动工）
- **分支：** `feat/combat-enemy`（实现阶段：F01–F03 合并后再拉）
- **相关：** `COMB-F01`～`F03`、`COMB-F05`（完整 Session/Manager/结算）、`COMB-feat-chain.md`

## TL;DR

定义 **`EnemyCombatComponent`**：继承 `CombatComponent`（含格挡），按可 **loop** 的回合行为表执行原语；行为与卡牌 **完全共用 `EffectOp` / `EffectSpec`**，执行走 F03 已定名的 **`CombatEffectExecutor`**（本 feat 增加 Session 重载）。  
**`EnemyPatternDef` 只描述行为**（不含敌人 id / displayName；身份与数值归后续 `EnemyData`）。  
**首版不做意图展示**（后续再加）。  
`ExecuteTurn(CombatSession session)`：从 Session 解析目标。本 feat 提供 **轻量 `CombatSession` 骨架**；回合机、清 Block、胜负、结算留给 **`COMB-F05`**。

---

## 范围

### In

- `EnemyPatternDef` / `TurnAction`（仅 `Effects`；静态样例表）
- `EnemyCombatComponent : CombatComponent`
- 扩展 `CombatEffectExecutor`：`Execute(effects, source, CombatSession session)`
- 轻量 `CombatSession`：持有 Player + Enemies，提供阵营查询
- Edit Mode 测试（单敌人即可）

### Out

- **意图 / Intent 预告 UI 与字符串展示**（延后）
- `EnemyData`（id、displayName、MaxHP 绑定、掉落等）
- `CombatManager` 回合状态机、先手、回合末 `SetBlock(0)`、胜负、`CombatResult`
- 玩家出牌编排、大门 UI、多敌人互助的具体效果（仅预留 Session API）
- 复杂 AI / 条件分支行为表

---

## 已拍板

| # | 决策 |
|---|------|
| 1 | 敌人 **有格挡**（走 F02 `GainBlock` / `Block`） |
| 2 | 行为表与卡牌 **完全共用 `EffectOp`**（及 `EffectSpec`） |
| 3 | **暂不支持意图展示**；后续再说 |
| 4 | `EnemyPatternDef` **不含** id / displayName，只定义行为 |
| 5 | `ExecuteTurn(CombatSession session)` |
| 6 | F04 定义 **基础 CombatSession**；F05 再做完整编排 |
| 7 | 执行器在 F03 即名为 **`CombatEffectExecutor`**（非 CardEffectExecutor） |

---

## 设计

### 1) 类型关系

```text
CombatComponent                 // F02
  ├─ PlayerCombatComponent      // F03
  └─ EnemyCombatComponent       // F04
        └─ EnemyPatternDef（行为 only）
              └─ TurnAction[]（loop，仅 Effects）

CombatSession（F04 骨架）
  ├─ Player : PlayerCombatComponent
  └─ Enemies : List<EnemyCombatComponent>

CombatEffectExecutor            // F03 引入；F04 增加 Session 重载
```

### 2) EnemyPatternDef（只行为）

```csharp
/// <summary>一轮敌方回合的动作；可被行为表循环使用。</summary>
public class TurnAction
{
    /// <summary>可执行原语；与卡牌共用 EffectSpec。</summary>
    public EffectSpec[] Effects;
}

/// <summary>可 loop 的行为表；不含敌人身份与展示名。</summary>
public class EnemyPatternDef
{
    public TurnAction[] Turns;   // Length >= 1；执行完最后一项后从 0 重新计
}
```

- **不包含：** `id`、`displayName`、`maxHp`、掉落、意图文案等 → 身份归 `EnemyData`；意图归后续。  
- 静态样例：在 `EnemyPatternCatalog` 里用 **字段名**区分模式（如 `BasicAttackDefendLoop`）。

样例（数值可调）：

```text
Turns[0]: Effects = [ DealDamage(8) → Enemy ]
Turns[1]: Effects = [ GainBlock(5) → Self ]
Turns[2]: Effects = [ ]   // 空 = 本回合无数值动作
→ loop
```

### 3) Effect 语义（敌方视角）

与 F03 **同一套** `EffectOp` / `EffectTarget` / `EffectSpec`。

| EffectTarget | 敌方执行时的解析（经 Session） |
|--------------|--------------------------------|
| `Self` | 正在行动的该 `EnemyCombatComponent` |
| `Enemy` | 对立阵营；首版 = Session 中的 **Player** |

`Draw`：敌方无牌库；行为表误用时 **忽略并 Log**（测试锁定）。

Session 预留 `GetAllies` / `GetOpponents`；首版行为表不用 Ally。

### 4) 轻量 CombatSession（F04）

```csharp
public class CombatSession
{
    public PlayerCombatComponent Player { get; }
    public IReadOnlyList<EnemyCombatComponent> Enemies { get; }

    public bool IsEnemy(CombatComponent c);
    public bool IsPlayer(CombatComponent c);

    public IReadOnlyList<CombatComponent> GetOpponents(CombatComponent self);
    public IReadOnlyList<CombatComponent> GetAllies(CombatComponent self);

    /// <summary>敌方 EffectTarget.Enemy 的默认受体：Player。</summary>
    public CombatComponent GetPrimaryOpponent(CombatComponent self);
}
```

**F04 不做：** 回合归属、出牌许可、胜负、清 Block、战斗结束。  
**F05：** Manager 持有/扩展 Session，编排回合与结算。

### 5) EnemyCombatComponent

```text
EnemyCombatComponent : CombatComponent
  - EnemyPatternDef pattern
  - int patternIndex

void BindPattern(EnemyPatternDef pattern)
  // HP 由外部 InitCombatant

void ExecuteTurn(CombatSession session)
  1. turn = Turns[patternIndex]
  2. CombatEffectExecutor.Execute(turn.Effects, source: this, session)
  3. patternIndex = (patternIndex + 1) % Turns.Length

bool IsAlive => HP > 0
```

格挡可通过 `GainBlock` 获得；**清 Block 时机由 F05 Manager** 调 `SetBlock(0)`。

### 6) CombatEffectExecutor（与 F03 对齐）

```csharp
// F03 已有（出牌）
void Execute(IReadOnlyList<EffectSpec> effects, CombatComponent source, CombatComponent enemyTarget)

// F04 新增（敌人行为 / 将来统一走 Session）
void Execute(IReadOnlyList<EffectSpec> effects, CombatComponent source, CombatSession session)
  // Self → source；Enemy → session.GetPrimaryOpponent(source)
```

`DealDamage` 一律：`source.SetDamage` → `source.DealDamage(resolvedTarget)`。

### 7) 文件布局（少拆）

```text
Assets/Scripts/Combat/
  ...
  Cards/
    CombatEffectExecutor.cs   // F03 已有；F04 追加 Session 重载
  EnemyPatternDef.cs          // TurnAction + EnemyPatternDef
  EnemyPatternCatalog.cs
  EnemyCombatComponent.cs
  CombatSession.cs
Assets/Tests/EditMode/
  EnemyCombatTests.cs
```

### 8) 与 F05 / EnemyData 的接缝

| 后续 | 用法 |
|------|------|
| `EnemyData` | `displayName`、`maxHp`、引用 `EnemyPatternDef`、掉落等 |
| `CombatManager` | 建 Session；调用 `ExecuteTurn`；清 Block；胜负 |
| 意图 UI | **本 feat 不提供**；以后再加 |

---

## 测试策略（Edit Mode）

1. Pattern 两步 loop：连续 `ExecuteTurn` 效果不同，第三次回到第一步。  
2. 攻击步：玩家 HP 下降。  
3. 防御步：敌人 Block 增加。  
4. `EnemyPatternDef` 无 id/displayName/Intent 字段。  
5. Session：`GetAllies(enemy)` 单敌时仅自己；`GetOpponents(enemy)` 含 Player。

---

## 实现步骤（整链批准后）

| 步骤 | 内容 |
|------|------|
| S1 | `EnemyPatternDef` + Catalog |
| S2 | 轻量 `CombatSession` |
| S3 | `CombatEffectExecutor` Session 重载 + `EnemyCombatComponent` |
| S4 | Edit Mode 全绿 |

---

## 验收清单

- [ ] 敌人有格挡；行为共用 EffectOp / CombatEffectExecutor
- [ ] Pattern 可 loop；无意图展示 API
- [ ] PatternDef 无身份字段；ExecuteTurn(session)
- [ ] 轻量 Session 可查阵营；无 Manager 回合机
- [ ] Edit Mode 全绿

## 依赖

- `COMB-F01`～`F03`  
- 后续：`COMB-F05`；`EnemyData` / 意图展示另议
