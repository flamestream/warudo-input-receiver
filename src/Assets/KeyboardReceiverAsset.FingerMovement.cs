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

        private Vector3 GetFingerTipMeshWorldPosition(Animator anim, SkinnedMeshRenderer[] smrs, KeyboardFinger finger, Vector3 fallbackBoneTip, bool isPressed, Matrix4x4 worldToRig, out bool usedMesh) {
            usedMesh = false;
            try {
                if (anim == null || smrs == null || smrs.Length == 0) return fallbackBoneTip;

                var distalBone = GetDistalBoneForFinger(finger);
                var distalTransform = anim.GetBoneTransform(distalBone);
                if (distalTransform == null) return fallbackBoneTip;

                var interBone = GetIntermediateBoneForFinger(finger);
                var interTransform = anim.GetBoneTransform(interBone);
                Vector3 boneDir = (interTransform != null) 
                    ? (distalTransform.position - interTransform.position).normalized 
                    : distalTransform.forward;

                SkinnedMeshRenderer targetSmr = null;
                int boneIdx = -1;

                for (int s = 0; s < smrs.Length; s++) {
                    var smr = smrs[s];
                    if (smr == null || smr.sharedMesh == null || !smr.sharedMesh.isReadable) continue;
                    var bones = smr.bones;
                    if (bones == null) continue;
                    int idx = Array.IndexOf(bones, distalTransform);
                    if (idx >= 0) {
                        targetSmr = smr;
                        boneIdx = idx;
                        break;
                    }
                }

                if (targetSmr == null || boneIdx < 0) return fallbackBoneTip;

                var sharedMesh = targetSmr.sharedMesh;
                var weights = sharedMesh.boneWeights;
                if (weights == null || weights.Length == 0) return fallbackBoneTip;

                Mesh bakedMesh = new Mesh();
                targetSmr.BakeMesh(bakedMesh);
                var bakedVerts = bakedMesh.vertices;
                if (bakedVerts == null || bakedVerts.Length != weights.Length) {
                    UnityEngine.Object.DestroyImmediate(bakedMesh);
                    return fallbackBoneTip;
                }

                var smrTransform = targetSmr.transform;
                Vector3 distalPos = distalTransform.position;

                List<Vector3> distalVertsWorld = new List<Vector3>();
                float maxForward = float.MinValue;
                Vector3 apexVertexWorld = fallbackBoneTip;

                for (int i = 0; i < weights.Length; i++) {
                    var bw = weights[i];
                    if ((bw.boneIndex0 == boneIdx && bw.weight0 > 0.25f) ||
                        (bw.boneIndex1 == boneIdx && bw.weight1 > 0.25f) ||
                        (bw.boneIndex2 == boneIdx && bw.weight2 > 0.25f) ||
                        (bw.boneIndex3 == boneIdx && bw.weight3 > 0.25f)) {

                        Vector3 vWorld = smrTransform.TransformPoint(bakedVerts[i]);
                        float fwd = Vector3.Dot(vWorld - distalPos, boneDir);
                        if (fwd > maxForward) {
                            maxForward = fwd;
                            apexVertexWorld = vWorld;
                        }
                        if (fwd >= -0.002f) {
                            distalVertsWorld.Add(vWorld);
                        }
                    }
                }

                UnityEngine.Object.DestroyImmediate(bakedMesh);

                if (distalVertsWorld.Count == 0) return fallbackBoneTip;

                usedMesh = true;
                if (!isPressed) {
                    return apexVertexWorld;
                }

                // In pressed state: find the lowest pad vertex in Rig space near the front half of the phalanx
                Vector3 bestContactWorld = apexVertexWorld;
                float minYRig = float.MaxValue;
                float minFwdThreshold = Mathf.Max(0.002f, maxForward * 0.35f);

                for (int i = 0; i < distalVertsWorld.Count; i++) {
                    Vector3 vW = distalVertsWorld[i];
                    float fwd = Vector3.Dot(vW - distalPos, boneDir);
                    if (fwd >= minFwdThreshold) {
                        Vector3 vRig = worldToRig.MultiplyPoint3x4(vW);
                        if (vRig.y < minYRig) {
                            minYRig = vRig.y;
                            bestContactWorld = vW;
                        }
                    }
                }
                return bestContactWorld;
            } catch {
                return fallbackBoneTip;
            }
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

            // Ensure finger animation layers are configured on Character
            SynchronizeAllFingerAnimations();

            // Cache character SkinnedMeshRenderers for mesh sampling
            var smrs = Character.GameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

            // World to Rig matrix
            Matrix4x4 worldToRig;
            Quaternion rootAnchorRot;
            if (RootAnchor != null && RootAnchor.GameObject != null) {
                worldToRig = RootAnchor.GameObject.transform.worldToLocalMatrix;
                rootAnchorRot = RootAnchor.GameObject.transform.rotation;
            } else {
                var charTransform = Character.GameObject.transform;
                Vector3 rigWorldPos = charTransform.TransformPoint(KeyboardRootPosition);
                rootAnchorRot = charTransform.rotation * Quaternion.Euler(KeyboardRootRotation);
                worldToRig = Matrix4x4.TRS(rigWorldPos, rootAnchorRot, Vector3.one).inverse;
            }

            // Resting wrist transforms in Rig Space
            Vector3 leftWrist0Rig = LeftHandRestPosition;
            Quaternion leftWristRot0Rig = Quaternion.Euler(LeftHandRestRotation);
            Vector3 rightWrist0Rig = RightHandRestPosition;
            Quaternion rightWristRot0Rig = Quaternion.Euler(RightHandRestRotation);

            // Sample Elbows in World Space and Rig Space
            var leftElbowBone = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            var rightElbowBone = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Vector3 leftElbowWorld = leftElbowBone != null ? leftElbowBone.position : (leftWristBone.position - leftWristBone.forward * 0.25f);
            Vector3 rightElbowWorld = rightElbowBone != null ? rightElbowBone.position : (rightWristBone.position - rightWristBone.forward * 0.25f);
            Vector3 leftElbowRig = worldToRig.MultiplyPoint3x4(leftElbowWorld);
            Vector3 rightElbowRig = worldToRig.MultiplyPoint3x4(rightElbowWorld);

            // Pre-sample neutral and pressed fingertip offsets in Hand Local Space
            Vector3[] tipNeutralLocal = new Vector3[10];
            Vector3[] tipPressedLocal = new Vector3[10];

            var animancer = Character.Animancer;

            int meshFingersSampled = 0;

            for (int f = 0; f < 10; f++) {
                KeyboardFinger finger = (KeyboardFinger)f;
                bool isLeft = finger.IsLeftHand();
                var wristBone = isLeft ? leftWristBone : rightWristBone;
                Vector3 wrist0Rig = isLeft ? leftWrist0Rig : rightWrist0Rig;
                Quaternion wristRot0Rig = isLeft ? leftWristRot0Rig : rightWristRot0Rig;
                Vector3 wristRig = (wristBone != null) ? worldToRig.MultiplyPoint3x4(wristBone.position) : wrist0Rig;

                // 1. Measure neutral fingertip offset in Hand Reference Space
                Vector3 fallbackNeutralTipWorld = GetFingerTipWorldPosition(animator, finger);
                Vector3 neutralTipWorld = GetFingerTipMeshWorldPosition(animator, smrs, finger, fallbackNeutralTipWorld, false, worldToRig, out bool usedMeshNeutral);
                Vector3 neutralTipRig = worldToRig.MultiplyPoint3x4(neutralTipWorld);
                Vector3 neutralOffsetRig = neutralTipRig - wristRig;
                tipNeutralLocal[f] = Quaternion.Inverse(wristRot0Rig) * neutralOffsetRig;

                // 2. Measure active/pressed fingertip offset using animation clip's final frame & mesh pad surface
                bool sampledWithAnim = false;
                bool usedMeshPressed = false;
                Vector3 pressedTipWorld = neutralTipWorld;

                if (animancer != null && Character.OverlappingAnimations != null) {
                    var layerId = GetFingerLayerId(finger);
                    var layerData = Array.Find(Character.OverlappingAnimations, a => a.CustomLayerID == layerId);
                    if (layerData != null) {
                        int layerIdx = GetAnimancerLayerIndex(layerData);
                        if (layerIdx >= 0 && layerIdx < animancer.Layers.Count) {
                            var layer = animancer.Layers[layerIdx];
                            if (layer != null) {
                                float oldWeight = layer.Weight;
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
                                    float oldTime = state.Time;
                                    bool wasPlaying = state.IsPlaying;

                                    layer.SetWeight(1f);
                                    state.NormalizedTime = 1f; // Final frame of pressed keystroke!
                                    state.IsPlaying = true;
                                    animancer.Evaluate();

                                    Vector3 fallbackPressedTipWorld = GetFingerTipWorldPosition(animator, finger);
                                    pressedTipWorld = GetFingerTipMeshWorldPosition(animator, smrs, finger, fallbackPressedTipWorld, true, worldToRig, out usedMeshPressed);
                                    sampledWithAnim = true;

                                    // Restore original layer state
                                    layer.SetWeight(oldWeight);
                                    state.Time = oldTime;
                                    state.IsPlaying = wasPlaying;
                                    animancer.Evaluate();
                                }
                            }
                        }
                    }
                }

                if (usedMeshPressed || usedMeshNeutral) {
                    meshFingersSampled++;
                }

                if (sampledWithAnim) {
                    Vector3 pressedTipRig = worldToRig.MultiplyPoint3x4(pressedTipWorld);
                    Vector3 pressedOffsetRig = pressedTipRig - wristRig;
                    tipPressedLocal[f] = Quaternion.Inverse(wristRot0Rig) * pressedOffsetRig;
                } else {
                    // Fallback curl displacement: move tip downward (-Y) ~2.5cm and slightly back (-Z) ~1.0cm in Hand Local Space
                    tipPressedLocal[f] = tipNeutralLocal[f] + new Vector3(0f, -0.025f, -0.010f);
                }
            }

            var list = new List<KeyboardKeyBinding>();

            // Resting key plane normal
            Vector3 restKeyNormal = (RootKeysetTransform != null) ? Quaternion.Euler(RootKeysetTransform.Rotation) * Vector3.up : Vector3.up;

            foreach (var k in VirtualKeysCollection.Keys) {
                if (k == null) continue;

                // 1. Get Key position and orientation in Rig Space
                Vector3 keyPosRig;
                Quaternion keyRotRig;

                if (k.KeyGameObject != null) {
                    keyPosRig = worldToRig.MultiplyPoint3x4(k.KeyGameObject.transform.position);
                    keyRotRig = Quaternion.Inverse(rootAnchorRot) * k.KeyGameObject.transform.rotation;
                } else {
                    Vector3 groupPos;
                    Quaternion groupRot;
                    Vector3 groupScale = Vector3.one;

                    if (k.AttachmentAnchor == VirtualKeyAnchor.LeftHand) {
                        var basePos = LeftHandRestPosition;
                        var baseRot = Quaternion.Euler(LeftHandRestRotation);
                        var pos = LeftKeysetTransform?.Position ?? Vector3.zero;
                        var rot = LeftKeysetTransform?.Rotation ?? Vector3.zero;
                        groupScale = LeftKeysetTransform?.Scale ?? Vector3.one;
                        groupPos = basePos + baseRot * pos;
                        groupRot = baseRot * Quaternion.Euler(rot);
                    } else if (k.AttachmentAnchor == VirtualKeyAnchor.RightHand) {
                        var basePos = RightHandRestPosition;
                        var baseRot = Quaternion.Euler(RightHandRestRotation);
                        var pos = RightKeysetTransform?.Position ?? Vector3.zero;
                        var rot = RightKeysetTransform?.Rotation ?? Vector3.zero;
                        groupScale = RightKeysetTransform?.Scale ?? Vector3.one;
                        groupPos = basePos + baseRot * pos;
                        groupRot = baseRot * Quaternion.Euler(rot);
                    } else {
                        var pos = RootKeysetTransform?.Position ?? Vector3.zero;
                        var rot = RootKeysetTransform?.Rotation ?? Vector3.zero;
                        groupScale = RootKeysetTransform?.Scale ?? Vector3.one;
                        groupPos = pos;
                        groupRot = Quaternion.Euler(rot);
                    }

                    Vector3 scaledKeyLocalPos = Vector3.Scale(k.Transform.Position, groupScale);
                    keyPosRig = groupPos + groupRot * scaledKeyLocalPos;
                    keyRotRig = groupRot * Quaternion.Euler(k.Transform.Rotation);
                }

                KeyboardFinger finger = k.AssignedFinger;
                int fingerIdx = (int)finger;
                bool isLeftHand = finger.IsLeftHand();

                Vector3 wrist0Rig = isLeftHand ? leftWrist0Rig : rightWrist0Rig;
                Quaternion wristRot0Rig = isLeftHand ? leftWristRot0Rig : rightWristRot0Rig;
                Vector3 elbowRig = isLeftHand ? leftElbowRig : rightElbowRig;

                // 2. Forearm reach alignment & key normal matching:
                // Align hand surface normal directly to the key's surface normal
                Vector3 keyNormalRig = keyRotRig * Vector3.up;

                // Hand forward axis follows forearm reach direction from elbow projected onto the key plane
                Vector3 vReach = keyPosRig - elbowRig;
                Vector3 fwdOnKeyPlane = Vector3.ProjectOnPlane(vReach, keyNormalRig).normalized;
                if (fwdOnKeyPlane.sqrMagnitude < 0.001f) {
                    fwdOnKeyPlane = Vector3.ProjectOnPlane(wristRot0Rig * Vector3.forward, keyNormalRig).normalized;
                }

                Quaternion targetHandRotRig = Quaternion.LookRotation(fwdOnKeyPlane, keyNormalRig);

                // 3. Closed-form wrist position anchored at pressed finger contact point:
                // When finger animation is at the end (pressed), the finger tip is exactly at the key center
                Vector3 contactLocal = tipPressedLocal[fingerIdx];
                Vector3 activeContactRig = targetHandRotRig * contactLocal;
                Vector3 targetWristRig = keyPosRig - activeContactRig;

                // 4. Delta Transforms
                Vector3 deltaPos = targetWristRig - wrist0Rig;

                Vector3 handScale = isLeftHand ? GetLeftHandMovementScale() : GetRightHandMovementScale();
                if (handScale.x != 0f) deltaPos.x /= handScale.x;
                if (handScale.y != 0f) deltaPos.y /= handScale.y;
                if (handScale.z != 0f) deltaPos.z /= handScale.z;

                Vector3 targetHandEuler = targetHandRotRig.eulerAngles;
                Vector3 restHandEuler = wristRot0Rig.eulerAngles;
                Vector3 deltaRot = Helper.NormalizeRotationAngles(targetHandEuler - restHandEuler);

                // 5. Ergonomic neutral check for resting home keys
                bool enableHandMove = true;
                if (deltaPos.sqrMagnitude < 0.0004f && Vector3.Angle(targetHandRotRig * Vector3.forward, wristRot0Rig * Vector3.forward) < 3.0f) {
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

            string modeDetail;
            if (meshFingersSampled == 10) {
                modeDetail = " (3D Skinned Mesh surface)";
            } else if (meshFingersSampled > 0) {
                modeDetail = $" ({meshFingersSampled}/10 Mesh surface, {10 - meshFingersSampled}/10 Bone fallback)";
            } else {
                modeDetail = " (Avatar Bone skeleton)";
            }

            Context.Service?.Toast(
                Warudo.Core.Server.ToastSeverity.Success,
                "AUTO_GENERATE_KEY_BINDINGS".Localized(),
                string.Format("AUTO_GENERATE_SUCCESS".Localized(), KeyBindings.Length) + modeDetail
            );

            Debug.Log($"[FS Keyboard Receiver] Auto-generated {KeyBindings.Length} key bindings using {modeDetail.Trim(' ', '(', ')')}. Character: '{Character.Name}'.");
        }

        #endregion
    }
}
