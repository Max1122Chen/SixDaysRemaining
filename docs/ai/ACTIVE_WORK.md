# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | **COMB-F10** 特质卡 | Review | Max | 半开放：人设/入住齐后完整 Play |
| — | Excel→JSON 导出（TECH） | Deferred | Max | 可选 |
| — | `TD-004` / `TD-005` / `TD-006` | Open | Max | 见 TECH_DEBT |

## 近期已收口

| ID | 状态 | 备注 |
|----|------|------|
| `CORE-F08` / `META-F01` / `SAVE-F01` | Done | EditMode 全绿；Play 基本正常 |
| `COMB-F10` | Review | 半开放待人设 |
| `END-F01` | Done | 政治家战败 → Ending.E |
| `SHLT-F03` | Done | Passive + endingId |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. **优先在 `main` 上按队列 slice 实现**；大域分支仅在有明确并行需求时使用
3. 实现 → Edit Mode 测试绿 → commit
4. 完成批次 → 更新 `PROGRESS_LOG`
