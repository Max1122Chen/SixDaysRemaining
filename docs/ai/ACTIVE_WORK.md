# 当前工作（Active Work）

## 当前队列

| 优先级 | 关联 | 状态 | 负责人 | 下一步行动 |
|--------|------|------|--------|-------------|
| P0 | 重构 / 系统性补强 | Planned | Max | F02+F08 已合 main；开 design / slice |
| — | 特质 `defId` 挂钩（替代名字碎片） | Discuss | Max | 与 UI 伙伴 `SurvivorTrait` 对齐 |
| — | `EVT-F01` 四套事件池 | Deferred | Max | 入住剧情/被动另 feat |
| — | Excel→JSON 导出（TECH） | Deferred | Max | 可选 |
| — | `TD-004` / `TD-005` / `TD-006` | Open | Max | 见 TECH_DEBT |

## 近期已收口

| ID | 状态 | 备注 |
|----|------|------|
| `SHLT-F02` | Done | 身份 JSON + 入住/状态/死亡 |
| `COMB-F01`～`F08` | Done | 含 StreamingAssets 战斗 JSON |
| `UI-F01` / `CORE-F03` | Done | 含伙伴 UI 提交 |

## Feat 开发纪律（摘要）

1. 登记 Feature ID → 写 design → **审阅**
2. **优先在已有大域分支上实现**（如 `feat/combat` / `feat/shelter`）；未经明确要求不新开细碎分支
3. 实现 → Edit Mode 测试绿 → **prepare commit 审阅** → commit
4. feat 验收通过 → merge `main` → 更新 `PROGRESS_LOG`
