using System;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Localization;
using Warudo.Plugins.Core.Nodes;
using Warudo.Plugins.Core.Nodes.Event;

namespace FlameStream {
    public partial class GamepadReceiverAsset : ReceiverAsset {

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

        Tween leftPositionTween;
        Tween leftRotationTween;

        Tween rightPositionTween;
        Tween rightRotationTween;

        // -1: Left
        //  0: None
        //  1: Right
        short firstTracked;
        bool LastIsLeftHandTracked;
        bool LastIsRightHandTracked;

        public short ProcessHandTracking(bool IsLeftHandTracked, bool IsRightHandTracked) {

            if (firstTracked == 0) {
                if (IsLeftHandTracked) {
                    firstTracked = -1;
                } else if (IsRightHandTracked) {
                    firstTracked = 1;
                }
            } else {
                if (!IsLeftHandTracked && !IsRightHandTracked) {
                    firstTracked = 0;
                }
            }

            if (IsLeftHandTracked && !LastIsLeftHandTracked && !IsRightHandTracked) {
                // Ensure gamepad is on right hand
                TransitionRightHandAnchorToHoldState();
            } else if (IsRightHandTracked && !LastIsRightHandTracked && !IsLeftHandTracked) {
                // Ensure gamepad is on left hand
                TransitionLeftHandAnchorToHoldState();
            } else if (!IsLeftHandTracked && !IsRightHandTracked && (LastIsLeftHandTracked || LastIsRightHandTracked)) {
                TransitionHandAnchorsToResetState();
            }

            LastIsLeftHandTracked = IsLeftHandTracked;
            LastIsRightHandTracked = IsRightHandTracked;

            return firstTracked;
        }

        public void TransitionRightHandAnchorToHoldState() {
            var leftAnchor = GamepadLeftHandAnchor;
            var rightAnchor = GamepadRightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            AttachGamepad(GamepadAnchorSide.RightHand);

            rightPositionTween?.Kill();
            rightPositionTween = DOTween.To(
                () => rightAnchor.Transform.Position,
                delegate(Vector3 it) { rightAnchor.Transform.Position = it; },
                RightHandAnchorPosition + HoldRightHandDisplacement,
                DisplacementTransitionTime
            ).SetEase(DisplacementEasing);
            rightRotationTween?.Kill();
            rightRotationTween = DOTween.To(
                () => rightAnchor.Transform.Rotation,
                delegate(Vector3 it) { rightAnchor.Transform.Rotation = it; },
                RightHandAnchorRotation + HoldLeftHandTilt,
                TiltTransitionTime
            ).SetEase(TiltEasing);
        }

        public void TransitionLeftHandAnchorToHoldState() {
            var leftAnchor = GamepadLeftHandAnchor;
            var rightAnchor = GamepadRightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            AttachGamepad(GamepadAnchorSide.LeftHand);

            leftPositionTween?.Kill();
            leftPositionTween = DOTween.To(
                () => leftAnchor.Transform.Position,
                delegate(Vector3 it) { leftAnchor.Transform.Position = it; },
                LeftHandAnchorPosition + HoldLeftHandDisplacement,
                DisplacementTransitionTime
            ).SetEase(DisplacementEasing);

            leftRotationTween?.Kill();
            leftRotationTween = DOTween.To(
                () => leftAnchor.Transform.Rotation,
                delegate(Vector3 it) { leftAnchor.Transform.Rotation = it; },
                LeftHandAnchorRotation + HoldLeftHandTilt,
                TiltTransitionTime
            ).SetEase(TiltEasing);
        }

        public void TransitionHandAnchorsToResetState() {
            var leftAnchor = GamepadLeftHandAnchor;
            var rightAnchor = GamepadRightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            rightPositionTween?.Kill();
            rightPositionTween = DOTween.To(
                () => rightAnchor.Transform.Position,
                delegate(Vector3 it) { rightAnchor.Transform.Position = it; },
                RightHandAnchorPosition,
                ReturnTime
            ).SetEase(ReturnEasing);

            rightRotationTween?.Kill();
            rightRotationTween = DOTween.To(
                () => rightAnchor.Transform.Rotation,
                delegate(Vector3 it) { rightAnchor.Transform.Rotation = it; },
                RightHandAnchorRotation,
                ReturnTime
            ).SetEase(ReturnEasing);

            leftPositionTween?.Kill();
            leftPositionTween = DOTween.To(
                () => leftAnchor.Transform.Position,
                delegate(Vector3 it) { leftAnchor.Transform.Position = it; },
                LeftHandAnchorPosition,
                ReturnTime
            ).SetEase(ReturnEasing);

            leftRotationTween?.Kill();
            leftRotationTween = DOTween.To(
                () => leftAnchor.Transform.Rotation,
                delegate(Vector3 it) { leftAnchor.Transform.Rotation = it; },
                LeftHandAnchorRotation,
                ReturnTime
            ).SetEase(ReturnEasing);
        }
    }
}