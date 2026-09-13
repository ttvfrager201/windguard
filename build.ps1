$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $root
try {
  dotnet restore .\WinGuardAgent.sln
  dotnet build .\WinGuardAgent.sln -c Release --no-restore
} finally { Pop-Location }
