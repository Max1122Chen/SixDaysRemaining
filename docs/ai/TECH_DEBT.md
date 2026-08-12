# 技术债务（Tech Debt）

记录“有意为之”的债务。不要在没有登记的情况下隐藏临时代码。

| 债务 ID | 状态 | 范围 | 摘要 | 影响 | 负责人 | 开始日期 | 再审日期 | 退出条件 |
|---------|--------|------|---------|--------|-------|--------|--------------|----------------|
| `TD-002` | Open | `设计确认` | 战斗侧（格挡/牌库/伤害时序）已有设计师反馈；庇护所分配仍用整型存量。 | `feat/combat` 数值细节可能随卡牌迭代。 | Max | 2026-07-29 | combat 设计定稿后 | 更新 `ROADMAP.md` 第 8 节；见 `designs/designer-feedback-2026-07-29.md` |
| `TD-003` | Resolved | `事件系统` | 已由 `EVT-F01` design + 实现承接；详见 `designs/EVT-F01-game-event-subsystem.md`。 | — | Max | 2026-07-29 | 2026-08-12 | `EVT-F01` 落地 |
| `TD-004` | Open | `COMB` | Corrupted 版 **蓄力一击** 是否仅放大基础 5、组合「槽内攻牌 +1」如何叠加，策划未严格定义。 | F07 先按「基础×倍率 + 原 combo 逻辑」实现。 | Max | 2026-08-07 | 策划确认或 xlsx 补一行 | 更新 `COMB-F07` 并补单测期望值 |
| `TD-005` | Open | `Bootstrap` | `GameInstance.debugStartCorruption` 为 Play 调试入口，正式发版应默认 0 或移除。 | 误留高值会污染开局手感/直接结局。 | Max | 2026-08-07 | F07 测完或进发版前 | 删字段或加 `#if UNITY_EDITOR` / Development Build 守卫 |
| `TD-006` | Open | `COMB-F08` | StreamingAssets 在部分移动平台需 `UnityWebRequest`；首版仅 Editor/Standalone `File.ReadAllText`。F08 **硬失败**，移动包未补齐前会直接抛错。 | 进移动目标前必须补读盘。 | Max | 2026-08-07 | 目标平台确定后 | 补异步加载；失败仍 throw（不引入 fallback） |
| `TD-007` | Resolved | `CORE-F05` / `EVT-F01` | 事件队列已迁入 `GameEventSubsystem`（`Assets/Scripts/Events/`）。 | — | Max | 2026-08-11 | 2026-08-12 | Flow 仅订阅广播 / 时机钩子 |
| `TD-008` | Open | `CORE-F04` / Debug | `combat.win` 可用，但 **`combat.skip` / `combat.sweep` Play 实测无效**（开关可设但行为未达 CORE-F04 设计：`skip` 出征应不进战斗直接结算；`sweep` 应强制胜场结算）。 | 事件/流程回归、速测战斗链受阻。 | Max | 2026-08-12 | EVT-F02 实现前或同会话 | `combat.skip on` 后点出征跳过 `CombatView` 并 `OnCombatFinished`；`combat.sweep on` 后战斗结束恒为 Win（非 flee）；补 EditMode/Play 抽测 |

## 状态

- Open（未解决）
- In Progress（进行中）
- Resolved（已解决）
- Rejected（被拒绝）
