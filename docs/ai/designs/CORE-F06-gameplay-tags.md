# CORE-F06 GameplayTag 基础设施

## 元信息

- **ID:** `CORE-F06`
- **类型:** `Feature`
- **状态:** `Review`（基础设施已实现，待 EditMode 验证）
- **负责人:** `Max`
- **最后更新：** `2026-08-12`
- **实现分支：** 审阅通过后切到 **`main`** 实现（按你的要求）
- **相关：** `CORE-F05`、`EVT-F02`、`FEATURE_REGISTRY.md`

## TL;DR

在 `Gameplay` 域新增一套 **GameplayTag 基础设施**，用于表达运行中状态，而不是继续把业务状态散落成事件专用 flag。  
首版对齐你拍板的方向：**层级匹配 + Count/Stack + 组合查询（All/Any/None）**，但**只交付基础设施，不迁具体业务**。  
也就是说，本 feat 结束后会有一套可被 `Flow / Events / Debug / Shelter` 消费的 Tag Runtime，但 `EVT-F02` 里的幼童/政治家逻辑先不改，后续再单独开迁移 slice。

---

## 背景

当前剧情状态主要挂在 `GameplaySubsystem` 内的 `storyFlags`：

- `GameplaySubsystem.HasStoryFlag()`
- `GameplaySubsystem.SetStoryFlag()`
- `GameplaySubsystem.ClearStoryFlag()`

而 `AppFlowController`、`EventRequirements` 会直接判断具体事件名语义的 flag，例如：

- `child_play_promised`
- `child_stone_declined_d2`
- `child_stone_declined_d3`

这样会带来两个问题：

1. **消费时机脆弱**：例如幼童陪玩问题里，flag 清除时机放错就会漏门禁。
2. **事件语义耦合到业务系统**：改事件结构时，Flow / UI / EventRequirements 里的判断也要跟着改。

---

## 范围

### In

- 新增 `GameplayTag` 值对象 / 规范化命名
- 新增 `GameplayTagContainer`（运行时容器）
- 支持 **层级匹配**
- 支持 **Count / Stack**
- 支持 **组合查询**：`All` / `Any` / `None`
- 在 `GameplaySubsystem` 接入 Tag 容器与 façade API
- EditMode 测试覆盖解析、层级、计数、查询、生命周期
- 文档 / Registry / Progress 更新

### Out

- 把 `EVT-F02` 当前 `storyFlags` 立即迁成 Tag
- 事件 JSON 新增 `AddTag` / `RemoveTag` fragment
- GameplayTag 编辑器、资产表、自动补全
- Save/Load 持久化升级
- 调试命令接入 Tag（可后续跟进）

---

## 设计目标

### 1) 用“原语状态”而不是“事件名状态”

后续希望让系统消费的是这种通用状态：

- `State.ForbiddenExpedition.Once`
- `State.SkipCombat.Once`
- `State.CorruptionDiscount.Today`

而不是直接消费：

- `child_play_promised`
- `politician_knock_seen`

剧情进度本身仍然可以有语义化 Tag，但它们应该更多留在事件域内部使用，而不是泄漏到 `Flow` / `UI`。

### 2) 保持基础设施先行

本 feat 不直接重写 `EVT-F02`，否则会把“基础设施”与“业务迁移”耦在一起，增大风险。  
先把 CORE 层能力打好，再做一次小范围迁移验证（例如仅迁幼童陪玩禁出征）。

---

## Design

### Option A（采纳）: Gameplay 域提供独立 Tag Runtime

```text
GameplaySubsystem
  └─ GameplayTagContainer
       ├─ Add/Remove/GetCount
       ├─ HasTag / HasTagExact
       ├─ HasAll / HasAny / HasNone
       └─ Match parent/child hierarchy
```

- 优点：
  - 与 `GameState` / 运行时阶段系统同域，职责自然
  - 不依赖事件域，可被 Flow / Debug / Shelter / EventRequirements 共用
  - 后续迁移旧 `storyFlags` 时只改调用方，不改底层模型
- 风险：
  - 首版若做太像 UE/GAS，容易把基础设施做大

### Option B（不采纳）: 继续在 `Events/` 域做 flag/tag 容器

- 问题：
  - Tag 会被 Flow / Debug / Shelter 用到，放 `Events` 会倒置依赖
  - 不符合“CORE 先提供通用状态原语”的方向

### Option C（不采纳）: 直接在 `GameState` 上堆字符串数组

- 问题：
  - 无法优雅支持 Count / 层级 / Query
  - 会把解析、匹配、计数逻辑散落到调用方

---

## 数据结构

### `GameplayTag`

建议为轻量值对象，内部仍存规范化字符串：

```text
Name = "State.ForbiddenExpedition.Once"
Segments = ["State", "ForbiddenExpedition", "Once"]
```

建议 API：

- `GameplayTag.Parse(string raw)`
- `GameplayTag.TryParse(...)`
- `MatchesExact(other)`
- `MatchesTag(other)`  
  例如：
  - `State.ForbiddenExpedition.Once` matches `State.ForbiddenExpedition`
  - `State.ForbiddenExpedition` does **not** exact-match child

### 命名规则

- 用 `.` 表示层级
- 每段建议 PascalCase
- 禁止：
  - 空字符串
  - 首尾 `.` 
  - 连续 `..`

示例：

- `State.ForbiddenExpedition.Once`
- `Story.ChildStone.Declined.Day2`
- `State.SkipCombat.Once`

### `GameplayTagContainer`

核心存储建议为：

```text
Dictionary<string, int> tagCounts
```

其中：

- key = 规范化 tag string
- value = count / stack

建议 API：

- `AddTag(GameplayTag tag, int count = 1)`
- `RemoveTag(GameplayTag tag, int count = 1)`
- `GetCount(GameplayTag tag)`
- `HasTagExact(GameplayTag tag)`
- `HasTag(GameplayTag tag)`（支持层级）
- `HasAll(...)`
- `HasAny(...)`
- `HasNone(...)`
- `ToSnapshot()`

### `GameplayTagQuery`

首版建议做一个**三段式查询对象**，先不要过度树状化：

- `All`
- `Any`
- `None`

这样已经能覆盖绝大多数业务判断，例如：

- 必须有 `State.ForbiddenExpedition`
- 不能有 `State.SkipCombat`
- 至少有 `Story.ChildStone.Declined.Day2` / `Day3` 之一

若以后确实需要 GAS 式复杂嵌套，再扩展成表达式树。

---

## 接入点

### 1) `GameplaySubsystem`

从当前：

- `private readonly HashSet<string> storyFlags`

扩展为：

- `private readonly GameplayTagContainer gameplayTags`

但**本 feat 不删掉 `storyFlags`**。  
建议短期并存：

- `storyFlags`：旧逻辑继续跑
- `gameplayTags`：新基础设施 ready

建议新增 façade API：

- `AddTag(string tag, int count = 1)`
- `RemoveTag(string tag, int count = 1)`
- `HasTag(string tag)`
- `HasTagExact(string tag)`
- `GetTagCount(string tag)`
- `MatchesQuery(GameplayTagQuery query)`
- `GetTagSnapshot()`

同时在 `StartNewRun()` 时清空 Tag 容器。

### 2) `GameState`

建议 **暂不** 直接把 Tag 数据裸存到 `GameState` 字段上。  
原因：

- 当前 `GameState` 还是轻量运行状态 DTO
- Tag 容器包含计数与查询行为，更适合放在 `GameplaySubsystem`
- 以后若要存档，再评估是否把快照序列化进 `GameState`

---

## 与现有系统的边界

本 feat 后，下面这些现有点先**不改业务**：

- `AppFlowController.OnDepart()` 里对 `child_play_promised` 的判断
- `EventRequirements` 对 `RequiredFlags` 的判定
- `RunStoryFlags` 常量
- `events.json` 里的 `SetFlag` / `ClearFlag`

理由：

- 你已经明确这轮只做 CORE 基础设施
- 若现在就改，会把 feat 膨胀为 `CORE + EVT` 联动迁移

后续推荐再开一条迁移 slice，把：

- `child_play_promised`

迁成：

- 事件效果产出 `State.ForbiddenExpedition.Once`
- Flow 门禁消费 `State.ForbiddenExpedition`

---

## 测试计划

新增 `Assets/Tests/EditMode/GameplayTagTests.cs`，至少覆盖：

1. `Parse` / `TryParse`
2. 层级匹配：
   - child matches parent
   - parent 不 exact-match child
3. Count：
   - `AddTag` 多次叠加
   - `RemoveTag` 递减
   - 归零后删除
4. 查询：
   - `HasAll`
   - `HasAny`
   - `HasNone`
5. 生命周期：
   - `StartNewRun()` 清空 container
6. 快照：
   - 外部拿到的快照不会反写内部容器

---

## 实现切片（建议）

| Slice | 内容 |
|-------|------|
| S1 | 新增 `GameplayTag` / `GameplayTagContainer` |
| S2 | 新增 `GameplayTagQuery`（`All/Any/None` 三段式） |
| S3 | `GameplaySubsystem` 接入容器 + façade API + `StartNewRun` 清空 |
| S4 | `GameplayTagTests` EditMode 覆盖 |
| S5 | 文档 / Registry / Progress 收口 |

---

## 验收清单

- [x] `Gameplay` 域存在独立 Tag Runtime，不依赖 `Events`
- [x] 支持层级匹配
- [x] 支持 Count / Stack
- [x] 支持 `All/Any/None` 组合查询
- [x] `GameplaySubsystem` 暴露只读消费 API
- [x] `StartNewRun()` 会清空 Tag 容器
- [x] EditMode 测试覆盖解析、层级、计数、查询、生命周期
- [x] **不修改** 现有 EVT/Flow 业务逻辑

---

## 审阅后下一步

若你审批通过：

1. 切到 `main`
2. 实现 `CORE-F06`
3. 完成后再回来看一个小迁移 feat，把 `EVT-F02` 的幼童禁出征链路改成 Tag 原语消费
