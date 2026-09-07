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
            if (Character.OverlappingAnimations != null) {
                for (int f = 0; f < 10; f++) {
                    KeyboardFinger finger = (KeyboardFinger)f;
                    ApplyFingerLayerWeight(finger, f, targetFingerWeights[f]);
                }
            }
        }

        int GetAnimancerLayerIndex(CharacterAsset.OverlappingAnimationData layerData) {
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

        private float GetAvatarFingerLength(Animator anim, HumanBodyBones proximalBone, HumanBodyBones interBone, HumanBodyBones distalBone) {
            float len = 0f;
            var p = anim.GetBoneTransform(proximalBone);
            var i = anim.GetBoneTransform(interBone);
            var d = anim.GetBoneTransform(distalBone);
            if (p != null && i != null) len += Vector3.Distance(p.position, i.position);
            if (i != null && d != null) len += Vector3.Distance(i.position, d.position);
            if (d != null && d.childCount > 0) len += Vector3.Distance(d.position, d.GetChild(0).position);
            else if (d != null && i != null) len += Vector3.Distance(i.position, d.position) * 0.75f;
            return len > 0.02f ? len : 0.085f;
        }

        public void AutoGenerateKeyBindingsFromFloatingKeys() {
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

            var leftWristBone = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightWristBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (leftWristBone == null || rightWristBone == null) {
                Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Error, "AUTO_GENERATE_KEY_BINDINGS".Localized(), "AUTO_SETUP_NO_HAND_BONES".Localized());
                return;
            }

            // World to Rig matrix
            Matrix4x4 worldToRig;
            if (RootAnchor != null && RootAnchor.GameObject != null) {
                worldToRig = RootAnchor.GameObject.transform.worldToLocalMatrix;
            } else {
                var charTransform = Character.GameObject.transform;
                Vector3 rigWorldPos = charTransform.TransformPoint(KeyboardRootPosition);
                Quaternion rigWorldRot = charTransform.rotation * Quaternion.Euler(KeyboardRootRotation);
                worldToRig = Matrix4x4.TRS(rigWorldPos, rigWorldRot, Vector3.one).inverse;
            }

            // Sample Elbows in Rig Space
            var leftElbowBone = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var rightElbowBone = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Vector3 leftElbowRig = leftElbowBone != null ? worldToRig.MultiplyPoint3x4(leftElbowBone.position) : (LeftHandRestPosition - new Vector3(0.05f, -0.05f, 0.25f));
            Vector3 rightElbowRig = rightElbowBone != null ? worldToRig.MultiplyPoint3x4(rightElbowBone.position) : (RightHandRestPosition - new Vector3(-0.05f, -0.05f, 0.25f));

            // Measure finger proportions from avatar bones
            float lIndexLen = GetAvatarFingerLength(animator, HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexDistal);
            float lMiddleLen = GetAvatarFingerLength(animator, HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleDistal);
            float lRingLen = GetAvatarFingerLength(animator, HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingDistal);
            float lPinkyLen = GetAvatarFingerLength(animator, HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleDistal);
            float lScale = lIndexLen / 0.085f;

            float rIndexLen = GetAvatarFingerLength(animator, HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal);
            float rMiddleLen = GetAvatarFingerLength(animator, HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal);
            float rRingLen = GetAvatarFingerLength(animator, HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal);
            float rPinkyLen = GetAvatarFingerLength(animator, HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal);
            float rScale = rIndexLen / 0.085f;

            // Fingertip offsets relative to wrist in palm forward frame
            Vector3 GetNeutralTipOffset(KeyboardFinger f) {
                switch (f) {
                    case KeyboardFinger.LeftThumb:  return new Vector3(0.035f * lScale, -0.015f * lScale, 0.045f * lScale);
                    case KeyboardFinger.LeftIndex:  return new Vector3(0.015f * lScale, -0.020f * lScale, lIndexLen);
                    case KeyboardFinger.LeftMiddle: return new Vector3(0.0f, -0.020f * lScale, lMiddleLen);
                    case KeyboardFinger.LeftRing:   return new Vector3(-0.015f * lScale, -0.020f * lScale, lRingLen);
                    case KeyboardFinger.LeftPinky:  return new Vector3(-0.030f * lScale, -0.020f * lScale, lPinkyLen);

                    case KeyboardFinger.RightThumb:  return new Vector3(-0.035f * rScale, -0.015f * rScale, 0.045f * rScale);
                    case KeyboardFinger.RightIndex:  return new Vector3(-0.015f * rScale, -0.020f * rScale, rIndexLen);
                    case KeyboardFinger.RightMiddle: return new Vector3(0.0f, -0.020f * rScale, rMiddleLen);
                    case KeyboardFinger.RightRing:   return new Vector3(0.015f * rScale, -0.020f * rScale, rRingLen);
                    case KeyboardFinger.RightPinky:  return new Vector3(0.030f * rScale, -0.020f * rScale, rPinkyLen);
                    default: return Vector3.forward * 0.08f;
                }
            }

            Vector3 GetPressedTipOffset(KeyboardFinger f) {
                float s = f.IsLeftHand() ? lScale : rScale;
                return GetNeutralTipOffset(f) + new Vector3(0f, -0.014f * s, -0.006f * s);
            }

            Vector3 leftWrist0 = LeftHandRestPosition;
            Quaternion leftWristRot0 = Quaternion.Euler(LeftHandRestRotation);
            Vector3 rightWrist0 = RightHandRestPosition;
            Quaternion rightWristRot0 = Quaternion.Euler(RightHandRestRotation);

            var list = new List<KeyboardKeyBinding>();

            foreach (var k in VirtualKeysCollection.Keys) {
                if (k == null) continue;

                // Compute key position and orientation in Rig Space
                Vector3 groupPos;
                Quaternion groupRot;
                if (k.AttachmentAnchor == VirtualKeyAnchor.LeftHand) {
                    var basePos = LeftHandRestPosition;
                    var baseRot = Quaternion.Euler(LeftHandRestRotation);
                    var pos = LeftKeysetTransform?.Position ?? Vector3.zero;
                    var rot = LeftKeysetTransform?.Rotation ?? Vector3.zero;
                    groupPos = basePos + baseRot * pos;
                    groupRot = baseRot * Quaternion.Euler(rot);
                } else if (k.AttachmentAnchor == VirtualKeyAnchor.RightHand) {
                    var basePos = RightHandRestPosition;
                    var baseRot = Quaternion.Euler(RightHandRestRotation);
                    var pos = RightKeysetTransform?.Position ?? Vector3.zero;
                    var rot = RightKeysetTransform?.Rotation ?? Vector3.zero;
                    groupPos = basePos + baseRot * pos;
                    groupRot = baseRot * Quaternion.Euler(rot);
                } else {
                    var pos = RootKeysetTransform?.Position ?? Vector3.zero;
                    var rot = RootKeysetTransform?.Rotation ?? Vector3.zero;
                    groupPos = pos;
                    groupRot = Quaternion.Euler(rot);
                }

                Vector3 keyPosRig = groupPos + groupRot * k.Transform.Position;
                Quaternion keyRotRig = groupRot * Quaternion.Euler(k.Transform.Rotation);

                KeyboardFinger finger = k.AssignedFinger;
                bool isLeftHand = finger.IsLeftHand();

                Vector3 wrist0 = isLeftHand ? leftWrist0 : rightWrist0;
                Quaternion wristRot0 = isLeftHand ? leftWristRot0 : rightWristRot0;
                Vector3 elbowRig = isLeftHand ? leftElbowRig : rightElbowRig;
                Vector3 tipPressed = GetPressedTipOffset(finger);

                // 1. Forearm yaw angle around elbow pivot
                Vector3 vReach = keyPosRig - elbowRig;
                Vector3 arm0 = wrist0 - elbowRig;
                float reachAngle = Mathf.Atan2(vReach.x, vReach.z) * Mathf.Rad2Deg;
                float neutralAngle = Mathf.Atan2(arm0.x, arm0.z) * Mathf.Rad2Deg;
                float deltaYaw = Mathf.DeltaAngle(neutralAngle, reachAngle);

                // 2. Hand pitch adjustment to match key tilt & row height
                float keyPitch = keyRotRig.eulerAngles.x;
                if (keyPitch > 180f) keyPitch -= 360f;

                Vector3 neutralTipRig = wrist0 + wristRot0 * GetNeutralTipOffset(finger);
                float deltaY = keyPosRig.y - neutralTipRig.y;
                float deltaPitch = keyPitch * 0.5f + Mathf.Clamp(deltaY / 0.08f, -1f, 1f) * 12f;

                Quaternion targetHandRot = Quaternion.Euler(deltaPitch, deltaYaw * 0.7f, 0f) * wristRot0;

                // 3. Wrist target position so pressed fingertip hits the key center
                Vector3 targetWristPos = keyPosRig - (targetHandRot * tipPressed);

                // 4. Displacements
                Vector3 deltaPos = targetWristPos - wrist0;

                Vector3 handScale = isLeftHand ? GetLeftHandMovementScale() : GetRightHandMovementScale();
                if (handScale.x != 0f) deltaPos.x /= handScale.x;
                if (handScale.y != 0f) deltaPos.y /= handScale.y;
                if (handScale.z != 0f) deltaPos.z /= handScale.z;

                Quaternion deltaRotQuat = targetHandRot * Quaternion.Inverse(wristRot0);
                Vector3 deltaRot = deltaRotQuat.eulerAngles;
                if (deltaRot.x > 180f) deltaRot.x -= 360f;
                if (deltaRot.y > 180f) deltaRot.y -= 360f;
                if (deltaRot.z > 180f) deltaRot.z -= 360f;

                // 5. Ergonomic neutral check
                bool enableHandMove = true;
                if (deltaPos.sqrMagnitude < 0.000064f && Mathf.Abs(deltaYaw) < 3f) {
                    deltaPos = Vector3.zero;
                    deltaRot = Vector3.zero;
                    enableHandMove = false;
                }

                var binding = StructuredData.Create<KeyboardKeyBinding>(b => {
                    b.Parent = this;
                    b.Key = k.Key;
                    b.Finger = finger;
                    b.DeltaPosition = deltaPos;
                    b.DeltaRotation = deltaRot;
                    b.EnableHandMovement = enableHandMove;
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
        }

        #endregion
    }
}
