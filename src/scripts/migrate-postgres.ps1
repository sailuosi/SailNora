<#
.SYNOPSIS
  将 src/sync/migrations/versioned 应用到 PostgreSQL。
.EXAMPLE
  $env:SAILNORA_PG = "Host=127.0.0.1;Port=5432;Database=weknora;Username=postgres;Password=..."
  .\src\scripts\migrate-postgres.ps1
  .\src\scripts\migrate-postgres.ps1 -DryRun
#>
param(
  [string] $Connection = $env:SAILNORA_PG,
  [string] $To = "",
  [switch] $DryRun
)

$ErrorActionPreference = "Stop"
$migrateProj = Resolve-Path (Join-Path $PSScriptRoot "..\cmd\migrate\SailNora.Migrate.csproj")
$args = @("--project", $migrateProj, "-f", "net10.0", "--")
if ($Connection) { $args += @("--connection", $Connection) }
if ($To) { $args += @("--to", $To) }
if ($DryRun) { $args += "--dry-run" }

dotnet run @args
if ($LASTEXITCODE -ne 0) { throw "migrate failed: $LASTEXITCODE" }
