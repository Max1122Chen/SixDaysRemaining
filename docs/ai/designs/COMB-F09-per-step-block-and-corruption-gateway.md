# COMB-F09 每步格挡结算 + Corruption Gateway

## 元信息

- **ID:** `COMB-F09`
- **类型:** `Refactor`
- **状态:** `Done`（实现于 `40bdf58`；2026-08-12 文档收口）
- **负责人:** `Max`
- **最后更新：** `2026-08-12`
- **分支：** 已合 `main`（原建议名 `feat/combat`）
- **相关：** `COMB-F02`、`COMB-F07`、`CORE-F04`、`FEATURE_REGISTRY.md`

## TL;DR

当前 Block 在整轮 5 槽结束后才清空，语义更接近“整回合护盾池”；这与目标中的“每一步像猜拳一样即时生效”不一致。  
同时，腐蚀虽然大多通过 `GameplaySubsystem.ApplyCorruption()` 修改，但 debug 初始化、战斗结算与个别 UI 胶水的边界仍不够统一。  
本 feat 统一两件事：**Block 改为每步结算后清空**，**Corruption 改为单一写入口 + 强制夹值**。

---

## 范围

### In

- `CombatManager` 的每步结算语义重构
- Player / Enemy block 清零时机改为 slot 级，而不是 round 级
- 统一 corruption 写入口，Debug / Event / Combat 结算不再直接写 `GameState.corruption`
- 明确腐蚀夹值：最小 0，最大 `CorruptedRules.FuseThreshold`
- 为后续 Debug 控制台与事件 fragment 暴露稳定写口

### Out

- Corrupted 数值重平衡
- Trait 效果重写
- 战斗 UI 样式改造
- 卡牌 / 遭遇 JSON schema 变化

---

## 现状、目标与差距

- **当前行为：** `CombatManager.EndRound()` 才 `SetBlock(0f)`；`GameplaySubsystem.ApplyCorruption()` 有夹值，但 `GameInstance.ApplyDebugStartCorruption()` 仍直接写字段。
- **目标行为：** 每个 slot 结算后 block 立即失效；任何腐蚀写入都经过统一 gateway。
- **差距：** 战斗步进时机与数值网关尚未统一；现有接口名称也没表达“谁拥有腐蚀真相”。

---

## Design

### Option A（recommended）

- 描述：
  - 在 `CombatManager.ResolvePlayerSlot()` / `ResolveEnemySlot()` 所在的每步流程中，加入明确的 **step boundary**
  - Player 行动后清理双方应失效的 block，再进入 enemy 行动；enemy 行动后再次清理，进入下一 slot
  - `GameInstance` / `AppFlowController` / 战斗结算统一改为调用 `ApplyCorruption()` 或新的更清晰命名网关
- 好处：
  - 规则语义和你的设计一致
  - 腐蚀来源统一，后续加 Debug 命令与事件 fragment 更简单
- 风险：
  - 需要重新核对现有防御牌、Trait、休眠等在每步边界的展示与体感

### Option B

- 描述：保留现有 `ResolvePlayerSlot()` / `ResolveEnemySlot()` 结构，只在 UI coroutine 中插入清 block 调用
- 为什么没选：
  - 规则会被 UI 驱动，逻辑层不再自洽
  - BattleOnly / 测试入口更容易跑出不一致结果

---

## 设计细节

### 1) 每步边界

当前顺序：
1. `CombatView` 调 `ResolvePlayerSlot(i)`
2. `CombatView` 调 `ResolveEnemySlot(i)`
3. 整轮结束后 `EndRound()` 清 block

推荐顺序：
1. `ResolvePlayerSlot(i)`
2. `CombatManager` 内或新增 `ResolveStep(i)` 中清理本步残留 block
3. `ResolveEnemySlot(i)`
4. 再清理本步残留 block
5. 进入下一 slot

这意味着“防御只保护当前这一步”，不会跨到下一张牌或下一次敌行动。

### 2) Corruption Gateway

现有良好入口：
- `GameplaySubsystem.ApplyCorruption(int delta)`
- `GameplayCorruptionBridge : ICorruptionRunState`

问题点：
- `GameInstance.ApplyDebugStartCorruption()` 直接写 `Gameplay.State.corruption`
- `AppFlowController` 自己裸改 `foodStock`，腐蚀却经 gateway，边界不对称
- `CombatManager.Finish()` 在没有 `runCorruption` 时会先攒局部 `corruption`，再写进 `CombatResult`

推荐做法：
- `GameplaySubsystem` 保持真相源
- 将“设置绝对值”和“写入 delta”都纳入统一入口，例如：
  - `ApplyCorruption(int delta)`
  - `SetCorruption(int value)`
- `GameInstance` debug 初始化改用 `SetCorruption()`
- 事件系统、Debug 命令、战斗桥接只走 gateway，不再直接碰 `State.corruption`

为什么不是“只暴露一个 setter”就够了：当前所有写入都要满足同一套语义（夹值 `0..FuseThreshold`、达到熔断阈值要进入 `Ending`，且 `ApplyCorruption(delta)` 需要返回是否熔断），同时战斗侧是纯 C# 流程，通过 `ICorruptionRunState` / `GameplayCorruptionBridge` 做依赖倒置，避免把 Unity `MonoBehaviour`/UI 细节泄漏到规则计算链路里。

### 3) 兼容 Corrupted 熔断

保留现有规则：
- 达到 `FuseThreshold` 立即进入 Ending
- 所有入口都统一遵守 `0..FuseThreshold`

### 4) 与后续重构关系

本 feat 不解决名字碎片解锁 trait，但会让 trait/debug/event 改腐蚀时都依赖统一接口，减少后续面向 subsystem 重构的摩擦。

---

## 实现注意点

- 影响的关键模块：
  - `Assets/Scripts/Combat/CombatManager.cs`
  - `Assets/Scripts/Combat/CombatComponent.cs`
  - `Assets/Scripts/Gameplay/GameplaySubsystem.cs`
  - `Assets/Scripts/Bootstrap/GameInstance.cs`
  - `Assets/Scripts/UI/CombatView.cs`
  - `Assets/Scripts/UI/AppFlowController.cs`（无论目录归属，腐蚀写入都经 gateway/统一接口）
- 旧路径的迁移/删除计划：
  - 删除或弱化 `EndRound()` 中的“唯一清 block 时机”职责
  - 删除 direct write `Gameplay.State.corruption = ...`
- 兼容性假设：
  - 现有战斗 UI 仍按 slot 动画驱动，不需要重画流程
  - 与 `CORE-F04` / `EVT-F01` 配合后，UI 对腐蚀修改应只经委托/网关反馈，而非再直接拼写字段

## 验证

- 构建/验证命令：Unity Play in `SampleScene`
- 测试命令（可多条）：
  - Edit Mode：block 每步清零、腐蚀 clamp、fuse 终局
  - Play：防御牌只挡当前步；腐蚀不会降到 0 以下
- 人工核对点：
  - 玩家防御后，下一步开始 block 已清空
  - 敌方防御同理
  - Debug、事件、战斗奖励都不会把腐蚀写成负数或超过 100

## 验收清单

- [x] Block 语义改为每步内生效（`ResolvePlayerSlot` / `ResolveEnemySlot` 后清对应侧 block）
- [x] Corruption 写入口统一到 `ApplyCorruption` / `SetCorruption`（GameInstance debug 起局已改）
- [x] `debugStartCorruption` / `DebugRunSettings.startCorruption` 不再直接写裸字段
- [x] Edit Mode：`CombatRoundFlowTests.ResolveStep_ClearsDefendingSideBlockPerAction`；`GameplaySetCorruption_ClampsAndFusesAtThreshold`
- [x] 已更新进度日志
- [x] Feature 注册表状态已同步

## 收口备注（2026-08-12）

- `EndRound()` 仍清双方 block：作为回合末兜底，与每步清零不冲突。
- BattleOnly / 无 `RunCorruption` 时，`CombatManager` 仍可在结算结果里累计局部腐蚀 delta，局内真相源仍是 `GameplaySubsystem`。
- design 文中 Bootstrap 路径已迁至 `App/`；以代码为准。
