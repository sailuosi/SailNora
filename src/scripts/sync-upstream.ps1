<#
.SYNOPSIS
  将 WeKnora 上游「无需改造」目录镜像到 src/sync/ 同名路径。

.DESCRIPTION
  只同步 SYNC 清单；不会写入 cmd/、internal/、cli/、client/ 等重写目录。
  详见 doc/ADR/0001、ADR-0002 与 src/sync/README.md。

.PARAMETER UpstreamRoot
  本地 WeKnora 仓库根目录。

.EXAMPLE
  .\sync-upstream.ps1 -UpstreamRoot 'C:\sleveen\code\github\ai\WeKnora-main\WeKnora-main'
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
  [Parameter(Mandatory = $true)]
  [string] $UpstreamRoot,

  [string[]] $Include = @(
    "frontend",
    "docreader",
    "mcp-server",
    "migrations",
    "config",
    "dataset",
    "examples",
    "testdata",
    "licenses",
    "docs",
    "website-docs",
    "Formula",
    "packages",
    "miniprogram"
  ),

  [switch] $SkipVersionStamp
)

$ErrorActionPreference = "Stop"

$srcRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$syncRoot = Join-Path $srcRoot "sync"
$upstream = Resolve-Path $UpstreamRoot

if (-not (Test-Path (Join-Path $upstream "frontend"))) {
  throw "UpstreamRoot 不像 WeKnora 根目录（未找到 frontend）: $upstream"
}

$rewriteOnly = @("cmd", "internal", "cli", "client", "scripts", "sync")
foreach ($name in $Include) {
  if ($rewriteOnly -contains $name) {
    throw "拒绝同步该名称到 sync: $name"
  }
}

New-Item -ItemType Directory -Force -Path $syncRoot | Out-Null

foreach ($name in $Include) {
  $from = Join-Path $upstream $name
  $to = Join-Path $syncRoot $name
  if (-not (Test-Path $from)) {
    Write-Warning "上游缺少目录，跳过: $name"
    continue
  }
  if ($PSCmdlet.ShouldProcess($to, "Mirror from $from")) {
    New-Item -ItemType Directory -Force -Path $to | Out-Null
    & robocopy $from $to /MIR `
      /XD .git node_modules dist .venv __pycache__ .pytest_cache .uv coverage .turbo `
      /XF *.pyc `
      /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    $rc = $LASTEXITCODE
    if ($rc -ge 8) {
      throw "robocopy 失败 code=$rc  path=$name"
    }
    Write-Host "synced: sync/$name (rc=$rc)"
  }
}

if (-not $SkipVersionStamp) {
  $commit = ""
  $tag = ""
  Push-Location $upstream
  try {
    $commit = (git rev-parse HEAD 2>$null)
    $tag = (git describe --tags --exact-match 2>$null)
  } catch { }
  finally {
    Pop-Location
  }

  $stampPath = Join-Path $srcRoot "UPSTREAM_VERSION"
  $lines = @(
    "# Sail Nora 上游对照记录（每次同步后更新）",
    "# upstream: https://github.com/Tencent/WeKnora",
    "product_version=0.9.0",
    "git_tag=$tag",
    "git_commit=$commit",
    "synced_at=$((Get-Date).ToString('yyyy-MM-ddTHH:mm:ssK'))",
    "synced_by=$env:USERNAME",
    "source_path=$upstream",
    "sync_root=src/sync",
    "include=$($Include -join ',')",
    "notes=sync-upstream.ps1 -> src/sync"
  )
  Set-Content -Path $stampPath -Value $lines -Encoding UTF8
  Write-Host "updated: UPSTREAM_VERSION"
}

Write-Host "done."
