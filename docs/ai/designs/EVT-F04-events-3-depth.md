# EVT-F04 事件 3.0 深度效果

## 元信息

- **ID:** `EVT-F04`
- **状态:** `Review`（半开放；他人 Play 验收）
- **分支:** `main`
- **依赖:** `EVT-F03`、`SHLT-F05`

## TL;DR

实验随机点名/杀人、围栏次日临时玩家 HP、D5 日常复用；幼童婉拒改 Tag 抑制。

## 规则

- `SetRandomSurvivorHealthy` / `KillRandomSurvivor` fragment
- `State.Combat.TempPlayerHp.Once` → 下场战斗 MaxHP=50、开局 HP=45，开战清 Tag
- D5：`corruptionMax:50` 蟑螂复用；`corruptionMin:51` 马桶低腐蚀版复用
