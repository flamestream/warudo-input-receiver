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
        Vector3 handPosition;
        Vector3 handPositionOffset;
        Vector3 handRotation;
        Vector3 handRotationOffset;
        AnchorAsset handAnchor;
        AnchorAsset handSetupRootAnchor;

        void OnCreateMoveHand() {
            Watch(nameof(IsHandEnabled), delegate { OnInputAffectingHandIKChange(); });
            Watch(nameof(IsRightHanded), delegate { OnInputAffectingHandIKChange(); });
            Watch(nameof(Character), delegate { OnInputAffectingHandIKChange(); });

            Watch(nameof(HandMouseMode), delegate { OnHandStateSettingChange(); });
            Watch(nameof(HandPenMode), delegate { OnHandStateSettingChange(); });
        }

        void OnReadyHand() {

            NeutralHandPosition.LocalizationKey = "NEUTRAL_POSITION";
            NeutralHandPosition.OnEnterVisualSetup = (tr) => { HandleNeutralEnterVisualSetup(); };
            NeutralHandPosition.OnExitVisualSetup = (a) => { HandleNeutralExitVisualSetup(a); };
            NeutralHandPosition.OnCreateAnchor = (tr, a) => { a.Attachable.Parent = screenAsset; };

            HandMouseMode.OnAnimationChange = (p) => { HandleHandAnimationChange((PoseState)p); };
            HandMouseMode.OnAnimationWeightChange = (p, w, r) => { HandleHandAnimationWeightChange((PoseState)p, w, r); };
            HandMouseMode.OnApplyAnchor = (p, tr, a) => { HandleHandApplyAnchor((PoseState)p, tr, a); };
            HandMouseMode.OnCreateAnchor = (p, tr, a) => { HandleHandCreateAnchor((PoseState)p, tr, a); };
            HandMouseMode.OnEnterVisualSetup = (p) => { HandleHandEnterVisualSetup((PoseState)p); };
            HandMouseMode.OnExitVisualSetup = (p, ia) => { HandleHandExitVisualSetup((PoseState)p, ia); };

            HandPenMode.OnAnimationChange = (p) => { HandleHandAnimationChange((PoseState)p); };
            HandPenMode.OnAnimationWeightChange = (p, w, r) => { HandleHandAnimationWeightChange((PoseState)p, w, r); };
            HandPenMode.OnApplyAnchor = (p, tr, a) => { HandleHandApplyAnchor((PoseState)p, tr, a); };
            HandPenMode.OnCreateAnchor = (p, tr, a) => { HandleHandCreateAnchor((PoseState)p, tr, a); };
            HandPenMode.OnEnterVisualSetup = (p) => { HandleHandEnterVisualSetup((PoseState)p); };
            HandPenMode.OnExitVisualSetup = (p, ia) => { HandleHandExitVisualSetup((PoseState)p, ia); };

            OnInputAffectingHandIKChange();
        }

        void OnUpdateMoveHand() {
            if (!IsHandEnabled) return;
            if (handAnchor == null) return;

            if (inSetupMode) {
                handState = setupModeHandState;
                if (setupModeHandState != null) {
                    var t = setupModeHandState.Transform.setupAnchor.Transform;
                    var r = t.Rotation;
                    var adjustedRotation = new Vector3(r.z, r.y, -r.x);
                    if (IsRightHanded) {
                        adjustedRotation.x *= -1;
                        adjustedRotation.z *= -1;
                    }

                    if (setupModeHandState == HandMouseMode) {
                        handSetupRootAnchor.Transform.Position = Vector3.zero;
                        handSetupRootAnchor.Transform.Rotation = Vector3.zero;
                        handPosition = t.Position;
                        handRotation = adjustedRotation;
                        handPositionOffset = Vector3.zero;
                        handRotationOffset = Vector3.zero;
                    } else {
                        handSetupRootAnchor.Transform.Position = HandMouseMode.Transform.Position;
                        handSetupRootAnchor.Transform.Rotation = HandMouseMode.Transform.Rotation;
                        handPosition = HandMouseMode.Transform.Position;
                        handRotation = HandMouseMode.Transform.Rotation;
                        handPositionOffset = t.Position;
                        handRotationOffset = adjustedRotation;
                    }
                    handSetupRootAnchor.Transform.Broadcast();
                }
            } else {
                handPosition = HandMouseMode.Transform.Position;
                handRotation = HandMouseMode.Transform.Rotation;
                handState = GetHandState(Source, Button1);
            }

            if (lastHandState != handState) {
                OnHandStateChange();
            }

            var ikt = handAnchor.Transform;
            ikt.Position = handPosition + handPositionOffset;
            ikt.Rotation = IsRightHanded
                ? IK_BASIS_RIGHT_HANDED
                : IK_BASIS_LEFT_HANDED;
            ikt.Rotation += handRotation + handRotationOffset;
            ikt.Broadcast();

            lastHandState = handState;
        }

        void OnHandStateChange() {
            var st = setupModeHandState ?? handState;
            if (st == null) return;

            var pos = st == HandMouseMode ? Vector3.zero : st.Transform.Position;
            var rot = st == HandMouseMode ? Vector3.zero : st.Transform.Rotation;
            var time = inSetupMode ? 0 : handState.EnterTransition.Time;
            var ease = handState.EnterTransition.Ease;

            SetHandOffsetTransforms(pos, rot, time, ease);

            Debug.Log($"ANIMTAION SET {st == HandMouseMode.ActiveState} {st?.Label}");

            HandMouseMode.StartAnimationTransition(true);
            HandMouseMode.ActiveState.StartAnimationTransition(st == HandMouseMode.ActiveState);
            HandPenMode.StartAnimationTransition(st == HandPenMode || st == HandPenMode.ActiveState);
            HandPenMode.ActiveState.StartAnimationTransition(st == HandPenMode.ActiveState);
        }

        void OnHandStateSettingChange() {
            OnHandStateChange();
        }

        void OnInputAffectingHandIKChange() {
            if (Character == null) return;

            handAnchor = GetHandIK();
            handAnchor.Attachable.Parent = GetCursorAnchor();
            handAnchor.Broadcast();

            EnableLimb(IsHandEnabled && !IsRightHanded, Character.LeftHandIK, handAnchor);
            EnableLimb(IsHandEnabled && IsRightHanded, Character.RightHandIK, handAnchor);
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

        void EnableLimb(bool isEnabled, LimbIKData limb, AnchorAsset anchor) {
            if (isEnabled) {
                limb.Enabled = true;
                limb.IkTarget = anchor;
                limb.PositionWeight = 1.0f;
                limb.RotationWeight = 1.0f;
            } else {
                limb.Enabled = false;
                limb.IkTarget = null;
            }
            limb.Broadcast();
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

            if (setupModeHandState == HandMouseMode) {
                GetBodyState(0, false)?.Transform.EnterVisualSetup(false);
                GetPropState(0, false)?.Transform.EnterVisualSetup(false);
            } else if (setupModeHandState == HandMouseMode.ActiveState) {
                GetBodyState(0, true)?.Transform.EnterVisualSetup(false);
                GetPropState(0, true)?.Transform.EnterVisualSetup(false);
            } else if (setupModeHandState == HandPenMode) {
                GetBodyState(2, false)?.Transform.EnterVisualSetup(false);
                GetPropState(2, false)?.Transform.EnterVisualSetup(false);
            } else if (setupModeHandState == HandPenMode.ActiveState) {
                GetBodyState(2, true)?.Transform.EnterVisualSetup(false);
                GetPropState(2, true)?.Transform.EnterVisualSetup(false);
            }
        }

        void HandleHandExitVisualSetup(PoseState p, bool isApply) {
            if (setupModeHandState == null) return;

            CleanDestroy(handSetupRootAnchor);
            ExitSetupModeMinimal();
            setupModeHandState = null;
            tweenHandPosition?.Kill();
            tweenHandRotation?.Kill();

            NeutralHandPosition.ExitVisualSetup(isApply);
            setupModeBodyState?.Transform.ExitVisualSetup(isApply);
            setupModePropState?.Transform.ExitVisualSetup(isApply);
        }

        void HandleHandCreateAnchor(PoseState p, VisualSetupTransform tr, VisualSetupAnchorAsset a) {
            handSetupRootAnchor = Scene.AddAsset<AnchorAsset>();
            handSetupRootAnchor.Attachable.Parent = cursorAnchorAsset;
            var title = "SETUP_ANCHOR_NAME_HAND_ROOT".Localized();
            handSetupRootAnchor.Name = $"🔒⌛⚓-{title}";
            Scene.UpdateNewAssetName(handSetupRootAnchor);

            a.Attachable.Parent = handSetupRootAnchor;
            a.Transform.Position = tr.Position;
            var r = tr.Rotation;
            if (IsRightHanded) {
                r.x *= -1;
                r.z *= -1;
            }
            a.Transform.Rotation = new Vector3(-r.z, r.y, r.x);
            a.Animation = p.Animation;
        }

        void HandleHandApplyAnchor(PoseState p, VisualSetupTransform tr, VisualSetupAnchorAsset a) {
            tr.Position = a.Transform.Position;
            var r = a.Transform.Rotation;
            if (IsRightHanded) {
                r.x *= -1;
                r.z *= -1;
            }
            tr.Rotation = new Vector3(r.z, r.y, -r.x);
        }

        void HandleHandAnimationChange(PoseState p) {
            Debug.Log("HandleHandAnimationChange");
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

            // Reorder layers
            pointerLayers.Sort((x, y) => x.CustomLayerID.CompareTo(y.CustomLayerID));
            userLayers.AddRange(pointerLayers);

            Character.SetDataInput($"{nameof(Character.OverlappingAnimations)}", userLayers.ToArray(), true);
        }

        void HandleHandAnimationWeightChange(PoseState p, float weight, bool resetAnimation = false) {
            if (Character == null) return;

            var layerId = p.GetAnimationLayerId(LAYER_NAME_PREFIX);
Debug.Log($"{p.Label}: layerId {layerId}");
            if (layerId == null) return;

            var animationData = Array.Find(
                Character.OverlappingAnimations,
                (a) => a.CustomLayerID == layerId
            );
Debug.Log($"{p.Label}: animationData {animationData}");
            if (animationData == null) return;

            animationData.Weight = weight;
            animationData.BroadcastDataInput("Weight");

            // Sanity: If no animation is set, then animancer layer is null
Debug.Log($"{p.Label}: animationData {animationData}");
            if (animationData.Animation == null) return;
            int animancerIdx = 1 + Array.IndexOf(Character.OverlappingAnimations, animationData);

            var layer = Character.Animancer.Layers[animancerIdx];
            var layer2 = Character.CloneAnimancer.Layers[animancerIdx];
Debug.Log($"{p.Label}: {weight}");
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
