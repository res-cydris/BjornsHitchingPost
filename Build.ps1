param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim',
    [Parameter(Mandatory=$true)][string]$DependenciesDir
)
$ErrorActionPreference='Stop'
# DependenciesDir contains extracted official Thunderstore packages:
# BepInEx/BepInExPack_Valheim/BepInEx/core and Jotunn/plugins.
# This script never installs or copies anything into the game directory.
$env:APPDATA = Join-Path $PSScriptRoot '.build\appdata'
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.build\dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$dependencies = (Resolve-Path -LiteralPath $DependenciesDir).Path
& (Join-Path $PSScriptRoot 'Make-Icon.ps1')
dotnet restore "$PSScriptRoot\src\BjornsHitchingPost.csproj" --configfile "$PSScriptRoot\NuGet.Config" --packages "$PSScriptRoot\.build\packages"
if ($LASTEXITCODE) { throw 'Restore failed' }
dotnet build "$PSScriptRoot\src\BjornsHitchingPost.csproj" -c Release --no-restore "-p:GameDir=$GameDir" "-p:DependenciesDir=$dependencies"
if ($LASTEXITCODE) { throw 'Build failed' }
dotnet restore "$PSScriptRoot\tests\Tests.csproj" --configfile "$PSScriptRoot\NuGet.Config" --packages "$PSScriptRoot\.build\packages"
if ($LASTEXITCODE) { throw 'Test restore failed' }
dotnet run --project "$PSScriptRoot\tests\Tests.csproj" -c Release --no-restore
if ($LASTEXITCODE) { throw 'Tests failed' }
New-Item -ItemType Directory -Force "$PSScriptRoot\package\plugins\BjornsHitchingPost" | Out-Null
Copy-Item "$PSScriptRoot\src\bin\Release\netstandard2.1\BjornsHitchingPost.dll" "$PSScriptRoot\package\plugins\BjornsHitchingPost\BjornsHitchingPost.dll"
Compress-Archive -Path "$PSScriptRoot\package\*" -DestinationPath "$PSScriptRoot\..\BjornsHitchingPost-1.0.2.zip" -Force
Copy-Item "$PSScriptRoot\..\BjornsHitchingPost-1.0.2.zip" "$PSScriptRoot\..\Bjørn's Hitching Post-1.0.2.zip" -Force

