# 与 AI 协作方式（Working With AI）

最后更新：2026-07-29

## 会话开始提示

建议提示：

```text
继续这个仓库。先阅读 docs/ai/PROJECT_CONTEXT.md、docs/ai/PROGRESS_LOG.md 和 docs/ai/ACTIVE_WORK.md。概括当前状态，并给出下一个最小可验证步骤。
```

## 会话结束提示

建议提示：

```text
请把今天的工作追加到 docs/ai/PROGRESS_LOG.md，并给出下一次会话的第一步行动。
```

## 工作习惯

- 对于实质性新工作：在进行大段代码改动前先注册 Feature ID。
- 对于玩法或架构决策：创建或更新 设计说明（Design Spec）与实现计划（Implementation Plan）。
- 在其他任务中发现横跨多个模块的缺陷：先写 bug 记录，再做针对性修复。
- 交接：更新进度日志，并明确标出未完成 slice 的状态。
- 提交相关：先准备提交草案；只有在你明确批准后才执行。

## 设计源规则

- 产品设计真相可以在这个目录之外；但在开始大规模编码前，必须把实现计划提炼并落到 `docs/ai/designs/` 和 `docs/ai/plans/` 里。
