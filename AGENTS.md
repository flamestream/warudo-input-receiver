# AGENTS.md — AI Assistant Guide for FS Input Receiver

> **For detailed architecture, runtime gotchas, and troubleshooting, read the [`.ai/` knowledge base](.ai/README.md) first.**
>
> **CRITICAL DIRECTIVE — Inconsistency Verification & Pushback**:
> If you notice an inconsistency or potential conflict in any statement or requirement that can be verified against active code, scene files, or architectural rules, **do not attempt to blindly accommodate it or force it to work**. Push back with verifiable evidence, explain the conflict clearly, and ask for clarification.

---

## 1. Repository Overview

**FS Input Receiver** is a C# Warudo plugin providing real-time tracking and animation for pen tablets, mice, keyboards, and game controllers via UDP streams from **FS Input Device Emitter**.

- **Language & Runtime**: C# 8.0, `.NET Framework 4.8` (Unity Mono/Roslyn)
- **Core Dependencies**: `Warudo.Core.dll`, `UnityEngine.dll`, `Animancer.dll`, `DOTween.dll`, `UniTask.dll`, `Newtonsoft.Json.dll`
- **Available Assemblies**: `Warudo_Data/Managed/` (~400 DLLs)

```
FS Input Receiver/
├── src/                        # Plugin C# source code
│   ├── PointerReceiverPlugin.cs  # [PluginType] entrypoint — register Assets & Nodes here
│   ├── Assets/                 # ReceiverAsset subclasses
│   ├── Nodes/                  # Blueprint Node classes
│   ├── Libs/                   # Helpers (VKCode, Helper.cs, Guide*, etc.)
│   ├── Localizations/          # en.json, ja.json, ...
│   └── PlaygroundPlugin.csproj / FSInputReceiver.sln
├── personal-tools/             # Sync & build tooling
│   ├── sync.bat / sync.ps1     # Robocopy mirror: src ↔ warudo | unity | clear warudo
│   ├── verify-build.ps1        # dotnet build check (use -Sync to also push)
│   └── build-mod.ps1           # Headless Unity mod package builder
├── .ai/                        # Knowledge base (read before making changes)
├── docs/                       # Architecture deep-dives
└── wiki/                       # User guides
```

---

## 2. Development Workflow

Warudo Playground hot-reloads `.cs` files from `Warudo_Data/StreamingAssets/Playground/` without packaging.

```powershell
dotnet build src/PlaygroundPlugin.csproj          # Compile-check only
.\personal-tools\verify-build.ps1                 # Compile-check with summary
.\personal-tools\verify-build.ps1 -Sync           # Compile-check + push to Warudo Playground
.\personal-tools\sync.bat src warudo              # Manual push to Warudo
.\personal-tools\sync.bat warudo src              # Manual pull from Warudo
.\personal-tools\sync.bat clear warudo            # Remove all plugin files from Playground
.\personal-tools\sync.bat src unity               # Sync to Unity SDK project (before mod build)
```

**Building a `.warudo` mod package:** Sync to Unity SDK (`sync.bat src unity`), then open `D:\Unity Projects\Warudo SDK 0.14.5.1 Modding Project` → **Warudo SDK → Build Mod**.

---

## 3. Critical Rules (Quick Reference)

These are the most commonly violated rules. Full explanations are in the `.ai/` files.

### 3.1 Plugin Registration
New `Asset` or `Node` classes **must** be registered in [`PointerReceiverPlugin.cs`](src/PointerReceiverPlugin.cs) under `AssetTypes`/`NodeTypes` in the `[PluginType]` attribute.

### 3.2 Thread Safety (UDP Receivers)
Never touch Unity objects or Warudo services on the `Listen()` background thread. Buffer raw packets in `protected string lastState` and decode on the main thread in `OnUpdate()`. → [Gotcha #9](.ai/warudo_runtime_gotchas.md)

### 3.3 Localization
Never hardcode user-facing strings. Define keys in `src/Localizations/en.json` and call `"KEY".Localized()`. → [Plugin Cheatsheet](.ai/plugin_development_cheatsheet.md)

### 3.4 Compatibility
Never rename existing node IDs or `[DataInput]` port field names — this breaks saved user scenes.

### 3.5 Enums in DataInput / StructuredData
Enums used as `[DataInput]` ports or inside `StructuredData` **must** be top-level namespace types. Nested enums (`class Foo { enum Bar }`) produce `Foo+Bar` CLR names that Warudo's type resolver cannot find. → [Gotcha #6](.ai/warudo_runtime_gotchas.md)

### 3.6 StructuredData Field Assignment
Always use `SetDataInput(nameof(field), value, true)` — never assign C# fields directly without it, as direct assignment bypasses Warudo's port collection and loses values on reload. Never set fallback values per-frame in `OnUpdate()`. → [Gotcha #27](.ai/warudo_runtime_gotchas.md)

### 3.7 Overlapping Animations (Playground vs Mod)
Always use [`Helper.SetCharacterOverlappingAnimations(character, layers)`](src/Libs/Helper.cs) — never call `SetDataInput` or assign `character.OverlappingAnimations` directly outside this helper.
- **Playground**: Overlapping animations cannot be activated in Playground mode (concurrent UMod loading races with graph updates, crashing Warudo to desktop). `Helper.SetCharacterOverlappingAnimations` simply displays a warning toast and safely no-ops.
- **Mod**: `SetDataInput` (required so `WatchAll` fires and Animancer layers are created).
→ [Gotcha #40](.ai/warudo_runtime_gotchas.md)

### 3.8 Avatar Masking
Any `OverlappingAnimationData` with `Masked = true` **must** have a non-null `MaskedBodyParts` array or `CreateAvatarMask()` throws a `NullReferenceException` that crashes Warudo. → [Gotcha #5](.ai/warudo_runtime_gotchas.md)

### 3.9 Scene Teardown
Never call `Scene.RemoveAsset()` inside `OnDestroy()` — Warudo enumerates its asset list during teardown; modifying it causes `InvalidOperationException: Collection was modified`. → [Gotcha #10](.ai/warudo_runtime_gotchas.md)

### 3.10 UMod Security (`System.Reflection` banned)
UMod's security scanner blocks all `System.Reflection` usage in packaged mods. The codebase contains zero reflection calls (`SetPortDefaultValue` was removed as default port values cannot be mutated without reflection). If Playground-only reflection is ever needed, wrap it in `// [PLAYGROUND_ONLY_START]` and `// [PLAYGROUND_ONLY_END]`. `personal-tools/sync.ps1` automatically comments these blocks out (`// [OMIT_MOD] ...`) when syncing to `unity`, passing UMod's scanner cleanly. → [Gotcha #41](.ai/warudo_runtime_gotchas.md)

### 3.11 Humanoid Hand IK Orientation
Straight-forward facing: Left hand `Vector3(0, 90, 0)`, Right hand `Vector3(0, -90, 0)`. `(0, 0, 0)` points hands sideways (T-pose). Never use raw hand anchor axes for palm normals; use rest-relative deltas. → [Gotcha #12, #39](.ai/warudo_runtime_gotchas.md)

### 3.12 Section Attributes & Dynamic UI
Only the **first** member of a section gets `[Section("NAME")]`. For dynamic section titles/button labels, bump `uiVersion` and call `BroadcastDataInput` after `await UniTask.Yield()` — never use `BroadcastOpenedScene()`. → [Gotcha #33, #34](.ai/warudo_runtime_gotchas.md)

### 3.13 Asset Auto-Migration
Use the two-phase pipeline in `ReceiverAsset.Deserialize()`: `OnPreMigrateAsset` (rename/remove raw port keys before deserialization) → `base.Deserialize()` → `OnPostMigrateAsset` (heal typed C# objects via `SetDataInput`). → [Gotcha #29](.ai/warudo_runtime_gotchas.md)

### 3.14 Key Origins Scaling & Natural Hand Reach Alignment
- Avatar bones do not scale with props/keyboards. Never scale wrist reach deltas from floor/rig origin `(0, 0, 0)`.
- Use key-relative reach compensation: shift the wrist target by the key's scale expansion `(scale - 1) * keyLocalPos` so the reaching offset between key cap and wrist is 100% scale-invariant. → [Gotcha #42](.ai/warudo_runtime_gotchas.md)
- Always solve hand reach orientations against live world/rig transformed keys (considering the live resting elbow line) and store relative to cluster via `InverseTransformFromRig`. → [Gotcha #43](.ai/warudo_runtime_gotchas.md)

### 3.15 Warudo Attachable Architecture & Scale-Free Reparenting
- `AnchorAsset` GameObjects are parented under an internal unscaled `"Attachable Wrapper"` (`scale = (1, 1, 1)`). Never call `GameObject.transform.SetParent(newParent)` directly — this decouples the anchor from Warudo's wrapper.
- For `CharacterAsset`, `Attachable` targets `Character.MainTransform`, never `Character.GameObject.transform`.
- When reparenting between coordinate spaces (Character, Screen, World), never use `Transform.InverseTransformPoint` or scaled temporary GameObjects (which divide/multiply coordinates by prop mesh scale). Convert reference frames using pure unscaled vector/quaternion transforms: $\mathbf{p}_{\text{local}} = \mathbf{R}_{\text{parent}}^{-1} \cdot (\mathbf{P}_{\text{world}} - \mathbf{P}_{\text{parent}})$.
- Never use absolute vertical threshold sanity checks (e.g. `Position.y < 0.2f`) on properties that can be parented to props or screens. → [Gotcha #44](.ai/warudo_runtime_gotchas.md)

---

## 4. Knowledge Base Index

| File | Covers |
|------|--------|
| [warudo_runtime_gotchas.md](.ai/warudo_runtime_gotchas.md) | All runtime quirks, crash bugs, and serialization traps (44 entries) |
| [plugin_development_cheatsheet.md](.ai/plugin_development_cheatsheet.md) | Plugin/Node/Asset patterns, port attributes, UniTask, toasts |
| [animancer_integration_guide.md](.ai/animancer_integration_guide.md) | Animancer Pro/Lite conflict, assembly identity, layer blending patterns |
| [warudo_modding_architecture.md](.ai/warudo_modding_architecture.md) | UMod build pipeline, `.warudo` package structure, SDK vs runtime env |
| [debugging_and_troubleshooting.md](.ai/debugging_and_troubleshooting.md) | Log paths, headless build, scene JSON inspection, assembly inspection |

---

## 5. Troubleshooting

- **Warudo Runtime Log**: `C:\Users\flame\AppData\LocalLow\HakuyaLabs\Warudo\Player.log`
- **Inspect evaluated port values**: Ask user to `Ctrl+S` save scene → Warudo writes full JSON state to the scene file.
- **Playground compilation errors**: Silent fallback to stale code — always check `Player.log` for Roslyn errors. → [Gotcha #16](.ai/warudo_runtime_gotchas.md)
- **Detailed troubleshooting workflows**: See [debugging_and_troubleshooting.md](.ai/debugging_and_troubleshooting.md)
