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

        public static void SetParent(AnchorAsset child, Transform parent) {
            _SetParent(child, parent);
        }


        /// <summary>
        /// Parent assets and preserve world transforms.
        /// Kinda hack-ish, since modifying GOA transforms directly seems to be undone immediately
        /// NOTE: This doesn't create the parent on GOA. Use #SetParent instead when possible.
        /// </summary>
        /// <param name="child">Child GOA</param>
        /// <param name="parent">Parent GOA</param>
        public static void _SetParent(GameObjectAsset child, GameObjectAsset parent) {
            var ch = new GameObject();
            ch.transform.SetParent(child.GameObject.transform.parent);
            ch.transform.localPosition = child.Transform.Position;
            ch.transform.localRotation = Quaternion.Euler(child.Transform.Rotation);
            ch.transform.localScale = child.Transform.Scale;

            var pa = new GameObject();
            pa.transform.SetParent(parent.GameObject.transform.parent);
            pa.transform.localPosition = parent.Transform.Position;
            pa.transform.localRotation = Quaternion.Euler(parent.Transform.Rotation);
            pa.transform.localScale = parent.Transform.Scale;

            // Take advantage of unity recursion multiplication
            ch.transform.SetParent(pa.transform, true);

            child.Transform.Position = ch.transform.localPosition;
            child.Transform.Rotation = ch.transform.localRotation.eulerAngles;
            child.Transform.Scale = ch.transform.localScale;

            Object.Destroy(ch);
            Object.Destroy(pa);
        }

        public static void _SetParent(GameObjectAsset child, Transform parent) {
            var ch = new GameObject();
            ch.transform.SetParent(child.GameObject.transform.parent);
            ch.transform.localPosition = child.Transform.Position;
            ch.transform.localRotation = Quaternion.Euler(child.Transform.Rotation);
            ch.transform.localScale = child.Transform.Scale;

            var pa = new GameObject();
            pa.transform.SetParent(parent.parent);
            ch.transform.localPosition = parent.localPosition;
            ch.transform.localRotation = parent.localRotation;
            ch.transform.localScale = parent.localScale;

            // Take advantage of unity recursion multiplication
            ch.transform.SetParent(pa.transform, true);
            child.Transform.Position = ch.transform.localPosition;
            child.Transform.Rotation = ch.transform.localRotation.eulerAngles;
            child.Transform.Scale = ch.transform.localScale;

            Object.Destroy(ch);
            Object.Destroy(pa);
        }

        public static void UnsetParent(AnchorAsset child) {
            _UnsetParent(child);
            child.Attachable.Parent = null;
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
    }
}
