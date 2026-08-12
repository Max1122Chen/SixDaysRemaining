# 进度日志（Progress Log）

追加式、按时间顺序的项目事实记录（append-only）。

## 2026-08-12（EVT-F01 实现）

- 范围：独立 Events 域 + Flow 瘦身 + JSON 事件
- 已完成：
  - `Assets/Scripts/Events/` + `SixDaysRemaining.Events`（子系统、Provider、JSON loader）
  - `StreamingAssets/Events/events.json`（三则 AfterTriumph）；退役 `RandomEventCatalog`
  - AppFlow 三钩子：`AfterTriumph` → `BeforeDayEnd` → 日结；`BeforeDepart` 进庇护所后
  - `GameEventView` 替换 `RandomEventView`；全日共享 cap=3
  - EditMode：`GameEventSubsystemTests`；`TD-003` / `TD-007` → Resolved
- 待验证：MainScene Play 日循环 + 事件链

## 2026-08-12（EVT-F01 设计修订 + 开分支）


- 范围：`EVT-F01` 审阅拍板写入 design；开 `feat/events`
- 已拍板：
  - 三钩子均接线（AfterTriumph / BeforeDayEnd / PrepStart；后两者可空）
  - 全日最多 3 事件，跨时机共享额度
  - 独立 `Assets/Scripts/Events/` + `SixDaysRemaining.Events` asmdef
  - 首版 fragment：Food/Corruption/TakeIn/Expel/JumpToEnding
- 下一步：二次审阅 design 文末「请确认」四项后实现

## 2026-08-12（文档收口：F04 / F09 Done）


- 范围：可信计划源状态同步
- 已完成：
  - `CORE-F04` → Done（Scene GameInstance + Hybrid Debug；抽测通过后已合 main）
  - `COMB-F09` → Done（核对代码 vs design：每步清 block + Corruption Gateway 已在 `40bdf58`）
  - `ACTIVE_WORK`：下一 P0 为 `EVT-F01`（承接 `TD-007`）
  - 更新 FEATURE_REGISTRY / design 验收清单 / PROJECT_CONTEXT / BOOTSTRAP_DIGEST
- 下一步：审阅 `EVT-F01` design 后实现

## 2026-08-11（CORE-F04 命令与门禁实现）

- 范围：Hybrid Debug 命令表 + 业务 API + gate
- 已完成：
  - Gameplay：`SetFood`、`ForceEnding`、`SetDay→MaxDay`、`RunSnapshot`
  - Shelter：`TryResolveSurvivor`、`Adjust/SetSurvivorHunger`、`ExpelSurvivor`
  - Combat：`PlayerInvincible`、`CombatSweep`、`ForceOutcome`、`ApplyEffectInCurrentCombat`
  - AppFlow：`BeginDayEnd`、`ForceEndingFlow`、skipCombat 分支；DebugRunSettings.combatSweep
  - Debug：`DebugCommandGates` + 全命令表；移除 `run.phase set`；Tab/help 按 gate 过滤
  - EditMode：`DebugCommandRegistryTests` 扩展
- 待验证：MainScene Play + EditMode 全绿

## 2026-08-11（CORE-F05 实现）

- 范围：AppFlow / Presentation 拆分
- 已完成：
  - `Gameplay/AppFlowController.cs`：日循环编排 + presentation 委托；**编译在 App 程序集**（打破 App↔Gameplay 循环依赖），命名空间仍为 `SixDaysRemaining.Gameplay`
  - `UI/PresentationManager.cs`：切屏 / Overlay / HUD / View Wire
  - `UiSceneBootstrap`：创建 Flow + Presentation，`BindGame` / `Bind` / Debug 回调
  - 删除 `UI/AppFlowController.cs`；各 View 改 `using SixDaysRemaining.Gameplay`
  - `Gameplay.asmdef` 增引 `App` + `Shelter`；登记 `TD-007`（事件队列留 Flow 至 EVT-F01）
- 待验证：MainScene Play 日循环回归
- 下一步：`CORE-F04` 业务 API + 门禁 + debug 命令收口

## 2026-08-11（CORE-F05 设计登记）

- 范围：`AppFlowController` 职责拆分
- 已完成：
  - 登记 `CORE-F05`；起草 `designs/CORE-F05-appflow-presentation.md`
  - 拍板方向：保留 `AppFlowController` 名并迁出 `UI/`；新增 `PresentationManager`；委托链接
  - `ACTIVE_WORK`：F05 置 P0；F04 调为 P1（编排边界依赖 F05）
- 下一步：
  - 审阅 F05（目录 App vs Gameplay、事件最小清理、与 F04 顺序）后再实现

## 2026-08-11（设计）

- 范围：重构 / 系统性补强 design intake
- 已完成：
  - 登记 `CORE-F04`、`COMB-F09`，并将 `EVT-F01` 提升为 `Draft`
  - 起草 3 份 design：场景化 `GameInstance` + Hybrid Debug、每步 block + corruption gateway、`GameEventSubsystem` + fragment 事件模型
- 下一步：
  - 审阅 design 后再实现

## 2026-08-11

- 范围：`SHLT-F02` 收口 + `COMB-F08` 合 main
- 已完成：
  - `feat/shelter` rebase 伙伴 UI 并 fast-forward 合入 `main`（`042a356`）
  - `feat/combat` rebase `main`；解决 `StreamingAssets.meta` / 文档冲突
  - F02 + F08 文档 → Done
- 下一步：
  - 重构 / 系统性补强（特质 defId、内容管线统一等）

## 2026-08-07

- 范围：`SHLT-F02` 实现（身份数据驱动）
- 已完成：
  - `StreamingAssets/Shelter/{survivors,starter}.json`：五人目录；开局幼童+运动员
  - `ShelterContent` / `ShelterContentJsonLoader`（硬失败）；`Survivor.defId` + 耐饿提案 A
  - `TakeIn(defId)`；移除 Alice/Bob；事件收留改为 `politician`
  - Edit Mode：`ShelterSurvivorIdentityTests` + 更新既有 Shelter 测试

## 2026-07-29

- 范围：`TECH-F01/TECH-F01-S01`
- 已完成：
  - 初始化了该 Unity GameJam 仓库的 `docs/ai/` 协作工作流。
  - 增加了 Cursor 规则：计划可信分层、工作流触发点，以及 Unity/C# 代码约定。
  - 注册了 bootstrap feature 以及首个核心循环规划 feature。
  - 创建了 workflow bootstrap 与 design intake 的 draft 设计/实现计划文档。
- 验证：
  - 检查了项目结构，并确认当前处于 Unity `2022.3.62f3c1` 的早期 bootstrap 阶段。
  - 验证 `Assets` 下尚无自定义玩法脚本。
- 文档已更新：
  - `docs/ai/PROJECT_CONTEXT.md`
  - `docs/ai/BOOTSTRAP_DIGEST.md`
  - `docs/ai/WORKING_WITH_AI.md`
  - `docs/ai/ACTIVE_WORK.md`
  - `docs/ai/FEATURE_REGISTRY.md`
  - `docs/ai/TECH_DEBT.md`
  - `docs/ai/designs/TECH-F01-agent-workflow-bootstrap.md`
  - `docs/ai/plans/TECH-F01-agent-workflow-bootstrap-plan.md`
  - `docs/ai/designs/CORE-F01-first-playable-core-loop.md`
  - `docs/ai/plans/CORE-F01-first-playable-core-loop-plan.md`
- 下一步行动：
  - 提供或指出实际的游戏设计文档来源，使 `CORE-F01` 能从假设转成可落地的 slice 级实现计划。

## 2026-07-29（晚）

- 范围：技术架构对齐 + `ROADMAP.md`
- 已完成：
  - 与负责人对齐系统拆分：`GameInstance`、`GameplaySubsystem`、`ShelterManager`、`CombatManager`、`EventDirector`。
  - 明确数据归属：全局 `RunState`；战斗收获暂存于 `CombatManager`；腐蚀度挂在编排层而非战斗实体。
  - 创建 `docs/ai/ROADMAP.md`（技术设计大纲 + Phase 0~4 + `main`/`feat/*` 分支策略）。
  - 移除 `CORE-F01` 草案文档；更新 `ACTIVE_WORK`、`FEATURE_REGISTRY`、`PROJECT_CONTEXT`、`README` 等。
  - 产品设计源关联至 `docs/designs/六日英雄—技术演示文档.pdf`。
- 验证：
  - 文档链路检查：`BOOTSTRAP_DIGEST` -> `ROADMAP` -> `ACTIVE_WORK` 一致。
- 文档已更新：
  - `docs/ai/ROADMAP.md`（新增）
  - `docs/ai/ACTIVE_WORK.md`、`FEATURE_REGISTRY.md`、`TECH_DEBT.md`、`PROJECT_CONTEXT.md`、`BOOTSTRAP_DIGEST.md`、`README.md`
  - 删除 `docs/ai/designs/CORE-F01-*`、`docs/ai/plans/CORE-F01-*`
- 下一步行动：
  - 负责人审阅 `ROADMAP.md` 第 9 节清单；通过后开始 Phase 0（`feat/tech-game-instance`）。

## 2026-07-29（夜）

- 范围：`ROADMAP` 修订 + `CORE-F02` 设计
- 已完成：
  - 统一 `GameState` 命名；feat 顺序改为 gameplay-framework → shelter → combat；事件延后。
  - 编写 `CORE-F02-gameplay-framework.md`：纯 C# `DayLoopDirector` + Edit Mode 测试策略。
  - 明确本 feat 不另写 plan；验收以 Unity Test Framework Edit Mode 为主。
- 下一步行动：
  - 负责人审阅并批准 commit；再批准拉 `feat/gameplay-framework`。

## 2026-07-29（夜·修订）

- 范围：`CORE-F02` 设计简化
- 已完成：
  - 去掉 `DayLoopDirector` / `IPhaseHandler`；由 `GameplaySubsystem` 直管阶段枚举。
  - 阶段语义改为：出征准备（ExpeditionPrep）→ 战斗 → 凯旋（TriumphReturn）→ 次日或结局。
  - 测试重点改为模拟抽象一日流程；文件少拆。
  - 同步更新 `ROADMAP.md`、`TECH_DEBT.md`。
- 下一步行动：
  - 负责人审阅修订后的 `CORE-F02`；通过后开 `feat/gameplay-framework`。

## 2026-07-29（实现）

- 范围：`CORE-F02` / `feat/gameplay-framework`
- 已完成：
  - 新增 `GameState`、`GameplayPhase`、`GameplaySubsystem`（纯 C# 阶段机）。
  - 新增 `GameInstance` 单例骨架（持有 Subsystem、主菜单/对局模式）。
  - 新增 Edit Mode 测试：准备→战斗→凯旋→次日，以及第六天后 Ending。
  - 配置 Gameplay / Bootstrap / EditModeTests asmdef。
- 验证：
  - Unity Test Runner EditMode：`GameplayFlowTests` 6 项全绿。
  - 修复 EditModeTests asmdef 与 `TestAssemblies` 重复引用导致的编译失败。
- 下一步行动：
  - merge `feat/gameplay-framework` → `main`；启动 `SHLT-F01` 设计讨论。

## 2026-07-29（Shelter 实现）

- 范围：`SHLT-F01` / `feat/shelter`
- 已完成：
  - 设计师反馈摘要：`docs/designs/designer-feedback-2026-07-29.md`
  - `SHLT-F01` 设计文档与 Shelter 代码骨架。
  - `Survivor` / `SurvivorStatus`、`ShelterManager`（入库、分配、日结、状态机）。
  - `GameInstance` 集成与 Debug 方法。
  - Edit Mode 测试 `ShelterManagerTests`。
- 验证：
  - Unity Test Runner EditMode：`ShelterManagerTests` + `GameplayFlowTests` 全绿。
  - 修复 Shelter 相关 `.meta` GUID 无效导致程序集被忽略的问题。
- 下一步行动：
  - merge `feat/shelter` → `main`；启动 `COMB-F01` 战斗设计。

## 2026-07-29（Shelter 合并）

- 范围：`SHLT-F01` merge → `main`
- 已完成：fast-forward 合并 `feat/shelter`（`0900f2c`）。
- 下一步行动：讨论并编写 `COMB-F01` 设计。

## 2026-07-30（COMB 设计链）

- 范围：`COMB-F01`～`COMB-F05` 设计 + `feat/combat`
- 已完成：
  - 设计链：`COMB-feat-chain.md`；F01 ASC、F02 伤害管线、F03 选 5 Commit、F04 敌人行为表、F05 Manager/Flee/结算。
  - 外部参考：`REFERENCES.md`（CardGameDemo）。
  - 约定：打牌在 Player；Manager 只编排；`FoodGained` 暂 int。
- 下一步行动：
  - 在 `feat/combat` 从 `COMB-F01` 起按序实现。

## 2026-07-30（COMB-F01 实现）

- 范围：`COMB-F01` / `feat/combat`
- 已完成：
  - `Assets/Scripts/Combat/Framework/`：`AttributeData`、`AttributeSet`、`CombatComponentBase`。
  - 程序集 `SixDaysRemaining.Combat`；Edit Mode 测试 `CombatComponentBaseTests`。
  - 约定落地：值在 Set 侧 Dictionary；同类型 Set 唯一；未绑定 Get/Set 抛异常；`PreAttributeChange` 可 clamp。
- 验证：
  - Framework 源码可用 `dotnet`/netstandard2.1 编译检查（无 Unity 依赖）。
  - Unity Edit Mode 待本机 Test Runner 确认全绿。
- 下一步行动：
  - Unity 跑 Edit Mode → 通过后进入 `COMB-F02`。

## 2026-07-30（COMB-F01～F05 实现链）

- 范围：`feat/combat` 全 COMB 实现链
- 已完成：
  - F01：`Combat/Framework` ASC 骨架
  - F02：`CombatAttributeSet` + `CombatComponent`（Deal/Take/Block，Floor）
  - F03：`PlayerCombatComponent` 选 5 Commit；`Cards/`（CardDef/Catalog/Deck/Executor）
  - F04：`EnemyCombatComponent` + Pattern + 轻量 `CombatSession`；Executor Session 重载
  - F05：`CombatManager`（Notify/Flee/清 Block/Result）；BattleOnly
  - Edit Mode：`CombatComponentBaseTests` / `CombatComponentTests` / `PlayerCombatCardTests` / `EnemyCombatTests` / `CombatManagerTests`
- 验证：
  - 全部 Combat 源码 `dotnet` netstandard2.1 编译 0 错误（无 UnityEngine 依赖）
  - Unity Edit Mode 已由负责人确认全绿；已提交 `78115f1`
- 下一步行动：
  - merge `feat/combat` → `main`（待指令）；启动可玩接入层设计

## 2026-07-30（CORE-F03 设计草案）

- 范围：单场景可玩接入（输入 + Log）
- 已完成：
  - 注册 `CORE-F03`；撰写 `designs/CORE-F03-playable-loop.md`
  - 拍板写入设计：单场景多面板；战斗 UI+数字键；反馈以 Debug.Log 为主；业务保持纯 C#
- 下一步行动：
  - 负责人审阅 design；通过后开 `feat/playable-loop` 实现

## 2026-07-30（CORE-F03 实现）

- 范围：`feat/playable-loop` 可玩接入
- 已完成：
  - `CombatComponentBase : MonoBehaviour`；Player/Enemy 挂 GO；`CombatManager` Instantiate 敌人并 Destroy
  - Edit Mode 测试改为 `CombatTestHost` + `AddComponent`
  - `GameInstance` 引用 Player/EnemyPrefab/CombatRoot；开局 `foodStock=5`
  - UI：`*Panel` + `AppFlowController`；`PlayableLoopBootstrap` 运行时搭建 Demo（含 AfterSceneLoad 自动拉起）
  - 战斗输入：按钮 + 数字键；反馈以 `[Flow]/[Shelter]/[Combat]` Log 为主
- 验证：
  - 待本机：Edit Mode 全绿；Play `SampleScene` → Start → Depart → 选 5 Commit / Flee → Continue
- 下一步行动：
  - Play 验收通过后 prepare commit

## 2026-07-30（UI 交接文档）

- 范围：给 UI 协作者的交接材料
- 已完成：
  - 新增 `docs/ai/UI_HANDOFF.md`（Demo 形态、面板、命名、AppFlow/业务 API、重构建议）
  - 更新根 `README.md`、`PROJECT_CONTEXT.md`、`BOOTSTRAP_DIGEST.md`、`ACTIVE_WORK.md`
- 下一步行动：
  - UI 同学按 `UI_HANDOFF.md` 接手表现层；程序侧讨论 `SHLT-F02`（未编码）

## 2026-08-01（Demo UI 改用 TMP）

- 范围：Demo 文字组件 `UnityEngine.UI.Text` → TextMeshPro
- 已完成：
  - `*Panel` / `PlayableLoopBootstrap` 改用 `TMP_Text` / `TextMeshProUGUI`
  - UI asmdef 引用 `Unity.TextMeshPro`；Demo 优先系统 CJK 动态字体（LiberationSans 无汉字）
  - 同步 `CORE-F03`、`UI_HANDOFF`、`README`
- 下一步行动：
  - 本机 Play 确认 TMP 文字正常显示

## 2026-08-07（COMB-F06 实现中：统一卡牌 / 意图）

- 范围：`feat/combat`（已 FF 到含队友 UI 的 main）；玩家牌与敌人意图同质为 `CardDef`
- 已完成：
  - `ICardLibrary` / `IEncounterLibrary` + `CombatContent` 内存种子（JSON → F08）
  - 日遭遇表、空槽、固定腐蚀 +3、消极惩罚、攻击蓄力占位
  - 接线 `CombatView` / `EnemyActionSlotView` / `AppFlowController`（保留拖拽五槽 UI）
  - 更新 Edit Mode 测试
- 下一步行动：
  - Unity Edit Mode 全绿 + Play 冒烟 Day1
  - 通过后 prepare commit

## 2026-08-07（晚：文档收口 + 下一域）

- 范围：战斗/可玩/UI 文档状态收口；准备讨论 `SHLT-F02`
- 已完成：
  - Registry：`COMB-F01`～`F08`、`UI-F01`、`CORE-F03` → **Done**
  - 更新 `ACTIVE_WORK`、`COMB-feat-chain`、`PROJECT_CONTEXT`、`BOOTSTRAP_DIGEST`
  - F08 实现已在 `feat/combat` 提交 `79470a7`（待合 main）
- 下一步行动：
  - 讨论并起草 `SHLT-F02` design（人物模板 2.0）
  - `feat/combat` 合 main / push

