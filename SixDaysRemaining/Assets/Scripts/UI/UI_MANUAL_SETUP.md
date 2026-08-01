# 手动搭建 UI 逐步指南

## 为什么按钮点不动

之前的原型是代码在运行时生成 UI。`SettingsOverlay` / `CreditsOverlay` 等全屏遮罩
生成后没有先隐藏，一直叠在开始界面上，把点击全部吃掉了。已修复：

- 三个 Overlay 创建后默认 `SetActive(false)`；
- `AppFlowController.SwitchScreen` 切换画面时也会强制关掉设置/制作组遮罩。

## 正式做法：手动搭 UI + 脚本挂引用

游戏 UI 的正常做法是：

1. 在场景里手动创建 `Canvas` 和各个 `Panel`；
2. 每个 Panel 挂一个 View 脚本（如 `StartScreenView`）；
3. 把按钮、文字、卡槽在 Inspector 拖给脚本的 `[SerializeField]` 字段；
4. 场景里放一个 `UiSceneBootstrap`，把各 View 拖进去，运行时会统一接线。

现在所有 View 都已支持“手动接线”：

- `StartScreenView` / `ShelterView` / `EndingView` / `CombatView` 等都有
  `[SerializeField]` 按钮/文本字段；
- 每个 View 都有 `Wire(AppFlowController flow)`，重复调用安全；
- `UiSceneBootstrap` 会补齐 EventSystem、GameInstance 和战斗占位物体。

## 建议：MainMenu 要不要单独场景？

现阶段建议**不拆场景**，把 MainMenu 做成同一个场景里的第一个面板：

- 流程切换只是 `SetActive`，没有场景加载黑屏和重复初始化；
- GameInstance 是单例，逻辑状态好维护；
- 以后要拆也很简单：新建 `MainMenu.unity` 和 `Game.unity`，
  各自放一个 `UiSceneBootstrap`，GameInstance 保持 `DontDestroyOnLoad` 即可。

## 一步步搭建

### Step 1：场景基础
1. 打开 `SampleScene.unity`。
2. 创建 `Canvas`：GameObject > UI > Canvas，Render Mode 选
   `Screen Space - Overlay`，CanvasScaler 设为 `Scale With Screen Size`，
   参考分辨率 `1920x1080`。
3. 确认场景里有 `EventSystem`（没有就 GameObject > UI > EventSystem）。
4. 把 `UIRoot.cs` 暂时移出项目（或删除），避免自动生成 UI 和手动 UI 重复。

### Step 2：MainMenu 面板
1. 在 Canvas 下创建空物体 `MainMenuPanel`，加 `Image` 做背景。
2. 加 4 个按钮：`开始游戏`、`新游戏`、`设置`、`退出`。
3. 给 `MainMenuPanel` 挂 `StartScreenView`，把 4 个按钮拖到对应字段。
4. 初始时该面板保持 active；其余面板先 `SetActive(false)`。

### Step 3：设置 / 制作组遮罩
1. 创建 `SettingsOverlay`（全屏半透明 Image），挂 `SettingsView`，
   拖入音量 Slider、全屏 Toggle、文本速度 Slider、`制作组`/`返回` 按钮。
2. 创建 `CreditsOverlay`，挂 `CreditsView`，拖入返回按钮。
3. 两个 Overlay 初始都 `SetActive(false)`。

### Step 4：故事 / 庇护所
1. `StoryIntroView`：挂到故事画面，拖入文字与`跳过`按钮。
2. `ShelterView`：挂到庇护所画面，拖入状态文字、幸存者文字、
   `出发`/`设置`/`返回主菜单` 按钮。

### Step 5：战斗
1. 创建 `CombatScreen`，挂 `CombatView`。
2. 拖入：我方状态文字、卡槽数量文字、`结算`/`清空`/`撤退`/`设置` 按钮、
   `EnemyPreview` 面板、`CardLayer`（放卡牌的透明层）。
3. 在 CardLayer 下创建 5 个空物体，各挂 `CardSlotView` 并做卡槽背景，
   按 0~4 顺序拖进 `CombatView.slots` 数组。
4. `EnemyPreviewView` 挂到敌人预告面板，拖入三条文字。

### Step 6：结算 / 结局
1. `SettlementView`：挂到结算遮罩，拖入 ScrollRect、Content、`继续`按钮。
2. `EndingView`：挂到结局画面，拖入结局文字与`返回主菜单`按钮。

### Step 7：接线
1. 在场景建一个空物体 `UI_Bootstrap`，挂 `UiSceneBootstrap`。
2. 把上面所有 View 拖进对应字段。
3. Play，应直接进入 MainMenu，所有按钮可点击。

## 常见检查

- 按钮点不到：先看有没有全屏 Image / 透明 Panel 叠在最上层；
  把它 `raycastTarget` 关掉或放到下层。例如 `EnemyPreviewPanel` 铺满全屏时，
  `CombatView.Wire` 会自动关掉它的点击拦截。
- 某面板看不到：检查它是否 active；`AppFlowController` 每次只显示一个画面。
- 战斗没反应：确认 `CombatView` 的 5 个卡槽已按顺序拖入，且
  `UiSceneBootstrap` 能创建 Player/Enemy 占位。

## 设置与制作组面板：具体创建步骤

### 1. SettingsOverlay

1. 在 Hierarchy 里右键 `Canvas` → UI → Panel，命名为 `SettingsOverlay`。
2. 选中 `SettingsOverlay`，RectTransform 锚点选九宫格中间“拉伸”（Alt+Shift 点
    Preset 的 stretch），四个偏移填 0，让它铺满 Canvas。
3. Image 颜色填 `R=0 G=0 B=0 A=140`（半透明黑，约 0.55）。
4. Add Component → 搜 `SettingsView`，挂上去。先保持物体 active，全部搭完再
    取消勾选隐藏。
5. 右键 `SettingsOverlay` → UI → Panel，命名 `Window`：
   - 锚点选中心（Center），Position (0,0)，Size `680 x 520`；
   - Image 颜色 `R=26 G=31 B=38 A=255`。
6. 在 `Window` 下创建标题：右键 → UI → Text - TextMeshPro，命名 `Txt_Title`，
   内容 `设置`，字号 36，居中，位置 (0, 220)。

### 2. 音量 Slider

1. 右键 `Window` → UI → Slider，命名 `Slider_Volume`。
2. 位置 (80, 130)，大小 `360 x 24`。
3. Inspector 里设置：
   - Min Value `0`
   - Max Value `1`
   - Whole Numbers 不勾
   - Value `0.8`
4. 默认生成的 Background / Fill Area / Handle Slide Area 保持不动即可；
   如果自己手搭过，要确保 Slider 的 `Fill Rect` 和 `Handle Rect` 已拖好。
5. 加标签：右键 `Window` → UI → Text - TextMeshPro，命名 `Txt_Volume`，
   内容 `音量`，位置 (-220, 130)。

### 3. 全屏 Toggle

1. 右键 `Window` → UI → Toggle，命名 `Toggle_Fullscreen`。
2. 位置 (80, 50)，大小 `70 x 34`。
3. 默认结构（Background + Checkmark）可用；勾选状态由 Toggle 的
   `Is On` 控制，运行时会自动同步 `Screen.fullScreen`。
4. 加标签 `Txt_Fullscreen`，内容 `全屏 / 窗口`，位置 (-220, 50)。

### 4. 文本速度 Slider（选做）

1. 复制 `Slider_Volume` 改名 `Slider_TextSpeed`，位置 (80, -30)。
2. Min Value `0.5`，Max Value `2`，Value `1`。
3. 加标签 `Txt_TextSpeed`，内容 `文本速度（选做）`，位置 (-220, -30)。

### 5. 制作组 / 返回按钮

1. 右键 `Window` → UI → Button - TextMeshPro，命名 `Btn_Credits`，
   文字 `制作组`，位置 (0, -130)，大小 `180 x 48`。
2. 同样创建 `Btn_Back`，文字 `返回`，位置 (0, -210)。

### 6. 拖引用

选中 `SettingsOverlay`，把以下物体拖进 `SettingsView` 的字段：

| 字段 | 拖入 |
| --- | --- |
| `volumeSlider` | `Slider_Volume` |
| `fullscreenToggle` | `Toggle_Fullscreen` |
| `textSpeedSlider` | `Slider_TextSpeed` |
| `creditsButton` | `Btn_Credits` |
| `backButton` | `Btn_Back` |

最后取消勾选 `SettingsOverlay` 隐藏。

### 7. CreditsOverlay

1. 右键 `Canvas` → UI → Panel，命名 `CreditsOverlay`，拉伸铺满，
   Image 颜色 `0, 0, 0, 180`。
2. Add Component → `CreditsView`。
3. 创建 `Txt_Title`：`制作组`，字号 44，位置 (0, 250)。
4. 创建 `Txt_Credits`：多行文案，字号 24，位置 (0, 0)。
5. 创建按钮 `Btn_Back`：文字 `返回`，位置 (0, -360)。
6. 选中 `CreditsOverlay`，把 `Btn_Back` 拖进 `CreditsView.backButton`。
7. 取消勾选隐藏。

### 8. 中文字体

手动建的 TMP 文字默认用 `LiberationSans SDF`，没有中文字形会显示成方块。
`UiSceneBootstrap` 会优先使用场景里手动配好的字体（例如 `typeface SDF`），
并把它应用到所有 TMP 文字和代码生成的 UI；没有手动字体时才退回动态中文字体。
