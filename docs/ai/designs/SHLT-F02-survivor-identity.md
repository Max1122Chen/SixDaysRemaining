# SHLT-F02 幸存者身份目录 + 入住 / 状态 / 死亡

## 元信息

- **ID:** `SHLT-F02`
- **类型:** `Feature`
- **状态:** `Done`（已合并 `main`）
- **负责人:** `Max`
- **最后更新：** `2026-08-07`
- **分支：** `feat/shelter`
- **产品源：** `docs/designs/人物模板2.0.pdf`、`docs/designs/六日英雄—技术演示文档2.0.pdf`（庇护所段）、卡牌表中的护士/小贼来源备注
- **相关：** `SHLT-F01`、`COMB-F08`（数据驱动先例）、`EVT-F01`（入住触发延后）、`[Feature Registry](../FEATURE_REGISTRY.md)`

## TL;DR

本 feat 只回答幸存者 **「是什么」**：用 **JSON 数据驱动的身份目录** 定义全部角色；运行时支持 **入住、状态转变、死亡（及已有的离开）**。  
**不做**复杂入住被动、长线剧情、结局钩子；**特质 / 特质牌** 定义不清，本阶段 **整块忽略**。  
架构对齐战斗侧：`StreamingAssets` + Loader + 硬校验失败。

---

## 范围

### In

- **身份定义（Def）** 与 **局内实例（Survivor）** 分离：Def 只读；实例持 `defId` + 可变状态
- **全角色目录**（首版一次录入，见下表）；开局 starter 从数据读，替换硬编码 `Alice` / `Bob`
- **入住 API**：按 `defId`（或稳定 id）创建实例并入 roster；禁止重复入住同一 `defId`（首版）
- **状态机**：沿用 / 收紧 `SHLT-F01` 的 Healthy → Hungry → Dying → Dead，以及 `Left`
- **耐饿差异（身份字段）**：不同身份「饥饿 → 濒死」所需天数不同（产品：幼童 / 运动员 / 政治家 = 1 / 2 / 3）；其它降档仍按 F01「一天一格」
- **死亡**：濒死且日结仍无粮 → `Dead`；人口同步；可记录人员变更文案（沿用 `RecentPersonnelChanges`）
- **数据驱动**：`StreamingAssets/Shelter/` JSON + Loader；缺文件 / 校验失败 → **抛错硬失败**（同 COMB-F08）
- Edit Mode 测试：加载、starter、入住、耐饿天数、死亡

### Out（明确不做）

| 项 | 说明 | 归属 |
|----|------|------|
| **特质 / traits 字段与 UI** | 产品未定义清 | 日后 `SHLT-F0x` |
| **特质牌**（希望之光 / 治疗 / 鹞子翻身等） | 战斗内容 | 已有 COMB；入住发牌另 feat |
| **入住被动行为** | 如幼童每日腐蚀 −8、政治家战败结局 E | 行为 / 事件 feat |
| **长线剧情 / 专属突发事件** | 幼童抛石头、政治家敲门剧情选项 | `EVT-F01` |
| **对话 / 检视文案树** | 仅允许列表显示 name + status + hunger | UI 迭代 |
| **道具互动**（幼童石头、政治家花等） | — | 延后 |
| **Excel 导出管线** | 可手写 JSON | 可选 TECH |

---

## 现状、目标与差距

| | |
|--|--|
| **当前** | `Survivor` 仅 `name` / `hunger` / `status`；`InitializeDefaultRoster` 写死 Alice/Bob；`TakeIn(string name)` 无身份模板 |
| **目标** | 全部角色有 Def；按 id 入住；状态/死亡可测；耐饿按身份；内容改 JSON 不改代码 |
| **差距** | 无 Def / JSON / Loader；无 `defId`；无饥饿→濒死的身份天数计数；无全角色表 |

---

## 角色目录（首版「全部角色」）

产品文档可提取到的命名身份如下。**行为栏仅作产品备忘，本 feat 不实现。**

| `id` | 显示名 | 建议开局 | 产品耐饿（饥→濒） | 产品备忘（Out） |
|------|--------|----------|-------------------|-----------------|
| `child` | 幼童 | **Starter** | 1 天 | 常驻腐蚀 −8；D2/D3 专属事件；拒两次会偷粮溜走 |
| `athlete` | 运动员 | **Starter**（技术演示「初始两位」中的另一位；模板 PDF 未单页展开，**待你确认**） | 2 天 | — |
| `politician` | 政治家 | 后期入住 | 3 天 | D3 事件带粮入住；战败触发结局 E |
| `nurse` | 护士 | 后期入住 | 默认 1（未单列则用默认） | 接纳后特质牌「治疗」→ Out |
| `thief` | 小贼 | 后期入住 | 默认 1 | 接纳后特质牌「鹞子翻身」→ Out |

**开局二人待确认点：** 模板写幼童为「初始两位之一」；技术演示列举身份为「幼童 / 运动员 / 政治家」。草案默认 **幼童 + 运动员** 为 starter。若第二人不是运动员，审阅时改 JSON / 本表即可。

**占位角色：** 事件原型里的「阿杰」**不**进正式身份目录（无模板）；调试入住只用目录内 id。

---

## Design（推荐方案）

### 数据：`SurvivorDef`

```text
StreamingAssets/Shelter/
  survivors.json     // 身份目录
  starter.json       // 开局 defId 列表（或 survivors 内 flags）
```

建议字段（可微调命名，实现前定 schema）：

| 字段 | 类型 | 含义 |
|------|------|------|
| `id` | string | 稳定键（如 `child`） |
| `displayName` | string | UI / Log 名 |
| `defaultHunger` | int | 入住或开局初始饱食度 |
| `defaultStatus` | string | 可选；缺省由 hunger 推导 |
| `hungryToDyingDays` | int | 处于 Hungry 且日结未回升时，累计几天后进入 Dying；幼童1 / 运动员2 / 政治家3 |
| `isStarter` | bool | 可选；或只写在 `starter.json` |

**不做：** `traits[]`、`passiveEffects`、`eventHooks`、`traitCardIds`（可在注释/文档里留给未来，JSON **不出现** 以免假实现）。

### 运行时：`Survivor` 实例

```csharp
public class Survivor
{
    public string defId;           // 指向 SurvivorDef.id
    public string name;            // 通常 = displayName；允许事件覆写显示名则另议（首版 = Def）
    public int hunger;
    public SurvivorStatus status;
    public int hungryDayCount;     // 连续处于 Hungry 且未喂饱的日结计数；供耐饿判定
}
```

### `ShelterManager` 变更要点

- `InitializeDefaultRoster`：Loader 读 starter → 按 Def 生成实例
- `TakeIn(string defId)`（或重载）：查 Def → `RegisterSurvivor`；已存在同 `defId` 则 no-op / 显性 Log
- 保留 `Expel` → `Left`
- `ProcessEndOfDay`：
  1. 每人 `hunger` 衰减（F01 规则）
  2. 更新 `hungryDayCount`（Healthy 清零；Hungry 累加；喂到 Healthy 清零）
  3. `hunger == 0` 或耐饿耗尽 → `Dying`；已 `Dying` 且仍无粮 → `Dead`
- `population` / `SyncPopulation` 逻辑保持：不计 `Dead` / `Left`

### 状态规则（相对 F01 的增量）

仍适用：

- `hunger == 0` → 至少 `Dying`（与 F01 一致）
- `Dying` 日结后仍 `hunger == 0` → `Dead`

新增：

- 当 `hunger > 0` 但处于「未获分配导致的饥饿档」时：用 `hungryDayCount` 与 `hungryToDyingDays` 决定是否从 `Hungry` 落入 `Dying`
- **精确边界**（实现前在 Review 拍板一次）：  
  - **提案 A（推荐）：** 日结扣粮后若 `hunger <= hungryThreshold` 则 `hungryDayCount++`，达到 `hungryToDyingDays` → `Dying`；任意一次分配使 `hunger` 回到 Healthy 阈值则清零  
  - 产品原文「一天不给食物掉一格」与「饥→濒要 1/2/3 天」并存：Healthy→Hungry 仍一天一格；仅 Hungry→Dying 吃耐饿天数

### 与事件 / 战斗的边界

- **入住入口**：本 feat 提供 `TakeIn(defId)`；随机事件何时调用 → `EVT-F01`（或现有 `RandomEventCatalog` 临时代码可先改成合法 `defId`，不扩展事件池）
- **战败结局 E、腐蚀被动、发特质牌**：禁止在 Shelter 日结里偷偷实现；登记后续 feat

### 为何不上继承树

单 `Survivor` + `SurvivorDef` 数据驱动；不为幼童/政治家写子类。行为日后用 hook / 小组件挂 Def，而不是 OOP 爆炸。

---

## 文件布局（预期）

```text
Assets/StreamingAssets/Shelter/
  survivors.json
  starter.json

Assets/Scripts/Shelter/
  Survivor.cs                 // 扩展字段
  SurvivorDef.cs              // 或 Content/ 下
  Content/ShelterContentJsonLoader.cs
  ShelterManager.cs           // roster / TakeIn / 日结
  ShelterContent.cs           // Ensure 单例式加载（可对齐 CombatContent）

Assets/Tests/EditMode/
  ShelterSurvivorIdentityTests.cs
```

---

## 验证

- Edit Mode：JSON 校验失败抛错；五人 Def 可加载；starter 两人；`TakeIn` 第三人；耐饿天数表驱动 Dying；Dying→Dead；重复入住拒绝
- Play（手测）：Shelter 面板名单显示中文名与状态；Debug 入住护士/小贼/政治家
- 不要求本 feat 做出入主动画或正式立绘

## 验收清单

- [x] Design 审阅通过（开局幼童+运动员；耐饿提案 A；护士/小贼=1）
- [x] 全角色进入 `survivors.json`（无行为字段）
- [x] 入住 / 状态 / 死亡测试（Edit Mode）
- [x] Alice/Bob 硬编码移除
- [x] 无特质牌、无腐蚀被动、无结局 E
- [ ] Registry / ACTIVE_WORK / PROGRESS 同步（随本批）
- [x] 已 rebase 伙伴 UI 提交并合入 `main`（Edit Mode 待本地 Unity 复核）

---

## 开放问题（已拍板）

1. 开局第二人 = **运动员**
2. 护士 / 小贼 `hungryToDyingDays` = **1**
3. 耐饿 = **提案 A**
4. `TakeIn` 主键 = string `id`
