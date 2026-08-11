# EVT-F01 GameEventSubsystem + 同质事件模型

## 元信息

- **ID:** `EVT-F01`
- **类型:** `Feature`
- **状态:** `Draft`
- **负责人:** `Max`
- **最后更新：** `2026-08-11`
- **分支（建议）：** `feat/events`
- **相关：** `SHLT-F02`、`CORE-F04`、`COMB-F09`、`FEATURE_REGISTRY.md`

## TL;DR

当前随机事件只是 `RandomEventCatalog` 的静态原型池，且调度与效果应用都写在 `AppFlowController`。  
本 feat 先把“随机事件”统一为同一种 **`GameEventDef`**，由新的 **`GameEventSubsystem`** 负责调度、排队、结果应用；UI 只负责展示与回传选择。  
（幸存者特有事件的调度/触发规则留到 **F02**；这里先预留扩展接口与数据模型字段。）  
第一版只把系统骨架、fragment 效果、数据导入和时机钩子建起来，**分池规则先留扩展口**。

---

## 范围

### In

- `GameEventSubsystem`：负责事件收集、排序、排队、应用结果
- `GameEventDef` / `GameEventOptionDef` / `GameEventEffectFragment` 数据模型
- 先重构随机事件（幸存者特有事件留到 F02）
- `StreamingAssets/Events/` 数据驱动导入与校验
- `RandomEventView` 重命名为 `GameEventView`，作为通用事件展示层
- 少量固定触发时机：`AfterTriumph`、`BeforeDayEnd`、`PrepStart`

### Out

- 完整四池（腐蚀×人口）分池算法
- 长线剧情完整内容生产
- 对话树编辑器
- 事件美术资源管理系统

---

## 现状、目标与差距

- **当前行为：** `AppFlowController` 自己维护 `pendingEvents`，凯旋后调用 `RandomEventCatalog.PickSequence()`，并在 `OnRandomEventChosen()` 中直接改 food/corruption/TakeIn/Expel；同时它还作为 UI 组件存在于 `UI/` 目录中，职责过重。
- **目标行为：** UI 只消费 `GameEventSubsystem` 的“当前事件”和“提交选项”接口；F01 先保证随机事件通过 fragment 落地。
- **差距：** 没有 subsystem、没有事件 effect fragment、没有 JSON 内容源（F02 扩展幸存者事件的调度规则在后续实现）。

---

## Design

### Option A（recommended）

- 描述：
  - 新增 `GameEventSubsystem`，归 `Gameplay` 域持有
  - 新增 `GameEventDef` / `GameEventOptionDef` / `GameEventEffectFragment`
  - `AppFlowController` 保留在 `UI/`，只保留：请求展示、提交选项、继续下一个事件（不再持有队列与效果落地逻辑）
  - `RandomEventCatalog` 退役，或仅保留临时兼容入口
- 好处：
  - UI 与事件逻辑解耦
  - 随机事件与未来扩展事件可以共用同一模型
  - 数据驱动和调试命令都更自然
- 风险：
  - 首次建模会牵动 Gameplay、Shelter、UI 三域接口

### Option B

- 描述：保留 `RandomEventDef` / `RandomEventOption`，仅把调度队列搬到 subsystem
- 为什么没选：
  - 选项效果仍会膨胀成越来越多的字段
  - 未来扩展事件的建模形态仍可能与随机事件不同（但本阶段只保证随机事件可用）

---

## 设计细节

### 1) 子系统职责

`GameEventSubsystem` 负责：
- 从内容库中收集候选事件
- 根据触发时机和条件过滤事件
- 将事件排队成 `EventQueue`
- 应用选项效果 fragment
- 暴露当前事件、是否还有后续事件、执行结果
- 通过委托/事件向外广播“事件队列已刷新 / 当前事件变更 / 事件链结束 / 结果已应用”

`AppFlowController` 负责：
- 在正确时机向 subsystem 请求事件序列
- 订阅 subsystem 的广播，而不是自己保存 `pendingEvents`
- 把当前事件交给 `GameEventView`
- 把用户选项索引回传给 subsystem
- 根据 subsystem 返回值刷新 HUD / 切屏 / 进 Ending

推荐广播方式：

```csharp
public event Action<GameEventDef> CurrentEventChanged;
public event Action<IReadOnlyList<GameEventDef>> EventQueuePrepared;
public event Action<GameEventResult> EventResolved;
public event Action EventSequenceFinished;
```

这样 UI 层只订阅和展示，不拥有事件队列本身；未来若接日志、音效、埋点，也可以复用同一广播面。

### 2) 同质事件模型

建议：

```text
GameEventDef
  id
  title
  body
  trigger
  priority
  requiredSurvivorIds[]
  requiredFlags[]
  options[]

GameEventOptionDef
  id
  label
  resultText
  effects[]
```

示例 effect fragment：
- `FoodDelta`
- `CorruptionDelta`
- `TakeInSurvivor`
- `ExpelSurvivor`
- `SetFlag`
- `ClearFlag`
- `OverrideHungerDecay`
- `JumpToEnding`

### 3) 幸存者事件扩展口（F02）

不是单独另一套 View，而是 subsystem 内一层 candidate provider（在 F02 再加入对应 provider）：
- `RandomPoolProvider`
- `SurvivorEventProvider`（F02 再实现）

### 4) 分池留口

第一版不做完整四池，但模型中预留：
- `poolId`
- `corruptionRange`
- `populationRange`
- `weight`

这样后续加四池时不用重写 `GameEventDef`。

### 5) 与 trait / defId 的关系

`SHLT-F02` 已有 `Survivor.defId`，因此事件系统优先按 `defId` 挂接，不按中文显示名做字符串匹配。

### 6) View 与目录归属

- 现有 `RandomEventView` 应重命名为 `GameEventView`
- 原因：
  - 我们已经决定复用同一套 UI 展示随机事件以及未来扩展事件
  - “Random” 已经不准确，会误导后续维护者
- `GameEventView` 仍可暂时留在 `UI/` 目录；但 `AppFlowController` 只作为桥接，不再是事件队列/效果落地逻辑的拥有者

### 7) 推荐数据流

```text
AfterTriumph / PrepStart / BeforeDayEnd
  -> AppFlowController 请求 GameEventSubsystem
  -> providers 收集候选
  -> subsystem 排队并广播当前事件
  -> GameEventView 展示
  -> 用户选项 -> subsystem.ApplyOption()
  -> fragment 修改 Gameplay / Shelter / flags
  -> subsystem.Next()
```

---

## 实现注意点

- 影响的关键模块：
  - `Assets/Scripts/Gameplay/RandomEventCatalog.cs`
  - `Assets/Scripts/UI/AppFlowController.cs`（先保留目录，但要瘦身：剥离事件队列/效果落地）
  - `Assets/Scripts/UI/RandomEventView.cs`（重命名为 `GameEventView`）
  - `Assets/Scripts/Shelter/ShelterManager.cs`
  - 新增 `Assets/Scripts/Gameplay/Events/`
  - 新增 `Assets/StreamingAssets/Events/`
- 旧路径的迁移/删除计划：
  - `pendingEvents` / `pendingEventIndex` 从 `AppFlowController` 移出
  - `RandomEventOption` 的硬编码字段逐步退役
- 兼容性假设：
  - `GameEventView` 先保持 3 按钮上限，内容模型先不强逼无限选项 UI

## 验证

- 构建/验证命令：Unity Play in `SampleScene`
- 测试命令（可多条）：
  - Edit Mode：事件加载、条件过滤、fragment 应用
  - Play：凯旋后事件链、day end 继续流程
- 人工核对点：
  - `AppFlowController` 不再直接应用 food/corruption/takein/expel
  - 随机事件能走同一 `GameEventView` 和 continue 流
  - 将来加入四池时不需要重写 View 或 option 模型

## 验收清单

- [ ] 已有原型随机事件迁入 `GameEventSubsystem`
- [ ] 随机事件迁入 `GameEventSubsystem` + fragment 化
- [ ] 选项效果改为 fragment 组合
- [ ] `StreamingAssets/Events/` 加载与校验可用
- [ ] `AppFlowController` 只保留展示/转发职责
- [ ] 已更新进度日志
- [ ] Feature 注册表状态已同步
