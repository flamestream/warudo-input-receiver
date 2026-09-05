# Debugging & Troubleshooting Warudo Mods

## 1. Where to Find Build & Runtime Logs

### Unity Editor & UMod Export Logs
* Location: `%LOCALAPPDATA%\Unity\Editor\Editor.log`
* UMod prints build events and compiler errors to this log with:
  ```
  [UMod.BuildPipeline.BuildLog]: Compile failed!
  [UMod.BuildPipeline.BuildLog]: [Error]: (): Assets\YourMod\Script.cs(line,col): error CSXXXX: ...
  ```
* To view recent build errors via PowerShell:
  ```powershell
  Get-Content -Tail 100 -Path "$env:LOCALAPPDATA\Unity\Editor\Editor.log" | Select-String "error CS", "BuildLog"
  ```

### Warudo Game Runtime Logs
* Locations:
  * `%USERPROFILE%\AppData\LocalLow\HakuyaLabs\Warudo\Player.log` (Primary)
  * `%USERPROFILE%\AppData\LocalLow\Warudo\Warudo\Player.log` (Alternative)
* When a mod crashes or fails to load, check this log for reflection errors, missing types, or missing assembly references.

### Common Runtime Error Signatures
See [warudo_runtime_gotchas.md](file:///d:/Code/FS%20Input%20Receiver/.ai/warudo_runtime_gotchas.md) for full context and fixes:
* `Enum type ... does not exist`: Caused by declaring an enum inside a class instead of at namespace level.
* `NullReferenceException: ... SetupOverlappingAnimations`: Caused by `Masked = true` on `OverlappingAnimationData` with `MaskedBodyParts == null`.
* `NullReferenceException: Object reference not set ...` inside a `StructuredData<TParent>` item: Caused by Warudo failing to inject `Parent` for array/list elements (run `RefreshKeyBindingsParent()`).
* `UnityException: get_transform can only be called from the main thread`: Caused by calling Unity APIs inside background UDP listener thread (`Listen()`).
* Code changes not reflecting during Playground testing: Roslyn encountered a compilation error and silently kept running the previous stale assembly in memory. Check `Player.log` for `[HotReloadScriptMonitor]` or `error CS`.

---

## 2. Inspecting Exported `.warudo` Packages

A `.warudo` mod file is a standard ZIP archive. You can inspect its contents directly:

### Extracting & Checking References Script
```powershell
# Extract the exported mod archive:
Expand-Archive -Path "C:\Path\To\yourmod.warudo" -DestinationPath ".\extracted_mod" -Force

# Inspect mod metadata:
Get-Content ".\extracted_mod\modinfo.dat" | ConvertFrom-Json
```

### Inspecting Assembly References with Mono.Cecil
Warudo SDK includes Mono.Cecil at `Packages/app.warudo.modtool/Packages/RoslynCSharp/Plugin/Trivial.Mono.Cecil.dll`.
You can check which assemblies your compiled mod depends on before distributing it:
```powershell
[System.Reflection.Assembly]::LoadFrom('Packages\app.warudo.modtool\Packages\RoslynCSharp\Plugin\Trivial.Mono.Cecil.dll') | Out-Null
$ad = [Trivial.Mono.Cecil.AssemblyDefinition]::ReadAssembly('path_to_mod_assembly.dll')

Write-Host "Assembly: $($ad.FullName)"
Write-Host "References:"
foreach ($r in $ad.MainModule.AssemblyReferences) {
    Write-Host "  - $($r.FullName)"
}
```
* **Verify**: Ensure **no** references to `Animancer.Lite` exist! All Animancer references must point to `Animancer, Version=7.4.2.25`.

---

## 3. Fast Verification & MSBuild Command Line

To test compilation of the project or packages without opening/switching Unity:

```powershell
# Rebuild Animancer.asmdef package:
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' 'Animancer.csproj' -t:Build

# Rebuild Warudo Plugins Core:
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' 'Warudo.Plugins.Core.csproj' -t:Build
```
Both projects should output:
```
0 Warning(s)
0 Error(s)
```

---

## 4. Inspecting Active Evaluated State via Saved Scene JSON

### The Principle
When debugging runtime values, data input ports, transform states, or `StructuredData` parameters (such as scale multipliers, offsets, or toggles), speculating about in-memory states or attempting to infer values from UI behavior often leads to false assumptions. 

Warudo's entire scene graph is serialized as human-readable JSON when saved. Instead of guessing, **ask the user to save the scene (`Ctrl+S`)** in Warudo. This triggers the web server API endpoint:
```
PUT /api/openedScene/save: "200 OK"
```

### Locating & Inspecting the Scene File
1. **Scene File Location**: Warudo scene files are stored in:
   - `%USERPROFILE%\AppData\LocalLow\HakuyaLabs\Warudo\Scenes\*.warudo` (or `StreamingAssets/Scenes/`)
   - Alternatively, check `Player.log` for the exact file path logged during scene open or save operations.
2. **Inspecting Data Input & StructuredData Values**:
   - Use `Select-String` or `jq` in PowerShell to inspect your specific asset ID or property names:
   ```powershell
   Get-Content -Path "$env:APPDATA\..\LocalLow\HakuyaLabs\Warudo\Scenes\*.json" | Select-String "LeftHandRestTransform", "Scale"
   ```
3. **Benefits**:
   - Confirms whether a port was deserialized as `(0, 0, 0)` due to missing fields in older scene versions.
   - Verifies whether UI sliders/vectors actually updated the backend port in `DataInputPortCollection`.
   - Distinguishes between code calculation bugs versus unexpected input configurations with 100% certainty.

