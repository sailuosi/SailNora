# Sail Nora 文档

本目录存放 **sail-nora**（WeKnora 全功能 .NET 版）工程文档。项目意图与路线图见仓库根目录 [`README.md`](../README.md)。

## 文档索引

| 文档 | 说明 | 状态 |
|------|------|------|
| [00-全局总任务纲领.md](./00-全局总任务纲领.md) | 总目标、范围边界、兼容契约、阶段路线图 | 生效 v0.6 |
| [.net选型/00-选型总览.md](./.net选型/00-选型总览.md) | .NET 栈与 sleveen-admin 对齐 | 生效 |
| [实施/README.md](./实施/README.md) | 实施总入口 | 生效 |
| [实施/01-骨架复刻适配.md](./实施/01-骨架复刻适配.md) | 复刻 admin 骨架 | 生效 |
| [实施/02-对标功能实现.md](./实施/02-对标功能实现.md) | WeKnora 功能波次 F1–F7 | 生效 |
| 01-架构设计.md | .NET 分层与核心时序 | 待写 |
| 02-API契约与兼容性.md | 平台层 vs 产品层契约 | 待写 |
| 03-模块移植映射.md | Go → C# 映射表 | 待写 |
| 04-数据与迁移.md | Schema / 队列 / 存储 | 待写 |
| 05-开发与部署.md | 本地开发与 Compose | 待写 |
| 06-里程碑与验收.md | 阶段门禁与差异表 | 待写 |
| [ADR/0001-tfm与上游同步.md](./ADR/0001-tfm与上游同步.md) | TFM 多目标与上游同步方式 | 已接受 |
| [ADR/0002-src目录与上游对齐.md](./ADR/0002-src目录与上游对齐.md) | `src/` 与上游 1:1 切分 | 已接受 |
| [ADR/0003-dotnet选型与sleveen-admin对齐.md](./ADR/0003-dotnet选型与sleveen-admin对齐.md) | mooSQL / MAF / Hangfire / JWT | 已接受 |

## 已确认工程决策

- **TFM**：`net8.0` · `net10.0` · `net11.0`（多目标 + CI 矩阵）
- **上游资产同步**：手动或脚本；暂不 git submodule
- **源码布局**：全部在 `src/`；无需改造资产在 [`src/sync/`](../src/sync/)
- **后端栈**：对齐 **sleveen-admin** — mooSQL · Hangfire · MAF · JWT/`ApiResult`/`Permission`

## 上游对照

- 项目：腾讯 [WeKnora](https://github.com/Tencent/WeKnora)（MIT）
- 基线版本：v0.9.0
- 工程导读：上游 `docs/architecture.md`
- 产品文档：上游 `website-docs/` / [官网文档](https://weknora.weixin.qq.com/docs/)

## 阅读顺序

1. **00-全局总任务纲领**（范围与硬约束）
2. **.net选型**（栈与 admin 对齐）
3. **实施/01 → 02**（骨架复刻 → 对标功能）
4. 架构 / 契约 / 映射分册（实现时补齐）
