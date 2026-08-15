# AGENTS.md - Developer & AI Assistant Guide for FS Input Receiver

Welcome to the **FS Input Receiver** repository. This guide provides architecture overview, development workflows, hot-reload instructions, and coding standards for AI agents and developers.

---

## 1. Repository Overview

**FS Input Receiver** is a C# plugin for the 3D VTubing application **Warudo**. It provides real-time tracking and animation systems for pen tablets, mice, keyboards, game controllers (GameInput, XInput, DirectInput), steering wheels, and flight sticks by receiving and translating UDP streams emitted by **FS Input Device Emitter**.

- **Language & Runtime**: C# 8.0, target framework `.NET Framework 4.8` (Unity Mono/Roslyn environment).
- **Core Dependencies**: Warudo Core SDK (`Warudo.Core.dll`), Unity Engine (`UnityEngine.dll`), Animancer (`Animancer.dll`), DOTween (`DOTween.dll`), UniTask (`UniTask.dll`), Newtonsoft.Json (`Newtonsoft.Json.dll`).
- **Available Assemblies**: Pre-shipped inside Warudo's `Warudo_Data/Managed/` directory (400 DLLs).

---

## 2. Directory Structure

```
FS Input Receiver/
├── src/                          # Plugin C# source code
│   ├── PointerReceiverPlugin.cs  # Main plugin entrypoint ([PluginType])
│   ├── Assets/                   # Warudo Asset classes (ReceiverAsset, PointerReceiverAsset, etc.)
│   ├── Nodes/                    # Warudo Blueprint Nodes (Animators, Trackers, Triggers)
│   ├── Libs/                     # Helper classes (VKCode, DynamicVector3, MathExpression, etc.)
│   ├── Localizations/            # Localization JSON dictionaries (en.json, ja.json, etc.)
│   ├── PlaygroundPlugin.csproj   # MSBuild SDK project configured with Managed DLL references
│   └── FSInputReceiver.sln       # Visual Studio Solution
├── personal-tools/               # Hot-reload sync & verification tooling
│   ├── sync.bat / sync.ps1       # Robocopy-based mirror sync between src/ and Warudo Playground
│   ├── verify-build.ps1          # One-step compilation test against Warudo Managed DLLs
│   └── UNIFIED-SYNC-README.md   # Sync tool documentation
├── docs/                         # Architectural documentation
│   └── architecture-and-development.md # Complete system architecture guide
├── wiki/                         # User guides and controller animation setup tutorials
├── AGENTS.md                     # AI Assistant instructions (this file)
└── README.md                     # User-facing project overview
```

---

## 3. Development & Hot-Reload Workflow

Testing does **not** require packaging the plugin into `.warudo` mod files. Warudo provides a **Playground** feature that compiles `.cs` files on-the-fly inside `Warudo_Data/StreamingAssets/Playground/`.

### 3.1 Recommended Development Cycle

```
┌─────────────────────────┐
│ 1. Edit code in src/   │
└────────────┬────────────┘
             │
┌────────────▼─────────────────────────┐
│ 2. Run verify-build.ps1 (Type check) │
└────────────┬─────────────────────────┘
             │
┌────────────▼─────────────────────────┐
│ 3. Run sync.bat src warudo           │
└────────────┬─────────────────────────┘
             │
┌────────────▼─────────────────────────┐
│ 4. Warudo Playground hot-reloads live│
└──────────────────────────────────────┘
```

### 3.2 Key Commands
```powershell
# 1. Compile & verify without pushing:
dotnet build src/PlaygroundPlugin.csproj

# 2. Compile & verify via helper script:
.\personal-tools\verify-build.ps1

# 3. Compile, verify, AND automatically push to Warudo:
.\personal-tools\verify-build.ps1 -Sync

# 4. Manual sync commands:
.\personal-tools\sync.bat src warudo          # Push to Warudo
.\personal-tools\sync.bat warudo src          # Pull from Warudo
.\personal-tools\sync.bat src warudo --dry-run# Preview changes
```

---

## 4. Architectural Rules & Best Practices

### 4.1 Plugin Registrations
When creating new `Asset` or `Node` classes, you **MUST** register them in [`src/PointerReceiverPlugin.cs`](file:///d:/Code/FS%20Input%20Receiver/src/PointerReceiverPlugin.cs) under the `AssetTypes` or `NodeTypes` arrays in the `[PluginType]` attribute.

### 4.2 Asset Architecture (`ReceiverAsset`)
All input receivers inherit from `ReceiverAsset`:
- `ReceiverAsset` runs a background task `Task.Run(() => Listen(udpClient))` using `UdpClient.Receive()`.
- The background thread writes the raw packet string to `protected string lastState`.
- **CRITICAL THREAD SAFETY RULE**: Never touch Unity objects (`GameObject`, `Transform`, `Vector3`, `Time`) or Warudo services on the `Listen()` thread. All packet decoding and state changes must occur on the main thread inside `OnUpdate()`.
- Check packet version using `CheckValidProtocolVersion(inputString, PROTOCOL_VERSION, PROTOCOL_ID)`.

### 4.3 Node Architecture
- Nodes inherit from `Warudo.Core.Nodes.Node`.
- Decorate classes with `[NodeType(Id = "...", Title = "...", Category = "...")]`.
- Input ports: `[DataInput]` or `[TriggerInput]`.
- Output ports: `[DataOutput]` or `[TriggerOutput]`.
- Use `Watch(nameof(PortName), callback)` in `OnCreate()` to react to data input changes.

### 4.4 Animancer & Character Animation
- Animations are layered over the character's base animator using Warudo's Animancer integration.
- Always use `CHARACTER_ANIM_LAYER_ID_PREFIX` to isolate plugin layers.
- Check if character assets are valid and active before querying bones or playing animations.

### 4.5 Localization
- Never hardcode user-facing strings in UI attributes or logs.
- Define keys in `src/Localizations/en.json` (and other languages if applicable).
- Call `"MY_STRING_KEY".Localized()` to retrieve translated text.

### 4.6 Retaining Compatibility
- Do not rename existing node IDs or data port names as this breaks existing user scenes and graphs in Warudo.

### 4.7 Enum Declarations for DataInput Ports & StructuredData
- **CRITICAL**: Any `enum` used as a `[DataInput]` port or inside a `StructuredData` class **MUST** be defined directly at the `namespace` level (top-level type).
- Do **NOT** declare enums nested inside a class (e.g. `class MyData { public enum MyEnum ... }`), because the CLR type reflection name becomes `Namespace.MyData+MyEnum`, which Warudo's type resolver cannot find, causing runtime log errors such as `Enum type ... does not exist`.

### 4.8 Limb IK Hand Orientation Standards
- In Warudo, humanoid avatar wrist/hand IK targets use the following rotation standards when facing straight forward in front of the avatar (relative to the Character or a forward-facing Root anchor):
  - **Left Hand (Straight Forward)**: `Vector3(0f, 90f, 0f)` (rotates left palm/fingers forward toward a keyboard, desk, or prop).
  - **Right Hand (Straight Forward)**: `Vector3(0f, -90f, 0f)` (rotates right palm/fingers forward toward a keyboard, desk, or prop).
  - *Note*: An IK rotation of `(0, 0, 0)` causes humanoid hands to point sideways (T-pose/A-pose resting angle) rather than forward.

---

## 5. Troubleshooting & Runtime Logs

- **Warudo Runtime Log**: `C:\Users\flame\AppData\LocalLow\HakuyaLabs\Warudo\Player.log`
  - Inspect `Player.log` when debugging hot-reload compilation errors, missing types, deserialization issues, or scene loading exceptions.


