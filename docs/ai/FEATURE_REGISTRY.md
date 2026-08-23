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
| `CORE-F05` | AppFlow 编排收敛 + PresentationManager | CORE | Done | `designs/CORE-F05-appflow-presentation.md` | — | `main` | Max | Flow 在 App 程序集 |
| `CORE-F04` | Scene-owned GameInstance + Hybrid Debug | CORE | Done | `designs/CORE-F04-scene-gameinstance-debug.md` | — | `main` | Max | `~` 控制台 + gate |
| `CORE-F06` | GameplayTag 基础设施 | CORE | Done | `designs/CORE-F06-gameplay-tags.md` | — | `main` | Max | Tag 容器 / 层级 / Query |
| `SHLT-F02` | 幸存者身份目录 + 入住/状态/死亡 | SHLT | Done | `designs/SHLT-F02-survivor-identity.md` | — | `feat/shelter` | Max | 已合 main |
| `COMB-F09` | 每步格挡结算 + Corruption Gateway | COMB | Done | `designs/COMB-F09-per-step-block-and-corruption-gateway.md` | — | `main` | Max | 已落地 |
| `EVT-F01` | GameEventSubsystem + 同质事件模型 | EVT | Done | `designs/EVT-F01-game-event-subsystem.md` | — | `main` | Max | 已 merge `feat/events` |
| `EVT-F02` | 幸存者特殊事件（SurvivorEventProvider） | EVT | Done | `designs/EVT-F02-survivor-events.md` | — | `main` | Max | 幼童线 Play 通过；政治家 D3 待复验 |
| `CORE-F07` | GameplayTag 业务迁移（storyFlags → Tag） | CORE | Done | `designs/CORE-F07-gameplay-tag-migration.md` | — | `main` | Max | Story Tag + requiredTags；删 storyFlags |
| `SHLT-F03` | 幸存者被动 + 人设闭环 | SHLT | Done | `designs/SHLT-F03-survivor-passives-and-personas.md` | — | `main` | Max | Play：幼童 −8 / 政治家回访通过 |
| `END-F01` | 结局钩子 + EndingEvaluator | CORE | Done | `designs/END-F01-ending-hooks.md` | — | `main` | Max | 政治家战败 E；Play 通过 |
| `COMB-F10` | 特质卡系统（defId 解锁 + 三特质） | COMB | Review（半开放） | `designs/COMB-F10-survivor-traits.md` | — | `main` | Max | 他人 Play 验收后 Done |
| `CORE-F08` | Persist 底座（JSON 文件存档基建） | CORE | Done | `designs/CORE-F08-persist-foundation.md` | — | `main` | Max | meta/run 分文件 |
| `META-F01` | 结局回顾（成就式 run summary） | META | Done | `designs/META-F01-ending-review.md` | — | `main` | Max | 终局解锁 + 回顾 |
| `SAVE-F01` | 受限存档读档 | CORE | Done | `designs/SAVE-F01-run-save.md` | — | `main` | Max | 节点粗粒度；禁战斗存档 |
| `EVT-F03` | 人物与随机事件 3.0 | EVT | Review（半开放） | `designs/EVT-F03-persona-and-events-3.md` | — | `main` | Max | S0–S4；他人 Play |
| `SHLT-F04` | 庇护所 cap5 / 死亡+8 / 分配例外 | SHLT | Review（半开放） | （并入 EVT-F03 design） | — | `main` | Max | 分配例外→SHLT-F05 |
| `SHLT-F05` | 分配例外 + 日结被动调制 | SHLT | Review（半开放） | `designs/SHLT-F05-alloc-and-passive-modulation.md` | — | `main` | Max | 他人 Play |
| `EVT-F04` | 事件 3.0 深度效果 | EVT | Review（半开放） | `designs/EVT-F04-events-3-depth.md` | — | `main` | Max | 他人 Play |
| `SAVE-F02` | 第四日存档询问 | CORE | Review（半开放） | `designs/SAVE-F02-day4-save-prompt.md` | — | `main` | Max | 他人 Play |
| `COMB-F11` | 卡牌数值 2.0 同步 | COMB | Review | `designs/COMB-F11-card-values-2.md` | — | `main` | Max | 敌人计划已同步；待 EditMode/轻 Play |
| `CORE-F09` | 设计师反馈修复包 01（逻辑） | CORE | Review | `designs/CORE-F09-designer-fix-pack-01.md` | — | `main` | Max | 逻辑 only；UI 2/3/7/8/9 交伙伴 |
| `END-F02` | 结局判定数据驱动（A–I） | CORE | Review | `designs/END-F02-data-driven-endings.md` | — | `main` | Max | endings.json 已落地；待轻 Play |
| `AUDIO-F01` | 场景 BGM 接入 | AUDIO | Done | `designs/AUDIO-F01-scene-bgm.md` | — | `main` | Max | 已合 main |
| `CORE-F10` | 设计师反馈修复包 02 | CORE | Review | `designs/CORE-F10-designer-fix-pack-02.md` | — | `main` | Max | 5 日战斗+日6终局/HP/牌库/投喂/奖励/结局条件 |

## 域代码

- `CORE` - 核心循环、GameState、阶段编排、Persist
- `SHLT` - 庇护所、NPC、饱食度
- `COMB` - 卡牌战斗
- `UI` - 界面交互与呈现
- `EVT` - 突发事件
- `META` - 局外回顾 / 成就式元数据
- `AUDIO` - 背景音 / 音效

## 近期执行顺序（2026-08-16）

1. **COMB-F11** — 卡牌数值 2.0（JSON 已同步；轻验后 Done）
2. **半开放批次** — EVT-F03 / SHLT-F05 / EVT-F04 / SAVE-F02 / COMB-F10 由他人 Play
3. Play 通过后将对应 feat 标 Done
4. 可选：Excel→JSON 导出工具

## 状态说明

- Draft / Planned / In Progress / Review / Done / Blocked / Deferred / Cancelled / Discuss
