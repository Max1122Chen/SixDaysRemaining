# Feature 注册表（Feature Registry）

在开始实现规划前，先注册每一个新的 Feature ID。  
**每个 feat 须先完成 design 并通过审阅，再开分支编码。**

| Feature ID | 标题 | 域 | 状态 | 设计文档 | 实现计划 | 分支 | 负责人 | 备注 |
|------------|------|----|------|----------|----------|------|--------|------|
| `TECH-F01` | 为 Unity 原型配置 agent 工作流 | TECH | Done | `designs/TECH-F01-agent-workflow-bootstrap.md` | `plans/TECH-F01-agent-workflow-bootstrap-plan.md` | — | Max | 已完成 |
| — | 技术设计大纲与开发路线图 | — | Review | `ROADMAP.md` | — | — | Max | 与 CORE-F02 设计同批 commit |
| `CORE-F02` | Gameplay Framework（阶段框架） | CORE | Done | `designs/CORE-F02-gameplay-framework.md` | （合并在 design 内） | `feat/gameplay-framework` | Max | 已合并 `main` |
| `SHLT-F01` | 庇护所 + 幸存者（无道具） | SHLT | Done | `designs/SHLT-F01-shelter-survivor.md` | — | `feat/shelter` | Max | 已合并 `main` |
| `COMB-F01` | 轻量 ASC（CombatComponentBase + AttributeSet） | COMB | Done | `designs/COMB-F01-combat-component-base.md` | — | `feat/combat` | Max | 已合 main |
| `COMB-F02` | 战斗 AttributeSet + 伤害/格挡管线 | COMB | Done | `designs/COMB-F02-combat-pipeline.md` | — | `feat/combat` | Max | 同分支 |
| `COMB-F03` | PlayerCombat + 选 5 Commit | COMB | Done | `designs/COMB-F03-player-cards.md` | — | `feat/combat` | Max | 同分支 |
| `COMB-F04` | EnemyCombat + 行为表 + 轻量 Session | COMB | Done | `designs/COMB-F04-enemy-pattern.md` | — | `feat/combat` | Max | 同分支 |
| `COMB-F05` | CombatManager + 编排 + 结算 | COMB | Done | `designs/COMB-F05-combat-manager.md` | — | `feat/combat` | Max | 同分支 |
| `COMB-F06` | 统一卡牌模型 + 设计师内容（内存种子） | COMB | Done | `designs/COMB-F06-designer-content.md` | — | `feat/combat` | Max | 内容由 F08 JSON 取代 |
| `COMB-F07` | Corrupted 伴生牌 | COMB | Done | `designs/COMB-F07-corrupted-cards.md` | — | `feat/combat` | Max | 已合 main |
| `COMB-F08` | 战斗内容 JSON 数据驱动 | COMB | Done | `designs/COMB-F08-data-driven-content.md` | — | `feat/combat` | Max | StreamingAssets；硬失败 |
| `UI-F01` | 战斗卡牌交互修复（伴生/复位/叠层/槽高亮） | UI | Done | `designs/UI-F01-combat-card-interaction.md` | — | `feat/ui` | Max / UI | 已合 main |
| `CORE-F03` | 可玩接入层（单场景 + 输入 + Log） | CORE | Done | `designs/CORE-F03-playable-loop.md` | — | `feat/playable-loop` | Max | 可玩主线已合 |
| `CORE-F05` | AppFlow 编排收敛 + PresentationManager | CORE | Done | `designs/CORE-F05-appflow-presentation.md` | — | `main` | Max | Flow 编译在 App 程序集；命名空间 Gameplay；`TD-007` |
| `CORE-F04` | Scene-owned GameInstance + Hybrid Debug | CORE | Done | `designs/CORE-F04-scene-gameinstance-debug.md` | — | `main` | Max | `~` 控制台 + gate + 全命令；`run.day skip` 后续可选 |
| `SHLT-F02` | 幸存者身份目录 + 入住/状态/死亡 | SHLT | Done | `designs/SHLT-F02-survivor-identity.md` | — | `feat/shelter` | Max | 已合 main |
| `COMB-F09` | 每步格挡结算 + Corruption Gateway | COMB | Done | `designs/COMB-F09-per-step-block-and-corruption-gateway.md` | — | `main` | Max | `40bdf58` 落地；文档 2026-08-12 收口 |
| `EVT-F01` | GameEventSubsystem + 同质事件模型 | EVT | In Progress | `designs/EVT-F01-game-event-subsystem.md` | — | `feat/events` | Max | 已实现于 `9567b64`；待 Play 标 Done |
| `EVT-F02` | 幸存者特殊事件（SurvivorEventProvider） | EVT | Draft | `designs/EVT-F02-survivor-events.md` | — | `feat/events` | Max | defId 专属事件；与随机池争全日额度 |

## 域代码

- `CORE` - 核心循环、GameState、阶段编排
- `SHLT` - 庇护所、NPC、饱食度
- `COMB` - 卡牌战斗
- `UI` - 界面交互与呈现
- `EVT` - 突发事件（延后）

## 状态说明

- Draft / Planned / In Progress / Review / Done / Blocked / Deferred / Cancelled / Discuss
