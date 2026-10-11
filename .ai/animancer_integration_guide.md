# Animancer Integration in Warudo Mods

## The Critical Animancer Pitfall & Root Cause

### What Went Wrong in SDK 0.14.5.1
1. **Animancer Lite vs Animancer Pro**:
   * Warudo the game runs **Animancer Pro**; all types (`AnimancerComponent`, `AnimancerPlayable`, `AnimancerLayer`, `LayerList`) exist in `Animancer.dll` (`Version=7.4.2.25`).
   * Warudo SDK 0.14.5.1 bundled **Animancer Lite** as a precompiled binary: `Animancer.Lite.dll`.
   * When any mod referenced `Character.Animancer.Layers[1]`, the return type `AnimancerPlayable.LayerList` was imported from `Animancer.Lite.dll`.
   * When exported to a `.warudo` mod and run inside Warudo, Mono threw:
     ```
     System.IO.FileNotFoundException: Could not load file or assembly 'Animancer.Lite, Version=7.4.2.25'
     ```

2. **The Solution (C# Stub Library)**:
   * Rather than distributing proprietary DLLs or requiring user runtime shims, the SDK uses clean C# stubs (`throw new NotImplementedException();`) under `Packages/app.warudo.modtool/Packages/Animancer/`.
   * The assembly compiles directly into `Library/ScriptAssemblies/Animancer.dll` with identity:
     ```csharp
     [assembly: AssemblyVersion("7.4.2.25")]
     [assembly: AssemblyFileVersion("7.4.2.25")]
     ```
   * All types (`AnimancerPlayable.LayerList`, `AnimancerLayer`, `AnimancerComponent`) belong to assembly `Animancer`.
   * At mod export time, UMod bakes references to `Animancer, Version=7.4.2.25`.
   * In the Warudo game, this matches `Warudo_Data/Managed/Animancer.dll` exactly.

---

## Safe Code Patterns When Using Animancer

### Accessing Animancer on a Character
```csharp
using Warudo.Core.Attributes;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets.Character;
using Animancer;

namespace MyPlugin 
{
    [AssetType(Id = "MyPlugin.Asset.AnimationController", Title = "Animation Controller")]
    public class MyAnimationAsset : Asset 
    {
        [DataInput]
        public CharacterAsset Character;

        [Trigger]
        public void PlayCustomLayer() 
        {
            if (Character == null || Character.Animancer == null) 
            {
                Warudo.Core.Context.Service.Toast(
                    Warudo.Core.Server.ToastSeverity.Warning,
                    "Warning",
                    "Character or Animancer is not assigned."
                );
                return;
            }

            // Safe access to Animancer layers:
            AnimancerComponent animancer = Character.Animancer;
            AnimancerPlayable.LayerList layers = animancer.Layers;
            
            // Accessing layer by index:
            AnimancerLayer layer = layers[1];
            layer.SetWeight(1.0f);
        }
    }
}
```

### Animancer Key Types Quick Reference
* `CharacterAsset.Animancer`: The `AnimancerComponent` attached to the character's root.
* `AnimancerComponent.Layers`: Accesses `AnimancerPlayable.LayerList` for multi-layer blending (facial expressions, overlays, gestures).
* `AnimancerComponent.States`: `AnimancerPlayable.StateDictionary` containing active/registered animation states.
* `AnimancerComponent.Play(AnimationClip)`: Plays a single clip.
* `AnimancerComponent.Play(ITransition)`: Transitions between animations using customizable fades.
* `AnimancerLayer.SetWeight(float)`: Adjusts layer blend weight (0.0 to 1.0).

---

## Driving Overlapping Animation Layers & Action Playback

When controlling Warudo's character overlapping animation layers (e.g., finger overlays, button presses, gestures):

### 1. Animancer Layer Index Calculation
In Warudo, `CharacterAsset.SetupOverlappingAnimations` orders active layers into Animancer as:
* **Layer 0**: Base animation (idle/walk).
* **Layers 1..N**: Active items in `Character.OverlappingAnimations.Where(a => a.Enabled && a.GetAnimationClip() != null)`.
```csharp
int GetAnimancerLayerIndex(CharacterAsset.OverlappingAnimationData layerData) {
    if (Character == null || Character.OverlappingAnimations == null || layerData == null) return -1;
    var activeLayers = Character.OverlappingAnimations.Where(a => a.Enabled && a.GetAnimationClip() != null).ToList();
    var indexInActive = activeLayers.IndexOf(layerData);
    if (indexInActive < 0) return -1;
    return 1 + indexInActive;
}
```

### 2. Looping vs. One-Shot Action Clips
* In Warudo's `SetupOverlappingAnimations()`, non-looping layers (`Looping = false`) attach an `Events.OnEnd` handler that marks `state.IsPlaying = false` once the clip finishes.
* To avoid freezing the state, always set `Looping = true` on weight-blended action layers.
* When a button is triggered (`Activated`), restart and rewind the layer explicitly:
```csharp
void RewindAnimancerLayer(Animancer.AnimancerComponent animancer, int layerIdx) {
    if (animancer == null || animancer.Layers == null || layerIdx < 0 || layerIdx >= animancer.Layers.Count) return;
    var layer = animancer.Layers[layerIdx];
    if (layer == null) return;
    var state = layer.CurrentState;
    if (state == null) {
        foreach (Animancer.AnimancerState s in layer) {
            state = s;
            break;
        }
    }
    if (state != null) {
        var playingState = layer.Play(state);
        playingState.Time = 0f;
    }
}
```

### 3. State Playing Guard on Weight Changes
When adjusting layer weight (`SetWeight`), ensure the state is marked as playing so Animancer evaluates the layer:
```csharp
void SetLayerWeight(Animancer.AnimancerComponent animancer, int layerIdx, float weight) {
    if (animancer == null || animancer.Layers == null || layerIdx < 0 || layerIdx >= animancer.Layers.Count) return;
    var layer = animancer.Layers[layerIdx];
    if (layer == null) return;
    layer.SetWeight(weight);
    if (weight > 0.001f && layer.CurrentState != null && !layer.CurrentState.IsPlaying) {
        layer.CurrentState.IsPlaying = true;
    }
}
```

