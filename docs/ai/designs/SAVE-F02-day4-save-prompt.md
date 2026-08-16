# SAVE-F02 第四日存档询问

## 元信息

- **ID:** `SAVE-F02`
- **状态:** `Review`（半开放；他人 Play 验收）
- **分支:** `main`
- **依赖:** `SAVE-F01`、`EVT-F03`

## TL;DR

第 4 天凯旋事件链结束后、进 BeforeDayEnd 前，弹一次「是否存档」；是则 `TryWriteCheckpoint`，然后继续日结链。用 Tag `Story.Save.Day4Prompted` 防重复。
