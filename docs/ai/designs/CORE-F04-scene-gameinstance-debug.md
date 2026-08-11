# CORE-F04 Scene-owned GameInstance + Hybrid Debug

## 元信息

- **ID:** `CORE-F04`
- **类型:** `Refactor`
- **状态:** `Draft`
- **负责人:** `Max`
- **最后更新：** `2026-08-11`
- **分支（建议）：** `feat/core-debug`
- **相关：** `CORE-F03`、`COMB-F09`、`EVT-F01`、`FEATURE_REGISTRY.md`

## TL;DR

把运行入口从“Scene 预置 + 缺失时代码补建”的混合模式，收敛成 **Scene 预置 `GameInstance`** 的单一路径。  
同时引入一套 **Hybrid Debug**：Inspector 常驻参数 + 游戏内 `~` 控制台，支持命令输入、Tab 补全、候选下拉、即时回显。  
目标是让调试能力脱离临时脚本入口，成为系统级基础设施，方便后续重构事件、战斗和庇护所。

---

## 范围

### In

- `GameInstance` 必须由 Scene 预置；移除 `UiSceneBootstrap` 对 `GameObject("GameInstance")` 的兜底创建
- Debug 参数 Inspector 化：起始腐蚀、跳过战斗、玩家无敌、饥饿流失倍率、自动胜利/失败等
- 游戏内 `~` 控制台：输入框、命令执行、日志回显、Tab 补全、候选列表向上预览
- Debug 命令注册表与执行上下文（读取 `GameInstance` / `GameplaySubsystem` / `ShelterManager` / `CombatManager`）
- 让 `AppFlowController` / `UiSceneBootstrap` / HUD 能安全感知 Debug 模式，但不直接保存 Debug 逻辑
- 明确 `AppFlowController` 暂保留在 `UI/` 目录；但必须剥离事件/战斗结算/腐蚀写入等业务逻辑，仅保留屏幕路由与输入转发

### Out

- 正式存档系统
- 远程控制台 / 网络调试
- 第三方命令框架
- 发布版作弊保护（首版只做开发态入口）

---

## 现状、目标与差距

- **当前行为：** `UiSceneBootstrap` 若 Inspector 未拖 `GameInstance`，会直接创建一个新的 `GameObject` 并挂上 `GameInstance`；`GameInstance` 仅内建 `debugStartCorruption`；`AppFlowController` 以 UI 组件名义承接了大量流程职责。
- **目标行为：** Scene 中有且仅有一个可见的 `GameInstance`，其 Debug 选项能在 Inspector 和运行时控制台统一生效。
- **差距：** 缺少显式的场景拥有权、缺少 Debug 配置容器、缺少命令系统、缺少运行时调试 UI。

---

## Design

### Option A（recommended）

- 描述：
  - 保留 `GameInstance` 作为 `MonoBehaviour` 真相源，但改为 **必须场景预置**
  - 新增 `DebugRunSettings`（可序列化类）作为 Inspector 配置容器
  - 新增 `DebugCommandConsole` / `DebugCommandRegistry` / `DebugCommandContext`
  - `AppFlowController` 保留在 `UI/`，仅保留“屏幕路由 + 输入转发”职责；事件与腐蚀写入走 subsystem/网关
  - 由 `UiSceneBootstrap` 负责“绑定现有对象”，不再负责“创建核心对象”
- 好处：
  - 调试参数一眼可见，可直接保存在场景
  - 控制台与 Inspector 共用一套状态，不会出现两套 debug 入口打架
  - 对后续 `GameEventSubsystem`、战斗规则调试最友好
- 风险：
  - 场景装配要求提高；空场景或缺引用会比现在更早失败

### Option B

- 描述：保留自动创建 `GameInstance`，只是在其上新增更多 Debug 字段和控制台
- 为什么没选：
  - 继续保留“启动路径不唯一”的问题
  - 你希望直接在 Scene 中调参数，这和 Option B 的代码兜底本质冲突

---

## 设计细节

### 1) 启动所有权

现有路径：
- `UiSceneBootstrap.Awake()` 中，若 `gameInstance == null && GameInstance.Instance == null`，则创建新物体
- `GameInstance.Awake()` 内 new `GameplaySubsystem` / `CombatManager`

调整后：
- `GameInstance` 必须预置在 Scene
- `UiSceneBootstrap` 只允许：
  - 读取现有 `gameInstance`
  - 绑定 player / enemyPrefab / combatRoot / View
  - 校验缺引用时显性报错
- `AppFlowController` 暂不强制迁目录（保持在 `Assets/Scripts/UI/`），先完成职责收敛；后续如目录整理再单独做
- `PlayableLoopBootstrap` 若仍保留，只允许用于“纯 Demo 快启”或 Editor-only 场景

### 2) Debug 数据面

建议容器：

```csharp
[Serializable]
public class DebugRunSettings
{
    public int startCorruption;
    public bool playerInvincible;
    public bool skipCombat;
    public int hungerDecayOverride;
    public bool enableConsole = true;
}
```

说明：
- `hungerDecayOverride <= 0` 视为“不覆盖默认规则”
- `skipCombat` 定义为：点击出征后直接产出一份可配置结算结果，而不是再进入 `CombatView`
- `playerInvincible` 应通过战斗层显式分支实现，而不是偷偷改 HP

### 3) 控制台

建议命令族：
- 第一批必做：
  - `debug.help [prefix]`
  - `run.corruption set <n>`
  - `run.day set <n>`
  - `run.food add <n>`
  - `run.phase set <Prep|Combat|Triumph|Ending>`
  - `combat.skip`
  - `combat.win`
  - `shelter.hungerDecay set <n>`
- 第一批可选但强烈建议：
  - `combat.invincible on|off`
  - `content.reload`
  - `shelter.takein <defId>`
  - `shelter.kill <defId>`
- 第二批再补：
  - `combat.lose`
  - `run.seed set <n>`
  - `event.queue show`
  - `event.fire <eventId>`

建议按命名空间分组：
- `debug.*`：帮助、日志、开关
- `run.*`：day / phase / food / corruption / seed
- `combat.*`：skip / win / lose / invincible
- `shelter.*`：hungerDecay / takein / kill
- `event.*`：queue / fire / reload

UI 形态：
- `~` 呼出/隐藏
- 输入框底部，候选列表向上展开
- Tab：补全当前唯一命令；若多候选则轮换或聚焦列表首项
- Enter：执行；Esc：关闭

首批命令取舍理由：
- 优先覆盖“快速进局、快速过战斗、快速压榨日结、快速看腐蚀边界”四类调试需求
- 先不上太多低频命令，避免 registry 一开始就失控

### 4) 命令上下文

命令实现不直接静态抓全局，统一从 `DebugCommandContext` 取：
- `GameInstance`
- `GameplaySubsystem`
- `ShelterManager`
- `CombatManager`
- 可选 `AppFlowController`

这样便于 Edit Mode 测试命令解析与行为，不绑定具体 View。

---

## 实现注意点

- 影响的关键模块：
  - `Assets/Scripts/Bootstrap/GameInstance.cs`
  - `Assets/Scripts/UI/UiSceneBootstrap.cs`
  - `Assets/Scripts/UI/AppFlowController.cs`（先剥离事件/腐蚀写入职责）
  - 新增 `Assets/Scripts/Debug/` 或 `Assets/Scripts/Bootstrap/Debug/`
- 旧路径的迁移/删除计划：
  - 移除 `UiSceneBootstrap` 的 `new GameObject("GameInstance")`
  - 保留 `GameInstance.Instance` 单例访问，但其来源必须是场景对象
- 兼容性假设：
  - `SampleScene` 会成为首个必须配置好 `GameInstance` 的场景
  - Debug 控制台先只保证 Editor/Standalone 输入

## 验证

- 构建/验证命令：Unity Play in `SampleScene`
- 测试命令（可多条）：
  - Edit Mode：命令解析、命令补全、Debug settings 应用测试
  - Play：`~` 呼出、Tab 补全、命令结果即时生效
- 人工核对点：
  - 缺失 `GameInstance` 时能显性报错
  - Inspector 改起始腐蚀/跳战斗/饥饿倍率后，新开局立刻生效
  - 控制台与 Inspector 改同一项时不互相打架

## 验收清单

- [ ] `GameInstance` 由 Scene 拥有，运行路径唯一
- [ ] Inspector Debug 参数可直接驱动新开局与运行中测试
- [ ] `~` 控制台具备输入、Tab 补全、候选预览、结果回显
- [ ] 跳战斗 / 无敌 / 饥饿流失覆盖可用
- [ ] 已更新进度日志
- [ ] Feature 注册表状态已同步
