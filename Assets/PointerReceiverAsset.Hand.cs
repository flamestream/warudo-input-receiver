using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
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
        Vector3 handPosition;
        Vector3 handRotation;
        AnchorAsset handAnchor;

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
                if (setupModeHandState != null) {
                    var tr = setupModeHandState.Transform.setupAnchor.Transform;
                    var r = tr.Rotation;
                    handPosition = tr.Position;
                    handRotation = new Vector3(r.z, r.y, -r.x);
                    if (IsRightHanded) {
                        handRotation.x *= -1;
                        handRotation.z *= -1;
                    }
                }
            } else {
                handState = GetHandState(Source, Button1);

                if (lastHandState != handState) {
                    OnHandStateChange();
                }
            }

            var ikt = handAnchor.Transform;
            ikt.Position = handPosition;
            ikt.Rotation = IsRightHanded
                ? IK_BASIS_RIGHT_HANDED
                : IK_BASIS_LEFT_HANDED;
            ikt.Rotation += handRotation;
            ikt.Broadcast();

            lastHandState = handState;
        }

        void OnHandStateChange(bool immediate = false) {
            if (handState == null) return;

            var pos = handState.Transform.Position;
            var rot = handState.Transform.Rotation;
            var time = immediate ? 0 : handState.EnterTransition.Time;
            var ease = handState.EnterTransition.Ease;

            SetHandTransforms(pos, rot, time, ease);

            HandMouseMode.StartAnimationTransition(handState == HandMouseMode);
            HandMouseMode.ActiveState.StartAnimationTransition(handState == HandMouseMode.ActiveState);
        }

        void OnHandStateSettingChange() {
            OnHandStateChange();
        }

        void OnInputAffectingHandIKChange() {
            if (!isReady) return;
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

        void SetHandTransforms(Vector3 pos, Vector3 rot, float time = 0, Ease ease = Ease.Linear) {

            tweenHandPosition?.Kill();
            if (time == 0) {
                handPosition = pos;
            } else {
                tweenHandPosition = DOTween.To(
                    () => handPosition,
                    delegate(Vector3 it) { handPosition = it; },
                    pos,
                    time
                ).SetEase(ease);
            }

            tweenHandRotation?.Kill();
            if (time == 0) {
                handRotation = rot;
            } else {
                tweenHandRotation = DOTween.To(
                    () => handRotation,
                    delegate(Vector3 it) { handRotation = it; },
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

            HandMouseMode.StartAnimationTransition(setupModeHandState == HandMouseMode);
            HandMouseMode.ActiveState.StartAnimationTransition(setupModeHandState == HandMouseMode.ActiveState);

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
            ExitSetupModeMinimal();
            setupModeHandState = null;
            tweenHandPosition?.Kill();
            tweenHandRotation?.Kill();
            OnHandStateChange();
            NeutralHandPosition.ExitVisualSetup(isApply);
            setupModeBodyState?.Transform.ExitVisualSetup(isApply);
            setupModePropState?.Transform.ExitVisualSetup(isApply);
        }

        void HandleHandCreateAnchor(PoseState p, VisualSetupTransform tr, VisualSetupAnchorAsset a) {
            a.Attachable.Parent = cursorAnchorAsset;
            a.Transform.Position = tr.Position;
            var r = tr.Rotation;
            if (IsRightHanded) {
                r.x *= -1;
                r.z *= -1;
            }
            a.Transform.Rotation = new Vector3(-r.z, r.y, r.x);
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
            var layerId = p.GetAnimationLayerId(LAYER_NAME_PREFIX);
            var layer = Character.OverlappingAnimations?.FirstOrDefault(d => d.CustomLayerID == layerId);
            if (layer == null) {
                layer = StructuredData.Create<OverlappingAnimationData>();
                layer.Weight = 0f;
                layer.Speed = 1f;
                layer.Masked = true;
                layer.Additive = false;
                layer.Looping = false;
                layer.CustomLayerID = layerId;

                var list = Character.OverlappingAnimations?.ToList() ?? new List<OverlappingAnimationData>();
                // Ensure that it's after the body poses
                var firstLayerElement = list.FindLast(d => d.CustomLayerID.StartsWith(LAYER_NAME_PREFIX));
                var idx = list.IndexOf(firstLayerElement);
                if (idx >= 0) {
                    list.Insert(idx + 1, layer);
                } else {
                    list.Add(layer);
                }
                Character.SetDataInput($"{nameof(Character.OverlappingAnimations)}", list.ToArray(), true);
            }
            layer.MaskedBodyParts = IsRightHanded
                ? new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers }
                : new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
            // layer.Animation = p.Animation;
            // layer.Broadcast();
            var idx2 = Array.IndexOf(Character.OverlappingAnimations, layer);
            Character.DataInputPortCollection.SetValueAtPath($"{nameof(Character.OverlappingAnimations)}.{idx2}.{nameof(layer.Animation)}", p.Animation, true);
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

            // Sanity: If no animation is set, then animancer layer is null
            if (animationData.Animation == null) return;
            int animancerIdx = 1 + Array.IndexOf(Character.OverlappingAnimations, animationData);

            var layer = Character.Animancer.Layers[animancerIdx];
            var layer2 = Character.CloneAnimancer.Layers[animancerIdx];

            layer.SetWeight(weight);
            layer2.SetWeight(weight);
            animationData.Weight = weight;
            animationData.BroadcastDataInput("Weight");

            if (resetAnimation) {
                // NOTE: Crash would occur if layer has no animation
                layer.Play(layer.CurrentState).Time = 0;
                layer2.Play(layer2.CurrentState).Time = 0;
            }
        }
    }
}
