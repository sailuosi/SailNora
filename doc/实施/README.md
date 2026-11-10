# 实施文档

本目录将纲领与选型落地为**可执行的实施计划**，分两大段：

| 文档 | 主题 | 目标 |
|------|------|------|
| [01-骨架复刻适配.md](./01-骨架复刻适配.md) | 从 sleveen-admin **复刻并适配**后端骨架 | 可启动 Host：JWT / ApiResult / mooSQL / Hangfire / MAF 冒烟 |
| [02-对标功能实现.md](./02-对标功能实现.md) | 对标 WeKnora **产品功能**实现 | RAG → Agent → 生态，门禁与差异表 |

**阅读顺序**：选型 [`../.net选型/00-选型总览.md`](../.net选型/00-选型总览.md) → 本文 01 → 02 → 纲领阶段门禁。

**依赖仓（本地同级）**

| 仓 | 路径（本机惯例） | 用途 |
|----|------------------|------|
| sleveen-admin | `C:\sleveen\code\gitee\sleveen-admin` | 骨架范式来源 |
| mooSQL2024 | `C:\sleveen\code\gitee\moo-sqlm\mooSQL2024` | ORM ProjectReference |
| hangfire.moo-sql | `C:\sleveen\code\gitee\hangfire.moo-sql` | Hangfire 存储 |
| WeKnora | `C:\sleveen\code\github\ai\WeKnora-main\WeKnora-main` | 功能与契约对照 |
