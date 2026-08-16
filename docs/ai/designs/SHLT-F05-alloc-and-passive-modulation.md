# SHLT-F05 分配例外 + 日结被动调制

## 元信息

- **ID:** `SHLT-F05`
- **状态:** `Review`（半开放；他人 Play 验收）
- **分支:** `main`
- **依赖:** `SHLT-F03`、`EVT-F03`、`CORE-F07`
- **相关:** `EVT-F04`、`SAVE-F02`

## TL;DR

医生辟谷后跳过分配并维持正常；幼童被动按当日 Tag 调制（陪玩 −12 / 婉拒当日关闭），不永久 Revoke。

## 规则

| Tag | 效果 |
|-----|------|
| `Story.Doctor.BiguFunded` | 资助研究（事件） |
| `Story.Doctor.BiguActive` | 日结首次见到 Funded 时激活：医生永正常、跳过饥饿衰减、禁止喂食 |
| `Story.Child.PlayBoost.Once` | 当日被动 −12，tick 后清除 |
| `Story.Child.PassiveOff.Once` | 当日跳过幼童被动，tick 后清除 |

## Out

战斗临时 HP、实验点名、D4 存档询问 → EVT-F04 / SAVE-F02
