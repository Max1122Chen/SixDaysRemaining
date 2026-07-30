# UI Demo 交接说明（给 UI / 程序协作者）

> **最后更新：** `2026-07-30`  
> **分支：** `feat/playable-loop`（提交 `5cea410` 一带）  
> **设计源：** `docs/ai/designs/CORE-F03-playable-loop.md`  
> **目的：** 说明当前 **可玩 Demo UI** 长什么样、怎么跑起来、业务接口在哪、UI 层该碰什么/不该碰什么。

---

## 1. 一句话现状

逻辑层（阶段机 / 庇护所 / 卡牌战斗）已可跑通；当前 UI 是 **运行时生成的极简 uGUI Demo**（灰底按钮 + 文本），反馈主要靠 **Console Log**，不是正式美术界面。

**接手 UI 的人：** 可以用 Prefab/场景重做表现，但应继续调用现有 `AppFlowController` / `GameInstance` / 战斗与庇护所 API，**不要在 UI 里重写伤害或日结公式**。

---

## 2. 如何运行 Demo

| 项 | 说明 |
|----|------|
| Unity | `2022.3.62f3c1`，打开工程目录 `SixDaysRemaining/` |
| 场景 | `Assets/Scenes/SampleScene.unity`（可为空场景） |
| 启动方式 | Play 后 `PlayableLoopBootstrap` 经 `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` **自动创建**，无需在场景里预挂脚本 |
| 操作路径 | Start →（可选 Alloc / +3 Food）→ Depart → 选满 5 张 Commit 或 Flee → Continue → 次日 Shelter |
| 战斗快捷键 | `1`–`8` 选牌；`Enter` Commit；`C` / `Backspace` Clear；`F` Flee（焦点在 Game 视图） |
| 日志前缀 | `[Flow]` 阶段；`[Shelter]` 分配/入库/日结；`[Combat]` 手牌/HP/出牌 |

**注意：** 自动拉起是原型便利；正式流程可改为场景内预置 `PlayableLoopBootstrap`（或关掉 `AutoBootIfNeeded`，只保留场景挂载）。

---

## 3. Demo UI 长什么样

### 3.1 面板（同时只显示一块）

| 面板 GO / 脚本 | 作用 |
|----------------|------|
| `MainMenuPanel` | 标题 + Start / Quit |
| `ShelterPanel` | 天数/阶段/存粮/腐蚀/人口；幸存者列表；分配、+食物、出门 |
| `CombatPanel` | 敌我 HP/Block；手牌 8 钮；Commit / Clear / Flee |
| `TriumphPanel` | 战斗结果文案 + Continue（入库、腐蚀、日结、进次日） |
| `EndingPanel` | 简单 Ending + 回主菜单 |

### 3.2 控件命名约定（请 UI 重构时尽量保持）

| 前缀 | 用途 | 例 |
|------|------|-----|
| `Txt_` | Text | `Txt_Status`, `Txt_Header`, `Txt_Selection` |
| `Btn_` | Button | `Btn_Start`, `Btn_Depart`, `Btn_Hand1`…`Btn_Hand8` |
| `Root_` | 可选分区空节点 | `Root_HandButtons` |

GO 名与脚本类名一致：`ShelterPanel` GO ↔ `ShelterPanel.cs`。

### 3.3 刻意没做的（留给正式 UI）

- 卡面美术、选中高亮、血条 Fill、动画、多场景
- TMP（当前用 `UnityEngine.UI.Text`）
- 新 Input System Action Map

---

## 4. 架构：谁负责 UI、谁负责逻辑

```text
PlayableLoopBootstrap          // 运行时搭 Canvas / Player / Enemy 模板（可替换为场景预置）
AppFlowController              // 面板显隐 + 阶段胶水（出门开战、凯旋结算）
  ├─ *Panel                    // 只绑按钮、Refresh 文本、把输入转给 Flow / 业务 API
  └─ GameInstance
        ├─ GameplaySubsystem   // 阶段：Prep → Combat → TriumphReturn → 次日 Prep / Ending
        ├─ ShelterManager      // 食物、幸存者、日结
        └─ CombatManager       // 战斗会话；不提供选牌 API
              └─ PlayerCombatComponent / EnemyCombatComponent（场景 GO）
```

**硬边界**

- 选牌 / Commit：只调 `PlayerCombatComponent`
- 推进敌方回合 / Flee：只调 `CombatManager.NotifyPlayerCommitted` / `Flee`
- **禁止**在 Panel 里写 `HP -= x` 或直接改 `foodStock`

---

## 5. UI 相关脚本一览

路径：`SixDaysRemaining/Assets/Scripts/UI/`（程序集 `SixDaysRemaining.UI`）

| 脚本 | 职责 | 对外主要方法 |
|------|------|----------------|
| `PlayableLoopBootstrap` | AfterSceneLoad 自动 boot；建 UI/Player/Enemy 模板 | （静态 AutoBoot + Awake） |
| `AppFlowController` | 阶段面板路由与开战/结算 | 见下节 |
| `MainMenuPanel` | 主菜单按钮 | `Bind`, `BindButtons` |
| `ShelterPanel` | 庇护所状态与分配/出门 | `Bind`, `BindRefs`, `Refresh` |
| `CombatPanel` | 战斗输入与状态刷新 | `Bind`, `BindRefs`, `Refresh` |
| `TriumphPanel` | 展示 `CombatResult` | `Bind`, `BindRefs`, `ShowResult` |
| `EndingPanel` | Ending 与回菜单 | `Bind`, `BindRefs`, `Refresh` |

各 Panel 的 `Bind(AppFlowController)`：挂按钮 `onClick`。  
`BindRefs(...)`：供 Bootstrap **代码建 UI** 时注入引用；若改为场景/Prefab，可在 Inspector 拖好 SerializeField，仍调一次 `Bind(flow)`。

---

## 6. `AppFlowController` 接口（UI 编排入口）

| 方法 | 何时调用 | 做什么 |
|------|----------|--------|
| `Bind(...)` | 启动时 | 注入 `GameInstance` 与五个 Panel |
| `ShowMainMenu` / `ShowShelter` / `ShowCombat` / `ShowEnding` | 切面板 | `SetActive` 互斥显示 |
| `OnStartNewGame` | Start 按钮 | `GameInstance.StartNewGame(42)` → Shelter |
| `OnDepart` | Depart 按钮 | 校验 `ExpeditionPrep` → `AdvancePhase`→Combat → `StartCombat` → 显示战斗 |
| `OnCombatFinished(result)` | Commit/Flee 结束战斗后 | `Combat→TriumphReturn`，显示凯旋 |
| `OnTriumphContinue` | Continue | 入库 + 腐蚀 + 日结 → `TriumphReturn→次日 Prep/Ending` |
| `OnBackToMenu` | Ending | 回主菜单 |
| `Game` | 属性 | 取 `GameInstance` |

阶段必须与面板一致，否则会卡死（例如凯旋 Continue 前必须已是 `TriumphReturn`）。详见 CORE-F03 / 近期 bugfix。

---

## 7. 业务 API（UI 可调用的稳定面）

### 7.1 `GameInstance`（`Bootstrap`）

| API | 说明 |
|-----|------|
| `Instance` | 单例 |
| `Gameplay` / `Shelter` / `Combat` | 子系统 |
| `PlayerCombat` / `EnemyPrefab` / `CombatRoot` | 战斗场景引用 |
| `StartNewGame(seed)` | 新局；默认开局食物见 `StartingFoodStock`（5） |
| `ReturnToMainMenu()` | 回菜单并清理敌人 |
| `DebugAllocateFood(index, amount)` | Shelter 分配（Demo 按钮在用） |
| `DebugDepositFood(amount)` | Debug 加粮 |
| `BindCombatSceneRefs(player, enemyPrefab, root)` | Bootstrap 注入引用 |

### 7.2 `GameplaySubsystem`（纯 C#）

| API | 说明 |
|-----|------|
| `State` | `day`, `foodStock`, `corruption`, `rngSeed`, `currentPhase`, … |
| `CurrentPhase` | `ExpeditionPrep` / `Combat` / `TriumphReturn` / `Ending` |
| `StartNewRun(seed)` | 新局状态 |
| `AdvancePhase()` | 推进一步（由 AppFlow 在出门/战斗结束/凯旋时调用） |

### 7.3 `ShelterManager`（纯 C#）

| API | 说明 |
|-----|------|
| `Survivors` / `Population` | 列表与存活人数 |
| `AllocateFood(survivor, amount)` | 分配；失败返回 false |
| `DepositFood(amount)` | 凯旋入库 |
| `ProcessEndOfDay()` | 日结扣饱食度、更新 status |
| `InitializeDefaultRoster(food)` | 默认 Alice/Bob |

### 7.4 战斗（UI 最常用）

**`CombatManager`（纯 C# 编排，不提供选牌）**

| API | 说明 |
|-----|------|
| `StartCombat(config, player, enemyPrefab, combatRoot)` | 开战；Instantiate 敌人 |
| `IsPlayerTurn` / `IsFinished` / `Session` / `Result` | UI 刷新与门控 |
| `NotifyPlayerCommitted()` | 玩家 Commit 成功后调用 |
| `Flee()` | 逃离 |
| `CleanupSpawnedEnemy()` | 清敌人 GO |

**`PlayerCombatComponent`（挂 Player GO）**

| API | 说明 |
|-----|------|
| `HandLimit=8`, `CommitCount=5` | 常量 |
| `Deck.Hand` / `Selection` / `DrawPile` | 只读列表 |
| `SelectFromHand(index)` / `ClearSelection()` / `DeselectAt` | 选牌 |
| `CommitPlay(enemy)` | 选满 5 才成功；按选中顺序结算 |

**展示用属性（Refresh）**

- `player.Attributes.HP` / `MaxHP` / `Block`
- `enemy.Attributes` 同上  
- 手牌文案：`Hand[i].Def.Id`（如 `strike`）

**牌序种子（非 UI，但影响手感）**

- 出门时：`DeckSeed = rngSeed + day * 997`（同局不同天洗牌不同）

---

## 8. 建议的 UI 重构路径（给接手同学）

1. **保留** `AppFlowController` 与各 Panel 的方法语义（可改实现与布局）。  
2. 用 Prefab 替换 `PlayableLoopBootstrap` 里的代码建 UI；场景预置 Panel，SerializeField 拖引用，启动时仍 `flow.Bind(...)`。  
3. 若去掉 AutoBoot：删除或 `#if` 掉 `AutoBootIfNeeded`，在 Bootstrap 场景挂好物体。  
4. 表现增强（高亮、血条）只读业务状态，在 `Refresh()` 里更新。  
5. 需要新按钮时：Panel 调已有 API，或让程序在 Shelter/Combat 加 API，再接线。  
6. Edit Mode 业务测试在 `Assets/Tests/EditMode/`；UI 以 Play Mode 人工验收为主。

---

## 9. 相关文档与分支

| 文档 | 用途 |
|------|------|
| `designs/CORE-F03-playable-loop.md` | 可玩接入设计全文 |
| `designs/COMB-feat-chain.md` | 战斗设计链索引 |
| `designs/SHLT-F01-shelter-survivor.md` | 庇护所/幸存者 |
| `ROADMAP.md` | 总架构 |
| `ACTIVE_WORK.md` | 当前队列 |

| 分支 | 内容 |
|------|------|
| `feat/combat` | 战斗逻辑（宜先合入或基于其上） |
| `feat/playable-loop` | 可玩 Demo + UI 胶水（当前交接主分支） |

**未做 / 下一批可能：** 正式 UI、幸存者特质与交互（`SHLT-F02` 讨论中）、突发事件（`EVT-F01`）、黑化牌。

---

## 10. 验收清单（接手前冒烟）

- [ ] Play `SampleScene`，Console 无编译错误  
- [ ] Start → Depart → 选 5 → Commit，能进 Triumph  
- [ ] Continue 后 `phase=ExpeditionPrep` 且 `day` +1，可再次出门  
- [ ] Flee 能结束并进 Triumph  
- [ ] 过滤 `[Flow]`/`[Shelter]`/`[Combat]` 能跟完一天  

有问题先看 `AppFlowController` 阶段推进与 `CombatPanel.Commit/Flee` 是否调用了 Manager。
