# 新 UI 基础实现指南

## 0. 现状

旧的 `PlayableLoopBootstrap` 与五个测试 Panel 已删除；`GameInstance` 里的
`DebugDepositFood` / `DebugAllocateFood` / `DebugProcessEndOfDay` / `DebugLogAllSurvivors`
也已移除。现在项目在任意场景 Play 时会由 `UIRoot` 自动拉起一套新的代码建 UI 原型。

## 1. 文件结构

`Assets/Scripts/UI/` 下：

| 文件 | 职责 |
| --- | --- |
| `UIRoot.cs` | Play 后自动启动，创建 GameInstance / Canvas / 所有视图 |
| `AppFlowController.cs` | 面板路由 + 开战/结算胶水，UI 只调用这里的方法 |
| `UiFactory.cs` | 统一创建 Panel / Text / Button / Slider / Toggle / Scroll 的占位工厂 |
| `UiAnim.cs` | Move / MoveAndResize / Scale / Fade / Shake 轻量动画 |
| `UiCjkFont.cs` | 运行时从系统字体生成含中文的 TMP 字体（占位） |
| `StartScreenView.cs` | 开始界面：开始游戏 / 新游戏 / 设置 / 退出 |
| `StoryIntroView.cs` | 故事背景介绍视频占位 + 跳过 |
| `SettingsView.cs` | 音量 / 全屏 / 文本速度 / 制作组 / 返回 |
| `CreditsView.cs` | 制作组名单占位 |
| `ShelterView.cs` | 庇护所状态 + 幸存者列表 + 出发 |
| `CombatView.cs` | 战斗主界面：手牌、卡槽、结算、回合横幅 |
| `CardView.cs` | 单张卡牌：拖拽、悬停高亮、拖拽阴影、选中放大 |
| `CardSlotView.cs` | 出战卡槽：拖拽悬停高亮 |
| `EnemyPreviewView.cs` | 怪物 HP/格挡/行动预告 |
| `SettlementView.cs` | 战斗结算逐行滚入 + 继续按钮 |
| `EndingView.cs` | 结局占位 |

## 2. 逐步实现路径（给 UI 同学）

### 2.1 先跑通流程
1. 打开 `Assets/Scenes/SampleScene.unity`，直接 Play。
2. 点击“开始游戏” → 故事占位 → 跳过 → 庇护所。
3. 庇护所点击“出发”进入战斗。
4. 手牌拖入下方 5 个卡槽；在卡槽之间拖动可换位；拖出卡槽回到手牌。
5. 选满 5 张后“结算”可用；结算后看怪物行动与回合横幅。
6. 战斗结束进入“战斗结算”，点“继续”回到庇护所或进入结局。

### 2.2 把代码建 UI 换成场景 Prefab
目前所有 UI 是 `UIRoot` 运行时生成的，方便原型阶段无场景工作流。
正式做法：
1. 在场景里手动搭 `Canvas` 与各 Panel，把按钮/文字拖到对应 View 的
   `[SerializeField]` 字段（目前 View 使用 `Build` 静态方法，改成字段后保留方法语义）。
2. 去掉 `UIRoot` 的 `AutoBootIfNeeded`，改成场景里放一个 `UIRoot` GameObject。
3. `AppFlowController.Bind(...)` 的调用保持不变。

### 2.3 拖拽交互怎么读
- `CardView` 实现 `IBeginDragHandler / IDragHandler / IEndDragHandler`，
  拖拽中只移动自身并抛事件，不直接写逻辑。
- `CombatView` 收到 `DragEnded` 后用
  `EventSystem.current.RaycastAll` 找指针下的 `CardSlotView`，再决定：
  - 手牌 → 空卡槽：放入；
  - 手牌 → 已占卡槽：找第一个空卡槽；
  - 卡槽 → 卡槽：交换位置；
  - 卡槽 → 空白：取消摆放，回到手牌。
- 每次摆放/换位后调用 `SyncSelection()`：清空逻辑选择，再按卡槽顺序
  通过 `PlayerCombatComponent.SelectFromHand` 重建选择，保证 `CommitPlay`
  的出牌顺序和卡槽顺序一致。

### 2.4 动画反馈在哪改
- 卡牌悬停放大/高亮：`CardView.OnPointerEnter / OnPointerExit`。
- 拖拽阴影：`CardView.Shadow`，拖拽时显示。
- 放入卡槽 / 回到手牌：`CardView.AnimateToSlot / AnimateBackToHand`。
- 无效摆放抖动：`CombatView.Reject` → `UiAnim.Shake`。
- 回合横幅：`CombatView.ShowBanner`。

### 2.5 怪物行动预告
`EnemyPreviewView.Refresh` 读取 `EnemyCombatComponent.Pattern` 与
`PlanIndex` / `GetRoundCards()`，把意图 `CardDef` 转成名称或效果文案。

### 2.6 结算滚动框架
`SettlementView.ShowResult` 逐条 `AddRow`，每条从透明淡入，
并自动滚动到底部；最后才启用“继续”按钮。

## 3. 后续接入点

- 标准化卡牌数据模板：替换 `CardDef` 的构造来源（现在来自 `CardCatalog`），
  卡片展示仍走 `CardView.SetCard`。
- 故事视频：在 `StoryIntroView` 挂 `VideoPlayer`，播完调用 `flow.OnStorySkip()`。
- 设置持久化：音量/全屏/文本速度目前直接写 `PlayerPrefs` 与
  `AudioListener.volume`，可换成正式存档。
- 美术资源：替换 `UiFactory` 里的占位色块，或直接把 View 改为 Prefab 引用。

## 4. 约定

- 不要在 Panel 里写 `HP -= x` 或直接改 `foodStock`；伤害/日结公式都在逻辑层。
- 新按钮先走 `AppFlowController`，如果逻辑 API 不存在，让程序加 API 再接线。
