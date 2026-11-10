# SailNora.App（骨架宿主）

## 运行

```powershell
cd src\cmd\server
dotnet run -f net10.0 --urls http://127.0.0.1:5080
```

- Swagger: `http://127.0.0.1:5080/swagger`
- Health: `GET /health`
- 登录: `POST /api/auth/login` body `{"userName":"admin","password":"admin123"}`
- 权限探针: `GET /api/probe/secure`（Bearer Token）

## 配置

`Configuration/` 分片：`Jwt` · `DBCoonfig`（默认 SQLite `App_Data/sailnora.db`）· `Hangfire`（默认 **Enabled=false**，避免当前 SQLite 方言下 Counter 查询告警；接 Postgres/MSSQL 或方言修复后再开）。

## 依赖仓

- `moo-sqlm/mooSQL2024`（ProjectReference）
- `hangfire.moo-sql`（ProjectReference）
