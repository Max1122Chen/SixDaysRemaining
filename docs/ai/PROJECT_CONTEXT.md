# 项目背景（Project Context）

最后更新：2026-08-12（CORE-F04 / F05 / COMB-F09 收口）

## 1) 项目目标

一句话使命：
- 做一个小而完整的 Unity GameJam 原型：有清晰核心循环，并且能在“一局/一轮”层面体验顺畅、可交付。

主要成功标准：
- 玩家能启动游戏、理解目标，并完成至少一轮完整可玩的流程。
- 核心的“逐帧交互体验”足够稳定，能支撑 GameJam 的快速迭代。

## 2) 架构方向

总体架构概述：
- Unity 2022.3 的 2D 项目；卡牌生存 + 文字事件驱动（见 `docs/designs/` 产品 PDF）。
- 技术拆分见 `docs/ai/ROADMAP.md`：`GameInstance` → `GameplaySubsystem` → `ShelterManager` / `CombatManager` /（事件延后）。
- 局内全局状态由 `GameState` 统一管理（天数、食物存量、腐蚀度、随机种子、当前阶段）。

脚本域划分（`SixDaysRemaining/Assets/Scripts/`）：
- `App` - `GameInstance`、`AppFlowController`、`GameplayCorruptionBridge`、`DebugRunSettings`
- `Gameplay` - `GameplaySubsystem`、`GameState`
- `Events` - `GameEventSubsystem`、JSON 内容、`RandomPoolProvider`（全日 cap=3；三钩子）
- `Shelter` - 庇护所、幸存者身份目录（JSON）、饱食度日结
- `Combat` - 卡牌 Library、JSON、`CombatManager`、Corrupted、`SurvivorTrait`
- `UI` - `PresentationManager`、`GameEventView`、Views、HUD
- `Debug` - Hybrid 控制台 / registry + gates

## 3) 当前阶段

已落地：
- 可玩主线（Prep→战斗→凯旋→次日→随机事件序列）
- 战斗 F01–F09（含每步格挡、Corruption Gateway、StreamingAssets JSON）
- 庇护所 F02（幸存者身份 JSON、入住/状态/死亡）
- CORE-F04 / F05：Scene GameInstance、Flow/Presentation 拆分、Debug Console
- UI：伙伴主界面/HUD/特质栏 + UI-F01 战斗交互

下一里程碑：
- `EVT-F01`：`GameEventSubsystem` + 同质事件模型（承接 `TD-007`）
- 入住被动 / 长线剧情（待 EVT/SHLT 后续 slice）

## 4) 协作约定

- 计划真相：`ACTIVE_WORK.md`、`FEATURE_REGISTRY.md`、`TECH_DEBT.md`、最近的 `PROGRESS_LOG.md`
- 分支：按大域划分；未经明确要求不新开细碎 `feat/*`
- 交付门槛：遵循 `templates/DOC_GOVERNANCE.md` 与 Slice DoD
- 会话恢复：`BOOTSTRAP_DIGEST.md`
