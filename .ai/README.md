# FS Input Receiver — AI & Engineering Knowledge Base

This directory contains key architecture notes, integration patterns, build pipeline mechanics, and troubleshooting workflows for developing Warudo plugins and mods in this project.

## Documents

1. [warudo_modding_architecture.md](file:///d:/Code/FS%20Input%20Receiver/.ai/warudo_modding_architecture.md)
   - Runtime environment (`Warudo_Data/Managed/`) vs. SDK environment.
   - UMod build pipeline (`CompileRuntimeScripts`) and `.warudo` package structure.
   - Embedded package configuration (`Packages/app.warudo.modtool`).

2. [animancer_integration_guide.md](file:///d:/Code/FS%20Input%20Receiver/.ai/animancer_integration_guide.md)
   - Detailed breakdown of the Animancer Pro vs. Animancer Lite conflict.
   - Assembly identity rules (`Animancer, Version=7.4.2.25`) for standalone store distribution.
   - Safe API patterns for character animation and layer blending.

3. [plugin_development_cheatsheet.md](file:///d:/Code/FS%20Input%20Receiver/.ai/plugin_development_cheatsheet.md)
   - Quickstart patterns for `Plugin`, `Node`, and `Asset` creation.
   - Flow input/output, data port attributes (`[DataInput]`, `[DataOutput]`, `[Trigger]`).
   - Concurrency (`UniTask`), toasts, and resource disposal.

4. [debugging_and_troubleshooting.md](file:///d:/Code/FS%20Input%20Receiver/.ai/debugging_and_troubleshooting.md)
   - Unity and Warudo log file paths (`Editor.log`, `Player.log`).
   - PowerShell scripts for extracting `.warudo` packages and inspecting assembly references with Mono.Cecil.
   - Headless build and verification with MSBuild.
   - Inspecting active evaluated state via saved scene JSON (`/api/openedScene/save`).

5. [warudo_runtime_gotchas.md](file:///d:/Code/FS%20Input%20Receiver/.ai/warudo_runtime_gotchas.md)
   - Runtime quirks, reflection bugs, and serialization traps uncovered from debugging.
   - `StructuredData<TParent>` orphaned parent in arrays/collections.
   - Attribute target restrictions (`[DataInput]` strictly on fields).
   - Fatal avatar masking crash in `SetupOverlappingAnimations()`.
   - Playground mode vs. packaged Plugin (uMod) mode execution duality.
   - Top-level enum declaration rule for reflection.
   - Humanoid limb IK target orientation conventions (90° / -90°) & intermediate transform decoupling.
   - Thread safety boundary for UDP background receivers (`ReceiverAsset`).
   - Visual setup anchor lifecycle and localization key token traps.
   - Dynamic rotation spherical pivot geometry and interactive drag feedback.
   - Silent stale code execution during Playground hot-reload failures.
   - Two-sided procedural geometry requirement (Unity `Standard` shader ignores `_Cull = Off`).
   - Hot-reload auto-restoration loop trap and high-frequency input strobing.
   - PBR diffuse albedo dimming vs. unlit self-illumination and Enlighten GI dirtiness.
   - Zero-allocation dynamic mesh vertex morphing vs. mesh rebuild allocations.
   - Allocation-free input transitions with DOTween easing (`DOVirtual.EasedValue`).
   - Constant-topology procedural arc generation for zero-allocation dynamic morphing.
   - `StructuredData` port assignment: C# field assignment vs. `SetDataInput`.
   - Ergonomic neutral home-row keys vs. reach displacement in keyboard tracking.
   - Scene deserialization interception & two-phase asset version auto-migration (`OnPreMigrateAsset` / `OnPostMigrateAsset`).
   - `ICollapsibleStructuredData`, default collapsed state (`CollapsedSelf`), and avoiding accordion recursion.
   - Visual setup reference origin visualization (red sphere indicator) & prop occlusion (`AlwaysOnTop`).
   - Asset versioning lifecycle policy: active development branches vs. production releases.
   - Duplicate `[Section]` attributes cause duplicate section UI banners.
   - `StructuredData` port defaults, constructor reflection, and Warudo UI "Reset" button mechanics.
   - Visual calibration architecture: decoupled origin/guides, automation triggers, and milestone thresholds.
   - Rest-relative hand slab & surface normal orientation (Gotcha #39).
   - `SetCharacterOverlappingAnimations` crash in Playground mode (Roslyn `RecursivelySetData` failure) and `IsPlaygroundMode` branch fix (Gotcha #40).
   - UMod security scanner blocks `System.Reflection`; `SetPortDefaultValue` removed entirely (Gotcha #41).
