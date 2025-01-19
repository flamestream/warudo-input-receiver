using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Prop;

namespace FlameStream
{
    [AssetType(
        Id = "FlameStream.Asset.GameInputGamepadReceiver",
        Title = "FS_ASSET_TITLE_GAMEINPUT_GAMEPAD",
        Category = "FS_ASSET_CATEGORY_INPUT"
    )]
    public partial class GamepadReceiverAsset : ReceiverAsset {

        protected override void OnCreate() {
            if (Port == 0) Port = DEFAULT_PORT;
            base.OnCreate();
            Watch(nameof(IsHandEnabled), delegate { OnIsHandEnabledChange(); });
            Watch(nameof(Character), delegate { OnIdleFingerAnimationChange(); });
            Watch(nameof(IdleFingerAnimation), delegate { OnIdleFingerAnimationChange(); });
        }

        public override void OnUpdate() {
            base.OnUpdate();
            PerformStateUpdateLoop();
            PerformShakingMotionLoop();
        }

        protected override void Log(string msg) {
            UnityEngine.Debug.Log($"[FlameStream.Asset.GamepadReceiver] {msg}");
        }

        /// <summary>
        /// GENERAL
        /// </summary>
        [Section("GENERAL_CONFIGURATION")]

        [DataInput]
        [Label("GAME_CONTROLLER_TYPE")]
        public GamepadType TargetGamepadType;

        [DataInput]
        [Label("DEFAULT_CONTROLLER_HAND")]
        public GamepadHandSide DefaultAnchorSide;

        /// <summary>
        /// BASIC SETUP
        /// </summary>
        [Section("BASIC_HAND_AND_PROP_SETUP")]

        [Markdown]
        [HiddenIf(nameof(IsBasicSetupDone))]
        public string BasicPropSetupInstructions = @"### Instructions

Make target controller hold controller in wanted neutral position, then set up anchors.";

        [DataInput]
        [DisabledIf(nameof(IsBasicSetupDone))]
        [Label("CHARACTER")]
        public CharacterAsset Character;

        [DataInput]
        [PreviewGallery]
        [AutoCompleteResource("CharacterAnimation", null)]
        [DisabledIf(nameof(IsBasicSetupDone))]
        [Label("IDLE_FINGER_ANIMATION")]
        public string IdleFingerAnimation;

        [DataInput]
        [DisabledIf(nameof(IsBasicSetupDone))]
        [Label("CONTROLLER")]
        public PropAsset Gamepad;

        [Trigger]
        [DisabledIf(nameof(IsBasicSetupInputMissing))]
        [HiddenIf(nameof(IsBasicSetupDone))]
        public void TriggerApplyBasicSetup() {
            ApplyBasicSetup();
        }

        [DataInput]
        [Label("ENABLE")]
        [HiddenIf(nameof(IsBasicSetupNotDone))]
        public bool IsHandEnabled;

        [Trigger]
        [Label("RESET_ALL_ANCHORS")]
        [HiddenIf(nameof(IsBasicSetupNotDone))]
        public void TriggerResetAllAnchors() {
            ResetAllAnchors();
        }

        [Trigger]
        [HiddenIf(nameof(IsBasicSetupNotDone))]
        [Label("CLEAR_ALL_ANCHORS")]
        public void TriggerClearAllAnchors() {
            ClearAllAnchors();
        }

        /// <summary>
        /// SHAKING AND TILTING MOTION
        /// </summary>
        [Section("SHAKING_AND_TILTING_MOTION")]

        [Markdown]
        [HiddenIf(nameof(CanConfigureShaking))]
        public string ShakingMotionInstructions = "Allows game controller to shake and tilt in response to input.";

        [DataInput]
        [Label("ENABLE")]
        [DisabledIf(nameof(IsBasicSetupNotDone))]
        public bool IsShakingEnabled;

        public bool CanConfigureShaking() {
            return IsShakingEnabled;
        }

        public bool CannotConfigureShaking() {
            return !IsShakingEnabled;
        }

        [DataInput]
        [HiddenIf(nameof(CannotConfigureShaking))]
        [FloatSlider(0.1f, 10f, 0.01f)]
        [Label("TILT_INFLUENCE_FACTOR")]
        float TiltInfluenceFactor = 1.0f;

        [DataInput]
        [HiddenIf(nameof(CannotConfigureShaking))]
        [FloatSlider(0.1f, 10f, 0.01f)]
        [Label("DISPLACEMENT_INFLUENCE_FACTOR")]
        float DisplacementInfluenceFactor = 1.0f;

        [DataInput]
        [HiddenIf(nameof(CannotConfigureShaking))]
        [DisabledIf(nameof(IsBasicSetupNotDone))]
        [Label("TILT_DISPLACEMENT_ENABLED")]
        public bool IsTiltDisplacementEnabled;

        /// <summary>
        /// HAND TRACKER
        /// </summary>
        [Section("HAND_TRACKER")]

        [Markdown]
        [HiddenIf(nameof(CanConfigureHandTracker))]
        public string HandTrackerInfo = @"Allows temporarily holding the game controller with the untracked hand while the other hand is being tracked.";

        [DataInput]
        [Label("ENABLE")]
        [DisabledIf(nameof(IsBasicSetupNotDone))]
        public bool IsHandTrackerEnabled;

        public bool CanConfigureHandTracker() {
            return IsHandTrackerEnabled;
        }

        public bool CannotConfigureHandTracker() {
            return !IsHandTrackerEnabled;
        }

        [Markdown]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        public string HandTrackerInstructions = @"### Setup Instructions
Go to your **Pose Tracking** blueprint and insert the **🔥🎮 Hand Tracker** node before the **Override Character Bone Rotations** node's **Bone Rotation Weights** input.";

        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [Label("HOLD_LEFT_HAND_TILT")]
        public Vector3 HoldLeftHandTilt = new Vector3(30f, 10f, 0f);
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [Label("HOLD_LEFT_HAND_DISPLACEMENT")]
        public Vector3 HoldLeftHandDisplacement = new Vector3(0f, -0.05f, -0.02f);

        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [Label("HOLD_RIGHT_HAND_TILT")]
        public Vector3 HoldRightHandTilt = new Vector3(30f, 10f, 0f);
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [Label("HOLD_RIGHT_HAND_DISPLACEMENT")]
        public Vector3 HoldRightHandDisplacement = new Vector3(0f, -0.05f, -0.02f);

        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [FloatSlider(0f, 2f, 0.01f)]
        [Label("TILT_TRANSITION_TIME")]
        public float TiltTransitionTime = 1.0f;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [Label("TILT_EASING")]
        public Ease TiltEasing = Ease.OutCubic;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [FloatSlider(0f, 2f, 0.01f)]
        [Label("DISPLACEMENT_TRANSITION_TIME")]
        public float DisplacementTransitionTime = 1.5f;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [Label("DISPLACEMENT_EASING")]
        public Ease DisplacementEasing = Ease.OutBack;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [FloatSlider(0f, 2f, 0.01f)]
        [Label("RETURN_TRANSITION_TIME")]
        public float ReturnTransitionTime = 0.2f;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [Label("RETURN_EASING")]
        public Ease ReturnEasing = Ease.OutBack;

        /// <summary>
        /// ANIMATION
        /// </summary>
        [Section("FINGER_AND_PROP_ANIMATION")]
        [Markdown]
        public string AnimationInstructions = @"### Game Controller Prop Setup
* The prop must have an Animator component with multiple named **Additive blending** layers for each wanted buttons
### Finger Animation
* Animations should have **Additive Reference Pose**
* Additive reference pose should be equivalent to the idle finger pose defined in the **General Configuration** section
* Idle finger pose must be resting thumbs on the two sticks";

        [Trigger]
        [Label("GENERATE_BUTTON_ANIMATION_DATA_TEMPLATE")]
        public void TriggerGenerateButtonAnimationDataTemplate() {
            GenerateButtonAnimationDataTemplate();
        }

        [DataInput]
        [Label("BUTTON_ANIMATION_DATA")]
        public GamepadButtonAnimationData[] ButtonAnimationData;

        [DataInput]
        [Label("DPAD_ANIMATION_DATA")]
        public GamepadDPadAnimationData[] DPadAnimationData;

        [DataInput]
        [Label("LEFT_STICK_ANIMATION_DATA")]
        public GamepadStickAnimationData LeftStickAnimationData;

        [DataInput]
        [Label("RIGHT_STICK_ANIMATION_DATA")]
        public GamepadStickAnimationData RightStickAnimationData;

        [Markdown]
        public string TriggerInstructions = @"The below operations need to be executed once, every time the above inputs are changed.";

        [Trigger]
        [Label("SYNC_CHARACTER_OVERLAYING_ANIMATION")]
        public void TriggerSyncCharacterOverlayingAnimations() {
            SyncCharacterOverlayingAnimations();
        }

        [Trigger]
        [Label("GENERATE_ANIMATION_BLUEPRINT")]
        public void TriggerGenerateAnimationBlueprint() {
            GenerateAnimationBlueprint();
        }
    }
}
