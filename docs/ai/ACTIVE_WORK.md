# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | UI 正式表现 / `UI_HANDOFF.md` | Handoff | UI 协作者 | 按交接文档替换 Demo 表现；保留 AppFlow / 业务 API |
| P1 | `CORE-F03` / `feat/playable-loop` | Review | Max | Demo 已提交；可与 `feat/combat` 一并考虑 merge `main` |
| P2 | `SHLT-F02` 幸存者行为/交互 | Discuss | Max | 设计讨论中；未开编码 |
| — | `EVT-F01` | Deferred | Max | 突发事件；设计明朗后再启动 |
| — | 黑化卡 | Deferred | Max | 设计未明朗，暂缓 |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. 拉 `feat/*` → 实现 → Edit Mode 测试绿 → **prepare commit 审阅** → commit
3. feat 验收通过 → merge `main` → 更新 `PROGRESS_LOG`
