# SHLT-F01 庇护所 + 幸存者（Shelter Manager）

## 元信息

- **ID:** `SHLT-F01`
- **类型:** `Feature`
- **状态:** `Review`（实现完成，待 merge）
- **负责人:** `Max`
- **最后更新：** `2026-07-29`（Edit Mode 全绿；命名 Survivor）
- **分支：** `feat/shelter`
- **相关：** `[ROADMAP](../ROADMAP.md)`、`[Feature Registry](../FEATURE_REGISTRY.md)`、`[设计师反馈](../../designs/designer-feedback-2026-07-29.md)`、`CORE-F02`

## TL;DR

在 `ExpeditionPrep`（出征准备）与 `TriumphReturn`（凯旋）两段，由 **`ShelterManager`** 负责：

1. **食物入库**（凯旋后，战斗收获折算进 `GameState.foodStock`）
2. **食物分配**（出征前，从存量分给幸存者）
3. **持有幸存者列表**，供玩家查询状态（对话由 Player↔Survivor 直接进行，不经 Shelter）

饱食度**日结**在**天数推进之前**（离开 `TriumphReturn`、`day++` 之前）执行。  
首版 **无 UI**，用 **Debug.Log** 验收；**Edit Mode** 测试为主。

---

## 范围

### In

- `Survivor` 数据模型 + `SurvivorStatus` 枚举
- `ShelterManager`：幸存者列表、`DepositFood` / `AllocateFood`、日结饱食度、状态更新
- 与 `GameState.foodStock` 集成（整型存量；「罐头」语义留给 combat 收获侧）
- `population` 作为 **ShelterManager 派生属性**（存活幸存者数量，见下文）
- Edit Mode 测试：分配、入库、日结、状态迁移
- 脚本目录：`Assets/Scripts/Shelter/` + asmdef

### Out

- 幸存者 **对话**系统（Player 与 Survivor 直接交互，本 feat 不实现）
- **traits**（特质）
- 庇护所 **道具**、大门 **UI**、路线选择（归 `feat/combat` 或后续）
- 完整 Canvas UI
- 战斗内「罐头」物品对象（combat feat 用 `CombatResult` 等表达即可）

---

## 现状、目标与差距

- **当前：** `GameplaySubsystem` 阶段机已跑通；`GameState.foodStock` / `population` 为占位字段。
- **目标：** 在抽象日循环中，庇护所侧能「入库 → 次日分配 → 日结扣饱食度 → 状态变化」。
- **差距：** 无 Shelter 域类型与 Manager；`population` 未与幸存者列表挂钩。

---

## 设计

### 1) Shelter 在日循环中的职责

```text
ExpeditionPrep（出征准备）
  └─ ShelterManager.AllocateFood(survivor, amount)   // 玩家分配；扣 foodStock、加 survivor.hunger
  └─ （可选）列出幸存者状态                          // Debug 或后续 UI

Combat
  └─ （combat feat）产出收获，如 cansGained

TriumphReturn（凯旋）
  └─ ShelterManager.DepositFood(amount)         // 入库，foodStock += amount
  └─ ShelterManager.ProcessEndOfDay()           // 日结：扣饱食度、更新 status
  └─ GameplaySubsystem.AdvancePhase()           // 随后 day++ 或 Ending
```

**调用方（首版）：** 测试代码或 `GameInstance` 上的 Debug 方法显式调用；**不在** `GameplaySubsystem.AdvancePhase()` 内硬编码 Shelter 逻辑。

### 2) 食物模型

| 层 | 表示 | 说明 |
|----|------|------|
| 全局存量 | `GameState.foodStock`（`int`） | 庇护所分配、入库的唯一账本 |
| 战斗获得 | `CombatResult.cansGained` 等（combat feat） | 文案/设计层称「罐头 +N」；凯旋时 `DepositFood(N)` |

**约定：** 1 罐头 = `foodStock` +1；分配时 1 单位存量 = 喂 1 单位饱食度（可调常量，首版 1:1）。

### 3) Survivor 模型

```csharp
public enum SurvivorStatus
{
    Healthy = 0,
    Hungry = 1,
    Dying = 2,
    Dead = 3,
    Left = 4
}

public class Survivor
{
    public string name;
    public int hunger;           // 饱食度，>= 0
    public SurvivorStatus status;
}
```

- **traits：** 首版不做。
- **对话：** 不属于 Shelter；Shelter 仅 **持有引用** 供查询 `name` / `hunger` / `status`。
- **初始 roster：** 首版 **2 名固定幸存者**（`StartNewRun` 时注入）。

### 4) 状态规则（首版）

| 常量 | 首版默认值 | 含义 |
|------|------------|------|
| `hungryThreshold` | `1` | `hunger <= 此值且 > 0` → `Hungry` |
| `hungerPerFoodUnit` | `1` | 分配 1 存量提升 1 饱食度 |
| `dailyHungerDecay` | `1` | 日结每人扣除饱食度 |

**状态推导（`UpdateSurvivorStatus(survivor)`）：**

```text
若 status 已是 Dead 或 Left → 不变
若 hunger == 0 → Dying
若 hunger <= hungryThreshold → Hungry
否则 → Healthy
```

**Dying → Dead：** 若已是 `Dying`，日结后仍 `hunger == 0` → `Dead`。

### 5) ShelterManager API

```csharp
public class ShelterManager
{
    public IReadOnlyList<Survivor> Survivors { get; }
    public int Population => /* 存活幸存者数：status 不为 Dead/Left */;

    void RegisterSurvivor(Survivor survivor);
    void DepositFood(int amount);
    bool AllocateFood(Survivor survivor, int amount);
    void ProcessEndOfDay();
    void UpdateSurvivorStatus(Survivor survivor);
}
```

**`population`：** `ShelterManager` 为幸存者唯一数据源；`GameState.population` 在 Shelter 操作后同步。

### 6) 文件布局

```text
Assets/Scripts/Shelter/
  Survivor.cs                 // Survivor + SurvivorStatus
  ShelterManager.cs
  SixDaysRemaining.Shelter.asmdef
```

### 7) Debug 验收

- `DebugDepositFood` / `DebugAllocateFood(survivorIndex, n)` / `DebugProcessEndOfDay` / `DebugLogAllSurvivors`

---

## 验收清单

- [ ] `ShelterManager` + `Survivor` / `SurvivorStatus` 就位
- [ ] 入库、分配、日结、状态更新可测
- [ ] Edit Mode 测试全绿；Console 无编译错误
- [ ] Debug Log 可观察 shelter 操作（无 Canvas UI）
