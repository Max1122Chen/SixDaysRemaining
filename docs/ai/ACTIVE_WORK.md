# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | **CORE-F07** GameplayTag 业务迁移 | Planned | Max | storyFlags → Tag；`requiredTags`；下一 commit |
| P1 | **TD-008** Debug combat skip/sweep | Done | Max | 已修复；Play 抽测可选 |
| P1 | **EVT-F02** 政治家 D3 | Review | Max | Play 复验敲门线；Out 项登记 SHLT-F03 / END-F01 |
| P1 | **COMB-F10** 特质卡系统 | Planned | Max | 先 design + defId 挂钩 |
| P2 | **META-F01** 结局回顾 | Planned | Max | run summary UI；不绑 mid-run 存档 |
| P2 | **SAVE-F01** 存档 | Planned | Max | CORE-F07 + 特质稳定后再做 |
| — | Excel→JSON 导出（TECH） | Deferred | Max | 可选 |
| — | `TD-004` / `TD-005` / `TD-006` | Open | Max | 见 TECH_DEBT |

## 近期已收口

| ID | 状态 | 备注 |
|----|------|------|
| `feat/events` → `main` | Done | fast-forward `ebcf6b9` |
| `EVT-F01` / `EVT-F02` | Done | 幼童线 Play 通过 |
| `CORE-F06` | Done | GameplayTag 基础设施 |
| `CORE-F04` / `CORE-F05` / `COMB-F09` | Done | — |
| `SHLT-F02` / `COMB-F01`～`F08` / `UI-F01` / `CORE-F03` | Done | — |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. **优先在 `main` 上按队列 slice 实现**；大域分支仅在有明确并行需求时使用
3. 实现 → Edit Mode 测试绿 → commit
4. 完成批次 → 更新 `PROGRESS_LOG`
