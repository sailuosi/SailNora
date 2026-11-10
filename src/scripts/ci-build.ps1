<#
.SYNOPSIS
  本地/CI：多 TFM 构建 SailNora（要求与 moo-sqlm、hangfire.moo-sql 同级）。
#>
param(
  [string[]] $Frameworks = @("net8.0", "net10.0"),
  [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$src = Resolve-Path (Join-Path $PSScriptRoot "..")
$gitee = Resolve-Path (Join-Path $src "..\..")

$moo = Join-Path $gitee "moo-sqlm\mooSQL2024\ext\mooSQL.Ext.csproj"
$hf = Join-Path $gitee "hangfire.moo-sql\src\Hangfire.MooSql\Hangfire.MooSql.csproj"
if (-not (Test-Path $moo)) { throw "缺少 mooSQL: $moo" }
if (-not (Test-Path $hf)) { throw "缺少 Hangfire.MooSql: $hf" }

Push-Location $src
try {
  foreach ($tfm in $Frameworks) {
    Write-Host "=== build $tfm ==="
    dotnet build SailNora.sln -c $Configuration -f $tfm --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "build failed: $tfm" }
  }
  Write-Host "ci-build ok"
}
finally {
  Pop-Location
}
