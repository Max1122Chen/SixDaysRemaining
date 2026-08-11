# CORE-F05 AppFlow 编排收敛 + PresentationManager

## 元信息

- **ID:** `CORE-F05`
- **类型:** `Refactor`
- **状态:** `Done`（2026-08-11 实现；事件队列留 `TD-007`）
- **负责人:** `Max`
- **最后更新：** `2026-08-11`
- **分支（建议）：** 在当前 `main` / 大域分支上做；备选名 `feat/core-flow`（未经明确要求不新开）
- **相关：** `CORE-F03`、`CORE-F04`、`EVT-F01`、`FEATURE_REGISTRY.md`

## TL;DR

把当前塞在 `UI/AppFlowController` 里的 **日循环编排** 与 **UI 切屏/Overlay/HUD** 拆开：  
- **保留命名 `AppFlowController`**，迁出 `UI/`，只做日循环编排（出征 / 战斗结束 / 凯旋 / 日结 / 终局）。  
- 新增 **`PresentationManager`**（`UI/`），负责屏幕路由与呈现。  
- 二者用 **委托** 链接：Flow 不引用具体 View；View 不写业务状态。

---

## 范围

### In

- `AppFlowController` 迁至 `Assets/Scripts/Gameplay/`（审阅拍板：Gameplay 域，非 App）
- 剥离：`SwitchScreen` / Overlay / HUD 显隐 / View 引用绑定
- 新增 `PresentationManager`：持有 View 引用；实现 Flow 注入的 presentation 委托
- View `Wire` 改为：按钮 → `AppFlowController` 公开编排方法；刷新 → `PresentationManager`
- Debug / 未来 EVT：经 Flow 公开 API + presentation 委托，不绑 View
- 顺带收敛反模式：裸写 `foodStock` / `currentPhase`（改走 Gameplay / Shelter API）
- 更新 asmdef 引用（`SixDaysRemaining.App` ↔ `SixDaysRemaining.UI`）

### Out

- 完整 EVT 子系统（仍归 `EVT-F01`；本 feat 可留「事件队列仍在 Flow」的临时态，或只抽出接口边界）
- Debug 命令全集（`CORE-F04`）
- 重做全部 View Prefab / 视觉
- 改名为 `UIManager`（本 feat 采用 **`PresentationManager`**）

---

## 现状、目标与差距

- **当前：** `AppFlowController` 在 `UI/`，同时做：①切屏 ②View 胶水 ③日循环编排 ④随机事件队列 ⑤直接改业务字段。
- **目标：** Flow = 编排；Presentation = UI 变化；委托通信。
- **差距：** 缺独立 Presentation 层；Flow 与 View 双向硬绑。

---

## Design

### Option A（recommended）— 保留 AppFlowController 名 + PresentationManager

```
GameInstance
  └─ AppFlowController（App/，编排）
         │ 委托：ShowShelter / ShowCombat / ShowSettlement / ShowEnding / RefreshHud …
         ▼
     PresentationManager（UI/）
         └─ *View（展示 + 用户输入回传 Flow）
```

- **AppFlowController**
  - `OnStartGame` / `OnDepart` / `OnCombatFinished` / `OnSettlementContinue` / `OnDayEndContinue` / `ForceEnding` 等
  - 调 `Gameplay` / `Shelter` / `Combat`
  - **不** `SetActive` View，**不**持有具体 View 字段
- **PresentationManager**
  - SerializeField 绑定各 View + HUD
  - `Bind(AppFlowController flow)`：把委托赋给 Flow；View 按钮绑 Flow 方法
  - 实现切屏 / Overlay / HUD

**好处：** 命名连续；编排可 Edit Mode 测（mock presentation）；Debug/EVT 自然挂 Flow。  
**风险：** 一次迁目录 + 改 Wire；需回归整条日循环。

### Option B — 另起 RunFlowController，AppFlow 当 façade

- 为什么没选：多一层临时名；Max 明确要 **保留 AppFlowController**。

### Option C — 只抽 RunFlowService，UI 仍挂 AppFlow

- 为什么没选：不能解决「Flow 在 UI 目录、职责混杂」；与拍板不符。

---

## 设计细节

### 1) 目录与程序集

| 类型 | 路径 | 程序集 |
|------|------|--------|
| `AppFlowController` | `Assets/Scripts/App/AppFlowController.cs` | `SixDaysRemaining.App` |
| `PresentationManager` | `Assets/Scripts/UI/PresentationManager.cs` | `SixDaysRemaining.UI` |

注意环依赖：
- **App 不能引用 UI 程序集**（否则又绑回 View）。
- Presentation 委托用 `System.Action` / 带业务 DTO 的 Action（如 `Action<CombatResult>`），定义在 App 或 Shared。
- `UiSceneBootstrap`：创建/绑定 `PresentationManager`，再 `presentation.Bind(flow)`。

### 2) Presentation 委托契约（首版）

由 Flow 持有、Presentation 赋值：

```csharp
// AppFlowController 上
public Action ShowStartScreen;
public Action ShowStoryIntroScreen;
public Action ShowShelterScreen;
public Action ShowCombatScreen;
public Action ShowEndingScreen;
public Action ShowSettingsOverlay;
public Action ShowCreditsOverlay;
public Action CloseOverlay;
public Action HideHud;
public Action RefreshHud;
public Action RefreshShelterView;
public Action RefreshCombatView;
public Action RefreshEndingView;
public Action<CombatResult> ShowSettlementOverlay;
public Action ShowDayEndOverlay; // 后续可改为带 personnelChanges 的 overload
```

Flow 内示例：

```csharp
public void OnCombatFinished(CombatResult result)
{
    if (result.RunEndedByCorruption)
    {
        ForceEnding(...);
        ShowEndingScreen?.Invoke();
        return;
    }
    Gameplay.AdvancePhase();
    ShowSettlementOverlay?.Invoke(result);
    RefreshHud?.Invoke();
}
```

### 3) View 接线

- `Wire(AppFlowController flow)` → 按钮只调 `flow.OnDepart` 等
- 或 `Wire(flow, presentation)`：仅当需要 Presentation 本地方法时
- **禁止** View 写 `State.foodStock` / `phase`

### 4) 随机事件（本 feat 边界）

- **最小：** 事件队列仍暂留 Flow，但展示经 `ShowEventOverlay` 委托；选项效果走 Shelter/Gameplay API（去掉裸写）。
- **理想：** 队列迁 `GameEventSubsystem`（`EVT-F01`）；本 feat 只留钩子 `BeginAfterTriumphEvents()`。

建议：**CORE-F05 做最小清理；队列迁出留给 EVT-F01。**

### 5) 与 CORE-F04 的关系

| CORE-F04 | CORE-F05 |
|----------|----------|
| Debug 命令、门禁、业务 API 补齐 | Flow / Presentation 拆分 |
| Debug 调 Flow 公开方法 + presentation 回调 | 提供稳定委托边界 |

**建议顺序：**  
- **先审阅并实现 CORE-F05（或与 F04 的业务 API 并行）**，再让 F04 的 `run.day end/skip`、`combat.skip` 挂到干净 Flow。  
- 若 F04 已开做：Debug 可先用临时 façade；F05 合并后删 façade。

### 6) 场景装配

- `UiSceneBootstrap` 或场景预置：`PresentationManager` + `AppFlowController`（可同 GO 或分 GO）
- `GameInstance` 仍场景预置；Flow 从 Bootstrap / GameInstance 取引用

---

## 实现切片（建议）

1. **S1** — 新建 `PresentationManager`；把现有切屏/HUD/Overlay 迁入；AppFlow 暂委托转发（同文件双类亦可，但目录先迁 App）
2. **S2** — AppFlow 去掉 View 字段；只留编排 + 委托槽
3. **S3** — View Wire 改绑；Bootstrap 接线；去掉裸写 phase/food
4. **S4** — 回归日循环 Play；Edit Mode：mock presentation 测 `OnDepart` / `OnCombatFinished` 状态机
5. **S5** — 文档 / Registry / 删 UI 下旧路径

---

## 验证

- Play：主菜单 → 开局 → 庇护所 → 出征 → 结算 → 事件/日结 → 次日 / 结局
- Debug（若已合 F04）：`ShowEnding` 仍经委托
- 编译：App 程序集无 UI View 类型引用

## 验收清单

- [x] `AppFlowController` 在 `Gameplay/`，不再引用具体 `*View`
- [x] `PresentationManager` 独占切屏 / Overlay / HUD
- [x] Flow ↔ Presentation 仅委托
- [ ] 日循环 Play 回归通过（待 Unity Play）
- [x] 随机事件裸写状态已改为 API（`AddFood`）；队列留 `TD-007`
- [x] FEATURE_REGISTRY / ACTIVE_WORK / PROGRESS_LOG 已更新

## 待拍板（已决议）

1. AppFlow 落 **`Gameplay/`**（非 App/）
2. 本 feat 事件队列：**最小清理** + `TD-007`；完整迁移 → `EVT-F01`
3. 与 CORE-F04 实现顺序：**先 F05，再 F04 命令收口**（已执行）
