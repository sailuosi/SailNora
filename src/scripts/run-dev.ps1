<#
.SYNOPSIS
  本地启动 SailNora.App（默认 http://127.0.0.1:5080）。
#>
param(
  [string] $Framework = "net10.0",
  [string] $Urls = "http://127.0.0.1:5080"
)

$ErrorActionPreference = "Stop"
$server = Resolve-Path (Join-Path $PSScriptRoot "..\cmd\server")
Push-Location $server
try {
  Write-Host "run SailNora.App -f $Framework --urls $Urls"
  dotnet run -f $Framework --urls $Urls
}
finally {
  Pop-Location
}
