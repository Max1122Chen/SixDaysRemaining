# 技术设计大纲与开发路线图（ROADMAP）

## 元信息

- **状态：** `Draft`（待负责人审阅）
- **负责人：** `Max`
- **最后更新：** `2026-07-29`（GameState 命名；feat 顺序与工作流纪律）
- **产品设计源：** `docs/designs/六日英雄—技术演示文档.pdf`
- **相关：** `PROJECT_CONTEXT.md`、`FEATURE_REGISTRY.md`、`ACTIVE_WORK.md`

## TL;DR

本文档是《六日英雄》技术实现的主路线图：系统拆分、数据归属、feat 开发顺序与 Git 分支策略。
**每个 feat 必须先有设计/计划文档并通过审阅，再开分支编码；提交同样需审阅批准。**
首版 feat 顺序：`gameplay-framework` → `shelter` → `combat`；**事件系统延后**，待设计明朗再开独立 feat。

---

## 1) 工程目标（程序员视角）

- 跑通 **6 天固定周期** 的主循环：`庇护所 -> 战斗 -> 突发事件 -> 天数+1`（事件阶段首版可占位跳过）。
- 用 **`GameState`** 作为局内全局数值的单一数据源，避免多系统双写。
- 战斗、庇护所各自独立演进，由 `GameplaySubsystem` 编排阶段切换。
- 支持 **BattleOnly 测试模式** 与 **seed 驱动伪随机**（在 `feat/combat` 及之后接入）。

---

## 2) 系统架构

### 2.1 分层总览

```text
GameInstance（应用层服务）
  ├─ 子系统初始化 / 生命周期
  ├─ 存档读写入口（后续 feat）
  └─ 模式切换（主菜单 / 对局中）

GameplaySubsystem（局内编排）
  ├─ 持有 GameState（当前局全局状态）
  ├─ 日循环阶段机：出征准备 -> 战斗 -> 凯旋 ->（下一天或结局）
  ├─ 本层只维护阶段枚举切换；业务由 Shelter/Combat 等后续 feat 填充
  └─ 结局结算触发（EndingEvaluator，可与事件 feat 一并落地）

ShelterManager（庇护所域）
  ├─ NPC 增删查、到来/死亡/离开
  ├─ 饱食度下降的唯一写入源（日结）
  ├─ NPC 交互（查看状态、分配食物等）
  └─ 【延后】庇护所道具刷新与交互

CombatManager（战斗域）
  ├─ CombatSession 生命周期
  ├─ 本局战斗收获（foodGainedThisCombat 等）
  ├─ 回合推进、胜负、逃离
  └─ 输出 CombatResult 给 GameplaySubsystem 写回 GameState

EventDirector（事件域）【整域延后】
  ├─ 每日事件优先级、四池随机、选项效果
  └─ 待事件系统设计明朗后，以独立 feat 实现

Content/Data（配置层）
  ├─ 卡牌、怪物、事件、NPC、结局定义
  └─ 各 Manager 只读引用；随对应 feat 逐步引入
```

### 2.2 各系统职责边界

| 系统 | 负责 | 不负责 |
|------|------|--------|
| `GameInstance` | 初始化、存档服务、场景/模式切换 | 天数、人口、腐蚀、战斗细节 |
| `GameplaySubsystem` | **阶段迁移**、`GameState` 生命周期 | NPC 饱食度细则、卡牌伤害结算 |
| `ShelterManager` | NPC、饱食度日结、庇护所交互 | 战斗内出牌；**道具系统（延后）** |
| `CombatManager` | 战斗会话、战斗产出、`CombatResult` | 全局食物存量长期持有 |
| `EventDirector` | （延后）事件抽取与执行 | — |

### 2.3 暂缓项

- **EventDirector / 突发事件**：设计未明朗，不在前三条 feat 内实现；不塞进首版日循环状态机，等 `feat/events`。
- **庇护所道具**：不在 `feat/shelter` 范围内。
- **GameplayEventSystem + Channel**：系统数量少时不引入。
- **图鉴 / 碎片化彩蛋**：低优先级。

---

## 3) 数据归属（已拍板）

### 3.1 GameState（由 GameplaySubsystem 持有）

```text
day                 // 当前天数 1~6
foodStock           // 食物存量（全局）
corruption          // 腐蚀度（全局）
rngSeed             // 本局随机种子
flags               // 长线事件布尔标记集
population          // 可由 NPC 列表推导，或冗余缓存
currentPhase        // ExpeditionPrep / Combat / TriumphReturn / Ending
```

### 3.2 战斗相关数据

| 数据 | 归属 | 说明 |
|------|------|------|
| 食物存量 `foodStock` | `GameState` | 战斗结束后由编排器合并入账 |
| 本局战斗收获 | `CombatManager` / `CombatSession` | 打包进 `CombatResult`，非存量 |
| 腐蚀度 `corruption` | `GameState` | 战斗/事件等效果最终写回此处 |
| 玩家战斗 HP、格挡、手牌 | `PlayerCombatComponent` | 仅战斗会话内 |
| 敌人意图与行动序列 | `EnemyCombatComponent` | 仅战斗会话内 |

### 3.3 实体模型（首版建议）

- `NPC`：基类 + 数据驱动特质；`feat/shelter` 先实现 1~2 个角色。
- `Player`（战斗实体）：不长期持有全局腐蚀/食物；战斗职责在 `PlayerCombatComponent`。
- `DeckRuntime`：在 `feat/combat` 中从战斗组件拆出。

---

## 4) 主循环状态机（编排器）

```text
[NewRun]
  -> ExpeditionPrep（出征准备）  （feat/shelter 填充业务）
  -> Combat（战斗）              （feat/combat 填充业务）
  -> TriumphReturn（凯旋）       （回庇护所结算/反馈；业务后续填充）
  -> day++；若 day > 6 则 Ending，否则回到 ExpeditionPrep

[Ending]                         （可与后续 feat 一并实现）
  -> EndingEvaluator -> 展示
```

`feat/gameplay-framework` 的目标：由 `GameplaySubsystem` **直接维护阶段枚举切换**（不拆 Director、不用 IPhaseHandler），跑通抽象流程即可。

---

## 5) 开发阶段与 feat 分支顺序

> **一个 feat 一条分支**；分支内多步用多次 commit 推进。  
> **开分支前**：须在 `FEATURE_REGISTRY` 登记，并写好 `designs/` + `plans/`，**审阅通过后再编码**。

### Feat 1 — Gameplay Framework · `feat/gameplay-framework`

| 步骤 | 内容 |
|------|------|
| F1-1 | `GameInstance`：初始化、主菜单/对局模式切换骨架 |
| F1-2 | `GameState` + `GameplayPhase`（出征准备 / 战斗 / 凯旋 / 结局） |
| F1-3 | `GameplaySubsystem`：直接维护阶段状态机（`StartNewRun` / `AdvancePhase`） |
| F1-4 | 脚本目录雏形 + Edit Mode 抽象流程测试 |

**范围外：** 饱食度、NPC、出牌、事件；不拆 DayLoopDirector / IPhaseHandler；不强制空壳 Manager。

**验收：** Edit Mode 证明「出征准备 → 战斗 → 凯旋 → 次日准备」；编译无错。详见 `designs/CORE-F02-gameplay-framework.md`。

### Feat 2 — 庇护所 + NPC · `feat/shelter`

| 步骤 | 内容 |
|------|------|
| F2-1 | `ShelterManager`：NPC 列表、分配食物 |
| F2-2 | 饱食度日结减少（唯一写入源） |
| F2-3 | NPC 交互（查看状态、特质等） |
| F2-4 | 饱食度过低 → 死亡或离开（跑路） |
| F2-5 | 庇护所基础 UI（分配、状态展示） |

**范围外：** 庇护所道具刷新与交互（等需求明朗再做）。

**验收：** 在 `ExpeditionPrep`（出征准备）内完成一次庇护所日结，NPC 状态按规则变化。

### Feat 3 — 战斗 · `feat/combat`

| 步骤 | 内容 |
|------|------|
| F3-1 | `CombatManager` + `CombatSession`：单敌人、基础回合 |
| F3-2 | `DeckRuntime` + 最小白牌组 |
| F3-3 | `CombatResult` 回写 `GameState`（食物收获、腐蚀 +3、逃离等） |
| F3-4 | BattleOnly 测试场景 |

**验收：** 可独立进战斗并产出结算；`GameplaySubsystem` 在 `CombatPhase` 能正常进出。

### Feat 4 及以后 — 延后

| feat | 触发条件 | 说明 |
|------|----------|------|
| `feat/events` | 事件系统设计明朗 | `EventDirector`、四池事件、选项效果 |
| `feat/save` | 核心 loop 稳定后 | 受限存档读档 |
| `feat/combat-advanced` | 战斗基础稳定后 | 黑化卡牌、特殊怪奖励等 |
| `feat/meta` | 低优先级 | 图鉴、碎片化彩蛋 |

**首版主线分支序列：**

```text
main
 ├─ feat/gameplay-framework
 ├─ feat/shelter
 └─ feat/combat
```

---

## 6) 测试与可复现性

- **BattleOnly 模式**：在 `feat/combat` 交付；不经过庇护所直接进入战斗。
- **Seed 驱动随机**：`GameState.rngSeed`；事件/战斗随机在对应 feat 接入。

---

## 7) Git 与工作流纪律

### 7.1 分支模型

- `main`：稳定可运行；每个 feat 验收通过后合并。
- `feat/<domain>`：从最新 `main` 拉出，一个 feat 一条分支。

### 7.2 Feat 生命周期（必须遵守）

每个 feat 按以下顺序，**不可跳过审阅闸门**：

```text
1. 登记 Feature ID（FEATURE_REGISTRY.md）
2. 编写 Design Spec（`docs/ai/designs/`）；复杂 feat 可另写 plans，简单 feat 可合并进 design
3. 【审阅】负责人批准设计
4. 从 main 拉 feat/* 分支，按 plans 实现
5. 本地验证（Unity 编译 + 该 feat 验收项）
6. 【审阅】prepare commit：草拟提交信息，负责人批准
7. 执行 commit；feat 全部完成后 merge 回 main，更新 PROGRESS_LOG
```

- **Draft 设计不授权大规模编码。**
- **未经明确指令不执行 `git commit` / push。**
- 同一 feat 内可多次 commit；不要跨 feat 混在一个分支。

### 7.3 提交约定

- 提交信息中文优先，Conventional Commits（如 `feat(gameplay): 增加阶段切换骨架`）。
- 文档类提交与代码类提交可分开，但同一 feat 内应可追溯。

---

## 8) 待设计师确认项

（不阻塞 `feat/gameplay-framework` 与 `feat/shelter` 骨架；阻塞战斗/事件细节）

1. 每日是否严格三阶段单次流转。
2. 食物是整型存量还是具名物品。
3. 卡牌战斗：防御语义、牌区流转、伤害公式。
4. 每场战斗敌人数量。

未确认前的默认假设见上一版记录；确认后更新本节并同步 `TECH_DEBT.md`。

---

## 9) 审阅清单

- [ ] 系统拆分与职责边界认可
- [ ] `GameState` 数据归属认可
- [ ] Feat 顺序（gameplay-framework → shelter → combat；事件延后）认可
- [ ] Feat 生命周期（先设计审阅、后开发、提交审阅）认可
- [ ] 待设计师确认项的默认假设可接受

## 10) 审阅通过后的下一步

1. 将本文档状态改为 `Planned`。
2. 提交本次「技术架构规划」文档变更（负责人批准后执行 commit）。
3. 审阅 `designs/CORE-F02-gameplay-framework.md`（实现步骤与测试策略已合并在该文档内）。
4. 从 `main` 拉 `feat/gameplay-framework`；以 **Edit Mode 测试** 为 DoD，不写业务逻辑。
