# 文档治理（Document Governance）

最后更新：2026-07-29
状态：Active

## 1) 目的

确保任何贡献者（人类或 agent）都能回答：
1. 正在解决什么问题？
2. 已完成 vs 仍待完成的内容是什么？
3. 下一步是什么“可验证”的行动？
4. 为什么工作被暂停、延后或取消？

## 2) 文档类型

- Roadmap：排期与优先级
- Design Spec：范围、方案与风险
- Implementation Plan：slice 划分与验收检查
- ADR：架构权衡决策
- Progress Log：事实时间线
- Bug Record：缺陷生命周期与回归安全

## 3) ID 约定

- Feature：`<DOMAIN>-F<nn>`
- Slice：`<FeatureID>-S<nn>`
- Bug：`BUG-<DOMAIN>-<nnn>`
- ADR：`ADR-<yyyyMMdd>-<nn>`

所有新的 Feature 在开始实现工作前，必须先登记到 `docs/ai/FEATURE_REGISTRY.md`。

## 4) 长文档必须包含的 Meta 块

Design / Roadmap / Implementation / ADR 文件应包含：

```markdown
## Meta
- **ID:** <FeatureID or N/A>
- **Status:** Draft | Planned | In Progress | Review | Done | Blocked | Deferred | Cancelled | Snapshot | Archived | Reference
- **Owner:** <name>
- **Last updated:** YYYY-MM-DD
- **Related:** [link1](./...), [link2](./...)
```

## 5) Agent 文档可信分层

计划来源（Planning sources）：
- `ACTIVE_WORK.md`
- `FEATURE_REGISTRY.md`（In Progress / Planned）
- `TECH_DEBT.md`（Open）
- 最近的 `PROGRESS_LOG.md`
- code/tests/verify 脚本

参考来源（Reference-only）：
- 旧路线图
- Snapshot / Archived / Reference 文档
- 陈旧且未核对的 checklist 片段

当文档与代码/tests/验证输出冲突时，以 code/tests/验证结果为准。

## 6) Slice Done Definition（DoD）

### 文档 DoD
- 追加 progress entry
- Feature 与 slice 状态同步
- 当范围或状态发生变化时，更新 Design/Plan
- 对有意义的架构权衡更新 ADR
- 对缺陷修复更新 bug record

### 工程 DoD
- 执行并记录验证命令
- 工作期间不应发现未记录的阻塞性缺陷
- 公共 API 的变更要反映到调用方，或明确写进文档

## 7) 交接要求（Handoff requirements）

当进行交接/会话切换时：
1. 若工作跨多个步骤，创建或更新一条会话说明。
2. 追加一条 progress entry。
3. 把未完成的 slice 标为 Blocked/Deferred，并给出原因与解除阻塞条件。
4. 提供下一次会话的“第一步具体行动”。

## 8) 工作边界（Work boundary）

完成一个有意义的批次后：
- 完成 DoD 更新
- 提出“prepare commit”
- 除非你明确要求，否则不要开始不相关的新 Feature 实现
