using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Plugins.Core.Assets.Character;
using Cysharp.Threading.Tasks;

namespace FlameStream
{
    public partial class KeyboardReceiverAsset : ReceiverAsset {

        [Section("FINGER_MOVEMENT")]

        [DataInput(300)]
        [Label("FINGER_ANIMATIONS")]
        public KeyboardFingerAnimations FingerAnimations;

        [DataInput(310)]
        [Label("FINGER_TRANSITIONS")]
        public KeyboardFingerTransitions FingerTransitions;

        [DataInput(320)]
        [Label("FINGER_MOVEMENT_DEFINITIONS")]
        public KeyboardFingerMovementCollection FingerMovementDefinitions;

        public KeyboardKeyBindingGenerator BindingGenerator => FingerMovementDefinitions?.Generator;

        public KeyboardKeyBinding[] MovementDefinitions {
            get => FingerMovementDefinitions?.MovementDefinitions ?? new KeyboardKeyBinding[0];
            set {
                if (FingerMovementDefinitions == null) {
                    FingerMovementDefinitions = StructuredData.Create<KeyboardFingerMovementCollection>();
                    FingerMovementDefinitions.Parent = this;
                }
                FingerMovementDefinitions.MovementDefinitions = value;
                FingerMovementDefinitions.SetDataInput(nameof(FingerMovementDefinitions.MovementDefinitions), value, true);
            }
        }

        public KeyboardKeyBinding[] KeyBindings {
            get => MovementDefinitions;
            set => MovementDefinitions = value;
        }

        public Transition FingerPressTransition => FingerTransitions?.FingerPress ?? FingerTransitions?.Press;
        public Transition FingerReleaseTransition => FingerTransitions?.FingerRelease ?? FingerTransitions?.Release;
        public Transition HandMovementTransition => FingerTransitions?.HandMovement;
        public Transition HandReturnTransition => FingerTransitions?.HandReturn;

        public Transition KeyPressTransition => FingerPressTransition;
        public Transition KeyReleaseTransition => FingerReleaseTransition;

        // Runtime animation and movement state
        Tween[] fingerTweens = new Tween[10];
        private bool isBatchAutoGenerating = false;

        Vector3 currentLeftHandPos;
        Vector3 currentLeftHandRot;
        Vector3 currentRightHandPos;
        Vector3 currentRightHandRot;

        Vector3 targetLeftHandPos;
        Vector3 targetLeftHandRot;
        Vector3 targetRightHandPos;
        Vector3 targetRightHandRot;

        Tween leftHandPosTween;
        Tween leftHandRotTween;
        Tween rightHandPosTween;
        Tween rightHandRotTween;

        Vector3 lastLeftKeyPos;
        Vector3 lastLeftKeyRot;
        bool hasLeftKeyBeenPressed;
        float leftKeyReleaseTime = -1f;

        Vector3 lastRightKeyPos;
        Vector3 lastRightKeyRot;
        bool hasRightKeyBeenPressed;
        float rightKeyReleaseTime = -1f;

        void OnCreateFingerMovement() {
            if (FingerAnimations == null) {
                FingerAnimations = StructuredData.Create<KeyboardFingerAnimations>();
                FingerAnimations.CollapsedSelf = true;
            }
            FingerAnimations.Parent = this;

            if (FingerTransitions == null) {
                FingerTransitions = StructuredData.Create<KeyboardFingerTransitions>();
                FingerTransitions.CollapsedSelf = true;
                SetDataInput(nameof(FingerTransitions), FingerTransitions, true);
            }
            FingerTransitions.Parent = this;

            if (FingerMovementDefinitions == null) {
                FingerMovementDefinitions = StructuredData.Create<KeyboardFingerMovementCollection>();
                FingerMovementDefinitions.CollapsedSelf = true;
                SetDataInput(nameof(FingerMovementDefinitions), FingerMovementDefinitions, true);
            }
            FingerMovementDefinitions.Parent = this;
            if (FingerMovementDefinitions.Generator != null) {
                FingerMovementDefinitions.Generator.Parent = this;
                FingerMovementDefinitions.Generator.UpdateStatus();
            }

            Watch(nameof(FingerAnimations), delegate {
                if (FingerAnimations != null) {
                    FingerAnimations.Parent = this;
                }
                SynchronizeAllFingerAnimations();
            });

            Watch(nameof(FingerTransitions), delegate {
                if (FingerTransitions != null) {
                    FingerTransitions.Parent = this;
                }
            });

            Watch(nameof(FingerMovementDefinitions), delegate {
                if (FingerMovementDefinitions != null) {
                    FingerMovementDefinitions.Parent = this;
                    if (FingerMovementDefinitions.Generator != null) {
                        FingerMovementDefinitions.Generator.Parent = this;
                        FingerMovementDefinitions.Generator.UpdateStatus();
                    }
                }
                RefreshKeyBindingsParent();
            });

            if (MovementDefinitions == null) {
                MovementDefinitions = new KeyboardKeyBinding[0];
            }
            RefreshKeyBindingsParent();
        }

        public KeyboardFinger? activeVisualSetupFinger = null;

        public void RefreshKeyBindingsParent() {
            if (KeyBindings != null) {
                for (int i = 0; i < KeyBindings.Length; i++) {
                    if (KeyBindings[i] != null) {
                        KeyBindings[i].Parent = this;
                        KeyBindings[i].SetupTransformCallbacks();
                    }
                }
            }
            SetupAllFingertipVisualSetupTransforms();
        }

        void OnDestroyFingerMovement() {
            activeVisualSetupFinger = null;

            leftHandPosTween?.Kill(false);
            leftHandRotTween?.Kill(false);
            rightHandPosTween?.Kill(false);
            rightHandRotTween?.Kill(false);

            for (int i = 0; i < fingerTweens.Length; i++) {
                fingerTweens[i]?.Kill(false);
            }
            ResetActiveKeyValues();
        }

        public void ResetActiveKeyValues() {
            leftHandPosTween?.Kill(false);
            leftHandRotTween?.Kill(false);
            rightHandPosTween?.Kill(false);
            rightHandRotTween?.Kill(false);

            hasLeftKeyBeenPressed = false;
            hasRightKeyBeenPressed = false;
            leftKeyReleaseTime = -1f;
            rightKeyReleaseTime = -1f;
            lastLeftKeyPos = LeftHandRestPosition;
            lastLeftKeyRot = LeftHandRestRotation;
            lastRightKeyPos = RightHandRestPosition;
            lastRightKeyRot = RightHandRestRotation;
            currentLeftHandPos = LeftHandRestPosition;
            currentLeftHandRot = LeftHandRestRotation;
            currentRightHandPos = RightHandRestPosition;
            currentRightHandRot = RightHandRestRotation;
            targetLeftHandPos = LeftHandRestPosition;
            targetLeftHandRot = LeftHandRestRotation;
            targetRightHandPos = RightHandRestPosition;
            targetRightHandRot = RightHandRestRotation;
            LastVkCode = 0;
        }

        void OnUpdateFingerMovement() {
            if (!IsEnabled || !IsHandEnabled || Character == null) return;

            if (KeyBindings != null && KeyBindings.Length > 0 && KeyBindings[0] != null && KeyBindings[0].Parent == null) {
                RefreshKeyBindingsParent();
            }

            float[] targetFingerWeights = new float[10];

            var activeKeySetup = ActiveKeyBindingInVisualSetup;
            bool isLeftKeySetup = activeKeySetup != null && activeKeySetup.Finger.IsLeftHand() && activeKeySetup.Transform != null && activeKeySetup.Transform.InVisualSetupMode;
            bool isRightKeySetup = activeKeySetup != null && !activeKeySetup.Finger.IsLeftHand() && activeKeySetup.Transform != null && activeKeySetup.Transform.InVisualSetupMode;

            // 1. Process Left Hand (Position & rotation driven by newest active key or visual setup)
            bool isLeftInVisualSetup = isLeftKeySetup || (LeftHandRestTransform != null && LeftHandRestTransform.InVisualSetupMode);
            if (isLeftKeySetup) {
                activeKeySetup.UpdateDebugVisuals();
            }

            var activeLeftKey = isLeftInVisualSetup ? null : GetActiveKeyForHand(true, KeyBindings);
            Vector3 restLeftPos = (LeftHandRestTransform != null && LeftHandRestTransform.InVisualSetupMode && LeftHandRestTransform.setupAnchor != null)
                ? LeftHandRestTransform.setupAnchor.Transform.Position
                : LeftHandRestPosition;
            Vector3 restLeftRot = (LeftHandRestTransform != null && LeftHandRestTransform.InVisualSetupMode && LeftHandRestTransform.setupAnchor != null)
                ? LeftHandRestTransform.setupAnchor.Transform.Rotation
                : LeftHandRestRotation;

            Vector3 targetLeftPos;
            Vector3 targetLeftRot;

            if (isLeftKeySetup && activeKeySetup.Transform.setupAnchor != null) {
                targetLeftPos = activeKeySetup.Transform.setupAnchor.Transform.Position;
                targetLeftRot = activeKeySetup.Transform.setupAnchor.Transform.Rotation;
                currentLeftHandPos = targetLeftPos;
                currentLeftHandRot = targetLeftRot;
                lastLeftKeyPos = targetLeftPos;
                lastLeftKeyRot = targetLeftRot;
                hasLeftKeyBeenPressed = false;
                leftKeyReleaseTime = -1f;
            } else if (isLeftInVisualSetup) {
                targetLeftPos = restLeftPos;
                targetLeftRot = restLeftRot;
                currentLeftHandPos = restLeftPos;
                currentLeftHandRot = restLeftRot;
                lastLeftKeyPos = restLeftPos;
                lastLeftKeyRot = restLeftRot;
                hasLeftKeyBeenPressed = false;
                leftKeyReleaseTime = -1f;
            } else if (activeLeftKey != null) {
                if (activeLeftKey.EnableHandMovement) {
                    var cluster = GetKeyClusterTransform(activeLeftKey);
                    targetLeftPos = cluster.TransformLocalToRig(activeLeftKey.DeltaPosition);
                    targetLeftRot = Helper.NormalizeRotationAngles(cluster.TransformRotationToRig(Quaternion.Euler(activeLeftKey.DeltaRotation)).eulerAngles);
                } else {
                    targetLeftPos = hasLeftKeyBeenPressed ? lastLeftKeyPos : restLeftPos;
                    targetLeftRot = hasLeftKeyBeenPressed ? lastLeftKeyRot : restLeftRot;
                }

                lastLeftKeyPos = targetLeftPos;
                lastLeftKeyRot = targetLeftRot;
                hasLeftKeyBeenPressed = true;
                leftKeyReleaseTime = -1f;
            } else {
                if (leftKeyReleaseTime < 0f) {
                    leftKeyReleaseTime = Time.time;
                }

                if (ReturnToRest && hasLeftKeyBeenPressed && (Time.time - leftKeyReleaseTime >= RestTimeout)) {
                    // Automatically return to resting pose after timeout -> return and reset active key values
                    targetLeftPos = restLeftPos;
                    targetLeftRot = restLeftRot;
                    hasLeftKeyBeenPressed = false;
                    leftKeyReleaseTime = -1f;
                    lastLeftKeyPos = restLeftPos;
                    lastLeftKeyRot = restLeftRot;
                } else if (hasLeftKeyBeenPressed) {
                    // Stay at last pressed key's position (when ReturnToRest is false, or before timeout expires)
                    targetLeftPos = lastLeftKeyPos;
                    targetLeftRot = lastLeftKeyRot;
                } else {
                    targetLeftPos = restLeftPos;
                    targetLeftRot = restLeftRot;
                }
            }

            // 2. Process Right Hand (Position & rotation driven by newest active key or visual setup)
            bool isRightInVisualSetup = isRightKeySetup || (RightHandRestTransform != null && RightHandRestTransform.InVisualSetupMode);
            if (isRightKeySetup) {
                activeKeySetup.UpdateDebugVisuals();
            }

            var activeRightKey = isRightInVisualSetup ? null : GetActiveKeyForHand(false, KeyBindings);
            Vector3 restRightPos = (RightHandRestTransform != null && RightHandRestTransform.InVisualSetupMode && RightHandRestTransform.setupAnchor != null)
                ? RightHandRestTransform.setupAnchor.Transform.Position
                : RightHandRestPosition;
            Vector3 restRightRot = (RightHandRestTransform != null && RightHandRestTransform.InVisualSetupMode && RightHandRestTransform.setupAnchor != null)
                ? RightHandRestTransform.setupAnchor.Transform.Rotation
                : RightHandRestRotation;

            Vector3 targetRightPos;
            Vector3 targetRightRot;

            if (isRightKeySetup && activeKeySetup.Transform.setupAnchor != null) {
                targetRightPos = activeKeySetup.Transform.setupAnchor.Transform.Position;
                targetRightRot = activeKeySetup.Transform.setupAnchor.Transform.Rotation;
                currentRightHandPos = targetRightPos;
                currentRightHandRot = targetRightRot;
                lastRightKeyPos = targetRightPos;
                lastRightKeyRot = targetRightRot;
                hasRightKeyBeenPressed = false;
                rightKeyReleaseTime = -1f;
            } else if (isRightInVisualSetup) {
                targetRightPos = restRightPos;
                targetRightRot = restRightRot;
                currentRightHandPos = restRightPos;
                currentRightHandRot = restRightRot;
                lastRightKeyPos = restRightPos;
                lastRightKeyRot = restRightRot;
                hasRightKeyBeenPressed = false;
                rightKeyReleaseTime = -1f;
            } else if (activeRightKey != null) {
                if (activeRightKey.EnableHandMovement) {
                    var cluster = GetKeyClusterTransform(activeRightKey);
                    targetRightPos = cluster.TransformLocalToRig(activeRightKey.DeltaPosition);
                    targetRightRot = Helper.NormalizeRotationAngles(cluster.TransformRotationToRig(Quaternion.Euler(activeRightKey.DeltaRotation)).eulerAngles);
                } else {
                    targetRightPos = hasRightKeyBeenPressed ? lastRightKeyPos : restRightPos;
                    targetRightRot = hasRightKeyBeenPressed ? lastRightKeyRot : restRightRot;
                }

                lastRightKeyPos = targetRightPos;
                lastRightKeyRot = targetRightRot;
                hasRightKeyBeenPressed = true;
                rightKeyReleaseTime = -1f;
            } else {
                if (rightKeyReleaseTime < 0f) {
                    rightKeyReleaseTime = Time.time;
                }

                if (ReturnToRest && hasRightKeyBeenPressed && (Time.time - rightKeyReleaseTime >= RestTimeout)) {
                    // Automatically return to resting pose after timeout -> return and reset active key values
                    targetRightPos = restRightPos;
                    targetRightRot = restRightRot;
                    hasRightKeyBeenPressed = false;
                    rightKeyReleaseTime = -1f;
                    lastRightKeyPos = restRightPos;
                    lastRightKeyRot = restRightRot;
                } else if (hasRightKeyBeenPressed) {
                    // Stay at last pressed key's position (when ReturnToRest is false, or before timeout expires)
                    targetRightPos = lastRightKeyPos;
                    targetRightRot = lastRightKeyRot;
                } else {
                    targetRightPos = restRightPos;
                    targetRightRot = restRightRot;
                }
            }

            // 3. Populate targetFingerWeights for ALL currently pressed keys across all bindings
            if (KeyBindings != null) {
                for (int i = 0; i < KeyBindings.Length; i++) {
                    var binding = KeyBindings[i];
                    if (binding == null) continue;

                    bool isLeft = binding.Finger.IsLeftHand();
                    if (isLeft && isLeftInVisualSetup) continue;
                    if (!isLeft && isRightInVisualSetup) continue;

                    if (Down((int)binding.Key)) {
                        targetFingerWeights[(int)binding.Finger] = 1.0f;
                    }

                    if (Activated((int)binding.Key)) {
                        PlayFingerAnimation(binding.Finger);
                    }
                }
            }

            if (isLeftKeySetup) {
                targetFingerWeights[(int)activeKeySetup.Finger] = 1.0f;
            }
            if (isRightKeySetup) {
                targetFingerWeights[(int)activeKeySetup.Finger] = 1.0f;
            }

            // 4. Drive Left Hand Anchor
            var leftAnchor = LeftHandAnchor;
            if (leftAnchor != null) {
                if (isLeftInVisualSetup) {
                    leftHandPosTween?.Kill(false);
                    leftHandRotTween?.Kill(false);
                    currentLeftHandPos = targetLeftPos;
                    currentLeftHandRot = targetLeftRot;
                    targetLeftHandPos = targetLeftPos;
                    targetLeftHandRot = targetLeftRot;
                } else {
                    bool isLeftReturningToRest = (activeLeftKey == null && !hasLeftKeyBeenPressed);
                    var leftTrans = isLeftReturningToRest ? HandReturnTransition : HandMovementTransition;

                    if (targetLeftHandPos != targetLeftPos) {
                        targetLeftHandPos = targetLeftPos;
                        leftHandPosTween?.Kill(false);
                        if (leftTrans != null && leftTrans.Time > 0f) {
                            leftHandPosTween = DOTween.To(() => currentLeftHandPos, v => currentLeftHandPos = v, targetLeftPos, leftTrans.Time).SetEase(leftTrans.Ease);
                        } else {
                            currentLeftHandPos = targetLeftPos;
                        }
                    }

                    if (targetLeftHandRot != targetLeftRot) {
                        targetLeftHandRot = targetLeftRot;
                        leftHandRotTween?.Kill(false);
                        if (leftTrans != null && leftTrans.Time > 0f) {
                            leftHandRotTween = DOTween.To(() => currentLeftHandRot, v => currentLeftHandRot = v, targetLeftRot, leftTrans.Time).SetEase(leftTrans.Ease);
                        } else {
                            currentLeftHandRot = targetLeftRot;
                        }
                    }
                }

                leftAnchor.Transform.Position = currentLeftHandPos;
                leftAnchor.Transform.Rotation = ApplyHandIKIntermediateRotation(true, currentLeftHandRot);
                leftAnchor.Transform.Broadcast();
            }

            // 5. Drive Right Hand Anchor
            var rightAnchor = RightHandAnchor;
            if (rightAnchor != null) {
                if (isRightInVisualSetup) {
                    rightHandPosTween?.Kill(false);
                    rightHandRotTween?.Kill(false);
                    currentRightHandPos = targetRightPos;
                    currentRightHandRot = targetRightRot;
                    targetRightHandPos = targetRightPos;
                    targetRightHandRot = targetRightRot;
                } else {
                    bool isRightReturningToRest = (activeRightKey == null && !hasRightKeyBeenPressed);
                    var rightTrans = isRightReturningToRest ? HandReturnTransition : HandMovementTransition;

                    if (targetRightHandPos != targetRightPos) {
                        targetRightHandPos = targetRightPos;
                        rightHandPosTween?.Kill(false);
                        if (rightTrans != null && rightTrans.Time > 0f) {
                            rightHandPosTween = DOTween.To(() => currentRightHandPos, v => currentRightHandPos = v, targetRightPos, rightTrans.Time).SetEase(rightTrans.Ease);
                        } else {
                            currentRightHandPos = targetRightPos;
                        }
                    }

                    if (targetRightHandRot != targetRightRot) {
                        targetRightHandRot = targetRightRot;
                        rightHandRotTween?.Kill(false);
                        if (rightTrans != null && rightTrans.Time > 0f) {
                            rightHandRotTween = DOTween.To(() => currentRightHandRot, v => currentRightHandRot = v, targetRightRot, rightTrans.Time).SetEase(rightTrans.Ease);
                        } else {
                            currentRightHandRot = targetRightRot;
                        }
                    }
                }

                rightAnchor.Transform.Position = currentRightHandPos;
                rightAnchor.Transform.Rotation = ApplyHandIKIntermediateRotation(false, currentRightHandRot);
                rightAnchor.Transform.Broadcast();
            }

            // 6. Drive 10 Finger Overlay Animations
            if (isBatchAutoGenerating) {
                // Batch auto-generation in progress: keep all 10 finger weights pinned at 1.0f
                if (Character.OverlappingAnimations != null) {
                    for (int f = 0; f < 10; f++) {
                        var layerId = GetFingerLayerId((KeyboardFinger)f);
                        var layerData = Array.Find(Character.OverlappingAnimations, a => a.CustomLayerID == layerId);
                        if (layerData != null) {
                            int animancerLayerIdx = GetAnimancerLayerIndex(layerData);
                            if (animancerLayerIdx >= 0) {
                                SetAnimancerLayerWeight(animancerLayerIdx, 1.0f);
                            }
                        }
                    }
                }
            } else if (Character.OverlappingAnimations != null) {
                for (int f = 0; f < 10; f++) {
                    KeyboardFinger finger = (KeyboardFinger)f;
                    float weight = (activeVisualSetupFinger.HasValue && activeVisualSetupFinger.Value == finger)
                        ? 1.0f
                        : targetFingerWeights[f];
                    ApplyFingerLayerWeight(finger, f, weight);
                }
            }
        }

        public int GetAnimancerLayerIndex(CharacterAsset.OverlappingAnimationData layerData) {
            if (Character == null || Character.OverlappingAnimations == null || layerData == null) return -1;
            // In Warudo, CharacterAsset.SetupOverlappingAnimations filters by enabled layers with valid clips
            var activeLayers = Character.OverlappingAnimations.Where(a => a.Enabled && a.GetAnimationClip() != null).ToList();
            var indexInActive = activeLayers.IndexOf(layerData);
            if (indexInActive < 0) return -1;
            return 1 + indexInActive;
        }

        void SetAnimancerLayerWeight(int animancerLayerIdx, float weight) {
            SetAnimancerComponentLayerWeight(Character?.Animancer, animancerLayerIdx, weight);
            SetAnimancerComponentLayerWeight(Character?.CloneAnimancer, animancerLayerIdx, weight);
        }

        void SetAnimancerComponentLayerWeight(Animancer.AnimancerComponent animancer, int animancerLayerIdx, float weight) {
            if (animancer == null || animancer.Layers == null || animancerLayerIdx < 0 || animancerLayerIdx >= animancer.Layers.Count) return;
            var layer = animancer.Layers[animancerLayerIdx];
            if (layer == null) return;
            layer.SetWeight(weight);
            if (weight > 0.001f) {
                var state = layer.CurrentState;
                if (state != null && !state.IsPlaying) {
                    state.IsPlaying = true;
                }
            }
        }

        public void PlayFingerAnimation(KeyboardFinger finger) {
            if (Character == null || Character.OverlappingAnimations == null) return;
            var layerId = GetFingerLayerId(finger);
            var layerData = Array.Find(Character.OverlappingAnimations, a => a.CustomLayerID == layerId);
            if (layerData == null) return;

            int animancerLayerIdx = GetAnimancerLayerIndex(layerData);
            if (animancerLayerIdx < 0) return;

            RewindAnimancerLayer(Character.Animancer, animancerLayerIdx);
            RewindAnimancerLayer(Character.CloneAnimancer, animancerLayerIdx);
        }

        void RewindAnimancerLayer(Animancer.AnimancerComponent animancer, int animancerLayerIdx) {
            if (animancer == null || animancer.Layers == null || animancerLayerIdx < 0 || animancerLayerIdx >= animancer.Layers.Count) return;
            var layer = animancer.Layers[animancerLayerIdx];
            if (layer == null) return;
            var state = layer.CurrentState;
            if (state == null) {
                foreach (Animancer.AnimancerState s in layer) {
                    state = s;
                    break;
                }
            }
            if (state != null) {
                var playingState = layer.Play(state);
                playingState.Time = 0f;
            }
        }

        void ApplyFingerLayerWeight(KeyboardFinger finger, int fingerIndex, float targetWeight) {
            if (Character == null || Character.OverlappingAnimations == null) return;

            var layerId = GetFingerLayerId(finger);
            var layerData = Array.Find(Character.OverlappingAnimations, a => a.CustomLayerID == layerId);
            if (layerData == null) return;

            int animancerLayerIdx = GetAnimancerLayerIndex(layerData);
            if (animancerLayerIdx < 0) return;

            var animancer = Character.Animancer;
            if (animancer == null || animancer.Layers == null || animancerLayerIdx >= animancer.Layers.Count) return;

            var layer = animancer.Layers[animancerLayerIdx];
            if (layer == null) return;

            var currentWeight = layer.Weight;
            if (Mathf.Abs(currentWeight - targetWeight) < 0.001f) return;

            var trans = targetWeight > 0.01f ? FingerPressTransition : FingerReleaseTransition;

            if (trans != null && trans.Time > 0f) {
                fingerTweens[fingerIndex]?.Kill(false);
                float weightDelta = Mathf.Abs(targetWeight - currentWeight);
                float duration = trans.Time * Mathf.Max(0.2f, weightDelta);
                fingerTweens[fingerIndex] = DOTween.To(
                    () => {
                        if (animancer != null && animancer.Layers != null && animancerLayerIdx < animancer.Layers.Count) {
                            return animancer.Layers[animancerLayerIdx].Weight;
                        }
                        return 0f;
                    },
                    v => {
                        SetAnimancerLayerWeight(animancerLayerIdx, v);
                    },
                    targetWeight,
                    duration
                ).SetEase(trans.Ease);
            } else {
                fingerTweens[fingerIndex]?.Kill(false);
                SetAnimancerLayerWeight(animancerLayerIdx, targetWeight);
            }
        }

        public void ResetFingerWeights() {
            for (int i = 0; i < fingerTweens.Length; i++) {
                fingerTweens[i]?.Kill();
            }

            ResetActiveKeyValues();

            if (Character == null || Character.OverlappingAnimations == null) return;

            for (int f = 0; f < 10; f++) {
                var layerId = GetFingerLayerId((KeyboardFinger)f);
                var layerData = Array.Find(Character.OverlappingAnimations, a => a.CustomLayerID == layerId);
                if (layerData != null) {
                    int animancerLayerIdx = GetAnimancerLayerIndex(layerData);
                    if (animancerLayerIdx >= 0) {
                        SetAnimancerLayerWeight(animancerLayerIdx, 0f);
                    }
                }
            }
        }


        #region Auto-Generate Key Bindings from Floating Keys

        private HumanBodyBones GetDistalBoneForFinger(KeyboardFinger finger) {
            switch (finger) {
                case KeyboardFinger.LeftThumb: return HumanBodyBones.LeftThumbDistal;
                case KeyboardFinger.LeftIndex: return HumanBodyBones.LeftIndexDistal;
                case KeyboardFinger.LeftMiddle: return HumanBodyBones.LeftMiddleDistal;
                case KeyboardFinger.LeftRing: return HumanBodyBones.LeftRingDistal;
                case KeyboardFinger.LeftPinky: return HumanBodyBones.LeftLittleDistal;
                case KeyboardFinger.RightThumb: return HumanBodyBones.RightThumbDistal;
                case KeyboardFinger.RightIndex: return HumanBodyBones.RightIndexDistal;
                case KeyboardFinger.RightMiddle: return HumanBodyBones.RightMiddleDistal;
                case KeyboardFinger.RightRing: return HumanBodyBones.RightRingDistal;
                case KeyboardFinger.RightPinky: return HumanBodyBones.RightLittleDistal;
                default: return HumanBodyBones.LeftIndexDistal;
            }
        }

        private HumanBodyBones GetIntermediateBoneForFinger(KeyboardFinger finger) {
            switch (finger) {
                case KeyboardFinger.LeftThumb: return HumanBodyBones.LeftThumbIntermediate;
                case KeyboardFinger.LeftIndex: return HumanBodyBones.LeftIndexIntermediate;
                case KeyboardFinger.LeftMiddle: return HumanBodyBones.LeftMiddleIntermediate;
                case KeyboardFinger.LeftRing: return HumanBodyBones.LeftRingIntermediate;
                case KeyboardFinger.LeftPinky: return HumanBodyBones.LeftLittleIntermediate;
                case KeyboardFinger.RightThumb: return HumanBodyBones.RightThumbIntermediate;
                case KeyboardFinger.RightIndex: return HumanBodyBones.RightIndexIntermediate;
                case KeyboardFinger.RightMiddle: return HumanBodyBones.RightMiddleIntermediate;
                case KeyboardFinger.RightRing: return HumanBodyBones.RightRingIntermediate;
                case KeyboardFinger.RightPinky: return HumanBodyBones.RightLittleIntermediate;
                default: return HumanBodyBones.LeftIndexIntermediate;
            }
        }

        public KeyboardFinger GetOppositeHandFinger(KeyboardFinger finger) {
            switch (finger) {
                case KeyboardFinger.LeftThumb: return KeyboardFinger.RightThumb;
                case KeyboardFinger.LeftIndex: return KeyboardFinger.RightIndex;
                case KeyboardFinger.LeftMiddle: return KeyboardFinger.RightMiddle;
                case KeyboardFinger.LeftRing: return KeyboardFinger.RightRing;
                case KeyboardFinger.LeftPinky: return KeyboardFinger.RightPinky;
                case KeyboardFinger.RightThumb: return KeyboardFinger.LeftThumb;
                case KeyboardFinger.RightIndex: return KeyboardFinger.LeftIndex;
                case KeyboardFinger.RightMiddle: return KeyboardFinger.LeftMiddle;
                case KeyboardFinger.RightRing: return KeyboardFinger.LeftRing;
                case KeyboardFinger.RightPinky: return KeyboardFinger.LeftPinky;
                default: return finger;
            }
        }

        public Vector3 GetFingerTipWorldPosition(Animator anim, KeyboardFinger finger) {
            Vector3 offset = FingerMovementDefinitions?.FingertipOffsets?.GetOffset(finger) ?? new Vector3(0f, 0f, 0.012f);
            var distalBone = GetDistalBoneForFinger(finger);
            var distalTransform = anim != null ? anim.GetBoneTransform(distalBone) : null;
            if (distalTransform != null) {
                return distalTransform.TransformPoint(offset);
            }

            var fallbackHandBone = finger.IsLeftHand() ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
            var fallbackHandTransform = anim != null ? anim.GetBoneTransform(fallbackHandBone) : null;
            return fallbackHandTransform != null ? fallbackHandTransform.position : Vector3.zero;
        }

        public Vector3 GetKeyboardBedNormalWorld(VirtualKeyAnchor anchor = VirtualKeyAnchor.KeyboardRoot) {
            Transform rootTr = RootAnchor?.GameObject != null ? RootAnchor.GameObject.transform : null;
            Quaternion rot = Quaternion.identity;
            if (anchor == VirtualKeyAnchor.LeftHand) {
                rot = Quaternion.Euler(LeftHandRestRotation) * Quaternion.Euler(LeftKeysetTransform?.Rotation ?? Vector3.zero);
            } else if (anchor == VirtualKeyAnchor.RightHand) {
                rot = Quaternion.Euler(RightHandRestRotation) * Quaternion.Euler(RightKeysetTransform?.Rotation ?? Vector3.zero);
            } else {
                rot = Quaternion.Euler(RootKeysetTransform?.Rotation ?? Vector3.zero);
            }
            Vector3 normal = rot * Vector3.up;
            return rootTr != null ? rootTr.rotation * normal : normal;
        }

        public void OnVirtualKeyHandAlignmentReferenceChanged(VirtualKeyDefinition changedKey) {
            if (changedKey == null || !changedKey.IsHandAlignmentReference) return;
            bool isLeft = changedKey.AssignedFinger.IsLeftHand();
            if (VirtualKeysCollection?.Keys == null) return;
            foreach (var other in VirtualKeysCollection.Keys) {
                if (other == null || other == changedKey) continue;
                if (other.AssignedFinger.IsLeftHand() == isLeft && other.IsHandAlignmentReference) {
                    other.IsHandAlignmentReference = false;
                    other.SetDataInput(nameof(other.IsHandAlignmentReference), false, true);
                    other.BroadcastDataInput(nameof(other.IsHandAlignmentReference));
                    other.RequestUiRefresh();
                }
            }
            BindingGenerator?.UpdateStatus();
        }

        public VirtualKeyDefinition GetHandAlignmentReferenceKey(bool isLeftHand) {
            if (VirtualKeysCollection?.Keys == null) return null;
            // 1. Look for explicit reference key
            var refKey = VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.AssignedFinger.IsLeftHand() == isLeftHand && k.IsHandAlignmentReference);
            if (refKey != null) return refKey;
            // 2. Fallback to first key in list for that hand
            return VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.AssignedFinger.IsLeftHand() == isLeftHand);
        }

        public Vector3 GetHandAlignmentReferenceBedNormalWorld(bool isLeftHand, VirtualKeyAnchor fallbackAnchor = VirtualKeyAnchor.KeyboardRoot) {
            var refKey = GetHandAlignmentReferenceKey(isLeftHand);
            if (refKey != null && TryGetKeyCenterAndNormal(refKey, out _, out Vector3 refNormal)) {
                return refNormal;
            }
            return GetKeyboardBedNormalWorld(fallbackAnchor);
        }

        public Vector3 GetHandAlignmentReferenceBedNormalWorldIdentity(bool isLeftHand, VirtualKeyAnchor fallbackAnchor = VirtualKeyAnchor.KeyboardRoot) {
            var refKey = GetHandAlignmentReferenceKey(isLeftHand);
            if (refKey != null && TryGetKeyCenterAndNormalIdentity(refKey, out _, out Vector3 refNormal)) {
                return refNormal;
            }
            Transform rootTr = RootAnchor?.GameObject != null ? RootAnchor.GameObject.transform : null;
            Quaternion rot = Quaternion.identity;
            if (fallbackAnchor == VirtualKeyAnchor.LeftHand) {
                rot = Quaternion.Euler(LeftHandRestRotation);
            } else if (fallbackAnchor == VirtualKeyAnchor.RightHand) {
                rot = Quaternion.Euler(RightHandRestRotation);
            }
            Vector3 normal = rot * Vector3.up;
            return rootTr != null ? rootTr.rotation * normal : normal;
        }

        public void SetupAllFingertipVisualSetupTransforms() {
            var offsets = FingerMovementDefinitions?.FingertipOffsets;
            if (offsets == null) return;
            offsets.EnsureTransforms();

            SetupFingertipVisualSetupTransform(offsets.UnifiedOffset, KeyboardFinger.LeftIndex, true);
            SetupFingertipVisualSetupTransform(offsets.LeftThumb, KeyboardFinger.LeftThumb, false);
            SetupFingertipVisualSetupTransform(offsets.LeftIndex, KeyboardFinger.LeftIndex, false);
            SetupFingertipVisualSetupTransform(offsets.LeftMiddle, KeyboardFinger.LeftMiddle, false);
            SetupFingertipVisualSetupTransform(offsets.LeftRing, KeyboardFinger.LeftRing, false);
            SetupFingertipVisualSetupTransform(offsets.LeftPinky, KeyboardFinger.LeftPinky, false);

            SetupFingertipVisualSetupTransform(offsets.RightThumb, KeyboardFinger.RightThumb, false);
            SetupFingertipVisualSetupTransform(offsets.RightIndex, KeyboardFinger.RightIndex, false);
            SetupFingertipVisualSetupTransform(offsets.RightMiddle, KeyboardFinger.RightMiddle, false);
            SetupFingertipVisualSetupTransform(offsets.RightRing, KeyboardFinger.RightRing, false);
            SetupFingertipVisualSetupTransform(offsets.RightPinky, KeyboardFinger.RightPinky, false);
        }

        private void SetupFingertipVisualSetupTransform(FingertipOffsetVisualSetupTransform tr, KeyboardFinger finger, bool isUnified) {
            if (tr == null) return;

            tr.OnEnterVisualSetup = (transform) => {
                activeVisualSetupFinger = finger;
                int fIdx = (int)finger;
                if (fIdx >= 0 && fIdx < fingerTweens.Length) {
                    fingerTweens[fIdx]?.Kill(false);
                }
                if (Character?.OverlappingAnimations != null) {
                    var layerId = GetFingerLayerId(finger);
                    var layerData = Array.Find(Character.OverlappingAnimations, a => a.CustomLayerID == layerId);
                    if (layerData != null) {
                        int animancerLayerIdx = GetAnimancerLayerIndex(layerData);
                        if (animancerLayerIdx >= 0) {
                            SetAnimancerLayerWeight(animancerLayerIdx, 1.0f);
                        }
                    }
                }
            };

            tr.OnMirrorToOppositeHand = (sourceTr) => {
                if (FingerMovementDefinitions?.FingertipOffsets == null) return;
                var offsets = FingerMovementDefinitions.FingertipOffsets;
                var oppositeFinger = GetOppositeHandFinger(finger);
                var oppositeTr = offsets.GetFingerTransform(oppositeFinger);
                if (oppositeTr != null) {
                    oppositeTr.Position = new Vector3(-sourceTr.Position.x, sourceTr.Position.y, sourceTr.Position.z);
                    offsets.SetDataInput(oppositeFinger.ToString(), oppositeTr, true);
                    offsets.BroadcastDataInput(oppositeFinger.ToString());
                    offsets.RequestUiRefresh();
                    Context.Service?.Toast(
                        Warudo.Core.Server.ToastSeverity.Success,
                        "APPLY_TO_OPPOSITE_HAND_FINGER".Localized(),
                        $"Mirrored {finger} to {oppositeFinger}."
                    );
                }
            };

            tr.OnCreateAnchor = (transform, anchor) => {
                var distalBone = GetDistalBoneForFinger(finger);

                anchor.Attachable.Parent = Character;
                anchor.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.HumanBodyBone;
                anchor.Attachable.AttachToBone = distalBone;

                anchor.Transform.Position = transform.Position;
                anchor.Transform.Rotation = Vector3.zero;
                anchor.Broadcast();
            };

            tr.OnApplyAnchor = (transform, anchor) => {
                transform.Position = anchor.Transform.Position;
                if (isUnified) {
                    FingerMovementDefinitions?.FingertipOffsets?.SyncUnifiedToIndividual();
                }
            };

            tr.OnExitVisualSetup = (isApplied) => {
                activeVisualSetupFinger = null;
                int fIdx = (int)finger;
                if (fIdx >= 0 && fIdx < fingerTweens.Length) {
                    fingerTweens[fIdx]?.Kill(false);
                }
                ApplyFingerLayerWeight(finger, fIdx, 0.0f);

                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };
        }

        public Vector3 GetCurledFingerTipContactVector(Animator anim, KeyboardFinger finger) {
            if (anim == null) return Vector3.zero;
            bool isLeft = finger.IsLeftHand();
            var handBone = isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
            var handTransform = anim.GetBoneTransform(handBone);
            Vector3 tipWorld = GetFingerTipWorldPosition(anim, finger);
            if (handTransform != null) {
                var qInter = isLeft 
                    ? Quaternion.Euler(HAND_IK_INTERMEDIATE_ROTATION_LEFT) 
                    : Quaternion.Euler(HAND_IK_INTERMEDIATE_ROTATION_RIGHT);
                Vector3 tipInHandBoneLocal = handTransform.InverseTransformPoint(tipWorld);
                return qInter * tipInHandBoneLocal;
            }
            return Vector3.zero;
        }

        public async void AutoGenerateKeyBindingsFromFloatingKeys() {
            if (Character == null || !Character.Active || Character.GameObject == null) {
                Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Warning, "AUTO_GENERATE_KEY_BINDINGS".Localized(), "AUTO_GENERATE_NO_CHARACTER".Localized());
                return;
            }

            var animator = Character.Animator;
            if (animator == null) {
                Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Error, "AUTO_GENERATE_KEY_BINDINGS".Localized(), "AUTO_SETUP_NO_ANIMATOR".Localized());
                return;
            }

            if (VirtualKeysCollection?.Keys == null || VirtualKeysCollection.Keys.Length == 0) {
                Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Warning, "AUTO_GENERATE_KEY_BINDINGS".Localized(), "AUTO_GENERATE_NO_KEYS".Localized());
                return;
            }

            EnsureAnchors();

            var rootAnchor = GetRootAnchor(false);
            Transform rootTr = rootAnchor?.GameObject != null ? rootAnchor.GameObject.transform : null;

            // Ensure finger animation layers are configured on Character
            SynchronizeAllFingerAnimations();

            // Resting wrist transforms in Rig Space
            Vector3 leftWrist0Rig = LeftHandRestPosition;
            Quaternion leftWristRot0Rig = Quaternion.Euler(LeftHandRestRotation);
            Vector3 rightWrist0Rig = RightHandRestPosition;
            Quaternion rightWristRot0Rig = Quaternion.Euler(RightHandRestRotation);

            // Resting hand anchor world transforms
            Vector3 leftHandPos0World = rootTr != null ? rootTr.TransformPoint(leftWrist0Rig) : leftWrist0Rig;
            Quaternion leftHandRot0World = rootTr != null ? rootTr.rotation * leftWristRot0Rig : leftWristRot0Rig;
            Vector3 rightHandPos0World = rootTr != null ? rootTr.TransformPoint(rightWrist0Rig) : rightWrist0Rig;
            Quaternion rightHandRot0World = rootTr != null ? rootTr.rotation * rightWristRot0Rig : rightWristRot0Rig;

            // Sample Elbows in World Space
            var leftElbowBone = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var rightElbowBone = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Vector3 leftElbowWorld = leftElbowBone != null ? leftElbowBone.position : (leftHandPos0World - (leftHandRot0World * Vector3.forward) * 0.25f);
            Vector3 rightElbowWorld = rightElbowBone != null ? rightElbowBone.position : (rightHandPos0World - (rightHandRot0World * Vector3.forward) * 0.25f);

            // Pre-sample curled fingertip contact vectors in Hand Anchor Space (identical to individual key AutoAlignKeyBinding)
            Vector3[] tipCurledLocal = new Vector3[10];
            var animancer = Character.Animancer;

            // 1. Snapshot Animancer layer states and activate all 10 finger layers
            float[] oldWeights = new float[10];
            float[] oldTimes = new float[10];
            bool[] wasPlaying = new bool[10];
            Animancer.AnimancerState[] states = new Animancer.AnimancerState[10];

            for (int i = 0; i < fingerTweens.Length; i++) {
                fingerTweens[i]?.Kill(false);
            }
            isBatchAutoGenerating = true;

            if (animancer != null && Character.OverlappingAnimations != null) {
                for (int f = 0; f < 10; f++) {
                    KeyboardFinger finger = (KeyboardFinger)f;
                    var layerId = GetFingerLayerId(finger);
                    var layerData = Array.Find(Character.OverlappingAnimations, a => a.CustomLayerID == layerId);
                    if (layerData != null) {
                        int layerIdx = GetAnimancerLayerIndex(layerData);
                        if (layerIdx >= 0 && layerIdx < animancer.Layers.Count) {
                            var layer = animancer.Layers[layerIdx];
                            if (layer != null) {
                                oldWeights[f] = layer.Weight;
                                Animancer.AnimancerState state = layer.CurrentState;
                                if (state == null) {
                                    foreach (Animancer.AnimancerState s in layer) {
                                        state = s;
                                        break;
                                    }
                                }
                                if (state == null && layerData.GetAnimationClip() != null) {
                                    state = layer.GetOrCreateState(layerData.GetAnimationClip());
                                }
                                if (state != null) {
                                    states[f] = state;
                                    oldTimes[f] = state.Time;
                                    wasPlaying[f] = state.IsPlaying;

                                    state.NormalizedTime = 1f;
                                    state.IsPlaying = false;
                                }
                                layer.SetWeight(1f);
                            }
                            SetAnimancerComponentLayerWeight(Character?.CloneAnimancer, layerIdx, 1f);
                        }
                    }
                }
            }

            // Wait 150ms (~9 frames) for Warudo's PlayableGraph, Animancer, and Animator to evaluate the curled finger bones
            await UniTask.Delay(150);

            // 2. Sample curled fingertip contact vectors in Hand Anchor space (matching individual key AutoAlignKeyBinding)
            for (int f = 0; f < 10; f++) {
                KeyboardFinger finger = (KeyboardFinger)f;
                tipCurledLocal[f] = GetCurledFingerTipContactVector(animator, finger);
                if (tipCurledLocal[f] == Vector3.zero) {
                    bool isLeft = finger.IsLeftHand();
                    Vector3 handPos0 = isLeft ? leftHandPos0World : rightHandPos0World;
                    Quaternion handRot0 = isLeft ? leftHandRot0World : rightHandRot0World;
                    Vector3 tipWorld = GetFingerTipWorldPosition(animator, finger);
                    tipCurledLocal[f] = Quaternion.Inverse(handRot0) * (tipWorld - handPos0);
                }
            }

            // 3. Restore original Animancer layer states and evaluate back to resting pose
            isBatchAutoGenerating = false;
            if (animancer != null && Character.OverlappingAnimations != null) {
                for (int f = 0; f < 10; f++) {
                    KeyboardFinger finger = (KeyboardFinger)f;
                    var layerId = GetFingerLayerId(finger);
                    var layerData = Array.Find(Character.OverlappingAnimations, a => a.CustomLayerID == layerId);
                    if (layerData != null) {
                        int layerIdx = GetAnimancerLayerIndex(layerData);
                        if (layerIdx >= 0 && layerIdx < animancer.Layers.Count) {
                            var layer = animancer.Layers[layerIdx];
                            if (layer != null) {
                                layer.SetWeight(oldWeights[f]);
                                if (states[f] != null) {
                                    states[f].Time = oldTimes[f];
                                    states[f].IsPlaying = wasPlaying[f];
                                }
                            }
                            SetAnimancerComponentLayerWeight(Character?.CloneAnimancer, layerIdx, oldWeights[f]);
                        }
                    }
                }
            }
            ResetFingerWeights();
            await UniTask.NextFrame();

            var list = new List<KeyboardKeyBinding>();

            bool tiltMode = (BindingGenerator?.GenerationMode ?? KeyGenerationMode.TiltToKeySurface) == KeyGenerationMode.TiltToKeySurface;

            Vector3 leftBedNormal = GetHandAlignmentReferenceBedNormalWorldIdentity(true, VirtualKeyAnchor.LeftHand);
            Vector3 rightBedNormal = GetHandAlignmentReferenceBedNormalWorldIdentity(false, VirtualKeyAnchor.RightHand);

            foreach (var k in VirtualKeysCollection.Keys) {
                if (k == null) continue;

                // 1. Get Key position and orientation in World Space (Identity cluster frame)
                if (!TryGetKeyCenterAndNormalIdentity(k, out Vector3 keyCenterWorld, out Vector3 keyNormalWorld)) continue;

                KeyboardFinger finger = k.AssignedFinger;
                int fingerIdx = (int)finger;
                bool isLeftHand = finger.IsLeftHand();

                Vector3 restPosRig = isLeftHand ? leftWrist0Rig : rightWrist0Rig;
                Vector3 restRotRig = isLeftHand ? LeftHandRestRotation : RightHandRestRotation;
                Vector3 handPos0 = isLeftHand ? leftHandPos0World : rightHandPos0World;
                Quaternion handRot0 = isLeftHand ? leftHandRot0World : rightHandRot0World;
                Vector3 elbowPosWorld = isLeftHand ? leftElbowWorld : rightElbowWorld;
                Vector3 dLocal = tipCurledLocal[fingerIdx];
                Vector3 handScale = isLeftHand ? GetLeftHandMovementScale() : GetRightHandMovementScale();
                Vector3 bedNormalWorld = isLeftHand ? leftBedNormal : rightBedNormal;

                // 2. Execute alignment math
                KeyboardKeyBinding.SolveKeyAlignment(
                    handPos0,
                    handRot0,
                    handRot0,
                    keyCenterWorld,
                    keyNormalWorld,
                    elbowPosWorld,
                    dLocal,
                    rootTr,
                    out Vector3 solvedPosRig,
                    out Quaternion solvedRotRig,
                    tiltMode,
                    bedNormalWorld
                );

                // 3. Convert solved wrist pose into identity cluster frame
                var clusterTransform = GetKeyClusterTransform(k.AttachmentAnchor);
                Vector3 deltaPos = Quaternion.Inverse(clusterTransform.BaseRotRig) * (solvedPosRig - clusterTransform.BasePivotRig);
                Quaternion deltaRotQuat = Quaternion.Inverse(clusterTransform.BaseRotRig) * solvedRotRig;
                Vector3 deltaRot = Helper.NormalizeRotationAngles(deltaRotQuat.eulerAngles);

                var binding = StructuredData.Create<KeyboardKeyBinding>(b => {
                    b.Parent = this;
                    b.Key = k.Key;
                    b.Finger = finger;
                    b.DeltaPosition = deltaPos;
                    b.DeltaRotation = deltaRot;
                    b.EnableHandMovement = true;
                    b.SetDataInput(nameof(b.KeyInput), b.KeyInput, true);
                    b.SetDataInput(nameof(b.Finger), b.Finger, true);
                    b.SetDataInput(nameof(b.EnableHandMovement), true, true);
                    if (b.Transform != null) {
                        b.Transform.Position = deltaPos;
                        b.Transform.Rotation = deltaRot;
                        b.Transform.SetDataInput(nameof(b.Transform.Position), deltaPos, true);
                        b.Transform.SetDataInput(nameof(b.Transform.Rotation), deltaRot, true);
                    }
                    b.SetupTransformCallbacks();
                });
                list.Add(binding);
            }

            MovementDefinitions = list.ToArray();
            FingerMovementDefinitions?.BroadcastDataInput(nameof(FingerMovementDefinitions.MovementDefinitions));
            BroadcastDataInput(nameof(FingerMovementDefinitions));
            RequestUiRefresh();

            Context.Service?.Toast(
                Warudo.Core.Server.ToastSeverity.Success,
                "AUTO_GENERATE_KEY_BINDINGS".Localized(),
                string.Format("AUTO_GENERATE_SUCCESS".Localized(), KeyBindings.Length)
            );

            Debug.Log($"[FS Keyboard Receiver] Auto-generated {KeyBindings.Length} key bindings matching visual setup alignment. Character: '{Character.Name}'.");
        }

        #endregion
    }
}
