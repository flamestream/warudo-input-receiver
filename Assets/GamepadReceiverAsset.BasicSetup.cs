using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Utility;
using static Warudo.Plugins.Core.Assets.Character.CharacterAsset;

namespace FlameStream
{
    public partial class GamepadReceiverAsset : ReceiverAsset {

        [DataInput]
        [Hidden]
        Guid RootAnchorAssetId;
        [DataInput]
        [Hidden]
        Guid GamepadAnchorAssetId;
        [DataInput]
        [Hidden]
        Guid LeftHandAnchorAssetId;
        [DataInput]
        [Hidden]
        Guid RightHandAnchorAssetId;
        [DataInput]

        [Hidden]
        public Vector3 GamepadPosition;
        [DataInput]
        [Hidden]
        public Vector3 GamepadRotation;
        [DataInput]
        [Hidden]
        public Vector3 RootAnchorPosition;
        [DataInput]
        [Hidden]
        public Vector3 RootAnchorRotation;
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

        public enum GamepadHandSide {
            [Label("LEFT_HAND")]
            LeftHand,
            [Label("RIGHT_HAND")]
            RightHand,
        }

        public enum GamepadType {
            [Label("Nintendo Switch Pro Controller")]
            SwitchProController,
            [Label("PlayStation 5 Controller")]
            PS5Controller,
            [Label("Xbox 360 Controller")]
            Xbox360Controller,
        }

        public AnchorAsset RootAnchor {
            get {
                if (RootAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == RootAnchorAssetId);
            }
        }

        public AnchorAsset GamepadAnchor {
            get {
                if (GamepadAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == GamepadAnchorAssetId);
            }
        }

        public AnchorAsset LeftHandAnchor {
            get {
                if (LeftHandAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == LeftHandAnchorAssetId);
            }
        }

        public AnchorAsset RightHandAnchor {
            get {
                if (RightHandAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == RightHandAnchorAssetId);
            }
        }

        bool IsBasicSetupNotDone() {
            return GamepadAnchor == null;
        }
        bool IsBasicSetupDone() {
            return !IsBasicSetupNotDone();
        }
        bool IsBasicSetupInputMissing() {
            return Gamepad == null || Character == null || IdleFingerAnimation == null;
        }

        void OnIsHandEnabledChange() {
            var idleLayer = Character.OverlappingAnimations.FirstOrDefault(d => d.CustomLayerID == LAYER_NAME_IDLE);
            if (idleLayer != null) {
                var idx = Array.IndexOf(Character.OverlappingAnimations, idleLayer);
                Character.DataInputPortCollection.SetValueAtPath(
                    $"{nameof(Character.OverlappingAnimations)}.{idx}.Weight",
                    IsHandEnabled ? 1f : 0f,
                    true
                );
            }

            for (var idx = 0; idx < Character.OverlappingAnimations.Length; ++idx) {
                var d = Character.OverlappingAnimations[idx];
                if (!d.CustomLayerID.StartsWith(LAYER_NAME_PREFIX)) {
                    continue;
                }

                if (d.CustomLayerID == LAYER_NAME_IDLE) {
                    d.Weight = IsHandEnabled ? 1f : 0f;
                } else if (!IsHandEnabled) {
                    d.Weight = 0f;
                }
                ApplyAnimancerPropertyWeight(idx, d.Weight);
                d.BroadcastDataInput("Weight");
            };
            EnableLimb(IsHandEnabled, Character.LeftHandIK, LeftHandAnchor);
            EnableLimb(IsHandEnabled, Character.RightHandIK, RightHandAnchor);

            var animationGraph = AnimationGraph;
            if (animationGraph != null) {
                animationGraph.Enabled = IsHandEnabled;
                Context.Service?.BroadcastGraphEnabled(animationGraph.Id, IsHandEnabled);
            }

            Gamepad.SetDataInput(nameof(Gamepad.Enabled), IsHandEnabled);
            Gamepad.BroadcastDataInput(nameof(Gamepad.Enabled));
        }

        void EnableLimb(bool isEnabled, LimbIKData limb, AnchorAsset anchor) {
            if (isEnabled) {
                limb.Enabled = true;
                limb.IkTarget = anchor;
                limb.PositionWeight = 1.0f;
                limb.RotationWeight = 1.0f;
            } else {
                limb.Enabled = false;
            }
            limb.Broadcast();
        }

        public void OnIdleFingerAnimationChange() {
            if (Character == null) return;

            var idleLayer = Character.OverlappingAnimations?.FirstOrDefault(d => d.CustomLayerID == LAYER_NAME_IDLE);
            if (idleLayer == null) {

                idleLayer = CreateIdleFingerAnimationData();

                var list = Character.OverlappingAnimations?.ToList() ?? new List<OverlappingAnimationData>();
                var firstLayerElement = list.Find(d => d.CustomLayerID.StartsWith(LAYER_NAME_PREFIX));
                var idx = list.IndexOf(firstLayerElement);
                if (idx >= 0) {
                    list.Insert(idx, idleLayer);
                } else {
                    list.Add(idleLayer);
                }
                // Plugin Mode
                Character.DataInputPortCollection.SetValueAtPath($"{nameof(Character.OverlappingAnimations)}", list.ToArray(), true);
                // Playground Mode
                // Character.OverlappingAnimations =  list.ToArray();
                // Character.BroadcastDataInput(nameof(Character.OverlappingAnimations));

            } else {

                var idx = Array.IndexOf(Character.OverlappingAnimations, idleLayer);
                // Plugin Mode
                Character.DataInputPortCollection.SetValueAtPath($"{nameof(Character.OverlappingAnimations)}.{idx}.Animation", IdleFingerAnimation, true);
                // Playground Mode
                // Character.OverlappingAnimations[idx].Animation = IdleFingerAnimation;
                // Character.BroadcastDataInput(nameof(Character.OverlappingAnimations));
            }
        }

        void ApplyBasicSetup() {

            IsHandEnabled = true;

            AnchorAsset rootAnchor = Scene.AddAsset<AnchorAsset>();
            rootAnchor.Name = "⚓-🔥🎮 Mover";
            Scene.UpdateNewAssetName(rootAnchor);
            RootAnchorAssetId = rootAnchor.Id;

            AnchorAsset gamepadAnchor = Scene.AddAsset<AnchorAsset>();
            gamepadAnchor.Name = "⚓-🔥🎮🎯";
            Scene.UpdateNewAssetName(gamepadAnchor);
            GamepadAnchorAssetId = gamepadAnchor.Id;

            var rootAnchorTransform = rootAnchor.Transform;
            var gamepadAnchorTransform = gamepadAnchor.Transform;
            var gamepadTransform = Gamepad.Transform;

            // Set anchor position to where the controller is
            rootAnchorTransform.Position = gamepadTransform.Position;
            rootAnchor.GameObject.transform.position = Gamepad.GameObject.transform.position;
            gamepadAnchorTransform.Position = gamepadTransform.Position;
            gamepadAnchor.GameObject.transform.position = Gamepad.GameObject.transform.position;

            Helper.SetParent(Gamepad, gamepadAnchor);
            Helper.SetParent(gamepadAnchor, rootAnchor);
            Helper.SetParent(rootAnchor, Character);

            // Record coordinates to reset later
            GamepadPosition = Gamepad.Transform.Position;
            GamepadRotation = Gamepad.Transform.Rotation;
            RootAnchorPosition = rootAnchor.Transform.Position;
            RootAnchorRotation = rootAnchor.Transform.Rotation;

            var leftAnchor = SetupIKTargetHandAnchor(
                "⚓-🔥🎮🫲",
                HumanBodyBones.LeftHand,
                Character.LeftHandIK,
                gamepadAnchor,
                ref LeftHandAnchorAssetId,
                ref LeftHandAnchorPosition,
                ref LeftHandAnchorRotation
            );

            var rightAnchor = SetupIKTargetHandAnchor(
                "⚓-🔥🎮🫱",
                HumanBodyBones.RightHand,
                Character.RightHandIK,
                gamepadAnchor,
                ref RightHandAnchorAssetId,
                ref RightHandAnchorPosition,
                ref RightHandAnchorRotation
            );

            Gamepad.Transform.Position = GamepadPosition;
            Gamepad.Transform.Rotation = GamepadRotation;
            Helper.SetParent(Gamepad, leftAnchor);
            GamepadLeftHandPosition = Gamepad.Transform.Position;
            GamepadLeftHandRotation = Gamepad.Transform.Rotation;

            Gamepad.Transform.Position = GamepadPosition;
            Gamepad.Transform.Rotation = GamepadRotation;
            Helper.SetParent(Gamepad, rightAnchor);
            GamepadRightHandPosition = Gamepad.Transform.Position;
            GamepadRightHandRotation = Gamepad.Transform.Rotation;

            AttachGamepad(DefaultAnchorSide);

            rootAnchor.Broadcast();
            gamepadAnchor.Broadcast();
            leftAnchor.Broadcast();
            rightAnchor.Broadcast();
            Gamepad.Broadcast();
            Broadcast();

            var idleLayer = Character.OverlappingAnimations?.FirstOrDefault(d => d.CustomLayerID == LAYER_NAME_IDLE);
            var idx = Array.IndexOf(Character.OverlappingAnimations, idleLayer);
            Character.DataInputPortCollection.SetValueAtPath(
                $"{nameof(Character.OverlappingAnimations)}.{idx}.MaskedBodyParts",
                new AnimationMaskedBodyPart[] {
                    AnimationMaskedBodyPart.LeftFingers,
                    AnimationMaskedBodyPart.RightFingers,
                },
                true
            );
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

            EnableLimb(true, limb, anchor);

            Helper.SetParent(anchor, parent);

            handPosition = anchor.Transform.Position;
            handRotation = anchor.Transform.Rotation;

            anchor.Broadcast();
            limb.Broadcast();

            return anchor;
        }

        void AttachGamepad(GamepadHandSide side = GamepadHandSide.LeftHand) {

            var position = GamepadLeftHandPosition;
            var rotation = GamepadLeftHandRotation;
            HumanBodyBones bone = HumanBodyBones.LeftHand;
            if (side == GamepadHandSide.RightHand) {
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

        void ResetAllAnchors() {
            Gamepad.Transform.Position = GamepadRightHandPosition;
            Gamepad.Transform.Rotation = GamepadRightHandRotation;
            AttachGamepad(DefaultAnchorSide);

            RootAnchor.Transform.Position = RootAnchorPosition;
            RootAnchor.Transform.Rotation = RootAnchorRotation;
            GamepadAnchor.Transform.Position = Vector3.zero;
            GamepadAnchor.Transform.Rotation = Vector3.zero;
            LeftHandAnchor.Transform.Position = LeftHandAnchorPosition;
            LeftHandAnchor.Transform.Rotation = LeftHandAnchorRotation;
            RightHandAnchor.Transform.Position = RightHandAnchorPosition;
            RightHandAnchor.Transform.Rotation = RightHandAnchorRotation;

            Gamepad.Broadcast();
            RootAnchor.Broadcast();
            GamepadAnchor.Broadcast();
            LeftHandAnchor.Broadcast();
            RightHandAnchor.Broadcast();
        }

        void ClearAllAnchors() {
            Helper.UnsetParent(Gamepad);
            CleanDestroy(RootAnchor);
            CleanDestroy(GamepadAnchor);
            CleanDestroy(LeftHandAnchor);
            CleanDestroy(RightHandAnchor);
        }

        void CleanDestroy(GameObjectAsset g) {
            if (g == null) return;
            try {
                Scene.RemoveAsset(g.Id);
            } catch {}
        }
    }
}
