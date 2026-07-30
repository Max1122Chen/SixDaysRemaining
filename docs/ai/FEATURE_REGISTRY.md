# Feature 注册表（Feature Registry）

在开始实现规划前，先注册每一个新的 Feature ID。  
**每个 feat 须先完成 design 并通过审阅，再开分支编码。**

| Feature ID | 标题 | 域 | 状态 | 设计文档 | 实现计划 | 分支 | 负责人 | 备注 |
|------------|------|----|------|----------|----------|------|--------|------|
| `TECH-F01` | 为 Unity 原型配置 agent 工作流 | TECH | Done | `designs/TECH-F01-agent-workflow-bootstrap.md` | `plans/TECH-F01-agent-workflow-bootstrap-plan.md` | — | Max | 已完成 |
| — | 技术设计大纲与开发路线图 | — | Review | `ROADMAP.md` | — | — | Max | 与 CORE-F02 设计同批 commit |
| `CORE-F02` | Gameplay Framework（阶段框架） | CORE | Done | `designs/CORE-F02-gameplay-framework.md` | （合并在 design 内） | `feat/gameplay-framework` | Max | 已合并 `main` |
| `SHLT-F01` | 庇护所 + 幸存者（无道具） | SHLT | Done | `designs/SHLT-F01-shelter-survivor.md` | — | `feat/shelter` | Max | 已合并 `main` |
| `COMB-F01` | 轻量 ASC（CombatComponentBase + AttributeSet） | COMB | In Progress | `designs/COMB-F01-combat-component-base.md` | — | `feat/combat` | Max | 实现链起点 |
| `COMB-F02` | 战斗 AttributeSet + 伤害/格挡管线 | COMB | Planned | `designs/COMB-F02-combat-pipeline.md` | — | `feat/combat` | Max | 依赖 F01 |
| `COMB-F03` | PlayerCombat + 选 5 Commit | COMB | Planned | `designs/COMB-F03-player-cards.md` | — | `feat/combat` | Max | 手牌 8；Commit 5 |
| `COMB-F04` | EnemyCombat + 行为表 + 轻量 Session | COMB | Planned | `designs/COMB-F04-enemy-pattern.md` | — | `feat/combat` | Max | 无意图展示 |
| `COMB-F05` | CombatManager + 编排 + 结算 | COMB | Planned | `designs/COMB-F05-combat-manager.md` | — | `feat/combat` | Max | Flee；FoodGained int |
| `EVT-F01` | 突发事件系统 | EVT | Deferred | — | — | `feat/events` | Max | 设计明朗后再写 design |

## 域代码

- `CORE` - 核心循环、GameState、阶段编排
- `SHLT` - 庇护所、NPC、饱食度
- `COMB` - 卡牌战斗
- `EVT` - 突发事件（延后）

## 状态说明

- Draft / Planned / In Progress / Review / Done / Blocked / Deferred / Cancelled
