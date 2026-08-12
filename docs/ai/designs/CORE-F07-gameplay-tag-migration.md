# CORE-F07 GameplayTag 业务迁移（storyFlags → Tag）

## 元信息

- **ID:** `CORE-F07`
- **类型:** `Refactor`
- **状态:** `Done`
- **负责人:** `Max`
- **最后更新：** `2026-08-12`
- **实现分支：** `main`（单 commit，审阅通过后实现）
- **相关：** `CORE-F06`、`EVT-F02`、`FEATURE_REGISTRY.md`

## TL;DR

在 **CORE-F06 Tag Runtime** 与 **幼童禁出征已迁 Tag** 的基础上，把事件域剩余的 **`storyFlags` 双轨** 收成 **GameplayTag 单轨**。  
具体包括：幼童拒玩进度、`requiredFlags` 条件、JSON fragment，以及 `GameplaySubsystem` 上旧 API 的删除。  
**不** 扩到 Debug 开关 / Gate 重构、Save/Load、被动腐蚀、政治家扩展剧情。

---

## 背景

### 当前状态（`main` @ 2026-08-12）

| 能力 | 状态 |
|------|------|
| `GameplayTagContainer` + façade API | ✅ CORE-F06 |
| 幼童陪玩 → `State.ForbiddenExpedition.Once` | ✅ 已迁 |
| Flow / Shelter 消费 `State.ForbiddenExpedition` | ✅ |
| 事件 fragment `AddTag` / `RemoveTag` | ✅ |
| 幼童拒玩 `child_stone_declined_d2/d3` | ❌ 仍用 `SetFlag` / `storyFlags` |
| D4 偷粮 `requiredFlags` | ❌ 仍读 `ActiveStoryFlags` |
| `GameplaySubsystem.storyFlags` | ❌ 与 `gameplayTags` **并存** |

### 问题

1. **双轨状态**：同一局运行中，剧情进度分散在 `storyFlags` 与 `gameplayTags`，Save/META 未来难以定义单一快照。
2. **命名不一致**：禁出征已用 `State.*` 原语，拒玩仍用事件 id 风格字符串（`child_stone_declined_d2`）。
3. **查询语义分裂**：`EventRequirements` 对 flag 做精确字符串匹配，Tag 容器已支持层级 / Query，但未接入事件过滤。

---

## 范围

### In

- 将 **幼童拒玩进度** 迁为 Story Tag：
  - `Story.ChildStone.Declined.Day2`
  - `Story.ChildStone.Declined.Day3`
- 更新 `events.json`：
  - `SetFlag` / `ClearFlag` → `AddTag` / `RemoveTag`（上述 Story Tag）
  - `requiredFlags` → **`requiredTags`**（D4 偷粮：需 **同时** 拥有两个 Tag）
- 扩展事件 schema / loader：
  - `GameEventDef.RequiredTags`
  - DTO / `EventContentJsonLoader` 解析 `requiredTags`
- 扩展 `EventRequirements`：
  - `PassesTagRequirements`：对 `requiredTags` 使用 **`HasTagExact`**（All 语义）
- 扩展 `GameEventQuery`：
  - `ActiveTags` 快照（替代 `ActiveStoryFlags`）
  - `GameEventSubsystem.BuildQuery` 从 `GetTagSnapshot()` 填充
- 扩展 `GameplayTags.cs` 常量（Story 域）
- **删除** `GameplaySubsystem` 的 `storyFlags` 及 `SetStoryFlag` / `HasStoryFlag` / `ClearStoryFlag` / `GetStoryFlagSnapshot`
- **删除** 事件 fragment `SetFlag` / `ClearFlag`（及 loader 中的 ImplementedOps 项）
- 删除 `RunStoryFlags.cs`（常量并入 `GameplayTags`）
- EditMode 测试更新（`GameEventSubsystemTests` 等）
- 文档：`FEATURE_REGISTRY`、`PROGRESS_LOG`、本 design 验收清单

### Out

| 项 | 归属 | 说明 |
|----|------|------|
| 政治家 `refuse` 记进度 | 后续 EVT / Story Tag | 当前 JSON 无 flag；有「拒收后再出现」需求时再加 |
| 幼童常驻腐蚀 −8 | SHLT-F03 | 被动，非 Tag 迁移 |
| 政治家战败 → 结局 E | END-F01 | Ending / Combat 钩子 |
| Tag Save/Load | SAVE-F01 | 本 feat 不序列化 |
| Debug `tag.*` QA 命令 | 可选 CORE-F07-S2 | 见下文「Debug 边界」；**不** 替代 `DebugRunSettings` / `DebugCommandGate` |
| `requiredTagQuery` 复杂表达式 | 后续 | 首版仅 All + Exact；不引入 JSON 嵌套 Query 树 |
| `requiredFlags` 长期并存 | — | 审阅拍板：**硬切**，loader 遇 `requiredFlags` **抛错**（与 F08 硬失败风格一致） |

### 已迁、本 feat 不重复

- `State.ForbiddenExpedition.Once` / Flow / Shelter 门禁
- `AddTag` / `RemoveTag` fragment 运行时

---

## 设计目标

### 1) 单轨运行时状态

一局运行中的 **可查询剧情/状态进度** 只经 `GameplayTagContainer` 读写；`Events` 域通过 fragment 与 `requiredTags` 消费，Flow 通过 `State.*` 原语消费。

### 2) 命名分层

| 前缀 | 用途 | 消费者示例 |
|------|------|------------|
| `State.*` | 系统行为原语（禁出征、将来跳过战斗等） | Flow、Shelter、Combat 门禁 |
| `Story.*` | 剧情进度 / 事件链进度 | EventRequirements、将来 META-F01 回顾 |

### 3) 查询语义拍板

| 场景 | 语义 | API |
|------|------|-----|
| 事件 `requiredTags` | 列表内 **全部** 满足；**精确** tag 名 | `HasTagExact` × All |
| Flow 禁出征 | 拥有原语或其子 tag | `HasTag("State.ForbiddenExpedition")` |
| 将来 Save 快照 | 存 exact key + count | `GetTagSnapshot()` |

**理由：** `Story.ChildStone.Declined.Day2` 不应被 `HasTag("Story.ChildStone")` 误匹配进 unrelated 事件；剧情条件默认 Exact。

---

## Design

### Option A（采纳）：硬切 storyFlags，事件 schema 改用 requiredTags

```text
events.json
  effects: AddTag / RemoveTag
  requiredTags: [ "Story....", ... ]   // All + Exact

GameEventSubsystem.BuildQuery
  ActiveTags ← gameplay.GetTagSnapshot().Keys

EventRequirements
  PassesTagRequirements → 全部 HasTagExact

GameplaySubsystem
  仅保留 gameplayTags（删除 storyFlags）
```

- **优点：** 无双轨；schema 清晰；与 CORE-F06 方向一致。
- **风险：** 需一次性改完 JSON + 测试；旧 `SetFlag` 内容若遗漏会在 loader 硬失败（可接受）。

### Option B（不采纳）：SetFlag 适配器写入 Tag

- 保留 `SetFlag` fragment，内部 map 到 Tag。
- **问题：** 长期双 schema；`requiredFlags` 仍要维护；违背「单 commit 收口」。

### Option C（不采纳）：requiredFlags 与 requiredTags 长期并存

- **问题：** 查询双路径；Save/META 边界继续模糊。

---

## 迁移映射表

| 旧（storyFlags / JSON） | 新（GameplayTag） |
|-------------------------|-------------------|
| `child_stone_declined_d2` | `Story.ChildStone.Declined.Day2` |
| `child_stone_declined_d3` | `Story.ChildStone.Declined.Day3` |
| `child_play_promised` | *(已删除)* → `State.ForbiddenExpedition.Once` |
| `requiredFlags: [d2, d3]` | `requiredTags: [Day2, Day3]` |
| `SetFlag` / `ClearFlag` | `AddTag` / `RemoveTag` |

### `GameplayTags.cs`（拟增）

```csharp
public static class GameplayTags
{
    // State（已有）
    public const string ForbiddenExpedition = "State.ForbiddenExpedition";
    public const string ForbiddenExpeditionOnce = "State.ForbiddenExpedition.Once";

    // Story
    public const string ChildStoneDeclinedDay2 = "Story.ChildStone.Declined.Day2";
    public const string ChildStoneDeclinedDay3 = "Story.ChildStone.Declined.Day3";
}
```

---

## JSON 变更示例

### 幼童 D2 decline

```json
{ "op": "AddTag", "tagId": "Story.ChildStone.Declined.Day2" }
```

### 幼童 D4 条件

```json
"requiredTags": [
  "Story.ChildStone.Declined.Day2",
  "Story.ChildStone.Declined.Day3"
]
```

### 幼童 D4 收尾

```json
{ "op": "RemoveTag", "tagId": "Story.ChildStone.Declined.Day2" },
{ "op": "RemoveTag", "tagId": "Story.ChildStone.Declined.Day3" }
```

---

## Debug 边界（审阅结论写入 design）

### 不迁 Tag 的 Debug 能力

| 能力 | 保留位置 | 理由 |
|------|----------|------|
| `combat.skip` / `combat.sweep` / `playerInvincible` | `DebugRunSettings` | 开发 session 开关，非叙事状态 |
| `DebugCommandGate`（InCombat / InShelter 等） | `DebugCommandGates` | 阶段/会话上下文，非 story 进度 |

### 可选后续 slice（CORE-F07-S2，**本 feat Out**）

QA 用命令，**写入正式 Gameplay Tag**，不替代 Gate：

| 命令 | 作用 |
|------|------|
| `tag add <tagId>` | 快速构造剧情前置（如 D4 测偷粮） |
| `tag remove <tagId>` | 清除 |
| `tag list` | 列出 snapshot |

**不** 把 `combat.skip on` 实现为 `Debug.SkipCombat` Tag。

---

## 实现切片

| Slice | 内容 |
|-------|------|
| S1 | `GameplayTags` Story 常量；`GameEventDef` / DTO `requiredTags` |
| S2 | `EventRequirements` + `BuildQuery` → Tag 快照与 Exact All |
| S3 | `events.json` 迁移；移除 SetFlag/ClearFlag/requiredFlags |
| S4 | 删除 `storyFlags` API、`RunStoryFlags`、旧 fragment enum 值 |
| S5 | EditMode 测试 + loader 硬失败测试（遗留 `requiredFlags` 应 throw） |
| S6 | Registry / Progress / CORE-F06 design 交叉引用更新 |

---

## 验证

### EditMode

- 现有 `GameplayTagTests` 全绿
- `GameEventSubsystemTests`：拒玩 AddTag、D4 requiredTags All、RemoveTag 清理
- 新增/更新：`EventRequirements` 对 `requiredTags` 的 Exact All 用例
- Loader：含 `requiredFlags` 的 JSON 应 **InvalidOperationException**

### Play（人工）

1. 幼童 D2/D3 各婉拒一次 → D4 BeforeDepart 触发偷粮
2. 幼童 D2 陪玩 → D3 禁出征（回归，确保 Story 迁移未破坏 State 路径）
3. 随机事件池仍正常（无 requiredTags 的事件）

---

## 验收清单

- [x] 运行中剧情进度 **仅** 经 `GameplayTagContainer`（无 `storyFlags`）
- [x] `events.json` 无 `SetFlag` / `ClearFlag` / `requiredFlags`
- [x] `requiredTags` 使用 HasTagExact + All 语义
- [x] `RunStoryFlags.cs` 已删除，常量集中在 `GameplayTags`
- [x] EditMode 测试更新（loader 硬失败 + requiredTags + Story AddTag）
- [ ] Play：幼童拒玩 ×2 → D4 线 + 陪玩禁出征回归
- [x] **未** 改动 DebugRunSettings / DebugCommandGate 行为

---

## 审阅后下一步

若审批通过：

1. 在 `main` 上单 commit 实现 CORE-F07
2. Play 复验政治家 D3（EVT-F02 余量）
3. 登记 SHLT-F03 / END-F01 design（幼童 −8、政治家 E）

## 请确认

1. **硬切** `requiredFlags` / `SetFlag` / `ClearFlag`（loader 遇旧字段抛错）是否 OK？
2. `requiredTags` 默认 **All + HasTagExact** 是否 OK？（暂不 support Any/None JSON）
3. Debug `tag.*` QA 命令：**CORE-F07 Out**，单独 S2 — 是否同意？
