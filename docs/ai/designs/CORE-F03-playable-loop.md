# CORE-F03 可玩接入层：单场景 + Demo UI + 战斗实体挂场景

## 元信息

- **ID:** `CORE-F03`
- **类型:** `Feature`
- **状态:** `In Progress`
- **负责人:** `Max`
- **最后更新：** `2026-07-30`（实现中：`feat/playable-loop`）
- **分支（建议）：** `feat/playable-loop`
- **相关：** `CORE-F02`、`SHLT-F01`、COMB 战斗链、`ROADMAP.md`、`FEATURE_REGISTRY.md`

## TL;DR

单场景跑通主线：极简 **Demo UI（`*Panel` 脚本）** + **Log** + **按钮/数字键**。  
C# **不能**同时继承 `MonoBehaviour` 与另一个类 → 将 **`CombatComponentBase` 改为 `MonoBehaviour`**，保留  
`Base → CombatComponent → Player/Enemy` 单链继承（比组合转发更简单）。  
Player 场景预置并由 `GameInstance` 引用；Enemy 由 `CombatManager` Instantiate。  
场景 Panel GO 与脚本均名 `XXXPanel`；子控件统一 `Txt_` / `Btn_` 等前缀。

---

## 范围

### In

- 单场景 Bootstrap + 五块 Demo 面板（见 §6）
- **`CombatComponentBase : MonoBehaviour`**，整条战斗组件继承链挂 GO
- `GameInstance` 引用场景 Player；持有 `Gameplay` / `Shelter` / `CombatManager`
- `CombatManager` 开战生成敌人、结束 Destroy
- Demo UI：`*Panel` 脚本 + 阶段显隐；战斗 UI + 数字快捷键
- Log 为主；面板仅测试必需控件
- Edit Mode：`AddComponent`；Play Mode 人工主验收

### Out

- 精美 UI / 动画 / 卡面 / 血条动效 / 选中高亮
- 多场景、黑化、意图、大门双路线、事件阶段
- 新 Input System Action Map（首版 `Input.GetKeyDown`）
- `GameplaySubsystem` / `ShelterManager` / `CardInstance` / `AttributeSet` 改成 MB
- `CombatManager` 改成 MB（本 feat 不强制）

---

## 现状、目标与差距

- **当前：** 战斗实体 `new` 纯 C#；无场景 Player/Enemy；无 Demo UI。
- **目标：** Play 可见实体 GO；点/键能打完至少一天并回写。
- **差距：** Base MB 化、Prefab、Panel UI、AppFlow。

---

## 已拍板

| # | 决策 |
|---|------|
| 1 | **单场景**多面板 |
| 2 | 战斗输入：**UI + 数字快捷键** |
| 3 | 反馈以 **Log 为主** |
| 4 | **不能**多重继承；采用 **`CombatComponentBase : MonoBehaviour`** |
| 5 | 继承链：`Base → CombatComponent → Player/Enemy`（不搞组合套壳） |
| 6 | Player **场景预置**，`GameInstance` 引用 |
| 7 | Enemy 由 **`CombatManager` 生成**；结束默认 **Destroy** |
| 8 | UI：**GO 与脚本均名 `XXXPanel`**（不用 Widget/Driver） |
| 9 | 控件命名前缀：`Txt_` / `Btn_`（见 §6.0） |
| 10 | 黑化等延后 |

---

## 设计

### 1) 分层与对象生命周期

```text
Bootstrap.unity
├─ GameInstance (MB, DDOL)
│    ├─ new GameplaySubsystem()
│    ├─ new ShelterManager(...)
│    ├─ new CombatManager(...)
│    └─ ref → Player
│              └─ PlayerCombatComponent
│
├─ World/CombatRoot
│    └─ (runtime) Enemy_xxx
│         └─ EnemyCombatComponent
│
├─ AppFlowController
└─ Canvas
     ├─ MainMenuPanel    + MainMenuPanel.cs
     ├─ ShelterPanel     + ShelterPanel.cs
     ├─ CombatPanel      + CombatPanel.cs
     ├─ TriumphPanel     + TriumphPanel.cs
     └─ EndingPanel      + EndingPanel.cs
```

```text
开战：Instantiate(enemyPrefab, combatRoot) → Init/Bind → CombatSession(player, enemies)
结束：Destroy(enemyGO) → Result 交 AppFlow 回写
```

### 2) 继承问题与拍板方案

#### 2.1 为什么不能「同时继承」？

C# **单类继承**：`class CombatComponent : MonoBehaviour, CombatComponentBase` **非法**（接口除外）。  
`MonoBehaviour` 与 `CombatComponentBase` 都是 class → 不能并列继承。

#### 2.2 采用：`CombatComponentBase` 本身变为 MonoBehaviour

```text
AttributeData / AttributeSet / CombatAttributeSet   // 仍纯 C#（值与 Set，不挂 GO）
CombatComponentBase : MonoBehaviour                 // ASC：RegisterSet / Get / Set / OnChange
  └─ CombatComponent : CombatComponentBase          // 伤害/格挡 API
        ├─ PlayerCombatComponent                    // 牌库 / 选 5 Commit
        └─ EnemyCombatComponent                     // Pattern / ExecuteTurn
```

**好处：** 无转发层；现有 `Player : CombatComponent : Base` 结构几乎不动，只把 Base 基类换成 MB。  
**代价：** 不能再 `new PlayerCombatComponent()`，一律 `go.AddComponent<...>()`；抽象基类不要直接 Add 到场景（只 Add 具体 Player/Enemy）。

`Awake`/`OnEnable`：Base 可在此确保 AttributeSet 已注册（若构造逻辑需挪到 `Awake`，实现时注意 Edit Mode 里 `AddComponent` 后立刻调 `InitCombatant` 的时序）。

#### 2.3 谁挂在哪

| 对象 | 放置 | 谁创建 | 引用方 |
|------|------|--------|--------|
| Player | 场景 GO | 预置 | `GameInstance.playerCombat` |
| Enemy | Prefab | `CombatManager` Instantiate | Session |
| `CombatManager` | 无 GO | `GameInstance` `new` | Panel / AppFlow |

```csharp
void StartCombat(
  CombatStartConfig config,
  PlayerCombatComponent player,
  EnemyCombatComponent enemyPrefab, // 或 GameObject
  Transform combatRoot);
```

### 2.4 测试迁移

```csharp
var go = new GameObject("P");
var player = go.AddComponent<PlayerCombatComponent>();
player.InitCombatant(30f);
// TearDown: Object.DestroyImmediate(go);
```

### 3) 哪些是 MonoBehaviour

| 类型 | MB？ |
|------|------|
| `GameInstance` / `AppFlowController` / `*Panel` | 是 |
| `CombatComponentBase` 及 Player/Enemy | **是** |
| `GameplaySubsystem` / `ShelterManager` / `CombatManager` | 否 |
| `CombatSession` / Cards / `AttributeSet` | 否 |

---

### 4) 主流程（AppFlow）

```text
MainMenu → StartNewGame → ShelterPanel
Shelter Depart → AdvancePhase → StartCombat → CombatPanel
Commit/Notify 或 Flee → Finished → TriumphPanel
Continue → DepositFood + corruption + EndOfDay + Advance → Shelter 或 Ending
Ending → MainMenu
```

主线写回食物/腐蚀。

---

### 5) 战斗输入

| 输入 | 行为 |
|------|------|
| 手牌 `Btn_Hand1..8` / 键 `1`–`8` | `SelectFromHand`；已选再点不取消 |
| `Btn_Commit` / Enter | `CommitPlay` → 成功则 `NotifyPlayerCommitted` |
| `Btn_Clear` / C 或 Backspace | `ClearSelection` |
| `Btn_Flee` / F | `Flee` |

仅玩家回合且未结束接受输入。

---

### 6) Demo UI

原则：丑但全；状态 = 少量 `Txt_` + Console。

#### 6.0 控件命名约定

| 前缀 | 用途 | 例 |
|------|------|-----|
| `Txt_` | Text / TMP_Text | `Txt_Status`, `Txt_HandHint` |
| `Btn_` | Button | `Btn_Start`, `Btn_Hand1` |
| `Img_` | 可选 Image（本 demo 尽量不用） | — |
| `Root_` | 可选分区空节点 | `Root_HandButtons` |

- **场景 Panel GO** 与 **脚本类名**一致：`ShelterPanel` GO ↔ `ShelterPanel.cs`。
- 手牌按钮用 **`Btn_Hand1`…`Btn_Hand8`**（显示编号 1–8；代码 index = 编号−1）。

#### 6.1 Canvas 层级

```text
Canvas
├─ MainMenuPanel   (MainMenuPanel.cs)
├─ ShelterPanel    (ShelterPanel.cs)
├─ CombatPanel     (CombatPanel.cs)
├─ TriumphPanel    (TriumphPanel.cs)
└─ EndingPanel     (EndingPanel.cs)
```

`AppFlowController`：同时只激活一个 Panel（主菜单看 `AppMode`）。

#### 6.2 MainMenuPanel

| 名称 | 类型 | 用途 |
|------|------|------|
| `Txt_Title` | Text | 「六日英雄 Demo」 |
| `Btn_Start` | Button | `StartNewGame(seed=42)` |
| `Btn_Quit` | Button | 可选 Quit / Editor 仅 Log |

#### 6.3 ShelterPanel

| 名称 | 类型 | 用途 |
|------|------|------|
| `Txt_Status` | Text | day / phase / food / corruption / population |
| `Txt_Survivors` | Text | `[i] name hunger status` |
| `Btn_Alloc0` / `Btn_Alloc1` | Button | 给对应幸存者 +1 食物 |
| `Btn_DepositDebug` | Button | 可选 +3 存粮 |
| `Btn_Refresh` | Button | 刷新 Text |
| `Btn_Depart` | Button | 出门开战 |

#### 6.4 CombatPanel

| 名称 | 类型 | 用途 |
|------|------|------|
| `Txt_Header` | Text | 敌我 HP/Block、是否玩家回合 |
| `Txt_HandHint` | Text | 快捷键说明 |
| `Txt_Selection` | Text | `Sel: 0,2,4 (3/5)` |
| `Btn_Hand1`…`Btn_Hand8` | Button | 选牌；无牌禁用，文案 `—` |
| `Btn_Commit` / `Btn_Clear` / `Btn_Flee` | Button | 见 §5 |
| `Txt_LogHint` | Text | 「细节见 Console [Combat]」 |

**不做**选中变色；靠 `Txt_Selection` + Log。  
每次操作后 `Refresh()` + `[Combat]` 手牌快照。

#### 6.5 TriumphPanel

| 名称 | 类型 | 用途 |
|------|------|------|
| `Txt_Result` | Text | Outcome / Food / Corruption / Turns |
| `Btn_Continue` | Button | 回写 + 日结 + Advance |

#### 6.6 EndingPanel

| 名称 | 类型 | 用途 |
|------|------|------|
| `Txt_Ending` | Text | 简单结束说明 |
| `Btn_ToMenu` | Button | 回主菜单 |

#### 6.7 脚本职责

```text
MainMenuPanel / ShelterPanel / CombatPanel / TriumphPanel / EndingPanel
  → 各自按钮与 Refresh；CombatPanel.Update 读快捷键
AppFlowController
  → 显隐 Panel；Depart 开战；战斗结束切 Triumph；Continue 结算
```

Panel **禁止**手写改 HP。

#### 6.8 Inspector 必填

**GameInstance：** `playerCombat`, `enemyPrefab`, `combatRoot`  
**AppFlowController：** 五个 Panel 根、`GameInstance`  
**CombatPanel：** `Txt_*`、`Btn_Hand*`、Commit/Clear/Flee  

缺引用 → `Awake`/`OnValidate` LogError。

---

### 7) Log 约定

```text
[Flow] [Shelter] [Combat]
```

例：`[Combat] Hand: [0]strike ... | Sel: 0,2 (2/5)`

### 8) 文件布局

```text
Assets/Scripts/Combat/Framework/CombatComponentBase.cs  // : MonoBehaviour
Assets/Scripts/Combat/CombatComponent.cs
Assets/Scripts/Combat/PlayerCombatComponent.cs
Assets/Scripts/Combat/EnemyCombatComponent.cs
Assets/Scripts/Combat/CombatManager.cs                  // 改 StartCombat 签名
Assets/Prefabs/Combat/Enemy.prefab
Assets/Scripts/Bootstrap/GameInstance.cs
Assets/Scripts/Bootstrap/AppFlowController.cs
Assets/Scripts/UI/
  MainMenuPanel.cs
  ShelterPanel.cs
  CombatPanel.cs
  TriumphPanel.cs
  EndingPanel.cs
  SixDaysRemaining.UI.asmdef
Assets/Scenes/Bootstrap.unity
```

### 9) 实现步骤

| 步骤 | 内容 |
|------|------|
| S0 | `CombatComponentBase : MonoBehaviour`；Manager Instantiate；测试 AddComponent |
| S1 | 场景 Player + Enemy Prefab + GameInstance 引用 |
| S2 | 五 Panel + 命名约定 + AppFlow |
| S3 | Shelter → 开战 |
| S4 | Combat 按钮+键 |
| S5 | Triumph / Ending 回写 |
| S6 | Build Settings 启动场景；文档同步 |

---

## 测试策略

1. Edit Mode 回归（MB + DestroyImmediate）  
2. Play Mode：§6 每条按钮路径  
3. 缺引用启动报错  

---

## 验收清单

- [ ] Base 为 MB；Player 场景引用；Enemy 开战生成、结束 Destroy
- [ ] 五 Panel GO/脚本同名；控件 `Txt_`/`Btn_` 前缀
- [ ] 战斗 UI + 数字键；Log 可测
- [ ] 结算写回；跨日或 Ending
- [ ] Edit Mode 绿；注册表同步

## 依赖与后续

- 本 feat **含**战斗 Base MB 化  
- 后续：正式 UI、黑化、事件  

## 审阅通过后

开 `feat/playable-loop` 实现。

---

## 附录：本轮还可讨论（未阻塞可默认）

| 项 | 默认建议 | 是否需你拍板 |
|----|----------|----------------|
| 启动场景名 `Bootstrap` vs 改 SampleScene | 新建 `Bootstrap.unity`，Sample 可留 | 可默认 |
| Enemy Prefab 是否带简单 Cube 标识 | 带，便于 Hierarchy 辨认 | 可默认 |
| Player 非战斗阶段是否 `SetActive` | 常驻激活即可 | 可默认 |
| `Btn_Quit` 要不要 | 要，Editor 内只 Log | 可默认 |
| seed 是否 UI 可改 | 固定 42 | 可默认 |
| 开局自动 `DepositDebug` 给点粮 | 开局 `foodStock=5` 便于测分配 | **建议拍板** |
| Lose 后是否进 Triumph 还是专用失败文案 | 仍进 Triumph，`Txt_Result` 显示 Lose | 可默认 |
| Combat 中 Shelter 幸存者是否只读展示 | 不展示，减少面板 | 可默认 |
| UI 用 `UnityEngine.UI.Text` 还是 TMP | 默认 **uGUI Text**（少依赖） | 可默认 |
| `feat/combat` 先 merge 再开本分支 | **是** | 建议确认 |
