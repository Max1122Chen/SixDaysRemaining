# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | **SHLT-F03** Play 手测 | Review | Max | 幼童 −8 / 政治家回访 / Ending 文案 |
| P1 | **END-F01** 结局钩子 | Planned | Max | design：战败 → Ending.E |
| P2 | **COMB-F10** 特质卡系统 | Planned | Max | 人设基础稳定后 design |
| P2 | **META-F01** 结局回顾 | Planned | Max | run summary UI；不绑 mid-run 存档 |
| P2 | **SAVE-F01** 存档 | Planned | Max | endingId + Tag + Passive 稳定后再做 |
| — | Excel→JSON 导出（TECH） | Deferred | Max | 可选 |
| — | `TD-004` / `TD-005` / `TD-006` | Open | Max | 见 TECH_DEBT |

## 近期已收口

| ID | 状态 | 备注 |
|----|------|------|
| `SHLT-F03` | Done | Passive + endingId + 政治家拒收/回访；待 Play |
| `CORE-F06` / `CORE-F07` | Done | Tag 基础设施 + storyFlags 迁移 |
| `feat/events` → `main` | Done | EVT-F01 / F02 |
| `CORE-F04` / `CORE-F05` / `COMB-F09` | Done | — |
| `SHLT-F02` / `COMB-F01`～`F08` / `UI-F01` / `CORE-F03` | Done | — |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. **优先在 `main` 上按队列 slice 实现**；大域分支仅在有明确并行需求时使用
3. 实现 → Edit Mode 测试绿 → commit
4. 完成批次 → 更新 `PROGRESS_LOG`
