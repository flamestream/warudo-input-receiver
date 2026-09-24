using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
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
        Tween tweenLeftIK;
        Tween tweenRightIK;

        public void OnHandDelegationChanged() {
            if (activeCoordinator != null) {
                tweenLeftIK?.Kill();
                tweenRightIK?.Kill();
            } else {
                OnInputAffectingHandSetupChange();
            }
        }


        void OnCreateMoveHand() {
            Watch(nameof(IsEnabled), delegate { OnInputAffectingHandSetupChange(); });
            Watch(nameof(IsHandEnabled), delegate { OnInputAffectingHandSetupChange(); });
            Watch(nameof(IsRightHanded), delegate { OnInputAffectingHandSetupChange(); });
            Watch(nameof(Character), delegate { OnInputAffectingHandSetupChange(); });

            Watch(nameof(IsRightHanded), delegate { OnHandSideChange(); });

            Watch(nameof(HandMouseMode), delegate { OnHandStateSettingChange(); });
            Watch(nameof(HandPenMode), delegate { OnHandStateSettingChange(); });
            Watch(nameof(HandDynamicRotation), delegate {
                if (HandDynamicRotation != null) {
                    HandDynamicRotation.AutoRotationEvaluator = CalculateAutoHandRotation;
                }
            });
            if (HandDynamicRotation != null) {
                HandDynamicRotation.AutoRotationEvaluator = CalculateAutoHandRotation;
            }
        }

        void OnReadyHand() {
            if (HandDynamicRotation != null) {
                HandDynamicRotation.AutoRotationEvaluator = CalculateAutoHandRotation;
            }

            NeutralHandPosition.LocalizationKey = "NEUTRAL_POSITION";
            NeutralHandPosition.OnEnterVisualSetup = (tr) => { HandleNeutralEnterVisualSetup(); };
            NeutralHandPosition.OnExitVisualSetup = (a) => { HandleNeutralExitVisualSetup(a); };
            NeutralHandPosition.OnCreateAnchor = (tr, a) => { a.Attachable.Parent = screenAsset ?? GetSurfaceScreen(); };

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
                        dominantLimb.BendGoalWeight = 1.0f;
                        dominantLimb.Broadcast();
                    }
                }
            }

            if (IsInRestHandVisualSetup) {
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

        void OnInputAffectingHandSetupChange(bool instant = false) {
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

        void OnHandSideChange() {
            if (!isReady) return;
            if (Character == null) return;
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
            SetupRestHandTransform();
            SetupRestHandPoleTransform();
            UpdateRestHandAnchorTransforms();
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
            return GetAsset<AnchorAsset>(ref HandAnchorAssetId, skipAutoCreate, newAssetName: $"🔒⚓-{LAYER_NAME_PREFIX} Hand IK");
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

            var targetValue = isEnabled ? 1.0f : 0.0f;
            tween?.Kill();
            var duration = instant ? 0f : (ActiveTransition != null ? ActiveTransition.Time : 0f);
            var ease = ActiveTransition != null ? ActiveTransition.Ease : Ease.Linear;

            if (isEnabled) {
                limb.Enabled = true;
                limb.IkTarget = anchor;
                limb.Broadcast();
            }

            if (duration <= 0f) {
                limb.PositionWeight = targetValue;
                limb.RotationWeight = targetValue;
                limb.BendGoalWeight = targetValue;
                if (!isEnabled) {
                    if (limb.IkTarget == handAnchor || limb.IkTarget == anchor || limb.IkTarget == null) {
                        limb.IkTarget = null;
                        limb.Enabled = false;
                    }
                    DestroyHandIK();
                    DestroyCursorAnchor();
                }
                limb.Broadcast();
            } else {
                tween = DOTween.To(
                    () => limb.PositionWeight,
                    delegate(float it) {
                        limb.PositionWeight = it;
                        limb.RotationWeight = it;
                        limb.BendGoalWeight = it;
                        limb.Broadcast();
                    },
                    targetValue,
                    duration
                ).SetEase(ease).OnComplete(() => {
                    if (!isEnabled) {
                        if (limb.IkTarget == handAnchor || limb.IkTarget == anchor || limb.IkTarget == null) {
                            limb.IkTarget = null;
                            limb.Enabled = false;
                            limb.Broadcast();
                        }
                        DestroyHandIK();
                        DestroyCursorAnchor();
                    }
                });
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
            if (inSetupMode) return;
            EnterSetupModeMinimal();
            GetHandState(0, false)?.Transform.EnterVisualSetup(false);
        }

        void HandleNeutralExitVisualSetup(bool isApply) {
            if (!inSetupMode) return;
            ExitSetupModeMinimal();
            setupModeHandState?.Transform.ExitVisualSetup(isApply);
            setupModeBodyState?.Transform.ExitVisualSetup(isApply);
            setupModePropState?.Transform.ExitVisualSetup(isApply);
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
            ExitSetupModeMinimal();
            setupModeHandState = null;
            tweenHandPosition?.Kill();
            tweenHandRotation?.Kill();

            if (isApply) {
                NeutralHandPosition.setupAnchor?.ApplyTrigger();
                setupModeBodyState?.Transform.setupAnchor?.ApplyTrigger();
                setupModePropState?.Transform.setupAnchor?.ApplyTrigger();
            } else {
                NeutralHandPosition.setupAnchor?.CancelTrigger();
                setupModeBodyState?.Transform.setupAnchor?.CancelTrigger();
                setupModePropState?.Transform.setupAnchor?.CancelTrigger();
            }
            lastHandState = null;
        }

        void HandleHandCreateAnchor(PoseState p, VisualSetupTransform tr, VisualSetupAnchorAsset a) {
            CleanDestroy(handSetupRootAnchor);
            handSetupRootAnchor = Scene.AddAsset<AnchorAsset>();
            handSetupRootAnchor.Attachable.Parent = cursorAnchorAsset;
            var title = "SETUP_ANCHOR_NAME_HAND_ROOT".Localized();
            handSetupRootAnchor.Name = $"🔒⌛⚓-{title}";
            Scene.UpdateNewAssetName(handSetupRootAnchor);

            a.Attachable.Parent = handSetupRootAnchor;
            a.Animation = p.Animation;
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
