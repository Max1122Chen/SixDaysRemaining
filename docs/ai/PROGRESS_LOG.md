# 进度日志（Progress Log）

追加式、按时间顺序的项目事实记录（append-only）。

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

