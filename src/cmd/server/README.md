# SailNora.App（骨架宿主）

## 运行

```powershell
cd src\cmd\server
dotnet run -f net10.0 --urls http://127.0.0.1:5080
```

- Swagger: `http://127.0.0.1:5080/swagger`
- Health: `GET /health`
- 登录: `POST /api/auth/login`（或 `/api/v1/auth/login`）body `{"userName":"admin","password":"admin123"}` → JWT 含 `tid`/`srole`
- 空间: `GET /api/spaces`；创建 API Key: `POST /api/spaces/{id}/api-keys`
- Capabilities: `GET /api/system/capabilities`（兼 `/api/v1/system/capabilities`）
- 权限探针: `GET /api/probe/secure`（Bearer / `X-API-Key`；可用 `X-Tenant-ID` 切空间）
- AI 流式: `POST /api/ai/chat` body `{"prompt":"..."}`（需 Bearer；默认 Placeholder SSE）
- 知识库: `POST /api/v1/knowledge-bases`；上传 `POST /api/v1/knowledge-bases/{id}/knowledge/file`；分块 `GET /api/v1/chunks/{knowledgeId}`
- 会话/RAG: `POST /api/v1/sessions`；`GET /api/v1/sessions/{id}/messages`；`POST /api/v1/knowledge-chat/{sessionId}`（SSE：load_history/references/answer/complete）
- 检索: `POST /api/v1/knowledge-search`（关键词 + embedding 混合分 + lexical rerank）
- Embedding: `Ai:EmbeddingProvider=hashing|openai`（openai 需 ApiKey / EmbeddingModel）
- DocReader: gRPC 客户端已接 proto；`GET /api/v1/system/docreader/info`（默认 Enabled=false，解析回退本地文本）
- Postgres 迁移: `src/scripts/migrate-postgres.ps1`（见 `src/sync/migrations/README.sail-nora.md`）

## 配置

`Configuration/` 分片：`Jwt` · `DBCoonfig`（默认 SQLite `App_Data/sailnora.db`）· `Ai`（默认 Placeholder；`Provider=OpenAI` + `ApiKey` 接真实模型）· `Hangfire`（默认 **Enabled=false**，避免当前 SQLite 方言下 Counter 查询告警；接 Postgres/MSSQL 或方言修复后再开）。

## 依赖仓

- `moo-sqlm/mooSQL2024`（ProjectReference）
- `hangfire.moo-sql`（ProjectReference）
