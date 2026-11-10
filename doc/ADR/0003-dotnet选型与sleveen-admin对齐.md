# ADR-0003：.NET 选型对齐 sleveen-admin（mooSQL / MAF / Hangfire / JWT）

- **状态**：已接受  
- **日期**：2026-11-10  
- **关联**：[`doc/.net选型/00-选型总览.md`](../.net选型/00-选型总览.md) · [`00-全局总任务纲领.md`](../00-全局总任务纲领.md) · 参考仓 **sleveen-admin**

## 背景

在完成 WeKnora Go 功能摸底后，需钉死 .NET 实现栈。团队已有可运行的 **sleveen-admin**（mooSQL、Hangfire.MooSql、MAF、JWT + Permission + ApiResult）。早期草案中的 EF Core、自研 asynq 兼容队列、平台层复刻 WeKnora 鉴权等，与现有资产重复且增加分叉成本。

## 决策

1. **整体后端范式参考 sleveen-admin**（分层、DI 三特性源生成、配置分片、ApiResult、JWT Challenge 行为等）。  
2. **ORM = mooSQL.Ext**；表结构优先执行 WeKnora `src/sync/migrations`，mooSQL 负责访问。  
3. **AI 基建 = MAF（Microsoft.Agents.AI）**；RAG/工具在其上扩展。  
4. **定时与后台任务 = Hangfire**（Hangfire.MooSql 复用主库）。  
5. **基础权限、统一接口包装、JWT = 与 sleveen-admin 一致**；WeKnora API Key/Embed/MCP 等为产品扩展，不替代平台登录主路径。  
6. 产品 SSE / 知识库等对 `src/sync/frontend` 的兼容通过适配层处理，**不**要求平台 JWT/ApiResult 字节级复刻 Go WeKnora。

## 后果

**正面**：复用已验证基建，降低权限/ORM/任务/AI 脚手架成本；与团队其它产品技术栈统一。  
**代价**：与 WeKnora 原生鉴权/响应形存在差异，前端登录与部分管理 API 需适配或分端面；后台任务从 asynq 语义迁到 Hangfire，运维面板与任务名映射需单独设计。

## 后续动作

- [x] 写入 `doc/.net选型/`  
- [x] 回写纲领 §4.2 / 根 README 选型摘要  
- [ ] 搭建 Host 时按 admin 范式接入 Jwt / ApiResult / mooSQL / Hangfire / MAF 最小切片  
- [ ] 在 `02-API契约` 中列出平台层 vs 产品层兼容差异
