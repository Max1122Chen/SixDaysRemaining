# Feature 注册表（Feature Registry）

在开始实现规划前，先注册每一个新的 Feature ID。  
**每个 feat 须先完成 design 并通过审阅，再开分支编码。**

| Feature ID | 标题 | 域 | 状态 | 设计文档 | 实现计划 | 分支 | 负责人 | 备注 |
|------------|------|----|------|----------|----------|------|--------|------|
| `TECH-F01` | 为 Unity 原型配置 agent 工作流 | TECH | Done | `designs/TECH-F01-agent-workflow-bootstrap.md` | `plans/TECH-F01-agent-workflow-bootstrap-plan.md` | — | Max | 已完成 |
| — | 技术设计大纲与开发路线图 | — | Review | `ROADMAP.md` | — | — | Max | 与 CORE-F02 设计同批 commit |
| `CORE-F02` | Gameplay Framework（阶段框架） | CORE | Done | `designs/CORE-F02-gameplay-framework.md` | （合并在 design 内） | `feat/gameplay-framework` | Max | 已合并 `main` |
| `SHLT-F01` | 庇护所 + 幸存者（无道具） | SHLT | Done | `designs/SHLT-F01-shelter-survivor.md` | — | `feat/shelter` | Max | 已合并 `main` |
| `COMB-F01` | 轻量 ASC（CombatComponentBase + AttributeSet） | COMB | Done | `designs/COMB-F01-combat-component-base.md` | — | `feat/combat` | Max | 已提交 `feat/combat` |
| `COMB-F02` | 战斗 AttributeSet + 伤害/格挡管线 | COMB | Done | `designs/COMB-F02-combat-pipeline.md` | — | `feat/combat` | Max | 同分支 |
| `COMB-F03` | PlayerCombat + 选 5 Commit | COMB | Done | `designs/COMB-F03-player-cards.md` | — | `feat/combat` | Max | 同分支 |
| `COMB-F04` | EnemyCombat + 行为表 + 轻量 Session | COMB | Done | `designs/COMB-F04-enemy-pattern.md` | — | `feat/combat` | Max | 同分支 |
| `COMB-F05` | CombatManager + 编排 + 结算 | COMB | Done | `designs/COMB-F05-combat-manager.md` | — | `feat/combat` | Max | 同分支 |
| `COMB-F06` | 统一卡牌模型 + 设计师内容（内存种子） | COMB | Review | `designs/COMB-F06-designer-content.md` | — | `feat/combat` | Max | 已提交；Corrupted 见 F07 |
| `COMB-F07` | Corrupted 伴生牌 | COMB | Review | `designs/COMB-F07-corrupted-cards.md` | — | `feat/combat` | Max | 逻辑已落；UI 缺陷见 UI-F01 |
| `COMB-F08` | 战斗内容 JSON 数据驱动 | COMB | Deferred | `designs/COMB-F08-data-driven-content.md` | — | `feat/combat-data-driven` | Max | 依赖 F06 接口；本轮不实现 |
| `UI-F01` | 战斗卡牌交互修复（伴生/复位/叠层/槽高亮） | UI | Review | `designs/UI-F01-combat-card-interaction.md` | — | `feat/ui` | Max / UI | Play 已通过；待合 main |
| `CORE-F03` | 可玩接入层（单场景 + 输入 + Log） | CORE | Review | `designs/CORE-F03-playable-loop.md` | — | `feat/playable-loop` | Max | Demo/UI 已合 main；文档待跟 UI |
| `SHLT-F02` | 幸存者身份目录 + 入住/状态/死亡 | SHLT | Done | `designs/SHLT-F02-survivor-identity.md` | — | `feat/shelter` | Max | 已合 `main` |
| `EVT-F01` | 突发事件系统 | EVT | Deferred | — | — | `feat/events` | Max | 四套事件池；设计明朗后再写 design |

## 域代码

- `CORE` - 核心循环、GameState、阶段编排
- `SHLT` - 庇护所、NPC、饱食度
- `COMB` - 卡牌战斗
- `UI` - 界面交互与呈现
- `EVT` - 突发事件（延后）

## 状态说明

- Draft / Planned / In Progress / Review / Done / Blocked / Deferred / Cancelled
