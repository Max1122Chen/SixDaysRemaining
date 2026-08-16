# CORE-F08 Persist 底座（双层存档基建）

## 元信息

- **ID:** `CORE-F08`
- **类型:** `Feature`
- **状态:** `Done`（EditMode + Play 基本正常）
- **负责人:** `Max`
- **分支：** `main`
- **最后更新：** `2026-08-16`
- **相关：** `META-F01`、`SAVE-F01`、`FEATURE_REGISTRY.md`

## TL;DR

抽出一层**极薄的磁盘 Persist 基建**：统一路径、JSON 读写、`schemaVersion`、原子写与坏档兜底。  
**本 feat 不实现**成就 UI，也**不实现**单局读档；只给 META / SAVE 共用同一套 I/O。

**原则：两层数据两份文件，共享底座、不共享 schema。**

---

## 范围

### In

| # | 交付 |
|---|------|
| 1 | `PersistPaths`：`persistentDataPath` 下固定根目录与文件名常量 |
| 2 | `JsonFileStore`：`TryLoad` / `Save` / `Exists` / `TryDelete`；UTF-8 JSON；原子写（`.tmp` → replace） |
| 3 | 信封约定：根对象含 `schemaVersion`（`int`）；版本不匹配 → 视为无档 / 可丢弃 |
| 4 | Debug：`persist.path` 打印根目录与两文件路径（Always） |
| 5 | EditMode：读写往返、坏 JSON、缺文件、删除 |
| 6 | 文档 + Registry |

### Out

| 项 | 归属 |
|----|------|
| 结局解锁 / 回顾 UI / meta 清档命令 | `META-F01` |
| 单局快照字段、检查点写档、run 清档命令 | `SAVE-F01` |
| 云存档 / 多槽 UI / 加密 | 远期 |
| 战斗粒度存档 | **永久 Out**（见 SAVE-F01） |
| 改 `PlayerPrefs` 设置项 | 保持现状（语速等） |

---

## 现状 → 目标

| | |
|--|--|
| 现状 | 仅有 `PlayerPrefs` 设置；无统一写盘 |
| 目标 | App 层一个小 Store；Meta / Run 各一份 JSON |
| 差距 | 无路径约定、无原子写、无版本信封 |

---

## Design

### 双层文件（硬约束）

```text
Application.persistentDataPath/
  SixDaysRemaining/
    meta-profile.json   ← META（跨局档案）
    run-save.json       ← SAVE（单局粗粒度检查点；本 feat 只预留路径常量）
```

- **禁止**把 meta 与 run 塞进同一文件。
- **禁止**用 `PlayerPrefs` 存解锁集合或 run 快照。

### API（建议）

```csharp
// App 程序集：SixDaysRemaining.App / Persist/
public static class PersistPaths
{
    public static string RootDirectory { get; }      // .../SixDaysRemaining
    public const string MetaProfileFileName = "meta-profile.json";
    public const string RunSaveFileName = "run-save.json";
    public static string MetaProfilePath { get; }
    public static string RunSavePath { get; }
}

public static class JsonFileStore
{
    public static bool TryLoad<T>(string path, out T data, out string error);
    public static bool Save<T>(string path, T data, out string error);
    public static bool Exists(string path);
    public static bool TryDelete(string path, out string error);
}
```

- `T` 为 **DTO**（`JsonUtility` 友好），与运行时领域对象解耦。
- 测试用临时绝对路径，避免污染真机档案。

### 原子写

1. 写 `path + ".tmp"`
2. replace / delete+move 到正式 `path`
3. 失败时尽量删 `.tmp`；**不得**留下截断的正式文件

### schemaVersion

- 每个消费者 DTO 自带 `schemaVersion`。
- 未知版本 → `TryLoad` false；**不自动清档**（调用方决定是否 `TryDelete` / `.bak`）。
- CORE-F08 **不**做迁移表。

### Debug（本 feat）

| 命令 | Gate | 行为 |
|------|------|------|
| `persist.path` | Always | 打印 `RootDirectory`、`meta-profile` / `run-save` 绝对路径与是否存在 |

清档 / 解锁类命令挂在 META / SAVE，不在底座堆业务。

### 放置

| 项 | 选择 |
|----|------|
| 程序集 | `SixDaysRemaining.App` |
| 目录 | `Assets/Scripts/App/Persist/` |
| 依赖 | BCL + `Application.persistentDataPath`；不引用 Shelter/Combat/Events |

---

## 验证

### EditMode（必达）

- [x] Save → TryLoad 往返
- [x] 缺文件 / 非法 JSON → false，无未处理异常
- [x] TryDelete 后 Exists false

### Play

- [x] `persist.path` 可用（随批验收）

---

## 验收清单

- [x] In 落地；EditMode 绿
- [x] Registry / PROGRESS 更新
- [x] META / SAVE 依赖本 feat

---

## 变更记录

| 日期 | 说明 |
|------|------|
| 2026-08-16 | 初稿 Discuss |
| 2026-08-16 | 补 `persist.path`；明确战斗粒度永久 Out |
| 2026-08-16 | 实现并通过验收 → Done |
