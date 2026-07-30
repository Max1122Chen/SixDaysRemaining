# 项目背景（Project Context）

最后更新：2026-07-29（GameState；feat 顺序与工作流纪律）

## 1) 项目目标

一句话使命：
- 做一个小而完整的 Unity GameJam 原型：有清晰核心循环，并且能在“一局/一轮”层面体验顺畅、可交付。

主要成功标准：
- 玩家能启动游戏、理解目标，并完成至少一轮完整可玩的流程。
- 核心的“逐帧交互体验”足够稳定，能支撑 GameJam 的快速迭代。

## 2) 架构方向

总体架构概述：
- Unity 2022.3 的 2D 项目；卡牌生存 + 文字事件驱动（详见 `docs/designs/六日英雄—技术演示文档.pdf`）。
- 技术拆分见 `docs/ai/ROADMAP.md`：`GameInstance` -> `GameplaySubsystem` -> `ShelterManager` / `CombatManager` / `EventDirector`。
- 局内全局状态由 `GameState` 统一管理（天数、食物存量、腐蚀度、标记位、随机种子、当前阶段）。
- Feat 顺序见 `ROADMAP.md`：`gameplay-framework` → `shelter` → `combat`；事件系统延后。

脚本域划分（`SixDaysRemaining/Assets/Scripts/`）：
- `Bootstrap` - `GameInstance`、场景与模式切换
- `Gameplay` - `GameplaySubsystem`、`GameState`、日循环阶段机
- `Shelter` - 庇护所 NPC、饱食度日结（道具延后）
- `Combat` - `CombatManager`、`CombatSession`、战斗组件与牌组运行时
- `UI` - 各阶段界面（庇护所、战斗、事件、结局）

## 3) 当前阶段

当前阶段：
- 技术架构规划（`ROADMAP.md`）待审阅；待 commit 批准后进入 `CORE-F02` 设计阶段。

下一里程碑：
- 批准「技术架构规划」commit → 编写 `CORE-F02` design/plan → 审阅 → `feat/gameplay-framework`。

## 4) 协作约定

- 计划真相（Planning truth）：`ACTIVE_WORK.md`、`FEATURE_REGISTRY.md`、`TECH_DEBT.md`、最近的 `PROGRESS_LOG.md`
- 交付门槛（Delivery bar）：遵循 `templates/DOC_GOVERNANCE.md` 与 Slice DoD
- 会话恢复（Session recovery）：使用 `BOOTSTRAP_DIGEST.md`
- 协作语言偏好：文档/提交信息用中文；代码标识符可保持英文；代码注释与文档内容以中文为主。

## 5) 验证基线

- 构建/验证命令：用 Unity `2022.3.62f3c1` 打开 `SixDaysRemaining/`，并确认 Console 没有编译错误。
- 测试命令：在 `SixDaysRemaining/Assets/Scenes/SampleScene.unity` 进入 Play Mode，并核对当前任务的人工检查清单。

## 6) 可选双轨文档模式

选择一种模式：
- 双轨：外部或产品视角的设计真相可以放在 `docs/ai/` 之外；该目录主要存放实现工作流与提炼后的工程决策。

## 7) 外部参考

- 卡牌 / 轻量 GAS 实践：[CardGameDemo](https://github.com/Max1122Chen/CardGameDemo.git)（本地 `D:\Dev\GitRepo\CardGameDemo`）
- 索引见 `docs/ai/REFERENCES.md`；重点读其 `docs/design/systems/attributes.md` 与 `gameplay-framework.md`。

