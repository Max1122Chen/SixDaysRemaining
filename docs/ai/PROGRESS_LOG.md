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

