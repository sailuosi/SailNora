# `src/` — 源码根

本目录是 sail-nora **唯一源码根**。

| 分区 | 路径 | 说明 |
|------|------|------|
| **同步区** | [`sync/`](./sync/) | 上游无需改造资产（frontend、docreader、migrations…），只读同步 |
| **.NET 解决方案** | [`SailNora.sln`](./SailNora.sln) | `cmd/server` Host + `internal/{foundation,platform,product}` |
| **重写区** | `cmd/` · `internal/` · `cli/` · `client/` | C# 重写；路径对齐上游同名目录 |
| **适配区** | `docker/` · `deploy/` · `helm/` | 部署拓扑，后续改为挂 .NET app |
| **评估区** | `third_party/` · `misc/` · `tests/` | 按需同步或移植 |
| **本仓脚本** | `scripts/` | 如 `sync-upstream.ps1` |
| **版本戳** | [`UPSTREAM_VERSION`](./UPSTREAM_VERSION) | 最近一次同步记录 |

对照与决策：[`doc/ADR/0002`](../doc/ADR/0002-src目录与上游对齐.md)。

## 同步

```powershell
cd src\scripts
.\sync-upstream.ps1 -UpstreamRoot '<本地 WeKnora 根目录>'
```

目标固定为 `src/sync/<同名目录>`；**不会**覆盖重写区。
