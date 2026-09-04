---
name: warudo-plugin-dev
description: >-
  Develop, verify, sync, test, and debug Warudo plugins and mods in FS Input Receiver.
  Use when adding or modifying nodes or assets, syncing code to Warudo Playground or Unity SDK project,
  testing hot-reload, building mod packages, or diagnosing runtime exceptions and IK animation issues.
---

# Warudo Plugin & Mod Development Runbook

This skill provides step-by-step procedures, build validation, synchronization workflows, and critical runtime gotchas for developing the **FS Input Receiver** plugin for **Warudo**.

---

## 1. Quick Directory & Assembly Reference

* **Source Code**: [`src/`](file:///d:/Code/FS%20Input%20Receiver/src/) (TargetFramework `.NET Framework 4.8`, C# 8.0/9.0)
* **Playground Target**: `E:\SteamLibrary\steamapps\common\Warudo\Warudo_Data\StreamingAssets\Playground\FS Input Receiver`
* **Unity SDK Target**: `D:\Unity Projects\Warudo SDK 0.14.5.1 Modding Project\Assets\FSInputReceiver`
* **Pre-shipped Managed DLLs**: `E:\SteamLibrary\steamapps\common\Warudo\Warudo_Data\Managed/` (400 assemblies including `Warudo.Core.dll`, `Warudo.Plugins.Core.dll`, `Animancer.dll` Pro v7.4.2.25)
* **Deep Knowledge Base**: [`.ai/`](file:///d:/Code/FS%20Input%20Receiver/.ai/) (architecture, gotchas, troubleshooting)

---

## 2. Standard Development Workflows

### Workflow A: Rapid Iteration via Playground Hot-Reload (Recommended)

Warudo compiles `.cs` files live from its `Playground/` folder using Roslyn. **Never test without compiling locally first**—if Roslyn encounters compile errors, Warudo silently continues running stale code without UI warnings.

```powershell
# 1. Type-check and compile plugin locally against Warudo Managed DLLs:
.\personal-tools\verify-build.ps1

# 2. If compilation succeeds, push to Warudo Playground:
.\personal-tools\sync.ps1 src warudo

# Or execute both in a single step:
.\personal-tools\verify-build.ps1 -Sync
```

#### Verifying Playground Load
Check the runtime log for hot-reload confirmation or silent errors:
* Log path: `%USERPROFILE%\AppData\LocalLow\HakuyaLabs\Warudo\Player.log`
* Search for: `[HotReloadScriptMonitor]` or `error CS`

---

### Workflow B: Production Mod Packaging via Unity SDK

When preparing a standalone `.warudo` mod file for distribution:

#### Option 1: Headless CLI Build (Fastest, Unity Closed)
```powershell
# Automatically syncs src -> unity, checks for Unity lockfile, and builds headlessly:
.\personal-tools\build-mod.ps1

# Or skip re-syncing if already synced:
.\personal-tools\build-mod.ps1 -SkipSync
```
*(Note: If Unity Editor is open, this script will detect the lock and abort with a helpful message).*

#### Option 2: Unity Editor GUI (When Unity is Already Open)
1. In Unity Editor (`D:\Unity Projects\Warudo SDK 0.14.5.1 Modding Project`), press **`Ctrl + Shift + B`** (or top menu: **Warudo -> Build Mod**).
2. The output `.warudo` mod package is generated in:
   `E:\SteamLibrary\steamapps\common\Warudo\Warudo_Data\StreamingAssets\Plugins/`

---

### Workflow C: Pulling Changes Back to Git Repository

If you made modifications or tweaks inside Unity:
```powershell
# Pull changes from Unity to src, excluding Unity's auto-generated .meta files:
.\personal-tools\sync.ps1 unity src -NoMeta
```

If you made modifications directly inside Warudo Playground:
```powershell
# Pull changes from Warudo Playground to src:
.\personal-tools\sync.ps1 warudo src
```

---

## 3. Pre-Flight Architecture & Runtime Gotchas Checklist

Before creating new classes or committing changes, verify against these non-obvious runtime behaviors. For detailed root cause analysis, consult [`.ai/warudo_runtime_gotchas.md`](file:///d:/Code/FS%20Input%20Receiver/.ai/warudo_runtime_gotchas.md).

### 1. `StructuredData<TParent>` in Arrays / Lists
* **Gotcha**: Warudo's JSON deserializer does **NOT** populate `Parent` on elements inside arrays (`T[]`) or lists (`List<T>`). Accessing `Parent` inside array items causes runtime `NullReferenceException`.
* **Requirement**: Always implement an explicit re-parenting pass:
  ```csharp
  public void RefreshKeyBindingsParent() {
      if (KeyBindings == null) return;
      for (int i = 0; i < KeyBindings.Length; i++) {
          if (KeyBindings[i] != null) KeyBindings[i].Parent = this;
      }
  }
  ```
  Call this in `OnCreate()`, inside `Watch(nameof(Collection), ...)` handlers, and guard before accessing modal operations.

### 2. `[DataInput]` and `[Hidden]` Only Apply to Fields
* **Gotcha**: Attributes in `Warudo.Core` have `[AttributeUsage(AttributeTargets.Field)]`. Putting them on C# properties (`get; set;`) fails compilation (`CS0592`).
* **Requirement**: Inputs must be public fields. Forwarding properties for backward compatibility must be plain C# properties **without** attributes.

### 3. Fatal Avatar Masking Crash (`CreateAvatarMask`)
* **Gotcha**: Any `OverlappingAnimationData` layer with `Masked = true` **MUST** have a non-null `MaskedBodyParts` array. Warudo's internal `CharacterAsset.SetupOverlappingAnimations()` loops over `layer.GetMaskedBodyParts()` without a null check; a null array crashes the Unity game loop.
* **Requirement**: Always initialize with a non-null array:
  ```csharp
  new OverlappingAnimationData {
      Masked = true,
      MaskedBodyParts = new[] { AnimationMaskedBodyPart.LeftFingers } // Never null!
  };
  ```

### 4. Top-Level Enum Declaration Requirement
* **Gotcha**: Nested enums produce CLR reflection names with `+` (`Class+Enum`). Warudo's type resolver cannot find nested enums, printing `[Error] Enum type ... does not exist` and rendering blank dropdowns.
* **Requirement**: All enums used in `[DataInput]` or `StructuredData` **MUST** be defined directly at namespace scope.

### 5. Humanoid Limb IK Orientation Standards
* **Gotcha**: Setting IK target rotation to `(0, 0, 0)` leaves humanoid hands pointing sideways in A/T-pose resting angle.
* **Standards**: Facing flat and forward toward a desk/keyboard/controller:
  * **Left Hand**: `Vector3(0f, 90f, 0f)`
  * **Right Hand**: `Vector3(0f, -90f, 0f)`

### 6. Thread Safety in UDP Receivers (`ReceiverAsset`)
* **Gotcha**: Background `Task.Run(() => Listen(udpClient))` cannot touch Unity objects (`Transform`, `GameObject`, `Time.time`). Doing so throws `UnityException` and aborts packet reception.
* **Requirement**: The background listener thread must **only** write raw string packets to `protected string lastState`. Parsing, math, and scene manipulation must occur on the main thread in `OnUpdate()`.

### 7. Playground Mode vs. Packaged Plugin (uMod) Mode
* **Gotcha**: Modifying `Character.OverlappingAnimations` array directly works in Playground, but desynchronizes the WebSocket serializer in packaged uMod mode.
* **Requirement**: Always use [`Helper.SetCharacterOverlappingAnimations(character, layers)`](file:///d:/Code/FS%20Input%20Receiver/src/Libs/Helper.cs) which branches via [`Helper.IsPlaygroundMode`](file:///d:/Code/FS%20Input%20Receiver/src/Libs/Helper.cs).

### 8. `uDesktopDuplication` Dependency in Unity SDK Projects
* **Gotcha**: `DrawingScreenAsset.cs` and `PointerReceiverAsset.cs` depend on `uDesktopDuplication.Monitor`. While pre-installed in Warudo runtime, a fresh Unity SDK project will fail with 6 `CS0246` errors unless `com.hecomi.udesktopduplication` is registered in `Packages/manifest.json`.
* **Requirement**: Ensure `"com.hecomi.udesktopduplication": "file:com.hecomi.udesktopduplication"` exists in `Packages/manifest.json`.

### 9. UMod Code Security Validation Rules (No Reflection)
* **Gotcha**: UMod scans mod bytecode during export and fails the build (`ModBuildException: Code security validation failed`) if any method references `System.Reflection`, `System.Reflection.Emit`, `Vexe.Fast.Reflection`, `System.IO.*`, `System.Diagnostics.Process`, `System.AppDomain`, or P/Invoke.
* **Requirement**: Never use reflection to inspect assemblies or types in mod code. For runtime mode detection, check `plugin.ModHost == null` (which is allowed by UMod) rather than querying `Assembly.Location` or `Assembly.GetName()`.

---

## 4. Troubleshooting & Diagnostic Runbook

### Log File Locations
* **Warudo Runtime Logs**:
  * Primary: `%USERPROFILE%\AppData\LocalLow\HakuyaLabs\Warudo\Player.log`
  * Alternative: `%USERPROFILE%\AppData\LocalLow\Warudo\Warudo\Player.log`
* **Unity Editor & UMod Build Logs**:
  * `%LOCALAPPDATA%\Unity\Editor\Editor.log`

### Common Log Error Signatures
* `[Error] Enum type ... does not exist`: Nested enum used in `[DataInput]`. Move to namespace level.
* `NullReferenceException: ... SetupOverlappingAnimations`: Missing `MaskedBodyParts` on `Masked = true` layer.
* `NullReferenceException: Object reference not set` inside `StructuredData`: Uninitialized `Parent` reference in array/list.
* `UnityException: get_transform can only be called from the main thread`: Unity API called from UDP background task.
* Code changes not taking effect in Playground: Roslyn compile error occurred. Check `Player.log` for `[HotReloadScriptMonitor]`.

---

## 5. Key Reference Documents
* [`.ai/warudo_runtime_gotchas.md`](file:///d:/Code/FS%20Input%20Receiver/.ai/warudo_runtime_gotchas.md) — Comprehensive explanation of runtime quirks and root causes.
* [`.ai/plugin_development_cheatsheet.md`](file:///d:/Code/FS%20Input%20Receiver/.ai/plugin_development_cheatsheet.md) — Asset, Node, and Plugin boilerplate templates.
* [`.ai/animancer_integration_guide.md`](file:///d:/Code/FS%20Input%20Receiver/.ai/animancer_integration_guide.md) — Animancer Pro integration rules.
* [`personal-tools/UNIFIED-SYNC-README.md`](file:///d:/Code/FS%20Input%20Receiver/personal-tools/UNIFIED-SYNC-README.md) — Sync script commands and options.
