# BUG-EVT-001 BeforeDepart 事件结果 Overlay 卡死

## 元信息

- **ID:** `BUG-EVT-001`
- **域：** `EVT`
- **状态：** `Fixed`（待你 Play 复验）
- **发现日期：** `2026-08-12`
- **负责人：** `Max`
- **相关：** `EVT-F02`、`CORE-F05`

## 现象

Day4 幼童跑路事件触发后，选择唯一选项并进入「事件结果」界面，点击继续后 UI 停留在结果 overlay，不回到庇护所，表现为“卡死在幼童离开了的事件结算界面”。

## 复现

1. Day2 幼童事件选择婉拒
2. Day3 幼童事件再次婉拒
3. Day4 进入 `BeforeDepart`，触发 `child_stole_food_day4`
4. 点击唯一选项，再点击结果界面的“继续”
5. 观察到 overlay 未关闭

## 根因

`GameEventSubsystem.FinishSequence()` 会在最后一个事件结果继续后触发 `EventSequenceFinished`。  
`AppFlowController.HandleEventSequenceFinished()` 对：

- `AfterTriumph`：继续链到 `BeforeDayEnd`
- `BeforeDayEnd`：切到日结 overlay
- `BeforeDepart`：**只** `RefreshHud()`，**没有** `CloseOverlay()`

因此 `BeforeDepart` 的结果界面在序列结束后失去后续驱动，但仍停留在前台。

## 修复

- 在 `AppFlowController.HandleEventSequenceFinished()` 的 `BeforeDepart` 分支补 `CloseOverlay()`
- 新增 EditMode 回归测试：`AppFlowControllerTests.HandleEventSequenceFinished_BeforeDepart_ClosesOverlay`

## 退出条件

- [x] `BeforeDepart` 最后一个事件结果继续后会关闭 overlay
- [x] 新增 EditMode 回归测试
- [ ] 你在 Unity Play 中复验通过
