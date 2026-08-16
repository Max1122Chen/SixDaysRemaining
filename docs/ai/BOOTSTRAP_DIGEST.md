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

- 真相分支：**本地 `main`**（已含全部已实现 feat；历史 `feat/*` 无未合入提交）。
- 内容：事件 3.0 + 卡牌 2.0 已进 StreamingAssets；多 feat **Review（半开放）** 待他人 Play。
- 总览：根目录 `README.md`（实现对齐摘要）。
- **下一批：** 半开放 Play 签字；可选 TECH 导出 / TD 清理。

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
