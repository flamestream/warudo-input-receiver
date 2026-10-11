# Warudo Modding Architecture & Build Pipeline

## Overview
Warudo is a Unity-based VTubing / 3D interaction system. Modding in Warudo uses **UMod** (a Unity-based modding framework) to compile C# source scripts and bundle assets into a `.warudo` archive.

---

## 1. Runtime Environment vs. SDK Environment

### Warudo Game Runtime (`<Warudo>/Warudo_Data/Managed/`)
* **Execution**: Runs under Unity Mono.
* **Core Assemblies**:
  * `Warudo.Core.dll` — Core graph engine, nodes, server communication, action serialization.
  * `Warudo.Plugins.Core.dll` — Core character controller, assets, expression models, blendshapes.
  * `Animancer.dll` (`Version=7.4.2.25`) — Full Animancer Pro runtime library.
  * `websocket-sharp.dll` — Real-time WebSocket communications.
  * `UniTask.dll`, `DOTween.dll`, `VRCSDK*.dll`, etc.
* **Assembly Resolution**:
  * When a `.warudo` mod is loaded at runtime, Warudo resolves dependencies against the assemblies loaded in its own AppDomain (`Warudo_Data/Managed/`).
  * **Strict Assembly Identity**: If a mod's compiled IL contains an `AssemblyReference` to an assembly that does not exist in the game directory (e.g. `Animancer.Lite`), loading fails immediately with `System.IO.FileNotFoundException`.

---

## 2. The UMod Build Pipeline

### How `.warudo` Packages Are Built
When clicking **Warudo -> Build Mod** (or using `UMod.ModTools.Export`):
1. **Source Script Compilation** (`UMod.BuildEngine.Build.SourceScriptCompiler:CompileRuntimeScripts`):
   * Locates all `.cs` files inside the mod directory (e.g., `Assets/Test` or `Assets/FS Input Receiver`).
   * Passes them to Roslyn C# Compiler (`csc.exe`).
   * Passes assembly references `-r:` from:
     * Unity Engine modules (`UnityEngine.CoreModule.dll`, etc.)
     * `Library/ScriptAssemblies/` (`Animancer.dll`, `Warudo.Plugins.Core.dll`, `Warudo.Core.dll`, etc.)
     * Plugin DLLs in packages (`DOTween.dll`, `UMod.dll`, `websocket-sharp.dll`, etc.)
   * Outputs compiled assemblies into a temporary folder.
2. **Asset Bundle Packaging**:
   * Packages prefabs, materials, textures, and assets into Unity AssetBundles.
3. **Archive Creation**:
   * Creates a ZIP archive with the `.warudo` extension containing:
     * `modinfo.dat` — JSON metadata (Mod ID, name, version, author, target Warudo version).
     * `assemblymodules.dat` — Serialized binary metadata mapping assemblies and types.
     * Content bundles / compiled DLLs.

---

## 3. Package Management & `app.warudo.modtool`

* **Embedded Package**:
  * The SDK tool is located at `Packages/app.warudo.modtool` (referenced in `Packages/manifest.json` as `"app.warudo.modtool": "file:app.warudo.modtool"`).
  * Unlike read-only packages in `Library/PackageCache`, embedded packages are directly editable and tracked in project version control.
* **Assembly Definitions (`.asmdef`)**:
  * `Packages/app.warudo.modtool/Packages/Animancer/Animancer.asmdef` (GUID: `4c25c05f410a3a447a75c3b0909152ef`)
  * `Packages/app.warudo.modtool/Warudo/Scripts/Warudo/Plugins/Core/Warudo.Plugins.Core.asmdef`
  * Changing `.asmdef` files or package references requires Unity to reload the domain and regenerate `.csproj` / `Library/ScriptAssemblies/` DLLs.
