# Warudo Plugin Development Cheatsheet

## 1. Plugin Core Architecture

A Warudo Plugin is composed of:
1. **Plugin Class** (`Plugin`): Lifecycle management (`OnCreate`, `OnDestroy`, settings registration).
2. **Nodes** (`Node`): Logic blocks used in Warudo node graphs.
3. **Assets** (`Asset`): Configurable entities that exist in the Warudo 3D scene.

---

## 2. Plugin Class Definition
```csharp
using Warudo.Core.Attributes;
using Warudo.Core.Plugins;
using Cysharp.Threading.Tasks;

namespace FSInputReceiver 
{
    [PluginType(
        Id = "FS.InputReceiver",
        Name = "FS Input Receiver",
        Description = "Receives and maps external input into Warudo",
        Version = "1.0.0",
        Author = "FlameStream"
    )]
    public class InputReceiverPlugin : Plugin 
    {
        protected override void OnCreate() 
        {
            base.OnCreate();
            // Initialization logic, register events, start background listeners
        }

        protected override void OnDestroy() 
        {
            // Clean up sockets, threads, hooks, and subscriptions
            base.OnDestroy();
        }
    }
}
```

---

## 3. Node Definition Pattern
```csharp
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FSInputReceiver.Nodes 
{
    [NodeType(
        Id = "FS.InputReceiver.Node.InputTrigger",
        Title = "Input Trigger",
        Category = "Input"
    )]
    public class InputTriggerNode : Node 
    {
        // Inputs
        [DataInput]
        [Description("Enable input processing")]
        public bool Enabled = true;

        [DataInput]
        [Description("Key or input code to listen for")]
        public int KeyCode;

        // Outputs
        [DataOutput]
        [Description("Current trigger state")]
        public bool IsActive;

        // Flow Ports
        [FlowInput]
        public Continuation Enter() 
        {
            if (!Enabled) return null;
            IsActive = true;
            Broadcast();
            return Exit;
        }

        [FlowOutput]
        public Continuation Exit;

        [Trigger]
        [Description("Manually activate this trigger")]
        public void ManualTrigger() 
        {
            Enter();
        }
    }
}
```

---

## 4. Asset Definition Pattern
```csharp
using Warudo.Core.Attributes;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets.Character;

namespace FSInputReceiver.Assets 
{
    [AssetType(
        Id = "FS.InputReceiver.Asset.Receiver",
        Title = "Input Receiver Asset",
        Category = "Input"
    )]
    public class InputReceiverAsset : Asset 
    {
        [DataInput]
        [Description("Target character to receive motion/animation input")]
        public CharacterAsset Character;

        protected override void OnCreate() 
        {
            base.OnCreate();
        }

        protected override void OnDestroy() 
        {
            base.OnDestroy();
        }
    }
}
```

---

## 5. Best Practices & Common Gotchas

* **Use `UniTask` instead of `async void`**:
  * Warudo relies heavily on `Cysharp.Threading.Tasks.UniTask`.
  * Avoid `async void` for methods unless required by an interface signature; unhandled exceptions in `async void` can terminate the Unity process.
* **Cleaning Up Resources in `OnDestroy()`**:
  * Mod scripts can be hot-reloaded or re-instantiated. Always unhook events, close WebSockets, and release memory in `OnDestroy()`.
* **Toast Notifications**:
  * Use Warudo's toast system for user-facing feedback:
    ```csharp
    Warudo.Core.Context.Service.Toast(
        Warudo.Core.Server.ToastSeverity.Info,
        "Plugin Title",
        "Action message here"
    );
    ```
* **Serialization & Data Attributes**:
  * All public fields marked with `[DataInput]` or `[DataOutput]` are serialized by Warudo's JSON serializer.
  * Avoid placing complex non-serializable objects (like raw Unity GameObjects or streams) as `[DataInput]` without custom serializer handling.
  * `[DataInput]` and `[Hidden]` only apply to **fields**, not C# properties (`get; set;`).
  * Enums used as `[DataInput]` **must** be top-level in the namespace (nested enums fail type resolution).

---

## 6. Deep Runtime Quirks & Gotchas Guide

For detailed breakdowns of critical runtime gotchas that cannot be determined by source code inspection, consult:
👉 **[warudo_runtime_gotchas.md](file:///d:/Code/FS%20Input%20Receiver/.ai/warudo_runtime_gotchas.md)**

Key gotchas covered:
- **Orphaned `Parent` in `StructuredData<TParent>` arrays**: Warudo deserializer does not inject `Parent` into array elements; you must run a manual `RefreshKeyBindingsParent()` pass.
- **Avatar Masking Crash**: Any `OverlappingAnimationData` with `Masked = true` MUST have a non-null `MaskedBodyParts` array or `CreateAvatarMask()` crashes the game loop with a `NullReferenceException`.
- **Playground vs. Plugin (uMod) Duality**: Direct character array modification works in Playground, but Plugin mode requires `SetValueAtPath` via `Character.DataInputPortCollection` (use `Helper.IsPlaygroundMode`).
- **Humanoid IK Orientations**: Straight forward hand facing in Warudo is `Vector3(0, 90, 0)` (Left) and `Vector3(0, -90, 0)` (Right); `(0, 0, 0)` points hands sideways in A-pose.
- **UDP Listener Thread Boundary**: Never touch Unity objects or Warudo context from background UDP threads; buffer in `lastState` and process in `OnUpdate()`.
- **Hot-Reload Stale Code Execution**: Roslyn compilation errors cause silent fallback to existing stale assemblies in memory without any UI error dialog.
- **`StructuredData` Port Assignment**: Always call `data.SetDataInput(nameof(...), val, true)` instead of direct C# field assignment to prevent values being wiped on domain reload or UI sync. Never overwrite ports per-frame in `OnUpdate()`.
- **Zero-Allocation Dynamic Mesh Morphing**: Enforce constant vertex topology (collapsing arc vertices at sharp corners) so asymmetric border widths and corner radii can be morphed dynamically via in-place vertex streaming without index buffer rebuilds.
- **Allocation-Free Input Transitions**: Use `Mathf.MoveTowards()` on a normalized scalar and sample `DOVirtual.EasedValue()` statically instead of instantiating `Tween` objects on high-frequency (60–120 Hz) inputs.
- **Scene JSON Inspection Workflow**: Ask the user to save the scene (`Ctrl+S`) to inspect exact evaluated port states in the saved scene JSON file without speculative debugging.


