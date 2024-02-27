using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Prop;

namespace FlameStream
{
    [AssetType(
        Id = "FlameStream.Asset.GamepadReceiver",
        Title = "ASSET_TITLE_GAMEPAD"
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

        /// <summary>
        /// GENERAL
        /// </summary>
        [Section("General Configuration")]

        [DataInput]
        [Label("Game Controller Type")]
        public GamepadType TargetGamepadType;

        [DataInput]
        [Label("Default Controller Hand")]
        public GamepadHandSide DefaultAnchorSide;

        /// <summary>
        /// BASIC SETUP
        /// </summary>
        [Section("Basic Hand and Prop Setup")]

        [Markdown]
        [HiddenIf(nameof(IsBasicSetupDone))]
        public string BasicPropSetupInstructions = @"### Instructions

Make target controller hold controller in wanted neutral position, then set up anchors.";

        [DataInput]
        [DisabledIf(nameof(IsBasicSetupDone))]
        public CharacterAsset Character;

        [DataInput]
        [PreviewGallery]
        [AutoCompleteResource("CharacterAnimation", null)]
        [DisabledIf(nameof(IsBasicSetupDone))]
        public string IdleFingerAnimation;

        [DataInput]
        [DisabledIf(nameof(IsBasicSetupDone))]
        [Label("Game Controller")]
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
        [Label("🔁 Reset All Anchors")]
        [HiddenIf(nameof(IsBasicSetupNotDone))]
        public void TriggerResetAllAnchors() {
            ResetAllAnchors();
        }

        [Trigger]
        [HiddenIf(nameof(IsBasicSetupNotDone))]
        [Label("💣 Clear All Anchors")]
        public void TriggerClearAllAnchors() {
            ClearAllAnchors();
        }

        /// <summary>
        /// SHAKING AND TILTING MOTION
        /// </summary>
        [Section("Shaking and Tilting Motion")]

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
        float TiltInfluenceFactor = 1.0f;

        [DataInput]
        [HiddenIf(nameof(CannotConfigureShaking))]
        [FloatSlider(0.1f, 10f, 0.01f)]
        float DisplacementInfluenceFactor = 1.0f;

        [DataInput]
        [DisabledIf(nameof(IsBasicSetupNotDone))]
        public bool IsTiltDisplacementEnabled;

        /// <summary>
        /// HAND TRACKER
        /// </summary>
        [Section("Hand Tracker")]

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
        public Vector3 HoldLeftHandTilt = new Vector3(30f, 10f, 0f);
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        public Vector3 HoldLeftHandDisplacement = new Vector3(0f, -0.05f, -0.02f);

        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        public Vector3 HoldRightHandTilt = new Vector3(30f, 10f, 0f);
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        public Vector3 HoldRightHandDisplacement = new Vector3(0f, -0.05f, -0.02f);

        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [FloatSlider(0f, 2f, 0.01f)]
        public float TiltTransitionTime = 1.0f;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        public Ease TiltEasing = Ease.OutCubic;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [FloatSlider(0f, 2f, 0.01f)]
        public float DisplacementTransitionTime = 1.5f;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        public Ease DisplacementEasing = Ease.OutBack;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        [FloatSlider(0f, 2f, 0.01f)]
        public float ReturnTime = 0.2f;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureHandTracker))]
        public Ease ReturnEasing = Ease.OutBack;

        /// <summary>
        /// ANIMATION
        /// </summary>
        [Section("Finger and Prop Animation")]
        [Markdown]
        public string AnimationInstructions = @"### Game Controller Prop Setup
* The prop must have an Animator component with multiple named **Additive blending** layers for each wanted buttons
### Finger Animation
* Animations should have **Additive Reference Pose**
* Additive reference pose should be equivalent to the idle finger pose defined in the **General Configuration** section
* Idle finger pose must be resting thumbs on the two sticks";

        [Trigger]
        public void TriggerGenerateButtonAnimationTemplate() {
            GenerateButtonAnimationTemplate();
        }

        [DataInput]
        public GamepadButtonAnimationData[] ButtonAnimationData;

        [DataInput]
        public GamepadDPadAnimationData[] DPadAnimationData;

        [DataInput]
        public GamepadStickAnimationData LeftStickAnimationData;

        [DataInput]
        public GamepadStickAnimationData RightStickAnimationData;

        [Trigger]
        [Description("Modifies your character to support currently defined finger animations.")]
        public void TriggerSyncCharacterOverlayingAnimations() {
            SyncCharacterOverlayingAnimations();
        }

        [Trigger]
        public void TriggerGenerateAnimationBlueprint() {
            GenerateAnimationBlueprint();
        }
    }
}
