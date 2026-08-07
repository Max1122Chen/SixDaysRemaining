# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | `UI-F01` 战斗卡牌交互修复 | Review | Max / UI | 已合 main / push |
| P1 | `COMB-F07` / `COMB-F06` 合入 main | Review | Max | `feat/combat` 本轮提交后 merge |
| P2 | `SHLT-F02` 幸存者特质 / 人物模板 | Discuss | Max | `人物模板2.0.pdf` |
| — | `COMB-F08` JSON 数据驱动 | Deferred | Max | F06 接口就绪后 |
| — | `EVT-F01` 四套事件池 | Deferred | Max | 设计明朗后再启动 |
| — | `CORE-F03` 文档收口 | Review | Max | 与现行 UI 对齐 |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. 拉 `feat/*` → 实现 → Edit Mode 测试绿 → **prepare commit 审阅** → commit
3. feat 验收通过 → merge `main` → 更新 `PROGRESS_LOG`
