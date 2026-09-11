[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

function Invoke-Native([string]$command, [string[]]$arguments)
{
    & $command @arguments
    if ($LASTEXITCODE -ne 0)
    {
        throw "$command failed with exit code $LASTEXITCODE."
    }
}
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$artifacts = Join-Path $root 'artifacts'
$portable = Join-Path $artifacts 'portable\TheThirdAlien'
$installerOutput = Join-Path $artifacts 'installer'

Remove-Item -LiteralPath $artifacts -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $portable, $installerOutput | Out-Null

Invoke-Native dotnet @("restore", "ThirdAlien.slnx", "--runtime", "win-x64")
Invoke-Native dotnet @("build", "ThirdAlien.slnx", "--configuration", "Release", "--no-restore")
Invoke-Native dotnet @("test", "ThirdAlien.slnx", "--configuration", "Release", "--no-build")
Invoke-Native dotnet @("publish", "src/ThirdAlien.App/ThirdAlien.App.csproj", "--configuration", "Release", "--runtime", "win-x64", "--self-contained", "true", "--no-restore", "--output", $portable)

$portableZip = Join-Path $artifacts "TheThirdAlien-$Version-win-x64-portable.zip"
Compress-Archive -Path (Join-Path $portable '*') -DestinationPath $portableZip

$compiler = @(
    (Get-Command iscc.exe -ErrorAction SilentlyContinue).Source,
    (Get-Command iscc -ErrorAction SilentlyContinue).Source,
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1

if ($compiler) {
    Invoke-Native $compiler @("/DMyAppVersion=$Version", "/O$installerOutput", 'installer\TheThirdAlien.iss')
    Write-Host "Installer: $(Join-Path $installerOutput "TheThirdAlien-Setup-$Version-win-x64.exe")"
}
else {
    Write-Warning 'Portable ZIP built. Install Inno Setup 6, then rerun this script to build the installer EXE.'
}

Write-Host "Portable ZIP: $portableZip"