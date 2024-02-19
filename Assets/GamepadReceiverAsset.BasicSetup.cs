using System;
using System.Drawing.Design;
using System.Linq;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Prop;
using Warudo.Plugins.Core.Assets.Utility;

namespace FlameStream {
    public partial class GamepadReceiverAsset : ReceiverAsset {

        [Section("Basic Prop Setup")]

        [Markdown]
        [HiddenIf(nameof(IsSetupComplete))]
        public String BasicPropSetupInstructions = @"### Instructions

Make target controller hold controller in wanted neutral position, then set up anchors.";

        [DataInput]
        [DisabledIf(nameof(IsSetupComplete))]
        public CharacterAsset Character;

        [DataInput]
        [DisabledIf(nameof(IsSetupComplete))]
        [Label("Game Controller")]
        public PropAsset Gamepad;

        [DataInput]
        [Hidden]
        Guid GamepadAnchorAssetId;
        [DataInput]
        [Hidden]
        Guid GamepadLeftHandAnchorAssetId;
        [DataInput]
        [Hidden]
        Guid GamepadRightHandAnchorAssetId;

        public AnchorAsset GamepadAnchor {
            get {
                if (GamepadAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == GamepadAnchorAssetId);
            }
        }

        public AnchorAsset GamepadLeftHandAnchor {
            get {
                if (GamepadLeftHandAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == GamepadLeftHandAnchorAssetId);
            }
        }

        public AnchorAsset GamepadRightHandAnchor {
            get {
                if (GamepadRightHandAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == GamepadRightHandAnchorAssetId);
            }
        }

        [DataInput]
        [Hidden]
        public Vector3 GamepadPosition;
        [DataInput]
        [Hidden]
        public Vector3 GamepadRotation;
        [DataInput]
        [Hidden]
        public Vector3 GamepadAnchorPosition;
        [DataInput]
        [Hidden]
        public Vector3 GamepadAnchorRotation;
        [DataInput]
        [Hidden]
        public Vector3 GamepadLeftHandPosition;
        [DataInput]
        [Hidden]
        public Vector3 GamepadLeftHandRotation;
        [DataInput]
        [Hidden]
        public Vector3 GamepadRightHandPosition;
        [DataInput]
        [Hidden]
        public Vector3 GamepadRightHandRotation;
        [DataInput]
        [Hidden]
        public Vector3 LeftHandAnchorPosition;
        [DataInput]
        [Hidden]
        public Vector3 LeftHandAnchorRotation;
        [DataInput]
        [Hidden]
        public Vector3 RightHandAnchorPosition;
        [DataInput]
        [Hidden]
        public Vector3 RightHandAnchorRotation;


        bool IsSetupIncomplete() {
            return GamepadAnchor == null;
        }
        bool IsSetupComplete() {
            return !IsSetupIncomplete();
        }

        bool IsMissingInput() {
            return Gamepad == null || Character == null;
        }

        [Trigger]
        [DisabledIf(nameof(IsMissingInput))]
        [HiddenIf(nameof(IsSetupComplete))]
        public void SetupGamepadAnchors() {

            AnchorAsset gamepadAnchor = Scene.AddAsset<AnchorAsset>();
            gamepadAnchor.Name = "⚓-🔥🎮";
            Scene.UpdateNewAssetName(gamepadAnchor);
            GamepadAnchorAssetId = gamepadAnchor.Id;

            var anchorTransform = gamepadAnchor.Transform;
            var gamepadTransform = Gamepad.Transform;
            anchorTransform.Position = gamepadTransform.Position;

            SetParent(Gamepad, gamepadAnchor);
            SetParent(gamepadAnchor, Character);

            GamepadPosition = Gamepad.Transform.Position;
            GamepadRotation = Gamepad.Transform.Rotation;

            GamepadAnchorPosition = gamepadAnchor.Transform.Position;
            GamepadAnchorRotation = gamepadAnchor.Transform.Rotation;

            var leftAnchor = SetupIKTargetHandAnchor(
                "⚓-🔥🎮🫲",
                HumanBodyBones.LeftHand,
                Character.LeftHandIK,
                gamepadAnchor,
                ref GamepadLeftHandAnchorAssetId,
                ref LeftHandAnchorPosition,
                ref LeftHandAnchorRotation
            );
            var rightAnchor = SetupIKTargetHandAnchor(
                "⚓-🔥🎮🫱",
                HumanBodyBones.RightHand,
                Character.RightHandIK,
                gamepadAnchor,
                ref GamepadRightHandAnchorAssetId,
                ref RightHandAnchorPosition,
                ref RightHandAnchorRotation
            );

            Gamepad.Transform.Position = GamepadPosition;
            Gamepad.Transform.Rotation = GamepadRotation;
            SetParent(Gamepad, leftAnchor);
            GamepadLeftHandPosition = Gamepad.Transform.Position;
            GamepadLeftHandRotation = Gamepad.Transform.Rotation;

            Gamepad.Transform.Position = GamepadPosition;
            Gamepad.Transform.Rotation = GamepadRotation;
            SetParent(Gamepad, rightAnchor);
            GamepadRightHandPosition = Gamepad.Transform.Position;
            GamepadRightHandRotation = Gamepad.Transform.Rotation;

            AttachGamepad(DefaultControllerAnchorSide);

            gamepadAnchor.Broadcast();
            leftAnchor.Broadcast();
            rightAnchor.Broadcast();
            Gamepad.Broadcast();
        }

        public void AttachGamepad(GamepadAnchorSide side = GamepadAnchorSide.LeftHand) {

            var position = GamepadLeftHandPosition;
            var rotation = GamepadLeftHandRotation;
            HumanBodyBones bone = HumanBodyBones.LeftHand;
            if (side == GamepadAnchorSide.RightHand) {
                position = GamepadRightHandPosition;
                rotation = GamepadRightHandRotation;
                bone = HumanBodyBones.RightHand;
            }

            var transform = Gamepad.Transform;
            transform.Position = position;
            transform.Rotation = rotation;

            Gamepad.Attachable.Parent = Character;
            Gamepad.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.HumanBodyBone;
            Gamepad.Attachable.AttachToBone = bone;
        }

        [Trigger]
        [Label("🔁 Reset All Anchors")]
        [HiddenIf(nameof(IsSetupIncomplete))]
        public void ResetAllAnchors() {
            Gamepad.Transform.Position = GamepadRightHandPosition;
            Gamepad.Transform.Rotation = GamepadRightHandRotation;
            AttachGamepad(DefaultControllerAnchorSide);

            GamepadAnchor.Transform.Position = GamepadAnchorPosition;
            GamepadAnchor.Transform.Rotation = GamepadAnchorRotation;
            GamepadLeftHandAnchor.Transform.Position = LeftHandAnchorPosition;
            GamepadLeftHandAnchor.Transform.Rotation = LeftHandAnchorRotation;
            GamepadRightHandAnchor.Transform.Position = RightHandAnchorPosition;
            GamepadRightHandAnchor.Transform.Rotation = RightHandAnchorRotation;

            Gamepad.Broadcast();
            GamepadAnchor.Broadcast();
            GamepadLeftHandAnchor.Broadcast();
            GamepadRightHandAnchor.Broadcast();
        }

        [Trigger]
        [HiddenIf(nameof(IsSetupIncomplete))]
        [Label("💣 Clear All Anchors")]
        public void ClearAllAnchors() {
            UnsetParent(Gamepad);
            CleanDestroy(GamepadAnchor);
            CleanDestroy(GamepadLeftHandAnchor);
            CleanDestroy(GamepadRightHandAnchor);
        }

        AnchorAsset SetupIKTargetHandAnchor(
            string name,
            HumanBodyBones targetBone,
            CharacterAsset.LimbIKData limb,
            AnchorAsset parent,
            ref Guid anchorAssetId,
            ref Vector3 handPosition,
            ref Vector3 handRotation
        ) {
			AnchorAsset anchor = Scene.AddAsset<AnchorAsset>();
			anchor.Name = name;
			Scene.UpdateNewAssetName(anchor);
			anchor.Transform.CopyFromWorldTransform(Character.Animator.GetBoneTransform(targetBone));
			anchor.Transform.ApplyAsWorldTransform(anchor.GameObject.transform);

            anchorAssetId = anchor.Id;

            limb.Enabled = true;
            limb.IkTarget = anchor;
            limb.PositionWeight = 1.0f;
            limb.RotationWeight = 1.0f;

            SetParent(anchor, parent);

            handPosition = anchor.Transform.Position;
            handRotation = anchor.Transform.Rotation;

			anchor.Broadcast();
            limb.Broadcast();

            return anchor;
		}

        void SetParent(AnchorAsset child, AnchorAsset parent) {
            _SetParent(child, parent);
            child.Attachable.Parent = parent;
        }

        void SetParent(PropAsset child, AnchorAsset parent) {
            _SetParent(child, parent);
            child.Attachable.Parent = parent;
        }

        void SetParent(AnchorAsset child, CharacterAsset parent) {
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
        void _SetParent(GameObjectAsset child, GameObjectAsset parent) {
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

            UnityEngine.Object.Destroy(ch);
            UnityEngine.Object.Destroy(pa);
        }

        void UnsetParent(PropAsset child) {
            _UnsetParent(child);
            child.Attachable.Parent = null;
        }

        void _UnsetParent(GameObjectAsset child) {

            var transform = child.Transform;
            var unityTransform = child.GameObject.transform;
            transform.Position = unityTransform.position;
            transform.Rotation = unityTransform.rotation.eulerAngles;
            transform.Scale = unityTransform.lossyScale;
        }

        void CleanDestroy(GameObjectAsset g) {
            if (g == null) return;
            try {
                Scene.RemoveAsset(g.Id);
            } catch {}
        }
    }
}
