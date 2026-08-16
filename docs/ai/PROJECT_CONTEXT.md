# 项目背景（Project Context）

最后更新：2026-08-16（内容对齐盘点 + README 总览）

## 1) 项目目标

一句话使命：
- 做一个小而完整的 Unity GameJam 原型：有清晰核心循环，并且能在“一局/一轮”层面体验顺畅、可交付。

主要成功标准：
- 玩家能启动游戏、理解目标，并完成至少一轮完整可玩的流程。
- 核心的“逐帧交互体验”足够稳定，能支撑 GameJam 的快速迭代。

## 2) 架构方向

总体架构概述：
- Unity 2022.3 的 2D 项目；卡牌生存 + 文字事件驱动（见 `docs/designs/` 产品源）。
- 技术拆分见 `docs/ai/ROADMAP.md`（**历史快照**，backlog 以 Registry / ACTIVE_WORK 为准）。
- 编排：`GameInstance` → `AppFlowController` → `GameplaySubsystem` + Shelter / Combat / Events。
- 局内全局状态由 `GameState` 统一管理（天数、食物、腐蚀、阶段、`endingId`、Tag）。

脚本域划分（`SixDaysRemaining/Assets/Scripts/`）：
- `App` - `GameInstance`、`AppFlowController`、Persist / Meta / Save
- `Gameplay` - `GameplaySubsystem`、`GameState`、GameplayTag、`EndingIds`
- `Events` - `GameEventSubsystem`、JSON 内容、Survivor/Random Provider
- `Shelter` - 身份目录、被动、饱食度、cap5
- `Combat` - 卡牌 Library、JSON、`CombatManager`、Corrupted、`SurvivorTrait`
- `UI` - `PresentationManager`、Views、HUD
- `Debug` - Hybrid 控制台 / registry + gates

## 3) 当前阶段

已落地（逻辑在 `main`）：
- 可玩主线 Prep→战斗→凯旋→事件→日结→次日
- 战斗 F01–F11；庇护所 F01–F05；事件 F01–F04；END / META / SAVE
- 人物与事件 3.0 + 卡牌数值 2.0 内容已写入 StreamingAssets

半开放（待他人完整 Play）：
- EVT-F03 / SHLT-F04–F05 / EVT-F04 / SAVE-F02 / COMB-F10（COMB-F11 轻验）

分支：
- 全部已实现 feat 已在本地 **`main`**；历史 `feat/*` **ahead-of-main = 0**
- 本地 `main` 相对 `origin/main` 可能超前（未 push 时）

下一里程碑：
- 半开放批次 Play 签字 → 对应 feat 标 Done
- 可选：Excel→JSON 导出；TD-004/005/006

## 4) 协作约定

- 计划真相：`ACTIVE_WORK.md`、`FEATURE_REGISTRY.md`、`TECH_DEBT.md`、最近的 `PROGRESS_LOG.md`
- 对外总览：根目录 `README.md`
- 分支：按大域划分；未经明确要求不新开细碎 `feat/*`
- 交付门槛：遵循 `templates/DOC_GOVERNANCE.md` 与 Slice DoD
- 会话恢复：`BOOTSTRAP_DIGEST.md`
