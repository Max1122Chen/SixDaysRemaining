# AUDIO-F01 场景 BGM 接入

## Meta
- **ID:** `AUDIO-F01`
- **类型:** `Feature`
- **状态:** `In Progress`
- **负责人:** `Max`
- **最后更新：** `2026-08-21`
- **相关：** `[Feature Registry](../FEATURE_REGISTRY.md)`、`CORE-F05`（PresentationManager 切屏）、`SettingsView`（全局音量）

## TL;DR（简述）

设计师交付 2 首 mp3 背景音，文件名即场景映射。本 feat 接入轻量 BGM 播放器：按界面切换曲目，同曲不重启，尊重设置里的全局音量。不做 SFX / 混音器 / 按结局分曲。

**实现备注：** 资源落在 `Assets/Resources/Audio/Bgm/`（`destiny` / `dark_altar`），便于代码 `Resources.Load`，无需场景序列化引用。

## 设计师素材（文件名 = 需求）

| 曲目 | 源文件（Downloads） | 使用界面 |
|------|---------------------|----------|
| 《宿命》 | `《宿命》—开始、战斗、结局界面背景音.mp3` | 开始界面、战斗界面、结局界面 |
| 《黑暗祭祀圣坛》 | `《黑暗祭祀圣坛》—背景故事、庇护所界面背景音.mp3` | 背景故事（开场 intro）、庇护所界面 |

入库路径：

```text
SixDaysRemaining/Assets/Resources/Audio/Bgm/
  destiny.mp3          # 《宿命》
  dark_altar.mp3       # 《黑暗祭祀圣坛》
```

（资源名用 ASCII；中文曲名保留在 `BgmService` 注释。）

## Scope
- **范围 In：**
  - 将 2 首 mp3 纳入 `Assets/Audio/Bgm/`
  - `BgmService`（或 `AudioDirector`）：按 `BgmId` 播放 / 切歌 / 同曲续播
  - 在 `PresentationManager` 切屏时设置目标 BGM：
    - Start / Combat / Ending → `destiny`
    - StoryIntro / Shelter → `dark_altar`
  - 尊重现有 `AudioListener.volume`（设置页已有）
  - 切屏短淡入淡出（约 0.3–0.6s，可调常量）
- **范围 Out：**
  - 卡牌 / UI 点击音效、战斗命中音
  - 按结局分支换曲（A–I 共用 Ending 的《宿命》）
  - AudioMixer 多总线、3D 音效
  - 独立「音乐开/关」开关（首版靠总音量；后续可加）
  - 事件弹窗 / 日结 overlay 单独 BGM（跟随底层主屏）

## 现状、目标与差距

- 当前行为：无 BGM；设置仅调 `AudioListener.volume`
- 目标行为：主流程界面有对应循环背景音；同曲界面间切换不从头播
- 差距：缺资源入库 + 播放服务 + 切屏挂钩

## Design

### Option A (recommended) — Presentation 挂钩 + 单通道 BGM

```text
PresentationManager.ShowXxxScreen()
  → BgmService.SetTarget(BgmId)
      同曲且在播 → no-op
      换曲 → 淡出当前 → 淡入新曲（loop）
Settings 音量 → AudioListener.volume（已有，BGM 自然跟随）
```

API 草案：

```csharp
public enum BgmId
{
    None = 0,
    Destiny = 1,    // 《宿命》— Start / Combat / Ending
    DarkAltar = 2   // 《黑暗祭祀圣坛》— Story / Shelter
}

public sealed class BgmService : MonoBehaviour
{
    public void SetTarget(BgmId id);
    public void Stop(float fadeSeconds = 0.4f);
}
```

挂载：场景 bootstrap（与 `PresentationManager` / `GameInstance` 同场景）一个 `AudioSource`（loop、playOnAwake=false）。

映射表（硬编码即可；仅 2 曲无需 JSON）：

| 屏幕 | BgmId |
|------|-------|
| Start | Destiny |
| StoryIntro | DarkAltar |
| Shelter | DarkAltar |
| Combat | Destiny |
| Ending | Destiny |
| MetaReview / Settings overlay | 不改 BGM（保持底层） |

### Option B

每屏各自挂 `AudioSource`——拒绝：切屏重复、难统一淡化与同曲续播。

### Option C

`StreamingAssets` + 运行时 `WWW/UnityWebRequest` 加载——首版不必要；mp3 可直接作 `AudioClip` 进 Resources/`Assets/Audio` 引用。

## 实现注意点

- 影响模块：`PresentationManager`、新建 `App` 或 `UI` 下 `BgmService`、场景 bootstrap
- 资源：从 Downloads 复制并改名；`.meta` 由 Unity 生成
- 平台：Unity 对 mp3 作 AudioClip 导入通常可用；若某目标平台有问题再转 ogg（记 debt）
- 不改玩法逻辑；AppFlow 不必感知音频

## 验证

- Play：
  - 主菜单播《宿命》
  - 新游戏 → 背景故事切《黑暗祭祀圣坛》
  - 进庇护所续播圣坛（不重启）
  - 出征战斗切《宿命》
  - 结局仍《宿命》
  - 设置拖音量，BGM 响度跟随
- EditMode：可选极轻量（映射表单元测试）；非必须

## 验收清单

- [x] 范围已实现
- [ ] 验证通过
- [x] 已更新进度日志
- [x] Feature 注册表状态已同步

## 拍板结论（2026-08-21）

1. 「开始」= 主菜单 StartScreen — 是
2. overlay 不换 BGM — 是
3. 淡化 ~0.5s — 是
4. 已进入实现
