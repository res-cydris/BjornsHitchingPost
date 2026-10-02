# Building Bjørn's Hitching Post

Requires .NET SDK 8, Valheim 1.0.26 installed locally, and extracted official dependencies. Game files are read only; no build step installs the mod.

Download and extract:

- https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/5.4.2351/ into `deps/BepInEx`
- https://thunderstore.io/package/download/ValheimModding/Jotunn/2.30.2/ into `deps/Jotunn`

Run in PowerShell from this source folder:

```powershell
./Build.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\Valheim' -DependenciesDir 'C:\path\to\deps'
./Validate.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\Valheim' -DependenciesDir 'C:\path\to\deps'
```

Build.ps1 compiles the plugin, runs geometry tests, updates `package/plugins/BjornsHitchingPost/BjornsHitchingPost.dll`, and creates the Thunderstore ZIP one directory above the source folder. Validate.ps1 checks game API metadata and package contents without loading Valheim or installing anything.

All source is original to this package. Dependency and game DLLs are deliberately omitted from the distribution. The generated post reuses vanilla materials at runtime; the user-supplied icon in assets/icon-original.png is resized by Make-Icon.ps1.

The build currently reports two MSB3277 warnings for game dependency references to System.IO.Compression and System.Net.Http. There are no compiler errors. This plugin does not use either API, and its external member references resolve against the installed assemblies. The warnings are recorded rather than suppressed.

The package name uses ASCII for Thunderstore compatibility; the display name is Bjørn's Hitching Post. Publishing team/author is selected on Thunderstore at upload time and is not invented in the manifest. This is the 1.0.2 release, following user-confirmed in-game testing of the release candidate. The server must install it. Modded clients must match 1.0.2; vanilla clients may join. Animals pause without a nearby compatible simulator.


