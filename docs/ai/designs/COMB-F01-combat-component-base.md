# COMB-F01 轻量 ASC：CombatComponentBase + AttributeSet

## 元信息

- **ID:** `COMB-F01`
- **类型:** `Feature`
- **状态:** `In Progress`（实现中）
- **负责人:** `Max`
- **最后更新：** `2026-07-30`（开始编码）
- **分支：** `feat/combat`
- **相关：** `[REFERENCES](../REFERENCES.md)`（CardGameDemo / UE GAS）、`[Feature Registry](../FEATURE_REGISTRY.md)`、下一 feat `COMB-F02`

## TL;DR

做一个**业务无关**的轻量 ASC：`CombatComponentBase` 持有若干 `AttributeSet`；**属性值存在 AttributeSet 字段上**（对齐 UE）；Base 只提供 **Get / Set + OnChange**，**不做 Modifier / GE / Tag / GA**。  
HP、伤害等具体属性留给 `COMB-F02` 的业务 Set 与派生 `CombatComponent`。

---

## 范围

### In

- `AttributeData`：至少 `Current`（可选内部同步 `Base == Current` 占位，便于以后加 Modifier）
- `AttributeSet` 抽象基类：归属某个 `CombatComponentBase`，子类用字段持有 `AttributeData`
- `CombatComponentBase`：注册 Set、按类型取 Set、统一 Get/Set、变更委托
- Edit Mode 测试：注册、读写、OnChange、禁止无主 Set
- 目录：`Assets/Scripts/Combat/Framework/`（或 `Combat/Asc/`）+ asmdef

### Out

- 任何战斗业务属性名（MaxHP / Damage 等）——归 `COMB-F02`
- Modifier 聚合、GameplayEffect、GameplayTag、GameplayAbility
- 打牌、牌库、敌人 AI、`CombatSession` / `CombatManager`
- Canvas UI

---

## 现状、目标与差距

- **当前：** 无战斗 Attribute 基础设施。
- **目标：** 与 UE「ASC + AttributeSet」对齐的最小可测骨架；业务可在下一 feat 挂 Set 并写伤害管线。
- **差距：** 缺 Base 组件与 Set 约定。

---

## 设计

### 1) 与 UE 对齐的存储模型

```text
CombatComponentBase（≈ 精简 UAbilitySystemComponent）
  └─ AttributeSets[]
        └─ AttributeSet 子类字段：AttributeData Xxx
              └─ Current（首版；Base 可选且恒等于 Current）
```

- **值在 Set 字段上**，不在 Base 的大 Dictionary 里复制一份业务真相。
- Base 负责：持有 Set 列表、统一写入入口、广播变更。
- 对齐参考：UE GAS；设计思想亦见 CardGameDemo `gameplay-framework.md`（本 feat 仅取 ASC+Set 骨架，不取 GE/Pipeline）。

### 2) 核心类型

```csharp
public struct AttributeData
{
    public float Current;
    // 可选：public float Base; // 无 Modifier 时始终 = Current
}

public abstract class AttributeSet
{
    public CombatComponentBase Owner { get; private set; }

    internal void Bind(CombatComponentBase owner) { ... }

    /// <summary>Set 前可改写 newValue（如 clamp）；默认原样返回。</summary>
    protected virtual float PreAttributeChange(string attributeName, float oldValue, float newValue)
    {
        return newValue;
    }
}

public class CombatComponentBase
{
    public event Action<AttributeChangeInfo> OnAttributeChanged;

    public void RegisterSet(AttributeSet set);
    public T GetSet<T>() where T : AttributeSet;

    public float Get(AttributeSet set, string attributeName);
    public void Set(AttributeSet set, string attributeName, float newValue);
}

public struct AttributeChangeInfo
{
    public AttributeSet Set;
    public string AttributeName;
    public float OldValue;
    public float NewValue;
}
```

### 3) 字段如何被 Get/Set 找到（实现约定）

首版推荐 **显式注册表**，避免反射魔法不好测：

```csharp
// AttributeSet 子类 Init 时：
protected void RegisterAttribute(string name, ref AttributeData data)
{
    Owner.BindAttributeStorage(this, name, ref data); // 或 Set 内 Dictionary<string, AttributeData存储槽>
}
```

更简单、够用的做法：

- 每个 `AttributeSet` 内部 `Dictionary<string, AttributeData>` **或** 对每个字段包一层 `AttributeHandle`；
- 对外仍用强类型属性访问器：

```csharp
public class ExampleSet : AttributeSet
{
    AttributeData health;

    public float Health
    {
        get { return Owner.Get(this, "Health"); }
        set { Owner.Set(this, "Health", value); }
    }

    protected override void OnBound()
    {
        Register("Health", ref health);
    }
}
```

**硬约束：** 业务代码改数值应走 `Owner.Set` / 属性 setter，以便触发 `PreAttributeChange` 与 `OnAttributeChanged`。测试可断言直改 `AttributeData` 字段而不走 Set 时不发事件（文档约定即可，不必强制 private）。

### 4) Set / OnChange 时序

```text
CombatComponentBase.Set(set, name, requested):
  old = current
  clampedOrAdjusted = set.PreAttributeChange(name, old, requested)
  if (clampedOrAdjusted == old) return  // 可选：无变化不广播
  write storage
  OnAttributeChanged({ set, name, old, clampedOrAdjusted })
```

- Clamp 逻辑**只写在具体 AttributeSet**（`COMB-F02` 的 Health 等），Base 不认识 MaxHP。

### 5) 文件布局建议

```text
Assets/Scripts/Combat/
  Framework/
    AttributeData.cs
    AttributeSet.cs
    AttributeChangeInfo.cs
    CombatComponentBase.cs
    SixDaysRemaining.Combat.asmdef   // 首版可仅 Framework；F02 同程序集追加
Assets/Tests/EditMode/
  CombatComponentBaseTests.cs
```

测试用一个内部/测试专用 `TestAttributeSet`（含 `Foo` 属性），**不要**把业务 Set 放进本 feat。

### 6) 与后续 feat 的边界

| Feat | 内容 |
|------|------|
| **COMB-F01（本文）** | Base + AttributeSet 机制 |
| **COMB-F02** | `CombatAttributeSet`（MaxHP/HP/Block/Damage/…）+ 派生 `CombatComponent` 的 Deal/Take/Block API |
| 更后 | 打牌 / Session / Manager / BattleOnly |

---

## 实现步骤

| 步骤 | 内容 | 验证 |
|------|------|------|
| S1 | asmdef + `AttributeData` / `AttributeChangeInfo` | 编译 |
| S2 | `AttributeSet` + `CombatComponentBase` Register/Get/Set/事件 | Edit Mode |
| S3 | `PreAttributeChange` 可改写数值 | Edit Mode |
| S4 | 文档与注册表状态同步 | — |

---

## 测试策略（Edit Mode）

1. Register 两个 Set，`GetSet<T>` 成功。  
2. Set 属性 → Get 一致；`OnAttributeChanged` 收到 old/new。  
3. `PreAttributeChange` 把 999 clamp 成上限（测试 Set 自造上限字段）。  
4. 重复 Register 同类型 Set → 失败或覆盖策略写死并测（建议：**同类型只允许一个**，第二次抛异常或 Debug.LogError）。  
5. 未 Bind 的 Set 调用 Get/Set → 显性失败。

---

## 验收清单

- [x] `CombatComponentBase` / `AttributeSet` / `AttributeData` 就位，无业务属性名
- [x] 值存在 Set 侧；Get/Set/OnChange 可测
- [ ] Edit Mode 全绿；Console 无编译错误（待 Unity Test Runner）
- [x] `FEATURE_REGISTRY` / `ACTIVE_WORK` 同步
