# END-F02 结局判定数据驱动

## Meta
- **ID:** `END-F02`
- **类型:** `Feature`
- **状态:** `In Progress`
- **负责人:** `Max`
- **最后更新：** `2026-08-20`
- **依赖：** `END-F01`（`ForceEnding` / `EndingEvaluator` 钩子）、`CORE-F08`（meta 解锁）
- **相关：** `[Feature Registry](../FEATURE_REGISTRY.md)`、`docs/designs/结局设计.docx`

## TL;DR（简述）

按《结局设计》落地 A–I 结局：用与事件系统同构的 JSON 目录 + 条件过滤 + 优先级匹配，替换硬编码 `Ending.MaxDay` 占位与零散文案。运行时仍走统一 `ForceEnding(endingId)`。

## Scope
- **范围 In：**
  - `StreamingAssets/Endings/endings.json`（id / title / body / trigger / priority / 条件字段）
  - `EndingContent` + JsonLoader（缺文件硬失败）
  - 扩展 `EndingEvaluator`：CombatLose / PopulationZero / RunComplete 三类触发
  - `EndingView` 从目录取 title+body；扩展 `EndingIds` A–I
  - 第 6 天推进终局时按腐蚀/人口匹配 A–D / H / I，不再一律 `MaxDay`
  - EditMode：条件过滤、优先级、文案 lookup
- **范围 Out：**
  - 结局 CG / 多页叙事 UI
  - Excel 导出工具
  - 改写 G 的腐蚀熔断路径（仍即时 `ForceEnding(G)`，目录里保留定义供文案）

## 现状、目标与差距

- 当前行为：仅有 `Ending.G` / `E` / `MaxDay` / `Debug`；E 硬编码；六日结束一律 MaxDay；文案 switch 硬编码
- 目标行为：A–I 均可触发；条件与文案在 JSON；判定类似 `EventRequirements`
- 差距：缺 endings 目录与 RunComplete 评估入口

## Design

### Option A (recommended) — 事件同构

与 `events.json` 对齐：

| 字段 | 含义 |
|------|------|
| `id` | `Ending.A` … `Ending.I`（及保留 `Ending.MaxDay` / `Ending.Debug` 兜底） |
| `title` / `body` | 展示文案 |
| `trigger` | `CombatLose` \| `PopulationZero` \| `RunComplete` \| `CorruptionFuse` |
| `priority` | 同 trigger 内降序取第一条匹配 |
| `enabled` | 可关 |
| `corruptionMin` / `corruptionMax` | 可选 |
| `populationMin` / `populationMax` | 可选（存活人数，不含 Dead/Left） |
| `requiredSurvivorIds` | 可选（如 E 需 `politician`） |

触发接入：

```text
腐蚀 ≥ 100          → CorruptionFuse → Ending.G（仍由 ApplyCorruption 即时触发；文案走目录）
战斗 Lose           → CombatLose     → Ending.E（政治家在场）
人口变为 0          → PopulationZero → Ending.F
第 6 日后推进 / 天数终局 → RunComplete → A/B/C/D/H/I（按 priority）
无匹配              → Ending.MaxDay 兜底
```

优先级建议（写入 JSON）：

| id | trigger | priority | 条件摘要 |
|----|---------|----------|----------|
| G | CorruptionFuse | 1000 | corruptionMin 100 |
| E | CombatLose | 900 | requiredSurvivorIds politician |
| F | PopulationZero | 800 | populationMax 0 |
| I | RunComplete | 70 | corruptionMin 81 |
| H | RunComplete | 60 | populationMin/Max 2 |
| C | RunComplete | 50 | corruptionMin 41, populationMax 1 |
| D | RunComplete | 50 | corruptionMin 41, populationMin 3 |
| A | RunComplete | 40 | corruptionMax 39, populationMin 3 |
| B | RunComplete | 40 | corruptionMax 39, populationMax 1 |

说明：设计稿用 ＜40 / ＞40，故 **=40** 不进 A–D；I 用 ＞80 → `corruptionMin: 81`。H 填满人口=2 缺口。

### Option B

把结局全塞进 `events.json` 的 ForceEnding——拒绝：终局不是日事件队列，语义与预算不同。

## 实现注意点

- 模块：`Gameplay/Ending*` 或 `App/Ending*` 内容加载；`EndingEvaluator` 扩查询；`AppFlowController` 日推进；`ShelterManager` 人口归零钩子；`EndingView` / `MetaProfileService.KnownEndingIds`
- 删除：`EndingView` 内 A–I 硬编码文案；RunComplete 路径上的盲目 `MaxDay`
- 兼容：旧存档只有 endingId 字符串，无 schema 迁移

## 验证

- EditMode：各 trigger 匹配 / 优先级 / JSON 坏数据硬失败 / 文案
- Play：六日结束按腐蚀+人口进 A/B/C/D/H/I；战败+政治家 E；腐蚀 100 G；全灭 F

## 验收清单

- [x] 范围已实现
- [ ] 验证通过
- [x] 已更新进度日志
- [x] Feature 注册表状态已同步
