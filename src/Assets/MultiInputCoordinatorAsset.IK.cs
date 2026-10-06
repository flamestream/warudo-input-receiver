using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Localization;
using Warudo.Plugins.Core.Assets;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Utility;

namespace FlameStream {
    public partial class MultiInputCoordinatorAsset {

        [DataInput]
        [Hidden]
        public Guid ProxyLeftHandAnchorAssetId;

        [DataInput]
        [Hidden]
        public Guid ProxyRightHandAnchorAssetId;

        AnchorAsset proxyLeftAnchor;
        AnchorAsset proxyRightAnchor;

        public CoordinatorHandState currentLeftState = CoordinatorHandState.Keyboard;
        public CoordinatorHandState currentRightState = CoordinatorHandState.Keyboard;

        CoordinatorHandState targetLeftState = CoordinatorHandState.Keyboard;
        CoordinatorHandState targetRightState = CoordinatorHandState.Keyboard;

        float lastMouseActivityTime = -1f;
        float lastPenActivityTime = -1f;
        float lastKeyboardLeftTime = -1f;
        float lastKeyboardRightTime = -1f;
        float lastKeyboardActivityTime = -1f;

        int lastMouseX;
        int lastMouseY;
        bool lastMouseButton1;
        bool lastMouseButton2;

        int lastPenX;
        int lastPenY;
        bool lastPenButton1;
        bool lastPenButton2;

        public bool hasInitializedMouse;
        public bool hasInitializedPen;

        CharacterAsset lastBoundCharacter;

        Tween leftProgressTween;
        Tween leftWeightTween;
        bool isLeftTransitioning;
        float leftTransitionProgress = 1f;
        Vector3 leftTransitionStartPos;
        Vector3 leftTransitionStartRot;
        float currentLeftWeight = 0f;

        Tween rightProgressTween;
        Tween rightWeightTween;
        bool isRightTransitioning;
        float rightTransitionProgress = 1f;
        Vector3 rightTransitionStartPos;
        Vector3 rightTransitionStartRot;
        float currentRightWeight = 0f;
        int bindSettleCountdown = 0;
        bool isIkBound = false;

        void OnCreateIK() {
            float now = Time.time;
            lastMouseActivityTime = now;
            lastPenActivityTime = now;
            lastKeyboardActivityTime = now;
            lastKeyboardLeftTime = now;
            lastKeyboardRightTime = now;

            if (IsEnabled && EnableHands) {
                proxyLeftAnchor = GetProxyLeftAnchor();
                proxyRightAnchor = GetProxyRightAnchor();
            }
        }

        void OnDestroyIK() {
            bindSettleCountdown = 0;
            isIkBound = false;
            ReleaseCharacterIK(instant: true);
            proxyLeftAnchor = null;
            proxyRightAnchor = null;
        }

        void KillAllTweens() {
            bindSettleCountdown = 0;
            leftProgressTween?.Kill();
            leftWeightTween?.Kill();
            rightProgressTween?.Kill();
            rightWeightTween?.Kill();
        }

        void CleanDestroy(GameObjectAsset g) {
            if (g == null) return;
            try {
                Scene.RemoveAsset(g.Id);
            } catch {}
        }

        public void DestroyProxyAnchors() {
            var left = proxyLeftAnchor ?? (ProxyLeftHandAnchorAssetId != Guid.Empty ? Scene?.GetAssets<AnchorAsset>().FirstOrDefault(a => a.Id == ProxyLeftHandAnchorAssetId) : null);
            if (left != null) {
                CleanDestroy(left);
            }
            proxyLeftAnchor = null;
            ProxyLeftHandAnchorAssetId = Guid.Empty;
            SetDataInput(nameof(ProxyLeftHandAnchorAssetId), Guid.Empty, true);

            var right = proxyRightAnchor ?? (ProxyRightHandAnchorAssetId != Guid.Empty ? Scene?.GetAssets<AnchorAsset>().FirstOrDefault(a => a.Id == ProxyRightHandAnchorAssetId) : null);
            if (right != null) {
                CleanDestroy(right);
            }
            proxyRightAnchor = null;
            ProxyRightHandAnchorAssetId = Guid.Empty;
            SetDataInput(nameof(ProxyRightHandAnchorAssetId), Guid.Empty, true);
        }

        public AnchorAsset GetProxyLeftAnchor(bool skipAutoCreate = false) {
            if (proxyLeftAnchor != null && proxyLeftAnchor.GameObject != null) return proxyLeftAnchor;
            bool isNew = (ProxyLeftHandAnchorAssetId == Guid.Empty || Scene?.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == ProxyLeftHandAnchorAssetId) == null);
            proxyLeftAnchor = GetAsset<AnchorAsset>(ref ProxyLeftHandAnchorAssetId, skipAutoCreate, newAssetName: "🔒⚓-🔥🎯 Left Proxy Hand IK");
            if (proxyLeftAnchor != null) {
                if (ProxyLeftHandAnchorAssetId != proxyLeftAnchor.Id) {
                    ProxyLeftHandAnchorAssetId = proxyLeftAnchor.Id;
                    SetDataInput(nameof(ProxyLeftHandAnchorAssetId), proxyLeftAnchor.Id, true);
                }
                if (isNew) {
                    InitializeProxyAnchorPose(proxyLeftAnchor, true);
                }
            }
            return proxyLeftAnchor;
        }

        public AnchorAsset GetProxyRightAnchor(bool skipAutoCreate = false) {
            if (proxyRightAnchor != null && proxyRightAnchor.GameObject != null) return proxyRightAnchor;
            bool isNew = (ProxyRightHandAnchorAssetId == Guid.Empty || Scene?.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == ProxyRightHandAnchorAssetId) == null);
            proxyRightAnchor = GetAsset<AnchorAsset>(ref ProxyRightHandAnchorAssetId, skipAutoCreate, newAssetName: "🔒⚓-🔥🎯 Right Proxy Hand IK");
            if (proxyRightAnchor != null) {
                if (ProxyRightHandAnchorAssetId != proxyRightAnchor.Id) {
                    ProxyRightHandAnchorAssetId = proxyRightAnchor.Id;
                    SetDataInput(nameof(ProxyRightHandAnchorAssetId), proxyRightAnchor.Id, true);
                }
                if (isNew) {
                    InitializeProxyAnchorPose(proxyRightAnchor, false);
                }
            }
            return proxyRightAnchor;
        }

        void InitializeProxyAnchorPose(AnchorAsset proxy, bool isLeft) {
            if (proxy == null) return;
            Transform bone = null;
            if (Character != null && Character.Animator != null) {
                bone = Character.Animator.GetBoneTransform(isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            }
            Vector3 initPos = bone != null ? bone.position : (Character != null && Character.GameObject != null ? Character.GameObject.transform.position + (isLeft ? new Vector3(-0.2f, 1f, 0.2f) : new Vector3(0.2f, 1f, 0.2f)) : Vector3.zero);
            Vector3 initRot = bone != null ? bone.eulerAngles : (isLeft ? new Vector3(0, 90f, 0) : new Vector3(0, -90f, 0));
            ApplyProxyWorldPose(proxy, initPos, initRot);
        }

        T GetAsset<T>(ref Guid assetId, bool skipAutoCreate = false, string newAssetName = null) where T : Warudo.Core.Scenes.Asset {
            if (isDestroyed || Scene == null) return null;
            var category = (!string.IsNullOrEmpty(newAssetName) && newAssetName.StartsWith("🔒"))
                ? "FS_ASSET_CATEGORY_INTERNAL".Localized()
                : "FS_ASSET_CATEGORY_INPUT".Localized();
            if (assetId != Guid.Empty) {
                var id = assetId;
                var fetchResult = Scene.GetAssets<T>().FirstOrDefault(p => p.Id == id);
                if (fetchResult != null) {
                    return fetchResult;
                }
            }
            if (skipAutoCreate) return null;
            var newAsset = Scene.AddAssetToGroup<T>(category);
            if (newAssetName != null) newAsset.Name = newAssetName;
            Scene.UpdateNewAssetName(newAsset);
            Helper.AssignAssetToCategory(Scene, newAsset, category);
            newAsset.Broadcast();
            assetId = newAsset.Id;
            return newAsset;
        }

        void EnsureReceiversRegistered() {
            if (!IsEnabled) return;

            if (KeyboardReceiver != null && KeyboardReceiver.ActiveCoordinator != this) {
                KeyboardReceiver.RegisterCoordinator(this);
            }
            if (MouseReceiver != null) {
                MouseReceiver.LayerPrefix = "🔥🖱️";
                if (MouseReceiver.ActiveCoordinator != this) MouseReceiver.RegisterCoordinator(this);
            }
            if (PenReceiver != null) {
                PenReceiver.LayerPrefix = "🔥🖊️";
                if (PenReceiver.ActiveCoordinator != this) PenReceiver.RegisterCoordinator(this);
            }
        }

        void RegisterAllReceivers() {
            KeyboardReceiver?.RegisterCoordinator(this);
            if (MouseReceiver != null) {
                MouseReceiver.LayerPrefix = "🔥🖱️";
                MouseReceiver.RegisterCoordinator(this);
            }
            if (PenReceiver != null) {
                PenReceiver.LayerPrefix = "🔥🖊️";
                PenReceiver.RegisterCoordinator(this);
            }
        }

        void UnregisterAllReceivers(bool isTeardown = false) {
            if (KeyboardReceiver != null) {
                var kr = KeyboardReceiver;
                kr.UnregisterCoordinator(this, isTeardown);
                if (!isTeardown) {
                    kr.SetDataInputPropertyAndBroadcast(nameof(kr.IsEnabled), disabled: false, description: "KEYBOARD_ENABLE_DESC".Localized());
                    kr.SetDataInputPropertyAndBroadcast(nameof(kr.IsHandEnabled), disabled: false, description: "KEYBOARD_ENABLE_DESC".Localized());
                }
            }
            if (MouseReceiver != null) {
                var mr = MouseReceiver;
                mr.LayerPrefix = "🔥👆";
                mr.UnregisterCoordinator(this, isTeardown);
                if (!isTeardown) {
                    mr.SetDataInputPropertyAndBroadcast(nameof(mr.IsEnabled), disabled: false, description: "POINTER_ENABLE_DESC".Localized());
                    mr.SetDataInputPropertyAndBroadcast(nameof(mr.IsHandEnabled), disabled: false, description: "POINTER_ENABLE_DESC".Localized());
                }
            }
            if (PenReceiver != null) {
                var pr = PenReceiver;
                pr.LayerPrefix = "🔥👆";
                pr.UnregisterCoordinator(this, isTeardown);
                if (!isTeardown) {
                    pr.SetDataInputPropertyAndBroadcast(nameof(pr.IsEnabled), disabled: false, description: "POINTER_ENABLE_DESC".Localized());
                    pr.SetDataInputPropertyAndBroadcast(nameof(pr.IsHandEnabled), disabled: false, description: "POINTER_ENABLE_DESC".Localized());
                }
            }
        }

        void BindCharacterIK() {
            if (Character == null) return;

            isIkBound = true;
            leftWeightTween?.Kill();
            rightWeightTween?.Kill();
            currentLeftWeight = 0f;
            currentRightWeight = 0f;

            if (lastBoundCharacter != null && lastBoundCharacter != Character) {
                if (lastBoundCharacter.LeftHandIK != null && lastBoundCharacter.LeftHandIK.IkTarget == proxyLeftAnchor) {
                    lastBoundCharacter.LeftHandIK.PositionWeight = 0f;
                    lastBoundCharacter.LeftHandIK.RotationWeight = 0f;
                    lastBoundCharacter.LeftHandIK.Enabled = false;
                    lastBoundCharacter.LeftHandIK.IkTarget = null;
                    lastBoundCharacter.LeftHandIK.Broadcast();
                }
                if (lastBoundCharacter.RightHandIK != null && lastBoundCharacter.RightHandIK.IkTarget == proxyRightAnchor) {
                    lastBoundCharacter.RightHandIK.PositionWeight = 0f;
                    lastBoundCharacter.RightHandIK.RotationWeight = 0f;
                    lastBoundCharacter.RightHandIK.Enabled = false;
                    lastBoundCharacter.RightHandIK.IkTarget = null;
                    lastBoundCharacter.RightHandIK.Broadcast();
                }
            }
            lastBoundCharacter = Character;

            if (Character.LeftHandIK != null) {
                Character.LeftHandIK.PositionWeight = 0f;
                Character.LeftHandIK.RotationWeight = 0f;
                Character.LeftHandIK.BendGoalWeight = 0f;
            }
            if (Character.RightHandIK != null) {
                Character.RightHandIK.PositionWeight = 0f;
                Character.RightHandIK.RotationWeight = 0f;
                Character.RightHandIK.BendGoalWeight = 0f;
            }

            proxyLeftAnchor = GetProxyLeftAnchor();
            proxyRightAnchor = GetProxyRightAnchor();

            // Give downstream receivers and Warudo Attachable hierarchies 3 frames to settle before binding
            bindSettleCountdown = 3;
        }

        void FinalizeBindCharacterIK() {
            if (!IsEnabled || !EnableHands || Character == null) return;

            float now = Time.time;
            lastMouseActivityTime = now;
            lastPenActivityTime = now;
            lastKeyboardActivityTime = now;
            lastKeyboardLeftTime = now;
            lastKeyboardRightTime = now;

            proxyLeftAnchor = GetProxyLeftAnchor();
            proxyRightAnchor = GetProxyRightAnchor();

            // 1. Detect device inputs and determine initial starting states
            DetectDeviceInputs();

            CoordinatorHandState initialLeft = GetInactivityReturnState(true);
            CoordinatorHandState initialRight = GetInactivityReturnState(false);

            bool isMouseRight = MouseReceiver != null && MouseReceiver.IsRightHanded;
            bool isPenRight = PenReceiver != null && PenReceiver.IsRightHanded;
            bool activeMouse = MouseReceiver != null && MouseReceiver.IsEnabled && MouseReceiver.IsHandEnabled && (now - lastMouseActivityTime < 0.15f);
            bool activePen = PenReceiver != null && PenReceiver.IsEnabled && PenReceiver.IsHandEnabled && (now - lastPenActivityTime < 0.15f);
            bool keyPressLeft = (now - lastKeyboardLeftTime < 0.1f);
            bool keyPressRight = (now - lastKeyboardRightTime < 0.1f);

            if (keyPressLeft) initialLeft = CoordinatorHandState.Keyboard;
            else if (activeMouse && !isMouseRight) initialLeft = CoordinatorHandState.Mouse;
            else if (activePen && !isPenRight) initialLeft = CoordinatorHandState.Pen;

            if (keyPressRight) initialRight = CoordinatorHandState.Keyboard;
            else if (activeMouse && isMouseRight) initialRight = CoordinatorHandState.Mouse;
            else if (activePen && isPenRight) initialRight = CoordinatorHandState.Pen;

            currentLeftState = initialLeft;
            targetLeftState = initialLeft;
            currentRightState = initialRight;
            targetRightState = initialRight;

            // 2. Pre-position proxy anchors immediately before attaching IK
            if (proxyLeftAnchor != null) {
                GetTargetWorldPose(true, initialLeft, out Vector3 leftPos, out Vector3 leftRot, out _);
                ApplyProxyWorldPose(proxyLeftAnchor, leftPos, leftRot);
                leftTransitionStartPos = leftPos;
                leftTransitionStartRot = leftRot;
                leftTransitionProgress = 1f;
            }
            if (proxyRightAnchor != null) {
                GetTargetWorldPose(false, initialRight, out Vector3 rightPos, out Vector3 rightRot, out _);
                ApplyProxyWorldPose(proxyRightAnchor, rightPos, rightRot);
                rightTransitionStartPos = rightPos;
                rightTransitionStartRot = rightRot;
                rightTransitionProgress = 1f;
            }

            // 3. Connect IK targets
            if (Character.LeftHandIK != null && proxyLeftAnchor != null) {
                Character.LeftHandIK.PositionWeight = 0f;
                Character.LeftHandIK.RotationWeight = 0f;
                Character.LeftHandIK.BendGoalWeight = 0f;
                Character.LeftHandIK.Enabled = true;
                Character.LeftHandIK.IkTarget = proxyLeftAnchor;
                Character.LeftHandIK.Broadcast();
            }

            if (Character.RightHandIK != null && proxyRightAnchor != null) {
                Character.RightHandIK.PositionWeight = 0f;
                Character.RightHandIK.RotationWeight = 0f;
                Character.RightHandIK.BendGoalWeight = 0f;
                Character.RightHandIK.Enabled = true;
                Character.RightHandIK.IkTarget = proxyRightAnchor;
                Character.RightHandIK.Broadcast();
            }

            // 4. Smooth fade-in weights using ActiveTransition
            float duration = ActiveTransition != null ? ActiveTransition.Time : 0.5f;
            Ease ease = ActiveTransition != null ? ActiveTransition.Ease : Ease.Linear;
            float delay = ActiveTransition != null ? ActiveTransition.Delay : 0f;

            float leftRemaining = 1.0f - Mathf.Clamp01(currentLeftWeight);
            float actualLeftDuration = duration * leftRemaining;
            if (actualLeftDuration <= 0.001f) {
                currentLeftWeight = 1.0f;
                if (Character.LeftHandIK != null) {
                    Character.LeftHandIK.PositionWeight = 1.0f;
                    Character.LeftHandIK.RotationWeight = 1.0f;
                    Character.LeftHandIK.Broadcast();
                }
            } else {
                leftWeightTween = DOTween.To(
                    () => currentLeftWeight,
                    v => {
                        currentLeftWeight = v;
                        if (Character?.LeftHandIK != null && Character.LeftHandIK.Enabled) {
                            Character.LeftHandIK.PositionWeight = v;
                            Character.LeftHandIK.RotationWeight = v;
                            Character.LeftHandIK.Broadcast();
                        }
                    },
                    1.0f,
                    actualLeftDuration
                ).SetEase(ease);
                if (delay > 0f) leftWeightTween.SetDelay(delay);
            }

            float rightRemaining = 1.0f - Mathf.Clamp01(currentRightWeight);
            float actualRightDuration = duration * rightRemaining;
            if (actualRightDuration <= 0.001f) {
                currentRightWeight = 1.0f;
                if (Character.RightHandIK != null) {
                    Character.RightHandIK.PositionWeight = 1.0f;
                    Character.RightHandIK.RotationWeight = 1.0f;
                    Character.RightHandIK.Broadcast();
                }
            } else {
                rightWeightTween = DOTween.To(
                    () => currentRightWeight,
                    v => {
                        currentRightWeight = v;
                        if (Character?.RightHandIK != null && Character.RightHandIK.Enabled) {
                            Character.RightHandIK.PositionWeight = v;
                            Character.RightHandIK.RotationWeight = v;
                            Character.RightHandIK.Broadcast();
                        }
                    },
                    1.0f,
                    actualRightDuration
                ).SetEase(ease);
                if (delay > 0f) rightWeightTween.SetDelay(delay);
            }
        }

        void ReleaseCharacterIK(bool instant = false) {
            isIkBound = false;
            bindSettleCountdown = 0;
            leftWeightTween?.Kill();
            rightWeightTween?.Kill();

            if (instant || Character == null) {
                KillAllTweens();
                if (Character != null) {
                    if (Character.LeftHandIK != null && (proxyLeftAnchor == null || Character.LeftHandIK.IkTarget == proxyLeftAnchor)) {
                        Character.LeftHandIK.PositionWeight = 0f;
                        Character.LeftHandIK.RotationWeight = 0f;
                        Character.LeftHandIK.Enabled = false;
                        Character.LeftHandIK.IkTarget = null;
                        Character.LeftHandIK.Broadcast();
                    }
                    if (Character.RightHandIK != null && (proxyRightAnchor == null || Character.RightHandIK.IkTarget == proxyRightAnchor)) {
                        Character.RightHandIK.PositionWeight = 0f;
                        Character.RightHandIK.RotationWeight = 0f;
                        Character.RightHandIK.Enabled = false;
                        Character.RightHandIK.IkTarget = null;
                        Character.RightHandIK.Broadcast();
                    }
                }
                currentLeftWeight = 0f;
                currentRightWeight = 0f;
                return;
            }

            float duration = InactiveTransition != null ? InactiveTransition.Time : 0.5f;
            Ease ease = InactiveTransition != null ? InactiveTransition.Ease : Ease.Linear;

            float leftCurrent = Mathf.Clamp01(currentLeftWeight);
            float leftDuration = duration * leftCurrent;
            if (leftDuration <= 0.001f || Character.LeftHandIK == null) {
                currentLeftWeight = 0f;
                if (Character?.LeftHandIK != null && (proxyLeftAnchor == null || Character.LeftHandIK.IkTarget == proxyLeftAnchor)) {
                    Character.LeftHandIK.PositionWeight = 0f;
                    Character.LeftHandIK.RotationWeight = 0f;
                    Character.LeftHandIK.Enabled = false;
                    Character.LeftHandIK.IkTarget = null;
                    Character.LeftHandIK.Broadcast();
                }
                CheckCleanupAnchorsAfterFadeOut();
            } else {
                leftWeightTween = DOTween.To(
                    () => currentLeftWeight,
                    v => {
                        currentLeftWeight = v;
                        if (Character?.LeftHandIK != null && Character.LeftHandIK.Enabled) {
                            Character.LeftHandIK.PositionWeight = v;
                            Character.LeftHandIK.RotationWeight = v;
                            if (Character.LeftHandIK.BendGoalTarget != null) {
                                Character.LeftHandIK.BendGoalWeight = v;
                            }
                            Character.LeftHandIK.Broadcast();
                        }
                    },
                    0f,
                    leftDuration
                ).SetEase(ease).OnComplete(() => {
                    currentLeftWeight = 0f;
                    if (Character?.LeftHandIK != null && (proxyLeftAnchor == null || Character.LeftHandIK.IkTarget == proxyLeftAnchor)) {
                        Character.LeftHandIK.PositionWeight = 0f;
                        Character.LeftHandIK.RotationWeight = 0f;
                        Character.LeftHandIK.Enabled = false;
                        Character.LeftHandIK.IkTarget = null;
                        Character.LeftHandIK.Broadcast();
                    }
                    CheckCleanupAnchorsAfterFadeOut();
                });
            }

            float rightCurrent = Mathf.Clamp01(currentRightWeight);
            float rightDuration = duration * rightCurrent;
            if (rightDuration <= 0.001f || Character.RightHandIK == null) {
                currentRightWeight = 0f;
                if (Character?.RightHandIK != null && (proxyRightAnchor == null || Character.RightHandIK.IkTarget == proxyRightAnchor)) {
                    Character.RightHandIK.PositionWeight = 0f;
                    Character.RightHandIK.RotationWeight = 0f;
                    Character.RightHandIK.Enabled = false;
                    Character.RightHandIK.IkTarget = null;
                    Character.RightHandIK.Broadcast();
                }
                CheckCleanupAnchorsAfterFadeOut();
            } else {
                rightWeightTween = DOTween.To(
                    () => currentRightWeight,
                    v => {
                        currentRightWeight = v;
                        if (Character?.RightHandIK != null && Character.RightHandIK.Enabled) {
                            Character.RightHandIK.PositionWeight = v;
                            Character.RightHandIK.RotationWeight = v;
                            if (Character.RightHandIK.BendGoalTarget != null) {
                                Character.RightHandIK.BendGoalWeight = v;
                            }
                            Character.RightHandIK.Broadcast();
                        }
                    },
                    0f,
                    rightDuration
                ).SetEase(ease).OnComplete(() => {
                    currentRightWeight = 0f;
                    if (Character?.RightHandIK != null && (proxyRightAnchor == null || Character.RightHandIK.IkTarget == proxyRightAnchor)) {
                        Character.RightHandIK.PositionWeight = 0f;
                        Character.RightHandIK.RotationWeight = 0f;
                        Character.RightHandIK.Enabled = false;
                        Character.RightHandIK.IkTarget = null;
                        Character.RightHandIK.Broadcast();
                    }
                    CheckCleanupAnchorsAfterFadeOut();
                });
            }
        }

        void CheckCleanupAnchorsAfterFadeOut() {
        }

        void OnUpdateIK() {
            if (!IsEnabled || !EnableHands || Character == null || HasCharacterMismatch()) {
                if (isIkBound) ReleaseCharacterIK();
                return;
            }

            if (!isIkBound) {
                BindCharacterIK();
                return;
            }

            if (bindSettleCountdown > 0) {
                --bindSettleCountdown;
                if (bindSettleCountdown == 0) {
                    FinalizeBindCharacterIK();
                } else {
                    return;
                }
            }

            if (proxyLeftAnchor == null || proxyLeftAnchor.GameObject == null) proxyLeftAnchor = GetProxyLeftAnchor();
            if (proxyRightAnchor == null || proxyRightAnchor.GameObject == null) proxyRightAnchor = GetProxyRightAnchor();

            if (proxyLeftAnchor == null || proxyRightAnchor == null) return;

            // 1. Check live inputs across all registered devices
            DetectDeviceInputs();

            // 2. Arbitrate Hand States
            ArbitrateHandStates();

            // 3. Update Proxy Hand Transforms in World Space
            UpdateProxyTransform(true);
            UpdateProxyTransform(false);

            // 4. Update Character Limb IK Weights
            UpdateCharacterLimbWeights();
        }

        void DetectDeviceInputs() {
            float now = Time.time;

            // Check Mouse Input
            if (MouseReceiver != null && (MouseReceiver.IsEnabled || (IsEnabled && EnableHands))) {
                if (!hasInitializedMouse) {
                    lastMouseX = MouseReceiver.X;
                    lastMouseY = MouseReceiver.Y;
                    lastMouseButton1 = MouseReceiver.Button1;
                    lastMouseButton2 = MouseReceiver.Button2;
                    hasInitializedMouse = true;
                }

                bool hasMouseMoved = MouseReceiver.X != lastMouseX || MouseReceiver.Y != lastMouseY;
                bool hasMouseClicks = MouseReceiver.Button1 || MouseReceiver.Button2;
                // In PointerReceiver packets, Source == 2 is Pen, whereas Mouse is Source != 2 (0 or 1)
                bool isMouseSource = MouseReceiver.Source != 2;

                bool isAnyRestSetup = (MouseReceiver != null && MouseReceiver.IsInRestHandVisualSetup) || (PenReceiver != null && PenReceiver.IsInRestHandVisualSetup);
                if (!isAnyRestSetup && isMouseSource && (hasMouseMoved || hasMouseClicks)) {
                    lastMouseActivityTime = now;
                }
                lastMouseX = MouseReceiver.X;
                lastMouseY = MouseReceiver.Y;
                lastMouseButton1 = MouseReceiver.Button1;
                lastMouseButton2 = MouseReceiver.Button2;
            }

            // Check Pen Input
            if (PenReceiver != null && (PenReceiver.IsEnabled || (IsEnabled && EnableHands))) {
                if (!hasInitializedPen) {
                    lastPenX = PenReceiver.X;
                    lastPenY = PenReceiver.Y;
                    lastPenButton1 = PenReceiver.Button1;
                    lastPenButton2 = PenReceiver.Button2;
                    hasInitializedPen = true;
                }

                bool hasPenMoved = PenReceiver.X != lastPenX || PenReceiver.Y != lastPenY;
                bool hasPenClicks = PenReceiver.Button1 || PenReceiver.Button2;
                bool isPenSource = PenReceiver.Source == 2;

                bool isAnyRestSetup = (MouseReceiver != null && MouseReceiver.IsInRestHandVisualSetup) || (PenReceiver != null && PenReceiver.IsInRestHandVisualSetup);
                if (!isAnyRestSetup && isPenSource && (hasPenMoved || hasPenClicks)) {
                    lastPenActivityTime = now;
                }
                lastPenX = PenReceiver.X;
                lastPenY = PenReceiver.Y;
                lastPenButton1 = PenReceiver.Button1;
                lastPenButton2 = PenReceiver.Button2;
            }

            // Check Keyboard Input
            if (KeyboardReceiver != null && KeyboardReceiver.IsEnabled) {
                bool leftKey = KeyboardReceiver.GetActiveKeyForHand(true, KeyboardReceiver.KeyBindings) != null;
                bool rightKey = KeyboardReceiver.GetActiveKeyForHand(false, KeyboardReceiver.KeyBindings) != null;

                if (CoordinationMode == HandCoordinationMode.NonHybrid && KeyboardReceiver.AnyDown) {
                    leftKey = true;
                    rightKey = true;
                }

                bool isAnyRestSetup = (MouseReceiver != null && MouseReceiver.IsInRestHandVisualSetup) || (PenReceiver != null && PenReceiver.IsInRestHandVisualSetup);
                if (!isAnyRestSetup && leftKey) lastKeyboardLeftTime = now;
                if (!isAnyRestSetup && rightKey) lastKeyboardRightTime = now;
                if (!isAnyRestSetup && (leftKey || rightKey || KeyboardReceiver.AnyDown)) lastKeyboardActivityTime = now;
            }
        }

        CoordinatorHandState GetInactivityReturnState(bool isLeft) {
            switch (ReturnTarget) {
                case InactivityReturnTarget.Mouse:
                    if (MouseReceiver != null && MouseReceiver.IsEnabled && MouseReceiver.IsHandEnabled) {
                        bool isDominant = (isLeft == !MouseReceiver.IsRightHanded);
                        if (isDominant || MouseReceiver.IsRestHandActive || MouseReceiver.IsInRestHandVisualSetup) {
                            return CoordinatorHandState.Mouse;
                        }
                        if (CoordinationMode == HandCoordinationMode.NonHybrid) return CoordinatorHandState.Released;
                    }
                    return CoordinatorHandState.Keyboard;

                case InactivityReturnTarget.Pen:
                    if (PenReceiver != null && PenReceiver.IsEnabled && PenReceiver.IsHandEnabled) {
                        bool isDominant = (isLeft == !PenReceiver.IsRightHanded);
                        if (isDominant || PenReceiver.IsRestHandActive || PenReceiver.IsInRestHandVisualSetup) {
                            return CoordinatorHandState.Pen;
                        }
                        if (CoordinationMode == HandCoordinationMode.NonHybrid) return CoordinatorHandState.Released;
                    }
                    return CoordinatorHandState.Keyboard;

                case InactivityReturnTarget.Keyboard:
                default:
                    return CoordinatorHandState.Keyboard;
            }
        }

        void ArbitrateHandStates() {
            float now = Time.time;

            // Check if active key presses occurred recently (within 0.1s)
            bool keyPressLeft = (now - lastKeyboardLeftTime < 0.1f);
            bool keyPressRight = (now - lastKeyboardRightTime < 0.1f);

            // Check if active mouse or pen movement/clicks occurred recently (within 0.15s)
            bool activeMouse = MouseReceiver != null && MouseReceiver.IsEnabled && MouseReceiver.IsHandEnabled && (now - lastMouseActivityTime < 0.15f);
            bool activePen = PenReceiver != null && PenReceiver.IsEnabled && PenReceiver.IsHandEnabled && (now - lastPenActivityTime < 0.15f);

            bool mouseTimedOut = EnableInactivityReturn && MouseIdleTimeout > 0f && (now - lastMouseActivityTime >= MouseIdleTimeout);
            bool penTimedOut = EnableInactivityReturn && PenIdleTimeout > 0f && (now - lastPenActivityTime >= PenIdleTimeout);
            bool keyboardTimedOut = EnableInactivityReturn && KeyboardIdleTimeout > 0f && (now - lastKeyboardActivityTime >= KeyboardIdleTimeout);

            bool isMouseRight = MouseReceiver != null && MouseReceiver.IsRightHanded;
            bool isPenRight = PenReceiver != null && PenReceiver.IsRightHanded;

            var nextLeft = targetLeftState;
            var nextRight = targetRightState;

            if (CoordinationMode == HandCoordinationMode.Hybrid) {
                // HYBRID MODE (PER-HAND ARBITRATION):
                // 1. Hand that needs to move will move.
                // 2. Hand that does not need to move stays where it was previously (targetLeftState / targetRightState).
                // 3. When coordinator as a whole idles (all active devices finish their countdown), both hands return to ReturnTarget.

                bool mouseClaimsLeft = MouseReceiver != null && MouseReceiver.IsEnabled && MouseReceiver.IsHandEnabled && !isMouseRight;
                bool penClaimsLeft = PenReceiver != null && PenReceiver.IsEnabled && PenReceiver.IsHandEnabled && !isPenRight;

                bool mouseClaimsRight = MouseReceiver != null && MouseReceiver.IsEnabled && MouseReceiver.IsHandEnabled && isMouseRight;
                bool penClaimsRight = PenReceiver != null && PenReceiver.IsEnabled && PenReceiver.IsHandEnabled && isPenRight;

                // Per-hand active input triggers:
                if (keyPressLeft) {
                    nextLeft = CoordinatorHandState.Keyboard;
                } else if (mouseClaimsLeft && activeMouse) {
                    nextLeft = CoordinatorHandState.Mouse;
                } else if (penClaimsLeft && activePen) {
                    nextLeft = CoordinatorHandState.Pen;
                }

                if (keyPressRight) {
                    nextRight = CoordinatorHandState.Keyboard;
                } else if (mouseClaimsRight && activeMouse) {
                    nextRight = CoordinatorHandState.Mouse;
                } else if (penClaimsRight && activePen) {
                    nextRight = CoordinatorHandState.Pen;
                }

                // Coordinator-wide Inactivity Return:
                if (EnableInactivityReturn) {
                    bool mouseConfigured = MouseReceiver != null && MouseReceiver.IsEnabled && MouseReceiver.IsHandEnabled;
                    bool penConfigured = PenReceiver != null && PenReceiver.IsEnabled && PenReceiver.IsHandEnabled;
                    bool keyboardConfigured = KeyboardReceiver != null && KeyboardReceiver.IsEnabled;

                    bool mouseActiveNow = activeMouse;
                    bool penActiveNow = activePen;
                    bool keyboardAnyDown = keyboardConfigured && KeyboardReceiver.AnyDown;
                    bool keyboardActiveNow = keyboardConfigured && (keyboardAnyDown || (now - lastKeyboardActivityTime < 0.15f));

                    bool anyInputActive = mouseActiveNow || penActiveNow || keyboardActiveNow;

                    // Each device counts down independently:
                    bool mouseIdle = !mouseConfigured || MouseIdleTimeout <= 0f || (now - lastMouseActivityTime >= MouseIdleTimeout);
                    bool penIdle = !penConfigured || PenIdleTimeout <= 0f || (now - lastPenActivityTime >= PenIdleTimeout);
                    bool keyboardIdle = !keyboardConfigured || (!keyboardAnyDown && (KeyboardIdleTimeout <= 0f || (now - lastKeyboardActivityTime >= KeyboardIdleTimeout)));

                    bool isCoordinatorIdle = !anyInputActive && mouseIdle && penIdle && keyboardIdle;

                    if (isCoordinatorIdle) {
                        nextLeft = GetInactivityReturnState(true);
                        nextRight = GetInactivityReturnState(false);
                    }
                }
            } else {
                // NON-HYBRID MODE:
                // Mouse/Pen moves active hand and rests other hand (Mouse/Pen or Released).
                // Key press on keyboard brings BOTH hands to keyboard.
                // Idle timer returns BOTH hands to configured ReturnTarget.

                bool anyKey = keyPressLeft || keyPressRight || (KeyboardReceiver != null && KeyboardReceiver.IsEnabled && KeyboardReceiver.AnyDown);

                if (anyKey) {
                    nextLeft = CoordinatorHandState.Keyboard;
                    nextRight = CoordinatorHandState.Keyboard;
                } else if (activePen) {
                    var restActive = PenReceiver != null && (PenReceiver.IsRestHandActive || PenReceiver.IsInRestHandVisualSetup);
                    var restState = restActive ? CoordinatorHandState.Pen : CoordinatorHandState.Released;
                    nextLeft = !isPenRight ? CoordinatorHandState.Pen : restState;
                    nextRight = isPenRight ? CoordinatorHandState.Pen : restState;
                } else if (activeMouse) {
                    var restActive = MouseReceiver != null && (MouseReceiver.IsRestHandActive || MouseReceiver.IsInRestHandVisualSetup);
                    var restState = restActive ? CoordinatorHandState.Mouse : CoordinatorHandState.Released;
                    nextLeft = !isMouseRight ? CoordinatorHandState.Mouse : restState;
                    nextRight = isMouseRight ? CoordinatorHandState.Mouse : restState;
                } else if (EnableInactivityReturn) {
                    bool currentOnMouse = (targetLeftState == CoordinatorHandState.Mouse || targetRightState == CoordinatorHandState.Mouse);
                    bool currentOnPen = (targetLeftState == CoordinatorHandState.Pen || targetRightState == CoordinatorHandState.Pen);
                    bool currentOnKeyboard = (targetLeftState == CoordinatorHandState.Keyboard || targetRightState == CoordinatorHandState.Keyboard);

                    if ((currentOnMouse && mouseTimedOut) || (currentOnPen && penTimedOut) || (currentOnKeyboard && keyboardTimedOut && ReturnTarget != InactivityReturnTarget.Keyboard)) {
                        nextLeft = GetInactivityReturnState(true);
                        nextRight = GetInactivityReturnState(false);
                    }
                }
            }

            // If currently in visual setup mode on pointer rest hand, lock both hands immediately
            if (MouseReceiver != null && MouseReceiver.IsInRestHandVisualSetup) {
                nextLeft = CoordinatorHandState.Mouse;
                nextRight = CoordinatorHandState.Mouse;
                if (targetLeftState != CoordinatorHandState.Mouse) {
                    leftProgressTween?.Kill();
                    leftWeightTween?.Kill();
                    isLeftTransitioning = false;
                    leftTransitionProgress = 1f;
                    currentLeftState = CoordinatorHandState.Mouse;
                    currentLeftWeight = 1.0f;
                }
                if (targetRightState != CoordinatorHandState.Mouse) {
                    rightProgressTween?.Kill();
                    rightWeightTween?.Kill();
                    isRightTransitioning = false;
                    rightTransitionProgress = 1f;
                    currentRightState = CoordinatorHandState.Mouse;
                    currentRightWeight = 1.0f;
                }
            } else if (PenReceiver != null && PenReceiver.IsInRestHandVisualSetup) {
                nextLeft = CoordinatorHandState.Pen;
                nextRight = CoordinatorHandState.Pen;
                if (targetLeftState != CoordinatorHandState.Pen) {
                    leftProgressTween?.Kill();
                    leftWeightTween?.Kill();
                    isLeftTransitioning = false;
                    leftTransitionProgress = 1f;
                    currentLeftState = CoordinatorHandState.Pen;
                    currentLeftWeight = 1.0f;
                }
                if (targetRightState != CoordinatorHandState.Pen) {
                    rightProgressTween?.Kill();
                    rightWeightTween?.Kill();
                    isRightTransitioning = false;
                    rightTransitionProgress = 1f;
                    currentRightState = CoordinatorHandState.Pen;
                    currentRightWeight = 1.0f;
                }
            }

            // Commit both target states before starting either transition so rest resolution is never stale
            bool leftChanged = (nextLeft != targetLeftState);
            bool rightChanged = (nextRight != targetRightState);

            targetLeftState = nextLeft;
            targetRightState = nextRight;

            if (leftChanged) {
                StartHandTransition(true, nextLeft);
            }
            if (rightChanged) {
                StartHandTransition(false, nextRight);
            }
        }

        void StartHandTransition(bool isLeft, CoordinatorHandState targetState) {
            CoordinatorHandState currentState = isLeft ? currentLeftState : currentRightState;
            Transition chosenTransition;
            float delay = 0f;

            if (targetState == CoordinatorHandState.Released) {
                chosenTransition = InactiveTransition ?? new InactiveTransition();
            } else if (currentState == CoordinatorHandState.Released) {
                chosenTransition = ActiveTransition ?? new ActiveTransition();
                delay = (ActiveTransition != null) ? ActiveTransition.Delay : 0.1f;
            } else {
                chosenTransition = SwitchTransition ?? new Transition { Time = 0.2f, Ease = Ease.OutQuad };
            }

            float duration = chosenTransition.Time;
            var ease = chosenTransition.Ease;

            var proxy = isLeft ? proxyLeftAnchor : proxyRightAnchor;
            if (proxy == null) return;

            Vector3 startPos = proxy.GameObject.transform.position;
            Vector3 startRot = proxy.GameObject.transform.eulerAngles;

            float currentWeight = isLeft ? currentLeftWeight : currentRightWeight;
            if (currentWeight <= 0.05f && Character != null && Character.GameObject != null) {
                var anim = Character.GameObject.GetComponentInChildren<Animator>();
                if (anim != null) {
                    var bone = anim.GetBoneTransform(isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
                    if (bone != null) {
                        startPos = bone.position;
                        startRot = bone.eulerAngles;
                    }
                }
            }

            GetTargetWorldPose(isLeft, targetState, out _, out _, out float destWeight);

            if (isLeft) {
                leftProgressTween?.Kill();
                leftWeightTween?.Kill();

                leftTransitionStartPos = startPos;
                leftTransitionStartRot = startRot;

                if (duration <= 0f && delay <= 0f) {
                    isLeftTransitioning = false;
                    leftTransitionProgress = 1f;
                    currentLeftState = targetState;
                    currentLeftWeight = destWeight;
                    GetTargetWorldPose(true, targetState, out Vector3 destPos, out Vector3 destRot, out _);
                    ApplyProxyWorldPose(proxy, destPos, destRot);
                    OnTransitionCompleted(true, targetState);
                    return;
                }

                isLeftTransitioning = true;
                leftTransitionProgress = 0f;

                float actualDuration = duration > 0f ? duration : 0.001f;
                leftProgressTween = DOTween.To(() => leftTransitionProgress, v => leftTransitionProgress = v, 1.0f, actualDuration)
                    .SetEase(ease)
                    .SetDelay(delay)
                    .OnComplete(() => {
                        isLeftTransitioning = false;
                        leftTransitionProgress = 1.0f;
                        currentLeftState = targetState;
                        OnTransitionCompleted(true, targetState);
                    });

                leftWeightTween = DOTween.To(() => currentLeftWeight, v => {
                    currentLeftWeight = v;
                    if (Character?.LeftHandIK != null && Character.LeftHandIK.Enabled) {
                        Character.LeftHandIK.PositionWeight = v;
                        Character.LeftHandIK.RotationWeight = v;
                        if (Character.LeftHandIK.BendGoalTarget != null) {
                            Character.LeftHandIK.BendGoalWeight = v;
                        }
                        Character.LeftHandIK.Broadcast();
                    }
                }, destWeight, actualDuration)
                .SetEase(ease)
                .SetDelay(delay);
            } else {
                rightProgressTween?.Kill();
                rightWeightTween?.Kill();

                rightTransitionStartPos = startPos;
                rightTransitionStartRot = startRot;

                if (duration <= 0f && delay <= 0f) {
                    isRightTransitioning = false;
                    rightTransitionProgress = 1f;
                    currentRightState = targetState;
                    currentRightWeight = destWeight;
                    GetTargetWorldPose(false, targetState, out Vector3 destPos, out Vector3 destRot, out _);
                    ApplyProxyWorldPose(proxy, destPos, destRot);
                    OnTransitionCompleted(false, targetState);
                    return;
                }

                isRightTransitioning = true;
                rightTransitionProgress = 0f;

                float actualDuration = duration > 0f ? duration : 0.001f;
                rightProgressTween = DOTween.To(() => rightTransitionProgress, v => rightTransitionProgress = v, 1.0f, actualDuration)
                    .SetEase(ease)
                    .SetDelay(delay)
                    .OnComplete(() => {
                        isRightTransitioning = false;
                        rightTransitionProgress = 1.0f;
                        currentRightState = targetState;
                        OnTransitionCompleted(false, targetState);
                    });

                rightWeightTween = DOTween.To(() => currentRightWeight, v => {
                    currentRightWeight = v;
                    if (Character?.RightHandIK != null && Character.RightHandIK.Enabled) {
                        Character.RightHandIK.PositionWeight = v;
                        Character.RightHandIK.RotationWeight = v;
                        if (Character.RightHandIK.BendGoalTarget != null) {
                            Character.RightHandIK.BendGoalWeight = v;
                        }
                        Character.RightHandIK.Broadcast();
                    }
                }, destWeight, actualDuration)
                .SetEase(ease)
                .SetDelay(delay);
            }
        }

        void UpdateProxyTransform(bool isLeft) {
            var proxy = isLeft ? proxyLeftAnchor : proxyRightAnchor;
            if (proxy == null) return;

            bool isTransitioning = isLeft ? isLeftTransitioning : isRightTransitioning;
            var activeState = isLeft ? currentLeftState : currentRightState;
            var targetState = isLeft ? targetLeftState : targetRightState;

            if (activeState == CoordinatorHandState.Mouse && MouseReceiver != null && MouseReceiver.IsInRestHandVisualSetup) {
                isTransitioning = false;
            } else if (activeState == CoordinatorHandState.Pen && PenReceiver != null && PenReceiver.IsInRestHandVisualSetup) {
                isTransitioning = false;
            }

            GetTargetWorldPose(isLeft, isTransitioning ? targetState : activeState, out Vector3 liveDestPos, out Vector3 liveDestRot, out float weight);

            if (isTransitioning) {
                float progress = Mathf.Clamp01(isLeft ? leftTransitionProgress : rightTransitionProgress);
                Vector3 startPos = isLeft ? leftTransitionStartPos : rightTransitionStartPos;
                Vector3 startRot = isLeft ? leftTransitionStartRot : rightTransitionStartRot;

                if (weight <= 0f) {
                    // When transitioning to Released, target has no physical anchor; hold starting pose while weight fades out
                    ApplyProxyWorldPose(proxy, startPos, startRot);
                } else {
                    // Dynamically interpolate toward the LIVE destination pose
                    Vector3 unwrappedLiveRot = new Vector3(
                        startRot.x + Mathf.DeltaAngle(startRot.x, liveDestRot.x),
                        startRot.y + Mathf.DeltaAngle(startRot.y, liveDestRot.y),
                        startRot.z + Mathf.DeltaAngle(startRot.z, liveDestRot.z)
                    );
                    Vector3 currentPos = Vector3.Lerp(startPos, liveDestPos, progress);
                    Vector3 currentRot = Vector3.Lerp(startRot, unwrappedLiveRot, progress);
                    ApplyProxyWorldPose(proxy, currentPos, currentRot);
                }
            } else {
                // Settled in state: lock 1:1 to live target world position and rotation in real time
                ApplyProxyWorldPose(proxy, liveDestPos, liveDestRot);
                bool isWeightTweening = isLeft
                    ? (leftWeightTween != null && leftWeightTween.IsActive() && leftWeightTween.IsPlaying())
                    : (rightWeightTween != null && rightWeightTween.IsActive() && rightWeightTween.IsPlaying());
                if (!isWeightTweening) {
                    if (isLeft) currentLeftWeight = weight;
                    else currentRightWeight = weight;
                }
            }
        }

        bool TryGetAnchorWorldPose(AnchorAsset anchor, out Vector3 worldPos, out Vector3 worldRot) {
            worldPos = Vector3.zero;
            worldRot = Vector3.zero;
            if (anchor == null) return false;

            if (TryResolveAnchorWorldPoseRecursive(anchor, 0, out worldPos, out Quaternion quatRot)) {
                worldRot = quatRot.eulerAngles;
                return true;
            }

            if (anchor.GameObject != null && anchor.GameObject.transform.position != Vector3.zero) {
                worldPos = anchor.GameObject.transform.position;
                worldRot = anchor.GameObject.transform.eulerAngles;
                return true;
            }

            return false;
        }

        bool TryResolveAnchorWorldPoseRecursive(AnchorAsset anchor, int depth, out Vector3 worldPos, out Quaternion worldRot) {
            worldPos = Vector3.zero;
            worldRot = Quaternion.identity;
            if (anchor == null || depth > 8) return false;

            Vector3 localPos = anchor.Transform.Position;
            Quaternion localRot = Quaternion.Euler(anchor.Transform.Rotation);

            var parent = anchor.Attachable?.Parent;
            if (parent == null) {
                worldPos = localPos;
                worldRot = localRot;
                return true;
            }

            if (parent is AnchorAsset parentAnchor) {
                if (TryResolveAnchorWorldPoseRecursive(parentAnchor, depth + 1, out Vector3 pPos, out Quaternion pRot)) {
                    worldPos = pPos + pRot * localPos;
                    worldRot = pRot * localRot;
                    return true;
                }
            } else if (parent is CharacterAsset character) {
                if (anchor.Attachable != null && anchor.Attachable.AttachType == Warudo.Plugins.Core.Assets.Mixins.AttachType.HumanBodyBone) {
                    var bone = anchor.Attachable.AttachToBone;
                    Transform boneTransform = character.Animator != null ? character.Animator.GetBoneTransform(bone) : null;
                    if (boneTransform != null) {
                        worldPos = boneTransform.position + boneTransform.rotation * localPos;
                        worldRot = boneTransform.rotation * localRot;
                        return true;
                    }
                }
                Transform charTransform = character.GameObject != null ? character.GameObject.transform : null;
                Vector3 pPos = charTransform != null ? charTransform.position : character.Transform.Position;
                Quaternion pRot = charTransform != null ? charTransform.rotation : Quaternion.Euler(character.Transform.Rotation);
                worldPos = pPos + pRot * localPos;
                worldRot = pRot * localRot;
                return true;
            } else if (parent is GameObjectAsset goa) {
                Transform goaTransform = goa.GameObject != null ? goa.GameObject.transform : null;
                Vector3 pPos = goaTransform != null ? goaTransform.position : goa.Transform.Position;
                Quaternion pRot = goaTransform != null ? goaTransform.rotation : Quaternion.Euler(goa.Transform.Rotation);
                worldPos = pPos + pRot * localPos;
                worldRot = pRot * localRot;
                return true;
            }

            return false;
        }

        void GetTargetWorldPose(bool isLeft, CoordinatorHandState state, out Vector3 worldPos, out Vector3 worldRot, out float weight) {
            Transform handBone = null;
            if (Character != null && Character.Animator != null) {
                handBone = Character.Animator.GetBoneTransform(isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            }
            worldPos = handBone != null ? handBone.position : (Character != null && Character.GameObject != null ? Character.GameObject.transform.position + (isLeft ? new Vector3(-0.2f, 1f, 0.2f) : new Vector3(0.2f, 1f, 0.2f)) : Vector3.zero);
            worldRot = handBone != null ? handBone.eulerAngles : (isLeft ? new Vector3(0, 90f, 0) : new Vector3(0, -90f, 0));
            weight = 1f;

            switch (state) {
                case CoordinatorHandState.Keyboard:
                    if (KeyboardReceiver != null) {
                        var anchor = isLeft ? KeyboardReceiver.LeftHandAnchor : KeyboardReceiver.RightHandAnchor;
                        if (TryGetAnchorWorldPose(anchor, out worldPos, out worldRot)) {
                            return;
                        }
                    }
                    weight = 0f;
                    break;

                case CoordinatorHandState.Mouse:
                    if (MouseReceiver != null && MouseReceiver.IsEnabled && MouseReceiver.IsHandEnabled) {
                        bool isDominant = (isLeft == !MouseReceiver.IsRightHanded);
                        if (isDominant) {
                            if (TryGetAnchorWorldPose(MouseReceiver.HandAnchor, out worldPos, out worldRot)) {
                                weight = 1f;
                                return;
                            }
                        } else {
                            if (MouseReceiver.IsRestHandActive || MouseReceiver.IsInRestHandVisualSetup) {
                                if (TryGetAnchorWorldPose(MouseReceiver.RestHandAnchor, out worldPos, out worldRot)) {
                                    weight = 1f;
                                    return;
                                }
                            }
                        }
                    }
                    weight = 0f;
                    break;

                case CoordinatorHandState.Pen:
                    if (PenReceiver != null && PenReceiver.IsEnabled && PenReceiver.IsHandEnabled) {
                        bool isDominant = (isLeft == !PenReceiver.IsRightHanded);
                        if (isDominant) {
                            if (TryGetAnchorWorldPose(PenReceiver.HandAnchor, out worldPos, out worldRot)) {
                                weight = 1f;
                                return;
                            }
                        } else {
                            if (PenReceiver.IsRestHandActive || PenReceiver.IsInRestHandVisualSetup) {
                                if (TryGetAnchorWorldPose(PenReceiver.RestHandAnchor, out worldPos, out worldRot)) {
                                    weight = 1f;
                                    return;
                                }
                            }
                        }
                    }
                    weight = 0f;
                    break;

                case CoordinatorHandState.Released:
                    weight = 0f;
                    break;
            }
        }

        PointerReceiverAsset GetPointerRestDevice(bool isLeft, CoordinatorHandState state) {
            if (state == CoordinatorHandState.Mouse && MouseReceiver != null && MouseReceiver.IsEnabled && MouseReceiver.IsHandEnabled) {
                bool isNonDominant = (isLeft == MouseReceiver.IsRightHanded);
                if (isNonDominant && (MouseReceiver.IsRestHandActive || MouseReceiver.IsInRestHandVisualSetup)) {
                    return MouseReceiver;
                }
            } else if (state == CoordinatorHandState.Pen && PenReceiver != null && PenReceiver.IsEnabled && PenReceiver.IsHandEnabled) {
                bool isNonDominant = (isLeft == PenReceiver.IsRightHanded);
                if (isNonDominant && (PenReceiver.IsRestHandActive || PenReceiver.IsInRestHandVisualSetup)) {
                    return PenReceiver;
                }
            }
            return null;
        }

        void ApplyProxyWorldPose(AnchorAsset proxy, Vector3 worldPos, Vector3 worldRot) {
            if (proxy == null || proxy.GameObject == null) return;

            // In Warudo, AnchorAsset with Parent == null has Transform.Position and Rotation as world space
            proxy.Transform.Position = worldPos;
            proxy.Transform.Rotation = worldRot;
            proxy.Transform.Broadcast();
            if (proxy.GameObject != null) {
                proxy.GameObject.transform.position = worldPos;
                proxy.GameObject.transform.rotation = Quaternion.Euler(worldRot);
            }
        }

        void UpdateCharacterLimbWeights() {
            if (Character == null) return;

            if (Character.LeftHandIK != null && Character.LeftHandIK.Enabled) {
                Character.LeftHandIK.PositionWeight = currentLeftWeight;
                Character.LeftHandIK.RotationWeight = currentLeftWeight;
                var restPointer = GetPointerRestDevice(true, currentLeftState);
                if (restPointer != null && restPointer.RestHandPoleAnchor != null && restPointer.RestPoleWeight > 0.001f) {
                    Character.LeftHandIK.BendGoalTarget = restPointer.RestHandPoleAnchor;
                    Character.LeftHandIK.BendGoalWeight = currentLeftWeight * restPointer.RestPoleWeight;
                } else if (Character.LeftHandIK.BendGoalTarget != null) {
                    Character.LeftHandIK.BendGoalWeight = 0f;
                    Character.LeftHandIK.BendGoalTarget = null;
                }
                Character.LeftHandIK.Broadcast();
            }

            if (Character.RightHandIK != null && Character.RightHandIK.Enabled) {
                Character.RightHandIK.PositionWeight = currentRightWeight;
                Character.RightHandIK.RotationWeight = currentRightWeight;
                var restPointer = GetPointerRestDevice(false, currentRightState);
                if (restPointer != null && restPointer.RestHandPoleAnchor != null && restPointer.RestPoleWeight > 0.001f) {
                    Character.RightHandIK.BendGoalTarget = restPointer.RestHandPoleAnchor;
                    Character.RightHandIK.BendGoalWeight = currentRightWeight * restPointer.RestPoleWeight;
                } else if (Character.RightHandIK.BendGoalTarget != null) {
                    Character.RightHandIK.BendGoalWeight = 0f;
                    Character.RightHandIK.BendGoalTarget = null;
                }
                Character.RightHandIK.Broadcast();
            }
        }
    }
}
