# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | `COMB-F08` 战斗 JSON 合 main | In Progress | Max | rebase 冲突已解 → merge |
| P1 | 重构 / 系统性补强 | Planned | Max | F02+F08 合入后开 design / slice |
| — | 特质 `defId` 挂钩（替代名字碎片） | Discuss | Max | 与 UI 伙伴 `SurvivorTrait` 对齐 |
| — | `EVT-F01` 四套事件池 | Deferred | Max | 入住剧情/被动另 feat |
| — | Excel→JSON 导出（TECH） | Deferred | Max | F08 后可选 |
| — | `TD-004` / `TD-005` / `TD-006` | Open | Max | 见 TECH_DEBT |

## 近期已收口

| ID | 状态 | 备注 |
|----|------|------|
| `SHLT-F02` | Done | 身份 JSON + 入住/状态/死亡；已合 `main` |
| `COMB-F01`～`F07` / `UI-F01` / `CORE-F03` | Done | 已在 `main` |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. 在约定域分支实现 → Edit Mode 测试绿 → **prepare commit 审阅** → commit
3. feat 验收通过 → merge `main` → 更新 `PROGRESS_LOG`
