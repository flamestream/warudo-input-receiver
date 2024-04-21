using System.Text;
using UnityEngine;
using Warudo.Plugins.Core.Assets;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Prop;
using Warudo.Plugins.Core.Assets.Utility;

namespace FlameStream {
    public class Helper {

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
    }
}
