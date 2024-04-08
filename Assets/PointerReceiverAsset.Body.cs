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
        Vector3 bodyPosition;
        Vector3 bodyRotation;
        Vector3 bodyPositionOffset;
        Vector3 bodyRotationOffset;
        AnchorAsset bodySetupRootAnchor;

        Tween characterMoveTween;


        Vector3 targetBodyPosition;
        Vector3 bodyDistanceFactor;

        void OnCreateMoveBody() {
            Watch(nameof(IsBodyEnabled), delegate { OnBodyEnabledSettingChange(); });
            OnBodyEnabledSettingChange();

            Watch(nameof(IsBodyEnabled), delegate { OnMoveCharacterEnabledChanged(); });
            OnMoveCharacterEnabledChanged();
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

        void OnUpdateMoveBody() {
            if (!IsBodyEnabled) return;
            if (Character == null) return;
            if (cursorAnchorAsset == null) return;

            if (inSetupMode) {
                if (setupModeBodyState != null) {
                    var t = setupModeBodyState.Transform.setupAnchor.GameObject.transform;
                    bodySetupRootAnchor.Transform.Position = cursorAnchorAsset.GameObject.transform.position;
                    bodySetupRootAnchor.Transform.Rotation = Vector3.zero;
                    bodySetupRootAnchor.Transform.Broadcast();
                    bodyPosition = t.position;
                    bodyRotation = t.rotation.eulerAngles;

                    if (debugSphereCharacterTargetPosition != null) {
                        debugSphereCharacterTargetPosition.transform.position = t.position;
                    }
                }
            } else {
                bodyState = GetBodyState(Source, Button1);

                if (bodyState != null) {

                    // Body position
                    targetBodyPosition = cursorAnchorAsset.GameObject.transform.position + bodyState.Transform.Position;
                    bodyDistanceFactor = screenAsset.GameObject.transform.InverseTransformPoint(targetBodyPosition) - screenAsset.GameObject.transform.InverseTransformPoint(Character.Transform.Position);
                    bodyDistanceFactor.x *= -1;
                    bodyRotation = BodyDynamicRotation.Evaluate(bodyDistanceFactor);

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

                if (lastBodyState != bodyState) {
                    OnBodyStateChange();
                }

                if (debugSphereCharacterTargetPosition != null) {
                    debugSphereCharacterTargetPosition.transform.position = targetBodyPosition;
                }
            }


            Character.Transform.Position = bodyPosition;
            Character.Transform.Rotation = bodyRotation;
            Character.Transform.Broadcast();

            lastBodyState = bodyState;
        }

        void OnMoveCharacterEnabledChanged() {
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

        void OnBodyStateChange(bool immediate = false) {

            var st = setupModeBodyState ?? bodyState;
            if (st == null) return;

            var pos = st.Transform.Position;
            var rot = st.Transform.Rotation;
            var time = immediate ? 0 : st.EnterTransition.Time;
            var ease = st.EnterTransition.Ease;

            SetBodyOffsetTransforms(pos, rot, time, ease);

            BodyMouseMode.StartAnimationTransition(st == BodyMouseMode);
            BodyMouseMode.ActiveState.StartAnimationTransition(st == BodyMouseMode.ActiveState);
        }

        void OnBodyEnabledSettingChange() {
            // if (!IsBodyEnabled) {
            //     bodyDistanceFactor = Vector3.zero;
            //     targetBodyPosition = Vector3.zero;
            // }
        }

        void SetBodyOffsetTransforms(Vector3 pos, Vector3 rot, float time = 0, Ease ease = Ease.Linear) {

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

            OnBodyStateChange();

            if (setupModeBodyState == BodyMouseMode) {
                GetHandState(0, false)?.Transform.EnterVisualSetup(false);
                GetPropState(0, false)?.Transform.EnterVisualSetup(false);
            } else if (setupModeBodyState == BodyMouseMode.ActiveState) {
                GetHandState(0, true)?.Transform.EnterVisualSetup(false);
                GetPropState(0, true)?.Transform.EnterVisualSetup(false);
            } else if (setupModeBodyState == BodyPenMode) {
                GetHandState(2, false)?.Transform.EnterVisualSetup(false);
                GetPropState(2, false)?.Transform.EnterVisualSetup(false);
            } else if (setupModeBodyState == BodyPenMode.ActiveState) {
                GetHandState(2, true)?.Transform.EnterVisualSetup(false);
                GetPropState(2, true)?.Transform.EnterVisualSetup(false);
            }
        }

        void HandleBodyExitVisualSetup(PoseState p, bool isApply) {
            if (setupModeBodyState == null) return;
            CleanDestroy(bodySetupRootAnchor);
            ExitSetupModeMinimal();
            setupModeBodyState = null;
            tweenHandPosition?.Kill();
            tweenHandRotation?.Kill();
            OnBodyStateChange();
            NeutralHandPosition.ExitVisualSetup(isApply);
            setupModeHandState?.Transform.ExitVisualSetup(isApply);
            setupModePropState?.Transform.ExitVisualSetup(isApply);
        }
        void HandleBodyCreateAnchor(PoseState p, VisualSetupTransform tr, VisualSetupAnchorAsset a) {
            bodySetupRootAnchor = Scene.AddAsset<AnchorAsset>();
            var title = "SETUP_ANCHOR_NAME_BODY_ROOT".Localized();
            bodySetupRootAnchor.Name = $"🔒⌛⚓-{title}";
            Scene.UpdateNewAssetName(bodySetupRootAnchor);
            a.Attachable.Parent = bodySetupRootAnchor;
        }
        void HandleBodyAnimationChange(PoseState p) {
            var layerId = p.GetAnimationLayerId(LAYER_NAME_PREFIX);
            var layer = Character.OverlappingAnimations?.FirstOrDefault(d => d.CustomLayerID == layerId);
            if (layer == null) {
                layer = StructuredData.Create<OverlappingAnimationData>();
                layer.Weight = 0f;
                layer.Speed = 1f;
                layer.Masked = false;
                layer.Additive = false;
                layer.Looping = false;
                layer.CustomLayerID = layerId;

                var list = Character.OverlappingAnimations?.ToList() ?? new List<OverlappingAnimationData>();
                // Ensure that it's before the hand poses
                var firstLayerElement = list.Find(d => d.CustomLayerID.StartsWith(LAYER_NAME_PREFIX));
                var idx = list.IndexOf(firstLayerElement);
                if (idx >= 0) {
                    list.Insert(idx, layer);
                } else {
                    list.Add(layer);
                }
                Character.SetDataInput($"{nameof(Character.OverlappingAnimations)}", list.ToArray(), true);
            }
            // layer.Animation = p.Animation;
            // layer.Broadcast();
            var idx2 = Array.IndexOf(Character.OverlappingAnimations, layer);
            Character.DataInputPortCollection.SetValueAtPath($"{nameof(Character.OverlappingAnimations)}.{idx2}.{nameof(layer.Animation)}", p.Animation, true);
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

            int animancerIdx = 1 + Array.IndexOf(Character.OverlappingAnimations, animationData);

            var layer = Character.Animancer.Layers[animancerIdx];
            layer.SetWeight(weight);
            var layer2 = Character.CloneAnimancer.Layers[animancerIdx];
            layer2.SetWeight(weight);
            animationData.Weight = weight;
            animationData.BroadcastDataInput("Weight");

            if (resetAnimation) {
                layer.Play(layer.CurrentState).Time = 0;
                layer2.Play(layer2.CurrentState).Time = 0;
            }
        }
    }
}
