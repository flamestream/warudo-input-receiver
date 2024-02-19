using System;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Prop;
using Warudo.Plugins.Core.Nodes;
using Warudo.Plugins.Core.Nodes.Event;

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
        public ControllerType TargetControllerType;

        [DataInput]
        [Label("Default Controller Hand")]
        public GamepadHandSide DefaultControllerAnchorSide;

        /// <summary>
        /// BASIC SETUP
        /// </summary>
        [Section("Basic Prop Setup")]

        [Markdown]
        [HiddenIf(nameof(IsBasicSetupDone))]
        public string BasicPropSetupInstructions = @"### Instructions

Make target controller hold controller in wanted neutral position, then set up anchors.";

        [DataInput]
        [DisabledIf(nameof(IsBasicSetupDone))]
        public CharacterAsset Character;

        [DataInput]
        [DisabledIf(nameof(IsBasicSetupDone))]
        [Label("Game Controller")]
        public PropAsset Gamepad;

        [Trigger]
        [DisabledIf(nameof(IsBasicSetupInputMissing))]
        [HiddenIf(nameof(IsBasicSetupDone))]
        public void TriggerSetupGamepadAnchors() {
            SetupGamepadAnchors();
        }

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
        /// SHAKING MOTION
        /// </summary>
        [Section("Shaking Motion")]

        [Markdown]
        [HiddenIf(nameof(CanConfigureShaking))]
        public string ShakingMotionInstructions = "Allows game controller to shake in response to input.";

        [DataInput]
        [Label("Enable")]
        [DisabledIf(nameof(IsBasicSetupNotDone))]
        public bool IsShakingEnabled;

        public bool CanConfigureShaking() {
            return IsShakingEnabled;
        }

        public bool CannotConfigureShaking() {
            return !IsShakingEnabled;
        }

        public enum ControllerType {
            SwitchProController,
            PS5Controller,
        }

        [DataInput]
        [HiddenIf(nameof(CannotConfigureShaking))]
        [FloatSlider(0.1f, 10f, 0.01f)]
        float TiltInfluenceFactor = 1.0f;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureShaking))]
        [FloatSlider(0.1f, 10f, 0.01f)]
        float DisplacementInfluenceFactor = 1.0f;

        /// <summary>
        /// HAND TRACKING
        /// </summary>
        [Section("Hand Tracking")]

        [Markdown]
        [HiddenIf(nameof(IsHandTrackingGraphPresent))]
        public string HandTrackingInstructions = @"### Information
Allows temporarily holding the game controller with the untracked hand while the other hand is being tracked.
### Instructions
Go to your **Pose Tracking** blueprint and insert the **🔥🎮 Hand Tracker** node before the **Override Character Bone Rotations** node's **Bone Rotation Weights** input.";

        string HandTrackingGraphId;

        bool IsHandTrackingGraphPresent() {
            return HandTrackingGraphId != null;
        }
        bool IsHandTrackingGraphBlank() {
            return !IsHandTrackingGraphPresent();
        }

        [Trigger]
        [HiddenIf(nameof(IsHandTrackingGraphPresent))]
        void GenerateBlueprint() {

            Graph graph = new Graph
            {
                Name = "🔥🎮 Hand Tracker",
                Enabled = true
            };

            CommentNode commentNode = graph.AddNode<CommentNode>();
            commentNode.SetDataInput("Text", $@"### Instructions

1. Place node corresponding to your hand tracker and feed it with the hand tracking flags.
2. Go to your **Pose Tracking** blueprint and insert the **🔥🎮 Hand Tracker** node before the **Override Character Bone Rotations** node's **Bone Rotation Weights** input. Feed it the hand tracking flags.
");

            OnUpdateNode onUpdateNode = graph.AddNode<OnUpdateNode>();

            GamepadHandSwitcherNode gamepadHandTrackerNode = graph.AddNode<GamepadHandSwitcherNode>();
            gamepadHandTrackerNode.Receiver = this;

            graph.AddFlowConnection(onUpdateNode, "Exit", gamepadHandTrackerNode, "Enter");

            base.Scene.AddGraph(graph);
            HandTrackingGraphId = graph.Id.ToString();
            Context.Service.PromptMessage("SUCCESS", $"Blueprint {graph.Name} has been succesfully generated.");
            Context.Service.BroadcastOpenedScene();
            Context.Service.NavigateToGraph(graph.Id, commentNode.Id);
        }

        [Trigger]
        [HiddenIf(nameof(IsHandTrackingGraphBlank))]
        async void DeleteGeneratedBlueprint() {

            Graph graph = Context.OpenedScene.GetGraph(Guid.Parse(HandTrackingGraphId));
            if (graph == null)
            {
                HandTrackingGraphId = null;
            }
            else if (await Context.Service.PromptConfirmation("WARNING", "BLUEPRINT_WILL_BE_REMOVED".Localized(graph.Name)))
            {
                Context.OpenedScene.RemoveGraph(graph.Id);
                Context.Service.BroadcastOpenedScene();
                HandTrackingGraphId = null;
            }
        }

        [DataInput]
        public Vector3 HoldLeftHandTilt = new Vector3(30f, 10f, 0f);
        [DataInput]
        public Vector3 HoldLeftHandDisplacement = new Vector3(0f, -0.05f, -0.02f);

        [DataInput]
        public Vector3 HoldRightHandTilt = new Vector3(30f, 10f, 0f);
        [DataInput]
        public Vector3 HoldRightHandDisplacement = new Vector3(0f, -0.05f, -0.02f);

        [DataInput]
        [FloatSlider(0f, 2f, 0.01f)]
        public float TiltTransitionTime = 1.0f;
        [DataInput]
        public Ease TiltEasing = Ease.OutCubic;
        [DataInput]
        [FloatSlider(0f, 2f, 0.01f)]
        public float DisplacementTransitionTime = 1.5f;
        [DataInput]
        public Ease DisplacementEasing = Ease.OutBack;
        [DataInput]
        [FloatSlider(0f, 2f, 0.01f)]
        public float ReturnTime = 0.2f;
        [DataInput]
        public Ease ReturnEasing = Ease.OutBack;

        /// <summary>
        /// ANIMATION
        /// </summary>
        [Section("Finger and Prop Animation")]
        [Markdown]
        public string AnimationInstructions = @"### Game Controller Prop Setup
* The prop must have an Animator component with multiple named Additive blending layers for each wanted buttons
* Map each button to the correct layer
### Finger Animation
* Finger animations files must be provided
* Two animations per button: hover and press
* Animations must have an additive reference pose";
    }
}
