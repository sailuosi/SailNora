# `src/sync/` — 上游无需改造资产（只读同步区）

本目录存放与 WeKnora **保持完全一致、本仓不改造** 的源码与资源。由手动拷贝或 `src/scripts/sync-upstream.ps1` 更新；版本见 [`../UPSTREAM_VERSION`](../UPSTREAM_VERSION)。

## 本轮已同步

| 目录 | 说明 |
|------|------|
| `frontend/` | Vue 3 控制台 |
| `docreader/` | Python gRPC 文档解析 |
| `mcp-server/` | 独立 MCP（上游已弃用，兼容保留） |
| `migrations/` | Postgres / SQLite / MySQL 等 SQL |
| `config/` | config.yaml、prompt 等 |
| `dataset/` · `examples/` · `testdata/` | 样例与测试资产 |
| `licenses/` · `docs/` · `website-docs/` | 许可与上游文档资源 |
| `Formula/` · `packages/` · `miniprogram/` | 生态周边（原样保留） |

## 不在此目录

| 路径 | 原因 |
|------|------|
| `src/cmd` · `src/internal` · `src/cli` · `src/client` | **C# 重写区** |
| `src/docker` · `deploy` · `helm` | 需改为挂 .NET app，属适配而非只读同步 |
| `src/third_party` | 评估后同步或替换 |
| `src/scripts` | 含本仓同步脚本 |

**约定**：业务改动禁止直接改 `src/sync/**`；契约问题只在 .NET 重写侧消化。
