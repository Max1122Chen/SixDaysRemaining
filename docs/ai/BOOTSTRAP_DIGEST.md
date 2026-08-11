# 启动摘要（Bootstrap Digest）

最后更新：2026-08-07  
目的：在 2 分钟内恢复协作上下文。

## 新会话的读取顺序

1. `PROJECT_CONTEXT.md`
2. **若做 UI：** `UI_HANDOFF.md`（必读）
3. `ROADMAP.md`（技术实现主计划）
4. `PROGRESS_LOG.md`（只看最近的条目）
5. `ACTIVE_WORK.md`
6. `FEATURE_REGISTRY.md`（In Progress / Planned）
7. `TECH_DEBT.md`（Open）
8. 产品设计源：`docs/designs/六日英雄—技术演示文档.pdf`

## 当前快照（2026-08-07）

- 分支：`feat/shelter` 上实现 `SHLT-F02`（身份 JSON + 入住/状态/死亡）；待 Edit Mode 确认后 commit。
- 可玩主线在 `main`；战斗 JSON（COMB-F08）仍可能在 `feat/combat` 待合。
- 下一：SHLT-F02 合入；特质/入住被动与 `EVT-F01` 仍延后。

## 不可协商的协作规则

- 只从可信计划源规划；不要从旧路线图快照里推断 backlog。
- 新 Feature / 重构：先注册 ID，再定义设计/计划，然后才是实现。
- `Draft` 状态的设计不授权大规模编码。
- 完成一个有意义的批次：更新文档，并在开始不相关的下一件事前提出“prepare commit”。
- “prepare commit”只代表草案与复核；只有在你明确指令后才执行提交。

## ID 规则

- Feature：`<DOMAIN>-F<nn>`
- Slice：`<FeatureID>-S<nn>`
- Bug：`BUG-<DOMAIN>-<nnn>`
- ADR：`ADR-<yyyyMMdd>-<nn>`

## 域代码（Domain codes）

- `CORE` - 核心循环、游戏状态、阶段编排、可玩接入
- `SHLT` - 庇护所、幸存者、饱食度
- `COMB` - 卡牌战斗
- `EVT` - 突发事件（延后）
- `UI` - 用户界面与反馈
- `LVL` - 场景布局、关卡脚本、环境
- `TECH` - 启动、工具、流水线、基础设施
- `AUDIO` - 音频系统与音频线索

## 验证基线

- 验证命令：用 Unity `2022.3.62f3c1` 打开 `SixDaysRemaining/`，并确认 Console 没有编译错误。
- 冒烟：`SampleScene` Play 跑完至少一天（含凯旋后次日可再出门）。

## 交接触发点（Handoff trigger cues）

当你提示需要交接/会话切换时：
- 若任务是多步骤或尚未完成，写一条会话说明
- 追加一条 progress log
- 把未完成的 slice 标为 Blocked/Deferred，并给出原因与解除阻塞条件
- UI 交接以 `UI_HANDOFF.md` 为准
