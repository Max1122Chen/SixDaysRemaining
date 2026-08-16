# EVT-F03 人物与随机事件 3.0（内容灌入 + 必要规则）

## 元信息

- **ID:** `EVT-F03`
- **类型:** `Feature`
- **状态:** `Review`（实现待 Play 验收）
- **负责人:** `Max`
- **分支：** `main`
- **最后更新：** `2026-08-16`
- **产品源：** `docs/designs/六日英雄 人物设定+随机事件3.0.docx`
- **依赖：** `EVT-F01`/`F02`、`SHLT-F02`/`F03`、`COMB-F10`、`END-F01`、`SAVE-F01`
- **相关：** `SHLT-F04`（并入）、`FEATURE_REGISTRY.md`

## TL;DR

灌入人物/事件 3.0；补死亡+8、**满员置换**、`OptionGate`、概率选项、`followUp` 链。  
`nurse`→`doctor`（无映射）。偷粮离开事件 **保留定义、`enabled:false`**。  
选项门禁用灰显；满员 TakeIn **先置换再入住**（对齐 3.0）。

---

## 已拍板

| # | 决定 |
|---|------|
| 1 | `doctor` 替换 `nurse`，无映射 |
| 2 | 二级选项 = `followUpEventId` + Tag |
| 3 | 选项 ≤3 |
| 4 | 幼童 −8 仅 Passive；日结 bulletin 文案，不双重扣减 |
| 5 | `child_stole_food_day4` 保留 + `enabled:false` |
| 6 | 旧随机池降权/不可达 |
| 7 | D4 存档轻询问（可后） |
| 8 | 满员 **置换 Overlay**（选人离开 / 取消回选项） |
| 9 | `gates[]` OptionGate；灰显 + hint |

---

## 范围

### In

- **SHLT：** cap5；Dead→腐蚀+8；TakeIn 满员走置换
- **EVT：** `enabled`、事件级腐蚀、`gates`、chance、followUp
- **内容：** 3.0 人物/日程；doctor 特质
- **UI：** 选项灰态；置换 Overlay

### Out / 后置（S5）

- 卡牌 2.0；嵌套选项 UI
- 医生辟谷分配例外、幼童陪玩当日翻倍、战斗临时 MaxHP
- D4 存档询问 UI

---

## Design（满员置换）

```text
点选含 TakeIn 的选项且 gates 通过
  → Population < 5：ApplyOption
  → Population >= 5：
       pendingOption = index
       ShowSwapOverlay(alive survivors)
       选中 → Expel 该人 → ApplyOption → 结果屏
       取消 → 回事件选项屏（未 Apply）
```

---

## OptionGate

`gates[]`：`CorruptionAtLeast/AtMost`、`HasSurvivor`、`LacksSurvivor`、`HasTag`、`LacksTag`、`FoodAtLeast`  
（满员 TakeIn 主路径不靠 gate 禁点，走置换）

---

## 切片

| Slice | 内容 | 状态 |
|-------|------|------|
| S0 | cap5、死亡+8、TakeIn/置换 API | Done |
| S1 | gates / chance / followUp / enabled | Done |
| S2 | doctor 改名；幼童/政治家 | Done |
| S3 | 新事件内容 | Done |
| S4 | 置换 UI、选项灰态 | Done |
| S5 | 辟谷等深度 | Deferred |
| S6 | COMB-F10 Done | 待 Play |

---

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-08-16 | 初稿；拍板 doctor/偷粮保留/gates |
| 2026-08-16 | **改回满员置换**（对齐 3.0）；开工 |
| 2026-08-16 | S0–S4 实现；待 Play 验收 |
