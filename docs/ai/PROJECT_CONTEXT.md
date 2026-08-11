# 项目背景（Project Context）

最后更新：2026-08-07（SHLT-F02 身份数据驱动）

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
- Feat 顺序见 `ROADMAP.md`：`gameplay-framework` → `shelter` → `combat`；事件系统延后；可玩接入见 `CORE-F03`。

脚本域划分（`SixDaysRemaining/Assets/Scripts/`）：
- `Bootstrap` - `GameInstance`、场景引用绑定
- `Gameplay` - `GameplaySubsystem`、`GameState`、日循环阶段机
- `Shelter` - 庇护所、幸存者身份目录（JSON）、饱食度日结（道具/特质/入住被动延后）
- `Combat` - ASC、卡牌、`CombatManager` / Session、Player/Enemy 组件（MB）
- `UI` - Demo 面板、`AppFlowController`、`PlayableLoopBootstrap`（正式 UI 待接手）

## 3) 当前阶段

当前阶段：
- **可玩主线 Demo 已落地**（`feat/playable-loop`）：空 `SampleScene` Play 即可跑 Prep→战斗→凯旋→次日。
- UI 为运行时极简 uGUI + Log；**正式 UI 交接给协作者**，详见 `docs/ai/UI_HANDOFF.md`。

下一里程碑：
- `SHLT-F02` 身份目录已实现（Review）；Edit Mode 确认后合入
- 幸存者入住被动 / 特质牌挂钩（定义清晰后再开）
- 突发事件（`EVT-F01` 仍延后）
- UI 同学按交接文档替换表现层

## 4) 协作约定

- 计划真相（Planning truth）：`ACTIVE_WORK.md`、`FEATURE_REGISTRY.md`、`TECH_DEBT.md`、最近的 `PROGRESS_LOG.md`
- UI 交接真相：`UI_HANDOFF.md` + `designs/CORE-F03-playable-loop.md`
- 交付门槛（Delivery bar）：遵循 `templates/DOC_GOVERNANCE.md` 与 Slice DoD
- 会话恢复（Session recovery）：使用 `BOOTSTRAP_DIGEST.md`
- 协作语言偏好：文档/提交信息用中文；代码标识符可保持英文；代码注释与文档内容以中文为主。

## 5) 验证基线

- 构建/验证命令：用 Unity `2022.3.62f3c1` 打开 `SixDaysRemaining/`，并确认 Console 没有编译错误。
- 冒烟：`SampleScene` Play → Start → Depart → 选 5 Commit → Continue → 次日可再出门。
- Edit Mode：`Assets/Tests/EditMode` 全绿。

## 6) 可选双轨文档模式

选择一种模式：
- 双轨：外部或产品视角的设计真相可以放在 `docs/ai/` 之外；该目录主要存放实现工作流与提炼后的工程决策。

## 7) 外部参考

- 卡牌 / 轻量 GAS 实践：[CardGameDemo](https://github.com/Max1122Chen/CardGameDemo.git)（本地 `D:\Dev\GitRepo\CardGameDemo`）
- 索引见 `docs/ai/REFERENCES.md`；重点读其 `docs/design/systems/attributes.md` 与 `gameplay-framework.md`。
