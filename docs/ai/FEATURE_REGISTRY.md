# Feature 注册表（Feature Registry）

在开始实现规划前，先注册每一个新的 Feature ID。  
**每个 feat 须先完成 design 并通过审阅，再开分支编码。**

| Feature ID | 标题 | 域 | 状态 | 设计文档 | 实现计划 | 分支 | 负责人 | 备注 |
|------------|------|----|------|----------|----------|------|--------|------|
| `TECH-F01` | 为 Unity 原型配置 agent 工作流 | TECH | Done | `designs/TECH-F01-agent-workflow-bootstrap.md` | `plans/TECH-F01-agent-workflow-bootstrap-plan.md` | — | Max | 已完成 |
| — | 技术设计大纲与开发路线图 | — | Review | `ROADMAP.md` | — | — | Max | 与 CORE-F02 设计同批 commit |
| `CORE-F02` | Gameplay Framework（阶段框架） | CORE | Review | `designs/CORE-F02-gameplay-framework.md` | （合并在 design 内） | `feat/gameplay-framework` | Max | Edit Mode 测试驱动阶段机 |
| `SHLT-F01` | 庇护所 + NPC（无道具） | SHLT | Planned | 待编写 | — | `feat/shelter` | Max | 依赖 CORE-F02 合并 |
| `COMB-F01` | 卡牌战斗基础 | COMB | Planned | 待编写 | — | `feat/combat` | Max | 依赖 SHLT-F01 合并 |
| `EVT-F01` | 突发事件系统 | EVT | Deferred | — | — | `feat/events` | Max | 设计明朗后再写 design |

## 域代码

- `CORE` - 核心循环、GameState、阶段编排
- `SHLT` - 庇护所、NPC、饱食度
- `COMB` - 卡牌战斗
- `EVT` - 突发事件（延后）

## 状态说明

- Draft / Planned / In Progress / Review / Done / Blocked / Deferred / Cancelled
