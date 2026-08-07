# COMB-F07 Corrupted 伴生牌（腐蚀 ≥40 战斗选项）

## 元信息

- **ID:** `COMB-F07`
- **类型:** `Feature`
- **状态:** `Review`（逻辑已落 `feat/combat`；UI 缺陷移交 `UI-F01`）
- **负责人:** `Max`
- **最后更新：** `2026-08-07`
- **分支：** `feat/combat`（与 F06 同分支）
- **相关：**
  - 产品：`docs/designs/六日英雄—技术演示文档2.0.pdf`
  - 前序：`COMB-F06`（`CanBlacken`、`CardTag.Attack`、`CombatResolveContext`）
  - 全局：`CORE-F02`（`GameState.corruption`）、`AppFlowController` / 结局流
  - 债务：`TD-004`（Corrupted 蓄力一击组合缩放未严格定义）

## TL;DR

当 **`GameState.corruption >= 40`** 时，手牌内可 Corrupt 的**攻击牌**会出现 **Corrupted 伴生镜像**（运行时生成，**无独立 `CardDef`**）。  
玩家在五槽中与原版 **二选一** 拖入结算；Corrupted 伤害按 **结算时实时腐蚀** 乘算 `(1 + 当前%)`，**保留小数**；每张 Corrupted 打出 **+8 腐蚀**（实时写回 `GameState`）。  
伴生 **不计入手牌上限 8**，**永不进抽牌堆**；原牌照常回库。  
**`corruption >= 100`** 时 **任意来源立即结束整局** 并进结局（结局 G 语义）。  
口语「黑化」= 本 feat 的 **Corrupted**。

---

## 已拍板（审批记录）

| # | 议题 | 决议 |
|---|------|------|
| P1 | 正式命名 | **Corrupted**（代码/设计）；「黑化」仅策划口语 |
| P2 | 数据形态 | **运行时伴生**，不单独 `CardDef` / Id |
| P3 | 出现条件 | `corruption >= 40`；**实时**出现/消失（读 `GameState`） |
| P4 | 伴生范围 | **仅当前手牌**内、满足 `CanBlacken && Attack` 的原牌实例 |
| P5 | 槽位 | 原牌与 Corrupted **二选一**，占同一逻辑槽位 |
| P6 | 打出后 | **原牌保留**流程：仍从手牌移除并 **回抽牌堆底**（与 F06 一致） |
| P7 | 抽牌堆 | Corrupted **永不**进入 draw/hand 上限计数 |
| P8 | 手牌上限 | Corrupted **不计入** 8 张上限 |
| P9 | 伤害取整 | **不取整**，保留小数 |
| P10 | 倍率时机 | **逐槽结算时动态读取**当前腐蚀；例：10→第一张 Corrupted +8→18，**第二张按 18%**（2026-08-07 设计师修订，弃快照） |
| P11 | 打出代价 | 每张 Corrupted 结算 **+8** 写入 `GameState.corruption`（实时） |
| P12 | 阈值下调 | 回合中腐蚀因缓释等 **降到 &lt;40**：**已在槽内、参与本回合结算的 Corrupted 不受影响**；手牌区伴生可隐藏 |
| P13 | 腐蚀 100 | **任意来源**写 `corruption` 后 **`>= 100`** → **立即结束整局**，跳转结局 G |
| P14 | 蓄力一击 Corrupted | **先按推荐**：仅放大 **基础伤害**；组合「槽内攻牌 +1」仍按 F06；登记 **TD-004** 待策划严格定义 |

---

## 倍率示例（P10，动态）

| 槽序 | 结算前 corrosion | 剑意 5 伤 Corrupted | 结算后 corrosion |
|------|------------------|---------------------|------------------|
| 1 | 10 | 5 × 1.10 = **5.5** | 18（+8） |
| 2 | 18 | 5 × 1.18 = **5.9** | 26（+8） |

---

## 实现接缝（已落地 / 进行中）

| 组件 | 要点 |
|------|------|
| `CardInstance` | `SourceCard` / `CorruptedCompanion` |
| `CorruptedCompanionService` | 手牌 Refresh；≥40 生成 |
| `CorruptedRules` | 阈值、+8、倍率公式 |
| `ICorruptionRunState` + `GameplayCorruptionBridge` | 战斗↔`GameState` |
| `GameplaySubsystem.ApplyCorruption` | 任意来源；100→`Ending` |
| `CombatEffectExecutor` | `ResolveAsCorrupted` 放大 attack base |
| `CombatManager` | 熔断 `RunEndedByCorruption` |
| `CombatView` | 伴生 UI、槽位互斥 |

---

## 验收清单

- [x] 审批通过
- [ ] `corruption >= 40` 手牌 Attack+CanBlacken 有 Corrupted 伴生
- [ ] 槽位二选一；Corrupted 不打进 drawPile；原牌回库
- [ ] 倍率动态；小数伤害；每张 Corrupted +8
- [ ] `corruption >= 100` 立即整局结局
- [ ] TD-004 已登记；测试绿
