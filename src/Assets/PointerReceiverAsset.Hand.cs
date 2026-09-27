using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Plugins.Core.Assets.Utility;
using static Warudo.Plugins.Core.Assets.Character.CharacterAsset;

namespace FlameStream
{
    public partial class PointerReceiverAsset : ReceiverAsset {

        [DataInput]
        [Hidden]
        public Guid HandAnchorAssetId;

        [DataInput]
        [Hidden]
        public Guid ActiveHandPoleAnchorAssetId;

        public PoseState handState;
        PoseState lastHandState;
        PoseState setupModeHandState;
        Tween tweenHandPosition;
        Tween tweenHandRotation;
        Tween tweenHandProgress;
        float handTweenProgress = 1f;
        Vector3 handPositionOffset;
        Vector3 handRotationOffset;
        AnchorAsset handAnchor;
        public AnchorAsset HandAnchor => handAnchor != null ? handAnchor : (handAnchor = GetHandIK());
        AnchorAsset handSetupRootAnchor;
        AnchorAsset activeHandPoleAnchor;
        public AnchorAsset ActiveHandPoleAnchor => activeHandPoleAnchor != null ? activeHandPoleAnchor : (activeHandPoleAnchor = GetActiveHandPoleAnchor());
        Tween tweenLeftIK;
        Tween tweenRightIK;
        Tween tweenActivePole;
        float currentActivePoleWeight = 0f;
        public float ActivePoleWeight => currentActivePoleWeight;
        bool lastIsRightHanded = true;
        GameObject cursorIndicatorGo;
        Material cursorIndicatorMat;

        public bool IsActiveHandPoleActive => (IsEnabled && IsHandEnabled && !isHandDisabledByOutOfBound && Character != null || IsInActiveHandPoleVisualSetup) && ActiveHand != null && ActiveHand.ElbowPole != null && (ActiveHand.ElbowPole.UsePole || IsInActiveHandPoleVisualSetup);
        public bool IsInActiveHandPoleVisualSetup => ActiveHand != null && ActiveHand.ElbowPole != null && ActiveHand.ElbowPole.Pole != null && ActiveHand.ElbowPole.Pole.InVisualSetupMode;
        protected bool isExitingVisualSetup = false;

        protected void TriggerVisualSetupAnchor(VisualSetupTransform tr, bool isApply) {
            if (tr == null || !tr.InVisualSetupMode) return;
            if (tr.setupAnchor != null) {
                if (isApply) tr.setupAnchor.ApplyTrigger();
                else tr.setupAnchor.CancelTrigger();
            }
            if (tr.InVisualSetupMode) {
                tr.ExitVisualSetup(isApply);
            }
        }

        AnchorParentType currentActiveHandPoleParentType = AnchorParentType.World;

        public AnchorAsset GetActiveHandPoleAnchor(bool skipAutoCreate = false) {
            bool shouldExist = ActiveHand != null && ActiveHand.ElbowPole != null && (ActiveHand.ElbowPole.UsePole || (ActiveHand.ElbowPole.Pole != null && ActiveHand.ElbowPole.Pole.InVisualSetupMode));
            if (!shouldExist && !skipAutoCreate) {
                return null;
            }
            var anchor = GetAsset<AnchorAsset>(ref ActiveHandPoleAnchorAssetId, out bool isNewlyCreated, skipAutoCreate, newAssetName: $"⚓-{LAYER_NAME_PREFIX} Active Hand Pole IK", category: "FS_ASSET_CATEGORY_INPUT".Localized());
            if (anchor != null && anchor.Name.StartsWith($"🔒⚓-{LAYER_NAME_PREFIX}")) {
                anchor.Name = anchor.Name.Replace("🔒⚓-", "⚓-");
                Scene?.UpdateNewAssetName(anchor);
                anchor.Broadcast();
                Helper.AssignAssetToCategory(Scene, anchor, "FS_ASSET_CATEGORY_INPUT".Localized());
            }
            if (isNewlyCreated && anchor != null) {
                InitializeActiveHandPoleAnchor(anchor);
            }
            return anchor;
        }

        void InitializeActiveHandPoleAnchor(AnchorAsset anchor) {
            var parentType = ActiveHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
            SetAnchorParentInitial(anchor, parentType, IsRightHanded);
            if (ActiveHand?.ElbowPole?.Pole != null && ActiveHand.ElbowPole.Pole.Position != Vector3.zero) {
                anchor.Transform.Position = ActiveHand.ElbowPole.Pole.Position;
                anchor.Transform.Rotation = ActiveHand.ElbowPole.Pole.Rotation;
            } else {
                Vector3 defaultWorld = GetDefaultElbowPoleWorldPosition(IsRightHanded);
                TransformAnchorWorldToLocal(parentType, defaultWorld, Quaternion.identity, out Vector3 localPos, out Vector3 localRot, IsRightHanded);
                anchor.Transform.Position = localPos;
                anchor.Transform.Rotation = localRot;
                if (ActiveHand?.ElbowPole?.Pole != null) {
                    ActiveHand.ElbowPole.Pole.Position = localPos;
                    ActiveHand.ElbowPole.Pole.Rotation = localRot;
                    ActiveHand.ElbowPole.Pole.SetDataInput(nameof(ActiveHand.ElbowPole.Pole.Position), localPos, true);
                    ActiveHand.ElbowPole.Pole.SetDataInput(nameof(ActiveHand.ElbowPole.Pole.Rotation), localRot, true);
                }
            }
            anchor.Transform.Scale = Vector3.one;
            anchor.Transform.Broadcast();
        }

        public void DestroyActiveHandPoleAnchor() {
            if (ActiveHandPoleAnchorAssetId != Guid.Empty) {
                try { Scene?.RemoveAsset(ActiveHandPoleAnchorAssetId); } catch {}
                ActiveHandPoleAnchorAssetId = Guid.Empty;
                activeHandPoleAnchor = null;
            }
        }

        public void OnHandDelegationChanged() {
            if (activeCoordinator != null) {
                tweenLeftIK?.Kill();
                tweenRightIK?.Kill();
            } else {
                OnInputAffectingHandSetupChange();
            }
        }


        void OnCreateMoveHand() {
            Watch(nameof(IsEnabled), delegate {
                OnInputAffectingHandSetupChange();
                bool isHandActive = IsEnabled && IsHandEnabled && !isHandDisabledByOutOfBound && Character != null;
                TransitionActivePole(isHandActive);
            });
            Watch(nameof(IsHandEnabled), delegate {
                OnInputAffectingHandSetupChange();
                bool isHandActive = IsEnabled && IsHandEnabled && !isHandDisabledByOutOfBound && Character != null;
                TransitionActivePole(isHandActive);
            });
            Watch(nameof(IsRightHanded), delegate {
                if (ActiveHand != null && ActiveHand.IsRightHanded != IsRightHanded) {
                    ActiveHand.IsRightHanded = IsRightHanded;
                    ActiveHand.SetDataInput(nameof(ActiveHand.IsRightHanded), IsRightHanded, true);
                }
                OnHandSideChange();
                OnInputAffectingHandSetupChange();
                OnRestHandSettingChange();
            });
            Watch(nameof(Character), delegate {
                OnInputAffectingHandSetupChange();
                if (ActiveHand?.ElbowPole?.ParentType == AnchorParentType.Character || ActiveHand?.ElbowPole?.ParentType == AnchorParentType.CharacterShoulder) {
                    var poleAnchor = GetActiveHandPoleAnchor(true);
                    if (poleAnchor != null) AttachAnchorToParent(poleAnchor, ActiveHand.ElbowPole.ParentType, IsRightHanded);
                }
            });

            Watch(nameof(HandMouseMode), delegate { OnHandStateSettingChange(); });
            Watch(nameof(HandPenMode), delegate { OnHandStateSettingChange(); });
            Watch(nameof(HandDynamicRotation), delegate {
                SetupHandDynamicRotation();
            });
            SetupHandDynamicRotation();
            SetupHandStateTransforms();

            if (ActiveHand?.ElbowPole != null) {
                ActiveHand.ElbowPole.Parent = this;
                ActiveHand.ElbowPole.IsActiveHand = true;
                ActiveHand.ElbowPole.OnPoleSettingChange = () => {
                    bool isHandActive = IsEnabled && IsHandEnabled && !isHandDisabledByOutOfBound && Character != null;
                    TransitionActivePole(isHandActive);
                };
                ActiveHand.ElbowPole.OnParentTypeChange = () => OnActiveHandPoleParentTypeChange();
                ActiveHand.ElbowPole.OnPoleTransformChange = () => SetupActiveHandPoleTransform();
            }
            SetupActiveHandPoleTransform();
        }

        void SetupHandStateTransforms() {
            if (HandMouseMode != null) {
                HandMouseMode.Transform.DefaultOriginSphereColor = null;
                HandMouseMode.Transform.OriginIndicatorParentGetter = null;
                if (HandMouseMode.ActiveState != null) {
                    HandMouseMode.ActiveState.Transform.DefaultOriginSphereColor = null;
                    HandMouseMode.ActiveState.Transform.OriginIndicatorParentGetter = null;
                }
            }

            if (HandPenMode != null) {
                HandPenMode.Transform.DefaultOriginSphereColor = null;
                HandPenMode.Transform.OriginIndicatorParentGetter = null;
                if (HandPenMode.ActiveState != null) {
                    HandPenMode.ActiveState.Transform.DefaultOriginSphereColor = null;
                    HandPenMode.ActiveState.Transform.OriginIndicatorParentGetter = null;
                }
            }
        }

        void SetupHandDynamicRotation() {
            if (HandDynamicRotation == null) return;
            HandDynamicRotation.LocalizationKey = "DYNAMIC_ROTATION_PIVOT";
            HandDynamicRotation.AutoRotationEvaluator = CalculateAutoHandRotation;
            HandDynamicRotation.GetParentAnchor = () => screenAsset ?? GetSurfaceScreen();
            HandDynamicRotation.GetRestPosition = () => NeutralHandPosition != null ? NeutralHandPosition.Position : Vector3.zero;
            HandDynamicRotation.GetIsRightHanded = () => IsRightHanded;
            HandDynamicRotation.GetDefaultPivotPosition = () => {
                bool isRight = IsRightHanded;
                var anim = Character != null ? (Character.Animator ?? Character.CloneAnimator) : null;
                var elbowBone = anim != null ? anim.GetBoneTransform(isRight ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm) : null;
                Vector3 elbowWorld = elbowBone != null ? elbowBone.position : GetNaturalRestingElbowWorldPosition(isRight);
                var s = screenAsset ?? GetSurfaceScreen();
                if (s != null) {
                    Quaternion screenRot = s.GameObject != null ? s.GameObject.transform.rotation : Quaternion.Euler(s.Transform.Rotation);
                    Vector3 screenPos = s.GameObject != null ? s.GameObject.transform.position : s.Transform.Position;
                    return Quaternion.Inverse(screenRot) * (elbowWorld - screenPos);
                }
                return (NeutralHandPosition != null ? NeutralHandPosition.Position : Vector3.zero) + new Vector3(isRight ? -0.1f : 0.1f, -0.3f, 0f);
            };
            HandDynamicRotation.OnEnterVisualSetup = (dr) => { HandleDynamicRotationEnterVisualSetup(); };
            HandDynamicRotation.OnExitVisualSetup = (isApplied) => { HandleDynamicRotationExitVisualSetup(isApplied); };
        }

        void OnReadyHand() {
            lastIsRightHanded = IsRightHanded;
            SetupHandDynamicRotation();
            SetupHandStateTransforms();

            currentActiveHandPoleParentType = ActiveHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
            if (ActiveHand?.ElbowPole != null) {
                ActiveHand.ElbowPole.Parent = this;
                ActiveHand.ElbowPole.IsActiveHand = true;
                ActiveHand.ElbowPole.OnPoleSettingChange = () => {
                    bool isHandActive = IsEnabled && IsHandEnabled && !isHandDisabledByOutOfBound && Character != null;
                    TransitionActivePole(isHandActive);
                };
                ActiveHand.ElbowPole.OnParentTypeChange = () => OnActiveHandPoleParentTypeChange();
                ActiveHand.ElbowPole.OnPoleTransformChange = () => SetupActiveHandPoleTransform();
                ActiveHand.ElbowPole.SyncUI();
            }
            SetupActiveHandPoleTransform();

            NeutralHandPosition.LocalizationKey = "NEUTRAL_POSITION";
            NeutralHandPosition.OnEnterVisualSetup = (tr) => { HandleNeutralEnterVisualSetup(); };
            NeutralHandPosition.OnExitVisualSetup = (a) => { HandleNeutralExitVisualSetup(a); };
            NeutralHandPosition.OnCreateAnchor = (tr, a) => {
                a.Attachable.Parent = screenAsset ?? GetSurfaceScreen();
            };

            HandMouseMode.OnAnimationChange = (p) => { HandleHandAnimationChange((PoseState)p); };
            HandMouseMode.OnAnimationWeightChange = (p, w, r) => { HandleHandAnimationWeightChange((PoseState)p, w, r); };
            HandMouseMode.OnCreateAnchor = (p, tr, a) => { HandleHandCreateAnchor((PoseState)p, tr, a); };
            HandMouseMode.OnEnterVisualSetup = (p) => { HandleHandEnterVisualSetup((PoseState)p); };
            HandMouseMode.OnExitVisualSetup = (p, ia) => { HandleHandExitVisualSetup((PoseState)p, ia); };

            HandPenMode.OnAnimationChange = (p) => { HandleHandAnimationChange((PoseState)p); };
            HandPenMode.OnAnimationWeightChange = (p, w, r) => { HandleHandAnimationWeightChange((PoseState)p, w, r); };
            HandPenMode.OnCreateAnchor = (p, tr, a) => { HandleHandCreateAnchor((PoseState)p, tr, a); };
            HandPenMode.OnEnterVisualSetup = (p) => { HandleHandEnterVisualSetup((PoseState)p); };
            HandPenMode.OnExitVisualSetup = (p, ia) => { HandleHandExitVisualSetup((PoseState)p, ia); };

            OnInputAffectingHandSetupChange(instant: true);
        }

        void OnUpdateHand() {
            if (!IsEnabled || !IsHandEnabled) return;
            if (handAnchor == null) handAnchor = GetHandIK();
            if (handAnchor == null) return;

            // Self-healing: if Dominant Limb IK was cleared or deactivated while we are active, restore it unless another asset is controlling it
            if (activeCoordinator == null && Character != null && !isHandDisabledByOutOfBound) {
                var dominantLimb = IsRightHanded ? Character.RightHandIK : Character.LeftHandIK;
                var activeTween = IsRightHanded ? tweenRightIK : tweenLeftIK;
                bool isTweening = activeTween != null && activeTween.IsActive();
                if (!isTweening && dominantLimb != null && (dominantLimb.IkTarget == null || dominantLimb.IkTarget == handAnchor)) {
                    if (!dominantLimb.Enabled || dominantLimb.IkTarget == null) {
                        dominantLimb.Enabled = true;
                        dominantLimb.IkTarget = handAnchor;
                        dominantLimb.PositionWeight = 1.0f;
                        dominantLimb.RotationWeight = 1.0f;
                        dominantLimb.BendGoalWeight = (ActiveHand != null && ActiveHand.ElbowPole != null && ActiveHand.ElbowPole.UsePole) ? 1.0f : 0.0f;
                        dominantLimb.Broadcast();
                    }
                }
            }

            if (IsInActiveHandPoleVisualSetup && Character != null) {
                var dominantLimb = IsRightHanded ? Character.RightHandIK : Character.LeftHandIK;
                if (dominantLimb != null && activeCoordinator == null) {
                    dominantLimb.BendGoalTarget = ActiveHandPoleAnchor;
                    dominantLimb.BendGoalWeight = 1.0f;
                    dominantLimb.Broadcast();
                }
                var pole = ActiveHand?.ElbowPole?.Pole;
                var livePole = GetActiveHandPoleAnchor(true);
                if (livePole != null && pole != null && pole.setupAnchor != null) {
                    Vector3 polePos = pole.setupAnchor.Transform.Position;
                    Vector3 poleRot = pole.setupAnchor.Transform.Rotation;
                    if (livePole.Transform.Position != polePos || livePole.Transform.Rotation != poleRot) {
                        livePole.Transform.Position = polePos;
                        livePole.Transform.Rotation = poleRot;
                        livePole.Transform.Broadcast();
                    }
                }
            }

            var nonDominantLimb = IsRightHanded ? Character?.LeftHandIK : Character?.RightHandIK;
            if (nonDominantLimb != null && nonDominantLimb.IkTarget == handAnchor) {
                nonDominantLimb.IkTarget = null;
                nonDominantLimb.PositionWeight = 0f;
                nonDominantLimb.RotationWeight = 0f;
                nonDominantLimb.BendGoalWeight = 0f;
                nonDominantLimb.Broadcast();
            }

            if (IsInRestHandVisualSetup || IsInActiveHandPoleVisualSetup) {
                handState = null;
            } else if (inSetupMode) {
                handState = setupModeHandState;
                CalculateSetupHandOffsets();
            } else {
                handState = GetHandState(Source, Button1);
            }
            if (lastHandState != handState) {
                OnHandStateChange();
            }

            var ikt = handAnchor.Transform;
            ikt.Position = handPositionOffset;
            ikt.Rotation = handRotationOffset;
            ikt.Broadcast();

            lastHandState = handState;
        }

        void OnHandStateChange() {
            var st = setupModeHandState ?? handState;

            var tr = GetGlobalCalculatorTransform();
            var r = Vector3.zero; // Used to allow 360+ degree rotation

            if (st != null) {
                if (st != HandMouseMode) {
                    tr.Translate(HandMouseMode.Transform.Position);
                    tr.Rotate(HandMouseMode.Transform.Rotation);
                    r += HandMouseMode.Transform.Rotation;
                }

                if (st == HandPenMode.ActiveState) {
                    tr.Translate(HandPenMode.Transform.Position);
                    tr.Rotate(HandPenMode.Transform.Rotation);
                    r += HandPenMode.Transform.Rotation;
                }

                tr.Translate(st.Transform.Position);
                tr.Rotate(st.Transform.Rotation);
                r += st.Transform.Rotation;
            }

            var time = (inSetupMode || !isReady) ? 0 : st?.EnterTransition.Time ?? HandMouseMode.EnterTransition.Time;
            var ease = st?.EnterTransition.Ease ?? HandMouseMode.EnterTransition.Ease;
            SetHandOffsetTransforms(tr.position, r, time, ease);

            HandMouseMode.StartAnimationTransition(st != null);
            HandMouseMode.ActiveState.StartAnimationTransition(st == HandMouseMode.ActiveState);
            HandPenMode.StartAnimationTransition(st == HandPenMode || st == HandPenMode.ActiveState);
            HandPenMode.ActiveState.StartAnimationTransition(st == HandPenMode.ActiveState);
        }

        void CalculateSetupHandOffsets() {
            if (setupModeHandState == null) return;
            var st = setupModeHandState;

            var tr = GetGlobalCalculatorTransform();
            var r = Vector3.zero; // Used to allow 360+ degree rotation
            if (st != HandMouseMode) {
                tr.Translate(HandMouseMode.Transform.Position);
                tr.Rotate(HandMouseMode.Transform.Rotation);
                r += HandMouseMode.Transform.Rotation;
            }

            // TODO: generalize
            if (st == HandPenMode.ActiveState) {
                tr.Translate(HandPenMode.Transform.Position);
                tr.Rotate(HandPenMode.Transform.Rotation);
                r += HandPenMode.Transform.Rotation;
            }

            var trr = handSetupRootAnchor.Transform;
            trr.Position = tr.position;
            trr.Rotation = r;
            trr.Broadcast();

            var trs = st.Transform.setupAnchor.Transform;
            tr.Translate(trs.Position);
            tr.Rotate(trs.Rotation);
            r += trs.Rotation;

            handPositionOffset = tr.position;
            handRotationOffset = r;
        }

        void OnHandStateSettingChange() {
            OnHandStateChange();
        }

        public void OnInputAffectingHandSetupChange(bool instant = false) {
            if (!isReady) return;

            var isHandActive = IsEnabled && IsHandEnabled && !isHandDisabledByOutOfBound && Character != null;
            if (!isHandActive) {
                handState = null;
                OnHandStateChange();
            }

            if (isHandActive) {
                cursorAnchorAsset = GetCursorAnchor();
                if (cursorAnchorAsset != null && screenAsset != null) {
                    cursorAnchorAsset.Attachable.Parent = screenAsset;
                }

                handAnchor = GetHandIK();
                if (handAnchor != null && cursorAnchorAsset != null) {
                    handAnchor.Attachable.Parent = cursorAnchorAsset;
                    handAnchor.Broadcast();
                }
            }

            var dominantLimb = Character != null ? (IsRightHanded ? Character.RightHandIK : Character.LeftHandIK) : null;
            if (Character != null) {
                if (IsRightHanded) {
                    EnableLimb(isHandActive, dominantLimb, handAnchor, ref tweenRightIK, instant);
                } else {
                    EnableLimb(isHandActive, dominantLimb, handAnchor, ref tweenLeftIK, instant);
                }

                var nonDominantLimb = IsRightHanded ? Character.LeftHandIK : Character.RightHandIK;
                if (nonDominantLimb != null && nonDominantLimb.IkTarget == handAnchor) {
                    nonDominantLimb.IkTarget = null;
                    nonDominantLimb.PositionWeight = 0f;
                    nonDominantLimb.RotationWeight = 0f;
                    nonDominantLimb.BendGoalWeight = 0f;
                    nonDominantLimb.Broadcast();
                }
            } else if (!isHandActive) {
                DestroyHandIK();
                DestroyCursorAnchor();
            }
        }

        public void OnHandSideChange() {
            if (!isReady) return;
            if (Character == null) return;

            cachedAutoHandRotX = float.NaN;
            cachedAutoHandRotation = Vector3.zero;
            autoHandSettleCountdown = 0;

            bool wasRightHanded = lastIsRightHanded;
            if (wasRightHanded != IsRightHanded) {
                MirrorRestHandTransforms(wasRightHanded);
                MirrorActiveHandPoleTransform(wasRightHanded);
                MirrorActiveHandTransforms(wasRightHanded);
                lastIsRightHanded = IsRightHanded;
            }

            // Clear old dominant hand IK
            var oldDominantLimb = !IsRightHanded ? Character.RightHandIK : Character.LeftHandIK;
            if (oldDominantLimb != null && (oldDominantLimb.IkTarget == handAnchor || oldDominantLimb.BendGoalTarget == activeHandPoleAnchor)) {
                if (oldDominantLimb.IkTarget == handAnchor) oldDominantLimb.IkTarget = null;
                if (oldDominantLimb.BendGoalTarget == activeHandPoleAnchor) oldDominantLimb.BendGoalTarget = null;
                oldDominantLimb.PositionWeight = 0f;
                oldDominantLimb.RotationWeight = 0f;
                oldDominantLimb.BendGoalWeight = 0f;
                oldDominantLimb.Enabled = false;
                oldDominantLimb.Broadcast();
            }

            // Clear old rest hand IK
            var oldRestLimb = IsRightHanded ? Character.RightHandIK : Character.LeftHandIK;
            if (oldRestLimb != null && (oldRestLimb.IkTarget == restHandAnchor || oldRestLimb.BendGoalTarget == restHandPoleAnchor)) {
                if (oldRestLimb.IkTarget == restHandAnchor) oldRestLimb.IkTarget = null;
                if (oldRestLimb.BendGoalTarget == restHandPoleAnchor) oldRestLimb.BendGoalTarget = null;
                oldRestLimb.PositionWeight = 0f;
                oldRestLimb.RotationWeight = 0f;
                oldRestLimb.BendGoalWeight = 0f;
                oldRestLimb.Enabled = false;
                oldRestLimb.Broadcast();
            }

            var matchingLayerNames = new List<string> {
                HandMouseMode.GetAnimationLayerId(LAYER_NAME_PREFIX),
                HandMouseMode.ActiveState.GetAnimationLayerId(LAYER_NAME_PREFIX),
                HandPenMode.GetAnimationLayerId(LAYER_NAME_PREFIX),
                HandPenMode.ActiveState.GetAnimationLayerId(LAYER_NAME_PREFIX),
            };

            var layers = Character.OverlappingAnimations.ToList().ToArray();
            foreach (OverlappingAnimationData layer in Character.OverlappingAnimations) {
                if (!matchingLayerNames.Contains(layer.CustomLayerID)) {
                    continue;
                }

                layer.MaskedBodyParts = IsRightHanded
                    ? new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers }
                    : new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
            }

            Character.SetDataInput($"{nameof(Character.OverlappingAnimations)}", layers, true);
            if (activeHandPoleAnchor != null && ActiveHand?.ElbowPole != null) {
                AttachAnchorToParent(activeHandPoleAnchor, ActiveHand.ElbowPole.ParentType, IsRightHanded);
            }
            SetupActiveHandPoleTransform();
            SetupRestHandTransform();
            SetupRestHandPoleTransform();
            UpdateRestHandAnchorTransforms();

            // Re-target and transition rest hand to the new limb if active
            bool currentRestActive = IsRestHandActive || IsInRestHandVisualSetup;
            lastRestHandActive = currentRestActive;
            TransitionRestHand(currentRestActive);

            bool currentRestPoleActive = IsRestHandPoleActive;
            lastRestPoleActive = currentRestPoleActive;
            TransitionRestPole(currentRestPoleActive);

            if (inSetupMode) {
                CalculateSetupHandOffsets();
                CalculateSetupBodyOffsets();
            } else {
                OnHandStateChange();
                OnBodyStateChange();
            }

            if (cursorAnchorAsset != null) {
                cursorAnchorAsset.Transform.Rotation = IsRightHanded ? IK_BASIS_RIGHT_HANDED : IK_BASIS_LEFT_HANDED;
                cursorAnchorAsset.Transform.Broadcast();
                if (cursorAnchorAsset.GameObject != null) {
                    cursorAnchorAsset.GameObject.transform.localRotation = Quaternion.Euler(cursorAnchorAsset.Transform.Rotation);
                }
            }
            if (handAnchor != null) {
                handAnchor.Transform.Position = handPositionOffset;
                handAnchor.Transform.Rotation = handRotationOffset;
                handAnchor.Transform.Broadcast();
                if (handAnchor.GameObject != null) {
                    handAnchor.GameObject.transform.localPosition = handPositionOffset;
                    handAnchor.GameObject.transform.localRotation = Quaternion.Euler(handRotationOffset);
                }
            }
        }

        PoseState GetHandState(int source, bool button1) {

            if (!IsHandEnabled) return null;

            var st = HandPenMode.Enabled && source == 2
                    ? (BasePoseState)HandPenMode
                    : HandMouseMode;

            if (!st.Enabled) return null;

            var o = st.ActiveState.Enabled && button1
                ? (PoseState)st.ActiveState
                : st;

            return o;
        }

        AnchorAsset GetHandIK(bool skipAutoCreate = false) {
            return GetAsset<AnchorAsset>(ref HandAnchorAssetId, skipAutoCreate, newAssetName: $"🔒⚓-{LAYER_NAME_PREFIX} Hand IK", category: "FS_ASSET_CATEGORY_INTERNAL".Localized());
        }

        public void DestroyHandIK() {
            tweenHandPosition?.Kill();
            tweenHandRotation?.Kill();
            tweenHandProgress?.Kill();
            var anchor = handAnchor ?? (HandAnchorAssetId != Guid.Empty ? Scene?.GetAssets<AnchorAsset>().FirstOrDefault(a => a.Id == HandAnchorAssetId) : null);
            if (anchor != null) {
                CleanDestroy(anchor);
            }
            HandAnchorAssetId = Guid.Empty;
            handAnchor = null;
            tweenActivePole?.Kill();
            DestroyActiveHandPoleAnchor();
        }

        void EnableLimb(bool isEnabled, LimbIKData limb, AnchorAsset anchor, ref Tween tween, bool instant = false) {
            if (activeCoordinator != null) {
                return;
            }
            if (limb == null) {
                if (!isEnabled) {
                    DestroyHandIK();
                    DestroyCursorAnchor();
                }
                return;
            }

            tween?.Kill();
            var duration = instant ? 0f : (isEnabled
                ? (ActiveTransition != null ? ActiveTransition.Time : 0.5f)
                : (InactiveTransition != null ? InactiveTransition.Time : 0.5f));
            var ease = isEnabled
                ? (ActiveTransition != null ? ActiveTransition.Ease : Ease.Linear)
                : (InactiveTransition != null ? InactiveTransition.Ease : Ease.Linear);
            var delay = (instant || !isEnabled || ActiveTransition == null) ? 0f : ActiveTransition.Delay;

            if (isEnabled) {
                // When transitioning to active, ensure the weights are at 0 before connecting the anchors to then transition them to 1
                if (limb.IkTarget != anchor || !limb.Enabled) {
                    limb.PositionWeight = 0f;
                    limb.RotationWeight = 0f;
                    limb.Enabled = true;
                    limb.IkTarget = anchor;
                    limb.Broadcast();
                }

                TransitionActivePole(true, instant);

                float currentWeight = Mathf.Clamp01(limb.PositionWeight);
                float remaining = 1.0f - currentWeight;
                float actualDuration = duration * remaining;

                if (actualDuration <= 0f) {
                    limb.PositionWeight = 1.0f;
                    limb.RotationWeight = 1.0f;
                    limb.Broadcast();
                } else {
                    tween = DOTween.To(
                        () => limb.PositionWeight,
                        delegate(float it) {
                            limb.PositionWeight = it;
                            limb.RotationWeight = it;
                            limb.Broadcast();
                        },
                        1.0f,
                        actualDuration
                    ).SetEase(ease);

                    if (delay > 0f) {
                        tween.SetDelay(delay);
                    }
                }
            } else {
                TransitionActivePole(false, instant);

                // When turning off IK, if for some reason the weight was not at 1.0, transition from there to 0 instead of putting to 1 and when going to 0
                float currentWeight = Mathf.Clamp01(limb.PositionWeight);
                float actualDuration = duration * currentWeight;

                if (currentWeight <= 0.001f || actualDuration <= 0f) {
                    limb.PositionWeight = 0f;
                    limb.RotationWeight = 0f;
                    if (limb.IkTarget == handAnchor || limb.IkTarget == anchor || limb.IkTarget == null) {
                        limb.IkTarget = null;
                        limb.Enabled = false;
                    }
                    limb.Broadcast();
                    DestroyHandIK();
                    DestroyCursorAnchor();
                } else {
                    tween = DOTween.To(
                        () => limb.PositionWeight,
                        delegate(float it) {
                            limb.PositionWeight = it;
                            limb.RotationWeight = it;
                            limb.Broadcast();
                        },
                        0.0f,
                        actualDuration
                    ).SetEase(ease).OnComplete(() => {
                        if (limb.IkTarget == handAnchor || limb.IkTarget == anchor || limb.IkTarget == null) {
                            limb.IkTarget = null;
                            limb.Enabled = false;
                            limb.Broadcast();
                        }
                        DestroyHandIK();
                        DestroyCursorAnchor();
                    });
                }
            }
        }

        public void TransitionActivePole(bool enable, bool instant = false) {
            tweenActivePole?.Kill();
            bool shouldTarget = enable && ActiveHand != null && ActiveHand.ElbowPole != null && ActiveHand.ElbowPole.UsePole;
            var dominantLimb = Character != null ? (IsRightHanded ? Character.RightHandIK : Character.LeftHandIK) : null;

            float duration = instant ? 0f : (shouldTarget
                ? (ActiveTransition != null ? ActiveTransition.Time : 0.5f)
                : (InactiveTransition != null ? InactiveTransition.Time : 0.5f));
            var ease = shouldTarget
                ? (ActiveTransition != null ? ActiveTransition.Ease : Ease.OutQuad)
                : (InactiveTransition != null ? InactiveTransition.Ease : Ease.Linear);
            float delay = (instant || !shouldTarget || ActiveTransition == null) ? 0f : ActiveTransition.Delay;

            if (shouldTarget) {
                var poleAnchor = GetActiveHandPoleAnchor();
                if (dominantLimb != null && activeCoordinator == null) {
                    if (dominantLimb.BendGoalTarget != poleAnchor) {
                        dominantLimb.BendGoalWeight = 0f;
                        currentActivePoleWeight = 0f;
                    }
                    dominantLimb.BendGoalTarget = poleAnchor;
                    dominantLimb.Broadcast();
                }

                float currentWeight = dominantLimb != null ? Mathf.Clamp01(dominantLimb.BendGoalWeight) : Mathf.Clamp01(currentActivePoleWeight);
                float remaining = 1.0f - currentWeight;
                float actualDuration = duration * remaining;

                if (actualDuration <= 0f) {
                    currentActivePoleWeight = 1.0f;
                    if (dominantLimb != null && activeCoordinator == null) {
                        dominantLimb.BendGoalWeight = 1.0f;
                        dominantLimb.Broadcast();
                    }
                    return;
                }

                currentActivePoleWeight = currentWeight;
                tweenActivePole = DOTween.To(
                    () => currentActivePoleWeight,
                    (w) => {
                        currentActivePoleWeight = w;
                        if (dominantLimb != null && activeCoordinator == null) {
                            dominantLimb.BendGoalWeight = w;
                            dominantLimb.Broadcast();
                        }
                    },
                    1.0f,
                    actualDuration
                ).SetEase(ease);

                if (delay > 0f) {
                    tweenActivePole.SetDelay(delay);
                }
            } else {
                float currentWeight = dominantLimb != null ? Mathf.Clamp01(dominantLimb.BendGoalWeight) : Mathf.Clamp01(currentActivePoleWeight);
                float actualDuration = duration * currentWeight;

                if (currentWeight <= 0.001f || actualDuration <= 0f) {
                    currentActivePoleWeight = 0f;
                    if (dominantLimb != null && activeCoordinator == null) {
                        dominantLimb.BendGoalWeight = 0f;
                        if (dominantLimb.BendGoalTarget == activeHandPoleAnchor) {
                            dominantLimb.BendGoalTarget = null;
                        }
                        dominantLimb.Broadcast();
                    }
                    DestroyActiveHandPoleAnchor();
                    return;
                }

                currentActivePoleWeight = currentWeight;
                tweenActivePole = DOTween.To(
                    () => currentActivePoleWeight,
                    (w) => {
                        currentActivePoleWeight = w;
                        if (dominantLimb != null && activeCoordinator == null) {
                            dominantLimb.BendGoalWeight = w;
                            dominantLimb.Broadcast();
                        }
                    },
                    0.0f,
                    actualDuration
                ).SetEase(ease).OnComplete(() => {
                    if (dominantLimb != null && dominantLimb.BendGoalTarget == activeHandPoleAnchor && activeCoordinator == null) {
                        dominantLimb.BendGoalTarget = null;
                        dominantLimb.Broadcast();
                    }
                    DestroyActiveHandPoleAnchor();
                });
            }
        }

        public void OnActiveHandPoleParentTypeChange() {
            var newParentType = ActiveHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
            var poleAnchor = GetActiveHandPoleAnchor(true);

            var oldParentType = poleAnchor != null
                ? GetAnchorParentType(poleAnchor)
                : currentActiveHandPoleParentType;

            if (newParentType == oldParentType) {
                currentActiveHandPoleParentType = newParentType;
                return;
            }

            var pole = ActiveHand?.ElbowPole?.Pole;
            if (pole != null) {
                Vector3 currentPolePos = poleAnchor != null ? poleAnchor.Transform.Position : pole.Position;
                Vector3 currentPoleRot = poleAnchor != null ? poleAnchor.Transform.Rotation : pole.Rotation;

                TransformAnchorLocalToWorld(oldParentType, currentPolePos, currentPoleRot, out Vector3 poleWorldPos, out Quaternion poleWorldRot, IsRightHanded);
                TransformAnchorWorldToLocal(newParentType, poleWorldPos, poleWorldRot, out Vector3 newPolePos, out Vector3 newPoleRot, IsRightHanded);

                pole.Position = newPolePos;
                pole.Rotation = newPoleRot;
                pole.Scale = Vector3.one;
                pole.SetDataInput(nameof(pole.Position), newPolePos, true);
                pole.SetDataInput(nameof(pole.Rotation), newPoleRot, true);
                pole.Broadcast();

                if (poleAnchor != null) {
                    AttachAnchorToParent(poleAnchor, newParentType, IsRightHanded);
                    poleAnchor.Transform.Position = newPolePos;
                    poleAnchor.Transform.Rotation = newPoleRot;
                    poleAnchor.Transform.Scale = Vector3.one;
                    poleAnchor.Transform.Broadcast();
                    if (poleAnchor.GameObject != null) {
                        poleAnchor.GameObject.transform.localPosition = newPolePos;
                        poleAnchor.GameObject.transform.localRotation = Quaternion.Euler(newPoleRot);
                        poleAnchor.GameObject.transform.localScale = Vector3.one;
                    }
                }

                if (pole.setupAnchor != null) {
                    AttachAnchorToParent(pole.setupAnchor, newParentType, IsRightHanded);
                    pole.setupAnchor.Transform.Position = newPolePos;
                    pole.setupAnchor.Transform.Rotation = newPoleRot;
                    pole.setupAnchor.Transform.Scale = Vector3.one;
                    pole.setupAnchor.Transform.Broadcast();
                    if (pole.setupAnchor.GameObject != null) {
                        pole.setupAnchor.GameObject.transform.localPosition = newPolePos;
                        pole.setupAnchor.GameObject.transform.localRotation = Quaternion.Euler(newPoleRot);
                        pole.setupAnchor.GameObject.transform.localScale = Vector3.one;
                    }
                }
            }

            currentActiveHandPoleParentType = newParentType;
            RequestUiRefresh();
        }

        public void SetupActiveHandPoleTransform() {
            var pole = ActiveHand?.ElbowPole?.Pole;
            if (pole == null) return;
            pole.LocalizationKey = "INITIAL_POLE_TRANSFORM";
            pole.HideScale();
            pole.CollapsedSelf = true;
            pole.ShowOriginIndicator = false;

            pole.OnEnterVisualSetup = (tr) => {
                EnsureActiveHandAnchors();
                if (NeutralHandPosition != null) {
                    NeutralHandPosition.ShowOriginIndicator = false;
                    NeutralHandPosition.EnterVisualSetup(false);
                }
                var livePole = GetActiveHandPoleAnchor();
                if (livePole != null) {
                    livePole.Transform.Position = tr.Position;
                    livePole.Transform.Rotation = tr.Rotation;
                    livePole.Transform.Broadcast();
                }
                var dominantLimb = Character != null ? (IsRightHanded ? Character.RightHandIK : Character.LeftHandIK) : null;
                if (dominantLimb != null && activeCoordinator == null) {
                    dominantLimb.BendGoalTarget = livePole;
                    currentActivePoleWeight = 1.0f;
                    dominantLimb.BendGoalWeight = 1.0f;
                    dominantLimb.Broadcast();
                }
            };

            pole.OnCreateAnchor = (tr, setupAnchor) => {
                var parentType = ActiveHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
                SetAnchorParentInitial(setupAnchor, parentType, IsRightHanded);

                setupAnchor.Transform.Position = tr.Position;
                setupAnchor.Transform.Rotation = tr.Rotation;
                setupAnchor.Transform.Scale = Vector3.one;
                var handSide = IsRightHanded ? "Right" : "Left";
                setupAnchor.Name = $"⌛⚓-{handSide} Hand Active Pole";
                setupAnchor.Instructions = "ACTIVE_HAND_POLE_INSTRUCTIONS".Localized();
                setupAnchor.Transform.Broadcast();

                var livePole = GetActiveHandPoleAnchor();
                if (livePole != null) {
                    livePole.Transform.Position = tr.Position;
                    livePole.Transform.Rotation = tr.Rotation;
                    livePole.Transform.Broadcast();
                }
            };

            pole.OnResetAnchor = (tr, setupAnchor) => {
                var parentType = ActiveHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
                Vector3 defaultWorldPos = GetDefaultElbowPoleWorldPosition(IsRightHanded);
                TransformAnchorWorldToLocal(parentType, defaultWorldPos, Quaternion.identity, out Vector3 localPos, out Vector3 localRot, IsRightHanded);

                setupAnchor.Transform.Position = localPos;
                setupAnchor.Transform.Rotation = localRot;
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Transform.Broadcast();
            };

            pole.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                tr.Scale = Vector3.one;
                var livePole = GetActiveHandPoleAnchor(true);
                if (livePole != null) {
                    livePole.Transform.Position = tr.Position;
                    livePole.Transform.Rotation = tr.Rotation;
                    livePole.Transform.Broadcast();
                }
            };

            pole.OnExitVisualSetup = (isApplied) => {
                if (!isApplied) {
                    var livePole = GetActiveHandPoleAnchor(true);
                    var currentPole = ActiveHand?.ElbowPole?.Pole;
                    if (livePole != null && currentPole != null) {
                        livePole.Transform.Position = currentPole.Position;
                        livePole.Transform.Rotation = currentPole.Rotation;
                        livePole.Transform.Broadcast();
                    }
                }
                OnInputAffectingHandSetupChange();

                if (!isExitingVisualSetup) {
                    isExitingVisualSetup = true;
                    try {
                        TriggerVisualSetupAnchor(NeutralHandPosition, isApplied);
                    } finally {
                        isExitingVisualSetup = false;
                    }
                }

                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
                RequestUiRefresh();
            };
        }

        void EnsureActiveHandAnchors() {
            var poleParentType = ActiveHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
            if (ActiveHand != null && (ActiveHand.ElbowPole != null && (ActiveHand.ElbowPole.UsePole || (ActiveHand.ElbowPole.Pole != null && ActiveHand.ElbowPole.Pole.InVisualSetupMode)))) {
                var poleAnchor = GetActiveHandPoleAnchor();
                if (poleAnchor != null) AttachAnchorToParent(poleAnchor, poleParentType, IsRightHanded);
            }
        }

        void SetHandOffsetTransforms(Vector3 pos, Vector3 rot, float time = 0, Ease ease = Ease.Linear) {
            // Tween doesn't seen to have a progress getter, so we're creating our own
            time *= handTweenProgress;
            handTweenProgress = 1 - handTweenProgress;
            tweenHandProgress?.Kill();
            if (time == 0) {
                handTweenProgress = 1f;
            } else {
                tweenHandProgress = DOTween.To(
                    () => handTweenProgress,
                    delegate(float it) { handTweenProgress = it; },
                    1f,
                    time
                ).SetEase(Ease.Linear);
            }

            tweenHandPosition?.Kill();
            if (time == 0) {
                handPositionOffset = pos;
            } else {
                tweenHandPosition = DOTween.To(
                    () => handPositionOffset,
                    delegate(Vector3 it) { handPositionOffset = it; },
                    pos,
                    time
                ).SetEase(ease);
            }

            tweenHandRotation?.Kill();
            if (time == 0) {
                handRotationOffset = rot;
            } else {
                tweenHandRotation = DOTween.To(
                    () => handRotationOffset,
                    delegate(Vector3 it) { handRotationOffset = it; },
                    rot,
                    time
                ).SetEase(ease);
            }
        }


        void HandleNeutralEnterVisualSetup() {
            if (inSetupMode || IsInActiveHandPoleVisualSetup || IsInRestHandPoleVisualSetup) return;
            NeutralHandPosition.ShowOriginIndicator = true;
            EnterSetupModeMinimal();
            GetHandState(0, false)?.Transform.EnterVisualSetup(false);
        }

        void HandleNeutralExitVisualSetup(bool isApply) {
            if (isExitingVisualSetup) return;
            isExitingVisualSetup = true;
            try {
                if (HandDynamicRotation != null && HandDynamicRotation.InVisualSetupMode) {
                    HandDynamicRotation.ExitVisualSetup(isApply);
                }
                if (IsInActiveHandPoleVisualSetup) {
                    TriggerVisualSetupAnchor(ActiveHand?.ElbowPole?.Pole, isApply);
                }
                if (IsInRestHandPoleVisualSetup) {
                    var restPole = RestHand?.ElbowPole?.Pole ?? RestHand?.Pole;
                    TriggerVisualSetupAnchor(restPole, isApply);
                }
                if (setupModeHandState != null || setupModeBodyState != null || setupModePropState != null || inSetupMode) {
                    var handTr = setupModeHandState?.Transform;
                    var bodyTr = setupModeBodyState?.Transform;
                    var propTr = setupModePropState?.Transform;
                    TriggerVisualSetupAnchor(handTr, isApply);
                    TriggerVisualSetupAnchor(bodyTr, isApply);
                    TriggerVisualSetupAnchor(propTr, isApply);
                    ExitSetupModeMinimal();
                }
            } finally {
                isExitingVisualSetup = false;
                if (NeutralHandPosition != null) NeutralHandPosition.ShowOriginIndicator = true;
            }
            Context.Service.NavigateToAsset(Id);
            Context.Service.BroadcastOpenedScene();
            RequestUiRefresh();
        }

        void HandleDynamicRotationEnterVisualSetup() {
            if (inSetupMode) return;
            EnterSetupModeMinimal();
            if (NeutralHandPosition != null) {
                NeutralHandPosition.ShowOriginIndicator = false;
                NeutralHandPosition.EnterVisualSetup(false);
            }
        }

        void HandleDynamicRotationExitVisualSetup(bool isApply) {
            if (!inSetupMode) return;
            TriggerVisualSetupAnchor(NeutralHandPosition, isApply);
            ExitSetupModeMinimal();
        }

        void HandleHandEnterVisualSetup(PoseState p) {
            if (setupModeHandState != null) return;
            EnterSetupModeMinimal();
            setupModeHandState = p;
            tweenHandPosition?.Kill();
            tweenHandRotation?.Kill();

            var source = 0;
            var button1 = false;
            if (setupModeHandState == HandMouseMode.ActiveState) {
                button1 = true;
            } else if (setupModeHandState == HandPenMode) {
                source = 2;
            } else if (setupModeHandState == HandPenMode.ActiveState) {
                source = 2;
                button1 = true;
            }
            TriggerOtherEnterSetupModes(source, button1);
        }

        void HandleHandExitVisualSetup(PoseState p, bool isApply) {
            if (setupModeHandState == null) return;

            CleanDestroy(handSetupRootAnchor);
            DestroyCursorIndicator();
            ExitSetupModeMinimal();
            setupModeHandState = null;
            tweenHandPosition?.Kill();
            tweenHandRotation?.Kill();

            if (!isExitingVisualSetup) {
                isExitingVisualSetup = true;
                try {
                    TriggerVisualSetupAnchor(NeutralHandPosition, isApply);
                    TriggerVisualSetupAnchor(setupModeBodyState?.Transform, isApply);
                    TriggerVisualSetupAnchor(setupModePropState?.Transform, isApply);
                } finally {
                    isExitingVisualSetup = false;
                }
            }
            lastHandState = null;
            Context.Service.NavigateToAsset(Id);
            Context.Service.BroadcastOpenedScene();
            RequestUiRefresh();
        }

        void HandleHandCreateAnchor(PoseState p, VisualSetupTransform tr, VisualSetupAnchorAsset a) {
            CleanDestroy(handSetupRootAnchor);
            handSetupRootAnchor = Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INTERNAL".Localized());
            if (cursorAnchorAsset == null) cursorAnchorAsset = GetCursorAnchor();
            handSetupRootAnchor.Attachable.Parent = cursorAnchorAsset;
            var title = "SETUP_ANCHOR_NAME_HAND_ROOT".Localized();
            handSetupRootAnchor.Name = $"🔒⌛⚓-{title}";
            Scene.UpdateNewAssetName(handSetupRootAnchor);

            a.Attachable.Parent = handSetupRootAnchor;
            a.Animation = p.Animation;

            CreateCursorIndicator(a);

            a.OnShowOriginChange = (show) => {
                tr.SetOriginIndicatorVisible(show);
                if (cursorIndicatorGo != null) cursorIndicatorGo.SetActive(show);
            };
            a.OnAlwaysOnTopChange = (onTop) => {
                tr.SetOriginAlwaysOnTop(onTop);
                UpdateCursorIndicatorVisuals(a);
            };
            a.OnOriginSphereChange = () => {
                tr.UpdateOriginIndicatorScale();
                tr.UpdateOriginIndicatorMaterial();
                UpdateCursorIndicatorVisuals(a);
            };
        }

        void CreateCursorIndicator(VisualSetupAnchorAsset setupAnchor) {
            DestroyCursorIndicator();
            if (cursorAnchorAsset == null) cursorAnchorAsset = GetCursorAnchor();
            if (cursorAnchorAsset == null || cursorAnchorAsset.GameObject == null) return;

            cursorIndicatorGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cursorIndicatorGo.name = "CursorIndicator_Yellow";
            var col = cursorIndicatorGo.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);

            cursorIndicatorGo.transform.SetParent(cursorAnchorAsset.GameObject.transform, false);
            cursorIndicatorGo.transform.localPosition = Vector3.zero;
            cursorIndicatorGo.transform.localRotation = Quaternion.identity;

            UpdateCursorIndicatorVisuals(setupAnchor);
            cursorIndicatorGo.SetActive(setupAnchor != null ? setupAnchor.ShowOrigin : true);
        }

        void UpdateCursorIndicatorVisuals(VisualSetupAnchorAsset setupAnchor) {
            if (cursorIndicatorGo == null) return;

            float radiusFactor = setupAnchor?.OriginSphere?.RadiusFactor ?? 1.0f;
            float targetSize = 0.035f * radiusFactor;
            Transform parent = cursorIndicatorGo.transform.parent;
            if (parent != null && parent.lossyScale != Vector3.one) {
                Vector3 lossy = parent.lossyScale;
                cursorIndicatorGo.transform.localScale = new Vector3(
                    lossy.x != 0 ? targetSize / lossy.x : targetSize,
                    lossy.y != 0 ? targetSize / lossy.y : targetSize,
                    lossy.z != 0 ? targetSize / lossy.z : targetSize
                );
            } else {
                cursorIndicatorGo.transform.localScale = Vector3.one * targetSize;
            }

            if (cursorIndicatorMat != null) {
                UnityEngine.Object.Destroy(cursorIndicatorMat);
                cursorIndicatorMat = null;
            }

            Color yellowColor = new Color(1f, 0.9f, 0f, 0.95f);
            bool glow = setupAnchor?.OriginSphere?.Glow ?? true;
            bool alwaysOnTop = setupAnchor?.AlwaysOnTop ?? false;

            if (alwaysOnTop) {
                var overlayShader = Shader.Find("GUI/Text Shader") ?? Shader.Find("Hidden/Internal-Colored");
                if (overlayShader != null) {
                    cursorIndicatorMat = new Material(overlayShader);
                    cursorIndicatorMat.color = yellowColor;
                } else {
                    cursorIndicatorMat = new Material(Shader.Find("Standard"));
                    cursorIndicatorMat.color = yellowColor;
                    if (glow) {
                        cursorIndicatorMat.EnableKeyword("_EMISSION");
                        cursorIndicatorMat.SetColor("_EmissionColor", new Color(yellowColor.r * 0.85f, yellowColor.g * 0.85f, yellowColor.b * 0.85f));
                    }
                    cursorIndicatorMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                }
                cursorIndicatorMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay + 1000;
            } else {
                cursorIndicatorMat = new Material(Shader.Find("Standard"));
                cursorIndicatorMat.color = yellowColor;
                if (glow) {
                    cursorIndicatorMat.EnableKeyword("_EMISSION");
                    cursorIndicatorMat.SetColor("_EmissionColor", new Color(yellowColor.r * 0.85f, yellowColor.g * 0.85f, yellowColor.b * 0.85f));
                }
            }

            var rend = cursorIndicatorGo.GetComponent<Renderer>();
            if (rend != null) rend.material = cursorIndicatorMat;
        }

        void DestroyCursorIndicator() {
            if (cursorIndicatorGo != null) {
                UnityEngine.Object.Destroy(cursorIndicatorGo);
                cursorIndicatorGo = null;
            }
            if (cursorIndicatorMat != null) {
                UnityEngine.Object.Destroy(cursorIndicatorMat);
                cursorIndicatorMat = null;
            }
        }

        void OnDestroyHand() {
            DestroyCursorIndicator();
        }

        void HandleHandAnimationChange(PoseState p) {
            var userLayers = Character.OverlappingAnimations.Where(d => !d.CustomLayerID.StartsWith(LAYER_NAME_PREFIX)).ToList() ?? new List<OverlappingAnimationData>();
            var pointerLayers = Character.OverlappingAnimations.Where(d => d.CustomLayerID.StartsWith(LAYER_NAME_PREFIX)).ToList() ?? new List<OverlappingAnimationData>();

            var layerId = p.GetAnimationLayerId(LAYER_NAME_PREFIX);
            var layer = pointerLayers.FirstOrDefault(d => d.CustomLayerID == layerId);
            if (layer == null) {
                layer = StructuredData.Create<OverlappingAnimationData>();
                layer.Weight = setupModeHandState == p ? 1f : 0f;
                layer.Speed = 1f;
                layer.Masked = true;
                layer.Additive = false;
                layer.Looping = false;
                layer.CustomLayerID = layerId;

                pointerLayers.Add(layer);
            }
            // Update values
            layer.MaskedBodyParts = IsRightHanded
                ? new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers }
                : new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
            layer.Animation = p.Animation;

            // Remove invalid animations
            pointerLayers = pointerLayers.Where(d => d.Animation != null).ToList();

            // Reorder layers
            pointerLayers.Sort((x, y) => x.CustomLayerID.CompareTo(y.CustomLayerID));
            userLayers.AddRange(pointerLayers);

            Character.SetDataInput($"{nameof(Character.OverlappingAnimations)}", userLayers.ToArray(), true);
        }

        void HandleHandAnimationWeightChange(PoseState p, float weight, bool resetAnimation = false) {
            if (Character == null) return;

            var layerId = p.GetAnimationLayerId(LAYER_NAME_PREFIX);
            if (layerId == null) return;

            var animationData = Array.Find(
                Character.OverlappingAnimations,
                (a) => a.CustomLayerID == layerId
            );
            if (animationData == null) return;

            // Not broatcasting here because it causes all animation layers to be reloaded.
            animationData.SetDataInput(nameof(animationData.Weight), weight, false);

            // Sanity: If no animation is set, then animancer layer is null
            if (animationData.Animation == null) return;
            int animancerIdx = 1 + Array.IndexOf(Character.OverlappingAnimations, animationData);

            var layer = Character.Animancer.Layers[animancerIdx];
            var layer2 = Character.CloneAnimancer.Layers[animancerIdx];
            layer.SetWeight(weight);
            layer2.SetWeight(weight);


            if (resetAnimation) {
                // NOTE: Crash would occur if layer has no animation
                layer.Play(layer.CurrentState).Time = 0;
                layer2.Play(layer2.CurrentState).Time = 0;
            }
        }
    }
}
