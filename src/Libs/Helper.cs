using System.Text;
using UnityEngine;
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
                var asm = typeof(Helper).Assembly;
                // 1. If assembly location contains Playground path, it is Playground mode
                if (!string.IsNullOrEmpty(asm.Location) && asm.Location.IndexOf("Playground", System.StringComparison.OrdinalIgnoreCase) >= 0) {
                    return true;
                }
                // 2. If assembly name contains "umod-", it is packaged Plugin (uMod) mode
                var name = asm.GetName().Name ?? "";
                if (name.IndexOf("umod-", System.StringComparison.OrdinalIgnoreCase) >= 0) {
                    return false;
                }
                // 3. Fallback: Check if OwnerPlugin is CorePlugin (Playground assigns CorePlugin as OwnerPlugin)
                var meta = Warudo.Core.Context.AssetTypeRegistry?.GetTypeMeta(typeof(PointerReceiverAsset));
                if (meta != null && meta.OwnerPlugin != null && meta.OwnerPlugin.GetType().Name == "CorePlugin") {
                    return true;
                }
                return true;
            }
        }

        /// <summary>
        /// Updates the character's OverlappingAnimations array, applying the appropriate technique
        /// depending on whether the plugin is executing in Playground mode or packaged Plugin (uMod) mode.
        /// </summary>
        public static void SetCharacterOverlappingAnimations(CharacterAsset character, CharacterAsset.OverlappingAnimationData[] layers) {
            if (character == null) return;

            if (IsPlaygroundMode) {
                character.OverlappingAnimations = layers;
                character.BroadcastDataInput(nameof(character.OverlappingAnimations));
            } else {
                character.DataInputPortCollection.SetValueAtPath(nameof(character.OverlappingAnimations), layers, true);
            }
        }

        /// <summary>
        /// Updates a single animation layer on the character, using the appropriate technique
        /// for Playground mode vs packaged Plugin mode.
        /// </summary>
        public static void SetCharacterOverlappingAnimationAt(CharacterAsset character, int index, string animationClipUri) {
            if (character == null || character.OverlappingAnimations == null || index < 0 || index >= character.OverlappingAnimations.Length) return;

            if (IsPlaygroundMode) {
                character.OverlappingAnimations[index].Animation = animationClipUri;
                character.BroadcastDataInput(nameof(character.OverlappingAnimations));
            } else {
                character.DataInputPortCollection.SetValueAtPath($"{nameof(character.OverlappingAnimations)}.{index}.Animation", animationClipUri, true);
            }
        }
    }
}
