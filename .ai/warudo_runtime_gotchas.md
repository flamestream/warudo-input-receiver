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

### The Intermediate Transform Pattern (Decoupling User Space from IK Space)
While Warudo humanoid IK targets require `(0, 90, 0)` (left hand) and `(0, -90, 0)` (right hand) to orient the avatar's palms down and fingers forward, forcing user-facing ports or serialized data to store `90°` / `-90°` causes significant usability and animation issues:
1. **Euler Axis Transposition**: In a 90° rotated frame, Pitch (X) tilts the hand sideways like roll, and Roll (Z) tilts fingers up/down like pitch. Users adjusting rotational sliders in the inspector experience unintuitive behavior.
2. **Visual Edit Gizmo Orientation**: In visual setup mode, a gizmo at `Rotation = (0, 0, 0)` points sideways (T-pose rest axis) rather than forward toward the keyboard or prop.
3. **Key Reach Offset Trigonometry**: Typing reach offsets $\vec{\Delta}_{\text{Rot}}$ become mathematically coupled to the 90° yaw offset.

#### The Solution: Dedicated Intermediate Transform Functions
Define static conversion helpers on the receiver asset to translate between clean user coordinates (where `(0, 0, 0)` = straight forward) and Warudo's internal limb IK orientation:

```csharp
public static readonly Vector3 HAND_IK_INTERMEDIATE_ROTATION_LEFT = new Vector3(0f, 90f, 0f);
public static readonly Vector3 HAND_IK_INTERMEDIATE_ROTATION_RIGHT = new Vector3(0f, -90f, 0f);

public static Vector3 ApplyHandIKIntermediateRotation(bool isLeft, Vector3 userRotation) {
    var intermediateQ = isLeft 
        ? Quaternion.Euler(HAND_IK_INTERMEDIATE_ROTATION_LEFT) 
        : Quaternion.Euler(HAND_IK_INTERMEDIATE_ROTATION_RIGHT);
    var userQ = Quaternion.Euler(userRotation);
    return Helper.NormalizeRotationAngles((userQ * intermediateQ).eulerAngles);
}

public static Vector3 RemoveHandIKIntermediateRotation(bool isLeft, Vector3 ikRotation) {
    var intermediateQ = isLeft 
        ? Quaternion.Euler(HAND_IK_INTERMEDIATE_ROTATION_LEFT) 
        : Quaternion.Euler(HAND_IK_INTERMEDIATE_ROTATION_RIGHT);
    var ikQ = Quaternion.Euler(ikRotation);
    return Helper.NormalizeRotationAngles((ikQ * Quaternion.Inverse(intermediateQ)).eulerAngles);
}
```

- **In the Inspector & Gizmo**: `RestRotation` and `KeyBinding.DeltaRotation` are stored and displayed in user space `(0, 0, 0)`. The visual setup gizmo anchor is positioned and rotated in user space so its blue Z-axis points straight forward.
- **In the Runtime Update Loop**: Right when writing to the Warudo `AnchorAsset` driving the limb IK target:
  ```csharp
  leftAnchor.Transform.Rotation = ApplyHandIKIntermediateRotation(true, currentLeftHandRot);
  ```
  The avatar's wrist turns forward naturally while the user's data remains clean and decoupled.

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

---

## 18. Unity Built-in `Standard` Shader Ignores `_Cull = Off` (Two-Sided Geometry Requirement)

### The Phenomenon
Procedural flat 2D meshes (such as floating keyboard keys, HUD panels, or hologram pads) are created and configured with transparent materials using Unity's built-in `Standard` PBR shader:
```csharp
mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
```
When looking at the meshes from the front, they appear properly. However, when the camera is positioned behind or underneath them, the meshes disappear completely.

### Root Cause Analysis
Unity's built-in `Standard.shader` does not expose a `[Cull]` material property keyword or dynamic uniform pass in its ForwardBase / ForwardAdd passes; it hardcodes `Cull Back` (or defaults to it) in the shader source. Calling `mat.SetInt("_Cull", 0)` only sets an unused shader property in the material's property table, which the compiled GPU rasterizer stage completely ignores.

### Architectural Rule
Any procedural 2D mesh generated for use with the `Standard` shader that needs to be visible from both sides **must generate explicit reverse-wound back-face triangles** in the mesh index buffer:
```csharp
// Front face (clockwise when viewed from +Y)
triangles[i * 6 + 0] = 0;
triangles[i * 6 + 1] = i + 1;
triangles[i * 6 + 2] = next + 1;

// Back face (clockwise when viewed from -Y)
triangles[i * 6 + 3] = 0;
triangles[i * 6 + 4] = next + 1;
triangles[i * 6 + 5] = i + 1;
```
Because front-face and back-face triangles share the same vertex positions and UVs, zero extra vertex buffer storage is required, and both sides render cleanly without depending on shader culling state.

---

## 19. Hot-Reload Invalidation & High-Frequency Mesh Rebuild Loop (Input Strobing)

### The Phenomenon
When holding down an input key or button, the corresponding virtual visual element violently flickers/strobes between unpressed (idle color) and pressed (bright/active color) at 60–120 FPS. Severe frame drops, micro-stutter, and heavy garbage collection allocations occur simultaneously.

### Root Cause Analysis
To ensure procedural visuals regenerate after Playground hot-reloading, an auto-restoration check was added to `OnUpdate()`:
```csharp
if (leftKeysGroupGo == null) {
    UpdateFloatingKeysState(); // Rebuilds all meshes!
}
```
However, if the user had all visual elements assigned to `RightHand` or `KeyboardRoot`, `leftKeysGroupGo` was never instantiated. As a result:
1. `leftKeysGroupGo == null` evaluated to `true` on **every single frame**.
2. Every frame, `UpdateFloatingKeysState()` destroyed and rebuilt all 28+ GameObjects, MeshFilters, and Materials.
3. Upon recreation in `RebuildVirtualKeyMesh()`, the key defaulted to `false` (unpressed).
4. One line later in the frame, input processing detected the held key and set it to `true` (pressed).
5. On the next frame, the cycle repeated: destroy -> recreate (false) -> set pressed (true), producing a 60 FPS strobing flicker and allocating thousands of objects per second.

### Architectural Rule
1. **Never check a container/group GameObject for auto-restoration**; keys or props can be anchored to different parent nodes.
2. **Check leaf entity runtime objects**: Check if the actual child entities have their runtime GameObject missing:
   ```csharp
   if (VirtualKeys != null && VirtualKeys.Length > 0 && VirtualKeys[0] != null && VirtualKeys[0].KeyGameObject == null) {
       UpdateFloatingKeysState();
   }
   ```
   Once constructed on the first frame, `VirtualKeys[0].KeyGameObject` remains valid until the next domain reload.
3. **Hook `Watch(nameof(IsEnabled))` in `OnCreate()`**:
   ```csharp
   Watch(nameof(IsEnabled), delegate { UpdateFloatingKeysState(); });
   ```
   During initial scene loading and hot-reload deserialization, `IsEnabled` transitions to `true` after `OnCreate()` completes, triggering visual creation reliably.

---

## 20. PBR Albedo Dimming vs. Self-Illumination / Color-Picker Fidelity

### The Phenomenon
When a user sets an appearance color in a color picker to pure red `#FF0000` (`255, 0, 0`), the 3D object appears dark maroon or washed out on screen rather than pure red.

### Root Cause Analysis
In Unity's `Standard` physically based shader, setting `material.color` configures the diffuse **Albedo**. In the rendering equation:
$$\text{Pixel Color} = \text{Albedo} \times (\text{Direct Lights} + \text{Ambient Light}) + \text{Emission}$$
If the scene has dim ambient light (e.g. 15–20% ambient) and the object is tilted away from directional key lights, `Albedo (1.0, 0, 0) * Ambient (0.2)` results in dark maroon `(0.2, 0, 0)`. The color picker value is physically darkened by the scene lighting.

### Architectural Rule
Provide a normalized `SelfIllumination` scale (0.0 = fully lit PBR, 1.0 = fully unlit / self-illuminating):
```csharp
// When SelfIllumination = 1, diffuse albedo is scaled to 0 so scene shadows cannot darken it.
// The base color is emitted directly, with any additional glow/bloom intensity added on top:
Color albedo = new Color(
    baseColor.r * (1f - selfIllum),
    baseColor.g * (1f - selfIllum),
    baseColor.b * (1f - selfIllum),
    baseColor.a
);
Color emission = (baseColor * selfIllum) + (glowColor * glowIntensity);

material.color = albedo;
material.SetColor("_EmissionColor", emission);
```
**Critical GI Performance Rule**: Always disable realtime emissive GI contributions on dynamic interactive objects:
```csharp
material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
```
If left at `MaterialGlobalIlluminationFlags.RealtimeEmissive`, modifying `_EmissionColor` during key presses or transitions marks Warudo's realtime Enlighten/GI buffer as dirty on every frame, degrading performance.

---

## 21. Zero-Allocation Dynamic Mesh Morphing vs. Mesh Rebuilding

### The Phenomenon
When animating procedural properties (such as corner roundness and border thickness during key press transitions), calling `mesh = CreateRoundedRectMesh(...)` every frame causes garbage collection pauses and frame hitching.

### Root Cause Analysis
Creating a new `Mesh` allocates a new native DirectX/OpenGL buffer and C# heap arrays for vertices, UVs, normals, and triangles.

### Architectural Rule
1. Call `mesh.MarkDynamic()` on the mesh at creation time to configure the GPU buffer for dynamic CPU streaming.
2. In-place update only the `vertices` array when dimensions, corner roundness, or border widths morph:
   ```csharp
   public static void UpdateRoundedRectVertices(Mesh mesh, float width, float height, float radius, int cornerSegments = 8) {
       var perimeter = GeneratePerimeterPoints(width, height, radius, cornerSegments);
       var vertices = mesh.vertices;
       vertices[0] = Vector3.zero;
       for (int i = 0; i < perimeter.Count; i++) {
           vertices[i + 1] = perimeter[i];
       }
       mesh.vertices = vertices;
       mesh.RecalculateBounds();
   }
   ```
   Triangle topology and UV coordinate arrays remain untouched.
3. **Layering Transparent Meshes**: When combining an outer border frame over an inner body mesh:
   - Separate them by a slight Y offset (e.g. Body at `Y = 0.0001f`, Border at `Y = 0.0006f`) to prevent depth precision fighting.
   - Use distinct render queues with `_ZWrite = 0`: Body at `renderQueue = 3000`, Border at `renderQueue = 3001` so the border cleanly renders on top regardless of camera viewing angle.

---

## 22. Allocation-Free Input Transitions with DOTween Easing (`DOVirtual.EasedValue`)

### The Phenomenon
When driving smooth visual transitions for high-frequency user inputs (such as 50–100 keyboard keys, gamepad buttons, or MIDI pads being pressed and released rapidly at 60–120 FPS), creating `Tweener` or `Tween` objects via `DOTween.To()` or `transform.DOMove()` produces continuous heap allocations and garbage collection spikes. Furthermore, rapid key mashing causes overlapping tweens to fight each other unless tracked, killed, or recycled through complex pooling logic.

### Root Cause Analysis
- Every call to `DOTween.To()` allocates internal tween controller instances, callback delegates, and linked-list node structures on the managed heap.
- High-frequency input states (e.g. typing at 100+ WPM or playing rhythm games) create and discard dozens of tweens every second.
- Handling distinct transition speeds and easing curves for **Press In** (e.g. fast 0.02s linear snap) versus **Press Out** (e.g. smooth 0.12s cubic decay) with active `Tween` objects requires canceling the active press tween mid-flight, calculating the remaining proportional duration, and spawning a new release tween.

### Architectural Pattern & Rule
Use a normalized scalar progress tracker and sample DOTween's mathematical easing functions statically via `DOVirtual.EasedValue()`:

1. **Track Normalized Scalar Progress**:
   Store a single float `CurrentPressProgress` (0.0 = fully released, 1.0 = fully pressed) on each visual key/item.
2. **Step Progress Linearly in Update Loop**:
   In `OnUpdate()`, step `CurrentPressProgress` towards `targetProgress` (1.0 if down, 0.0 if up) using `Mathf.MoveTowards()`:
   ```csharp
   float targetProgress = isDown ? 1.0f : 0.0f;
   var transition = isDown ? appearance.PressInTransition : appearance.PressOutTransition;
   float duration = Mathf.Max(0.001f, transition.Duration);
   float speed = (1.0f / duration) * Time.deltaTime;

   key.CurrentPressProgress = Mathf.MoveTowards(key.CurrentPressProgress, targetProgress, speed);
   ```
3. **Evaluate DOTween Easing Function Statically**:
   Instead of allocating a tween, evaluate the eased interpolation factor using `DOVirtual.EasedValue()`:
   ```csharp
   float easedT = DOVirtual.EasedValue(0f, 1f, key.CurrentPressProgress, transition.Ease);
   ```
4. **Interpolate Visual Attributes**:
   Use `easedT` to interpolate colors, transform elevation/scale, corner roundness, and border widths:
   ```csharp
   // 100% allocation-free interpolation
   Color currentColor = Color.Lerp(upColor, downColor, easedT);
   Vector4 currentRadii = Vector4.Lerp(upRadii, downRadii, easedT);
   Vector4 currentBorders = Vector4.Lerp(upBorders, downBorders, easedT);
   ```

### Benefits
- **Zero GC Allocations**: No heap allocations, no delegate creations, and no garbage collection overhead during rapid input hammering.
- **Zero Tween Tracking**: No list of active tweens, no `.Kill()` calls, and no risk of orphaned tween leaks.
- **Seamless Reversal**: If a user releases or re-presses a key mid-transition, `Mathf.MoveTowards()` smoothly reverses direction from the exact current progress value without visual snapping or time calculation bugs.

---

## 23. Constant-Topology Procedural Meshes for Zero-Allocation Morphing

### The Phenomenon
When procedural UI components, keys, or props support asymmetric styles (such as individual corner roundness where some corners are sharp `0mm` while others are rounded `5mm`, or varying border widths per edge), re-generating the mesh geometry on style changes or during press transitions often alters the vertex count. This forces rebuilding the entire triangle index buffer (`mesh.triangles = ...`), causing heap allocations and stalling the GPU render pipeline.

### Root Cause Analysis
- Naive arc generators emit fewer vertices for sharp corners (e.g. 1 point) and more vertices for rounded corners (e.g. $N$ arc segments).
- When a corner transitions between sharp and rounded (or between different asymmetric styles), the total vertex count of the mesh fluctuates.
- Unity's `Mesh` API forbids assigning a new `mesh.vertices` array whose length does not match the existing vertex count without completely clearing the mesh and reassigning `mesh.triangles`, which reallocates native DirectX/OpenGL vertex and index buffers.

### Architectural Pattern & Rule
Enforce **fixed vertex count and constant topology** across all geometric configurations:

1. **Fixed Segment Allocation per Corner**:
   Always evaluate a fixed number of perimeter points per corner (e.g. $N = 8$ segments per corner $\times 4$ corners $= 32$ perimeter vertices), regardless of whether the corner radius is $0$ or positive.
2. **Collapse Arc Points at Sharp Corners**:
   When a corner radius is $0$ (sharp corner), evaluate all $N$ arc segment vertices at the exact same geometric corner position $(hw, 0, hh)$:
   ```csharp
   if (cornerRad <= 0f) {
       // Collapse all arc segments to the sharp point without changing vertex count or triangle topology
       for (int s = 0; s <= cornerSegments; s++) {
           points.Add(new Vector3(centerX + hw, 0, centerZ + hh));
       }
   } else {
       // Generate smooth arc
       for (int s = 0; s <= cornerSegments; s++) {
           float angle = ...;
           points.Add(new Vector3(centerX + Mathf.Cos(angle) * cornerRad, 0, centerZ + Mathf.Sin(angle) * cornerRad));
       }
   }
   ```
3. **In-Place Vertex Streaming**:
   Because the vertex count and triangle winding order are invariant, asymmetric corners, border widths, widths, and heights can be morphed dynamically in-place:
   ```csharp
   // Fast dynamic CPU-to-GPU stream: 0 allocations
   VirtualKeyMeshBuilder.UpdateRoundedRectVertices(key.BodyMesh, key.CurrentSize.x, key.CurrentSize.y, key.CurrentCornerRadii);
   ```
   Only `mesh.vertices = verts; mesh.RecalculateBounds();` is executed. The triangle index buffer and UV layout remain unchanged on the GPU.

---

## 24. `StructuredData` Port Assignment: Direct Field Assignment vs. `SetDataInput`

### The Phenomenon
Assigning directly to a C# field on a `StructuredData` item (for example, `LeftHandRestTransform.Scale = Vector3.one;` or `customApp.CornerRadius = 0.25f;`) fails to persist in Warudo's UI or internal evaluation, and can silently be overwritten back to `(0, 0, 0)` or default on domain reload or UI refresh.

### Root Cause Analysis
- Every field marked `[DataInput]` on an `Asset`, `Node`, or `StructuredData` is backed by Warudo's internal `DataInputPortCollection`.
- Direct C# field assignment updates the memory variable temporarily, but completely bypasses the underlying `DataInputPort`.
- Warudo's watcher system, serialization pipeline, and graph evaluations query the `DataInputPort`, which retains its old or uninitialized value.
- When Warudo next synchronizes ports with the frontend or deserializes properties, the port's internal value overwrites the C# field, reverting the programmatic change.

### Architectural Pattern & Rule
1. **Always use `SetDataInput` for programmatic assignment**:
   ```csharp
   transform.SetDataInput(nameof(transform.Scale), Vector3.one, true);
   ```
   This updates the internal port value, triggers registered watchers, marks the scene as dirty, and broadcasts the new value to Warudo's web interface.
2. **Avoid per-frame fallback overwrites**:
   Do **not** place fallback assignments (such as `if (scale == Vector3.zero) scale = Vector3.one;`) inside `OnUpdate()`. Doing so prevents the user from intentionally setting a value to zero (e.g. setting displacement scale to `0` to freeze hand reach). Restrict default fallback assignment strictly to object creation or explicit user actions (such as clicking an `[Apply Preset]` button).

---

## 25. Ergonomic Neutral Keys vs. Reach Displacement in Keyboard Tracking

### The Phenomenon
When testing keyboard tracking, pressing home row keys (such as `A`, `S`, `D`, `F` on the left hand or `J`, `K`, `L`, `;` on the right hand) moves the avatar's fingers but produces zero displacement on the hand or wrist IK anchor, leading to false assumptions that hand tracking is broken.

### Root Cause Analysis
In ergonomic keyboard tracking models, the hand rest position (`LeftHandRestPosition` / `RightHandRestPosition`) is calibrated to sit directly above the home row keys (`A`, `S`, `D`, `F` and `J`, `K`, `L`, `;`). Therefore, the relative offset vector $\Delta\text{Position}$ for these home row keys is intentionally `(0, 0, 0)`. Only the finger overlay animation plays when typing home keys. Non-neutral keys (such as `Q`, `W`, `E`, `R`, `T`, `1`–`5`, `Space`, etc.) have non-zero delta positions (e.g. `T` has $\Delta X = +0.014\text{m}$, $\Delta Z = +0.019\text{m}$) and correctly displace the hand anchor.

### Architectural Pattern & Rule
1. **Verify with Reach Keys**: Always test hand movement by pressing reach keys (`Q`, `W`, `E`, `R`, `T`, `Space`) rather than neutral resting keys.
2. **Per-Hand Displacement Scaling**: Use each hand's individual rest transform scale as the reach multiplier:
   ```csharp
   Vector3 leftScale = GetLeftHandMovementScale();
   Vector3 deltaPos = Vector3.Scale(activeLeftKey.DeltaPosition, leftScale);
   targetLeftPos = restLeftPos + deltaPos;
   ```
   Avoid redundant global multipliers (such as an asset-level `MovementFactor`) that conflict with per-hand rest transform scales.

---

## 26. Scene Deserialization Interception & Asset Version Auto-Migration

### The Phenomenon
As plugins evolve across releases, data port names change, obsolete fields are removed, and data structures are restructured. When users load scenes created in older versions of the plugin, missing ports throw deserialization warnings, renamed ports lose their saved user settings (resetting to defaults), or obsolete port data persists silently in scene files forever.

### Root Cause Analysis
Warudo deserializes scenes by instantiating the asset class, invoking `OnCreate()`, and then passing a [`SerializedAsset`](file:///d:/Code/FS%20Input%20Receiver/src/Assets/ReceiverAsset.cs) to the virtual method `Asset.Deserialize(SerializedAsset serialized)`.
- `SerializedAsset` contains a dictionary `public Dictionary<string, SerializedDataInputPort> dataInputs` containing raw JSON string values keyed by port name.
- Standard deserialization blindly maps dictionary keys to existing `[DataInput]` ports on the C# class. Any renamed key is skipped (leaving the new port uninitialized), and deleted keys are discarded or left unhandled.

### Architectural Pattern & Framework
All input receivers inherit from [`ReceiverAsset`](file:///d:/Code/FS%20Input%20Receiver/src/Assets/ReceiverAsset.cs), which implements a two-phase auto-migration pipeline by overriding `Deserialize(SerializedAsset serialized)`:

```csharp
public abstract class ReceiverAsset : Asset {
    protected virtual int CURRENT_ASSET_VERSION => 1;

    [Hidden]
    [DataInput]
    public int AssetVersion = 1;

    public override void Deserialize(SerializedAsset serialized) {
        int savedVersion = 0;
        if (serialized?.dataInputs != null && serialized.dataInputs.TryGetValue(nameof(AssetVersion), out var versionPort)) {
            if (!string.IsNullOrEmpty(versionPort?.value) && int.TryParse(versionPort.value, out int parsed)) {
                savedVersion = parsed;
            }
        }

        // Phase 1: Pre-deserialization migration on raw serialized dictionaries
        if (savedVersion < CURRENT_ASSET_VERSION) {
            OnPreMigrateAsset(savedVersion, CURRENT_ASSET_VERSION, serialized);
        }

        // Standard Warudo port deserialization
        base.Deserialize(serialized);

        // Phase 2: Post-deserialization migration on typed C# objects
        if (savedVersion < CURRENT_ASSET_VERSION) {
            OnPostMigrateAsset(savedVersion, CURRENT_ASSET_VERSION);
            AssetVersion = CURRENT_ASSET_VERSION;
            BroadcastDataInput(nameof(AssetVersion));
            Log($"Auto-migrated asset '{Name}' from v{savedVersion} to v{CURRENT_ASSET_VERSION}.");
        }
    }
}
```

### Migration Guidelines
1. **Pre-Deserialization (`OnPreMigrateAsset`)**:
   - Use for **port renaming** (`MigrateRenamePort(rawSerialized, "OldName", "NewName")`).
   - Use for **stripping deleted/obsolete ports** (`MigrateRemovePort(rawSerialized, "ObsoletePort")`).
   - Use for JSON payload transformations before Warudo parses types into memory.
2. **Post-Deserialization (`OnPostMigrateAsset`)**:
   - Runs after all ports, arrays, and `StructuredData` objects are deserialized into memory.
   - Use typed C# methods and properties to recalculate defaults, transform coordinate spaces, or heal zero values using `SetDataInput(..., true)`.
3. **Automatic Persistence**:
   - Once migrated, `AssetVersion` is updated to `CURRENT_ASSET_VERSION`.
   - When the user saves the scene (`Ctrl+S`), Warudo serializes the clean, upgraded state, permanently retiring the old schema with zero legacy crutches in code.

---

## 27. `ICollapsibleStructuredData`, Default Collapsed State (`CollapsedSelf`), & Avoiding Accordion Recursion

### The Phenomenon
1. **Unwanted Expanded Accordions by Default**: When creating structured data implementing `ICollapsibleStructuredData` to keep complex inspector panels clean, newly added or loaded cards appear completely expanded by default. Users must manually collapse multiple cards every time they inspect the asset.
2. **Nested Accordion Recursion**: When a parent structured data card (e.g. `KEYSET_TRANSFORMS`) contains child structured data fields (e.g. `Left`, `Right`, `Root`), if the child types also implement `ICollapsibleStructuredData`, Warudo wraps each child in its own collapsible accordion card. This forces the user to navigate a "Russian nesting doll" hierarchy (expanding the parent card, then expanding each child card individually) just to adjust basic properties like displacement or scale.

### Root Cause Analysis
In `Warudo.Core.dll`:
- `ICollapsibleStructuredData` requires `string GetHeader()` and `Guid Id { get; }`. When implemented, Warudo's frontend renders the structured data inside a `<CollapsibleCard>` accordion component with fold arrow and header text.
- `StructuredData` defines a property `public bool CollapsedSelf { get; set; }` backed by `<CollapsedSelf>k__BackingField`.
- In standard C#, auto-initialized booleans default to `false`. Therefore, newly instantiated structured data has `CollapsedSelf == false` (expanded).
- During serialization, `StructuredData.Serialize()` writes `public bool collapsed;` directly from `CollapsedSelf` into `SerializedStructuredData`.
- If a child structured data implements `ICollapsibleStructuredData`, Warudo recursively renders collapsible card wrappers for each child level.

### Architectural Patterns & Rules
1. **Collapsing by Default (`CollapsedSelf = true`)**:
   Always set `CollapsedSelf = true;` inside `OnCreate()` or immediately after factory instantiation (`StructuredData.Create<T>()`):
   ```csharp
   public class VirtualKeyPresetConfig : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
   {
       ...
       protected override void OnCreate() {
           base.OnCreate();
           CollapsedSelf = true; // Collapsed by default upon creation and scene load
       }
   }
   ```
2. **Decoupling Inline vs. Collapsible Transforms (Preventing Accordion Recursion)**:
   Never make core data primitives (like `VisualSetupTransform`) implement `ICollapsibleStructuredData` globally if they are meant to be embedded inside parent structures.
   Instead, use a two-tier type hierarchy:
   ```csharp
   // Tier 1: Core non-collapsible structured data (renders inline inside parent cards)
   public class VisualSetupTransform : StructuredData {
       [DataInput] public Vector3 Position;
       [DataInput] public Vector3 Rotation;
       [DataInput] public Vector3 Scale;
       ...
   }

   // Tier 2: Collapsible subclass for standalone top-level ports on the asset root
   public class CollapsibleVisualSetupTransform : VisualSetupTransform, ICollapsibleStructuredData {
       public virtual string GetHeader() {
           var title = !string.IsNullOrEmpty(LocalizationKey) ? LocalizationKey.Localized() : "TRANSFORM".Localized();
           return $"{title} Pos({Position.x:F3}, {Position.y:F3}, {Position.z:F3})";
       }
   }
   ```
3. **Compound Container Hierarchy**:
   When grouping secondary setup controls (such as presets and cluster offsets) with main items (such as key definitions):
   - Wrap secondary controls in their own collapsible sub-structs with `CollapsedSelf = true`.
   - Embed them at the top of the main container (e.g. `Floating Key Layout`), keeping the primary workflow immediately accessible while keeping setup utilities neatly tucked away.

---

## 28. Visual Setup Reference Origin Visualization & Prop Occlusion (`AlwaysOnTop`)

### The Phenomenon
When positioning anchors in 3D visual setup mode (such as `VisualSetupTransform`), the user manipulates a temporary gizmo anchor in the viewport. However:
1. **Ambiguous Reference Origin**: The user often does not know what coordinate space the anchor is moving relative to (e.g. Is this relative to the Character avatar root, the Keyboard Origin, or the Hand Rest Pose?).
2. **Prop Occlusion**: In typical VTuber setups, 3D props (desks, computer monitors, mechanical keyboards) occupy the space where hands and anchors rest. An origin marker placed at the parent transform's position is completely occluded inside the 3D desk mesh and invisible to the user.

### Architectural Patterns & Solutions
1. **Explicit Origin Text in Gizmo Instructions**:
   In `OnCreateAnchor`, include an explicit description of the parent anchor in `setupAnchor.Instructions`, which renders as an on-screen guide in the Warudo viewport:
   ```csharp
   setupAnchor.Instructions = "Position, rotate, and scale the **Left Hand Rest Pose**, relative to the **Keyboard Origin**.\nThe red sphere indicates the origin location (Keyboard Origin).\nClick **[Apply Transform]** when finished.";
   ```
2. **Procedural Origin Marker (Red Sphere Indicator)**:
   Instantiate a temporary unlit or emissive red sphere (`GameObject.CreatePrimitive(PrimitiveType.Sphere)`) placed at `Vector3.zero` of the parent transform (or the reference origin):
   ```csharp
   debugRestSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
   debugRestSphere.transform.SetParent(parentTransform, false);
   debugRestSphere.transform.localPosition = originPos;
   debugRestSphere.transform.localScale = Vector3.one * 0.015f; // 1.5cm sphere
   ```
   Always strip the `Collider` component (`Destroy(sphere.GetComponent<Collider>())`) so it does not interfere with scene picking or physics.
3. **Always-On-Top Depth Rendering Through Props**:
   To prevent 3D prop geometry from hiding the reference marker, provide an **Always On Top** toggle in the transform's **ADVANCED** section:
   - When enabled, assign a material with an overlay render queue (`renderQueue = 4000` or higher) and `ZTest Always` / unlit shader.
   - When disabled, restore standard Z-buffering (`renderQueue = 2000`, `ZTest LEqual`).
   - Clean up debug markers immediately in `OnExitVisualSetup()` or asset destruction.

---

## 29. Asset Versioning Lifecycle Policy: Active Development vs. Production Releases

### The Dilemma
When an asset schema is refactored (e.g. ports renamed, default rotations changed, structures modularized into child classes), developers often rush to increment `CURRENT_ASSET_VERSION` (e.g. from 1 to 2, 3, 4) and write elaborate `OnPreMigrateAsset` and `OnPostMigrateAsset` migration routines.

However, during active pre-release development branches (such as `version-5.5` before public distribution):
1. **Schema Instability**: Ports and structures may be reorganized multiple times within the same week. Chaining migrations for unreleased intermediate schemas creates fragile, untested dead code.
2. **Unnecessary Code Bloat**: Migration routines for schemas that no real user ever possessed waste maintenance time and add startup overhead.

### The Rule
- **During Active Pre-Release Feature Branches**:
  Keep `CURRENT_ASSET_VERSION => 1;` throughout the feature development cycle.
  Do not write migration code for unreleased assets. Standard default values, sane fallbacks, and preset applicators should handle initial state cleanly.
- **Post-Release Versioning**:
  Only bump `CURRENT_ASSET_VERSION` and implement two-phase auto-migration when a breaking schema change occurs **after** a version has been officially packaged and released to users in production.

---

## 30. Duplicate `[Section]` Attributes & Dynamic `sectionTitle` Cause Duplicate Section UI Banners

### The Phenomenon
In Warudo's inspector, the user suddenly sees multiple sections with the identical title appearing in sequence (e.g. three separate "FLOATING_KEYS" header banners, or two "ADVANCED [DEBUG MODE]" banners).

### Root Cause Analysis
Warudo generates inspector UI by sequentially scanning ports:
1. **Static Attributes**: When reflection encounters a member decorated with `[Section("SECTION_NAME")]`, Warudo closes the previous section container and begins a **new** section block with that header title.
   - If a subsequent port or trigger method in the same section is *also* decorated with `[Section("SECTION_NAME")]`, Warudo interprets each instance as a command to start a new section container.
2. **Dynamic Runtime Broadcasting (`port.Properties.sectionTitle`)**:
   - Setting `port.Properties.sectionTitle` dynamically and broadcasting via `BroadcastTriggerProperties` / `BroadcastDataInputProperties` does **NOT** work as a passive "category tag" that groups items together.
   - Instead, setting a non-empty `sectionTitle` on a port dynamically designates that port as the **start boundary** of a section container.
   - If multiple ports in the same section broadcast a non-empty `sectionTitle` (e.g. setting `deactPort.Properties.sectionTitle = title; resetPort.Properties.sectionTitle = title;`), Warudo treats *both* as section starts, creating duplicate section banners in the inspector.

### The Rule
1. **Static Members**: **Only annotate the FIRST member of a section with `[Section("SECTION_NAME")]`.** All subsequent members in that section must omit `[Section]` so they fold into the active container.
2. **Dynamic Section Title Updates**:
   - **Never transfer `sectionTitle` to a port that can become hidden**: When a port that was assigned a section title becomes `hidden: true`, Warudo's Vue UI keeps the existing section container (because sibling ports are still in it) but will **not** rename it when another port later claims the title.
   - **Always let a permanent, visible port own the section title**:
     ```csharp
     // TriggerResetAnchors is permanent (always visible) and owns [Section("ADVANCED")]:
     resetPort.Properties.sectionTitle = isDebug ? "ADVANCED_DEBUG_MODE".Localized() : "ADVANCED".Localized();
     BroadcastTriggerProperties(nameof(TriggerResetAnchors));

     // Child port toggles hidden only, keeping sectionTitle = "":
     deactPort.Properties.hidden = !isDebug;
     deactPort.Properties.sectionTitle = "";
     BroadcastTriggerProperties(nameof(TriggerDeactivateAllDebugKeys));

     // CRITICAL: BroadcastTriggerProperties only updates individual button state.
     // Call Broadcast() on the asset to force Warudo's UI to re-render the section container in place:
     Broadcast();
     ```

---

## 31. Visual Calibration Mode Non-Destructive Lifecycle (Reset to Origin & Cancel Rollback)

### The Phenomenon
When building an interactive visual calibration mode (spawning a temporary `VisualSetupAnchorAsset` gizmo in 3D to let users visually reposition/rotate transforms):
- Users wanted a `↺ Reset to Origin` button during calibration to easily return the calibration anchor to zero.
- However, if the reset button called `Parent.SetDataInput(nameof(Position), Vector3.zero)`, the parent asset's serialized data was overwritten immediately in memory.
- If the user subsequently clicked `[Cancel Transform]`, the session exited, but the original transform values were already gone and could not be restored.
- Furthermore, exposing `[Trigger] ResetToOriginTrigger` directly on the `VisualSetupTransform` class caused a redundant reset button to appear in the main inspector's data display outside of visual setup mode.

### Architectural Rules
1. **Calibration Triggers Belong Exclusively on the Temporary Anchor**:
   Triggers intended strictly for the calibration session (`[Trigger] ResetTrigger` ➔ `RESET_TO_ORIGIN`) must be declared on `VisualSetupAnchorAsset` (order index `[Trigger(2)]`, placed directly above `[Trigger(3)] CANCEL_TRANSFORM`), **not** on the base `VisualSetupTransform` class. This keeps the asset inspector clean and prevents confusion outside of visual setup mode.
2. **Never Mutate Asset State During Calibration**:
   Calibration manipulations and reset helpers must exclusively mutate `setupAnchor.Transform` (the temporary 3D anchor gizmo) and any preview GameObjects:
   ```csharp
   public void ResetAnchorToOrigin() {
       if (!InVisualSetupMode || setupAnchor == null) return;
       if (OnResetAnchor != null) {
           OnResetAnchor.Invoke(this, setupAnchor);
       } else {
           setupAnchor.Transform.Position = Vector3.zero;
           setupAnchor.Transform.Rotation = Vector3.zero;
           setupAnchor.Transform.Scale = Vector3.one;
           setupAnchor.Transform.Broadcast();
       }
   }
   ```
   Do **not** call `SetDataInput` or mutate `Position`, `Rotation`, or `Scale` on the parent asset during `ResetAnchorToOrigin`.
3. **Commit Strictly on Apply, Rollback on Cancel**:
   - `Apply(anchor)`: Copies `anchor.Transform` to the parent `VisualSetupTransform`, calls `OnApplyAnchor`, and commits values via `SetDataInput` / `BroadcastDataInput`.
   - `ExitVisualSetup(false)`: Discards the anchor without committing. Since the asset's fields were never modified, the original values are preserved 100%. Handlers (e.g. `OnExitVisualSetup(isApplied)`) should restore live preview objects to the original committed values when `!isApplied`.

---

## 32. Decoupling Visual Key & Prop Rigs from Intermediate Humanoid Hand IK Rotations

### The Phenomenon
In Warudo, humanoid avatar wrist/hand IK targets require intermediate rotations (e.g. `(0f, 90f, 0f)` for Left Hand, `(0f, -90f, 0f)` for Right Hand) so the avatar's palm faces forward/downward toward a desk or workstation.
If props, keyboard clusters (e.g. split keyboard halves), or visual meshes are parented directly to Hand IK anchors or evaluate their coordinate frame inside the Hand IK target's local rotation space:
- Setting the cluster's rotation to `Vector3.zero` causes the geometry to face 90 degrees sideways or upside down.
- Users are forced to apply counter-rotations (e.g. `-90` degrees yaw) just to make a keyboard sit flat on a desk.

### The Fix: Parent to Workstation Root & Offset by Hand Rest Poses
1. Parent prop/key cluster GameObjects (e.g. `leftKeysGroupGo`, `rightKeysGroupGo`) to the workstation root (`RootAnchor`), **not** to the Hand IK targets.
2. Calculate world/local placement relative to the resting hand position using resting hand Euler angles, completely bypassing the intermediate IK wrist twists:
   ```csharp
   var basePos = LeftHandRestPosition;
   var baseRot = Quaternion.Euler(LeftHandRestRotation);
   leftKeysGroupGo.transform.localPosition = basePos + baseRot * LeftKeysetTransform.Position;
   leftKeysGroupGo.transform.localRotation = baseRot * Quaternion.Euler(LeftKeysetTransform.Rotation);
   leftKeysGroupGo.transform.localScale = LeftKeysetTransform.Scale;
   ```
3. At `Position = Vector3.zero` and `Rotation = Vector3.zero`, the prop or keyset aligns perfectly flat and straight with the workstation rest pose without fighting the humanoid wrist IK standard.

---

## 33. Dynamic Section Header Updating & Inspector Re-rendering via `Entity.GetVersion()`

### The Phenomenon
When dynamically modifying `port.Properties.sectionTitle` in C# (for example, renaming a section from `"Advanced"` to `"Advanced ⚠️[DEBUG MODE]"`), calling `BroadcastTriggerProperties` or `this.Broadcast()` updates port properties, but Warudo's Vue UI inspector never updates the section header text on screen. The new section title only appears if the user clicks away to another asset in the scene hierarchy and then clicks back to the asset.

Conversely, attempting to force a refresh with `Context.Service?.BroadcastOpenedScene()` causes a disruptive modal "Waiting..." loading overlay because the frontend interprets it as a complete scene reload.

### Root Cause Analysis
In Warudo's frontend (`warudo-client-electron`):
1. The inspector component (`EntityInspector`) builds sections using `createSections(inputs.value)`.
2. This `build()` function is watched by two triggers:
   ```javascript
   watch(() => props.entityId, build);
   watch(() => entity.value?.version, build);
   ```
3. In `Warudo.Core.Data.Entity`, the base implementation of `GetVersion()` is:
   ```csharp
   public virtual long GetVersion() { return 0L; }
   ```
   Because `GetVersion()` constantly returns `0L`, every serialized `FrameUpdate` sends `version: 0`. The Vue reactive watcher `entity.value?.version` never sees a change, so `build()` is never invoked on `Broadcast()`. Switching to another asset changes `props.entityId`, which is why navigating away and back forced a rebuild.

### Architectural Solution
1. In `ReceiverAsset`, introduce a `uiVersion` counter and override `GetVersion()`:
   ```csharp
   protected long uiVersion = 0;

   public override long GetVersion() {
       return base.GetVersion() + uiVersion;
   }

   public void RequestUiRefresh() {
       uiVersion++;
       Broadcast();
   }
   ```
2. Whenever section headers or schema layouts change dynamically:
   - Update `port.Properties.sectionTitle`.
   - Call `RequestUiRefresh()`.
3. Warudo serializes the asset with the incremented `version`. The Vue client detects `entity.value.version` changing and re-runs `build()`, instantly updating all section titles and schema containers in-place without reloading the scene or showing any "Waiting..." overlay.

---

## 34. Warudo Trigger Button Label Reactivity in Vue (`TriggerInterfaceComponent`)

### The Phenomenon
When dynamically modifying `port.Properties.label` on a `TriggerPort` (e.g. toggling button text between `"Force Key"` and `"Release Key"`) and calling `BroadcastTriggerProperties`, the button's `description` updates immediately on screen, but the button text itself does not update until the user interacts with another element or forces a component re-mount.

### Root Cause Analysis
In Warudo's web client (`warudo-client-electron`):
1. In `TriggerInterfaceComponent.vue`:
   - The button text is rendered as `toDisplayString(_ctx.intf.name)`.
   - The description is rendered as `toDisplayString(_ctx.intf.portModel.properties.description)`.
2. When receiving `BroadcastTriggerProperties`, Vue updates `portModel.properties`, so `description` reactively re-renders immediately.
3. However, `intf.name` is a plain string field on `TriggerInterface` initialized only once inside `build()` (`new TriggerInterface(port)`).
4. Because `intf.name` is not a reactive getter pointing to `portModel.properties.label`, changing the label in C# does not trigger a Vue text node re-evaluation on the button until the interface is re-created.
5. In addition, base `StructuredData.Serialize()` omits assigning `serialized.version = GetVersion()`, so `entity.version` is always 0 on `StructuredData` unless explicitly overridden.

### Architectural Solution: Async `RequestUiRefresh()` & `Serialize()` Override on StructuredData
1. Override `Serialize()` and `GetVersion()` on the `StructuredData`:
   ```csharp
   protected long uiVersion = 0;

   public override long GetVersion() {
       return base.GetVersion() + uiVersion;
   }

   public override SerializedStructuredData Serialize() {
       var serialized = base.Serialize();
       serialized.version = GetVersion();
       return serialized;
   }
   ```
2. Implement an **async yield-first** `RequestUiRefresh()` using `UniTask.Yield()`:
   ```csharp
   public async void RequestUiRefresh() {
       await UniTask.Yield();
       uiVersion++;
       Broadcast();
   }
   ```
3. Whenever changing `port.Properties.label`:
   - Use `.Localized()` (e.g. `"DEBUG_RELEASE_KEY".Localized()`).
   - Call `BroadcastTriggerProperties(...)`.
   - Call `RequestUiRefresh()`.
4. Yielding first (`await UniTask.Yield()`) waits for the next frame so Vue finishes processing the active click event and clears its callstack before receiving the version increment. On the next tick, `watch(() => entity.value?.version, build)` cleanly fires to recreate `inputs` with the updated button label.

---

## 35. Multi-Element Visual Setup Guides: Z-Ordering in Always On Top Mode & Per-Frame Visibility

### The Phenomenon
1. When multiple visual helpers (e.g. Spheres, Quads/Slabs, Cylinders/Lines) all use `Always On Top` shaders (`_ZTest = Always` / `GUI/Text Shader`), objects with identical render queue numbers (`Overlay + 1000`) exhibit camera-distance dependent draw orders. Slabs or lines positioned between the camera and target spheres can draw over the spheres, visually occluding or washing out the spheres.
2. In visual setup modes where GameObjects are synchronized or repositioned per-frame in `UpdateDebugVisuals()`, calling `SetActive(true)` unconditionally overrides the user's `SHOW_ORIGIN` toggle, making hidden guides reappear on the very next frame.

### Architectural Solution: Explicit Render Queue Hierarchy & Master Visibility Guard

1. **Layered `renderQueue` and `sortingOrder`**:
   Assign explicit ascending queue priorities and renderer sorting orders so foreground elements always render on top of background reference geometry:
   - **Background (Lines/Axes)**: `Overlay + 1000`, `sortingOrder = 1000`.
   - **Middle (Planes/Slabs)**: `Overlay + 2000`, `sortingOrder = 2000`.
   - **Foreground (Spheres/Fingertip/Key Targets)**: `Overlay + 3000`, `sortingOrder = 3000`.

2. **Master Visibility Guard**:
   At the start of `UpdateDebugVisuals()`, check the master visibility boolean:
   ```csharp
   if (!VisualGuidesVisible) {
       if (debugKeyCenterSphere != null && debugKeyCenterSphere.activeSelf) debugKeyCenterSphere.SetActive(false);
       // ... deactivate all guides
       return;
   }
   ```
   This guarantees that `SHOW_ORIGIN` completely silences the entire visual setup subsystem without per-frame flickering or unintended reactivation.

---

## 36. StructuredData Port Defaults, Constructor Reflection, and Warudo's UI Reset Button

### The Phenomenon
1. A developer creates a generic `StructuredData` class (e.g. `GuideSphere`) with `public Color Color = Color.white;`.
2. When creating specialized instances (e.g. `FingerTipSphere`, `KeyCenterSphere`, `HandSlabTransform`), the developer passes a setup lambda or calls a configuration method (e.g. `StructuredData.Create<GuideSphere>(sp => sp.Color = customGreen)`).
3. In Warudo's UI, the port opens displaying `Color.white` rather than `customGreen`, or if the user clicks the native circular "Reset" button next to the port in the inspector, it resets back to `Color.white`.

### Root Cause
In Warudo's architecture, `DataInputPortCollection` reflects ports by calling `CreateDataInputPortFromReflectionMeta`. During reflection:
- It creates an instance getter and invokes it: `getter.Invoke(instance)`.
- It sets the port's internal `DefaultValue` (`set_DefaultValue(string)`) to the value read at that exact moment.
- If a class defines `public Color Color = Color.white;`, the field value when `CreateEntity` runs is `Color.white`.
- Modifying C# fields inside `StructuredData.Create<T>(Action<T> initializer)` or assigning fields afterwards without `SetDataInput` leaves the underlying `DataInputPort` uninitialized. Even if `SetDataInput` is called later, `port.DefaultValue` remains `Color.white`, so Warudo's UI Reset button will revert to white.

### Architectural Solution: Dedicated Subclasses with Constructor Initializers
Define specialized concrete subclasses for ports that need distinct defaults:
```csharp
public class FingerTipGuideSphere : GuideSphere {
    public FingerTipGuideSphere() {
        Color = new Color(0.1f, 0.95f, 0.2f, 0.9f);
        RadiusFactor = 1.0f;
        Glow = true;
    }
}
```
Because the subclass constructor runs immediately upon `new FingerTipGuideSphere()` before `CreateDataInputPortFromReflectionMeta` reflects the fields:
1. `getter.Invoke(instance)` reads the customized color/position.
2. Warudo captures the customized values directly into `port.DefaultValue`.
3. The UI immediately displays the intended color, and clicking Warudo's "Reset" button cleanly restores the intended configuration.

---

## 37. Visual Calibration Architecture: Decoupling Anchors, Guides, and Milestone Thresholds

### The Phenomenon
1. When mixing coordinate reference indicators (e.g. parent anchor origin sphere) with alignment guides (e.g. fingertip tracking sphere, planar contact slabs, line pivots), tying all visuals to a single `ShowOrigin` or `AlwaysOnTop` toggle conflates coordinate debugging with alignment workflow.
2. In keyboard key binding calibration, users need one-click alignment (pitch/roll planar parallelism, fingertip placement, forearm yaw pivot) and fine-grained distance/angle thresholds.
3. Placing loose float threshold fields directly on the root asset clutters the UI and lacks visual hierarchy.

### Architectural Solution
1. **Strictly Decouple Origin Indicator vs. Alignment Guides**:
   - `ShowOrigin` & `AlwaysOnTop` strictly control the coordinate origin sphere attached to the parent anchor.
   - `ShowVisualGuides` & `GuidesAlwaysOnTop` control the alignment guides and automatically hide all guide settings and threshold cards when toggled off.
2. **Subclassing `VisualSetupTransform` & Callback Wiring**:
   - Subclasses overriding `CreateAnchor()` must explicitly wire `anchor.OnOriginSphereChange` to ensure changes to the origin sphere's radius factor or color immediately rescale and re-render the primitive.
3. **Structured Milestone Thresholds**:
   - Group thresholds into collapsible `StructuredData` classes (`DistanceAlignmentThreshold`, `AngleAlignmentThreshold`) with `[FloatSlider(min, max, step)]` attributes.
   - The milestone system cleanly splits proximity into 3 distinct zones:
     - **Far $\to$ Approach Span**: Base color.
     - **Approach Span $\to$ Snap-to-White Threshold**: Smooth blend from base color to bright tint.
     - **Snap-to-White Threshold $\to$ Contact**: Pure glowing white.

---

## 38. Batch Sampling Curled Animancer Poses vs. `OnUpdate` Tween Race Conditions

### The Phenomenon
When batch-generating key bindings or auto-aligning multiple fingers simultaneously across 90+ keys (`AUTO_GENERATE_KEY_BINDINGS`):
1. In individual visual setup, `AUTO_ALIGN_KEY_BINDING` positions the activated fingertip with pinpoint accuracy, turning all alignment guides (spheres, slabs, forearm lines) pure white.
2. But batch generation was calculating against the relaxed (un-curled) finger pose, placing solved hand targets too low:
   - Left index finger key `F`: solved $y = 0.0198\text{m}$ instead of $0.0319\text{m}$ ($+1.2\text{cm}$ error).
3. If code attempted to set weights to $1.0\text{f}$ and evaluate synchronously (`animator.Update(0f)`), the finger positions remained unchanged at their rest pose.
4. When introducing an asynchronous frame delay (`await UniTask.Delay(100)`), the avatar's fingers visibly curled for a brief split second, but still recorded the relaxed delta.

### Root Causes

#### 1. Unity PlayableGraph / Animancer Requires Engine Loop Frames
In Unity, Animancer controls character animation via an active `PlayableGraph`. 
Calling `animator.Update(0f)` in a single synchronous tick does not propagate bone matrix evaluations to `Transform.position` or `animator.GetBoneTransform()`. Frame progression in the player loop is required.

#### 2. The `OnUpdate` Reset Race Condition
During asynchronous frame waits (`await UniTask.Delay(150)`), `OnUpdate()` continues executing every frame on the main thread.
Because the user is not actively pressing keys, `OnUpdate()` Section 6 (`ApplyFingerLayerWeight`) observed `targetFingerWeights[f] == 0.0f` and launched release tweens or immediately zeroed out the Animancer layer weights (`SetAnimancerLayerWeight(..., 0f)`).
By the time the delay expired and `GetCurledFingerTipContactVector` was sampled, all layers had already been reset to weight $0$.

#### 3. Animancer Looping & `Events.OnEnd` Resets
When non-looping layers (`Looping = false`) reach `NormalizedTime = 1f` with `IsPlaying = true`, Warudo's `Events.OnEnd` handler triggers and sets `state.IsPlaying = false`, which can rewind or reset the state if not frozen properly.

### Architectural Solution: Batch Protection Guard & Bone-Local Coordinate Invariance

1. **State Protection Guard (`isBatchAutoGenerating`)**:
   - In `OnUpdate()`, wrap layer weight updates in a guard:
     ```csharp
     if (isBatchAutoGenerating) {
         // Keep all 10 finger weights pinned firmly at 1.0f without tweening
         for (int f = 0; f < 10; f++) {
             ...
             SetAnimancerLayerWeight(animancerLayerIdx, 1.0f);
         }
     } else if (Character.OverlappingAnimations != null) {
         for (int f = 0; f < 10; f++) {
             ApplyFingerLayerWeight(finger, f, targetFingerWeights[f]);
         }
     }
     ```
2. **End-Pose Freezing**:
   - Before awaiting frames, kill all runtime tweens (`fingerTweens[i]?.Kill(false)`).
   - Set `state.NormalizedTime = 1f; state.IsPlaying = false;` to hold the animation cleanly at the curled end pose without triggering `Events.OnEnd`.
3. **Bone-Local Coordinate Invariance**:
   - Sample fingertip contact vectors relative to the humanoid hand bone:
     $$\vec{d}_{\text{local}} = \mathbf{Q}_{\text{intermediate}} \times \text{handTransform.InverseTransformPoint}(\vec{P}_{\text{tip}})$$
   - Bone-local evaluation is completely invariant to where the character, workstation, or hand anchors are located in world space, guaranteeing mathematical identity between batch generation and individual key visual calibration.
4. **Clean Teardown**:
   - After sampling, set `isBatchAutoGenerating = false`, restore prior weights/states, and call `ResetFingerWeights()` to ensure clean runtime state.

---

## 39. Rest-Relative Hand Slab & Surface Normal Calibration

### The Phenomenon
1. In individual key visual setup and batch key alignment, the green hand slab was visibly tilted relative to the red keycap slab at resting pose, preventing the slab milestone guide from turning pure glowing white.
2. In `SolveKeyAlignment` and `AlignHandWithButtonSurface`, an artificial $8^\circ\text{–}10^\circ$ tilt was applied to the avatar's hand, twisting the wrist on resting home row keys (`F`, `J`, `A`, `S`, `D`) where the finger was already positioned directly above the button.

### Root Cause
The code was calculating the hand's palm normal as `handNormal = handRot * Vector3.up`.
In Warudo and Unity humanoid rigs, avatar wrist/hand IK targets use intermediate Euler rotations (`Vector3(0, 90, 0)` / `Vector3(0, -90, 0)`) to face palms forward toward props. Consequently:
- The raw $+Y$ axis of the hand anchor does **not** correspond to the palm normal.
- At rest, `handRot * Vector3.up` had an $8^\circ\text{–}10^\circ$ angular difference from the keyboard surface normal `keyNormal`.
- `Quaternion.FromToRotation(handNormal, keyNormal)` interpreted this IK coordinate artifact as a physical misalignment, applying an erroneous tilt rotation to the hand.

### Architectural Solution: Rest-Relative Surface Reference
The avatar's hand at rest is calibrated by the user to be properly aligned over the keyboard. Therefore, the hand's surface normal and slab orientation must always be computed relative to resting rotation $\mathbf{R}_{\text{hand0}}$:

1. **Rest-Relative Rotation Delta**:
   $$\Delta \mathbf{R} = \mathbf{R}_{\text{hand}} \mathbf{R}_{\text{hand0}}^{-1}$$
2. **Rest-Aligned Hand Surface Normal**:
   $$\vec{N}_{\text{hand}} = \Delta \mathbf{R} \times \vec{N}_{\text{key}}$$
3. **Rest-Aligned Hand Slab Orientation**:
   $$\mathbf{R}_{\text{handSlab}} = \Delta \mathbf{R} \times \mathbf{R}_{\text{keyBase}} \times \mathbf{R}_{\text{handSlabRot}}$$
   where $\mathbf{R}_{\text{keyBase}} = \text{FromToRotation}(\vec{u}_y, \vec{N}_{\text{key}})$.

### Benefits
- **Zero Angular Drift at Rest**: At resting pose ($\Delta \mathbf{R} = \mathbf{I}$), $\vec{N}_{\text{hand}} = \vec{N}_{\text{key}}$ and $\mathbf{R}_{\text{handSlab}} = \mathbf{R}_{\text{keySlab}}$, so the hand slab is automatically parallel ($0.0^\circ$ difference) to the keycap slab, immediately turning the slab milestone pure glowing white.
- **Zero False Tilt on Planar Keys**: In `SolveKeyAlignment()`, `tilt = FromToRotation(handNormal, keyNormal)` evaluates to identity for all keys on the keyboard surface. Home row resting keys keep $\Delta\text{Rot} \approx (0, 0, 0)$, and reach keys rotate purely in natural forearm yaw around the surface normal without artificial pitch/roll wrist twist.






---

## 40. `SetCharacterOverlappingAnimations` Crashes to Desktop in Playground Mode When Adding New Layers

### The Phenomenon
When assigning finger animations to a `KeyboardReceiverAsset` for the first time in Warudo Playground mode, Warudo crashes to desktop. The same operation works correctly as a compiled `.warudo` mod.

**Key observations:**
- No crash if layer already exists on the character before our code runs (animation mod already loaded)
- No crash in packaged mod mode under any circumstances
- Crash does NOT happen if no FingerAnimations are configured

### Root Cause — Three-way Race Condition
The crash is caused by **three simultaneous conditions**, all of which must be true:

1. **`BroadcastDataInput(character.OverlappingAnimations)` cascades into Watch(FingerAnimations).**  
   The blueprint graph connects `character.OverlappingAnimations` → downstream nodes → eventually sets `keyboardAsset.FingerAnimations` port. This triggers a **second sync** (`ExecuteSynchronizeAllFingerAnimations`) immediately after the first completes.

2. **The second sync creates a new `OverlappingAnimationData` entry with an animation URI**, triggering `SetupOverlappingAnimations` → **`ModHost.Load <animation>.warudo`** (async animation mod loading).

3. **`OVRLipSyncNode._outputBlendshapeList()` throws `NullReferenceException` every frame** from the face tracking blueprint's `OnUpdateNode` graph (service not fully initialized). Normally Warudo recovers from this gracefully, but when it fires **concurrently with animation mod loading**, the two together crash Warudo to desktop.

### Failed Approaches (for reference)
- `SetDataInput` → immediate `RecursivelySetData` crash via graph traversal
- `BroadcastDataInput` alone → cascades into Watch(FingerAnimations) → infinite sync loop → crash
- `SetValueAtPath` → triggers same graph traversal, same crash
- Direct assign + `await UniTask.Yield()` + `BroadcastDataInput` → mod loads during yield, crash still happens
- Direct assign + `BroadcastDataInput` (even with loaded mods) → Watch re-trigger → infinite sync loop → crash
- Checking loaded mods via `character.OverlappingAnimations?.Animation` → unreliable (field can be stale after mutations)

### Final Solution: No-Op with Warning Toast in Playground Mode

Overlapping animation activation in Playground mode is fundamentally unsafe due to the race condition between UMod animation mod loading, blueprint graph watchers, and face-tracking node initialization. Rather than attempting complex workarounds that can still trigger crashes under various user operations, `Helper.SetCharacterOverlappingAnimations` simply shows a warning toast and no-ops in Playground mode.

```csharp
public static UniTask SetCharacterOverlappingAnimations(CharacterAsset character, CharacterAsset.OverlappingAnimationData[] layers) {
    if (character == null) return UniTask.CompletedTask;

    try {
        if (IsPlaygroundMode) {
            Warudo.Core.Context.Service?.Toast(
                ToastSeverity.Warning,
                "FINGER_ANIM_PLAYGROUND_UNSUPPORTED".Localized(),
                "FINGER_ANIM_PLAYGROUND_UNSUPPORTED_BODY".Localized());
            return UniTask.CompletedTask;
        }

        character.SetDataInput(nameof(character.OverlappingAnimations), layers, true);
    } catch (Exception ex) {
        Log.Error($"[Helper] SetCharacterOverlappingAnimations failed: {ex}");
    }
    return UniTask.CompletedTask;
}
```

**Why this works:**
- In Playground mode: displays a non-intrusive warning toast informing the user to use the compiled mod, and cleanly aborts without mutating `character.OverlappingAnimations`, touching Animancer, or invoking `ModHost.Load`. Zero crash risk.
- In compiled mod mode: standard `character.SetDataInput(...)` executes, updating Animancer layers normally.

### Rule
Always use `Helper.SetCharacterOverlappingAnimations(character, layers)`. Overlapping animations cannot be modified in Playground mode — the user must build and install the compiled mod package (`build-mod.ps1`).

---

## 41. UMod Security Scanner Blocks `System.Reflection` in Packaged Mods

### The Phenomenon
Building a `.warudo` packaged mod fails with:
```
[UMod.BuildEngine.BuildEngineService]: Illegal reference to disallowed namespace: System.Reflection (N)
```
The build succeeds in Playground (Roslyn) mode but is rejected by UMod's `CodeSecurityReport` pipeline during mod packaging.

### Root Cause
UMod's `BuildEngineService` scans all compiled IL for references to the `System.Reflection` namespace and rejects the build if any are found, **regardless of whether the code path is exercised at runtime**.

### Solution: Marked Playground-Only Comment Blocks via `sync.ps1`

Rather than relying on preprocessor defines (which RoslynCSharp in Warudo Playground does not define without custom compiler settings), code that needs reflection in Playground mode is marked with comment delimiters:

```csharp
// [PLAYGROUND_ONLY_START]
if (port != null && IsPlaygroundMode) {
    try {
        var prop = typeof(Warudo.Core.Graphs.DataInputPort).GetProperty("DefaultValue");
        prop?.SetValue(port, value, null);
    } catch {}
}
// [PLAYGROUND_ONLY_END]
```

When [`personal-tools/sync.ps1`](file:///d:/Code/FS%20Input%20Receiver/personal-tools/sync.ps1) mirrors `src` to `unity` (`sync.bat src unity`):
- It scans `.cs` files in Unity.
- Any lines inside `[PLAYGROUND_ONLY_START]` ... `[PLAYGROUND_ONLY_END]` are automatically commented out (`// [OMIT_MOD] ...`).
- When Unity builds the mod package, no `System.Reflection` exists in the compiled IL, passing UMod's security scanner with zero warnings.
- When syncing back from `unity` to `src` (`sync.bat unity src`), `sync.ps1` automatically uncomments `// [OMIT_MOD] ` lines to restore them in source.

### Rule
- Avoid `System.Reflection` whenever possible (e.g. `CharacterAsset.SetupOverlappingAnimations` is public, no reflection needed).
- `SetPortDefaultValue` was removed entirely from the codebase because modifying `DataInputPort.DefaultValue` is impossible without reflection. Port defaults must be specified via standard C# property/field initializers on the class.
- If Playground-only reflection is ever needed in the future, always wrap it in `// [PLAYGROUND_ONLY_START]` and `// [PLAYGROUND_ONLY_END]`.
- Always use `sync.bat src unity` before running `build-mod.ps1` or packaging in Unity.

---

## 42. Key Origins Scaling & Dynamic Hand IK Reach Compensation

### The Phenomenon
When scaling `Key Origins` (`Center Key Origin`, `Left Key Origin`, `Right Key Origin`), applying naive scale transformations to hand IK reach targets causes severe distortion:
1. **Vertical/Spatial Blow-up**: If wrist positions are scaled relative to the rig root `(0, 0, 0)` on the floor, the elevation of the keyboard (e.g., table height `Y = 0.85m`) is multiplied by the scale factor, launching the hands high into the air or sinking them into the floor.
2. **Arm Geometry Distortion**: In Warudo, `Attachable` does not propagate scale to children or IK targets. More importantly, the avatar's arms, forearms, and fingers have fixed skeletal lengths and **do not scale** when the keyboard scales. If the reach offset is scaled proportionally from the origin, the hand target can easily exceed the physical reach limit of the avatar's arm.
3. **Key Drift Mismatch**: When the keyboard mesh or cluster scales, individual keys move outward from the cluster origin by `(Scale - 1) * localKeyPos`. If the wrist does not track this outward expansion, the fingers will miss the key caps entirely.

### Root Cause
A key cluster scaling operation changes the position of key caps relative to the cluster origin:
$$\mathbf{P}_{\text{key, rig}} = \mathbf{P}_{\text{cluster}} + \mathbf{R}_{\text{cluster}} \cdot (\mathbf{S}_{\text{origin}} \odot \mathbf{p}_{\text{key, local}})$$
where $\mathbf{S}_{\text{origin}}$ is the cluster scale vector, and $\mathbf{p}_{\text{key, local}}$ is the key position relative to the unscaled cluster origin.

The key's displacement due strictly to scaling is:
$$\Delta \mathbf{P}_{\text{key, rig}} = \mathbf{R}_{\text{cluster}} \cdot \left[ (\mathbf{S}_{\text{origin}} - \mathbf{1}) \odot \mathbf{p}_{\text{key, local}} \right]$$

### The Scale-Invariant Reach Solution
To keep the finger-to-key reaching vector identical regardless of scale, the wrist IK target must shift by the exact same scaling displacement as the target key:
$$\mathbf{W}_{\text{rig}} = \mathbf{P}_{\text{cluster}} + \mathbf{R}_{\text{cluster}} \cdot \left[ \mathbf{\delta}_{\text{local}} + (\mathbf{S}_{\text{origin}} - \mathbf{1}) \odot \mathbf{p}_{\text{key, local}} \right]$$

When computing the vector from the key cap to the wrist in rig space:
$$\mathbf{W}_{\text{rig}} - \mathbf{P}_{\text{key, rig}} = \mathbf{R}_{\text{cluster}} \cdot (\mathbf{\delta}_{\text{local}} - \mathbf{p}_{\text{key, local}})$$

**Notice that $\mathbf{S}_{\text{origin}}$ completely cancels out!**
The relative reaching offset between the key cap and the wrist target is 100% scale-invariant. The keyboard cluster scales naturally, keys spread out or compress, and the hands track their keys without arm distortion or launching into the air.

### Rule
- Never scale wrist reach deltas from the floor/rig origin $(0, 0, 0)$.
- In `KeyClusterTransform`, incorporate `(originScale - Vector3.one) * keyLocalPos` into the rig reach position so the key-to-wrist vector remains constant regardless of cluster scale.

---

## 43. Natural Hand Rotation Alignment Must Solve Against Live Transformed Keys

### The Phenomenon
When auto-generating finger key bindings (`AutoGenerateKeyBindingsFromFloatingKeys`), solving forearm yaw and hand pitch/roll against unrotated/unscaled identity keys (`TryGetKeyCenterAndNormalIdentity`) produced unnatural or misaligned hand orientations on angled or rotated keyboards in user scenes. The hands looked oriented against an unrotated, flat desk rather than resting naturally on the slanted keyboard.

### Root Cause
Hand reach orientation depends on two geometric constraints:
1. **The key surface normal**: Determines the palm bed tilt and roll.
2. **The vector from the avatar's resting elbow to the key**: Determines the natural forearm yaw and approach angle.

Because 3D rotation composition is non-commutative and rotation cannot compensate for linear translation or scaling offsets relative to the avatar's body without considering the live elbow line, solving at identity and post-multiplying by `clusterRot` ignores the actual spatial geometry between the avatar's body and the key in the user's scene.

### Solution: Solve in Live Rig Space & Store Relative to Cluster
1. **Solve against live world/rig keys**: Auto-generation queries `TryGetKeyCenterAndNormal` and `GetHandAlignmentReferenceBedNormalWorld`, determining the true natural wrist orientation $\mathbf{R}_{\text{solved}}$ in the live scene.
2. **Store relative to the cluster**: Use `clusterTransform.InverseTransformFromRig(R_solved)` to record:
   $$\mathbf{R}_{\text{stored}} = \mathbf{R}_{\text{cluster}}^{-1} \cdot \mathbf{R}_{\text{solved}}$$
3. **Reconstruct at runtime**: When evaluating finger movement at runtime, `clusterTransform.TransformToRig(R_stored)` applies:
   $$\mathbf{R}_{\text{cluster}} \cdot \mathbf{R}_{\text{stored}} = \mathbf{R}_{\text{cluster}} \cdot \left( \mathbf{R}_{\text{cluster}}^{-1} \cdot \mathbf{R}_{\text{solved}} \right) = \mathbf{R}_{\text{solved}}$$

This captures the natural tilt and yaw of the hand on the keys in the user's actual scene, while remaining fully responsive to dynamic changes in the `Key Origins` anchor.

### Rule
- Always calculate hand reach alignment against live transformed keys in rig/world space.
- Store orientations relative to the cluster via `clusterTransform.InverseTransformFromRig(rotation)` so dynamic changes to `Key Origins` propagate smoothly.

---

## 44. Warudo `Attachable` Architecture, `"Attachable Wrapper"`, and Scale-Free Reparenting

### The Phenomenon
When reparenting an `AnchorAsset` or `PropAsset` between different spaces (`World`, `Character`, `Screen`) at runtime:
1. Reparenting to `Screen` (from `Character` or `World`) caused the anchor to shoot tens or hundreds of meters into space.
2. Reparenting `Character` -> `World` appeared to work, but `World` -> `Character` calculated incorrect offsets and displaced the anchor.
3. Legacy scripts using `Helper._SetParent` or `Helper.SetParent` divided local positions by `parent.Transform.Scale` and multiplied `child.Transform.Scale` by `1 / parent.Transform.Scale`, corrupting the anchor's transform scale permanently.
4. Calling `anchor.GameObject.transform.SetParent(newParent)` in user code broke Warudo's internal transform tracking, preventing the anchor from following its parent.

### Root Cause: How Warudo's `Attachable` Mixin Actually Works
Decompilation of `Warudo.Plugins.Core.Assets.Mixins.Attachable` and `GameObjectAsset` reveals the actual internal transform hierarchy:

1. **The Hidden `"Attachable Wrapper"` GameObject**:
   When an `AnchorAsset` or `PropAsset` is initialized via `Attachable.Initialize(anchor.GameObject)`:
   ```csharp
   var wrapper = new GameObject("Attachable Wrapper");
   this.Transform = wrapper.transform;
   anchor.GameObject.transform.SetParent(wrapper.transform, false);
   ```
   `anchor.GameObject` is **always** a child of this internal `wrapper.transform`!
   - `wrapper.transform` itself is **never** parented to the target parent in Unity (`wrapper.transform.parent == null`). It remains a root-level transform.
   - `wrapper.transform.localScale` is **always** `Vector3.one` (Warudo never touches the wrapper's scale).

2. **Per-Frame Positioning in `Attachable.OnPostLateUpdate`**:
   Warudo evaluates `Attachable.Parent`:
   - If `Parent == null` (`World`): Sets `wrapper.transform.position = Vector3.zero` and `wrapper.transform.rotation = Quaternion.identity`.
   - If `Parent is CharacterAsset`: Sets `wrapper.transform.position = Character.MainTransform.position` and `wrapper.transform.rotation = Character.MainTransform.rotation` (using `CharacterAsset.MainTransform`, **not** `Character.GameObject.transform`!).
   - If `Parent is GameObjectAsset` (e.g. `ScreenAsset`): Sets `wrapper.transform.position = Parent.GameObject.transform.position` and `wrapper.transform.rotation = Parent.GameObject.transform.rotation`.

3. **Per-Frame Local Transform Application in `GameObjectAsset.OnLateUpdate`**:
   `TransformData.ApplyAsLocalTransform(this.GameObject.transform)` sets:
   - `anchor.GameObject.transform.localPosition = anchor.Transform.Position`
   - `anchor.GameObject.transform.localRotation = Quaternion.Euler(anchor.Transform.Rotation)`
   - `anchor.GameObject.transform.localScale = anchor.Transform.Scale`

4. **Why `Helper._SetParent` and Native `InverseTransformPoint` Exploded**:
   - Because `wrapper.transform` is unscaled (`localScale = (1, 1, 1)`), the child's local coordinates `anchor.Transform.Position` are in an **unscaled** rotated reference frame centered at `parent.position`.
   - `Helper._SetParent` naively created a temporary parent GameObject with `pa.transform.localScale = parent.Transform.Scale`. For screens where `Transform.Scale` is small (e.g. 0.05), parenting divided `localPosition` by 0.05 (multiplying the numbers by 20) and multiplied `child.Transform.Scale` by 20. But because `wrapper.transform` does not have that scale, the anchor was placed 20 times too far away!
   - Similarly, Unity's `Transform.InverseTransformPoint(worldPos)` divides by `lossyScale`. Calling `InverseTransformPoint` on a scaled prop or screen divides coordinates by the prop's mesh scale, causing spatial explosion when rendered under Warudo's unscaled wrapper.

5. **Arbitrary Threshold Traps (`Position.y < 0.2f`)**:
   - Sanity checks assuming `Position.y < 0.2f` indicates an uninitialized character floor height clobber valid coordinates in other coordinate frames (e.g. `Screen`, where `(0, 0, 0)` is the center of the display and resting hands have `y <= 0`).

### The Solution: Clean Attachable Parenting & Scale-Free Vector Transforms

1. **Never Touch Unity `Transform.SetParent` Manually**:
   Let Warudo manage `Attachable.Parent`. Only ensure `anchor.GameObject.transform` stays parented to `anchor.Attachable.Transform`:
   ```csharp
   public void AttachAnchorToParent(AnchorAsset anchor, RestHandParentType parentType) {
       if (anchor == null) return;
       switch (parentType) {
           case RestHandParentType.Character:
               anchor.Attachable.Parent = Character;
               anchor.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.TransformPath;
               anchor.Attachable.AttachToTransform = null;
               break;
           case RestHandParentType.Screen:
               var screen = screenAsset ?? GetSurfaceScreen();
               anchor.Attachable.Parent = screen;
               anchor.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.TransformPath;
               anchor.Attachable.AttachToTransform = null;
               break;
           case RestHandParentType.World:
               anchor.Attachable.Parent = null;
               break;
       }
       if (anchor.GameObject != null && anchor.Attachable?.Transform != null) {
           anchor.GameObject.transform.SetParent(anchor.Attachable.Transform, false);
       }
   }
   ```

2. **Query the True Parent Transform**:
   - For `CharacterAsset`: Use `Character.MainTransform ?? Character.GameObject?.transform`.
   - For `GameObjectAsset` (`ScreenAsset`, props): Use `asset.GameObject?.transform`.
   - For `World`: `null` (origin at `Vector3.zero`, rotation `Quaternion.identity`).

3. **Convert Between Frames Without Scale**:
   ```csharp
   // Local to World
   worldPos = p != null ? p.position + p.rotation * localPos : localPos;
   worldRot = p != null ? p.rotation * Quaternion.Euler(localRot) : Quaternion.Euler(localRot);

   // World to Local
   localPos = p != null ? Quaternion.Inverse(p.rotation) * (worldPos - p.position) : worldPos;
   localRot = p != null ? Helper.NormalizeRotationAngles((Quaternion.Inverse(p.rotation) * worldRot).eulerAngles) : Helper.NormalizeRotationAngles(worldRot.eulerAngles);
   ```

4. **Bi-Directional Migration of Stored StructuredData**:
   When `ParentType` changes, convert both live anchors (`targetAnchor`, `poleAnchor`), visual setup gizmos (`setupAnchor`), and stored configuration data (`Target.Position`, `Target.Rotation`, `Pole.Position`, `Pole.Rotation`) simultaneously so scene serialization and visual calibration stay in sync.

### Rule
- Never call `anchor.GameObject.transform.SetParent(target)` directly — this decouples the anchor from Warudo's `"Attachable Wrapper"`.
- When parent is `CharacterAsset`, always reference `Character.MainTransform`, never `Character.GameObject.transform`.
- Never use `Transform.InverseTransformPoint` or scale temporary GameObjects when reparenting Warudo `Attachable` assets. Convert coordinate frames using pure position offsets and rotation quaternions: $\mathbf{p}_{\text{local}} = \mathbf{R}_{\text{parent}}^{-1} \cdot (\mathbf{P}_{\text{world}} - \mathbf{P}_{\text{parent}})$.
- Never use absolute vertical threshold sanity checks (e.g. `Position.y < 0.2f`) on properties that can be parented to props or screens.



