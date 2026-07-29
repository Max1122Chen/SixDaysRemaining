# CORE-F02 Gameplay Framework（阶段框架）

## 元信息

- **ID:** `CORE-F02`
- **类型:** `Feature`
- **状态:** `Review`（待负责人审阅；通过后开 `feat/gameplay-framework`）
- **负责人:** `Max`
- **最后更新：** `2026-07-29`
- **分支：** `feat/gameplay-framework`
- **相关：** `[ROADMAP](../ROADMAP.md)`、`[Feature Registry](../FEATURE_REGISTRY.md)`

## TL;DR

建立局内骨架：`GameInstance` 提供应用级服务；`GameplaySubsystem` 持有 `GameState` 并驱动日循环阶段切换；各域 Manager 仅提供空壳入口。
**核心阶段逻辑用纯 C# 类实现**，以便 Unity **Edit Mode 测试**覆盖阶段迁移，无需依赖编辑器手工点 Play 验收。
本 feat **不实现** 庇护所/战斗/事件业务逻辑；`EventPhase` 首版自动跳过。

---

## 范围

### In

- `GameState` 数据结构（字段可先占位默认值）
- `GameplayPhase` 枚举与日循环阶段机（纯 C# `DayLoopDirector`）
- `GameplaySubsystem`：MonoBehaviour 薄包装，持有 `GameState`、驱动 Director、调用各 Manager 钩子
- `GameInstance`：子系统初始化、主菜单/对局模式切换骨架（场景加载可最简）
- 空壳：`ShelterManager`、`CombatManager`、`EventDirector`（仅 `OnPhaseEnter` / `OnPhaseExit` 或等价接口）
- 脚本目录：`Assets/Scripts/Bootstrap|Gameplay|Shelter|Combat`
- **Edit Mode 单元测试**：阶段顺序、天数推进、Event 跳过、非法迁移拒绝

### Out

- 饱食度、NPC、卡牌、伤害、事件抽取
- 存档读档、UI 完整流程、结局判定细节
- Play Mode 集成测试（首版非必须；见第 6 节）

---

## 现状、目标与差距

- **当前：** 无自定义玩法脚本；ROADMAP 已定义架构与 feat 顺序。
- **目标：** 可在测试中证明「新局 → 多 day 循环 → 阶段按序切换」；各 Manager 被 Subsystem 正确调度。
- **差距：** 缺少可测试的阶段机与类骨架。

---

## 设计

### 2.1 类与职责

```text
Bootstrap/
  GameInstance.cs              // 应用入口：初始化、模式切换、暴露 GameplaySubsystem 引用

Gameplay/
  GameState.cs                 // 纯 C#：day, foodStock, corruption, rngSeed, flags, currentPhase
  GameplayPhase.cs             // 枚举：None, Shelter, Combat, Event, DayAdvance, Ending
  DayLoopDirector.cs           // 纯 C#：阶段机核心（可单测）
  GameplaySubsystem.cs         // MonoBehaviour：组合 Director + 注入 Manager 钩子
  IPhaseHandler.cs             // 各阶段域入口（Shelter/Combat/Event 实现空壳）

Shelter/
  ShelterManager.cs            // 实现 IPhaseHandler（空逻辑 + 可选 Debug.Log）

Combat/
  CombatManager.cs             // 同上

Gameplay/ (或 Events/)
  EventDirector.cs             // 同上；Event 阶段首版标记 skip
```

**命名说明：** 阶段编排核心用 `DayLoopDirector`（导演），避免与 Unity 自带 `Subsystem` 概念混淆；对外仍称 Gameplay Framework。

### 2.2 GameState

```csharp
// 字段均为 camelCase（实例字段约定）
public class GameState
{
    public int day;                    // 1~6，DayAdvance 后 +1
    public int foodStock;
    public int corruption;
    public int rngSeed;
    public int population;             // 可先占位 0，后续由 Shelter 维护
    public GameplayPhase currentPhase;
    // flags：首版可用 Dictionary<string,bool> 或 HashSet<string>，后续再定型
}
```

- 由 `GameplaySubsystem` **持有唯一实例**；其他系统通过 Subsystem 或注入接口读写，禁止复制一份“影子状态”。

### 2.3 DayLoopDirector（纯 C#，可测试）

职责：

1. 持有 `GameState` 引用（构造注入）。
2. 定义标准日循环：`Shelter → Combat → Event → DayAdvance → (day<=6 ? Shelter : Ending)`。
3. `Event` 阶段：若 `eventPhaseEnabled == false`（首版默认 false），则 **不调用** EventDirector 业务，直接 `TransitionTo(DayAdvance)`。
4. 每次 `TransitionTo(next)`：
   - 调用当前 phase 对应 handler 的 `ExitPhase()`
   - 更新 `GameState.currentPhase`
   - 调用下一 phase handler 的 `EnterPhase()`
   - `DayAdvance` 内执行 `day++`，再根据 day 决定下一 phase

公开 API（供 Subsystem 与测试使用）：

```csharp
void StartNewRun(int seed);
void AdvanceToNextPhase();           // 外部“阶段完成”信号（首版可由测试或 Debug 按钮调用）
bool CanAdvance();                   // 可选：防止重复推进
GameplayPhase CurrentPhase { get; }
```

**首版不实现** 各阶段内部“完成条件”；阶段推进由显式调用 `AdvanceToNextPhase()` 驱动（后续 UI/Manager 在业务完成后调用）。

### 2.4 GameplaySubsystem（MonoBehaviour 薄层）

- `Awake`/`Start`：向 `GameInstance` 注册或由 `GameInstance` 注入 Manager 引用。
- 构造 `DayLoopDirector(gameState, handlers)`。
- 对外：`StartNewRun()`、`AdvanceToNextPhase()` 转发给 Director。
- **不在 Update 里自动切阶段**，避免难以测试与非确定性。

### 2.5 GameInstance

- 单例或场景唯一对象（项目允许单例）。
- 职责：初始化 `GameplaySubsystem`、切换 `MainMenu` / `InGame` 模式（首版可同场景 +  bool 标志）。
- **不持有** 局内业务字段（day/food 等均在 `GameState`）。

### 2.6 空壳 Manager 约定

各 Manager 实现 `IPhaseHandler`：

```csharp
public interface IPhaseHandler
{
    GameplayPhase HandledPhase { get; }
    void EnterPhase(GameState state);
    void ExitPhase(GameState state);
}
```

首版 `EnterPhase` / `ExitPhase` 仅打日志或计数（供测试断言“被调用过”）。  
`EventDirector` 在 framework feat 中即使被跳过_transition_，类仍存在以便后续 feat 填充。

### 2.7 与 ROADMAP 的 Event 延后策略一致

- `DayLoopDirector` 构造参数：`bool skipEventPhase = true`（默认 true）。
- 测试覆盖：`skipEventPhase=true` 时序列不含 Event 停留；`=false` 时含 Event 调用。

---

## 实现步骤（合并在本文，不另写 plan）

| 步骤 | 内容 | 验证 |
|------|------|------|
| S1 | 建立 `Assets/Scripts/` 目录与 asmdef（Gameplay / Bootstrap；Tests 程序集引用 Gameplay） | 编译通过 |
| S2 | `GameState`、`GameplayPhase`、`IPhaseHandler` | EditMode：状态默认值 |
| S3 | `DayLoopDirector` + 阶段迁移逻辑 | EditMode：见第 5 节用例 |
| S4 | 空壳 `ShelterManager`、`CombatManager`、`EventDirector` | EditMode：Enter/Exit 被调用 |
| S5 | `GameplaySubsystem`、`GameInstance` 骨架 | 编译通过；可选 1 条 PlayMode 冒烟 |
| S6 | Edit Mode 测试全绿 | Test Runner / CLI |

---

## 测试策略（Unity Test Framework）

### 5.1 可行性结论：**可行，且推荐**

项目已依赖 `com.unity.test-framework`（1.1.33）。  
将 **阶段机放在纯 C# `DayLoopDirector`** 后，绝大部分正确性可在 **Edit Mode** 用 NUnit 断言，无需打开场景、无需手工 Play。

| 测试类型 | 适用 | 本 feat |
|----------|------|---------|
| **Edit Mode** | 纯 C# 逻辑、状态机、数据 | **主要手段** |
| **Play Mode** | MonoBehaviour 生命周期、场景加载 | 可选 1 条冒烟 |
| 手工编辑器 | UI、手感 | 非 DoD 必须 |

### 5.2 测试程序集

```text
Assets/Tests/EditMode/
  SixDaysRemaining.EditModeTests.asmdef   // 引用 Gameplay 程序集
  DayLoopDirectorTests.cs
  GameStateTests.cs（可选）
```

### 5.3 建议测试用例（Edit Mode）

1. **StartNewRun**：`day==1`，`currentPhase==Shelter`。
2. **单次 Advance**：`Shelter → Combat`。
3. **完整一日（skip Event）**：`Shelter → Combat → DayAdvance`，`day` 变为 2，下一 phase 为 `Shelter`。
4. **第六日结束后**：`day` 变为 7 或进入 `Ending`（与实现对齐后固定断言）。
5. **skipEventPhase=false**：经过 `Event` 时 `EventDirector`  mock 的 `EnterPhase` 被调用一次。
6. **Handler 顺序**：离开 Shelter 先 `ExitPhase(Shelter)` 再 `EnterPhase(Combat)`（可用 mock/spy 记录顺序）。

### 5.4 验证命令

- **编辑器：** `Window → General → Test Runner → EditMode → Run All`
- **CI/本地批处理（可选）：** Unity `-runTests -testPlatform editmode -projectPath ...`（需本机 Unity 路径；GameJam 可先以 Test Runner 为准）

### 5.5 设计约束（为可测性）

- 阶段迁移 **不得** 写死在 `Update()` 轮询里。
- `DayLoopDirector` **不依赖** `UnityEngine.Object`；Manager 钩子用接口 + 测试注入 fake。
- `GameplaySubsystem` 仅转发；核心断言针对 Director。

---

## 风险与缓解

| 风险 | 缓解 |
|------|------|
| 逻辑写进 MonoBehaviour 导致难测 | Director 纯 C#；Subsystem 薄包装 |
| Event 跳过与日后启用行为不一致 | Director 参数化 `skipEventPhase` + 两套 EditMode 用例 |
| asmdef 配置错误导致测试程序集找不到类型 | S1 与 S6 作为门禁 |

---

## 验收清单

- [ ] `GameState` / `DayLoopDirector` / 空壳 Manager / Subsystem / GameInstance 就位
- [ ] Edit Mode 测试全绿（第 5.3 节用例覆盖）
- [ ] Console 无编译错误
- [ ] `FEATURE_REGISTRY` 中 `CORE-F02` 状态同步
- [ ] `PROGRESS_LOG` 追加本 feat 条目

## 审阅通过后

1. 从 `main` 拉 `feat/gameplay-framework`
2. 按本文「实现步骤」开发；每步保持测试绿
3. 完成后 prepare commit → 负责人批准 → merge `main`
