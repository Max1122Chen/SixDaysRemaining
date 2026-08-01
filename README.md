# SixDaysRemaining（项目协作说明）

这个仓库用于承载《六日英雄》Unity 原型与协作文档。  
本文档面向**设计师**、**程序**与**UI 协作者**。

## 项目结构（根目录）

- `SixDaysRemaining/`：Unity 工程（用 Unity Hub 打开此目录）
  - `Assets/Scripts/`：按域拆分
    - `Bootstrap/`：`GameInstance`
    - `Gameplay/`：阶段机、`GameState`
    - `Shelter/`：庇护所、幸存者
    - `Combat/`：战斗 ASC、卡牌、Manager
    - `UI/`：**Demo 面板与可玩接入**（`AppFlowController`、`*Panel`、`PlayableLoopBootstrap`）
  - `Assets/Scenes/SampleScene.unity`：当前可直接 Play（可为空场景）
  - `Assets/Tests/EditMode/`：逻辑回归测试
- `docs/ai/`：工程协作与设计
  - **`UI_HANDOFF.md`：UI Demo 交接说明（UI 同学优先读）**
  - `ROADMAP.md`：技术路线图
  - `PROJECT_CONTEXT.md` / `BOOTSTRAP_DIGEST.md`：项目快照与会话恢复
  - `ACTIVE_WORK.md` / `FEATURE_REGISTRY.md` / `PROGRESS_LOG.md` / `TECH_DEBT.md`
  - `designs/`：Feature 设计（含 `CORE-F03-playable-loop.md`）
- `docs/designs/`：产品设计源（含 PDF、设计师反馈）
- `.cursor/rules/`：Agent 协作规则

## 当前可玩 Demo（给 UI 接手）

**状态：** 主线已可在 Play Mode 跑通（主菜单 → 庇护所 → 战斗选 5 Commit → 凯旋 → 次日）。  
**表现：** 运行时生成的极简 uGUI + **TMP 文本**（灰底按钮），细节靠 Console Log（`[Flow]` / `[Shelter]` / `[Combat]`）。

### 怎么跑

1. Unity `2022.3.62f3c1` 打开 `SixDaysRemaining/`
2. 打开 `SampleScene` → Play（无需在场景里预挂脚本；`PlayableLoopBootstrap` 会自动拉起）
3. Start → Depart → `1`–`8` 选满 5 张 → Enter 提交（或 F 逃离）→ Continue

### UI 相关入口

| 内容 | 位置 |
|------|------|
| 交接全文（面板、命名、接口表、重构建议） | [`docs/ai/UI_HANDOFF.md`](docs/ai/UI_HANDOFF.md) |
| 可玩接入设计 | [`docs/ai/designs/CORE-F03-playable-loop.md`](docs/ai/designs/CORE-F03-playable-loop.md) |
| UI 脚本 | `SixDaysRemaining/Assets/Scripts/UI/` |
| 编排胶水 | `AppFlowController`（切面板、出门开战、凯旋结算） |
| 业务门面 | `GameInstance` → `Gameplay` / `Shelter` / `Combat`；打牌只调 `PlayerCombatComponent` |

**UI 重构原则：** 可换 Prefab/美术/布局；不要在 UI 内重写伤害或日结；新交互优先接已有 API。

### 推荐分支

- 可玩 Demo + UI 胶水：`feat/playable-loop`
- 战斗逻辑：`feat/combat`（若尚未合入，请基于或先合并后再做正式 UI）

## 适用对象与使用建议

- **UI / 表现：** 先读 `docs/ai/UI_HANDOFF.md`，再看 `Scripts/UI/`
- **设计师：** `PROJECT_CONTEXT.md`、`docs/designs/`、`ACTIVE_WORK.md`
- **程序：** `ROADMAP.md`、`FEATURE_REGISTRY.md`、对应 `designs/`；进度写 `PROGRESS_LOG.md`

## 协作原则（当前）

- 文档、进度、提交信息：中文优先
- 代码注释：中文；标识符英文
- 命名：类型/属性 `PascalCase`；字段与局部变量 `camelCase`
- UI 控件建议前缀：`Txt_` / `Btn_`（见交接文档）

## 打开与验证

- Unity：`2022.3.62f3c1`
- 打开：`SixDaysRemaining/`
- 基线：
  - Console 无编译错误
  - `SampleScene` Play 能完成至少一天循环
  - Edit Mode：`Assets/Tests/EditMode` 相关测试通过

## 当前阶段

已完成（逻辑 + Demo 接入）：

- Gameplay 阶段机、庇护所幸存者、卡牌战斗编排
- 单场景可玩 Demo UI（Log 反馈为主）

交接中：

- **正式 UI 表现**交给 UI 协作者（见 `UI_HANDOFF.md`）

讨论中 / 未开：

- 幸存者特质与庇护所交互（`SHLT` 下一 feat）
- 突发事件系统（`EVT-F01`，延后）
- 黑化牌等战斗进阶
