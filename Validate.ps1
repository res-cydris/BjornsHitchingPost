param(
 [string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\Valheim',
 [Parameter(Mandatory=$true)][string]$DependenciesDir
)
$ErrorActionPreference='Stop'
$deps=(Resolve-Path -LiteralPath $DependenciesDir).Path
Add-Type -Path "$deps\BepInEx\BepInExPack_Valheim\BepInEx\core\Mono.Cecil.dll"
$resolver=[Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory("$GameDir\valheim_Data\Managed")
$resolver.AddSearchDirectory("$deps\Jotunn\plugins")
$resolver.AddSearchDirectory("$deps\BepInEx\BepInExPack_Valheim\BepInEx\core")
$options=[Mono.Cecil.ReaderParameters]::new()
$options.AssemblyResolver=$resolver
$game=[Mono.Cecil.AssemblyDefinition]::ReadAssembly("$GameDir\valheim_Data\Managed\assembly_valheim.dll",$options)
$plugin=[Mono.Cecil.AssemblyDefinition]::ReadAssembly("$PSScriptRoot\package\plugins\BjornsHitchingPost\BjornsHitchingPost.dll",$options)
$contracts=@{
 'Character'=@('Awake','RPC_Damage','CustomFixedUpdate','Damage');
 'Projectile'=@('IsValidTarget');
 'Growup'=@('GrowUpdate');
 'PrivateArea'=@('IsEnabled','IsInside','IsPermitted');
 'ZNet'=@('Awake','OnNewConnection','Disconnect');
 'ZNetView'=@('Awake','GetPrefabName');
 'ZDO'=@('Deserialize','SetOwner');
 'ZDOMan'=@('Load','LoadChunks','RPC_ZDOData');
 'Player'=@('TryPlacePiece','TakeInput','Update');
 'Piece'=@('DropResources');
 'ZRoutedRpc'=@('HandleRoutedRPC');
 'ZNetScene'=@('CreateObject');
 'WearNTear'=@('RPC_Remove','RPC_Damage')
}
$checks=0
foreach($entry in $contracts.GetEnumerator()) {
 $type=$game.MainModule.Types | Where-Object Name -EQ $entry.Key
 foreach($method in $entry.Value) {
   if (@($type.Methods | Where-Object Name -EQ $method).Count -ne 1) { throw "Missing or ambiguous patch/reflection target $($entry.Key).$method" }
   $checks++
 }
}
foreach($pair in @(@('ZNetScene','m_instances'),@('SE_Harpooned','m_attacker'),@('Projectile','m_owner'),@('Projectile','m_statusEffectHash'),@('PrivateArea','m_allAreas'),@('ZDOMan','m_objectsByID'))) {
 $type=$game.MainModule.Types | Where-Object Name -EQ $pair[0]
 if (-not ($type.Fields | Where-Object Name -EQ $pair[1])) { throw "Missing field $pair" }; $checks++
}
$members=0
foreach($reference in $plugin.MainModule.GetMemberReferences()) {
 if($reference.DeclaringType.Scope.Name -match '^(assembly_|UnityEngine|Jotunn|BepInEx|0Harmony)') {
   if(-not $reference.Resolve()) { throw "Unresolved external member $reference" }; $members++
 }
}
"PASS: $checks game patch/reflection contracts; $members external member references resolved."
function Check-Patches($types) {
 foreach($type in $types) {
   $attribute=$type.CustomAttributes | Where-Object {$_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch'} | Select-Object -First 1
   if($attribute -and $attribute.ConstructorArguments.Count -eq 2) {
     $targetType=$attribute.ConstructorArguments[0].Value.Resolve()
     $target=@($targetType.Methods | Where-Object Name -EQ $attribute.ConstructorArguments[1].Value)
     if($target.Count -ne 1) {throw "Ambiguous Harmony target $($type.FullName)"}
     foreach($patch in $type.Methods | Where-Object {$_.Name -in @('Prefix','Postfix','Finalizer')}) {
       foreach($parameter in $patch.Parameters) {
         $name=$parameter.Name
         if($name.StartsWith('___')) {
           if(-not ($targetType.Fields | Where-Object Name -EQ $name.Substring(3))) {throw "Missing injected field $name"}
         } elseif(-not $name.StartsWith('__')) {
           if(-not ($target[0].Parameters | Where-Object Name -EQ $name)) {throw "Missing original parameter $name in $($type.FullName)"}
         }
       }
     }
   }
   Check-Patches $type.NestedTypes
 }
}
Check-Patches $plugin.MainModule.Types
$pluginType=$plugin.MainModule.Types | Where-Object FullName -EQ 'BjornsHitchingPost.Plugin'
$compat=$pluginType.CustomAttributes | Where-Object {$_.AttributeType.Name -eq 'NetworkCompatibilityAttribute'}
$enumType=$compat.ConstructorArguments[0].Type.Resolve()
$serverOnly=($enumType.Fields | Where-Object Name -EQ 'ServerMustHaveMod').Constant
if($compat.ConstructorArguments[0].Value -ne $serverOnly) {throw 'Client installation is not optional'}
"PASS: Harmony parameter bindings; compatibility requires server only."
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing
$zip=[System.IO.Compression.ZipFile]::OpenRead("$PSScriptRoot\..\BjornsHitchingPost-1.0.2.zip")
try {
 $names=@($zip.Entries | ForEach-Object {$_.FullName.Replace('\','/')})
 foreach($name in @('LICENSE','manifest.json','README.md','icon.png','plugins/BjornsHitchingPost/BjornsHitchingPost.dll')) {
   if($name -cnotin $names) { throw "Missing ZIP entry $name" }
 }
 if(@($names | Where-Object {$_ -like '*.dll'}).Count -ne 1) { throw 'Unexpected redistributed DLLs' }
 $reader=[System.IO.StreamReader]::new($zip.GetEntry('manifest.json').Open())
 try {$manifest=$reader.ReadToEnd() | ConvertFrom-Json} finally {$reader.Dispose()}
 if($manifest.version_number -ne '1.0.2' -or $plugin.Name.Version.ToString() -ne '1.0.2.0') {throw 'Package/assembly version mismatch'}
 $packed=$zip.Entries | Where-Object {$_.FullName.Replace('\','/') -eq 'plugins/BjornsHitchingPost/BjornsHitchingPost.dll'}
 $stream=$packed.Open(); $hash=[System.Security.Cryptography.SHA256]::Create()
 try { $packedHash=[BitConverter]::ToString($hash.ComputeHash($stream)).Replace('-','') } finally {$hash.Dispose();$stream.Dispose()}
 if($packedHash -ne (Get-FileHash "$PSScriptRoot\package\plugins\BjornsHitchingPost\BjornsHitchingPost.dll").Hash) {throw 'ZIP has stale plugin'}
 if($manifest.name -notmatch '^[A-Za-z0-9_]{1,128}$' -or $manifest.description.Length -gt 250 -or $manifest.version_number -notmatch '^\d+\.\d+\.\d+$') {throw 'Invalid manifest'}
 $icon=[System.Drawing.Image]::FromFile("$PSScriptRoot\package\icon.png")
 try { if($icon.Width -ne 256 -or $icon.Height -ne 256) {throw 'Invalid icon dimensions'} } finally {$icon.Dispose()}
 "PASS: ZIP structure, manifest, 256x256 PNG, and plugin-only binary contents."
 "Plugin assembly: $($plugin.Name.FullName)"
 "ZIP SHA256: $((Get-FileHash "$PSScriptRoot\..\BjornsHitchingPost-1.0.2.zip").Hash)"
} finally {$zip.Dispose(); $game.Dispose(); $plugin.Dispose()}
