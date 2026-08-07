# UI-F01 战斗卡牌交互修复（Corrupted 伴生 + 卡槽高亮）

## 元信息

- **ID:** `UI-F01`
- **类型:** `Feature`
- **状态:** `Draft`
- **负责人:** `Max` / UI
- **最后更新：** `2026-08-07`
- **分支（建议）：** `feat/ui`
- **相关：** `COMB-F07` Corrupted 伴生；现有 `CombatView` / `CardSlotView` / `CardView`

## TL;DR

COMB-F07 逻辑已落；本 feat **只修战斗 UI 交互**，不改结算规则。

## 已知缺陷（Play 反馈）

1. **第二回合起无法选中 Corrupted 伴生**  
   首回合可拖入槽并打出；第二回合及以后伴生无法选中。
2. **伴生未命中卡槽不复位**  
   业务要求：松手未命中槽时，Corrupted 应自动回到原牌下方伴生位；当前可随意摆放。
3. **原牌 / 伴生叠层不稳定**  
   sibling 顺序时而上时而下，需稳定：手牌区伴生在原牌下（或统一约定）。
4. **卡槽悬停高亮粘滞（旧洞）**  
   牌拖到槽上方会变色；未松手移开后高亮不恢复。待确认是否与「已填充」态混淆。

## 对队友提交的核对（2026-08-07）

本地 `main` / `feat/playable-loop` 已含：

| Commit | 说明 |
|--------|------|
| `3ec31c1` | 基础UI |
| `46d5273` | 修改ui（场景） |
| `f99b00d` | 修改战斗界面及流程（含 `CardSlotView.SetActive`） |
| `73e4c3e` | 修改卡牌吸附位置 |

**结论：** `CardSlotView` 仍只有 `SetHighlight` / `SetActive`，**未见**「悬停离开必清高亮 / 填充态与悬停态分离」的修复；**缺陷 4 大概率仍在**，应在本 feat 一并修。

## 范围

### In

- 上述 1–4 的 `CombatView` / `CardView` / `CardSlotView` 修复
- 必要的 Edit Mode / Play 冒烟清单

### Out

- Corrupted 结算、倍率、+8、100 熔断（属 COMB-F07）
- 美术重做

## 根因分析（代码核对）

### 1) 第二回合起选不中伴生 — **高置信**

`SetInputEnabled` 只刷新 `handCards` / `slotCards`，**漏了 `companionCards`**：

```1280:1294:SixDaysRemaining/Assets/Scripts/UI/CombatView.cs
        private void SetInputEnabled(bool on)
        {
            inputEnabled = on;
            for (int i = 0; i < handCards.Count; i++)
            {
                handCards[i].SetInteractable(on);
            }

            for (int i = 0; i < slotCards.Length; i++)
            {
                if (slotCards[i] != null)
                {
                    slotCards[i].SetInteractable(on);
                }
            }
```

回合结算中 `inputEnabled=false` → `EndRound` 后 `Refresh/RebuildCards` 用 false 创建伴生 → 再 `SetInputEnabled(true)` 不碰伴生 → **伴生永久 `Interactable=false`**。首回合 `OpenCombat` 时 `inputEnabled` 已是 true，故首回合正常。

**修法：** `SetInputEnabled` 同步遍历 `companionCards`。

### 2) 未命中槽不复位 — **高置信**

`OnCardDragEnded` 未命中槽且不在槽内时走 `SnapOrAnimateToHand`，但该方法只在 `handCards` 里找索引；伴生在 `companionCards`，`IndexOf < 0` 直接 return，牌停在松手处。

**修法：** 伴生专用复位：找到 `SourceCard` 对应 `CardView`，落到 `sourcePos + CompanionYOffset`，并重新加入 `companionCards` / 更新可见性。

### 3) 叠层不稳定 — **高置信**

- 创建顺序：先原牌后伴生（伴生后创建 → 默认更靠上）
- `CardView.OnBeginDrag` 调用 `SetAsLastSibling()`，拖过谁谁置顶
- `UpdateHandLayout` **不重排** sibling

**修法：** 布局后统一 `EnsurePairSorting`：每对原牌在下、伴生在上（或反过来，固定一种）；拖拽结束再排一次。

### 4) 卡槽高亮粘滞 — **中高置信（未在队友提交中修复）**

`CardSlotView` 只有悬停色 / Active 色，**无“已填充”态**；`SetHighlight(false)` 一律回到 `normalColor`。  
粘滞更可能来自：

- 悬停与 `SetActive`（回合结算橙光）互相覆盖，离开时只清了 `hoveredSlot` 引用但视觉已被 Active 污染；或
- 拖拽中牌挡住射线，离开槽后 `RaycastSlot` 仍偶发命中旧槽（需 Play 复核）

**修法：** 槽位显式状态机：`Normal | Hover | Filled | Resolving`，`ApplyVisual()` 单一出口；离开悬停只清 Hover，不破坏 Filled。

---

## 建议实现顺序

1. 修 `SetInputEnabled`（最快验证缺陷 1）  
2. 伴生 miss 复位（缺陷 2）  
3. 布局后强制 sibling（缺陷 3）  
4. 槽位状态机（缺陷 4）  
