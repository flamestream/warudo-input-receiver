using System;
using System.Linq;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets;
using Warudo.Plugins.Core.Assets.Utility;
using uDesktopDuplication;
using DG.Tweening;
using Warudo.Core;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public partial class PointerReceiverAsset : ReceiverAsset {
        [DataInput]
        [Hidden]
        public Guid SurfaceAnchorAssetId;
        [DataInput]
        [Hidden]
        public Guid SurfaceScreenAssetId;
        [DataInput]
        [Hidden]
        public Guid CursorAnchorAssetId;

        public const string LAYER_NAME_PREFIX = "🔥👆";
        static Vector3 IK_BASIS_LEFT_HANDED = new Vector3(0, -90f, -90f);
        static Vector3 IK_BASIS_RIGHT_HANDED = new Vector3(0, 90f, 90f);

        Monitor monitor;
        DrawingScreenAsset screenAsset;
        AnchorAsset cursorAnchorAsset;
        Tween cursorMoveTween;

        GameObject calculator;
        GameObject debugSphereCursorScreenPosition;
        GameObject debugSphereNeutralPosition;
        GameObject debugSphereCharacterPosition;
        GameObject debugSphereCharacterTargetPosition;
        GameObject debugSphereCharacterTargetBasePosition;
        Material debugSphereCursorScreenPositionMaterial;
        Material debugSphereNeutralPositionMaterial;
        Material debugSphereCharacterPositionMaterial;
        Material debugSphereCharacterTargetPositionMaterial;
        Material debugSphereCharacterTargetBasePositionMaterial;

        Vector3 cursorScreenPosition;
        Vector3 cachedAutoHandRotation = Vector3.zero;
        float cachedAutoHandRotX = float.NaN;
        int autoHandSettleCountdown = 0;

        bool isReady;
        int onReadyCountdown = 3;
        bool isDebugModeForced;
        bool inSetupMode;
        bool isBasicSetupComplete;
        public int adjustedX;
        public int adjustedY;
        int lastX;
        int lastY;
        int lastAdjustedX;
        int lastAdjustedY;
        public bool isOutOfBound;
        bool lastIsOutOfBound;
        bool isHandDisabledByOutOfBound;

        protected override void OnCreate() {
            base.OnCreate();
            OnCreateBasicSetup();
            OnCreateMoveHand();
            OnCreateProp();
            OnCreateMoveBody();
            OnCreateRestHand();
        }

        protected override void OnDestroy() {
            base.OnDestroy();

            DestroyDebugSpheres(true);
            CleanDestroy(bodySetupRootAnchor);
            GameObject.Destroy(calculator);
            OnDestroyRestHand();
            if (originalScreenParent != null && screenAsset != null && screenAsset.GameObject != null) {
                try {
                    screenAsset.GameObject.transform.SetParent(originalScreenParent, true);
                } catch {}
                originalScreenParent = null;
            }
            ScreenSetup?.Transform?.DestroyAnchor();
        }

        public override void OnUpdate() {
            base.OnUpdate();

            OnUpdateState();

            if (Character == null) return;

            // OnReady: One-time execution before update events are sent
            // Delay if a few tick is needed because things are for settled
            if (onReadyCountdown > 0) {
                --onReadyCountdown;
                if (onReadyCountdown == 0) {
                    isReady = true;
                    OnReadyBasicSetup();
                    OnReadyHand();
                    OnReadyBody();
                    OnReadyProp();
                    OnReadyRestHand();
                };
            }

            OnUpdateBasicSetup();
            OnUpdateHand();
            OnUpdateProp();
            OnUpdateBody();
            OnUpdateRestHand();
        }

        void OnReadyBasicSetup() {
            OnSurfaceSettingChange();
            if (screenAsset == null) {
                screenAsset = GetSurfaceScreen();
            }
            if (cursorAnchorAsset == null) {
                cursorAnchorAsset = GetCursorAnchor();
            }
            if (cursorAnchorAsset != null && screenAsset != null) {
                cursorAnchorAsset.Attachable.Parent = screenAsset;
            }
        }

        void OnCreateBasicSetup() {
            calculator = new GameObject();

            SetupDrawingScreenTransform();

            if (ScreenSetup == null) {
                ScreenSetup = StructuredData.Create<PointerDrawingScreenSetup>();
                SetDataInput(nameof(ScreenSetup), ScreenSetup, true);
            }
            ScreenSetup.Parent = this;
            if (ScreenSetup.AutoSetup != null) {
                ScreenSetup.AutoSetup.Parent = this;
                ScreenSetup.AutoSetup.UpdateDescription();
            }

            // Compatibility: migrate older saved single-port screen transforms if present
            if (HasSavedScreenTransform && !HasConfiguredScreenTransform && ScreenSetup?.Transform != null) {
                ScreenSetup.Transform.Position = SavedScreenPosition;
                ScreenSetup.Transform.Rotation = SavedScreenRotation;
                ScreenSetup.Transform.Scale = SavedScreenScale;
                ScreenSetup.Transform.SetDataInput(nameof(ScreenSetup.Transform.Position), SavedScreenPosition, true);
                ScreenSetup.Transform.SetDataInput(nameof(ScreenSetup.Transform.Rotation), SavedScreenRotation, true);
                ScreenSetup.Transform.SetDataInput(nameof(ScreenSetup.Transform.Scale), SavedScreenScale, true);
                HasConfiguredScreenTransform = true;
                SetDataInput(nameof(HasConfiguredScreenTransform), true, true);
            }

            Watch(nameof(DisplayName), delegate { OnSurfaceSettingChange(); });
            // OnSurfaceSettingChange(); // No need; will be called by #OnBasicSettingChange

            Watch(nameof(IsEnabled), delegate { OnBasicSettingChange(); });
            Watch(nameof(IsHandEnabled), delegate { OnBasicSettingChange(); });
            Watch(nameof(Character), delegate {
                ScreenSetup?.UpdateAutoSetupStatus();
                BroadcastDataInput(nameof(ScreenSetup));
                OnBasicSettingChange();
            });
            Watch(nameof(IsRightHanded), delegate {
                ScreenSetup?.UpdateAutoSetupStatus();
                BroadcastDataInput(nameof(ScreenSetup));
            });
            Watch(nameof(DisplayName), delegate { OnBasicSettingChange(); });
            Watch(nameof(ScreenSetup), delegate {
                SetupDrawingScreenTransform();
                OnSurfaceSettingChange();
            });
            OnBasicSettingChange();

            Watch(nameof(DebugSphereScaleFactor), delegate { ApplyDisplayPointerSphereScale(); });
            Watch(nameof(DataSourceReceiver), delegate { UpdateSocketLifecycle(); RequestUiRefresh(); });
            Watch(nameof(RestHand), delegate { OnRestHandSettingChange(); });
        }

        private bool isDataSourceFallbackActive = false;
        public bool IsDataSourceFallbackActive => isDataSourceFallbackActive;

        public void UpdateSocketLifecycle() {
            if (!IsEnabled) {
                if (IsSocketListening) StopReceiver();
                isDataSourceFallbackActive = false;
                return;
            }

            bool hasValidDataSource = DataSourceReceiver != null && DataSourceReceiver != this;
            if (hasValidDataSource) {
                if (DataSourceReceiver.IsEnabled && DataSourceReceiver.Active) {
                    // Sharing stream cleanly from data source; no direct socket needed
                    if (IsSocketListening) StopReceiver();
                    if (isDataSourceFallbackActive) {
                        isDataSourceFallbackActive = false;
                        RequestUiRefresh();
                    }
                } else {
                    // DataSource is disabled or inactive! Fall back to listening directly
                    if (!IsSocketListening) {
                        StartReceiver();
                        if (!isDataSourceFallbackActive) {
                            isDataSourceFallbackActive = true;
                            RequestUiRefresh();
                        }
                    }
                }
            } else {
                if (isDataSourceFallbackActive) {
                    isDataSourceFallbackActive = false;
                    RequestUiRefresh();
                }
                if (!IsSocketListening) StartReceiver();
            }
        }

        void OnUpdateBasicSetup() {
            UpdateSocketLifecycle();
            UpdateUiStatus();

            if (ScreenSetup?.Transform != null && ScreenSetup.Transform.InVisualSetupMode && ScreenSetup.Transform.setupAnchor != null) {
                var anchorTr = ScreenSetup.Transform.setupAnchor.Transform;
                if (screenAsset != null) {
                    screenAsset.Transform.Position = anchorTr.Position;
                    screenAsset.Transform.Rotation = anchorTr.Rotation;
                    screenAsset.Transform.Scale = anchorTr.Scale;
                    screenAsset.Transform.Broadcast();
                }
            }

            if (!IsEnabled || !IsHandEnabled) return;
            if (screenAsset == null) screenAsset = GetSurfaceScreen();
            if (monitor == null && !string.IsNullOrEmpty(DisplayName)) {
                monitor = Manager.monitors.FirstOrDefault((Monitor it) => it.name == DisplayName);
            }
            if (cursorAnchorAsset == null) cursorAnchorAsset = GetCursorAnchor();
            if (cursorAnchorAsset != null && screenAsset != null && cursorAnchorAsset.Attachable.Parent != screenAsset) {
                cursorAnchorAsset.Attachable.Parent = screenAsset;
            }
            if (monitor == null) return;
            if (cursorAnchorAsset == null) return;

            if (CursorMode.Mode == CursorModeValue.FixedDelta) {
                adjustedX = 0;
                adjustedY = 0;
                if (X != lastX) {
                    var midX = (monitor.left + monitor.right) / 2;
                    if (X != midX) {
                        adjustedX = (int)((X - midX) * CursorMode.DisplacementFactor);
                    }
                }
                if (Y != lastY) {
                    var midY = (monitor.top + monitor.bottom) / 2;
                    if (Y != midY) {
                        adjustedY = (int)((Y - midY) * CursorMode.DisplacementFactor);
                    }
                }
                adjustedX += lastAdjustedX;
                adjustedY += lastAdjustedY;
            } else {
                adjustedX = X;
                adjustedY = Y;
            }

            var inX = Mathf.Clamp(adjustedX, monitor.left - (int)CursorMode.BoundOffsets.x, monitor.right + (int)CursorMode.BoundOffsets.z);
            var inY = Mathf.Clamp(adjustedY, monitor.top - (int)CursorMode.BoundOffsets.y, monitor.bottom + (int)CursorMode.BoundOffsets.w);
            isOutOfBound = inX != X || inY != Y;


            if (CursorMode.Mode == CursorModeValue.FixedDelta) {
                switch (CursorMode.OutOfBoundFixedDeltaHandling) {
                    case OutOfBoundFixedDeltaHandlingValue.BackToCenter:
                        if (adjustedX != inX) adjustedX = (monitor.left + monitor.right) / 2;
                        if (adjustedY != inY) adjustedY = (monitor.top + monitor.bottom) / 2;
                        break;
                    case OutOfBoundFixedDeltaHandlingValue.Clamped:
                    default:
                        adjustedX = inX;
                        adjustedY = inY;
                        break;
                }
            } else {
                if (CursorMode.OutOfBoundRawTrackingHandling != OutOfBoundRawTrackingHandlingValue.Overflow) {
                    adjustedX = inX;
                    adjustedY = inY;
                    if (CursorMode.OutOfBoundRawTrackingHandling == OutOfBoundRawTrackingHandlingValue.DisableHand) {
                        if (isOutOfBound != lastIsOutOfBound) {
                            isHandDisabledByOutOfBound = isOutOfBound;
                            OnInputAffectingHandSetupChange();
                        }
                    } else if (CursorMode.OutOfBoundRawTrackingHandling == OutOfBoundRawTrackingHandlingValue.Freeze) {
                        if (isOutOfBound) {
                            adjustedX = lastAdjustedX;
                            adjustedY = lastAdjustedY;
                        }
                    }
                }
            }

            var anchorTransform = cursorAnchorAsset.Transform;
            anchorTransform.Rotation = IsRightHanded
                ? IK_BASIS_RIGHT_HANDED
                : IK_BASIS_LEFT_HANDED;

            if (inSetupMode || IsInRestHandVisualSetup || IsInScreenVisualSetup) {
                anchorTransform.Position = NeutralHandPosition.setupAnchor?.Transform.Position ?? NeutralHandPosition.Position;
                cursorScreenPosition = anchorTransform.Position;

                if (HandDynamicRotation != null && HandDynamicRotation.Enabled && HandDynamicRotation.Auto) {
                    if (HandDynamicRotation.AutoRotationEvaluator == null) {
                        HandDynamicRotation.AutoRotationEvaluator = CalculateAutoHandRotation;
                    }
                    cachedAutoHandRotation = CalculateAutoHandRotation(Vector3.zero);
                    cachedAutoHandRotX = cachedAutoHandRotation.x;
                    autoHandSettleCountdown = 0;
                    anchorTransform.Rotation += cachedAutoHandRotation;
                }
            } else {

                var targetPlanarPosition = screenAsset.GetChildCursorPosition(adjustedX, adjustedY) * PointerFactorCorrection;

                if (CursorSmoothness == 0) {
                    cursorMoveTween?.Kill();
                    cursorScreenPosition = targetPlanarPosition;
                } else if (adjustedX != lastAdjustedX || adjustedY != lastAdjustedY) {
                    cursorMoveTween?.Kill();
                    cursorMoveTween = DOTween.To(
                        () => cursorScreenPosition,
                        delegate(Vector3 it) { cursorScreenPosition = it; },
                        targetPlanarPosition,
                        CursorSmoothness
                    ).SetEase(Ease.Linear);
                }

                anchorTransform.Position = cursorScreenPosition;

                Vector3 delta = NeutralHandPosition != null
                    ? NeutralHandPosition.Position - cursorAnchorAsset.Transform.Position
                    : -cursorAnchorAsset.Transform.Position;

                if (HandDynamicRotation != null && HandDynamicRotation.Enabled) {
                    if (HandDynamicRotation.AutoRotationEvaluator == null) {
                        HandDynamicRotation.AutoRotationEvaluator = CalculateAutoHandRotation;
                    }

                    bool mouseMoved = (adjustedX != lastAdjustedX || adjustedY != lastAdjustedY);
                    if (mouseMoved) {
                        autoHandSettleCountdown = HandDynamicRotation.AutoSettleFrames;
                    }

                    if (HandDynamicRotation.Auto) {
                        bool shouldEvaluate = mouseMoved
                            || autoHandSettleCountdown > 0
                            || float.IsNaN(cachedAutoHandRotX);

                        if (shouldEvaluate) {
                            if (!mouseMoved && autoHandSettleCountdown > 0) {
                                autoHandSettleCountdown--;
                            }

                            Vector3 rawTarget = CalculateAutoHandRotation(delta);
                            float targetRotX = rawTarget.x;

                            if (float.IsNaN(cachedAutoHandRotX)) {
                                cachedAutoHandRotX = targetRotX;
                            } else {
                                float maxSpeed = HandDynamicRotation.AutoMaxSpeed;
                                float dt = Time.deltaTime > 0f ? Time.deltaTime : 0.016f;
                                float maxDelta = maxSpeed * dt;
                                cachedAutoHandRotX = Mathf.MoveTowardsAngle(cachedAutoHandRotX, targetRotX, maxDelta);

                                if (!mouseMoved && Mathf.Abs(Mathf.DeltaAngle(cachedAutoHandRotX, targetRotX)) < 0.05f) {
                                    autoHandSettleCountdown = 0;
                                }
                            }
                            cachedAutoHandRotation = new Vector3(cachedAutoHandRotX, 0f, 0f);
                        }
                    } else {
                        bool pointerMoved = mouseMoved || float.IsNaN(cachedAutoHandRotX);
                        if (pointerMoved) {
                            cachedAutoHandRotation = HandDynamicRotation.Evaluate(delta);
                            cachedAutoHandRotX = cachedAutoHandRotation.x;
                        }
                    }
                    anchorTransform.Rotation += cachedAutoHandRotation;
                } else if (HandDynamicRotation != null && !HandDynamicRotation.Enabled) {
                    cachedAutoHandRotation = Vector3.zero;
                    cachedAutoHandRotX = float.NaN;
                    autoHandSettleCountdown = 0;
                }
            }

            if (debugSphereNeutralPosition != null) {
                // Zero division protection
                if (screenAsset.Transform.Scale.x > 0 && screenAsset.Transform.Scale.y > 0 && screenAsset.Transform.Scale.z > 0) {
                    debugSphereNeutralPosition.gameObject.transform.localPosition = new Vector3(
                        NeutralHandPosition.Position.x / screenAsset.Transform.Scale.x,
                        NeutralHandPosition.Position.y / screenAsset.Transform.Scale.y,
                        NeutralHandPosition.Position.z / screenAsset.Transform.Scale.z
                    );
                }
            }

            anchorTransform.Broadcast();
            lastX = X;
            lastY = Y;
            lastAdjustedX = adjustedX;
            lastAdjustedY = adjustedY;
            lastIsOutOfBound = isOutOfBound;
        }

        public Vector3 CalculateAutoHandRotation(Vector3 delta) {
            if (Character == null || screenAsset == null) return Vector3.zero;

            var charTr = Character.MainTransform ?? Character.GameObject?.transform;
            if (charTr == null) return Vector3.zero;

            bool isRight = IsRightHanded;
            var anim = Character.Animator ?? Character.CloneAnimator;
            var elbowBone = anim != null ? anim.GetBoneTransform(isRight ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm) : null;
            Vector3 elbowWorld = elbowBone != null ? elbowBone.position : GetNaturalRestingElbowWorldPosition();

            Quaternion screenRot = screenAsset.GameObject != null ? screenAsset.GameObject.transform.rotation : Quaternion.Euler(screenAsset.Transform.Rotation);
            Vector3 screenPos = screenAsset.GameObject != null ? screenAsset.GameObject.transform.position : screenAsset.Transform.Position;
            Vector3 elbowScreen = Quaternion.Inverse(screenRot) * (elbowWorld - screenPos);
            Vector2 elbowScreen2D = new Vector2(elbowScreen.x, elbowScreen.y);

            Vector2 cursorScreen2D = new Vector2(cursorScreenPosition.x, cursorScreenPosition.y);
            Vector2 toElbow = elbowScreen2D - cursorScreen2D;

            if (toElbow.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector2 baseLine = new Vector2(0f, -1f);
            float sweepAngle = Vector2.SignedAngle(baseLine, toElbow);
            float targetRotX = isRight ? -sweepAngle : sweepAngle;

            return new Vector3(targetRotX, 0f, 0f);
        }

        public Vector3 GetNaturalRestingElbowWorldPosition() {
            if (Character == null) return Vector3.zero;
            var charTr = Character.MainTransform ?? Character.GameObject?.transform;
            if (charTr == null) return Vector3.zero;

            bool isRight = IsRightHanded;
            var anim = Character.Animator ?? Character.CloneAnimator;
            var shoulder = anim != null ? anim.GetBoneTransform(isRight ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm) : null;
            var elbow = anim != null ? anim.GetBoneTransform(isRight ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm) : null;
            var chest = anim != null ? (anim.GetBoneTransform(HumanBodyBones.Chest) ?? anim.GetBoneTransform(HumanBodyBones.Spine)) : null;

            float upperArmLength = (shoulder != null && elbow != null) ? Vector3.Distance(shoulder.position, elbow.position) : 0.28f;
            Vector3 lateralDir = isRight ? charTr.right : -charTr.right;
            Vector3 shoulderPos = shoulder != null ? shoulder.position : (chest != null ? chest.position + lateralDir * 0.18f : (charTr.position + charTr.up * 1.3f + lateralDir * 0.18f));

            float cos20 = Mathf.Cos(20f * Mathf.Deg2Rad);
            float sin20 = Mathf.Sin(20f * Mathf.Deg2Rad);
            float sin10 = Mathf.Sin(10f * Mathf.Deg2Rad);

            Vector3 deltaDrop = -charTr.up * (upperArmLength * cos20);
            Vector3 deltaOut = lateralDir * (upperArmLength * sin20);
            Vector3 deltaFwd = charTr.forward * (upperArmLength * sin10);
            return shoulderPos + deltaDrop + deltaOut + deltaFwd;
        }

        protected override void Log(string msg) {
            Debug.Log($"[FlameStream.Asset.PointerReceiver] {msg}");
        }

        void OnBasicSettingChange() {
            SetDataInputPropertyAndBroadcast(nameof(Character), hidden: !IsHandEnabled);
            SetDataInputPropertyAndBroadcast(nameof(IsRightHanded), hidden: !IsHandEnabled);
            SetDataInputPropertyAndBroadcast(nameof(DisplayName), hidden: !IsHandEnabled);
            SetDataInputPropertyAndBroadcast(nameof(CursorSmoothness), hidden: !IsHandEnabled);
            OnSurfaceSettingChange();

            SetDataInputPropertyAndBroadcast(nameof(CursorMode), hidden: !IsHandEnabled);
            SetDataInputPropertyAndBroadcast(nameof(ScreenSetup), hidden: !IsHandEnabled);

            isBasicSetupComplete = IsHandEnabled && Character != null && monitor != null;
            SetDataInputPropertyAndBroadcast(nameof(HandMouseMode), hidden: !isBasicSetupComplete);
            SetDataInputPropertyAndBroadcast(nameof(HandPenMode), hidden: !isBasicSetupComplete);
            SetDataInputPropertyAndBroadcast(nameof(HandDynamicRotation), hidden: !isBasicSetupComplete);
            SetDataInputPropertyAndBroadcast(nameof(IsPropEnabled), hidden: !isBasicSetupComplete);
            SetDataInputPropertyAndBroadcast(nameof(IsBodyEnabled), hidden: !isBasicSetupComplete);
            SetDataInputPropertyAndBroadcast(nameof(NeutralHandPosition), hidden: !isBasicSetupComplete);
            SetDataInputPropertyAndBroadcast(nameof(PointerFactorCorrection), hidden: !isBasicSetupComplete);
            SetDataInputPropertyAndBroadcast(nameof(DebugSphereScaleFactor), hidden: !isBasicSetupComplete);

            OnPropEnabledChanged();
            OnBodyEnabledChanged();
            OnDebugSettingChange();
            ScreenSetup?.UpdateAutoSetupStatus();
        }

        void OnSurfaceSettingChange() {
            // Scene check
            if (Scene == null) return;

            bool isRemoveWanted = !IsEnabled && (ScreenSetup?.RemoveOnDisable ?? false);
            if (isRemoveWanted) {
                DestroySurfaceScreen();
                DestroyCursorAnchor();
                return;
            }

            // Screen check
            screenAsset = GetSurfaceScreen();
            if (screenAsset == null) return;

            // Ensure screen stays enabled even if pointer asset is disabled
            screenAsset.DataInputPortCollection.SetValueAtPath("Enabled", true, true);
            screenAsset.DataInputPortCollection.SetValueAtPath("DisplayName", DisplayName, true);

            bool isDisplayMustBeVisible = IsEnabled && IsHandEnabled;
            if (isDisplayMustBeVisible) {
                // Bind cursor anchor to screen
                cursorAnchorAsset = GetCursorAnchor();
                if (cursorAnchorAsset != null) {
                    cursorAnchorAsset.Attachable.Parent = screenAsset;
                }
            } else {
                DestroyCursorAnchor();
            }

            // Monitor data check
            monitor = Manager.monitors.FirstOrDefault((Monitor it) => it.name == DisplayName);
            if (monitor == null) return;
        }

        public void OnDrawingScreenSettingChange() {
            OnSurfaceSettingChange();
        }

        public bool TryGetCharacterScreenTransform(out Vector3 position, out Vector3 rotation, out Vector3 scale) {
            return CalculateScreenPresetTransform(DrawingScreenAutoSetupPreset.StandingTilted, out position, out rotation, out scale);
        }

        public bool CalculateScreenPresetTransform(DrawingScreenAutoSetupPreset preset, out Vector3 position, out Vector3 rotation, out Vector3 scale) {
            scale = preset == DrawingScreenAutoSetupPreset.StandingTilted ? new Vector3(0.03f, 0.03f, 0.03f) : new Vector3(0.01f, 0.01f, 0.01f);
            if (Character != null && Character.GameObject != null) {
                var anim = Character.Animator ?? Character.CloneAnimator;
                var charTr = Character.MainTransform ?? Character.GameObject.transform;
                var forward = charTr.forward;
                var up = charTr.up;
                var right = charTr.right;
                var charYaw = charTr.eulerAngles.y;

                bool isRight = IsRightHanded;
                var shoulder = anim != null ? anim.GetBoneTransform(isRight ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm) : null;
                var elbow = anim != null ? anim.GetBoneTransform(isRight ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm) : null;
                var wrist = anim != null ? anim.GetBoneTransform(isRight ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand) : null;
                var knuckle = anim != null ? anim.GetBoneTransform(isRight ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal) : null;
                var chest = anim != null ? (anim.GetBoneTransform(HumanBodyBones.Chest) ?? anim.GetBoneTransform(HumanBodyBones.Spine)) : null;

                float upperArmLength = (shoulder != null && elbow != null) ? Vector3.Distance(shoulder.position, elbow.position) : 0.28f;
                float forearmLength = (elbow != null && wrist != null) ? Vector3.Distance(elbow.position, wrist.position) : 0.25f;
                float palmLength = (wrist != null && knuckle != null) ? Vector3.Distance(wrist.position, knuckle.position) : 0.08f;

                if (preset == DrawingScreenAutoSetupPreset.StandingTilted) {
                    var baseChestPos = chest != null ? chest.position : (charTr.position + up * 1.2f);
                    float forwardReach = Mathf.Max(0.18f, forearmLength * 0.9f) * 1.5f;
                    position = baseChestPos + forward * forwardReach;
                    rotation = new Vector3(-60f, charYaw + 180f, 0f);
                    scale = new Vector3(0.03f, 0.03f, 0.03f);
                    return true;
                } else {
                    // Flat Surface (Mouse/Touchpad)
                    Vector3 naturalElbow = GetNaturalRestingElbowWorldPosition();

                    float forwardReach = forearmLength + 0.5f * palmLength;
                    position = new Vector3(naturalElbow.x, naturalElbow.y - 0.02f, naturalElbow.z) + forward * forwardReach;
                    rotation = new Vector3(-90f, charYaw + 180f, 0f);
                    scale = new Vector3(0.01f, 0.01f, 0.01f);
                    return true;
                }
            }

            position = new Vector3(0, 0.6f, 1f);
            rotation = new Vector3(-60f, 180f, 0);
            scale = preset == DrawingScreenAutoSetupPreset.StandingTilted ? new Vector3(0.03f, 0.03f, 0.03f) : new Vector3(0.01f, 0.01f, 0.01f);
            return false;
        }

        public void ApplyScreenAutoSetup(DrawingScreenAutoSetupPreset preset) {
            if (CalculateScreenPresetTransform(preset, out Vector3 pos, out Vector3 rot, out Vector3 scl)) {
                if (ScreenSetup?.Transform != null) {
                    ScreenSetup.Transform.Position = pos;
                    ScreenSetup.Transform.Rotation = rot;
                    ScreenSetup.Transform.Scale = scl;
                    ScreenSetup.Transform.SetDataInput(nameof(ScreenSetup.Transform.Position), pos, true);
                    ScreenSetup.Transform.SetDataInput(nameof(ScreenSetup.Transform.Rotation), rot, true);
                    ScreenSetup.Transform.SetDataInput(nameof(ScreenSetup.Transform.Scale), scl, true);
                    ScreenSetup.Transform.Broadcast();
                }

                HasConfiguredScreenTransform = true;
                SetDataInput(nameof(HasConfiguredScreenTransform), true, true);

                var screen = screenAsset ?? (SurfaceScreenAssetId != Guid.Empty ? Scene?.GetAssets<DrawingScreenAsset>().FirstOrDefault(s => s.Id == SurfaceScreenAssetId) : null);
                if (screen != null) {
                    screen.Transform.Position = pos;
                    screen.Transform.Rotation = rot;
                    screen.Transform.Scale = scl;
                    screen.Transform.Broadcast();
                }

                string presetName = preset == DrawingScreenAutoSetupPreset.StandingTilted
                    ? "SCREEN_PRESET_STANDING_TILTED".Localized()
                    : "SCREEN_PRESET_FLAT_SURFACE".Localized();
                ShowToast(string.Format("SCREEN_AUTO_SETUP_APPLIED_TOAST".Localized(), presetName));
            } else {
                ShowToast("SCREEN_AUTO_SETUP_NO_CHARACTER_TOAST".Localized());
            }
        }


        private Transform originalScreenParent = null;

        public void SetupDrawingScreenTransform() {
            if (ScreenSetup == null) return;
            if (ScreenSetup.Transform == null) {
                ScreenSetup.Transform = StructuredData.Create<CollapsibleVisualSetupTransform>();
                Vector3 defaultPos = new Vector3(0, 0.6f, 1f);
                Vector3 defaultRot = new Vector3(-60f, 180f, 0);
                Vector3 defaultScl = new Vector3(0.07f, 0.07f, 0.07f);
                var activePreset = ScreenSetup?.AutoSetup?.Preset ?? DrawingScreenAutoSetupPreset.StandingTilted;
                if (CalculateScreenPresetTransform(activePreset, out Vector3 charPos, out Vector3 charRot, out Vector3 charScl)) {
                    defaultPos = charPos;
                    defaultRot = charRot;
                    defaultScl = charScl;
                }
                ScreenSetup.Transform.Position = defaultPos;
                ScreenSetup.Transform.Rotation = defaultRot;
                ScreenSetup.Transform.Scale = defaultScl;
                ScreenSetup.SetDataInput(nameof(ScreenSetup.Transform), ScreenSetup.Transform, true);
            }

            var tr = ScreenSetup.Transform;
            tr.LocalizationKey = "SCREEN_TRANSFORM";
            tr.CollapsedSelf = true;
            tr.ShowOriginIndicator = false;

            tr.OnEnterVisualSetup = (t) => {
                if (screenAsset == null) {
                    screenAsset = GetSurfaceScreen();
                }
            };

            tr.OnCreateAnchor = (t, setupAnchor) => {
                setupAnchor.Name = "⌛⚓-" + "SETUP_ANCHOR_NAME_SCREEN_SETUP".Localized();
                setupAnchor.Instructions = "SETUP_ANCHOR_INSTRUCTIONS_SCREEN_SETUP".Localized();
                setupAnchor.Transform.Position = t.Position;
                setupAnchor.Transform.Rotation = t.Rotation;
                setupAnchor.Transform.Scale = t.Scale;
                setupAnchor.SetHideScale(false);
                setupAnchor.Transform.Broadcast();

                if (screenAsset != null && screenAsset.GameObject != null && setupAnchor.GameObject != null) {
                    originalScreenParent = screenAsset.GameObject.transform.parent;
                    screenAsset.GameObject.transform.SetParent(setupAnchor.GameObject.transform, true);
                }
            };

            tr.OnResetAnchor = (t, setupAnchor) => {
                var activePreset = ScreenSetup?.AutoSetup?.Preset ?? DrawingScreenAutoSetupPreset.StandingTilted;
                if (CalculateScreenPresetTransform(activePreset, out Vector3 defaultPos, out Vector3 defaultRot, out Vector3 defaultScale)) {
                    setupAnchor.Transform.Position = defaultPos;
                    setupAnchor.Transform.Rotation = defaultRot;
                    setupAnchor.Transform.Scale = defaultScale;
                    setupAnchor.Transform.Broadcast();
                }
            };

            tr.OnApplyAnchor = (t, setupAnchor) => {
                if (screenAsset != null && screenAsset.GameObject != null && originalScreenParent != null) {
                    screenAsset.GameObject.transform.SetParent(originalScreenParent, true);
                    originalScreenParent = null;
                }
                t.Position = setupAnchor.Transform.Position;
                t.Rotation = setupAnchor.Transform.Rotation;
                t.Scale = setupAnchor.Transform.Scale;
                t.SetDataInput(nameof(t.Position), t.Position, true);
                t.SetDataInput(nameof(t.Rotation), t.Rotation, true);
                t.SetDataInput(nameof(t.Scale), t.Scale, true);
                HasConfiguredScreenTransform = true;
                SetDataInput(nameof(HasConfiguredScreenTransform), true, true);
                if (screenAsset != null) {
                    screenAsset.Transform.Position = t.Position;
                    screenAsset.Transform.Rotation = t.Rotation;
                    screenAsset.Transform.Scale = t.Scale;
                    screenAsset.Transform.Broadcast();
                }
            };

            tr.OnExitVisualSetup = (isApplied) => {
                if (screenAsset != null && screenAsset.GameObject != null && originalScreenParent != null) {
                    screenAsset.GameObject.transform.SetParent(originalScreenParent, true);
                    originalScreenParent = null;
                }
                if (!isApplied && screenAsset != null) {
                    screenAsset.Transform.Position = tr.Position;
                    screenAsset.Transform.Rotation = tr.Rotation;
                    screenAsset.Transform.Scale = tr.Scale;
                    screenAsset.Transform.Broadcast();
                    if (screenAsset.GameObject != null) {
                        screenAsset.GameObject.transform.position = tr.Position;
                        screenAsset.GameObject.transform.rotation = Quaternion.Euler(tr.Rotation);
                        screenAsset.GameObject.transform.localScale = tr.Scale;
                    }
                }
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };

            tr.Watch(nameof(tr.Position), delegate { OnDrawingScreenTransformDirectEdit(); });
            tr.Watch(nameof(tr.Rotation), delegate { OnDrawingScreenTransformDirectEdit(); });
            tr.Watch(nameof(tr.Scale), delegate { OnDrawingScreenTransformDirectEdit(); });
        }

        public void OnDrawingScreenTransformDirectEdit() {
            if (screenAsset != null && ScreenSetup?.Transform != null && !ScreenSetup.Transform.InVisualSetupMode) {
                HasConfiguredScreenTransform = true;
                SetDataInput(nameof(HasConfiguredScreenTransform), true, true);
                screenAsset.Transform.Position = ScreenSetup.Transform.Position;
                screenAsset.Transform.Rotation = ScreenSetup.Transform.Rotation;
                screenAsset.Transform.Scale = ScreenSetup.Transform.Scale;
                screenAsset.Transform.Broadcast();
            }
        }

        public void DestroySurfaceScreen() {
            if (originalScreenParent != null && screenAsset != null && screenAsset.GameObject != null) {
                try {
                    screenAsset.GameObject.transform.SetParent(originalScreenParent, true);
                } catch {}
                originalScreenParent = null;
            }
            var screen = screenAsset ?? (SurfaceScreenAssetId != Guid.Empty ? Scene?.GetAssets<DrawingScreenAsset>().FirstOrDefault(s => s.Id == SurfaceScreenAssetId) : null);
            if (screen != null) {
                CleanDestroy(screen);
            }
            SurfaceScreenAssetId = Guid.Empty;
            screenAsset = null;
        }

        public void DestroyCursorAnchor() {
            cursorMoveTween?.Kill();
            var anchor = cursorAnchorAsset ?? (CursorAnchorAssetId != Guid.Empty ? Scene?.GetAssets<AnchorAsset>().FirstOrDefault(a => a.Id == CursorAnchorAssetId) : null);
            if (anchor != null) {
                CleanDestroy(anchor);
            }
            CursorAnchorAssetId = Guid.Empty;
            cursorAnchorAsset = null;
        }

        void OnDebugSettingChange() {
            GetTriggerPort(nameof(TriggerForceDebugSpheres)).Properties.hidden = isDebugModeForced || !isBasicSetupComplete;
            GetTriggerPort(nameof(TriggerDestroyDebugSpheres)).Properties.hidden = !isDebugModeForced || !isBasicSetupComplete;
            BroadcastTriggerProperties(nameof(TriggerForceDebugSpheres));
            BroadcastTriggerProperties(nameof(TriggerDestroyDebugSpheres));
        }

        public void EnterSetupModeMinimal() {
            if (inSetupMode) return;
            inSetupMode = true;
            cachedAutoHandRotation = Vector3.zero;
            cachedAutoHandRotX = float.NaN;
            autoHandSettleCountdown = 0;
            ResetCharacterPosition();
            DisplayDebugSpheres();
        }

        public void TriggerOtherEnterSetupModes(int source, bool button1) {
            NeutralHandPosition.EnterVisualSetup(false);
            if (setupModeHandState == null) {
                GetHandState(source, button1)?.Transform.EnterVisualSetup(false);
            }
            if (setupModePropState == null) {
                GetPropState(source, button1)?.Transform.EnterVisualSetup(false);
            }
            if (setupModeBodyState == null) {
                GetBodyState(source, button1)?.Transform.EnterVisualSetup(false);
            }
        }

        public void ExitSetupModeMinimal() {
            if (!inSetupMode) return;
            inSetupMode = false;
            cachedAutoHandRotation = Vector3.zero;
            cachedAutoHandRotX = float.NaN;
            autoHandSettleCountdown = 0;
            DestroyDebugSpheres();

            Context.Service.NavigateToAsset(Id);
            Context.Service.BroadcastOpenedScene();
        }

        void CleanDestroy(GameObjectAsset g) {
            if (g == null) return;
            try {
                Scene.RemoveAsset(g.Id);
            } catch {}
        }

        DrawingScreenAsset GetSurfaceScreen(bool skipAutoCreate = false) {
            var screen = GetAsset<DrawingScreenAsset>(ref SurfaceScreenAssetId, out bool isNewlyCreated, skipAutoCreate, newAssetName: $"📦-{LAYER_NAME_PREFIX} Screen");
            if (screen != null) {
                if (isNewlyCreated && ScreenSetup?.Transform != null) {
                    screen.Transform.Position = ScreenSetup.Transform.Position;
                    screen.Transform.Rotation = ScreenSetup.Transform.Rotation;
                    screen.Transform.Scale = ScreenSetup.Transform.Scale;
                    screen.Transform.Broadcast();
                }
            }
            return screen;
        }

        AnchorAsset GetCursorAnchor(bool skipAutoCreate = false) {
            return GetAsset<AnchorAsset>(ref CursorAnchorAssetId, skipAutoCreate, newAssetName: $"🔒⚓-{LAYER_NAME_PREFIX}🎯");
        }

        T GetAsset<T>(ref Guid assetId, out bool isNewlyCreated, bool skipAutoCreate = false, string newAssetName = null) where T : Asset  {
            isNewlyCreated = false;

            // Return null if Scene is not ready yet (during creation)
            if (Scene == null) {
                return null;
            }

            if (assetId != Guid.Empty) {
                var id = assetId;
                var fetchResult = Scene.GetAssets<T>().FirstOrDefault(p => p.Id == id);
                if (fetchResult != null) {
                    return fetchResult;
                }
            }

            // Check if an existing asset matching the intended name is in the scene (avoid duplicate " 2" suffixes)
            if (!string.IsNullOrEmpty(newAssetName)) {
                var existing = Scene.GetAssets<T>().FirstOrDefault(p => p.Name == newAssetName || (p.Name != null && p.Name.Contains(newAssetName)));
                if (existing != null) {
                    assetId = existing.Id;
                    return existing;
                }
            }

            if (skipAutoCreate) return null;

            var newAsset = Scene.AddAsset<T>();
            newAsset.Name = newAssetName ?? $"📦-{LAYER_NAME_PREFIX} {nameof(T)}";
            Scene.UpdateNewAssetName(newAsset);
            newAsset.Broadcast();
            assetId = newAsset.Id;

            isNewlyCreated = true;
            return newAsset;
        }

        T GetAsset<T>(ref Guid assetId, bool skipAutoCreate = false, string newAssetName = null) where T : Asset  {
            return GetAsset<T>(ref assetId, out bool _isNewlyCreated, skipAutoCreate, newAssetName);
        }

        GameObject GetDebugSphereCursorPosition(bool skipAutoCreate = false) {
            if (debugSphereCursorScreenPosition) return debugSphereCursorScreenPosition;
            if (skipAutoCreate) return null;

            var s = CreateDebugSphere(
                ref debugSphereCursorScreenPosition,
                ref debugSphereCursorScreenPositionMaterial,
                new Color(1f, 0.55f, 0f, 0.9f)
            );
            s.transform.position = Vector3.zero;
            s.transform.SetParent(cursorAnchorAsset.GameObject.transform, false);
            return s;
        }

        GameObject GetDebugSphereNeutralPosition(bool skipAutoCreate = false) {
            if (debugSphereNeutralPosition) return debugSphereNeutralPosition;
            if (skipAutoCreate) return null;

            var s = CreateDebugSphere(
                ref debugSphereNeutralPosition,
                ref debugSphereNeutralPositionMaterial,
                Color.yellow
            );
            s.transform.position = Vector3.zero;
            s.transform.SetParent(screenAsset.GameObject.transform, false);
            return s;
        }

        GameObject GetDebugSphereCharacterPosition(bool skipAutoCreate = false) {
            if (debugSphereCharacterPosition) return debugSphereCharacterPosition;
            if (skipAutoCreate) return null;

            var s = CreateDebugSphere(
                ref debugSphereCharacterPosition,
                ref debugSphereCharacterPositionMaterial,
                Color.green
            );
            s.transform.position = Vector3.zero;
            s.transform.SetParent(Character.GameObject.transform, false);
            return s;
        }

        GameObject GetDebugSphereCharacterTargetPosition(bool skipAutoCreate = false) {
            if (debugSphereCharacterTargetPosition) return debugSphereCharacterTargetPosition;
            if (skipAutoCreate) return null;

            var s = CreateDebugSphere(
                ref debugSphereCharacterTargetPosition,
                ref debugSphereCharacterTargetPositionMaterial,
                new Color(1f, 0f, 1f, 0.5f)
            );
            s.transform.position = Vector3.zero;
            s.transform.SetParent(cursorAnchorAsset.GameObject.transform, false);
            return s;
        }

        GameObject GetDebugSphereCharacterTargetBasePosition(bool skipAutoCreate = false) {
            if (debugSphereCharacterTargetBasePosition) return debugSphereCharacterTargetBasePosition;
            if (skipAutoCreate) return null;

            var s = CreateDebugSphere(
                ref debugSphereCharacterTargetBasePosition,
                ref debugSphereCharacterTargetBasePositionMaterial,
                Color.blue
            );
            s.transform.position = Vector3.zero;
            s.transform.SetParent(cursorAnchorAsset.GameObject.transform, false);
            return s;
        }

        GameObject CreateDebugSphere(ref GameObject go, ref Material m, Color c) {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            m = new Material(Shader.Find("Standard"));
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c);
            m.SetFloat("_EmissionIntensity", 1.0f);
            m.color = c;
            m.SetFloat("_Mode", 3); // Unlit
            go.GetComponent<Renderer>().material = m;

            return go;
        }

        void DisplayDebugSpheres(bool force = false) {
            if (force) isDebugModeForced = true;
            GetDebugSphereCursorPosition();
            GetDebugSphereNeutralPosition();
            GetDebugSphereCharacterPosition();
            GetDebugSphereCharacterTargetPosition();
            GetDebugSphereCharacterTargetBasePosition();
            ApplyDisplayPointerSphereScale();
        }

        void ApplyDisplayPointerSphereScale() {
            var screenScale = (screenAsset != null) ? screenAsset.Transform.Scale : Vector3.one;
            var baseScaleFactor = 0.2f * DebugSphereScaleFactor;
            var scale = screenScale * baseScaleFactor;
            var scale2 = Vector3.one * baseScaleFactor;
            if (debugSphereCursorScreenPosition != null) {
                debugSphereCursorScreenPosition.gameObject.transform.localScale = scale;
            }
            if (debugSphereNeutralPosition != null) {
                debugSphereNeutralPosition.gameObject.transform.localScale = scale2 * 0.99f;
            }
            if (debugSphereCharacterPosition != null) {
                debugSphereCharacterPosition.gameObject.transform.localScale = scale;
            }
            if (debugSphereCharacterTargetPosition != null) {
                debugSphereCharacterTargetPosition.gameObject.transform.localScale = scale * 0.99f;
            }
            if (debugSphereCharacterTargetBasePosition != null) {
                debugSphereCharacterTargetBasePosition.gameObject.transform.localScale = scale * 0.98f;
            }
        }

        void DestroyDebugSpheres(bool force = false) {
            if (isDebugModeForced && !force) return;

            isDebugModeForced = false;

            GameObject.Destroy(debugSphereCursorScreenPosition);
            Material.Destroy(debugSphereCursorScreenPositionMaterial);
            GameObject.Destroy(debugSphereNeutralPosition);
            Material.Destroy(debugSphereNeutralPositionMaterial);
            GameObject.Destroy(debugSphereCharacterPosition);
            Material.Destroy(debugSphereCharacterPositionMaterial);
            GameObject.Destroy(debugSphereCharacterTargetPosition);
            Material.Destroy(debugSphereCharacterTargetPositionMaterial);
            GameObject.Destroy(debugSphereCharacterTargetBasePosition);
            Material.Destroy(debugSphereCharacterTargetBasePositionMaterial);
        }

        Transform GetGlobalCalculatorTransform() {
            var tr = calculator.transform;
            tr.position = Vector3.zero;
            tr.rotation = Quaternion.identity;
            tr.localScale = Vector3.one;
            return tr;
        }
    }
}
