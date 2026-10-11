using System.Linq;
using System.Text;
using UnityEngine;
using Warudo.Core.Localization;
using Warudo.Plugins.Core.Assets;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Prop;
using Warudo.Plugins.Core.Assets.Utility;

namespace FlameStream {
    public class Helper {

        public static void CopyTransform(Warudo.Core.Data.Models.TransformData from, Warudo.Core.Data.Models.TransformData to, bool isBroadcastWanted = false) {
            to.Position = from.Position;
            to.Rotation = from.Rotation;
            to.Scale = from.Scale;

            if (isBroadcastWanted) to.Broadcast();
        }

        public static void SetParent(AnchorAsset child, AnchorAsset parent) {
            _SetParent(child, parent);
            child.Attachable.Parent = parent;
        }

        public static void SetParent(PropAsset child, AnchorAsset parent) {
            _SetParent(child, parent);
            child.Attachable.Parent = parent;
        }

        public static void SetParent(AnchorAsset child, CharacterAsset parent) {
            _SetParent(child, parent);
            child.Attachable.Parent = parent;
            child.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.TransformPath;
            child.Attachable.AttachToTransform = null;
        }

        /// <summary>
        /// Parent assets and preserve world transforms.
        /// Kinda hack-ish, since modifying GOA transforms directly seems to be undone immediately
        /// NOTE: This doesn't create the parent on GOA. Use #SetParent instead when possible.
        /// </summary>
        /// <param name="child">Child GOA</param>
        /// <param name="parent">Parent GOA</param>
        public static void _SetParent(GameObjectAsset child, GameObjectAsset parent) {

            var childUnityTransform = child.GameObject.transform;
            var parentUnityTransform = parent.GameObject.transform;
            var childWorldPosition = childUnityTransform.position;
            var childWorldRotation = childUnityTransform.rotation;
            var parentWorldPosition = parentUnityTransform.position;
            var parentWorldRotation = parentUnityTransform.rotation;

            var ch = new GameObject();
            ch.transform.SetPositionAndRotation(childWorldPosition, childWorldRotation);
            ch.transform.localScale = child.Transform.Scale;

            var pa = new GameObject();
            pa.transform.SetPositionAndRotation(parentWorldPosition, parentWorldRotation);
            pa.transform.localScale = parent.Transform.Scale;

            // Take advantage of unity recursion multiplication
            // FIXME: Apparently unity transforms have no parent, but is controlled by Attachable post calculation
            // So this is just a single inverse matrix calculation
            ch.transform.SetParent(pa.transform, true);

            var finalLocalPosition = ch.transform.localPosition;
            var finalLocalRotation = ch.transform.localRotation;
            var finalWorldPosition = ch.transform.position;
            var finalWorldRotation = ch.transform.rotation;
            child.Transform.Position = finalLocalPosition;
            child.Transform.Rotation = finalLocalRotation.eulerAngles;
            child.Transform.Scale = ch.transform.localScale;
            child.GameObject.transform.SetPositionAndRotation(finalWorldPosition, finalWorldRotation);
            child.GameObject.transform.localScale = ch.transform.localScale;

            Object.Destroy(ch);
            Object.Destroy(pa);
        }

        public static void UnsetParent(PropAsset child) {
            _UnsetParent(child);
            child.Attachable.Parent = null;
        }

        public static void _UnsetParent(GameObjectAsset child) {

            var transform = child.Transform;
            var unityTransform = child.GameObject.transform;
            transform.Position = unityTransform.position;
            transform.Rotation = unityTransform.rotation.eulerAngles;
            transform.Scale = unityTransform.lossyScale;
        }

        public static Vector3 NormalizeRotationAngles(Vector3 r) {
            return new Vector3(
                NormalizeAngle(r.x),
                NormalizeAngle(r.y),
                NormalizeAngle(r.z)
            );
        }

        public static float NormalizeAngle(float angle) {
            angle %= 360.0f;
            if (angle > 180.0f) return angle - 360.0f;
            if (angle <= -180.0f) return angle + 360.0f;
            return angle;
        }

        /// <summary>
        /// Determines if the code is currently executing inside Warudo's live Roslyn-compiled Playground environment
        /// or as a packaged .warudo mod loaded via uMod.
        /// </summary>
        public static bool IsPlaygroundMode {
            get {
                // When running as a packaged mod in Warudo, Plugin.ModHost is populated by UMod.
                // In Playground mode, plugins are compiled by Roslyn into the host domain and ModHost is null.
                if (PointerReceiverPlugin.Instance != null) {
                    return PointerReceiverPlugin.Instance.ModHost == null;
                }
                var plugin = Warudo.Core.Context.PluginManager?.GetPlugin("FlameStream.PointerReceiver");
                if (plugin != null) {
                    return plugin.ModHost == null;
                }
                return false;
            }
        }

        public static int GetOverlappingAnimationSortPriority(CharacterAsset.OverlappingAnimationData layer) {
            if (layer == null || string.IsNullOrEmpty(layer.CustomLayerID)) return 0;
            if (!layer.CustomLayerID.StartsWith("🔥")) return 0; // Non-plugin user layers first
            if (layer.CustomLayerID.StartsWith("🔥⌨️")) return 1; // Keyboard layers (base pose + finger presses)
            if (layer.CustomLayerID.StartsWith("🔥🖱️") || layer.CustomLayerID.StartsWith("🔥🖊️")) return 2; // Pointer layers on top
            return 1;
        }

        public static Cysharp.Threading.Tasks.UniTask SetCharacterOverlappingAnimations(CharacterAsset character, CharacterAsset.OverlappingAnimationData[] layers) {
            if (character == null) return Cysharp.Threading.Tasks.UniTask.CompletedTask;

            try {
                if (layers != null) {
                    layers = layers.OrderBy(GetOverlappingAnimationSortPriority).ToArray();
                }

                if (IsPlaygroundMode) {
                    Warudo.Core.Context.Service?.Toast(
                        Warudo.Core.Server.ToastSeverity.Warning,
                        "FINGER_ANIM_PLAYGROUND_UNSUPPORTED".Localized(),
                        "FINGER_ANIM_PLAYGROUND_UNSUPPORTED_BODY".Localized());
                    return Cysharp.Threading.Tasks.UniTask.CompletedTask;
                }

                character.SetDataInput(nameof(character.OverlappingAnimations), layers, true);
            } catch (System.Exception ex) {
                Warudo.Core.Utils.Log.Error($"[Helper] SetCharacterOverlappingAnimations failed: {ex}");
            }
            return Cysharp.Threading.Tasks.UniTask.CompletedTask;
        }

        /// <summary>
        /// Updates a single animation layer on the character, using the appropriate technique
        /// for Playground mode vs packaged Plugin mode.
        /// </summary>
        public static void SetCharacterOverlappingAnimationAt(CharacterAsset character, int index, string animationClipUri) {
            if (character == null || character.OverlappingAnimations == null || index < 0 || index >= character.OverlappingAnimations.Length) return;

            if (IsPlaygroundMode) {
                Warudo.Core.Context.Service?.Toast(
                    Warudo.Core.Server.ToastSeverity.Warning,
                    "FINGER_ANIM_PLAYGROUND_UNSUPPORTED".Localized(),
                    "FINGER_ANIM_PLAYGROUND_UNSUPPORTED_BODY".Localized());
                return;
            }

            character.DataInputPortCollection.SetValueAtPath($"{nameof(character.OverlappingAnimations)}.{index}.Animation", animationClipUri, true);
        }

        /// <summary>
        /// Moves an asset to the specified first-level category/folder in Warudo's asset list hierarchy and broadcasts the update.
        /// </summary>
        public static void AssignAssetToCategory(Warudo.Core.Scenes.Scene scene, Warudo.Core.Scenes.Asset asset, string category) {
            if (scene == null || asset == null || string.IsNullOrEmpty(category)) return;
            try {
                scene.AssetHierarchy?.AssignAtFirstLevel(asset.IdString, category);
                Warudo.Core.Context.Service?.BroadcastAssetHierarchy();
            } catch {}
        }

        /// <summary>
        /// Updates the character's DefaultIdleAnimation port using SetValueAtPath,
        /// exactly replicating how Warudo's UI updates the character asset and triggers StartAnimation.
        /// </summary>
        public static void ApplyCharacterIdleAnimation(CharacterAsset character, string animationUri) {
            if (character == null || string.IsNullOrEmpty(animationUri)) return;

            try {
                character.DataInputPortCollection?.SetValueAtPath(nameof(character.DefaultIdleAnimation), animationUri, true);
            } catch (System.Exception ex) {
                UnityEngine.Debug.LogWarning($"[Helper] Failed to set DefaultIdleAnimation via SetValueAtPath: {ex.Message}");
            }
        }
    }
}
