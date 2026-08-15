using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

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
        SplitErgonomic
    }

    public partial class KeyboardReceiverAsset : ReceiverAsset {

        [Section("FINGER_MOVEMENT")]

        [DataInput(200)]
        [Label("KEYBOARD_LAYOUT_PRESET")]
        public KeyboardLayoutPreset LayoutPreset = KeyboardLayoutPreset.StandardQWERTY;

        [Trigger(210)]
        [Label("APPLY_LAYOUT_PRESET")]
        public void TriggerApplyLayoutPreset() {
            ApplyPreset(LayoutPreset);
        }

        [DataInput(220)]
        [Label("KEY_PRESS_TRANSITION")]
        public Transition KeyPressTransition = new Transition { Time = 0.04f, Ease = Ease.OutQuad };

        [DataInput(230)]
        [Label("KEY_RELEASE_TRANSITION")]
        public Transition KeyReleaseTransition = new Transition { Time = 0.08f, Ease = Ease.OutQuad };

        [DataInput(240)]
        [Label("KEY_BINDINGS")]
        public KeyboardKeyBinding[] KeyBindings = new KeyboardKeyBinding[0];

        // Runtime animation and movement state
        Tween[] fingerTweens = new Tween[10];

        Vector3 currentLeftHandPos;
        Vector3 currentLeftHandRot;
        Vector3 currentRightHandPos;
        Vector3 currentRightHandRot;

        Vector3 lastLeftKeyPos;
        Vector3 lastLeftKeyRot;
        bool hasLeftKeyBeenPressed;
        float leftKeyReleaseTime = -1f;

        Vector3 lastRightKeyPos;
        Vector3 lastRightKeyRot;
        bool hasRightKeyBeenPressed;
        float rightKeyReleaseTime = -1f;

        void OnCreateFingerMovement() {
            if (KeyBindings == null || KeyBindings.Length == 0) {
                ApplyPreset(KeyboardLayoutPreset.StandardQWERTY, false);
            }
        }

        void OnDestroyFingerMovement() {
            for (int i = 0; i < fingerTweens.Length; i++) {
                fingerTweens[i]?.Kill();
            }
            ResetActiveKeyValues();
        }

        public void ResetActiveKeyValues() {
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
            LastVkCode = 0;
        }

        void OnUpdateFingerMovement() {
            if (!IsEnabled || !IsHandEnabled || Character == null) return;

            float[] targetFingerWeights = new float[10];

            // 1. Process Left Hand (Up to 1 finger active)
            bool isLeftInVisualSetup = LeftHandRestTransform != null && LeftHandRestTransform.InVisualSetupMode;
            var activeLeftKey = isLeftInVisualSetup ? null : GetActiveKeyForHand(true, KeyBindings);
            Vector3 restLeftPos = (isLeftInVisualSetup && LeftHandRestTransform.setupAnchor != null)
                ? LeftHandRestTransform.setupAnchor.Transform.Position
                : LeftHandRestPosition;
            Vector3 restLeftRot = (isLeftInVisualSetup && LeftHandRestTransform.setupAnchor != null)
                ? LeftHandRestTransform.setupAnchor.Transform.Rotation
                : LeftHandRestRotation;

            Vector3 targetLeftPos;
            Vector3 targetLeftRot;

            if (isLeftInVisualSetup) {
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
                    targetLeftPos = restLeftPos + Vector3.Scale(activeLeftKey.DeltaPosition, MovementFactor);
                    targetLeftRot = restLeftRot + activeLeftKey.DeltaRotation;
                } else {
                    targetLeftPos = hasLeftKeyBeenPressed ? lastLeftKeyPos : restLeftPos;
                    targetLeftRot = hasLeftKeyBeenPressed ? lastLeftKeyRot : restLeftRot;
                }

                lastLeftKeyPos = targetLeftPos;
                lastLeftKeyRot = targetLeftRot;
                hasLeftKeyBeenPressed = true;
                leftKeyReleaseTime = -1f;

                if (activeLeftKey.EnableHandMovement || PlayAnimationOnDisabledKey) {
                    targetFingerWeights[(int)activeLeftKey.Finger] = 1.0f;
                }
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

            // 2. Process Right Hand (Up to 1 finger active)
            bool isRightInVisualSetup = RightHandRestTransform != null && RightHandRestTransform.InVisualSetupMode;
            var activeRightKey = isRightInVisualSetup ? null : GetActiveKeyForHand(false, KeyBindings);
            Vector3 restRightPos = (isRightInVisualSetup && RightHandRestTransform.setupAnchor != null)
                ? RightHandRestTransform.setupAnchor.Transform.Position
                : RightHandRestPosition;
            Vector3 restRightRot = (isRightInVisualSetup && RightHandRestTransform.setupAnchor != null)
                ? RightHandRestTransform.setupAnchor.Transform.Rotation
                : RightHandRestRotation;

            Vector3 targetRightPos;
            Vector3 targetRightRot;

            if (isRightInVisualSetup) {
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
                    targetRightPos = restRightPos + Vector3.Scale(activeRightKey.DeltaPosition, MovementFactor);
                    targetRightRot = restRightRot + activeRightKey.DeltaRotation;
                } else {
                    targetRightPos = hasRightKeyBeenPressed ? lastRightKeyPos : restRightPos;
                    targetRightRot = hasRightKeyBeenPressed ? lastRightKeyRot : restRightRot;
                }

                lastRightKeyPos = targetRightPos;
                lastRightKeyRot = targetRightRot;
                hasRightKeyBeenPressed = true;
                rightKeyReleaseTime = -1f;

                if (activeRightKey.EnableHandMovement || PlayAnimationOnDisabledKey) {
                    targetFingerWeights[(int)activeRightKey.Finger] = 1.0f;
                }
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

            // 3. Drive Left Hand Anchor
            var leftAnchor = LeftHandAnchor;
            if (leftAnchor != null) {
                var isPress = activeLeftKey != null;
                var trans = isPress ? KeyPressTransition : KeyReleaseTransition;

                if (!isLeftInVisualSetup && trans != null && trans.Time > 0f) {
                    currentLeftHandPos = Vector3.Lerp(currentLeftHandPos, targetLeftPos, Time.deltaTime / trans.Time);
                    currentLeftHandRot = Vector3.Lerp(currentLeftHandRot, targetLeftRot, Time.deltaTime / trans.Time);
                } else {
                    currentLeftHandPos = targetLeftPos;
                    currentLeftHandRot = targetLeftRot;
                }

                leftAnchor.Transform.Position = currentLeftHandPos;
                leftAnchor.Transform.Rotation = currentLeftHandRot;
                leftAnchor.Transform.Broadcast();
            }

            // 4. Drive Right Hand Anchor
            var rightAnchor = RightHandAnchor;
            if (rightAnchor != null) {
                var isPress = activeRightKey != null;
                var trans = isPress ? KeyPressTransition : KeyReleaseTransition;

                if (!isRightInVisualSetup && trans != null && trans.Time > 0f) {
                    currentRightHandPos = Vector3.Lerp(currentRightHandPos, targetRightPos, Time.deltaTime / trans.Time);
                    currentRightHandRot = Vector3.Lerp(currentRightHandRot, targetRightRot, Time.deltaTime / trans.Time);
                } else {
                    currentRightHandPos = targetRightPos;
                    currentRightHandRot = targetRightRot;
                }

                rightAnchor.Transform.Position = currentRightHandPos;
                rightAnchor.Transform.Rotation = currentRightHandRot;
                rightAnchor.Transform.Broadcast();
            }

            // 5. Drive 10 Finger Overlay Animations
            if (Character.OverlappingAnimations != null) {
                for (int f = 0; f < 10; f++) {
                    KeyboardFinger finger = (KeyboardFinger)f;
                    ApplyFingerLayerWeight(finger, f, targetFingerWeights[f]);
                }
            }
        }

        void ApplyFingerLayerWeight(KeyboardFinger finger, int fingerIndex, float targetWeight) {
            var animancer = Character?.Animancer;
            if (animancer == null || animancer.Layers == null) return;
            if (Character.OverlappingAnimations == null) return;

            var layerId = GetFingerLayerId(finger);
            var layerData = Array.Find(Character.OverlappingAnimations, a => a.CustomLayerID == layerId);
            if (layerData == null) return;

            var idx = Array.IndexOf(Character.OverlappingAnimations, layerData);
            if (idx < 0) return;

            // In Warudo, Animancer Layer 0 is the Base Animation Layer. Overlapping animations start at Layer 1.
            int animancerLayerIdx = 1 + idx;
            if (animancerLayerIdx >= animancer.Layers.Count) return;

            var layer = animancer.Layers[animancerLayerIdx];
            if (layer == null) return;

            var currentWeight = layer.Weight;
            if (Mathf.Abs(currentWeight - targetWeight) < 0.001f) return;

            var trans = targetWeight > 0.01f ? KeyPressTransition : KeyReleaseTransition;

            if (trans != null && trans.Time > 0f) {
                fingerTweens[fingerIndex]?.Kill(false);
                fingerTweens[fingerIndex] = DOTween.To(
                    () => {
                        if (animancer != null && animancer.Layers != null && animancerLayerIdx < animancer.Layers.Count) {
                            return animancer.Layers[animancerLayerIdx].Weight;
                        }
                        return 0f;
                    },
                    v => {
                        if (animancer != null && animancer.Layers != null && animancerLayerIdx < animancer.Layers.Count) {
                            animancer.Layers[animancerLayerIdx].SetWeight(v);
                        }
                    },
                    targetWeight,
                    trans.Time
                ).SetEase(trans.Ease);
            } else {
                layer.SetWeight(targetWeight);
            }
        }

        public void ResetFingerWeights() {
            for (int i = 0; i < fingerTweens.Length; i++) {
                fingerTweens[i]?.Kill();
            }

            ResetActiveKeyValues();

            var animancer = Character?.Animancer;
            if (animancer == null || animancer.Layers == null || Character?.OverlappingAnimations == null) return;

            for (int i = 0; i < Character.OverlappingAnimations.Length; i++) {
                var d = Character.OverlappingAnimations[i];
                if (d != null && d.CustomLayerID != null && d.CustomLayerID.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX)) {
                    int layerIdx = 1 + i;
                    if (layerIdx < animancer.Layers.Count && animancer.Layers[layerIdx] != null) {
                        animancer.Layers[layerIdx].SetWeight(0f);
                    }
                }
            }
        }

        public void ApplyPreset(KeyboardLayoutPreset preset, bool showToast = true) {
            var list = new List<KeyboardKeyBinding>();

            // Helper to add key definition
            void AddKey(VKCode key, KeyboardFinger finger, float deltaX, float deltaZ, float rotY = 0f, bool enableHandMove = true) {
                var binding = StructuredData.Create<KeyboardKeyBinding>(b => {
                    b.Key = key;
                    b.Finger = finger;
                    b.DeltaPosition = new Vector3(deltaX, 0f, deltaZ);
                    b.DeltaRotation = new Vector3(0f, rotY, 0f);
                    b.EnableHandMovement = enableHandMove;
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

            // Function Keys (for Full / TKL presets)
            if (preset == KeyboardLayoutPreset.StandardQWERTY || preset == KeyboardLayoutPreset.StandardTKL) {
                AddKey(VKCode.F1, KeyboardFinger.LeftPinky, -0.019f, 0.057f);
                AddKey(VKCode.F2, KeyboardFinger.LeftRing, -0.005f, 0.057f);
                AddKey(VKCode.F3, KeyboardFinger.LeftMiddle, -0.005f, 0.057f);
                AddKey(VKCode.F4, KeyboardFinger.LeftIndex, -0.010f, 0.057f);
                AddKey(VKCode.F5, KeyboardFinger.LeftIndex, 0.014f, 0.057f);
                AddKey(VKCode.F6, KeyboardFinger.RightIndex, -0.024f, 0.057f);
                AddKey(VKCode.F7, KeyboardFinger.RightIndex, -0.005f, 0.057f);
                AddKey(VKCode.F8, KeyboardFinger.RightMiddle, -0.005f, 0.057f);
                AddKey(VKCode.F9, KeyboardFinger.RightRing, -0.005f, 0.057f);
                AddKey(VKCode.F10, KeyboardFinger.RightPinky, -0.005f, 0.057f);
                AddKey(VKCode.F11, KeyboardFinger.RightPinky, 0.014f, 0.057f);
                AddKey(VKCode.F12, KeyboardFinger.RightPinky, 0.033f, 0.057f);

                // Navigation Cluster (Right Pinky)
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

            // Adjustments for Split Ergonomic preset
            if (preset == KeyboardLayoutPreset.SplitErgonomic) {
                LeftHandRestPosition = new Vector3(-0.09f, 0.02f, 0f);
                LeftHandRestRotation = new Vector3(0f, 78f, 0f);
                RightHandRestPosition = new Vector3(0.09f, 0.02f, 0f);
                RightHandRestRotation = new Vector3(0f, -78f, 0f);

                foreach (var b in list) {
                    if (b.Finger.IsLeftHand()) {
                        b.DeltaRotation = new Vector3(0f, -12f, 0f);
                    } else {
                        b.DeltaRotation = new Vector3(0f, 12f, 0f);
                    }
                }
            } else {
                LeftHandRestPosition = new Vector3(-0.07f, 0.02f, 0f);
                LeftHandRestRotation = new Vector3(0f, 90f, 0f);
                RightHandRestPosition = new Vector3(0.07f, 0.02f, 0f);
                RightHandRestRotation = new Vector3(0f, -90f, 0f);
            }

            KeyBindings = list.ToArray();
            BroadcastDataInput(nameof(KeyBindings));
            BroadcastDataInput(nameof(LeftHandRestTransform));
            BroadcastDataInput(nameof(RightHandRestTransform));

            if (showToast) {
                Context.Service?.Toast(
                    Warudo.Core.Server.ToastSeverity.Success,
                    Name,
                    $"Applied keyboard layout preset [{preset}] with {KeyBindings.Length} keys."
                );
            }
        }
    }
}
