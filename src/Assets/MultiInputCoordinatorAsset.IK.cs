using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Localization;
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

        Tween leftPosTween;
        Tween leftRotTween;
        Tween leftWeightTween;
        bool isLeftTransitioning;
        float currentLeftWeight = 1f;

        Tween rightPosTween;
        Tween rightRotTween;
        Tween rightWeightTween;
        bool isRightTransitioning;
        float currentRightWeight = 1f;

        Vector3 tweenLeftPos;
        Vector3 tweenLeftRot;
        Vector3 tweenRightPos;
        Vector3 tweenRightRot;

        void OnCreateIK() {
            proxyLeftAnchor = GetProxyLeftAnchor();
            proxyRightAnchor = GetProxyRightAnchor();
        }

        void OnDestroyIK() {
            KillAllTweens();
        }

        void KillAllTweens() {
            leftPosTween?.Kill();
            leftRotTween?.Kill();
            leftWeightTween?.Kill();
            rightPosTween?.Kill();
            rightRotTween?.Kill();
            rightWeightTween?.Kill();
        }

        public AnchorAsset GetProxyLeftAnchor(bool skipAutoCreate = false) {
            return GetAsset<AnchorAsset>(ref ProxyLeftHandAnchorAssetId, skipAutoCreate, newAssetName: "🔒⚓-🔥🎯 Left Proxy Hand IK");
        }

        public AnchorAsset GetProxyRightAnchor(bool skipAutoCreate = false) {
            return GetAsset<AnchorAsset>(ref ProxyRightHandAnchorAssetId, skipAutoCreate, newAssetName: "🔒⚓-🔥🎯 Right Proxy Hand IK");
        }

        T GetAsset<T>(ref Guid assetId, bool skipAutoCreate = false, string newAssetName = null) where T : Warudo.Core.Scenes.Asset {
            if (Scene == null) return null;
            var category = (!string.IsNullOrEmpty(newAssetName) && newAssetName.StartsWith("🔒"))
                ? "FS_ASSET_CATEGORY_INTERNAL".Localized()
                : "FS_ASSET_CATEGORY_INPUT".Localized();
            if (assetId != Guid.Empty) {
                var id = assetId;
                var fetchResult = Scene.GetAssets<T>().FirstOrDefault(p => p.Id == id);
                if (fetchResult != null) {
                    try { Scene.AddAssetToGroup(fetchResult, category); } catch {}
                    return fetchResult;
                }
            }
            if (skipAutoCreate) return null;
            var newAsset = Scene.AddAssetToGroup<T>(category);
            if (newAssetName != null) newAsset.Name = newAssetName;
            Scene.UpdateNewAssetName(newAsset);
            newAsset.Broadcast();
            assetId = newAsset.Id;
            return newAsset;
        }

        void EnsureReceiversRegistered() {
            if (!IsEnabled) return;

            if (KeyboardReceiver != null && KeyboardReceiver.ActiveCoordinator != this) {
                KeyboardReceiver.RegisterCoordinator(this);
            }
            if (MouseReceiver != null && MouseReceiver.ActiveCoordinator != this) {
                MouseReceiver.RegisterCoordinator(this);
            }
            if (PenReceiver != null && PenReceiver.ActiveCoordinator != this) {
                PenReceiver.RegisterCoordinator(this);
            }
        }

        void RegisterAllReceivers() {
            KeyboardReceiver?.RegisterCoordinator(this);
            MouseReceiver?.RegisterCoordinator(this);
            PenReceiver?.RegisterCoordinator(this);
        }

        void UnregisterAllReceivers() {
            KeyboardReceiver?.UnregisterCoordinator(this);
            MouseReceiver?.UnregisterCoordinator(this);
            PenReceiver?.UnregisterCoordinator(this);
        }

        void BindCharacterIK() {
            if (Character == null) return;

            proxyLeftAnchor = GetProxyLeftAnchor();
            proxyRightAnchor = GetProxyRightAnchor();

            if (Character.LeftHandIK != null && proxyLeftAnchor != null) {
                Character.LeftHandIK.Enabled = true;
                Character.LeftHandIK.IkTarget = proxyLeftAnchor;
                Character.LeftHandIK.PositionWeight = currentLeftWeight;
                Character.LeftHandIK.RotationWeight = currentLeftWeight;
                Character.LeftHandIK.Broadcast();
            }

            if (Character.RightHandIK != null && proxyRightAnchor != null) {
                Character.RightHandIK.Enabled = true;
                Character.RightHandIK.IkTarget = proxyRightAnchor;
                Character.RightHandIK.PositionWeight = currentRightWeight;
                Character.RightHandIK.RotationWeight = currentRightWeight;
                Character.RightHandIK.Broadcast();
            }
        }

        void ReleaseCharacterIK() {
            if (Character == null) return;
            KillAllTweens();

            if (Character.LeftHandIK != null && Character.LeftHandIK.IkTarget == proxyLeftAnchor) {
                Character.LeftHandIK.Enabled = false;
                Character.LeftHandIK.IkTarget = null;
                Character.LeftHandIK.Broadcast();
            }

            if (Character.RightHandIK != null && Character.RightHandIK.IkTarget == proxyRightAnchor) {
                Character.RightHandIK.Enabled = false;
                Character.RightHandIK.IkTarget = null;
                Character.RightHandIK.Broadcast();
            }
        }

        void OnUpdateIK() {
            if (!IsEnabled || Character == null) return;

            proxyLeftAnchor = GetProxyLeftAnchor();
            proxyRightAnchor = GetProxyRightAnchor();

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
            if (MouseReceiver != null && MouseReceiver.IsEnabled) {
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

                bool isMouseRestSetup = MouseReceiver.IsInRestHandVisualSetup;
                if (!isMouseRestSetup && isMouseSource && (hasMouseMoved || hasMouseClicks)) {
                    lastMouseActivityTime = now;
                }
                lastMouseX = MouseReceiver.X;
                lastMouseY = MouseReceiver.Y;
                lastMouseButton1 = MouseReceiver.Button1;
                lastMouseButton2 = MouseReceiver.Button2;
            }

            // Check Pen Input
            if (PenReceiver != null && PenReceiver.IsEnabled) {
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

                bool isPenRestSetup = PenReceiver.IsInRestHandVisualSetup;
                if (!isPenRestSetup && isPenSource && (hasPenMoved || hasPenClicks)) {
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

                if (leftKey) lastKeyboardLeftTime = now;
                if (rightKey) lastKeyboardRightTime = now;
            }
        }

        void ArbitrateHandStates() {
            float now = Time.time;

            // Check if active key presses occurred recently (within 0.1s)
            bool keyPressLeft = (now - lastKeyboardLeftTime < 0.1f);
            bool keyPressRight = (now - lastKeyboardRightTime < 0.1f);

            // Check if active mouse or pen movement/clicks occurred recently (within 0.15s)
            bool activeMouse = MouseReceiver != null && MouseReceiver.IsEnabled && (now - lastMouseActivityTime < 0.15f);
            bool activePen = PenReceiver != null && PenReceiver.IsEnabled && (now - lastPenActivityTime < 0.15f);

            bool mouseTimedOut = EnableInactivityReturn && MouseReturnTimeout > 0f && (now - lastMouseActivityTime >= MouseReturnTimeout);
            bool penTimedOut = EnableInactivityReturn && PenReturnTimeout > 0f && (now - lastPenActivityTime >= PenReturnTimeout);

            bool isMouseRight = MouseReceiver != null && MouseReceiver.IsRightHanded;
            bool isPenRight = PenReceiver != null && PenReceiver.IsRightHanded;

            var nextLeft = targetLeftState;
            var nextRight = targetRightState;

            if (CoordinationMode == HandCoordinationMode.Hybrid) {
                // HYBRID MODE:
                // Mouse/Pen moves active hand; other hand stays on keyboard shortcuts.
                // Key press moves only the hand of that key.
                // Idle timer returns both hands to keyboard.

                bool mouseClaimsLeft = MouseReceiver != null && MouseReceiver.IsEnabled && !isMouseRight;
                bool penClaimsLeft = PenReceiver != null && PenReceiver.IsEnabled && !isPenRight;

                if (keyPressLeft) {
                    nextLeft = CoordinatorHandState.Keyboard;
                } else if (mouseClaimsLeft && activeMouse) {
                    nextLeft = CoordinatorHandState.Mouse;
                } else if (penClaimsLeft && activePen) {
                    nextLeft = CoordinatorHandState.Pen;
                } else if ((mouseClaimsLeft && mouseTimedOut) || (penClaimsLeft && penTimedOut)) {
                    nextLeft = CoordinatorHandState.Keyboard;
                } else if (!mouseClaimsLeft && !penClaimsLeft) {
                    nextLeft = CoordinatorHandState.Keyboard;
                }

                bool mouseClaimsRight = MouseReceiver != null && MouseReceiver.IsEnabled && isMouseRight;
                bool penClaimsRight = PenReceiver != null && PenReceiver.IsEnabled && isPenRight;

                if (keyPressRight) {
                    nextRight = CoordinatorHandState.Keyboard;
                } else if (mouseClaimsRight && activeMouse) {
                    nextRight = CoordinatorHandState.Mouse;
                } else if (penClaimsRight && activePen) {
                    nextRight = CoordinatorHandState.Pen;
                } else if ((mouseClaimsRight && mouseTimedOut) || (penClaimsRight && penTimedOut)) {
                    nextRight = CoordinatorHandState.Keyboard;
                } else if (!mouseClaimsRight && !penClaimsRight) {
                    nextRight = CoordinatorHandState.Keyboard;
                }
            } else {
                // NON-HYBRID MODE:
                // Mouse/Pen moves active hand and rests other hand (PointerRest or Released).
                // Key press on keyboard brings BOTH hands to keyboard.
                // Idle timer returns BOTH hands to keyboard.

                bool anyKey = keyPressLeft || keyPressRight || (KeyboardReceiver != null && KeyboardReceiver.IsEnabled && KeyboardReceiver.AnyDown);

                if (anyKey) {
                    nextLeft = CoordinatorHandState.Keyboard;
                    nextRight = CoordinatorHandState.Keyboard;
                } else if (activePen) {
                    var restActive = PenReceiver != null && (PenReceiver.IsRestHandActive || PenReceiver.IsInRestHandVisualSetup);
                    var restState = restActive ? CoordinatorHandState.PointerRest : CoordinatorHandState.Released;
                    nextLeft = !isPenRight ? CoordinatorHandState.Pen : restState;
                    nextRight = isPenRight ? CoordinatorHandState.Pen : restState;
                } else if (activeMouse) {
                    var restActive = MouseReceiver != null && (MouseReceiver.IsRestHandActive || MouseReceiver.IsInRestHandVisualSetup);
                    var restState = restActive ? CoordinatorHandState.PointerRest : CoordinatorHandState.Released;
                    nextLeft = !isMouseRight ? CoordinatorHandState.Mouse : restState;
                    nextRight = isMouseRight ? CoordinatorHandState.Mouse : restState;
                } else if (EnableInactivityReturn) {
                    bool currentOnMouse = (targetLeftState == CoordinatorHandState.Mouse || targetRightState == CoordinatorHandState.Mouse);
                    bool currentOnPen = (targetLeftState == CoordinatorHandState.Pen || targetRightState == CoordinatorHandState.Pen);

                    if ((currentOnMouse && mouseTimedOut) || (currentOnPen && penTimedOut)) {
                        nextLeft = CoordinatorHandState.Keyboard;
                        nextRight = CoordinatorHandState.Keyboard;
                    }
                }

                // If currently in visual setup mode on pointer rest hand, force rest hand into PointerRest immediately
                if (MouseReceiver != null && MouseReceiver.IsInRestHandVisualSetup) {
                    if (isMouseRight) {
                        nextLeft = CoordinatorHandState.PointerRest;
                        if (targetLeftState != CoordinatorHandState.PointerRest) {
                            leftPosTween?.Kill();
                            leftRotTween?.Kill();
                            leftWeightTween?.Kill();
                            isLeftTransitioning = false;
                            currentLeftState = CoordinatorHandState.PointerRest;
                            currentLeftWeight = 1.0f;
                        }
                    } else {
                        nextRight = CoordinatorHandState.PointerRest;
                        if (targetRightState != CoordinatorHandState.PointerRest) {
                            rightPosTween?.Kill();
                            rightRotTween?.Kill();
                            rightWeightTween?.Kill();
                            isRightTransitioning = false;
                            currentRightState = CoordinatorHandState.PointerRest;
                            currentRightWeight = 1.0f;
                        }
                    }
                } else if (PenReceiver != null && PenReceiver.IsInRestHandVisualSetup) {
                    if (isPenRight) {
                        nextLeft = CoordinatorHandState.PointerRest;
                        if (targetLeftState != CoordinatorHandState.PointerRest) {
                            leftPosTween?.Kill();
                            leftRotTween?.Kill();
                            leftWeightTween?.Kill();
                            isLeftTransitioning = false;
                            currentLeftState = CoordinatorHandState.PointerRest;
                            currentLeftWeight = 1.0f;
                        }
                    } else {
                        nextRight = CoordinatorHandState.PointerRest;
                        if (targetRightState != CoordinatorHandState.PointerRest) {
                            rightPosTween?.Kill();
                            rightRotTween?.Kill();
                            rightWeightTween?.Kill();
                            isRightTransitioning = false;
                            currentRightState = CoordinatorHandState.PointerRest;
                            currentRightWeight = 1.0f;
                        }
                    }
                }
            }

            // Trigger left transition if changed
            if (nextLeft != targetLeftState) {
                targetLeftState = nextLeft;
                StartHandTransition(true, nextLeft);
            }

            // Trigger right transition if changed
            if (nextRight != targetRightState) {
                targetRightState = nextRight;
                StartHandTransition(false, nextRight);
            }
        }

        void StartHandTransition(bool isLeft, CoordinatorHandState targetState) {
            var trans = Transition ?? new ActiveTransition();
            float duration = trans.Time;
            var ease = trans.Ease;

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

            GetTargetWorldPose(isLeft, targetState, out Vector3 destPos, out Vector3 destRot, out float destWeight);

            if (destWeight <= 0f) {
                destPos = startPos;
                destRot = startRot;
            }

            if (isLeft) {
                leftPosTween?.Kill();
                leftRotTween?.Kill();
                leftWeightTween?.Kill();

                if (duration <= 0f) {
                    isLeftTransitioning = false;
                    currentLeftState = targetState;
                    currentLeftWeight = destWeight;
                    tweenLeftPos = destPos;
                    tweenLeftRot = destRot;
                    ApplyProxyWorldPose(proxy, destPos, destRot);
                    OnTransitionCompleted(true, targetState);
                    return;
                }

                isLeftTransitioning = true;
                tweenLeftPos = startPos;
                tweenLeftRot = startRot;

                leftPosTween = DOTween.To(() => tweenLeftPos, v => tweenLeftPos = v, destPos, duration).SetEase(ease);
                leftRotTween = DOTween.To(() => tweenLeftRot, v => tweenLeftRot = v, destRot, duration).SetEase(ease);
                leftWeightTween = DOTween.To(() => currentLeftWeight, v => currentLeftWeight = v, destWeight, duration)
                    .SetEase(ease)
                    .OnComplete(() => {
                        isLeftTransitioning = false;
                        currentLeftState = targetState;
                        OnTransitionCompleted(true, targetState);
                    });
            } else {
                rightPosTween?.Kill();
                rightRotTween?.Kill();
                rightWeightTween?.Kill();

                if (duration <= 0f) {
                    isRightTransitioning = false;
                    currentRightState = targetState;
                    currentRightWeight = destWeight;
                    tweenRightPos = destPos;
                    tweenRightRot = destRot;
                    ApplyProxyWorldPose(proxy, destPos, destRot);
                    OnTransitionCompleted(false, targetState);
                    return;
                }

                isRightTransitioning = true;
                tweenRightPos = startPos;
                tweenRightRot = startRot;

                rightPosTween = DOTween.To(() => tweenRightPos, v => tweenRightPos = v, destPos, duration).SetEase(ease);
                rightRotTween = DOTween.To(() => tweenRightRot, v => tweenRightRot = v, destRot, duration).SetEase(ease);
                rightWeightTween = DOTween.To(() => currentRightWeight, v => currentRightWeight = v, destWeight, duration)
                    .SetEase(ease)
                    .OnComplete(() => {
                        isRightTransitioning = false;
                        currentRightState = targetState;
                        OnTransitionCompleted(false, targetState);
                    });
            }
        }

        void UpdateProxyTransform(bool isLeft) {
            var proxy = isLeft ? proxyLeftAnchor : proxyRightAnchor;
            if (proxy == null) return;

            bool isTransitioning = isLeft ? isLeftTransitioning : isRightTransitioning;
            var activeState = isLeft ? currentLeftState : currentRightState;

            if (activeState == CoordinatorHandState.PointerRest) {
                var activePointer = GetActiveRestPointer(isLeft);
                if (activePointer != null && activePointer.IsInRestHandVisualSetup) {
                    isTransitioning = false;
                }
            }

            if (isTransitioning) {
                // During tween transition, follow interpolated world position and rotation
                ApplyProxyWorldPose(proxy, isLeft ? tweenLeftPos : tweenRightPos, isLeft ? tweenLeftRot : tweenRightRot);
            } else {
                // Settled in state: lock 1:1 to target world position and rotation in real time
                GetTargetWorldPose(isLeft, activeState, out Vector3 worldPos, out Vector3 worldRot, out float weight);
                ApplyProxyWorldPose(proxy, worldPos, worldRot);
                if (isLeft) currentLeftWeight = weight;
                else currentRightWeight = weight;
            }
        }

        void GetTargetWorldPose(bool isLeft, CoordinatorHandState state, out Vector3 worldPos, out Vector3 worldRot, out float weight) {
            worldPos = Vector3.zero;
            worldRot = isLeft ? new Vector3(0, 90f, 0) : new Vector3(0, -90f, 0);
            weight = 1f;

            switch (state) {
                case CoordinatorHandState.Keyboard:
                    if (KeyboardReceiver != null) {
                        var anchor = isLeft ? KeyboardReceiver.LeftHandAnchor : KeyboardReceiver.RightHandAnchor;
                        if (anchor != null && anchor.GameObject != null) {
                            worldPos = anchor.GameObject.transform.position;
                            worldRot = anchor.GameObject.transform.eulerAngles;
                            return;
                        }
                    }
                    weight = 0f;
                    break;

                case CoordinatorHandState.Mouse:
                    if (MouseReceiver != null && MouseReceiver.HandAnchor != null && MouseReceiver.HandAnchor.GameObject != null) {
                        worldPos = MouseReceiver.HandAnchor.GameObject.transform.position;
                        worldRot = MouseReceiver.HandAnchor.GameObject.transform.eulerAngles;
                        return;
                    }
                    weight = 0f;
                    break;

                case CoordinatorHandState.Pen:
                    if (PenReceiver != null && PenReceiver.HandAnchor != null && PenReceiver.HandAnchor.GameObject != null) {
                        worldPos = PenReceiver.HandAnchor.GameObject.transform.position;
                        worldRot = PenReceiver.HandAnchor.GameObject.transform.eulerAngles;
                        return;
                    }
                    weight = 0f;
                    break;

                case CoordinatorHandState.PointerRest:
                    var activePointer = GetActiveRestPointer(isLeft);
                    if (activePointer != null && (activePointer.IsRestHandActive || activePointer.IsInRestHandVisualSetup)) {
                        var restAnchor = activePointer.RestHandAnchor;
                        if (restAnchor != null && restAnchor.GameObject != null) {
                            worldPos = restAnchor.GameObject.transform.position;
                            worldRot = restAnchor.GameObject.transform.eulerAngles;
                            weight = 1f;
                            return;
                        }
                    }
                    weight = 0f;
                    break;

                case CoordinatorHandState.Released:
                    weight = 0f;
                    break;
            }
        }

        PointerReceiverAsset GetActiveRestPointer(bool isLeft) {
            bool dominantIsRight = isLeft;
            if (MouseReceiver != null && MouseReceiver.IsRightHanded == dominantIsRight && (targetRightState == CoordinatorHandState.Mouse || targetLeftState == CoordinatorHandState.Mouse || MouseReceiver.IsInRestHandVisualSetup)) {
                return MouseReceiver;
            }
            if (PenReceiver != null && PenReceiver.IsRightHanded == dominantIsRight && (targetRightState == CoordinatorHandState.Pen || targetLeftState == CoordinatorHandState.Pen || PenReceiver.IsInRestHandVisualSetup)) {
                return PenReceiver;
            }
            return MouseReceiver ?? PenReceiver;
        }

        void ApplyProxyWorldPose(AnchorAsset proxy, Vector3 worldPos, Vector3 worldRot) {
            if (proxy == null || proxy.GameObject == null) return;

            // In Warudo, AnchorAsset with Parent == null has Transform.Position and Rotation as world space
            proxy.Transform.Position = worldPos;
            proxy.Transform.Rotation = worldRot;
            proxy.Transform.Broadcast();
        }

        void UpdateCharacterLimbWeights() {
            if (Character == null) return;

            if (Character.LeftHandIK != null && Character.LeftHandIK.Enabled) {
                Character.LeftHandIK.PositionWeight = currentLeftWeight;
                Character.LeftHandIK.RotationWeight = currentLeftWeight;
                if (currentLeftState == CoordinatorHandState.PointerRest) {
                    var activePointer = GetActiveRestPointer(true);
                    if (activePointer != null && activePointer.RestHandPoleAnchor != null && activePointer.RestPoleWeight > 0.001f) {
                        Character.LeftHandIK.BendGoalTarget = activePointer.RestHandPoleAnchor;
                        Character.LeftHandIK.BendGoalWeight = currentLeftWeight * activePointer.RestPoleWeight;
                    } else if (Character.LeftHandIK.BendGoalTarget != null) {
                        Character.LeftHandIK.BendGoalWeight = 0f;
                        Character.LeftHandIK.BendGoalTarget = null;
                    }
                } else if (Character.LeftHandIK.BendGoalTarget != null) {
                    Character.LeftHandIK.BendGoalWeight = 0f;
                    Character.LeftHandIK.BendGoalTarget = null;
                }
                Character.LeftHandIK.Broadcast();
            }

            if (Character.RightHandIK != null && Character.RightHandIK.Enabled) {
                Character.RightHandIK.PositionWeight = currentRightWeight;
                Character.RightHandIK.RotationWeight = currentRightWeight;
                if (currentRightState == CoordinatorHandState.PointerRest) {
                    var activePointer = GetActiveRestPointer(false);
                    if (activePointer != null && activePointer.RestHandPoleAnchor != null && activePointer.RestPoleWeight > 0.001f) {
                        Character.RightHandIK.BendGoalTarget = activePointer.RestHandPoleAnchor;
                        Character.RightHandIK.BendGoalWeight = currentRightWeight * activePointer.RestPoleWeight;
                    } else if (Character.RightHandIK.BendGoalTarget != null) {
                        Character.RightHandIK.BendGoalWeight = 0f;
                        Character.RightHandIK.BendGoalTarget = null;
                    }
                } else if (Character.RightHandIK.BendGoalTarget != null) {
                    Character.RightHandIK.BendGoalWeight = 0f;
                    Character.RightHandIK.BendGoalTarget = null;
                }
                Character.RightHandIK.Broadcast();
            }
        }
    }
}
