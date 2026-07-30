# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | `CORE-F03` / `feat/playable-loop` | In Progress | Max | 代码已落；本机 Play SampleScene 验收 |
| P1 | `COMB` / `feat/combat` | Done（分支内） | Max | 可随后 merge → `main` |
| — | `SHLT-F01` | Done | Max | 已合并 `main` |
| — | `CORE-F02` | Done | Max | 已合并 `main` |
| — | `EVT-F01` | Deferred | Max | 事件系统设计明朗后再启动 |
| — | 黑化卡 | Deferred | Max | 设计未明朗，暂缓 |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. 拉 `feat/*` → 实现 → Edit Mode 测试绿 → **prepare commit 审阅** → commit
3. feat 验收通过 → merge `main` → 更新 `PROGRESS_LOG`
