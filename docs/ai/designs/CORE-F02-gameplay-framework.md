# CORE-F02 Gameplay Framework（阶段框架）

## 元信息

- **ID:** `CORE-F02`
- **类型:** `Feature`
- **状态:** `Done`（已合并 `main`）
- **负责人:** `Max`
- **最后更新：** `2026-07-29`（简化：Subsystem 直管状态机；语义重命名）
- **分支：** `feat/gameplay-framework`
- **相关：** `[ROADMAP](../ROADMAP.md)`、`[Feature Registry](../FEATURE_REGISTRY.md)`

## TL;DR

本 feat 很小：搭起 `GameInstance` + `GameState` + `GameplaySubsystem`，让 `GameplaySubsystem` **直接维护日循环阶段枚举的状态机**。
不拆 `DayLoopDirector`，不用 `IPhaseHandler`，不把各阶段做成一套“Phase 插件体系”；**运作逻辑对即可**。
文件少拆：阶段枚举可与 `GameState`/`GameplaySubsystem` 同文件或相邻，不必一文件一类堆叠。
测试重点：用 Edit Mode **模拟玩家从「出征准备」→「战斗」→「凯旋」再进入下一天准备** 的抽象流程；突发事件本 feat 不落地。

---

## 范围

### In

- `GameState`（局内全局数值占位）
- `GameplayPhase` 枚举 + `GameplaySubsystem` 内的状态机切换
- `GameInstance`：初始化、主菜单/对局模式骨架
- 脚本目录雏形：`Assets/Scripts/Bootstrap`、`Gameplay`（Shelter/Combat 目录可先建空，**不必**在本 feat 写空壳 Manager）
- Edit Mode 测试：模拟一整日抽象流程与多日推进

### Out

- 庇护所业务（饱食度、NPC）、战斗业务、事件抽取
- `IPhaseHandler` / 各 Phase Enter-Exit 钩子体系
- `DayLoopDirector` 独立类
- 存档、完整 UI、结局细节

---

## 现状、目标与差距

- **当前：** 无玩法脚本；ROADMAP 已定架构与 feat 顺序。
- **目标：** 能证明「一天：出征准备 → 战斗 → 凯旋 → 下一天出征准备」可被驱动；六天循环边界清晰。
- **差距：** 缺最小可测状态机。

---

## 设计

### 1) 阶段语义（枚举）

| 枚举值（英文标识） | 中文语义 | 含义 |
|--------------------|----------|------|
| `ExpeditionPrep` | 出征准备 | 当天在庇护所出发前：分配/交互等（业务留给 `feat/shelter`） |
| `Combat` | 战斗 | 外出卡牌战斗 |
| `TriumphReturn` | 凯旋 | 战斗后回到庇护所：结算回写、归来反馈（业务留给后续 feat；本 feat 只切状态） |
| `Ending` | 结局 | 第 6 天流程走完后进入（首版可仅切状态） |

说明：

- **不要**再叫笼统的 `Shelter`：庇护所内其实覆盖「出发前」与「归来后」两段，语义不同。
- **突发事件**本 feat 不进状态机；`feat/events` 明朗后再插入或挂在凯旋段。
- 不必单独搞 `DayAdvance` 阶段：在离开 `TriumphReturn`、进入下一天 `ExpeditionPrep` 时 `day++` 即可。

日循环（本 feat）：

```text
StartNewRun
  -> ExpeditionPrep（day = 1）
  -> Combat
  -> TriumphReturn
  -> （day++；若 day > 6 则 Ending，否则 ExpeditionPrep）
  -> …
```

### 2) 文件与类（尽量少）

建议最少文件：

```text
Assets/Scripts/Bootstrap/
  GameInstance.cs

Assets/Scripts/Gameplay/
  GameState.cs              // 可含 GameplayPhase 枚举
  GameplaySubsystem.cs      // 持有 GameState；StartNewRun / AdvancePhase

Assets/Tests/EditMode/
  GameplaySubsystemTests.cs // 或 GameplayFlowTests.cs
```

可选：`GameplayPhase` 单独文件——**不强制**。

**不需要：** `DayLoopDirector.cs`、`IPhaseHandler.cs`、本 feat 内的 `ShelterManager`/`CombatManager`/`EventDirector` 空壳。

### 3) GameState

```csharp
public class GameState
{
    public int day;
    public int foodStock;
    public int corruption;
    public int rngSeed;
    public int population;
    public GameplayPhase currentPhase;
}
```

由 `GameplaySubsystem` 持有唯一实例。

### 4) GameplaySubsystem

职责（就这些）：

- 持有并暴露 `GameState`
- `StartNewRun(seed)`：初始化状态，`day=1`，`currentPhase=ExpeditionPrep`
- `AdvancePhase()`：按固定表推进下一阶段；从 `TriumphReturn` 离开时处理 `day++` / `Ending`
- **不在 `Update` 里自动切阶段**；由测试或后续业务显式调用 `AdvancePhase()`

实现上：`GameplaySubsystem` 可以是纯 C# 类（更易 Edit Mode 测），或 MonoBehaviour 包一层；**若用 MonoBehaviour，请把状态机逻辑放在可被 new 的普通类方法里，或直接测其 public 方法且不依赖场景**——推荐优先 **非 MonoBehaviour 的 `GameplaySubsystem`**，由 `GameInstance` 持有实例，后续再挂场景对象。由你实现时二选一，设计偏好：**普通 C# 类 `GameplaySubsystem`，GameInstance 负责挂场景**。

### 5) GameInstance

- 单例可接受。
- 创建/持有 `GameplaySubsystem`，模式切换（主菜单 / 对局中）骨架。
- 不持有 day/food 等局内字段。

---

## 实现步骤（合并在本文）

| 步骤 | 内容 |
|------|------|
| S1 | 建目录 + 必要 asmdef（Gameplay 被 Tests 引用） |
| S2 | `GameState` + `GameplayPhase` + `GameplaySubsystem` 状态机 |
| S3 | `GameInstance` 骨架 |
| S4 | Edit Mode：抽象一日/多日流程测试全绿 |

---

## 测试策略

### 结论：可行

核心测的是 **抽象游玩流程**，不是编辑器操作：

1. `StartNewRun` → `ExpeditionPrep`，`day==1`
2. `AdvancePhase` → `Combat`
3. `AdvancePhase` → `TriumphReturn`
4. 再 `AdvancePhase` → `day==2` 且回到 `ExpeditionPrep`（模拟「准备 → 战斗 → 凯旋 → 下一天准备」）
5. 连续推进至第 6 天结束后进入 `Ending`（边界断言与实现对齐后写死）

不必测 Handler 调用顺序（已无 Handler）。不必为本 feat 测 Event 跳过。

验证：`Test Runner → EditMode → Run All`

---

## 验收清单

- [ ] `GameState` / `GameplaySubsystem` / `GameInstance` 就位（文件不过度拆分）
- [ ] 阶段语义为 出征准备 / 战斗 / 凯旋 / 结局
- [ ] Edit Mode 覆盖「准备→战斗→凯旋→次日准备」流程
- [ ] 编译无错；注册表与进度日志同步

## 审阅通过后

1. 拉 `feat/gameplay-framework`
2. 按上表实现，测试绿后 prepare commit
