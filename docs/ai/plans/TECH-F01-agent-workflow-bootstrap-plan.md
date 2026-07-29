# TECH-F01 工作流引导 实现计划

## Meta
- **ID:** `TECH-F01`
- **状态:** `Done`
- **负责人:** `Max`
- **最后更新：** `2026-07-29`
- **相关：** `[设计说明](../designs/TECH-F01-agent-workflow-bootstrap.md)`

## 目标
在开始玩法编码之前，先建立一个最小但可用的工作流基线。

## 切片计划
| 切片 ID | 摘要 | 依赖 | 验证方式 | 状态 |
|----------|------|------|----------|------|
| `TECH-F01-S01` | 创建 docs 骨架与 Cursor 规则集 | None | 检查生成文件的结构与范围 | Done |
| `TECH-F01-S02` | 注册首个“玩法规划”相关 Feature | `TECH-F01-S01` | 更新 `FEATURE_REGISTRY.md` 与 `ACTIVE_WORK.md` | Done |

## 风险与缓解
- 风险：文档可能会与真实设计源发生偏差。
- 缓解：在进入大规模玩法编码前，把设计输入落到 `CORE-F01`。

## Blocked/Deferred 说明（如适用）
- 原因：N/A
- 影响：N/A
- 解除阻塞条件：N/A
- 下次检查日期：N/A
