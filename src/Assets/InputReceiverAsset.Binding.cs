using System;
using System.Linq;
using System.Runtime.Remoting.Channels;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Data.Models;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;
using Warudo.Core.UI;
using Warudo.Plugins.Core.Assets;
using Warudo.Plugins.Core.Assets.Utility;
using static Warudo.Plugins.Core.Assets.Character.CharacterAsset;

namespace FlameStream
{
    public partial class InputReceiverAsset : ReceiverAsset {

        bool IsAssetChangeRequested;
        Tween IkTween;

        // Track previous state to detect enable/disable transitions
        bool PreviousIsEnabled = false;
        bool PreviousIsControlEnabled = false;
        bool HasInitializedState = false;

        [DataInput]
        [Hidden]
        public Guid MoverAnchorAssetId;
        [DataInput]
        [Hidden]
        public Guid TargetAnchorAssetId;
        [DataInput]
        [Hidden]
        public Guid LeftHandAnchorAssetId;
        [DataInput]
        [Hidden]
        public Guid RightHandAnchorAssetId;

        public AnchorAsset MoverAnchor {
            get {
                if (MoverAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == MoverAnchorAssetId);
            }
        }

        public AnchorAsset TargetAnchor {
            get {
                if (TargetAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == TargetAnchorAssetId);
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

        protected bool CheckIsBindingSetupInputMissing() {
            return HeldProp == null || Character == null;
        }

        void HandleIsControlEnabledChanged() {
            IsAssetChangeRequested = true;
        }

        void OnUpdateBinding() {
            // Initialize state tracking on first run (startup)
            if (!HasInitializedState) {
                PreviousIsEnabled = IsEnabled;
                PreviousIsControlEnabled = IsControlEnabled;
                HasInitializedState = true;
                return; // Don't process anything on startup
            }

            // Check for state transitions
            var currentEnabled = IsEnabled && IsControlEnabled;
            var previousEnabled = PreviousIsEnabled && PreviousIsControlEnabled;

            // Only trigger changes when there's an actual state transition
            if (currentEnabled != previousEnabled || IsAssetChangeRequested) {
                if (IsAssetChangeRequested) {
                    IsAssetChangeRequested = false;
                }

                var enable = currentEnabled && Character != null;
                if (enable && !previousEnabled) {
                    // Transitioning from disabled to enabled
                    FadeInControl();
                } else if (!enable && previousEnabled) {
                    // Transitioning from enabled to disabled
                    FadeOutControl();
                }
            }

            // Update previous state
            PreviousIsEnabled = IsEnabled;
            PreviousIsControlEnabled = IsControlEnabled;
        }

        void SetupLimbIkTarget(LimbIKData limb, AnchorAsset anchor, float? weight) {

            if (weight != null) {
                var w = weight.Value;
                limb.PositionWeight = w;
                limb.RotationWeight = w;
            }
            limb.IkTarget = anchor;
            limb.Broadcast();
        }

        void SetupLimbBendGoalTarget(LimbIKData limb, AnchorAsset bendGoalTarget, float? weight) {

            if (weight != null) {
                var w = weight.Value;
                limb.BendGoalWeight = w;
            }
            limb.BendGoalTarget = bendGoalTarget;
            limb.Broadcast();
        }

        void SetupLeftLimbBendGoalTarget(AnchorAsset bendGoalTarget, float? weight = null) {
            SetupLimbBendGoalTarget(Character.LeftHandIK, bendGoalTarget, weight);
        }

        void SetupRightLimbBendGoalTarget(AnchorAsset bendGoalTarget, float? weight = null) {
            SetupLimbBendGoalTarget(Character.RightHandIK, bendGoalTarget, weight);
        }

        void TransitionIkLimbs(bool isEnabledWanted, Transition transition, TweenCallback onComplete = null) {
            if (transition == null) return;

            Character.LeftHandIK.Enabled = true;
            Character.RightHandIK.Enabled = true;
            var baseLayerIndex = 1 + Array.FindIndex(Character.OverlappingAnimations, l => l.CustomLayerID == SavedBindingData.BaseAnimationLayer.CustomLayerID);
            if (isEnabledWanted && baseLayerIndex > 0) {
                CharacterAnimancer.Layers[baseLayerIndex].SetWeight(0);
                CharacterAnimancerClone.Layers[baseLayerIndex].SetWeight(0);
            }

            IkTween?.Kill();
            IkTween = DOTween
                .To(
                    () => Character.LeftHandIK.PositionWeight,
                    (float v) => {
                        Character.LeftHandIK.PositionWeight = v;
                        Character.LeftHandIK.RotationWeight = v;
                        Character.LeftHandIK.BendGoalWeight = v;
                        Character.LeftHandIK.Broadcast();
                        Character.RightHandIK.PositionWeight = v;
                        Character.RightHandIK.RotationWeight = v;
                        Character.RightHandIK.BendGoalWeight = v;
                        Character.RightHandIK.Broadcast();
                        if (baseLayerIndex > 0) {
                            CharacterAnimancer.Layers[baseLayerIndex].SetWeight(v);
                            CharacterAnimancerClone.Layers[baseLayerIndex].SetWeight(v);
                        }
                    },
                    isEnabledWanted ? 1f : 0f,
                    transition.Time
                )
                .SetEase(transition.Ease);

            if (onComplete != null) IkTween.OnComplete(onComplete);
        }

        void RecordState() {

            // Create Base Layer
            SavedBindingData.SetDataInput(nameof(SavedBindingData.BaseAnimationLayer), StructuredData.Create<OverlayingAnimationDefinition>(oad =>{
                oad.Animation = Character.DefaultIdleAnimation;
                oad.Weight = 1f;
                oad.Speed = 1f;
                oad.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers, AnimationMaskedBodyPart.RightFingers };
                oad.Additive = false;
                oad.Looping = false;
            }), true);

            // Imitate the prop transforms
            AnchorAsset propAnchor = Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            var propWorldPosition = propAnchor.Transform.Position = propAnchor.GameObject.transform.position = HeldProp.GameObject.transform.position;
            propAnchor.GameObject.transform.rotation = HeldProp.GameObject.transform.rotation;
            propAnchor.Transform.Rotation = HeldProp.GameObject.transform.rotation.eulerAngles;
            propAnchor.Transform.Scale = HeldProp.Transform.Scale;

            // Set anchors' position to where the prop is
            AnchorAsset moverAnchor = Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            AnchorAsset targetAnchor = Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            moverAnchor.Transform.Position = moverAnchor.GameObject.transform.position = propWorldPosition;
            targetAnchor.Transform.Position = targetAnchor.GameObject.transform.position = propWorldPosition;

            Helper.SetParent(propAnchor, targetAnchor);
            Helper.SetParent(targetAnchor, moverAnchor);
            Helper.SetParent(moverAnchor, Character);
            Helper.CopyTransform(moverAnchor.Transform, SavedBindingData.MoverAnchor, true);


            var leftAnchor = CreateIKTargetHandAnchor(HumanBodyBones.LeftHand, targetAnchor);
            var rightAnchor = CreateIKTargetHandAnchor(HumanBodyBones.RightHand, targetAnchor);
            Helper.CopyTransform(leftAnchor.Transform, SavedBindingData.LeftHandAnchor, true);
            Helper.CopyTransform(rightAnchor.Transform, SavedBindingData.RightHandAnchor, true);

            // Record coordinates to reset later
            var propNeutralTransform = propAnchor.Transform;

            Helper.SetParent(propAnchor, leftAnchor);
            Helper.CopyTransform(propAnchor.Transform, SavedBindingData.PropLeft, true);

            propAnchor.Transform.Position = propNeutralTransform.Position;
            propAnchor.Transform.Rotation = propNeutralTransform.Rotation;
            Helper.SetParent(propAnchor, rightAnchor);
            Helper.CopyTransform(propAnchor.Transform, SavedBindingData.PropRight, true);

            SetDataInput(nameof(IsStateSaved), true, true);

            CleanDestroy(propAnchor);
            CleanDestroy(leftAnchor);
            CleanDestroy(rightAnchor);
            CleanDestroy(targetAnchor);
            CleanDestroy(moverAnchor);
        }

        void ApplyBindingData() {
            AnchorAsset moverAnchor = Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == MoverAnchorAssetId) ?? Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            moverAnchor.Name = $"{CHARACTER_ANIM_LAYER_ID_PREFIX} Mover";
            Scene.UpdateNewAssetName(moverAnchor);
            MoverAnchorAssetId = moverAnchor.Id;

            AnchorAsset targetAnchor = Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == TargetAnchorAssetId) ?? Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            targetAnchor.Name = $"{CHARACTER_ANIM_LAYER_ID_PREFIX}🔒🎯";
            Scene.UpdateNewAssetName(targetAnchor);
            TargetAnchorAssetId = targetAnchor.Id;

            AnchorAsset leftHandAnchor = Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == LeftHandAnchorAssetId) ?? Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            leftHandAnchor.Name = $"{CHARACTER_ANIM_LAYER_ID_PREFIX}🔒🫲";
            Scene.UpdateNewAssetName(leftHandAnchor);
            LeftHandAnchorAssetId = leftHandAnchor.Id;

            AnchorAsset rightHandAnchor = Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == RightHandAnchorAssetId) ?? Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            rightHandAnchor.Name = $"{CHARACTER_ANIM_LAYER_ID_PREFIX}🔒🫱";
            Scene.UpdateNewAssetName(rightHandAnchor);
            RightHandAnchorAssetId = rightHandAnchor.Id;

            Helper.CopyTransform(SavedBindingData.MoverAnchor, moverAnchor.Transform);
            Helper.CopyTransform(SavedBindingData.LeftHandAnchor, leftHandAnchor.Transform);
            Helper.CopyTransform(SavedBindingData.RightHandAnchor, rightHandAnchor.Transform);

            moverAnchor.Attachable.Parent = Character;
            moverAnchor.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.TransformPath;
            moverAnchor.Attachable.AttachToTransform = null;

            targetAnchor.Attachable.Parent = moverAnchor;
            leftHandAnchor.Attachable.Parent = targetAnchor;
            rightHandAnchor.Attachable.Parent = targetAnchor;

            SetupLimbIkTarget(Character.LeftHandIK, leftHandAnchor, 0f);
            SetupLimbIkTarget(Character.RightHandIK, rightHandAnchor, 0f);

            var bendGoalControlOptions = AdvancedOptions.BendGoalControlOptionsInstance;
            if (bendGoalControlOptions.IsEnabled) {
                SetupLeftLimbBendGoalTarget(bendGoalControlOptions.LeftHandIkBendGoalTarget, 0f);
                SetupRightLimbBendGoalTarget(bendGoalControlOptions.RightHandIkBendGoalTarget, 0f);
            }

            moverAnchor.Broadcast();
            targetAnchor.Broadcast();
            leftHandAnchor.Broadcast();
            rightHandAnchor.Broadcast();
            HeldProp.Broadcast();
        }

        AnchorAsset CreateIKTargetHandAnchor(HumanBodyBones targetBone, AnchorAsset parent) {
            AnchorAsset anchor = Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            anchor.Transform.CopyFromWorldTransform(Character.Animator.GetBoneTransform(targetBone));
            anchor.Transform.ApplyAsWorldTransform(anchor.GameObject.transform);
            Helper.SetParent(anchor, parent);
            return anchor;
        }

        void AttachGamepad(bool isRightSideWanted) {

            HumanBodyBones bone;
            if (isRightSideWanted) {
                Helper.CopyTransform(SavedBindingData.PropRight, HeldProp.Transform);
                bone = HumanBodyBones.RightHand;
            } else {
                Helper.CopyTransform(SavedBindingData.PropLeft, HeldProp.Transform);
                bone = HumanBodyBones.LeftHand;
            }

            HeldProp.Attachable.Parent = Character;
            HeldProp.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.HumanBodyBone;
            HeldProp.Attachable.AttachToBone = bone;
            HeldProp.Broadcast();

            var prefix = $"{CHARACTER_ANIM_LAYER_ID_PREFIX}🔒 ";
            if (!HeldProp.Name.StartsWith(prefix)) {
                HeldProp.Name = $"{prefix}{HeldProp.Name}";
                HeldProp.Broadcast();
                Scene.UpdateNewAssetName(HeldProp);
            }
        }

        void ClearSavedState() {
            ResetDataInput(nameof(SavedBindingData), true);
            SetDataInput(nameof(IsStateSaved), false, true);
        }

        void FadeInControl() {
            ApplyBindingData();
            HandleCharacterAnimationChange(); // OK here as it's bound to the update layer
            if (AdvancedOptions.TurnOnPropOnEnter) {
                HeldProp.SetDataInput(nameof(HeldProp.Enabled), true, true);
            }
            TransitionIkLimbs(true, AdvancedOptions.LimbIkEnterTransition, () =>{
                AttachGamepad(AdvancedOptions.IsPropHeldInRightHandWanted);
            });
        }

        void FadeOutControl() {

            // Clear managed character animation layers immediately when disabling
            RequestCharacterAnimationChange();

            // Detach prop
            Helper.UnsetParent(HeldProp);
            var prefix = $"{CHARACTER_ANIM_LAYER_ID_PREFIX}🔒 ";
            if (HeldProp.Name.StartsWith(prefix)) {
                HeldProp.Name = HeldProp.Name.Substring(prefix.Length);
                HeldProp.Broadcast();
                Scene.UpdateNewAssetName(HeldProp);
            }

            if (AdvancedOptions.TurnOffPropOnLeave) {
                HeldProp.SetDataInput(nameof(HeldProp.Enabled), false, true);
            }

            TransitionIkLimbs(false, AdvancedOptions.LimbIkLeaveTransition, () => {
                var leftIk = Character.LeftHandIK;
                leftIk.Enabled = false;
                leftIk.IkTarget = null;

                if (AdvancedOptions.BendGoalControlOptionsInstance.IsEnabled) {
                    leftIk.BendGoalTarget = null;
                }
                leftIk.Broadcast();

                var rightIk = Character.RightHandIK;
                rightIk.Enabled = false;
                rightIk.IkTarget = null;
                if (AdvancedOptions.BendGoalControlOptionsInstance.IsEnabled) {
                    rightIk.BendGoalTarget = null;
                }
                rightIk.Broadcast();

                ClearAllSceneAssets();

                RequestCharacterAnimationChange();
            });
        }
        void ClearAllSceneAssets() {
            CleanDestroy(LeftHandAnchor);
            LeftHandAnchorAssetId = Guid.Empty;
            CleanDestroy(RightHandAnchor);
            RightHandAnchorAssetId = Guid.Empty;
            CleanDestroy(TargetAnchor);
            TargetAnchorAssetId = Guid.Empty;
            CleanDestroy(MoverAnchor);
            MoverAnchorAssetId = Guid.Empty;
            ShowToast("ASSET_CLEANED_NOTIFICATION".Localized());
        }

        void CleanDestroy(GameObjectAsset g) {
            if (g == null) return;
            try {
                Scene.RemoveAsset(g.Id);
            } catch {}
        }

        public class AdvancedBindings : StructuredData<InputReceiverAsset>, ICollapsibleStructuredData {

            protected override void OnCreate() {
                base.OnCreate();
                BendGoalControlOptionsInstance.Parent = Parent;

                Watch(nameof(BendGoalControlOptionsInstance), HandleBendGoalControlOptionsChanged);
            }

            [DataInput]
            [Label("HOLD_PROP_IN_RIGHT_HAND")]
            public bool IsPropHeldInRightHandWanted;

            [DataInput]
            [Label("BEND_GOAL_CONTROLS")]
            public BendGoalControlOptions BendGoalControlOptionsInstance;

            [DataInput]
            [Label("ENTER_TRANSITION")]
            public Transition LimbIkEnterTransition;

            [DataInput]
            [Label("LEAVE_TRANSITION")]
            public Transition LimbIkLeaveTransition;

            [DataInput]
            [Label("TURN_ON_PROP_ON_ENTER")]
            public bool TurnOnPropOnEnter;

            [DataInput]
            [Label("TURN_OFF_PROP_ON_LEAVE")]
            public bool TurnOffPropOnLeave;

            public string GetHeader() {
                return "ADVANCED_OPTIONS";
            }

            void HandleBendGoalControlOptionsChanged() {
                // Character control check
                if (!Parent.IsCharacterControlActive) return;

                // Bend goal controls check
                if (!BendGoalControlOptionsInstance.IsEnabled) return;

                Parent.SetupLeftLimbBendGoalTarget(BendGoalControlOptionsInstance.LeftHandIkBendGoalTarget);
                Parent.SetupRightLimbBendGoalTarget(BendGoalControlOptionsInstance.RightHandIkBendGoalTarget);
            }
        }

        public class BindingDefinitions : StructuredData<InputReceiverAsset>, ICollapsibleStructuredData {

            [DataInput]
            public OverlayingAnimationDefinition BaseAnimationLayer;

            [DataInput]
            public TransformData PropLeft;

            [DataInput]
            public TransformData PropRight;

            [DataInput]
            public TransformData MoverAnchor;

            [DataInput]
            public TransformData LeftHandAnchor;

            [DataInput]
            public TransformData RightHandAnchor;

            public string GetHeader() {
                return "SAVED_BINDINGS_DATA";
            }
        }

        public class BendGoalControlOptions : StructuredData<InputReceiverAsset> {

            [DataInput]
            [Label("ENABLED")]
            [Description("BEND_GOAL_CONTROLS_OPTIONS_ENABLED_DESCRIPTION")]
            public bool IsEnabled;

            [DataInput]
            [Label("LEFT_HAND_IK_BEND_GOAL_TARGET")]
            public AnchorAsset LeftHandIkBendGoalTarget;

            [DataInput]
            [Label("RIGHT_HAND_IK_BEND_GOAL_TARGET")]
            public AnchorAsset RightHandIkBendGoalTarget;
        }
    }
}
