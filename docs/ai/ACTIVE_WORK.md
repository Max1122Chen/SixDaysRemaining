# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | **EVT-F03** 人物/事件 3.0 | Review | Max | Play 验收人设闭环；S5 深度可后 |
| P1 | **SHLT-F04**（并入 F03） | Review | Max | cap5/死亡+8 已落地；分配例外属 S5 |
| P2 | **COMB-F10** 特质 | Review | Max | doctor 已对齐；Play 后人设闭环标 Done |
| — | 卡牌数值 2.0 | Planned | Max | 另批；不挡 F03 |
| — | Excel→JSON 导出（TECH） | Deferred | Max | 可选 |
| — | `TD-004` / `TD-005` / `TD-006` | Open | Max | 见 TECH_DEBT |

## 近期已收口

| ID | 状态 | 备注 |
|----|------|------|
| `CORE-F08` / `META-F01` / `SAVE-F01` | Done | EditMode + Play |
| `END-F01` | Done | 政治家战败 → Ending.E |
| `SHLT-F03` | Done | Passive + endingId |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. **优先在 `main` 上按队列 slice 实现**；大域分支仅在有明确并行需求时使用
3. 实现 → Edit Mode 测试绿 → commit
4. 完成批次 → 更新 `PROGRESS_LOG`
