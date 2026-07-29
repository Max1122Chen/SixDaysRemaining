# SixDaysRemaining（项目协作说明）

这个仓库用于承载 `SixDaysRemaining` 的 Unity 游戏项目与协作文档。
本文档面向**设计师**与**技术协作者**，用于快速理解项目结构、协作方式与入口位置。

## 项目结构（根目录）

- `SixDaysRemaining/`：Unity 工程目录（可直接用 Unity Hub 打开）
  - `Assets/`：游戏资源、场景、脚本（后续在 `Assets/Scripts/` 下按域拆分）
  - `Packages/`：Unity 包依赖
  - `ProjectSettings/`：Unity 项目设置
- `docs/ai/`：项目协作与工程决策文档（设计-实现工作流）
  - `ROADMAP.md`：技术设计大纲与开发路线图（实现主计划）
  - `PROJECT_CONTEXT.md`：项目目标、阶段、协作约定、验证基线
  - `ACTIVE_WORK.md`：当前任务队列
  - `FEATURE_REGISTRY.md`：功能注册表（先注册再实现）
  - `PROGRESS_LOG.md`：进度日志（追加式记录）
  - `TECH_DEBT.md`：技术债登记
  - `designs/`：设计说明（Design Spec）
  - `plans/`：实现计划（Implementation Plan）
  - `templates/`：文档模板
- `docs/designs/`：产品设计文档（面向策划/设计）
  - `六日英雄—技术演示文档.pdf`：当前玩法设计源
- `.cursor/rules/`：面向 Cursor Agent 的项目规则（代码风格/文档流程/协作边界）
- `.gitignore`：Unity 与 IDE 生成文件忽略规则

## 适用对象与使用建议

- 设计师：
  - 先看 `docs/ai/PROJECT_CONTEXT.md` 和 `docs/ai/designs/`
  - 关注 `ACTIVE_WORK.md` 了解当前实现节奏与下一步
- 技术协作者（程序/TA/技术策划）：
  - 开发前先看 `ROADMAP.md`、`FEATURE_REGISTRY.md` 与对应 `plans/`
  - 过程记录统一写入 `PROGRESS_LOG.md`
  - 临时方案或待清理项登记到 `TECH_DEBT.md`

## 协作原则（当前）

- 文档、进度记录、提交信息：中文优先
- 代码注释：中文表达；点名函数/变量/类型时可直接使用英文标识符
- 命名约定：
  - 类型、属性（property）使用 `PascalCase`
  - 除属性外，字段与变量使用 `camelCase`
- 全局系统命名（`Manager` / `SubSystem`）按系统性质选择，由需求讨论后确定

## 打开与验证

- Unity 版本：`2022.3.62f3c1`
- 打开方式：用 Unity Hub 打开 `SixDaysRemaining/`
- 基线检查：
  - Console 无编译错误
  - `SixDaysRemaining/Assets/Scenes/SampleScene.unity` 可进入 Play Mode

## 当前阶段

项目处于初始化阶段，已完成：
- 仓库初始化与远端绑定
- Unity 忽略规则配置
- 协作文档体系与 Cursor 规则建立

当前状态：`ROADMAP.md` 技术架构规划待审阅 commit；通过后按 feat 纪律推进 `CORE-F02` → `feat/gameplay-framework`。
