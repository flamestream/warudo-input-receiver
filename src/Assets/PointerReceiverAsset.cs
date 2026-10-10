using Cysharp.Threading.Tasks;
using System.Linq;
using uDesktopDuplication;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Utility;

namespace FlameStream {
    [AssetType(
        Id = "FlameStream.Asset.PointerReceiver",
        Title = "FS_ASSET_TITLE_POINTER",
        Category = "FS_ASSET_CATEGORY_INPUT"
    )]
    public partial class PointerReceiverAsset : ReceiverAsset {

        protected override string CHARACTER_ANIM_LAYER_ID_PREFIX {
            get {
                return "🔥🖱️";
            }
        }

        protected override ushort PROTOCOL_VERSION {
            get {
                return 1;
            }
        }

        protected override string PROTOCOL_ID {
            get {
                return "P";
            }
        }

        protected override int DEFAULT_PORT {
            get {
                return 40610;
            }
        }

        protected override int CURRENT_ASSET_VERSION => 2;

        protected override void OnPostMigrateAsset(int fromVersion, int toVersion) {
            base.OnPostMigrateAsset(fromVersion, toVersion);
            if (fromVersion < 2 && HandDynamicRotation != null) {
                HandDynamicRotation.Auto = false;
                HandDynamicRotation.SetDataInput(nameof(HandDynamicRotation.Auto), false, true);
            }
        }

        public string LayerPrefix { get; set; } = LAYER_NAME_PREFIX;

        /// <summary>
        /// BASIC SETUP
        /// </summary>
        [Section("BASIC_SETUP")]
        [Markdown]
        public string DelegationStatus = "";

        [DataInput]
        [Label("ENABLE")]
        public bool IsHandEnabled;

        [DataInput]
        [Label("CHARACTER")]
        [Description("CHARACTER_DESC")]
        public CharacterAsset Character;

        private MultiInputCoordinatorAsset activeCoordinator;
        public MultiInputCoordinatorAsset ActiveCoordinator => activeCoordinator;
        public bool IsHandPuppeteered => activeCoordinator != null && activeCoordinator.IsEnabled && activeCoordinator.EnableHands;

        public void RegisterCoordinator(MultiInputCoordinatorAsset coordinator) {
            if (activeCoordinator == coordinator) return;
            activeCoordinator = coordinator;
            OnHandDelegationChanged();
            OnCoordinatorStateChanged();
            RequestUiRefresh();
        }

        public void UnregisterCoordinator(MultiInputCoordinatorAsset coordinator, bool isTeardown = false) {
            if (activeCoordinator != coordinator) return;
            activeCoordinator = null;
            OnHandDelegationChanged();
            if (!isTeardown && !isDestroyed) {
                OnCoordinatorStateChanged();
                RequestUiRefresh();
            }
        }

        protected override bool ShouldStartReceiver() {
            if (activeCoordinator != null && activeCoordinator.IsEnabled) {
                return false;
            }
            return true;
        }

        public void OnCoordinatorStateChanged() {
            if (isDestroyed) return;
            bool isPuppeteered = activeCoordinator != null && activeCoordinator.IsEnabled;
            SetDataInputPropertyAndBroadcast(
                nameof(IsEnabled),
                disabled: isPuppeteered,
                description: isPuppeteered
                    ? string.Format("IK_DELEGATED_TO".Localized(), "ENABLE".Localized(), activeCoordinator.Name)
                    : "POINTER_ENABLE_DESC".Localized()
            );

            if (isPuppeteered) {
                if (IsSocketListening) {
                    StopReceiver();
                    StartReceiver();
                }
            } else if (IsEnabled && !IsSocketListening) {
                StopReceiver();
                StartReceiver();
            }

            bool shouldEnableHands = isPuppeteered ? activeCoordinator.EnableHands : (IsEnabled && IsHandEnabled);
            if (shouldEnableHands) {
                OnSurfaceSettingChange();
                OnPropEnabledChanged();
                OnBodyEnabledChanged();
                SynchronizeHandAnimationLayers();
                OnInputAffectingHandSetupChange();
            } else {
                CleanupCharacterAnimationLayers();
                if (!IsEnabled && (ScreenSetup?.RemoveOnDisable ?? false)) {
                    DestroySurfaceScreen();
                    DestroyCursorAnchor();
                } else {
                    OnSurfaceSettingChange();
                }
            }
            OnBasicSettingChange();
            RequestUiRefresh();
        }

        public string GetDelegationStatusMessage() {
            if (activeCoordinator != null && activeCoordinator.IsEnabled) {
                var handStr = (IsRightHanded ? "RIGHT_HAND" : "LEFT_HAND").Localized();
                return string.Format("IK_DELEGATED_TO".Localized(), handStr, activeCoordinator.Name);
            }
            if (Character != null && IsEnabled && IsHandEnabled) {
                var dominantLimb = IsRightHanded ? Character.RightHandIK : Character.LeftHandIK;
                var handStr = (IsRightHanded ? "RIGHT_HAND" : "LEFT_HAND").Localized();

                if (dominantLimb != null && dominantLimb.Enabled && dominantLimb.IkTarget != null && (handAnchor == null || dominantLimb.IkTarget != handAnchor)) {
                    return string.Format("IK_NOT_CONTROLLING_HAND".Localized(), handStr, dominantLimb.IkTarget.Name);
                }
                if (dominantLimb != null && !dominantLimb.Enabled && handAnchor != null) {
                    return string.Format("IK_NOT_ACTIVE".Localized(), handStr);
                }

                var allReceivers = Scene?.GetAssets<PointerReceiverAsset>();
                if (allReceivers != null) {
                    bool multipleActivePointers = allReceivers.Any(p => p != this && p.Character == Character && p.IsEnabled && p.IsHandEnabled && p.ActiveCoordinator == null);
                    if (multipleActivePointers) {
                        return "⚠️ " + "IK_CONFLICT_MULTIPLE_POINTERS".Localized();
                    }
                }
                var allKeyboards = Scene?.GetAssets<KeyboardReceiverAsset>();
                if (allKeyboards != null) {
                    bool conflict = allKeyboards.Any(k => k.Character == Character && k.IsEnabled && k.IsHandEnabled && k.ActiveCoordinator == null);
                    if (conflict) {
                        return string.Format("IK_CONFLICT_DETECTED".Localized(), Character.Name);
                    }
                }
                var allGamepads = Scene?.GetAssets<GamepadReceiverAsset>();
                if (allGamepads != null) {
                    bool conflict = allGamepads.Any(g => g.Character == Character && g.IsEnabled && g.IsHandEnabled);
                    if (conflict) {
                        return string.Format("IK_CONFLICT_DETECTED".Localized(), Character.Name);
                    }
                }
            }
            return "";
        }

        public void UpdateUiStatus() {
            var newDelegation = GetDelegationStatusMessage();
            if (DelegationStatus != newDelegation) {
                DelegationStatus = newDelegation;
                BroadcastDataInput(nameof(DelegationStatus));
            }
        }


        [DataInput]
        [AutoComplete(nameof(AutoCompleteDisplayName), true, "")]
        [Label("SCREEN_DISPLAY")]
        public string DisplayName;
        async UniTask<AutoCompleteList> AutoCompleteDisplayName() {
            return AutoCompleteList.Single(Manager.monitors.Select((Monitor it) => new AutoCompleteEntry
            {
                label = it.name,
                value = it.name
            }).ToList());
        }

        [DataInput]
        [Label("DRAWING_SCREEN_SETUP")]
        [Description("DRAWING_SCREEN_SETUP_DESC")]
        public PointerDrawingScreenSetup ScreenSetup;

        [DataInput]
        [Label("CURSOR_SETUP")]
        [Description("CURSOR_SETUP_DESC")]
        public CursorMode CursorMode;

        [DataInput]
        [Label("ACTIVE_HAND_SETUP")]
        [Description("ACTIVE_HAND_SETUP_DESC")]
        public PointerActiveHandSetup ActiveHand;

        [DataInput]
        [Label("REST_HAND_SETUP")]
        [Description("REST_HAND_SETUP_DESC")]
        public PointerHandRestSetup RestHand;

        [DataInput]
        [Label("ACTIVE_TRANSITION")]
        [Description("ACTIVE_TRANSITION_DESC")]
        public ActiveTransition ActiveTransition;

        [DataInput]
        [Label("INACTIVE_TRANSITION")]
        [Description("INACTIVE_TRANSITION_DESC")]
        public InactiveTransition InactiveTransition;

        [DataInput]
        [Hidden]
        public bool IsRightHanded = true;

        [DataInput]
        [Hidden]
        public float CursorSmoothness = 0.05f;

        /// <summary>
        /// HAND MOVEMENT
        /// </summary>
        [Section("HAND_MOVEMENT")]
        [DataInput]
        [Label("MOUSE_MODE")]
        public MouseHandState HandMouseMode;

        [DataInput]
        [Label("PEN_MODE")]
        public PenHandState HandPenMode;

        [DataInput]
        [Label("DYNAMIC_ROTATION")]
        public HandRotationDynamicVector3 HandDynamicRotation;

        /// <summary>
        /// HAND PROP CONTROL
        /// </summary>
        [Section("HAND_PROP_CONTROL")]
        [DataInput]
        [Label("ENABLE")]
        public bool IsPropEnabled;

        [DataInput]
        [Label("PROP_SOURCE")]
        [AutoComplete(nameof(GetPropSources), true, "NONE")]
        public string PropSource;

        [DataInput]
        [Label("MOUSE_MODE")]
        public MousePropState PropMouseMode;

        [DataInput]
        [Label("PEN_MODE")]
        public PenPropState PropPenMode;

        /// <summary>
        /// MOVE BODY
        /// </summary>
        [Section("BODY_MOVEMENT")]
        [DataInput]
        [Label("ENABLE")]
        public bool IsBodyEnabled;

        [DataInput]
        [Label("MOUSE_MODE")]
        public MouseBodyState BodyMouseMode;

        [DataInput]
        [Label("PEN_MODE")]
        public PenBodyState BodyPenMode;

        [DataInput]
        [Label("MOVEMENT_TYPE")]
        public Movement BodyMovementType;

        [DataInput]
        [Label("MINIMUM_MOVEMENT_DELTA")]
        [FloatSlider(0f, 5f, 0.1f)]
        public float BodyMinimumMovementDelta = 0f;

        [DataInput]
        [Label("DYNAMIC_ROTATION")]
        public BodyRotationDynamicVector3 BodyDynamicRotation;

        public bool IsInScreenVisualSetup => ScreenSetup?.Transform?.InVisualSetupMode ?? false;

        [DataInput]
        [Hidden]
        public bool HasConfiguredScreenTransform;

        [DataInput]
        [Hidden]
        public bool HasSavedScreenTransform;

        [DataInput]
        [Hidden]
        public UnityEngine.Vector3 SavedScreenPosition;

        [DataInput]
        [Hidden]
        public UnityEngine.Vector3 SavedScreenRotation;

        [DataInput]
        [Hidden]
        public UnityEngine.Vector3 SavedScreenScale = new UnityEngine.Vector3(0.07f, 0.07f, 0.07f);

        [DataInput]
        [Hidden]
        public bool IsScreenDisableBasedOnAssetStateWanted = true;

        /// <summary>
        /// ADVANCED
        /// </summary>
        [Section("ADVANCED")]
        [DataInput(1000)]
        [Label("NEUTRAL_POSITION")]
        public NaturalPositionVisualSetupTransform NeutralHandPosition;

        [DataInput(1001)]
        [Label("POINTER_FACTOR_CORRECTION")]
        [Description("POINTER_FACTOR_CORRECTION_DESC")]
        [FloatSlider(0.01f, 2f, 0.01f)]
        public float PointerFactorCorrection = 1;

        [DataInput(1002)]
        [Label("DEBUG_MODE_SCALE_FACTOR")]
        [FloatSlider(0.01f, 10f, 0.01f)]
        public float DebugSphereScaleFactor = 1;

        [Trigger(1003)]
        [Label("FORCE_DEBUG_MODE")]
        public void TriggerForceDebugSpheres() {
            DisplayDebugSpheres(true);
            OnDebugSettingChange();
        }

        [Trigger(1004)]
        [Hidden]
        [Label("DISABLE_DEBUG_MODE")]
        public void TriggerDestroyDebugSpheres() {
            DestroyDebugSpheres(true);
            OnDebugSettingChange();
        }

        public void ShowToast(string message, Warudo.Core.Server.ToastSeverity severity = Warudo.Core.Server.ToastSeverity.Info) {
            Warudo.Core.Context.Service?.Toast(severity, Name, message);
        }
    }
}
