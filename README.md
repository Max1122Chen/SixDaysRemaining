# SixDaysRemaining（《六日英雄》Unity 原型）

卡牌生存 + 文字事件的 **6 日** GameJam 原型。  
当前开发以本地 **`main`** 为准（已合入全部已实现 Feature；历史 `feat/*` 分支均无未合入提交）。

---

## 一句话现状

**可玩主线已通**：主菜单 → 庇护所（分配/入住）→ 出征战斗（选 5 Commit）→ 凯旋 → 随机/人物事件 → 日结 → 次日；支持腐蚀熔断 / 结局 / Meta 回顾 / 节点存档。  
**内容层**：人物与事件 3.0、卡牌数值 2.0 已写入 StreamingAssets；若干批次处于 **Review（半开放）**，完整 Play 验收交由他人。

---

## 实现总览（按系统）

| 系统 | 内容 | 状态 |
|------|------|------|
| **CORE** | 日循环阶段机、`GameState`、GameplayTag、AppFlow + Presentation | Done |
| **COMB** | ASC 卡牌战斗、Corrupted、JSON 内容、日表遭遇、三特质 | Done；F10/F11 Review |
| **SHLT** | 幸存者身份、cap5、饱食/死亡+8、被动、辟谷/幼童日调制 | Done；F04/F05 半开放 |
| **EVT** | 同质事件 + Provider、gates/chance/followUp、3.0 日程 | Done；F03/F04 半开放 |
| **END / META / SAVE** | 政治家战败 E、结局回顾、粗粒度检查点、D4 存档询问 | Done；SAVE-F02 半开放 |
| **UI / Debug** | 运行时 uGUI、庇护所/事件/战斗面板；`~` 调试控制台 | 可玩 Demo 级 |

**数据入口（改完需重启 Play）：**  
`Assets/StreamingAssets/Combat|Events|Shelter/`

---

## 设计师内容 ↔ 实现对齐

产品源在 `docs/designs/`；工程拍板与切片在 `docs/ai/designs/`（以后者为实现准绳）。

### 已对齐（已落地）

| 产品源 | 实现对齐点 |
|--------|------------|
| 卡牌表 / **卡牌 2.0.xlsx** | 玩家基础牌 + starter；敌人回合计划已按 2.0 同步（`COMB-F11`） |
| **人物设定+随机事件 3.0** | 日程事件、doctor 改名、满员置换、gates、死亡+8、辟谷/实验/临时 HP/D4 存档等 |
| **人物模板 2.0**（样板） | 幼童被动 −8、政治家入住/回访、结局 E 钩子 |
| 技术演示 2.0 + 设计师反馈 | 每日单场战斗、格挡/抽牌主规则、罐头语义（存量仍用整型 `foodStock`） |

### 半开放（代码在 main，待他人完整 Play）

- `EVT-F03` / `SHLT-F04` / `SHLT-F05` / `EVT-F04` / `SAVE-F02` / `COMB-F10`（及 `COMB-F11` 轻验）
- 详见 `docs/ai/ACTIVE_WORK.md`、`FEATURE_REGISTRY.md`

### 明确未做 / 后置（相对产品或旧反馈）

| 项 | 说明 |
|----|------|
| 幼童/政治家/运动员等**更多特质** | `COMB-F10` Out；仅英雄/医生/小贼 |
| 特质进牌库（CardDef 1006+） | Out |
| 嵌套选项 UI | Out（二级选项用 `followUpEventId`） |
| 下毒 / 易伤、问号怪、大门双路线 | 设计师反馈「后续」；首版未做 |
| 庇护所道具系统 | ROADMAP 延后 |
| `child_stole_food_day4` | 保留定义、`enabled:false` |
| 旧随机池部分事件 | 降权 / `enabled:false` |
| Excel→JSON 自动导出 | Deferred |
| 移动端 StreamingAssets 异步读 | `TD-006` |

### 已知语义差异（有意）

- 产品旧称「护士」→ 运行时统一 **`doctor` / 医生**（无别名）
- 「陪玩翻倍」落地为日结被动 **−12**（非常驻 −8 的简单 ×2 文案）
- 食物 UI/文案可用「罐头」，分配库存仍为整型

---

## Feature 状态速览

- **Done：** CORE-F02–F08、SHLT-F01–F03、COMB-F01–F09、UI-F01、EVT-F01–F02、END-F01、META-F01、SAVE-F01 …
- **Review / 半开放：** EVT-F03、SHLT-F04/F05、EVT-F04、SAVE-F02、COMB-F10、COMB-F11  
完整表：[`docs/ai/FEATURE_REGISTRY.md`](docs/ai/FEATURE_REGISTRY.md)

---

## 仓库结构

```text
SixDaysRemaining/          Unity 工程（用 Hub 打开此目录）
  Assets/Scripts/
    App/                   GameInstance、AppFlow、Persist/Meta/Save
    Gameplay/              阶段机、Tag、EndingIds
    Shelter/               幸存者、被动
    Combat/                卡牌、特质、Manager
    Events/                事件子系统与 JSON loader
    UI/                    Presentation + Views
    Debug/                 Hybrid 控制台
  Assets/StreamingAssets/  Combat | Events | Shelter 内容
  Assets/Tests/EditMode/   逻辑回归
docs/ai/                   工程协作真相源（ACTIVE_WORK / designs / …）
docs/designs/              产品 PDF / xlsx / docx
.cursor/rules/             Agent 规则
```

---

## 怎么跑

1. Unity **`2022.3.62f3c1`** 打开 `SixDaysRemaining/`
2. 打开引导场景 → **Play**
3. 主线：庇护所分配 → 出征 → 选满 5 张 Commit → 结算 → 事件 → 日结
4. 调试：游戏内 **`~`** 控制台（命令见 Debug registry）

**验证门槛：** Console 无编译错误；至少跑完一天循环；Edit Mode 测试绿。

---

## 文档导航

| 角色 | 优先阅读 |
|------|----------|
| 恢复上下文 | [`docs/ai/BOOTSTRAP_DIGEST.md`](docs/ai/BOOTSTRAP_DIGEST.md) → `ACTIVE_WORK` → `FEATURE_REGISTRY` |
| 设计师 | `docs/designs/` 产品源 + `ACTIVE_WORK` |
| 程序 | `FEATURE_REGISTRY`、对应 `docs/ai/designs/`、`TECH_DEBT` |
| UI | [`docs/ai/UI_HANDOFF.md`](docs/ai/UI_HANDOFF.md)（部分内容可能偏旧；以 `Scripts/UI` + `main` 为准） |

---

## 协作约定（摘要）

- 计划真相：`ACTIVE_WORK` / `FEATURE_REGISTRY` / `TECH_DEBT` / 最近 `PROGRESS_LOG`（**不要**从过期 ROADMAP 快照推 backlog）
- 分支：优先在 **`main`** 按大域推进；未经要求不新开细碎 `feat/*`
- 「prepare commit」≠ 执行提交；push 需明确指令

---

*README 概述更新：2026-08-16（对齐盘点 + main 合入确认）*
