# COMB-F02 战斗 AttributeSet + CombatComponent（伤害/格挡管线）

## 元信息

- **ID:** `COMB-F02`
- **类型:** `Feature`
- **状态:** `Planned`（已批准；**COMB 设计链完成前暂缓实现**）
- **负责人:** `Max`
- **最后更新：** `2026-07-30`（批准；整链设计完再动工）
- **分支：** `feat/combat-pipeline`（实现阶段：在 `COMB-F01` 合并后再拉）
- **相关：** `COMB-F01`、`REFERENCES.md`、设计师反馈、`COMB-feat-chain.md`

## TL;DR

在 `CombatComponentBase` 之上挂载 `CombatAttributeSet`，派生 `CombatComponent`，提供伤害/格挡业务 API。  
**造成方** `SetDamage`（含 `DamageMultiplier`）→ `DealDamage` **直接调用**对方 `TakeDamage`；格挡增减由 Component 提供原语，**何时清 Block（如回合结束）由 `CombatManager` 决定并调用 `SetBlock`**，Component 不解释「回合结束」。  
本 feat **不含**打牌、Session 编排、Manager 回合机。

---

## 范围

### In

- `CombatAttributeSet`：`MaxHP`、`HP`、`Block`、`Damage`、`DamageToTake`、`DamageMultiplier`
- `CombatComponent : CombatComponentBase`：注册上述 Set，实现下表 API
- HP / Block clamp；伤害结算写入 HP 时 **向下取整（Floor）**
- Edit Mode 测试覆盖管线与 API

### Out

- 手牌 / 牌库 / 出牌
- `CombatSession` / `CombatManager` 回合状态机（仅约定：**Manager 将来调用 `SetBlock(0)`**）
- 罐头、腐蚀回写、大门 UI、敌人 Intent
- Modifier / GE / Tag；`DamageMultiplier` 仅作可读写占位并在 `SetDamage` 中乘算

---

## 已拍板契约

| # | 决策 |
|---|------|
| 1 | `DealDamage(target)` **直接**调用 `target.TakeDamage()` |
| 2 | `DamageMultiplier` 在 **`SetDamage`** 时参与计算（写入 `Damage`） |
| 3 | **清 Block 时机**不由 `CombatComponent` 判断；由 **`CombatManager`**（或更高编排）在回合结束等时机调用 Component 的 **`SetBlock`**（例如 `SetBlock(0)`） |

说明：产品反馈曾写「本次出牌后格挡消失」。工程上统一为「**编排层决定时机，Component 只提供 SetBlock 原语**」。若以后改为出牌结算末清挡，仍由 Manager/Session 调同一 API，无需改 Component 语义。

---

## CombatAttributeSet

| 属性 | 角色 | 约束 / 默认 |
|------|------|-------------|
| `MaxHP` | 存量 | `> 0`（初始化时设定） |
| `HP` | 存量 | clamp `[0, MaxHP]` |
| `Block` | 存量 | clamp `≥ 0` |
| `Damage` | 元属性 | 攻击方「将造成」；非负 |
| `DamageToTake` | 元属性 | 受击方「将吸收」；非负 |
| `DamageMultiplier` | 占位 | 默认 `1`；易伤等后续只改此值或并行扩展 |

`PreAttributeChange`：在 Set 内对 `HP` / `Block` / 元属性做 clamp，不把业务公式写进 `CombatComponentBase`。

---

## CombatComponent API

### 构造 / 初始化

- 构造时 `RegisterSet(new CombatAttributeSet())`
- 提供 `InitCombatant(maxHp, currentHp = maxHp)`：设 MaxHP/HP，Block=0，Damage=0，DamageToTake=0，DamageMultiplier=1

### 伤害

```text
SetDamage(panelDamage):
  // panelDamage 为卡面/面板基础值（可带一位小数）
  Damage := panelDamage * DamageMultiplier
  // 不在此处改对方；不在此处 Floor HP

DealDamage(CombatComponent target):
  if target == null → 失败/忽略（测试约定）
  target 的 DamageToTake := 本方 Damage
  target.TakeDamage()
  本方 Damage := 0

TakeDamage():
  amount := DamageToTake
  blocked := Min(Block, amount)
  LoseBlock(blocked)                 // 只扣除用于抵消的部分
  hpLoss := Floor(amount - blocked)  // 向下取整
  HP := Max(0, HP - hpLoss)
  DamageToTake := 0
  // 不在此处清光剩余 Block
```

### 格挡原语

```text
GainBlock(amount):   amount > 0 时 Block += amount
LoseBlock(amount):   Block := Max(0, Block - amount)
SetBlock(value):     Block := Max(0, value)   // Manager 回合结束清挡：SetBlock(0)
```

`SetBlock` 走 Base 的 Set 路径，触发 OnChange，便于 UI/测试订阅。

---

## 职责边界

```text
CombatComponent
  └─ 会算伤害、会改自己/对方 Attribute；不知道「第几回合」「回合是否结束」

CombatManager（后续 feat）
  └─ 回合开始/结束、出牌循环
  └─ 回合结束：对各 CombatComponent 调用 SetBlock(0)
  └─ 不在 Manager 里手写 HP -= x
```

---

## 文件布局（实现时）

```text
Assets/Scripts/Combat/
  Framework/          // COMB-F01
  CombatAttributeSet.cs
  CombatComponent.cs
Assets/Tests/EditMode/
  CombatComponentTests.cs
```

---

## 测试策略（Edit Mode）

1. `SetDamage(10)` 且 Multiplier=1 → Damage=10；Multiplier=1.5 → Damage=15。  
2. 目标 Block=3，`SetDamage(10)` + `DealDamage` → 目标 Block=0（抵消 3），HP 减少 `Floor(7)`；攻击方 Damage=0。  
3. `DealDamage` 后攻击方 Damage 为 0；目标 DamageToTake 为 0。  
4. `GainBlock` / `LoseBlock` / `SetBlock(0)` 行为正确；`SetBlock` 不隐含「回合」语义。  
5. HP 不会超过 MaxHP、不会低于 0。

---

## 实现步骤（设计链完成后再做）

| 步骤 | 内容 |
|------|------|
| S1 | `CombatAttributeSet` + clamp |
| S2 | `CombatComponent` API |
| S3 | Edit Mode 全绿 |

---

## 验收清单

- [ ] 上表 API 与拍板契约一致
- [ ] 无打牌 / 无 Manager 回合机实现（仅 API 可被外部调用）
- [ ] Edit Mode 全绿
- [ ] 注册表状态同步

## 依赖与后续

- **依赖：** `COMB-F01` 已实现并合并  
- **后续：** 见 `COMB-feat-chain.md`（手牌流转、Manager/Session、结算回写）
