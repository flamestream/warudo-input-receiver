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
    public enum KeyboardLayoutPreset {
        [Label("LAYOUT_PRESET_QWERTY_FULL")]
        StandardQWERTY,
        [Label("LAYOUT_PRESET_TKL")]
        StandardTKL,
        [Label("LAYOUT_PRESET_60PERCENT")]
        Standard60Percent,
        [Label("LAYOUT_PRESET_SPLIT_ERGO")]
        SplitErgonomic,
        [Label("LAYOUT_PRESET_KEYCHRON_Q11")]
        KeychronQ11
    }

    public partial class KeyboardReceiverAsset : ReceiverAsset {

        [Section("FINGER_MOVEMENT")]

        [DataInput(300)]
        [Label("FINGER_ANIMATIONS")]
        public KeyboardFingerAnimations FingerAnimations;

        [DataInput(310)]
        [Label("KEY_TRANSITIONS")]
        public KeyboardKeyTransitions Transitions;

        [DataInput(320)]
        [Label("KEY_BINDINGS_LAYOUT")]
        public KeyboardKeyBindingCollection KeyBindingsCollection;

        public KeyboardKeyBinding[] KeyBindings {
            get => KeyBindingsCollection?.KeyBindings ?? new KeyboardKeyBinding[0];
            set {
                if (KeyBindingsCollection == null) {
                    KeyBindingsCollection = StructuredData.Create<KeyboardKeyBindingCollection>();
                    KeyBindingsCollection.Parent = this;
                }
                KeyBindingsCollection.KeyBindings = value;
                KeyBindingsCollection.SetDataInput(nameof(KeyBindingsCollection.KeyBindings), value, true);
            }
        }

        public KeyboardLayoutPreset LayoutPreset {
            get => KeyBindingsCollection?.Preset ?? KeyboardLayoutPreset.StandardQWERTY;
            set {
                if (KeyBindingsCollection != null) {
                    KeyBindingsCollection.Preset = value;
                }
            }
        }

        public void TriggerApplyLayoutPreset() {
            ApplyPreset(LayoutPreset);
        }

        public Transition FingerPressTransition => Transitions?.FingerPress ?? Transitions?.Press;
        public Transition FingerReleaseTransition => Transitions?.FingerRelease ?? Transitions?.Release;
        public Transition HandMovementTransition => Transitions?.HandMovement;
        public Transition HandReturnTransition => Transitions?.HandReturn;

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

            if (Transitions == null) {
                Transitions = StructuredData.Create<KeyboardKeyTransitions>();
                Transitions.CollapsedSelf = true;
            }
            Transitions.Parent = this;

            if (KeyBindingsCollection == null) {
                KeyBindingsCollection = StructuredData.Create<KeyboardKeyBindingCollection>();
                KeyBindingsCollection.CollapsedSelf = true;
            }
            KeyBindingsCollection.Parent = this;

            Watch(nameof(FingerAnimations), delegate {
                if (FingerAnimations != null) {
                    FingerAnimations.Parent = this;
                }
                SynchronizeAllFingerAnimations();
            });

            Watch(nameof(Transitions), delegate {
                if (Transitions != null) {
                    Transitions.Parent = this;
                }
            });

            Watch(nameof(KeyBindingsCollection), delegate {
                if (KeyBindingsCollection != null) {
                    KeyBindingsCollection.Parent = this;
                }
                RefreshKeyBindingsParent();
            });

            if (KeyBindings == null || KeyBindings.Length == 0) {
                ApplyPreset(KeyboardLayoutPreset.StandardQWERTY, false);
            }
            RefreshKeyBindingsParent();
        }

        public void RefreshKeyBindingsParent() {
            if (KeyBindings == null) return;
            for (int i = 0; i < KeyBindings.Length; i++) {
                if (KeyBindings[i] != null) {
                    KeyBindings[i].Parent = this;
                    KeyBindings[i].SetupTransformCallbacks();
                }
            }
        }

        void OnDestroyFingerMovement() {
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
                    Vector3 leftScale = GetLeftHandMovementScale();
                    Vector3 deltaPos = Vector3.Scale(activeLeftKey.DeltaPosition, leftScale);
                    targetLeftPos = restLeftPos + deltaPos;
                    targetLeftRot = restLeftRot + activeLeftKey.DeltaRotation;
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
                    Vector3 rightScale = GetRightHandMovementScale();
                    Vector3 deltaPos = Vector3.Scale(activeRightKey.DeltaPosition, rightScale);
                    targetRightPos = restRightPos + deltaPos;
                    targetRightRot = restRightRot + activeRightKey.DeltaRotation;
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
                    ApplyFingerLayerWeight(finger, f, targetFingerWeights[f]);
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

        public void ApplyPreset(KeyboardLayoutPreset preset, bool showToast = true) {
            var list = new List<KeyboardKeyBinding>();

            // Helper to add key definition
            void AddKey(VKCode key, KeyboardFinger finger, float deltaX, float deltaZ, float rotY = 0f, bool enableHandMove = true) {
                var binding = StructuredData.Create<KeyboardKeyBinding>(b => {
                    b.Parent = this;
                    b.Key = key;
                    b.Finger = finger;
                    b.DeltaPosition = new Vector3(deltaX, 0f, deltaZ);
                    b.DeltaRotation = new Vector3(0f, rotY, 0f);
                    b.EnableHandMovement = enableHandMove;
                    b.SetupTransformCallbacks();
                });
                list.Add(binding);
            }

            // Left Hand Rest (ASDF): A=(0,0), S=(0,0), D=(0,0), F=(0,0)
            // Right Hand Rest (JKL;): J=(0,0), K=(0,0), L=(0,0), ;=(0,0)

            // --- LEFT HAND ---
            // Left Pinky
            AddKey(VKCode.ESCAPE, KeyboardFinger.LeftPinky, -0.028f, 0.057f);
            AddKey(VKCode.OEM_3, KeyboardFinger.LeftPinky, -0.010f, 0.038f); // ~
            AddKey(VKCode.KEY_1, KeyboardFinger.LeftPinky, 0.0f, 0.038f);
            AddKey(VKCode.TAB, KeyboardFinger.LeftPinky, -0.028f, 0.019f);
            AddKey(VKCode.Q, KeyboardFinger.LeftPinky, -0.005f, 0.019f);
            AddKey(VKCode.CAPITAL, KeyboardFinger.LeftPinky, -0.024f, 0.0f);
            AddKey(VKCode.A, KeyboardFinger.LeftPinky, 0.0f, 0.0f);
            AddKey(VKCode.LSHIFT, KeyboardFinger.LeftPinky, -0.028f, -0.019f);
            AddKey(VKCode.Z, KeyboardFinger.LeftPinky, 0.005f, -0.019f);
            AddKey(VKCode.LCONTROL, KeyboardFinger.LeftPinky, -0.028f, -0.038f);

            // Left Ring
            AddKey(VKCode.KEY_2, KeyboardFinger.LeftRing, -0.005f, 0.038f);
            AddKey(VKCode.W, KeyboardFinger.LeftRing, -0.005f, 0.019f);
            AddKey(VKCode.S, KeyboardFinger.LeftRing, 0.0f, 0.0f);
            AddKey(VKCode.X, KeyboardFinger.LeftRing, 0.005f, -0.019f);

            // Left Middle
            AddKey(VKCode.KEY_3, KeyboardFinger.LeftMiddle, -0.005f, 0.038f);
            AddKey(VKCode.E, KeyboardFinger.LeftMiddle, -0.005f, 0.019f);
            AddKey(VKCode.D, KeyboardFinger.LeftMiddle, 0.0f, 0.0f);
            AddKey(VKCode.C, KeyboardFinger.LeftMiddle, 0.005f, -0.019f);

            // Left Index
            AddKey(VKCode.KEY_4, KeyboardFinger.LeftIndex, -0.010f, 0.038f);
            AddKey(VKCode.KEY_5, KeyboardFinger.LeftIndex, 0.009f, 0.038f);
            AddKey(VKCode.R, KeyboardFinger.LeftIndex, -0.005f, 0.019f);
            AddKey(VKCode.T, KeyboardFinger.LeftIndex, 0.014f, 0.019f);
            AddKey(VKCode.F, KeyboardFinger.LeftIndex, 0.0f, 0.0f);
            AddKey(VKCode.G, KeyboardFinger.LeftIndex, 0.019f, 0.0f);
            AddKey(VKCode.V, KeyboardFinger.LeftIndex, 0.005f, -0.019f);
            AddKey(VKCode.B, KeyboardFinger.LeftIndex, 0.024f, -0.019f);

            // Left Thumb
            AddKey(VKCode.LMENU, KeyboardFinger.LeftThumb, -0.024f, -0.038f); // Alt
            AddKey(VKCode.SPACE, KeyboardFinger.LeftThumb, 0.0f, -0.038f);

            // --- RIGHT HAND ---
            // Right Thumb
            AddKey(VKCode.RMENU, KeyboardFinger.RightThumb, 0.024f, -0.038f); // Alt

            // Right Index
            AddKey(VKCode.KEY_6, KeyboardFinger.RightIndex, -0.029f, 0.038f);
            AddKey(VKCode.KEY_7, KeyboardFinger.RightIndex, -0.010f, 0.038f);
            AddKey(VKCode.Y, KeyboardFinger.RightIndex, -0.024f, 0.019f);
            AddKey(VKCode.U, KeyboardFinger.RightIndex, -0.005f, 0.019f);
            AddKey(VKCode.H, KeyboardFinger.RightIndex, -0.019f, 0.0f);
            AddKey(VKCode.J, KeyboardFinger.RightIndex, 0.0f, 0.0f);
            AddKey(VKCode.N, KeyboardFinger.RightIndex, -0.014f, -0.019f);
            AddKey(VKCode.M, KeyboardFinger.RightIndex, 0.005f, -0.019f);

            // Right Middle
            AddKey(VKCode.KEY_8, KeyboardFinger.RightMiddle, -0.010f, 0.038f);
            AddKey(VKCode.I, KeyboardFinger.RightMiddle, -0.005f, 0.019f);
            AddKey(VKCode.K, KeyboardFinger.RightMiddle, 0.0f, 0.0f);
            AddKey(VKCode.OEM_COMMA, KeyboardFinger.RightMiddle, 0.005f, -0.019f); // ,

            // Right Ring
            AddKey(VKCode.KEY_9, KeyboardFinger.RightRing, -0.010f, 0.038f);
            AddKey(VKCode.O, KeyboardFinger.RightRing, -0.005f, 0.019f);
            AddKey(VKCode.L, KeyboardFinger.RightRing, 0.0f, 0.0f);
            AddKey(VKCode.OEM_PERIOD, KeyboardFinger.RightRing, 0.005f, -0.019f); // .

            // Right Pinky
            AddKey(VKCode.KEY_0, KeyboardFinger.RightPinky, -0.010f, 0.038f);
            AddKey(VKCode.OEM_MINUS, KeyboardFinger.RightPinky, 0.009f, 0.038f); // -
            AddKey(VKCode.OEM_PLUS, KeyboardFinger.RightPinky, 0.028f, 0.038f); // =
            AddKey(VKCode.BACK, KeyboardFinger.RightPinky, 0.052f, 0.038f); // Backspace
            AddKey(VKCode.P, KeyboardFinger.RightPinky, -0.005f, 0.019f);
            AddKey(VKCode.OEM_4, KeyboardFinger.RightPinky, 0.014f, 0.019f); // [
            AddKey(VKCode.OEM_6, KeyboardFinger.RightPinky, 0.033f, 0.019f); // ]
            AddKey(VKCode.OEM_5, KeyboardFinger.RightPinky, 0.052f, 0.019f); // \
            AddKey(VKCode.OEM_1, KeyboardFinger.RightPinky, 0.0f, 0.0f); // ;
            AddKey(VKCode.OEM_7, KeyboardFinger.RightPinky, 0.019f, 0.0f); // '
            AddKey(VKCode.RETURN, KeyboardFinger.RightPinky, 0.043f, 0.0f); // Enter
            AddKey(VKCode.OEM_2, KeyboardFinger.RightPinky, 0.005f, -0.019f); // /
            AddKey(VKCode.RSHIFT, KeyboardFinger.RightPinky, 0.038f, -0.019f);
            AddKey(VKCode.RCONTROL, KeyboardFinger.RightPinky, 0.048f, -0.038f);

            // Function Keys (for Full / TKL / Keychron Q11 presets)
            if (preset == KeyboardLayoutPreset.StandardQWERTY || preset == KeyboardLayoutPreset.StandardTKL || preset == KeyboardLayoutPreset.KeychronQ11) {
                AddKey(VKCode.F1, KeyboardFinger.LeftPinky, -0.019f, 0.057f);
                AddKey(VKCode.F2, KeyboardFinger.LeftRing, -0.005f, 0.057f);
                AddKey(VKCode.F3, KeyboardFinger.LeftMiddle, -0.005f, 0.057f);
                AddKey(VKCode.F4, KeyboardFinger.LeftIndex, -0.010f, 0.057f);
                AddKey(VKCode.F5, KeyboardFinger.LeftIndex, 0.014f, 0.057f);
                AddKey(VKCode.F6, KeyboardFinger.LeftIndex, 0.033f, 0.057f);
                AddKey(VKCode.F7, KeyboardFinger.RightIndex, -0.024f, 0.057f);
                AddKey(VKCode.F8, KeyboardFinger.RightIndex, -0.005f, 0.057f);
                AddKey(VKCode.F9, KeyboardFinger.RightMiddle, -0.005f, 0.057f);
                AddKey(VKCode.F10, KeyboardFinger.RightRing, -0.005f, 0.057f);
                AddKey(VKCode.F11, KeyboardFinger.RightPinky, -0.005f, 0.057f);
                AddKey(VKCode.F12, KeyboardFinger.RightPinky, 0.014f, 0.057f);

                // Navigation Cluster (Right Pinky)
                if (preset == KeyboardLayoutPreset.KeychronQ11) {
                    AddKey(VKCode.SNAPSHOT, KeyboardFinger.RightPinky, 0.033f, 0.057f); // PrtSc
                    AddKey(VKCode.DELETE, KeyboardFinger.RightPinky, 0.052f, 0.038f);
                    AddKey(VKCode.PRIOR, KeyboardFinger.RightPinky, 0.052f, 0.019f); // PageUp
                    AddKey(VKCode.NEXT, KeyboardFinger.RightPinky, 0.052f, 0.0f);   // PageDown
                    AddKey(VKCode.END, KeyboardFinger.RightPinky, 0.052f, -0.019f);  // End

                    // Keychron Q11 Left Macro Column (M1-M5 via F13-F17)
                    AddKey(VKCode.F13, KeyboardFinger.LeftPinky, -0.048f, 0.038f); // M1
                    AddKey(VKCode.F14, KeyboardFinger.LeftPinky, -0.048f, 0.019f); // M2
                    AddKey(VKCode.F15, KeyboardFinger.LeftPinky, -0.048f, 0.0f);   // M3
                    AddKey(VKCode.F16, KeyboardFinger.LeftPinky, -0.048f, -0.019f); // M4
                    AddKey(VKCode.F17, KeyboardFinger.LeftPinky, -0.048f, -0.038f); // M5

                    // Arrow Cluster (compact 75%)
                    AddKey(VKCode.UP, KeyboardFinger.RightPinky, 0.033f, -0.019f);
                    AddKey(VKCode.LEFT, KeyboardFinger.RightPinky, 0.014f, -0.038f);
                    AddKey(VKCode.DOWN, KeyboardFinger.RightPinky, 0.033f, -0.038f);
                    AddKey(VKCode.RIGHT, KeyboardFinger.RightPinky, 0.052f, -0.038f);
                } else {
                    AddKey(VKCode.INSERT, KeyboardFinger.RightPinky, 0.075f, 0.038f);
                    AddKey(VKCode.HOME, KeyboardFinger.RightPinky, 0.094f, 0.038f);
                    AddKey(VKCode.PRIOR, KeyboardFinger.RightPinky, 0.113f, 0.038f); // PageUp
                    AddKey(VKCode.DELETE, KeyboardFinger.RightPinky, 0.075f, 0.019f);
                    AddKey(VKCode.END, KeyboardFinger.RightPinky, 0.094f, 0.019f);
                    AddKey(VKCode.NEXT, KeyboardFinger.RightPinky, 0.113f, 0.019f); // PageDown
                    AddKey(VKCode.UP, KeyboardFinger.RightPinky, 0.094f, -0.019f);
                    AddKey(VKCode.LEFT, KeyboardFinger.RightPinky, 0.075f, -0.038f);
                    AddKey(VKCode.DOWN, KeyboardFinger.RightPinky, 0.094f, -0.038f);
                    AddKey(VKCode.RIGHT, KeyboardFinger.RightPinky, 0.113f, -0.038f);
                }
            }

            // Adjustments for Split presets
            if (preset == KeyboardLayoutPreset.KeychronQ11) {
                LeftHandRestPosition = new Vector3(-0.11f, 0.02f, 0f);
                LeftHandRestRotation = new Vector3(0f, -10f, 0f);
                RightHandRestPosition = new Vector3(0.11f, 0.02f, 0f);
                RightHandRestRotation = new Vector3(0f, 10f, 0f);

                foreach (var b in list) {
                    b.DeltaRotation = Vector3.zero;
                }
            } else if (preset == KeyboardLayoutPreset.SplitErgonomic) {
                LeftHandRestPosition = new Vector3(-0.09f, 0.02f, 0f);
                LeftHandRestRotation = new Vector3(0f, -12f, 0f);
                RightHandRestPosition = new Vector3(0.09f, 0.02f, 0f);
                RightHandRestRotation = new Vector3(0f, 12f, 0f);

                foreach (var b in list) {
                    b.DeltaRotation = Vector3.zero;
                }
            } else {
                LeftHandRestPosition = new Vector3(-0.07f, 0.02f, 0f);
                LeftHandRestRotation = Vector3.zero;
                RightHandRestPosition = new Vector3(0.07f, 0.02f, 0f);
                RightHandRestRotation = Vector3.zero;
            }

            if (LeftHandRestTransform != null && LeftHandRestTransform.Scale == Vector3.zero) {
                LeftHandRestTransform.Scale = Vector3.one;
                LeftHandRestTransform.SetDataInput(nameof(LeftHandRestTransform.Scale), Vector3.one, true);
            }
            if (RightHandRestTransform != null && RightHandRestTransform.Scale == Vector3.zero) {
                RightHandRestTransform.Scale = Vector3.one;
                RightHandRestTransform.SetDataInput(nameof(RightHandRestTransform.Scale), Vector3.one, true);
            }

            KeyBindings = list.ToArray();
            KeyBindingsCollection?.BroadcastDataInput(nameof(KeyBindingsCollection.KeyBindings));
            BroadcastDataInput(nameof(KeyBindingsCollection));
            BroadcastDataInput(nameof(HandRestSetup));

            if (showToast) {
                Context.Service?.Toast(
                    Warudo.Core.Server.ToastSeverity.Success,
                    Name,
                    $"Applied keyboard layout preset [{preset}] with {KeyBindings.Length} keys."
                );
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

        public Vector3 GetFingerTipWorldPosition(Animator anim, KeyboardFinger finger) {
            var distalBone = GetDistalBoneForFinger(finger);
            var distalTransform = anim.GetBoneTransform(distalBone);
            if (distalTransform != null) {
                if (distalTransform.childCount > 0) {
                    return distalTransform.GetChild(0).position;
                }
                var interBone = GetIntermediateBoneForFinger(finger);
                var interTransform = anim.GetBoneTransform(interBone);
                if (interTransform != null) {
                    var delta = distalTransform.position - interTransform.position;
                    if (delta.sqrMagnitude > 0.00001f) {
                        return distalTransform.position + delta.normalized * (delta.magnitude * 0.75f);
                    }
                }
                return distalTransform.position;
            }
            var handBone = finger.IsLeftHand() ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
            var handTransform = anim.GetBoneTransform(handBone);
            return handTransform != null ? handTransform.position : Vector3.zero;
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

            foreach (var k in VirtualKeysCollection.Keys) {
                if (k == null) continue;

                // 1. Get Key position and orientation in World Space
                if (!TryGetKeyCenterAndNormal(k, out Vector3 keyCenterWorld, out Vector3 keyNormalWorld)) continue;

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

                // 2. Execute the exact same alignment math as individual key setup
                KeyboardKeyBinding.SolveKeyAlignment(
                    handPos0,
                    handRot0,
                    keyCenterWorld,
                    keyNormalWorld,
                    elbowPosWorld,
                    dLocal,
                    rootTr,
                    out Vector3 solvedPosRig,
                    out Quaternion solvedRotRig
                );

                KeyboardKeyBinding.CalculateKeyDelta(
                    restPosRig,
                    restRotRig,
                    solvedPosRig,
                    solvedRotRig,
                    handScale,
                    out Vector3 deltaPos,
                    out Vector3 deltaRot
                );

                var binding = StructuredData.Create<KeyboardKeyBinding>(b => {
                    b.Parent = this;
                    b.Key = k.Key;
                    b.Finger = finger;
                    b.DeltaPosition = deltaPos;
                    b.DeltaRotation = deltaRot;
                    b.EnableHandMovement = true;
                    b.SetDataInput(nameof(b.Key), b.Key, true);
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

            KeyBindings = list.ToArray();
            KeyBindingsCollection?.BroadcastDataInput(nameof(KeyBindingsCollection.KeyBindings));
            BroadcastDataInput(nameof(KeyBindingsCollection));
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
