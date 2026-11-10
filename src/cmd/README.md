# `cmd/` — C# 重写（对齐上游 Go 入口）

| 子目录 | 上游 | sail-nora |
|--------|------|-----------|
| `server/` | `cmd/server` 主 API 进程 | ASP.NET Core Host（多 TFM） |
| `desktop/` | Wails 桌面 | 后期评估 |
| `milvus-migrate/` · `download/` | 运维工具 | 按需移植 |

勿将上游 Go 源码同步至此。
