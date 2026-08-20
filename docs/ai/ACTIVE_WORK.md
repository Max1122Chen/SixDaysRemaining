# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | **CORE-F09** 设计师反馈修复包 01（逻辑） | In Progress | Max | 逻辑项 1/4/5/6/10/11；UI 2/3/7/8 交 UI 伙伴 |
| P0 | **COMB-F11** 卡牌数值 2.0 | Review | Max | JSON 已同步；轻 Play / EditMode 后可 Done |
| — | **SHLT-F05** / **EVT-F04** / **SAVE-F02** | Review（半开放） | 他人验收 | Play：辟谷、陪玩 −12、实验、临时 HP、D4 存档、D5 |
| — | **EVT-F03**（+ **SHLT-F04**） | Review（半开放） | 他人验收 | 人设闭环 Play |
| — | **COMB-F10** 特质 | Review（半开放） | 他人验收 | 与人设同批；通过后标 Done |
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
