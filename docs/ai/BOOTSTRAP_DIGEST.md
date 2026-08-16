# 启动摘要（Bootstrap Digest）

最后更新：2026-08-16  
目的：在 2 分钟内恢复协作上下文。

## 新会话的读取顺序

1. `PROJECT_CONTEXT.md`
2. `ACTIVE_WORK.md`
3. `FEATURE_REGISTRY.md`（In Progress / Planned / Discuss / Review）
4. `PROGRESS_LOG.md`（只看最近条目）
5. `TECH_DEBT.md`（Open）
6. 相关 `designs/`；产品源在 `docs/designs/`

## 当前快照（2026-08-16）

- `main`：Persist/Meta/Save Done；事件 3.0 深度 + COMB-F10 为 **Review（半开放，他人验收）**；**COMB-F11** 卡牌数值 2.0 进行中/刚同步。
- 内容编辑：`StreamingAssets/Shelter/`、`StreamingAssets/Events/`、`StreamingAssets/Combat/`（改完需重启 Play）。
- **下一批：** COMB-F11 收口；半开放 feat 由他人 Play 后标 Done。

## 不可协商的协作规则

- 只从可信计划源规划；不要从旧路线图快照里推断 backlog。
- 新 Feature：先注册 ID，再 design，审阅后再实现。
- **未经明确要求不要新建分支**；优先大域分支（如 `feat/combat`、`feat/shelter`）。
- “prepare commit”只代表草案；只有明确指令后才 `git commit` / push。

## ID 规则

- Feature：`<DOMAIN>-F<nn>`
- Slice：`<FeatureID>-S<nn>`
- Bug：`BUG-<DOMAIN>-<nnn>`
- ADR：`ADR-<yyyyMMdd>-<nn>`

## 域代码

- `CORE` / `SHLT` / `COMB` / `UI` / `EVT` / `Debug` / `END` / `META` / `SAVE`
