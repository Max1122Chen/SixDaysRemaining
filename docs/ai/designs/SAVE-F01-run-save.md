# SAVE-F01 受限单局存档（检查点 · 粗粒度）

## 元信息

- **ID:** `SAVE-F01`
- **类型:** `Feature`
- **状态:** `Done`（EditMode + Play 基本正常）
- **负责人:** `Max`
- **分支：** `main`
- **最后更新：** `2026-08-16`
- **依赖：** `CORE-F08`（Persist）、Tag / Passive / defId / `endingId`（已基本稳定）
- **相关：** `META-F01`、`CORE-F05`、`FEATURE_REGISTRY.md`

## TL;DR

单局存档 = **特定节点上的粗粒度检查点**，只恢复「庇护所层面」的大状态（天、食物、腐蚀、NPC 等），**永远不支持战斗局内粒度**（手牌 / 槽位 / 意图 / 回合）。  
文件：`run-save.json`（与 meta 分家）。本轮定边界与 Debug；字段详表实现前可再对一版。

---

## 策划意图（已对齐）

| 原则 | 含义 |
|------|------|
| **节点存档** | 只在约定安全节点写入/允许继续；不是随时 F5 |
| **粗粒度** | 存「这一天庇护所还剩什么」级数据，不存战斗过程 |
| **不进 Combat** | 读档后落在庇护所相位（Prep / 凯旋后），由玩家再次出征 |

---

## 范围

### In（实现时）

| # | 交付 |
|---|------|
| 1 | `RunSaveDto` + `RunSaveService`（经 `JsonFileStore`） |
| 2 | 文件：`run-save.json` |
| 3 | **检查点写档**（见节点表）；非法相位 Save → 拒绝 |
| 4 | 主菜单「继续」：合法档 → 重建粗状态并进对应相位 |
| 5 | 新开局：删除或覆盖旧 run 档 |
| 6 | **Debug**：强制写/读/清/打印 run 档 |
| 7 | EditMode：往返、相位门禁、清档 |

### Out（硬拒绝）

| 项 | 说明 |
|----|------|
| 战斗中途存档 | 手牌、五槽、敌意图、回合数、Trait 已用标记等 |
| 读档直接进 `GameplayPhase.Combat` | 禁止 |
| 事件对话中途存档 | 禁止；事件**全部结束**回到庇护所节点后再写 |
| Meta 解锁 | `META-F01` |
| 多存档槽 | 远期 |
| 与 meta 同一 JSON | **禁止** |

---

## 允许的存档节点（检查点）

写入时机 = 「节点到达且世界已稳定」（自动写一份最新检查点即可；首版可不做手动「存档」按钮）。

| 节点 | 相位 | 说明 |
|------|------|------|
| **出征准备就绪** | `ExpeditionPrep` | 当天庇护所操作/事件已收束，可出征前 |
| **凯旋落地** | `TriumphReturn` | 战斗已结算回写完毕、结算 UI 可继续之后 |
| **日结完成 → 次日早晨** | 进入新一天的 `ExpeditionPrep` | 与「出征准备」可合并为同一类检查点 |

**明确不写：**

- `Combat` 任意时刻  
- 事件序列播放中（BeforeDepart / 凯旋事件等未 `Finished`）  
- `Ending`（终局应清 run 档或禁止「继续」）

读档落点：始终落到表中的庇护所相位，**重新开打需玩家再点出征**。

---

## 快照字段（粗粒度 v1 草案）

### 必存（大状态）

| 域 | 字段 |
|----|------|
| Run | `schemaVersion`, `rngSeed`, `day`, `foodStock`, `corruption`, `population`, `currentPhase` |
| Ending | `endingId`（有值则档视为不可「继续」，或写档前已删 run） |
| Shelter | 每位幸存者：`defId`、`status`、饱食/饥饿相关、被动 id 列表等**实例状态** |
| Tags | GameplayTag 容器快照（Story 等） |
| Events | 日额度 / 已触发计数等**足以不重复刷关键事件**的最小运行态（实现对表） |

### 明确不存

- `CombatSession` / `DeckRuntime` / 手牌 / 牌库顺序  
- 敌意图、五槽 Commit、回合内 Block  
- Trait「本场已用」  
- UI 叠层、拖拽中卡牌  

若缺某字段会导致「继续后事件错乱」，宁可列入粗状态表，也**不要**因此引入战斗粒度。

---

## Debug 命令（SAVE-F01 In）

| 命令 | Gate | 行为 |
|------|------|------|
| `save.status` | Always | 是否存在 run 档、`schemaVersion`、day/phase 摘要 |
| `save.write` | InShelter（或 RunActive 且相位合法） | 若当前在允许节点则强制写检查点；否则报错说明相位 |
| `save.load` | MenuOnly 或 Always | 从盘读档并重建（测试用；正式「继续」走同一 API） |
| `save.clear` | Always | 删除 `run-save.json`；**不影响** `meta-profile.json` |

与 META：

| 命令 | 影响 |
|------|------|
| `meta.clear` | 只清成就档案 |
| `save.clear` | 只清单局检查点 |
| `persist.path` | 只打印路径（CORE-F08） |

---

## 与 Persist / META

```text
CORE-F08  JsonFileStore
    ├─ META-F01   meta-profile.json   ← 跨局
    └─ SAVE-F01   run-save.json       ← 单局粗检查点
```

---

## 验证（实现时）

### EditMode

- [x] DTO 往返；Combat 相位 Save 失败
- [x] Load 后 day/food/corruption/NPC 一致
- [x] `save.clear` / `meta.clear` 互不影响

### Play

- [x] 基本正常（检查点继续 / 新游戏清档）

---

## 当前动作

- 已实现并通过验收 → **Done**。

---

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-08-16 | 边界稿 Discuss |
| 2026-08-16 | 对齐策划：节点检查点 + 粗粒度；禁战斗粒度；补 save.* Debug |
| 2026-08-16 | 实现并通过验收 → Done |
