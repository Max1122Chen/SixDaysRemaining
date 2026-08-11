# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | `SHLT-F02` 幸存者身份 + 入住/状态/死亡 | Review | Max | Edit Mode 确认后 prepare commit / merge |
| P1 | `feat/combat`（含 F08）合 main / push | Review | Max | 仍在 `feat/combat`；与 shelter 注意合入顺序 |
| — | `EVT-F01` 四套事件池 | Deferred | Max | 入住剧情/被动归属此处或后续 SHLT 行为 feat |
| — | 特质 / 特质牌挂钩 | Deferred | Max | 定义清晰后再开 feat |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. 在约定域分支（本 feat：`feat/shelter`）实现 → Edit Mode 测试绿 → **prepare commit 审阅** → commit
3. feat 验收通过 → merge `main` → 更新 `PROGRESS_LOG`
