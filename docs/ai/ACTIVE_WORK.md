# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | `COMB-F06` 统一卡牌 + 内容种子 | In Progress | Max | 在 `feat/combat` 实现中；接队友 UI |
| P1 | UI ↔ F06（意图按 CardDef 展示） | Review | UI / Max | 随 F06 改意图绑定 |
| P2 | `SHLT-F02` 幸存者特质 / 人物模板 | Discuss | Max | `人物模板2.0.pdf` 另议 |
| — | `COMB-F08` JSON 数据驱动 | Deferred | Max | F06 后再做；本轮不实现 |
| — | `COMB-F07` 黑化 | Deferred | Max | 腐蚀&gt;40 |
| — | `EVT-F01` 四套事件池 | Deferred | Max | 设计明朗后再启动 |
| — | `CORE-F03` 文档收口 | Review | Max | 与现行 UI 对齐后可标 Done |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. 拉 `feat/*` → 实现 → Edit Mode 测试绿 → **prepare commit 审阅** → commit
3. feat 验收通过 → merge `main` → 更新 `PROGRESS_LOG`
