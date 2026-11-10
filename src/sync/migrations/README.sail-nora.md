# sail-nora 迁移说明

本目录与上游 WeKnora `migrations` 同步，**面向 PostgreSQL**（含 pgvector / 业务表）。

## 本地开发（当前默认）

`SailNora.App` 默认使用 **SQLite** + mooSQL DDL（平台表 `Sys*`、产品精简表 `Kn*`）。  
**不会**自动执行本目录 SQL。

## 对齐上游 schema

1. 准备 Postgres，连接串示例：  
   `Host=127.0.0.1;Port=5432;Database=weknora;Username=postgres;Password=***`
2. 运行：

```powershell
$env:SAILNORA_PG = "Host=...;Database=...;Username=...;Password=..."
.\src\scripts\migrate-postgres.ps1
# 或 dry-run
.\src\scripts\migrate-postgres.ps1 -DryRun
```

工具：`src/cmd/migrate`（表 `sailnora_schema_migrations` 记录已应用版本）。

## 注意

- 部分 upstream 脚本依赖扩展（如 `vector`、`pg_search`），需在目标库先安装。
- 平台 JWT/空间表与 WeKnora `tenants` 仍是双轨；业务表对齐后需再做 ID/租户映射适配。
