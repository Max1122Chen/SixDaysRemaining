# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | **META-F01** 结局回顾 | Planned | Max | 在 `main` 讨论 scope / design |
| P1 | **COMB-F10** 特质卡 | Review | Max | 半开放待验收（人设齐后完整 Play） |
| P2 | **SAVE-F01** 存档 | Planned | Max | endingId + Tag + Passive 稳定后 |
| — | Excel→JSON 导出（TECH） | Deferred | Max | 可选 |
| — | `TD-004` / `TD-005` / `TD-006` | Open | Max | 见 TECH_DEBT |

## 近期已收口

| ID | 状态 | 备注 |
|----|------|------|
| `END-F01` | Done | 政治家战败 → Ending.E |
| `SHLT-F03` | Done | Passive + endingId |
| `CORE-F06` / `CORE-F07` | Done | Tag 单轨 |
| `EVT-F01` / `EVT-F02` | Done | — |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. **优先在 `main` 上按队列 slice 实现**；大域分支仅在有明确并行需求时使用
3. 实现 → Edit Mode 测试绿 → commit
4. 完成批次 → 更新 `PROGRESS_LOG`
