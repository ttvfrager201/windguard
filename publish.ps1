$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $root 'publish'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory -Path $out | Out-Null

dotnet publish "$root\WinGuardAgent\WinGuardAgent.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $out
dotnet publish "$root\WinGuardAdmin\WinGuardAdmin.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $out

Write-Host "Published service/CLI: $out\WinGuardAgent.exe"
Write-Host "Published admin dashboard: $out\WinGuardAdmin.exe"
