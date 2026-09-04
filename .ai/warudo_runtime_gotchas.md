# Warudo Runtime Quirks, Non-Obvious Gotchas & Engineering Tips

This document catalogs non-obvious runtime behaviors, SDK quirks, reflection restrictions, and architectural pitfalls in the **Warudo** modding framework. These issues **cannot** be deduced simply by inspecting mod source code and were uncovered through runtime debugging, decompilation, and stack trace analysis.

---

## 1. `StructuredData<TParent>` in Arrays/Lists: The "Orphaned Parent" Pitfall

### The Phenomenon
When you define a class inheriting from `StructuredData<TParent>` and expose an array or list of them as a `[DataInput]` port on an Asset or Node:
```csharp
[DataInput]
public KeyboardKeyBinding[] KeyBindings;
```
Inside `KeyboardKeyBinding`, the base property `public TParent Parent { get; internal set; }` exists. It is natural to assume Warudo's deserializer will populate `Parent` for each element in the array.

**It does not.** Warudo's custom JSON deserializer only injects the `Parent` reference on single direct fields, NOT on elements inside an array (`T[]`) or list (`List<T>`).

### Runtime Failure
Any property getter, method, or visual setup trigger inside the item that accesses `Parent` (e.g., `Parent.GetRootAnchor()`, `Parent.ActiveKeyBindingInVisualSetup`, or `Parent.Character`) throws an unhandled `NullReferenceException` or silently aborts at runtime:
```
NullReferenceException: Object reference not set to an instance of an object
  at FSInputReceiver.Assets.KeyboardKeyBinding.GetRootAnchor()
```

### The Fix: Explicit Re-Parenting Pass
You **must** implement a manual re-parenting pass on the parent asset/node:
```csharp
protected override void OnCreate() {
    base.OnCreate();
    Watch(nameof(KeyBindings), delegate { RefreshKeyBindingsParent(); });
    RefreshKeyBindingsParent();
}

public void RefreshKeyBindingsParent() {
    if (KeyBindings == null) return;
    for (int i = 0; i < KeyBindings.Length; i++) {
        if (KeyBindings[i] != null) {
            KeyBindings[i].Parent = this;
            KeyBindings[i].SetupTransformCallbacks();
        }
    }
}
```
In addition, guard in `OnUpdate()` or before entering modal states (e.g., Visual Setup) by checking if `KeyBindings[0].Parent == null` and re-invoking `RefreshKeyBindingsParent()`.

---

## 2. Attribute Targets: `[DataInput]` and `[Hidden]` Only Apply to Fields

### The Phenomenon
In standard C#, attributes like `[SerializeField]` can often be placed on auto-properties or fields. However, in `Warudo.Core.dll`:
```csharp
[AttributeUsage(AttributeTargets.Field)]
public class DataInputAttribute : Attribute { ... }
```
`[DataInput]`, `[DataOutput]`, and `[Hidden]` are strictly restricted to `AttributeTargets.Field`.

### Compiler Error
Attempting to decorate a C# property:
```csharp
[DataInput]
public Vector3 DeltaPosition { get; set; } // ERROR CS0592
```
Fails compilation with:
```
error CS0592: Attribute 'DataInput' is not valid on this declaration type. It is only valid on 'field' declarations.
```

### The Fix: Backing Fields vs. Property Forwarding
- If you need a serialized Warudo input, it **must** be declared as a public field:
  ```csharp
  [DataInput]
  public VisualSetupTransform Transform = new VisualSetupTransform();
  ```
- If you need backward compatibility with existing code or scenes that reference the old field name as a property, create a plain C# property **without** attributes that forwards to the sub-field:
  ```csharp
  // Backward compatibility forwarder (no [DataInput] attribute!)
  public Vector3 DeltaPosition {
      get => Transform?.OffsetPosition ?? Vector3.zero;
      set { if (Transform != null) Transform.OffsetPosition = value; }
  }
  ```

---

## 3. Fatal Avatar Masking Crash in `SetupOverlappingAnimations`

### The Phenomenon
Warudo allows custom animation layers to be blended over a character using `CharacterAsset.OverlappingAnimations`. Each layer is represented by an `OverlappingAnimationData` instance.

If you create an `OverlappingAnimationData` layer with `Masked = true`, Warudo's internal `CharacterAsset.SetupOverlappingAnimations()` creates an avatar mask via `CreateAvatarMask()`.

### The Crash
In `Warudo.Plugins.Core.dll`, the internal implementation iterates over `layer.GetMaskedBodyParts()` **without a null check**. If `Masked = true` and `MaskedBodyParts == null`, Warudo throws an unhandled `NullReferenceException` on the main Unity thread:
```
NullReferenceException: Object reference not set to an instance of an object
  at Warudo.Plugins.Core.Assets.Character.CharacterAsset.SetupOverlappingAnimations ()
```
This crashes animation playback immediately and can freeze avatar tracking entirely.

### The Rule
Whenever `Masked = true` is set on any `OverlappingAnimationData`, you **MUST** ensure `MaskedBodyParts` is populated with a non-null array:
```csharp
var layer = new CharacterAsset.OverlappingAnimationData {
    Animation = clipUri,
    Masked = true,
    MaskedBodyParts = new[] { AnimationMaskedBodyPart.LeftFingers } // MUST NEVER BE NULL!
};
```
Even if you intend to mask everything or customize it later, never leave `MaskedBodyParts` as `null`.

---

## 4. Playground Mode vs. Packaged Plugin (uMod) Mode

### The Phenomenon
Warudo code can run in two completely different runtime execution modes:
1. **Playground Mode**: Source files (`.cs`) inside `Warudo_Data/StreamingAssets/Playground/` are compiled on-the-fly by Warudo's Roslyn monitor (`HotReloadScriptMonitor`) into the host Unity domain.
2. **Packaged Plugin (uMod) Mode**: Compiled as a `.warudo` mod archive and loaded by UMod into an isolated mod AppDomain.

### The Desynchronization Gotcha
In **Playground Mode**, you can modify character arrays directly and call broadcast:
```csharp
character.OverlappingAnimations = layers;
character.BroadcastDataInput(nameof(character.OverlappingAnimations));
```
In **Plugin (uMod) Mode**, directly replacing the array causes the internal WebSocket state synchronizer to drop messages or throw deserialization exceptions because the uMod boundary requires port mutations to go through Warudo's dirty-tracking port collection:
```csharp
character.DataInputPortCollection.SetValueAtPath(nameof(character.OverlappingAnimations), layers, true);
```

### The Solution: Runtime Mode Detection Helper
Always use [`Helper.IsPlaygroundMode`](file:///d:/Code/FS%20Input%20Receiver/src/Libs/Helper.cs) to branch behavior:
```csharp
public static bool IsPlaygroundMode {
    get {
        var asm = typeof(Helper).Assembly;
        if (!string.IsNullOrEmpty(asm.Location) && asm.Location.IndexOf("Playground", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        var name = asm.GetName().Name ?? "";
        if (name.IndexOf("umod-", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        return true; // default safe fallback
    }
}

public static void SetCharacterOverlappingAnimations(CharacterAsset character, CharacterAsset.OverlappingAnimationData[] layers) {
    if (character == null) return;
    if (IsPlaygroundMode) {
        character.OverlappingAnimations = layers;
        character.BroadcastDataInput(nameof(character.OverlappingAnimations));
    } else {
        character.DataInputPortCollection.SetValueAtPath(nameof(character.OverlappingAnimations), layers, true);
    }
}
```

---

## 5. Top-Level Enum Declaration Rule for Reflection & UI Dropdowns

### The Phenomenon
When declaring an `enum` intended for use as a `[DataInput]` dropdown or inside a `StructuredData` class, developers frequently nest the enum inside the class for encapsulation:
```csharp
public class MyAsset : Asset {
    public enum TrackingMode { Absolute, Relative } // NESTED ENUM - DANGER!
    
    [DataInput]
    public TrackingMode Mode;
}
```

### The Runtime Error
In the CLR, a nested type's reflection full name uses a plus sign `+` separator: `FSInputReceiver.Assets.MyAsset+TrackingMode`.
Warudo's type resolver parses type names using dot separators (`.`) and fails to parse `+`. At runtime, Warudo logs an error to `Player.log`:
```
[Error] Enum type FSInputReceiver.Assets.MyAsset+TrackingMode does not exist
```
The inspector fails to render the dropdown or displays it as an empty or unselectable field.

### The Rule
**Every enum used in a `[DataInput]` or `StructuredData` MUST be declared directly at the namespace level (top-level type).**
```csharp
namespace FSInputReceiver.Assets {
    public enum TrackingMode { Absolute, Relative } // CORRECT: Top-level

    public class MyAsset : Asset {
        [DataInput]
        public TrackingMode Mode;
    }
}
```

---

## 6. Limb IK Target Orientation Standards in Warudo

### The Phenomenon
In Warudo, humanoid avatars use standard Unity Humanoid IK rigs. When avatar arms are in their natural rest/T-pose, the wrist coordinate axes do not point "forward" relative to the avatar root.

If you set an IK target rotation to `Quaternion.identity` or `Vector3(0, 0, 0)`:
```csharp
character.LeftHandIKTarget.transform.rotation = character.transform.rotation;
```
The avatar's wrists will point **sideways** (outwards or towards the thighs, following the A/T-pose resting angle), rather than forward towards a desk, keyboard, or steering wheel.

### The Standard Forward Orientations
To orient humanoid hands facing flat and straight forward in front of the avatar (palms facing down/forward towards a desk or prop):
- **Left Hand (Facing Forward)**: `Vector3(0f, 90f, 0f)`
- **Right Hand (Facing Forward)**: `Vector3(0f, -90f, 0f)`

All relative rotation offsets (key press tilt, mouse grip angle, pen tilt) should be added to or composed with these base forward rotations.

---

## 7. UDP Receiver Thread Safety Boundary (`ReceiverAsset`)

### The Phenomenon
`ReceiverAsset` listens for external packets (e.g. from `FsInputEmitter.exe`) using a background task:
```csharp
Task.Run(() => Listen(udpClient));
```
Inside the `Listen()` loop, socket reads block on `udpClient.Receive(ref endpoint)`.

### The Fatal Error
Unity's underlying C++ engine is strictly single-threaded. If any code on this background thread attempts to touch Unity API objects:
- `transform.position`, `gameObject.activeSelf`
- `Time.time`, `Time.deltaTime`
- `Mathf.Clamp`, `Quaternion.Euler`
- Warudo context services or toast notifications

Unity immediately throws an exception:
```
UnityException: get_transform can only be called from the main thread.
Constructors and field initializers will be executed from the loading thread when loading a scene.
```
This terminates the background listener thread silently, killing external packet reception without crashing the entire process.

### The Rule
The background listener thread **must only** read UDP bytes, convert them to raw strings, and write them to a synchronized field:
```csharp
protected string lastState;
```
**All packet parsing, version validation, coordinate calculations, and scene manipulations MUST take place in `OnUpdate()` on the main Unity thread.**

---

## 8. Visual Setup Anchor Lifecycle & Localization Key Tokens

### The Phenomenon
When creating visual setup gizmos (such as `VisualSetupTransform.CreateAnchor()`), the anchor asset is instantiated into the scene hierarchy:
```csharp
setupAnchor = Scene.AddAssetToGroup<VisualSetupAnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
var title = $"SETUP_ANCHOR_NAME_{LocalizationKey}".Localized();
setupAnchor.Name = $"⌛⚓-{title}";
```

### The Missing Key Bug
If `LocalizationKey` is `null` or empty:
1. `$"SETUP_ANCHOR_NAME_{LocalizationKey}"` evaluates to `"SETUP_ANCHOR_NAME_"`.
2. `".Localized()"` cannot find this key in `en.json` / `ja.json`.
3. Warudo's localization engine falls back to the raw token string.
4. The user sees an anchor named `⌛⚓-SETUP_ANCHOR_NAME_` with description `SETUP_ANCHOR_INSTRUCTIONS_` in their 3D scene.

### Best Practices:
1. Always assign `Transform.LocalizationKey = "KEYBOARD_KEY_BINDING"` (or the appropriate specific token) during initialization.
2. Add both English (`en.json`) and Japanese (`ja.json`) translations for every anchor:
   ```json
   "SETUP_ANCHOR_NAME_KEYBOARD_KEY_BINDING": "Key Binding",
   "SETUP_ANCHOR_INSTRUCTIONS_KEYBOARD_KEY_BINDING": "Move the anchor to position the finger key press target."
   ```
3. Always implement anchor cleanup in `OnDestroy()` and `OnExitVisualSetup()` to avoid leaving orphaned `⌛⚓` assets in user scenes if the node or asset is deleted while in setup mode.

---

## 9. Dynamic Rotation Spherical Math & Interactive Drag Feedback

### The Mathematics
To dynamically rotate a hand or wrist as it moves translationally away from a resting position $\vec{P}_{\text{rest}}$ to an offset $\vec{P}$:
$$\vec{\Delta} = \vec{P} - \vec{P}_{\text{rest}} = (dx, dy, dz)$$
The spherical rotation angles required to point or pivot towards/away from the pivot center are:
$$\Delta \text{Pitch} (X) = \operatorname{atan2}(-dy, \sqrt{dx^2 + dz^2}) \times \frac{180}{\pi}$$
$$\Delta \text{Yaw} (Y) = \operatorname{atan2}(dx, dz) \times \frac{180}{\pi}$$

### Visual Setup Interactivity Gotcha
When a user enables dynamic rotation on a hand and enters Visual Setup mode for a key or rest target:
1. Moving the anchor changes $\vec{\Delta}$.
2. If dynamic rotation is active, the hand preview must **immediately apply** the calculated dynamic rotation on top of the anchor rotation:
   $$\vec{R}_{\text{target}} = \vec{R}_{\text{anchor}} + \vec{R}_{\text{dynamic}}(\vec{\Delta})$$
   Otherwise, the user will align the anchor assuming zero rotation, only to find the hand rotated incorrectly once they exit setup mode!
3. If dynamic rotation is **disabled**, do NOT apply dynamic rotation and **hide** any spherical indicator circles in the scene to avoid visual clutter and confusion.

---

## 10. Playground Hot-Reload Failure Mode: Silent Stale Code Execution

### The Phenomenon
When modifying code in `Warudo_Data/StreamingAssets/Playground/`, Warudo's `HotReloadScriptMonitor` watches for file modification events and invokes Roslyn compiler in the background.

If your code has a compilation error (e.g. syntax error, missing type, or broken reference):
1. Roslyn fails compilation and writes the error to `Player.log`.
2. **Warudo does NOT unload the previous assembly.**
3. Warudo continues executing the **stale assembly** already in memory without any alert in the UI.

### Why This Wastes Hours
Developers make changes, see no error in the UI, but test the game and notice their changes have no effect. They assume their logic is flawed, when in reality their new code was never loaded!

### The Prevention Rule
1. **Never sync without type-checking**: Always run `personal-tools/verify-build.ps1` before pushing to `Playground`.
2. **Check Player.log**: If hot-reload seems unresponsive, check:
   `%USERPROFILE%\AppData\LocalLow\HakuyaLabs\Warudo\Player.log`
   Search for `[HotReloadScriptMonitor]` or `error CS`.

---

## 11. Scene Cloning and Transient State Loss

### The Phenomenon
Warudo duplicates assets and nodes via deep JSON serialization (clone). In addition, when loading or switching scenes, instances are reconstructed from JSON.

### Gotchas:
1. **Non-Serialized State**: Any field marked `[JsonIgnore]` or not decorated with `[DataInput]` / serialized properties is discarded during cloning.
2. **Event Actions (`Action`, `EventHandler`)**: Callbacks assigned in code (like `OnCreateAnchor`, `OnEnterVisualSetup`) are not serialized. If a node is cloned or reloaded, these action delegates will be `null` unless re-wired in `OnCreate()` or `Watch()`.
3. **GameObject Caches**: Cached Unity `Transform` or `GameObject` references become invalid or null after scene reload. Always resolve transforms lazily or re-fetch them in `OnCreate()` and `OnUpdate()`.

---

## 12. `uDesktopDuplication` / `Monitor` Dependency in Unity SDK

### The Phenomenon
`DrawingScreenAsset.cs` and `PointerReceiverAsset.cs` interact with desktop monitor boundaries using `uDesktopDuplication.Monitor` and `uDesktopDuplication.Manager`:
```csharp
using uDesktopDuplication;
...
Monitor monitor = Manager.monitors.FirstOrDefault(m => m.name == DisplayName);
```
In the Warudo game runtime (`Warudo_Data/Managed/`), `uDesktopDuplication.Runtime.dll` is pre-installed, so local MSBuild / Playground builds compile cleanly.

### The Unity Build Error
When setting up a new Unity Modding Project from Warudo SDK 0.14.5.1, the default `Packages/manifest.json` does **not** include `com.hecomi.udesktopduplication`.
Upon importing `FSInputReceiver`, the Unity Console fails with 6 compilation errors:
```
error CS0246: The type or namespace name 'uDesktopDuplication' could not be found
error CS0246: The type or namespace name 'Monitor' could not be found
```

### The Fix
The package must be present in the Unity project's `Packages/` directory and declared in `Packages/manifest.json`:
```json
{
  "dependencies": {
    "app.warudo.modtool": "file:app.warudo.modtool",
    "com.hecomi.udesktopduplication": "file:com.hecomi.udesktopduplication",
    ...
```
The assembly identity compiled by Unity (`uDesktopDuplication.Runtime, Version=0.0.0.0`) matches Warudo's runtime assembly identity exactly.

---

## 13. UMod Code Security Validation & Prohibited Capabilities

### The Phenomenon
When exporting a mod via **Warudo -> Build Mod**, UMod passes the compiled IL bytecode through a security analysis engine (`Trivial.CodeSecurity` / `ModToolsSecurityRestrictions.asset`). If any method body contains an instruction referencing blacklisted namespaces, types, or members, the build fails immediately:
```
[UMod.BuildEngine.BuildEngineService]: Referenced in method body: '...' at instruction: 'IL_XXXX: Callvirt ...'
ModBuildException: Code security validation failed
```

### Complete Blacklist of Denied Capabilities
From `Packages/app.warudo.modtool/Packages/UMod/Resources/ModToolsSecurityRestrictions.asset`:

| Category | Denied Items |
| :--- | :--- |
| **Denied Namespaces** | • `System.Reflection`<br>• `System.Reflection.Emit`<br>• `Vexe.Fast.Reflection`<br>• `System.IO.*` (Direct disk file access)<br>• `System.Runtime.InteropServices` (P/Invoke)<br>• `RoslynCSharp` / `RoslynCSharp.*`<br>• `UMod.*` (Except `UMod.ModHost` which is explicitly allowed) |
| **Denied Types** | • `System.AppDomain`<br>• `System.Diagnostics.Process`<br>• `System.Threading.Process`<br>• `UMod.Mod` |
| **Denied Assemblies** | • `UnityEditor`<br>• `Mono.Cecil`<br>• `UMod-ModTools` |
| **Denied Members** | • `UnityEngine.Application.Quit`<br>• `UnityEngine.Application.OpenURL`<br>• `UMod.ModHost.AllLoadedModHosts`<br>• `UMod.ModHost.AllModHosts` |
| **P/Invoke** | • `allowPInvoke: false` (strictly prohibited) |

### The Gotcha with Reflection in Environment Detection
Attempting to detect runtime environment (e.g. Playground vs. uMod) by querying assembly metadata:
```csharp
// REJECTED BY UMOD CODE VALIDATOR:
var asm = typeof(Helper).Assembly;
var loc = asm.Location;       // Calls System.Reflection.Assembly::get_Location() -> DENIED
var name = asm.GetName();     // Calls System.Reflection.Assembly::GetName() -> DENIED
var t = obj.GetType().Name;   // Calls System.Reflection.MemberInfo::get_Name() -> DENIED
```

### The Solution (Zero-Reflection Detection)
Warudo sets the `ModHost` property on plugins when loaded via uMod, but leaves it `null` in Playground mode. Because `UMod.ModHost` is in `allowEntries`, checking `ModHost` is completely reflection-free and passes security scanning:
```csharp
public static bool IsPlaygroundMode {
    get {
        if (PointerReceiverPlugin.Instance != null) {
            return PointerReceiverPlugin.Instance.ModHost == null;
        }
        var plugin = Warudo.Core.Context.PluginManager?.GetPlugin("FlameStream.PointerReceiver");
        return plugin == null || plugin.ModHost == null;
    }
}
```

---

## 14. Mod Export Player Build Failure: `Validate.ProOnlyMessage` & `AssemblyUpdater` Errors

### The Phenomenon
When exporting a mod in Unity Editor via **Warudo -> Build Mod**, the build pipeline compiles assets and player scripts for the target platform (Windows Standalone). The build aborts with:
```
ModBuildException: Failed to create shared resources for target platform
Error building Player because scripts had compiler errors
BUILD FAILED!
```
In the Unity Console and `Editor.log`, two distinct errors appear:

1. **The Real Compiler Error (`error CS0117`)**:
   ```
   Packages\app.warudo.modtool\Packages\Animancer\Utilities\Custom Fade\MixerParameterTween.cs(32,31): error CS0117: 'Validate' does not contain a definition for 'ProOnlyMessage'
   ```
2. **The Secondary AssemblyUpdater Log (`Error = 131`)**:
   ```
   Unable to update following assemblies: Assets/umod-compiled-...dll (Name = ..., Error = 131)
   System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation. ---> Mono.Cecil.AssemblyResolutionException: Failed to resolve assembly: 'Warudo.Core, Version=0.0.0.0'
   System.IO.FileNotFoundException: Could not find file "Assets/umod-compiled-...dll"
   ```

### Root Cause Analysis

#### 1. Why `Validate.ProOnlyMessage` Fails:
In `Packages\app.warudo.modtool\Packages\Animancer\Utilities\Custom Fade\MixerParameterTween.cs`, Animancer Lite contains conditional obsolete attributes:
```csharp
#if !UNITY_EDITOR
    [System.Obsolete(Validate.ProOnlyMessage)]
#endif
```
In editor builds, `UNITY_EDITOR` is defined so this line is ignored.
However, when UMod triggers a standalone player build to produce asset bundles, `!UNITY_EDITOR` becomes active.
Because `Validate.cs` in Warudo SDK is located inside an `Editor/` folder (`Packages/app.warudo.modtool/Packages/Animancer/Internal/Editor/Validate.cs`) and does not define `ProOnlyMessage`, the C# compiler fails when building for Player.

**Fix**: In `MixerParameterTween.cs`, replace `Validate.ProOnlyMessage` with a string literal:
```csharp
#if !UNITY_EDITOR
    [System.Obsolete("This feature is only available in Animancer Pro.")]
#endif
```

#### 2. Why `AssemblyUpdater` Logs `Error = 131` / `Warudo.Core` Resolution Failures:
When UMod compiles the mod scripts, it temporarily outputs `Assets/umod-compiled-<guid>.dll` into the `Assets/` root.
Unity's built-in AssetDatabase immediately triggers `APIUpdater` (`AssemblyUpdater.exe`) on any newly imported DLL in `Assets/`.
`AssemblyUpdater.exe` uses `Mono.Cecil` to inspect the DLL and search for deprecated Unity API calls.
Because `Warudo.Core` in the mod project is compiled from an ASMDEF source package (outputting to `Library/ScriptAssemblies/Warudo.Core.dll`), `AssemblyUpdater.exe` does not have `Library/ScriptAssemblies` in its search paths and logs `Failed to resolve assembly: 'Warudo.Core'`.
Concurrently, UMod deletes the temporary DLL during build cleanup, causing `AssemblyUpdater` to throw `FileNotFoundException`.
**This `AssemblyUpdater` error is secondary/harmless and does not block the mod build**; the actual fatal blocker was the Player compilation error in `MixerParameterTween.cs`.

---

## 15. `Scene.Destroy()` Collection Modification: Never Call `Scene.RemoveAsset()` in `OnDestroy()`

### The Phenomenon
When restarting a scene, opening a new scene, or closing Warudo, an unhandled exception crashes scene destruction:
```
System.InvalidOperationException: Collection was modified; enumeration operation may not execute.
  at System.Collections.Generic.List`1+Enumerator[T].MoveNextRare ()
  at System.Collections.Generic.List`1+Enumerator[T].MoveNext ()
  at Warudo.Core.Scenes.Scene.Destroy ()
  at Warudo.Core.Context.OpenScene (Warudo.Core.Serializations.SerializedScene serializedScene)
  at Warudo.Core.Scenes.Scene.Restart ()
```

### Root Cause Analysis
In `Warudo.Core.Scenes.Scene`:
```csharp
public void Destroy() {
    foreach (var asset in this.assetList) {
        try {
            asset.Destroy();
        } catch (Exception ex) {
            Log.Error("Error while destroying asset " + asset.Id, ex);
        }
    }
    this.assetList.Clear();
    this.assetMap.Clear();
    ...
}
```
`Scene.Destroy()` enumerates its internal `List<Asset> assetList` using `List<T>.Enumerator`.
If an asset's `OnDestroy()` method attempts to clean up its own child anchors or helper assets by calling `Scene.RemoveAsset(childAsset.Id)`:
```csharp
void OnDestroy() {
    Scene.RemoveAsset(myAnchor.Id); // FATAL!
}
```
`Scene.RemoveAsset()` calls `this.assetList.Remove(asset)`. This mutates `assetList` while `Scene.Destroy()` is actively enumerating it with `List<T>.Enumerator`, immediately throwing `InvalidOperationException: Collection was modified`.

### Architectural Rule
* **Never invoke `Scene.RemoveAsset()` inside `OnDestroy()`**.
* During scene teardown or restart, Warudo **already calls `Destroy()` on every asset** in the scene automatically. Explicitly calling `Scene.RemoveAsset()` is both redundant and fatal.
* Reserve `Scene.RemoveAsset()` exclusively for user-invoked cleanup actions (e.g. clicking a "Clear Anchors" trigger button) or modal visual setup teardowns while the scene remains active.

---

## 16. `CharacterAsset.OverlappingAnimations`: Always Use `SetDataInput`, Never Direct Field Assignment

### The Phenomenon
A plugin populates `Character.OverlappingAnimations`:
```csharp
character.OverlappingAnimations = layers;
character.BroadcastDataInput(nameof(character.OverlappingAnimations));
```
In the Warudo editor UI, the layers appear in the list under the Character asset inspector. However, at runtime, **no animation plays**, and adjusting layer weights has no effect on character bones.

### Root Cause Analysis
In `CharacterAsset.cs` (inside `Warudo.Plugins.Core.dll`), the internal Animancer layers are allocated and managed by:
```csharp
WatchAll(new[] { "OverlappingAnimations", "OverrideHandPoses", ... }, delegate {
    SetupOverlappingAnimations();
});
```
Directly assigning the C# property `character.OverlappingAnimations = layers;` and calling `character.BroadcastDataInput(...)` only sends WebSocket serialization messages to update the web UI. It completely bypasses `DataInputPortCollection.SetPortValue()`, so `WatchAll` **never fires**.
Because `WatchAll` does not fire, `CharacterAsset.SetupOverlappingAnimations()` is never called, and **no Animancer layers are created on `Character.Animancer`**. The plugin then attempts to drive weights on nonexistent Animancer layers.

### Architectural Rule
Always update `OverlappingAnimations` via `SetDataInput`:
```csharp
character.SetDataInput(nameof(character.OverlappingAnimations), layers, true);
```
`SetDataInput`:
1. Calls `DataInputPortCollection.SetPortValue()`, which notifies watchers and triggers `SetupOverlappingAnimations()` synchronously to rebuild the Animancer layers.
2. Broadcasts the port change to the frontend UI.

---

## 17. Animancer Layer Playback & Stopped State Gotcha (`Events.OnEnd` and `IsPlaying = false`)

### The Phenomenon
Animancer layers are present on `Character.Animancer`, and code calls `animancer.Layers[layerIdx].SetWeight(1.0f)` when a button is pressed, but the character's hand/finger does not move.

### Root Cause Analysis
In Warudo's `SetupOverlappingAnimations()`:
```csharp
var state = layer.GetOrCreateState("OverlapLayer" + layerIndex, clip, true);
layer.Play(state);
state.Events.OnEnd = delegate {
    if (layerData.IsLooping()) {
        AnimancerEvent.CurrentState.Time = 0f;
    } else {
        state.IsPlaying = false;
    }
    layerData.OnAnimationEnd(state);
};
```
If an overlapping animation has `Looping = false`:
1. Upon scene creation, the clip starts playing.
2. Even with `Weight = 0f`, the clip runs to completion in the background, reaches `NormalizedTime >= 1.0f`, and triggers `Events.OnEnd`.
3. `Events.OnEnd` executes `state.IsPlaying = false`.
4. When a user later presses a button and the plugin raises `layer.Weight` to `1.0f`, Animancer will **not** evaluate the state because `state.IsPlaying == false` and its playhead is stuck at the end of the clip.

### Architectural Rule
1. **Always set `Looping = true`** on overlapping animation layers that are dynamically driven by weight blending or key inputs.
2. **Rewind and play on trigger**: When triggering an action animation on button press (`Activated`), always explicitly restart playback:
   ```csharp
   var state = layer.Play(layer.CurrentState);
   state.Time = 0f;
   ```
3. **Ensure `IsPlaying = true` when setting weight**: When applying layer weight (`weight > 0.001f`), ensure the state is marked playing:
   ```csharp
   layer.SetWeight(weight);
   if (weight > 0.001f && layer.CurrentState != null && !layer.CurrentState.IsPlaying) {
       layer.CurrentState.IsPlaying = true;
   }
   ```



