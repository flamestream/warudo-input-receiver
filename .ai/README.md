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

5. [warudo_runtime_gotchas.md](file:///d:/Code/FS%20Input%20Receiver/.ai/warudo_runtime_gotchas.md)
   - Runtime quirks, reflection bugs, and serialization traps uncovered from debugging.
   - `StructuredData<TParent>` orphaned parent in arrays/collections.
   - Attribute target restrictions (`[DataInput]` strictly on fields).
   - Fatal avatar masking crash in `SetupOverlappingAnimations()`.
   - Playground mode vs. packaged Plugin (uMod) mode execution duality.
   - Top-level enum declaration rule for reflection.
   - Humanoid limb IK target orientation conventions (90° / -90°).
   - Thread safety boundary for UDP background receivers (`ReceiverAsset`).
   - Visual setup anchor lifecycle and localization key token traps.
   - Dynamic rotation spherical pivot geometry and interactive drag feedback.
   - Silent stale code execution during Playground hot-reload failures.
