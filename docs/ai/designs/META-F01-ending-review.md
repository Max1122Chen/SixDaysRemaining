# META-F01 结局回顾（成就式档案）

## 元信息

- **ID:** `META-F01`
- **类型:** `Feature`
- **状态:** `Done`（EditMode + Play 基本正常）
- **负责人:** `Max`
- **分支：** `main`
- **最后更新：** `2026-08-16`
- **依赖：** `CORE-F08`（Persist 底座）、`END-F01` / `SHLT-F03`（`endingId`）、`CORE-F07`（Story Tag 字段预留）
- **相关：** `SAVE-F01`（同底座、不同文件）、`FEATURE_REGISTRY.md`

## TL;DR

在 **Persist 底座** 之上做 **App 级 meta 档案**：终局写入已见 `endingId`，终局屏展示本局摘要，主菜单「回顾」列已解锁结局。  
**不**做单局读档；**不**与 `run-save.json` 混写。

**体验：C 极简（终局摘要 + 跨局解锁列表）。**

---

## 范围

### In

| # | 交付 |
|---|------|
| 1 | `MetaProfileDto` + `MetaProfileService`（Load/Save/UnlockEnding/Query/Clear） |
| 2 | 文件：`meta-profile.json` |
| 3 | 终局写入：`ForceEndingFlow` / 进入 Ending 且 `endingId` 有效 → `UnlockEnding` |
| 4 | `EndingView`：本局文案 + 轻量摘要（天 / 腐蚀） |
| 5 | 主菜单「回顾」：已解锁结局列表 |
| 6 | **Debug 命令**（见下）：解锁 / 列出 / 清空 meta |
| 7 | EditMode：解锁幂等、往返、清档 |

### Out

| 项 | 归属 |
|----|------|
| Persist I/O | `CORE-F08` |
| 单局存档 | `SAVE-F01` |
| 成就弹窗 / Steam / 条件表 | 远期 |
| 战斗统计 | 远期 |
| Story Tag 回顾 UI | 首版不做（DTO 可留空数组） |

---

## Design

### 已拍板（本轮补充后）

| 点 | 决定 |
|----|------|
| Story Tag | 字段预留；首版不写、不展示 |
| 坏档 | 若可写则先 `.bak`，再空 profile 继续 |
| 回顾 UI | 主菜单独立轻量页 |
| Debug | 必须有解锁 / 清档命令，便于验收 |

### 数据（v1）

```json
{
  "schemaVersion": 1,
  "unlockedEndingIds": ["Ending.G", "Ending.E"],
  "unlockedStoryTags": []
}
```

| 字段 | 规则 |
|------|------|
| `schemaVersion` | `1` |
| `unlockedEndingIds` | 去重；只增不减（清档仅 Debug / 显式 Clear） |
| `unlockedStoryTags` | 首版恒可空；UI 不展示 |

### 服务

```csharp
public sealed class MetaProfileService
{
    public void LoadOrCreate();
    public bool UnlockEnding(string endingId);
    public bool HasEnding(string endingId);
    public IReadOnlyList<string> GetUnlockedEndingIds();
    public void ClearAll();   // 删档或写空 DTO 并 Save
}
```

- `GameInstance` 持有；菜单 / 启动时 `LoadOrCreate`。
- `StartNewRun` **不**清 meta。

### 写入时机

```text
ForceEndingFlow / Ending 且 endingId 有效
  → UnlockEnding(endingId) → 立即 Save
```

凡写入 `endingId` 的路径（G / E / MaxDay / `run.ending force`）都应 Unlock。

### UI

| 面 | 行为 |
|----|------|
| `EndingView` | 文案 + 「第 N 天」等摘要 |
| 主菜单「回顾」 | 列表；点开看文案；关闭回菜单 |

### Debug 命令（META-F01 In）

| 命令 | Gate | 行为 |
|------|------|------|
| `meta.ending unlock <endingId>` | Always | 写入解锁并 Save；已有则提示已存在 |
| `meta.ending unlock all` | Always | 解锁已知常量：`Ending.G` / `E` / `MaxDay`（及文档列出的 id） |
| `meta.list` | Always | 打印已解锁 endingId 列表 |
| `meta.clear` | Always | `ClearAll()`；不影响 `run-save.json` |

说明：
- 与现有 `run.ending force` 区分：后者是**本局强制进结局**；`meta.ending unlock` 只改**档案**，可不进 Ending 屏。
- EditMode：`DebugCommandRegistryTests` 覆盖 unlock / clear / list（可用临时 persist 根或 service 注入）。

### 与 SAVE

| | META | SAVE |
|--|------|------|
| 文件 | `meta-profile.json` | `run-save.json` |
| `meta.clear` | 只清 meta | run 不动 |
| `save.clear`（SAVE） | meta 不动 | 只清 run |

---

## 验证

### EditMode

- [x] Unlock 幂等；Clear 后列表空
- [x] 坏 JSON → `.bak` + 空档可继续
- [x] Debug 命令注册与门禁

### Play

- [x] 基本正常（终局解锁 / 回顾 / Debug）

---

## 验收清单

- [x] 依赖 CORE-F08 已合
- [x] In + Debug 命令落地
- [x] EditMode 绿；Play 抽检

---

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-08-16 | 初稿 Discuss（方案 C） |
| 2026-08-16 | 拍板 Story/坏档/UI；补 meta.* Debug 命令 |
| 2026-08-16 | 实现并通过验收 → Done |
