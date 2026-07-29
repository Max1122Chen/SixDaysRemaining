# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | `SHLT-F01` / `feat/shelter` | Review | Max | 已提交；待 merge `main` 后启动 COMB-F01 |
| — | `CORE-F02` | Done | Max | 已合并 `main` |
| — | `COMB-F01` | Blocked | Max | 依赖 `SHLT-F01` 合并 |
| — | `EVT-F01` | Deferred | Max | 事件系统设计明朗后再启动 |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. 拉 `feat/*` → 实现 → Edit Mode 测试绿 → **prepare commit 审阅** → commit
3. feat 验收通过 → merge `main` → 更新 `PROGRESS_LOG`
