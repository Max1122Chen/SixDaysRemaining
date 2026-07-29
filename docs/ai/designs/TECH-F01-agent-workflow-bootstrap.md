# TECH-F01 Unity 工作流引导（Agent Workflow Bootstrap）

## 元信息
- **ID:** `TECH-F01`
- **类型:** `Feature`
- **状态:** `Done`
- **负责人:** `Max`
- **最后更新：** `2026-07-29`
- **相关：** `[Feature 注册表](../FEATURE_REGISTRY.md)`, `[实现计划](../plans/TECH-F01-agent-workflow-bootstrap-plan.md)`

## TL;DR
把 `min-agent-workflows` 的核心结构适配到该 Unity 项目，并以 Cursor 作为活跃 adapter。
目标是在玩法代码开始膨胀之前，先建立一个可靠的规划循环。
这会包含：可信文档、ID 规则、工作流触发点，以及 Unity/C# 代码约定。

## 范围

- **范围 In：**
  - `docs/ai/` 协作骨架
  - `.cursor/rules/`（Cursor 的 adapter 规则）
  - bootstrap 与首个可玩规划所需的初始 Feature 注册
- **范围 Out：**
  - 暂未使用的 Git hooks 或其他 agent adapter
  - 具体玩法实现本身

## 现状、目标与差距

- 当前现状：该仓库几乎是一个空的 Unity 项目，没有自定义工作流文档与代码约定。
- 目标状态：让后续 agent 会话能稳定引导到“小、可文档化、可验证”的工作。
- 缺口：缺少显式结构会让 GameJam 的迭代速度变快，但事后难以恢复/复盘/审查。

## 设计

### 方案 A（推荐）
- 描述：保留模板核心 + Cursor 规则，并为 Unity 的手工流程定制语言与验证方式。
- 好处：仪式感低、启动快，适配早期原型迭代。
- 风险：在自定义脚本与测试出现之前，部分验证仍会偏手动。

### 方案 B
- 描述：立刻把所有可用 adapter 和模板文件都搬进来。
- 为什么不选：在项目还未需要时，会带来额外维护成本。

## 实现注意点

- 受影响的关键模块：本 slice 仅涉及文档
- 旧路径的迁移/删除计划：目前不适用
- 兼容性假设：Unity `2022.3.62f3c1`，C# 脚本会放到 `Assets` 下面

## 验证

- 构建/验证命令：检查新建文档与规则是否符合预期结构
- 测试命令：bootstrap prompt 能基于新文档概括当前状态
- 人工核对：确认文件确实放在 `docs/ai/` 和 `.cursor/rules/` 下

## 验收清单

- [x] 范围已完成
- [x] 验证通过
- [x] 进度日志已更新
- [x] Feature 注册表状态已同步
