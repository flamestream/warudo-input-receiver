using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Plugins.Core.Assets.Utility;
using static Warudo.Plugins.Core.Assets.Character.CharacterAsset;

namespace FlameStream
{
    public partial class PointerReceiverAsset : ReceiverAsset {

        public PoseState bodyState;
        PoseState lastBodyState;
        PoseState setupModeBodyState;
        Tween tweenBodyPosition;
        Tween tweenBodyRotation;
        Tween tweenBodyProgress;
        float bodyTweenProgress = 1f;
        Vector3 bodyPosition;
        Vector3 bodyRotation;
        Vector3 bodyPositionOffset;
        Vector3 bodyRotationOffset;
        AnchorAsset bodySetupRootAnchor;

        Tween characterMoveTween;


        Vector3 targetBodyPosition;
        Vector3 bodyDistanceFactor;

        void OnCreateMoveBody() {
            Watch(nameof(IsBodyEnabled), delegate { OnBodyEnabledChanged(); });
            OnBodyEnabledChanged();

            Watch(nameof(BodyMouseMode), delegate { OnBodyStateSettingChange(); });
            Watch(nameof(BodyPenMode), delegate { OnBodyStateSettingChange(); });
        }

        void OnReadyBody() {

            BodyMouseMode.OnAnimationChange = (p) => { HandleBodyAnimationChange((PoseState)p); };
            BodyMouseMode.OnAnimationWeightChange = (p, w, r) => { HandleBodyAnimationWeightChange((PoseState)p, w, r); };
            BodyMouseMode.OnCreateAnchor = (p, tr, a) => { HandleBodyCreateAnchor((PoseState)p, tr, a); };
            BodyMouseMode.OnEnterVisualSetup = (p) => { HandleBodyEnterVisualSetup((PoseState)p); };
            BodyMouseMode.OnExitVisualSetup = (p, ia) => { HandleBodyExitVisualSetup((PoseState)p, ia); };

            BodyPenMode.OnAnimationChange = (p) => { HandleBodyAnimationChange((PoseState)p); };
            BodyPenMode.OnAnimationWeightChange = (p, w, r) => { HandleBodyAnimationWeightChange((PoseState)p, w, r); };
            BodyPenMode.OnCreateAnchor = (p, tr, a) => { HandleBodyCreateAnchor((PoseState)p, tr, a); };
            BodyPenMode.OnEnterVisualSetup = (p) => { HandleBodyEnterVisualSetup((PoseState)p); };
            BodyPenMode.OnExitVisualSetup = (p, ia) => { HandleBodyExitVisualSetup((PoseState)p, ia); };
        }

        void OnUpdateBody() {
            if (!IsBodyEnabled) return;
            if (Character == null) return;
            if (cursorAnchorAsset == null) return;

            if (inSetupMode) {
                bodyState = setupModeBodyState;
                CalculateSetupBodyOffsets();
            } else {
                bodyState = GetBodyState(Source, Button1);
                if (bodyState != null) {

                    // Body target state
                    targetBodyPosition = cursorAnchorAsset.GameObject.transform.position + BodyMouseMode.Transform.Position;
                    bodyRotation = BodyMouseMode.Transform.Rotation;

                    // Dynamic extra offset
                    bodyDistanceFactor = screenAsset.GameObject.transform.InverseTransformPoint(targetBodyPosition) - screenAsset.GameObject.transform.InverseTransformPoint(Character.Transform.Position);
                    bodyDistanceFactor.x *= -1;
                    bodyRotation += BodyDynamicRotation.Evaluate(bodyDistanceFactor);

                    // Body movement style
                    var distanceFactor = bodyDistanceFactor.magnitude;
                    if (distanceFactor >= BodyMinimumMovementDelta) {
                        switch (BodyMovementType.Mode) {
                            case MovementMode.Elastic:
                                BodyMovementType.ElasticModeData.UpdateVelocity(
                                    Character.Transform.Position,
                                    targetBodyPosition,
                                    BodyMinimumMovementDelta
                                );
                                break;
                            default:
                                MoveCharacterFollowMode();
                                break;
                        }
                    }
                    if (BodyMovementType.Mode == MovementMode.Elastic) {
                        bodyPosition += BodyMovementType.ElasticModeData.Velocity;
                    }

                    // Sanity
                    if (float.IsNaN(bodyPosition.x) || float.IsNaN(bodyPosition.y) || float.IsNaN(bodyPosition.z)) {
                        bodyPosition = Vector3.zero;
                    }
                }
            }

            if (lastBodyState != bodyState) {
                OnBodyStateChange();
            }

            if (debugSphereCharacterTargetBasePosition != null) {
                debugSphereCharacterTargetBasePosition.transform.position = bodyPosition;
            }
            if (debugSphereCharacterTargetPosition != null) {
                debugSphereCharacterTargetPosition.transform.position = targetBodyPosition + bodyPositionOffset;
            }

            Character.Transform.Position = bodyPosition + bodyPositionOffset;
            Character.Transform.Rotation = bodyRotation + bodyRotationOffset;
            Character.Transform.Broadcast();

            lastBodyState = bodyState;
        }

        void OnBodyEnabledChanged() {
            GetDataInputPort(nameof(BodyMouseMode)).Properties.hidden = !(IsBodyEnabled && isBasicSetupComplete);
            GetDataInputPort(nameof(BodyPenMode)).Properties.hidden = !(IsBodyEnabled && isBasicSetupComplete);
            GetDataInputPort(nameof(BodyMovementType)).Properties.hidden = !(IsBodyEnabled && isBasicSetupComplete);
            GetDataInputPort(nameof(BodyMinimumMovementDelta)).Properties.hidden = !(IsBodyEnabled && isBasicSetupComplete);
            GetDataInputPort(nameof(BodyDynamicRotation)).Properties.hidden = !(IsBodyEnabled && isBasicSetupComplete);

            BroadcastDataInputProperties(nameof(BodyMouseMode));
            BroadcastDataInputProperties(nameof(BodyPenMode));
            BroadcastDataInputProperties(nameof(BodyMovementType));
            BroadcastDataInputProperties(nameof(BodyMinimumMovementDelta));
            BroadcastDataInputProperties(nameof(BodyDynamicRotation));
        }

        void OnBodyStateChange() {
            var st = setupModeBodyState ?? bodyState;

            var tr = GetGlobalCalculatorTransform();
            var r = Vector3.zero; // Used to allow 360+ degree rotation

            if (st != null && st != BodyMouseMode) {
                // tr.Translate(BodyMouseMode.Transform.Position);
                tr.Rotate(BodyMouseMode.Transform.Rotation);
                // r += BodyMouseMode.Transform.Rotation;

                if (st == BodyPenMode.ActiveState) {
                    tr.Translate(BodyPenMode.Transform.Position);
                    tr.Rotate(BodyPenMode.Transform.Rotation);
                    r += BodyPenMode.Transform.Rotation;
                }

                tr.Translate(st.Transform.Position);
                tr.Rotate(st.Transform.Rotation);
                r += st.Transform.Rotation;
            }

            var time = inSetupMode ? 0 : st?.EnterTransition.Time ?? BodyMouseMode.EnterTransition.Time;
            var ease = st?.EnterTransition.Ease ?? BodyMouseMode.EnterTransition.Ease;
            SetBodyOffsetTransforms(tr.position, r, time, ease);

            BodyMouseMode.StartAnimationTransition(st != null);
            BodyMouseMode.ActiveState.StartAnimationTransition(st == BodyMouseMode.ActiveState);
            BodyPenMode.StartAnimationTransition(st == BodyPenMode || st == BodyPenMode.ActiveState);
            BodyPenMode.ActiveState.StartAnimationTransition(st == BodyPenMode.ActiveState);
        }

        void CalculateSetupBodyOffsets() {
            if (setupModeBodyState == null) return;
            var st = setupModeBodyState;

            var tr = GetGlobalCalculatorTransform();
            var r = Vector3.zero; // Used to allow 360+ degree rotation
            tr.Translate(cursorAnchorAsset.GameObject.transform.position);

            if (st != BodyMouseMode) {
                tr.Translate(BodyMouseMode.Transform.Position);
                tr.Rotate(BodyMouseMode.Transform.Rotation);
                r += BodyMouseMode.Transform.Rotation;
            }

            // TODO: generalize
            if (st == BodyPenMode.ActiveState) {
                tr.Translate(BodyPenMode.Transform.Position);
                tr.Rotate(BodyPenMode.Transform.Rotation);
                r += BodyPenMode.Transform.Rotation;
            }

            var trr = bodySetupRootAnchor.Transform;
            trr.Position = bodyPosition = targetBodyPosition = tr.position;
            trr.Rotation = bodyRotation = tr.eulerAngles;
            trr.Broadcast();

            var trs = st.Transform.setupAnchor.Transform;
            var tr2 = GetGlobalCalculatorTransform();
            tr2.Rotate(trr.Rotation);
            tr2.Translate(trs.Position);
            tr2.Rotate(trs.Rotation);
            r += trs.Rotation;

            bodyPositionOffset = tr2.position;
            bodyRotationOffset = r;
        }

        void OnBodyStateSettingChange() {
            OnBodyStateChange();
        }

        void SetBodyOffsetTransforms(Vector3 pos, Vector3 rot, float time = 0, Ease ease = Ease.Linear) {
            // Tween doesn't seen to have a progress getter, so we're creating our own
            time *= bodyTweenProgress;
            bodyTweenProgress = 1 - bodyTweenProgress;
            tweenBodyProgress?.Kill();
            if (time == 0) {
                bodyTweenProgress = 1f;
            } else {
                tweenBodyProgress = DOTween.To(
                    () => bodyTweenProgress,
                    delegate(float it) { bodyTweenProgress = it; },
                    1f,
                    time
                ).SetEase(Ease.Linear);
            }

            tweenBodyPosition?.Kill();
            if (time == 0) {
                bodyPositionOffset = pos;
            } else {
                tweenBodyPosition = DOTween.To(
                    () => bodyPositionOffset,
                    delegate(Vector3 it) { bodyPositionOffset = it; },
                    pos,
                    time
                ).SetEase(ease);
            }

            tweenBodyRotation?.Kill();
            if (time == 0) {
                bodyRotationOffset = rot;
            } else {
                tweenBodyRotation = DOTween.To(
                    () => bodyRotationOffset,
                    delegate(Vector3 it) { bodyRotationOffset = it; },
                    rot,
                    time
                ).SetEase(ease);
            }
        }

        void MoveCharacterFollowMode() {
            var d = BodyMovementType.FollowModeData;
            characterMoveTween?.Kill();
            if (d.Time <= 0) {
                bodyPosition = targetBodyPosition;
            } else {
                characterMoveTween = DOTween.To(
                    () => bodyPosition,
                    delegate(Vector3 it) { bodyPosition = it; },
                    targetBodyPosition,
                    d.Time
                ).SetEase(d.Ease);
            }
        }

        PoseState GetBodyState(int source, bool button1) {

            if (!IsBodyEnabled) return null;

            var st = BodyPenMode.Enabled && source == 2
                    ? (BasePoseState)BodyPenMode
                    : BodyMouseMode;

            if (!st.Enabled) return null;

            var o = st.ActiveState.Enabled && button1
                ? (PoseState)st.ActiveState
                : st;

            return o;
        }

        void ResetCharacterPosition() {
            if (!IsBodyEnabled) return;
            Character.Transform.Position = cursorAnchorAsset.GameObject.transform.position + BodyMouseMode.Transform.Position;
            Character.Transform.BroadcastDataInput(nameof(Character.Transform.Position));
        }

        void HandleBodyEnterVisualSetup(PoseState p) {
            if (setupModeBodyState != null) return;
            EnterSetupModeMinimal();
            setupModeBodyState = p;
            tweenBodyPosition?.Kill();
            tweenBodyRotation?.Kill();

            var source = 0;
            var button1 = false;
            if (setupModeBodyState == BodyMouseMode.ActiveState) {
                button1 = true;
            } else if (setupModeBodyState == BodyPenMode) {
                source = 2;
            } else if (setupModeBodyState == BodyPenMode.ActiveState) {
                source = 2;
                button1 = true;
            }
            TriggerOtherEnterSetupModes(source, button1);
        }

        void HandleBodyExitVisualSetup(PoseState p, bool isApply) {
            if (setupModeBodyState == null) return;

            CleanDestroy(bodySetupRootAnchor);
            ExitSetupModeMinimal();
            setupModeBodyState = null;
            tweenHandPosition?.Kill();
            tweenHandRotation?.Kill();

            if (isApply) {
                NeutralHandPosition.setupAnchor?.ApplyTrigger();
                setupModeHandState?.Transform.setupAnchor?.ApplyTrigger();
                setupModePropState?.Transform.setupAnchor?.ApplyTrigger();
            } else {
                NeutralHandPosition.setupAnchor?.CancelTrigger();
                setupModeHandState?.Transform.setupAnchor?.CancelTrigger();
                setupModePropState?.Transform.setupAnchor?.CancelTrigger();
            }
            lastBodyState = null;
        }
        void HandleBodyCreateAnchor(PoseState p, VisualSetupTransform tr, VisualSetupAnchorAsset a) {
            bodySetupRootAnchor = Scene.AddAsset<AnchorAsset>();
            var title = "SETUP_ANCHOR_NAME_BODY_ROOT".Localized();
            bodySetupRootAnchor.Name = $"🔒⌛⚓-{title}";
            Scene.UpdateNewAssetName(bodySetupRootAnchor);

            a.Attachable.Parent = bodySetupRootAnchor;
            a.Animation = p.Animation;
        }
        void HandleBodyAnimationChange(PoseState p) {
            var userLayers = Character.OverlappingAnimations.Where(d => !d.CustomLayerID.StartsWith(LAYER_NAME_PREFIX)).ToList() ?? new List<OverlappingAnimationData>();
            var pointerLayers = Character.OverlappingAnimations.Where(d => d.CustomLayerID.StartsWith(LAYER_NAME_PREFIX)).ToList() ?? new List<OverlappingAnimationData>();

            var layerId = p.GetAnimationLayerId(LAYER_NAME_PREFIX);
            var layer = pointerLayers.FirstOrDefault(d => d.CustomLayerID == layerId);
            if (layer == null) {
                layer = StructuredData.Create<OverlappingAnimationData>();
                layer.Weight = setupModeBodyState == p ? 1f : 0f;
                layer.Speed = 1f;
                layer.Masked = false;
                layer.Additive = false;
                layer.Looping = false;
                layer.CustomLayerID = layerId;

                pointerLayers.Add(layer);
            }
            // Update values
            layer.Animation = p.Animation;

            // Remove invalid animations
            pointerLayers = pointerLayers.Where(d => d.Animation != null).ToList();

            // Reorder layers
            pointerLayers.Sort((x, y) => x.CustomLayerID.CompareTo(y.CustomLayerID));
            userLayers.AddRange(pointerLayers);

            Character.SetDataInput($"{nameof(Character.OverlappingAnimations)}", userLayers.ToArray(), true);
        }

        void HandleBodyAnimationWeightChange(PoseState p, float weight, bool resetAnimation = false) {
            if (Character == null) return;

            var layerId = p.GetAnimationLayerId(LAYER_NAME_PREFIX);
            if (layerId == null) return;

            var animationData = Array.Find(
                Character.OverlappingAnimations,
                (a) => a.CustomLayerID == layerId
            );
            if (animationData == null) return;

            animationData.SetDataInput(nameof(animationData.Weight), weight, true);

            int animancerIdx = 1 + Array.IndexOf(Character.OverlappingAnimations, animationData);

            var layer = Character.Animancer.Layers[animancerIdx];
            layer.SetWeight(weight);
            var layer2 = Character.CloneAnimancer.Layers[animancerIdx];
            layer2.SetWeight(weight);


            if (resetAnimation) {
                layer.Play(layer.CurrentState).Time = 0;
                layer2.Play(layer2.CurrentState).Time = 0;
            }
        }
    }
}
