# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | `ROADMAP.md` + `CORE-F02` design | Review | Max | 审阅后执行 commit；再批准开 `feat/gameplay-framework` |
| — | `SHLT-F01` / `COMB-F01` | Blocked | Max | 依序在前序 feat 合并后再写 design |
| — | `EVT-F01` | Deferred | Max | 事件系统设计明朗后再启动 |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. 拉 `feat/*` → 实现 → Edit Mode 测试绿 → **prepare commit 审阅** → commit
3. feat 验收通过 → merge `main` → 更新 `PROGRESS_LOG`
