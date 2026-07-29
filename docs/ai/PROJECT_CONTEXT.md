# 项目背景（Project Context）

最后更新：2026-07-29

## 1) 项目目标

一句话使命：
- 做一个小而完整的 Unity GameJam 原型：有清晰核心循环，并且能在“一局/一轮”层面体验顺畅、可交付。

主要成功标准：
- 玩家能启动游戏、理解目标，并完成至少一轮完整可玩的流程。
- 核心的“逐帧交互体验”足够稳定，能支撑 GameJam 的快速迭代。

## 2) 架构方向

总体架构概述：
- Unity 2022.3 的 2D 项目，从一个最小场景开始，当前不引入自定义玩法脚本。
- 原型阶段优先使用简单的 MonoBehaviour 驱动玩法。
- 系统尽量保持小而明确：按职责拆分，便于迭代中替换或调整。

核心模块（Core modules）：
- `Gameplay` - 规则、状态流转、胜负条件、进度循环
- `Player` - 输入、移动、交互、反馈
- `World` - 可交互物、危险区域、触发器、场景对象
- `UI` - HUD、提示、菜单、游戏状态反馈
- `Bootstrap` - 场景启动的装配/连线与全局引用

## 3) 当前阶段

当前阶段：
- 前置准备：初始化工作流、约定与设计输入（design intake）。

下一里程碑：
- 把设计文档转成一个注册过的 Feature，并为首个可玩循环写基于 slice 的实现计划（Implementation Plan）。

## 4) 协作约定

- 计划真相（Planning truth）：`ACTIVE_WORK.md`、`FEATURE_REGISTRY.md`、`TECH_DEBT.md`、最近的 `PROGRESS_LOG.md`
- 交付门槛（Delivery bar）：遵循 `templates/DOC_GOVERNANCE.md` 与 Slice DoD
- 会话恢复（Session recovery）：使用 `BOOTSTRAP_DIGEST.md`
- 协作语言偏好：文档/提交信息用中文；代码标识符可保持英文；代码注释与文档内容以中文为主。

## 5) 验证基线

- 构建/验证命令：用 Unity `2022.3.62f3c1` 打开 `SixDaysRemaining/`，并确认 Console 没有编译错误。
- 测试命令：在 `SixDaysRemaining/Assets/Scenes/SampleScene.unity` 进入 Play Mode，并核对当前任务的人工检查清单。

## 6) 可选双轨文档模式

选择一种模式：
- 双轨：外部或产品视角的设计真相可以放在 `docs/ai/` 之外；该目录主要存放实现工作流与提炼后的工程决策。

