using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Plugins.Core.Nodes;
using Warudo.Plugins.Core.Nodes.Event;

namespace FlameStream
{
    public partial class GamepadReceiverAsset : ReceiverAsset {

        Tween leftHandPositionTween;
        Tween leftHandRotationTween;

        Tween rightHandPositionTween;
        Tween rightHandRotationTween;

        // Indicates which hand was tracked first, to determins which hand gets to hold controller
        // -1: Left
        //  0: None
        //  1: Right
        short priorityTrackedHand;
        bool LastIsLeftHandTracked;
        bool LastIsRightHandTracked;

//         void DoGenerateBlueprint() {

//             Graph graph = new Graph
//             {
//                 Name = "🔥🎮 Hand Tracker",
//                 Enabled = true
//             };

//             CommentNode commentNode = graph.AddNode<CommentNode>();
//             commentNode.SetDataInput("Text", $@"### Instructions

// 1. Place node corresponding to your hand tracker and feed it with the hand tracking flags.
// 2. Go to your **Pose Tracking** blueprint and insert the **🔥🎮 Hand Tracker** node before the **Override Character Bone Rotations** node's **Bone Rotation Weights** input. Feed it the hand tracking flags.
// ");

//             OnUpdateNode onUpdateNode = graph.AddNode<OnUpdateNode>();

//             GamepadHandSwitcherNode gamepadHandTrackerNode = graph.AddNode<GamepadHandSwitcherNode>();
//             gamepadHandTrackerNode.Receiver = this;

//             graph.AddFlowConnection(onUpdateNode, "Exit", gamepadHandTrackerNode, "Enter");

//             base.Scene.AddGraph(graph);
//             HandTrackingGraphId = graph.Id.ToString();
//             Context.Service.PromptMessage("SUCCESS", $"Blueprint {graph.Name} has been succesfully generated.");
//             Context.Service.BroadcastOpenedScene();
//             Context.Service.NavigateToGraph(graph.Id, commentNode.Id);
//         }

        // Called by node, since I don't really know how to associate tracker asset (like LeapMotion)
        public short ProcessHandTracking(bool IsLeftHandTracked, bool IsRightHandTracked) {

            if (priorityTrackedHand == 0) {
                if (IsLeftHandTracked) {
                    priorityTrackedHand = -1;
                } else if (IsRightHandTracked) {
                    priorityTrackedHand = 1;
                }
            } else {
                if (!IsLeftHandTracked && !IsRightHandTracked) {
                    priorityTrackedHand = 0;
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

            return priorityTrackedHand;
        }

        public void TransitionRightHandAnchorToHoldState() {
            var leftAnchor = LeftHandAnchor;
            var rightAnchor = RightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            AttachGamepad(GamepadHandSide.RightHand);

            rightHandPositionTween?.Kill();
            rightHandPositionTween = DOTween.To(
                () => rightAnchor.Transform.Position,
                delegate(Vector3 it) { rightAnchor.Transform.Position = it; },
                RightHandAnchorPosition + HoldRightHandDisplacement,
                DisplacementTransitionTime
            ).SetEase(DisplacementEasing);

            rightHandRotationTween?.Kill();
            rightHandRotationTween = DOTween.To(
                () => rightAnchor.Transform.Rotation,
                delegate(Vector3 it) { rightAnchor.Transform.Rotation = it; },
                RightHandAnchorRotation + HoldLeftHandTilt,
                TiltTransitionTime
            ).SetEase(TiltEasing);
        }

        public void TransitionLeftHandAnchorToHoldState() {
            var leftAnchor = LeftHandAnchor;
            var rightAnchor = RightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            AttachGamepad(GamepadHandSide.LeftHand);

            leftHandPositionTween?.Kill();
            leftHandPositionTween = DOTween.To(
                () => leftAnchor.Transform.Position,
                delegate(Vector3 it) { leftAnchor.Transform.Position = it; },
                LeftHandAnchorPosition + HoldLeftHandDisplacement,
                DisplacementTransitionTime
            ).SetEase(DisplacementEasing);

            leftHandRotationTween?.Kill();
            leftHandRotationTween = DOTween.To(
                () => leftAnchor.Transform.Rotation,
                delegate(Vector3 it) { leftAnchor.Transform.Rotation = it; },
                LeftHandAnchorRotation + HoldLeftHandTilt,
                TiltTransitionTime
            ).SetEase(TiltEasing);
        }

        public void TransitionHandAnchorsToResetState() {
            var leftAnchor = LeftHandAnchor;
            var rightAnchor = RightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            rightHandPositionTween?.Kill();
            rightHandPositionTween = DOTween.To(
                () => rightAnchor.Transform.Position,
                delegate(Vector3 it) { rightAnchor.Transform.Position = it; },
                RightHandAnchorPosition,
                ReturnTime
            ).SetEase(ReturnEasing);

            rightHandRotationTween?.Kill();
            rightHandRotationTween = DOTween.To(
                () => rightAnchor.Transform.Rotation,
                delegate(Vector3 it) { rightAnchor.Transform.Rotation = it; },
                RightHandAnchorRotation,
                ReturnTime
            ).SetEase(ReturnEasing);

            leftHandPositionTween?.Kill();
            leftHandPositionTween = DOTween.To(
                () => leftAnchor.Transform.Position,
                delegate(Vector3 it) { leftAnchor.Transform.Position = it; },
                LeftHandAnchorPosition,
                ReturnTime
            ).SetEase(ReturnEasing);

            leftHandRotationTween?.Kill();
            leftHandRotationTween = DOTween.To(
                () => leftAnchor.Transform.Rotation,
                delegate(Vector3 it) { leftAnchor.Transform.Rotation = it; },
                LeftHandAnchorRotation,
                ReturnTime
            ).SetEase(ReturnEasing);
        }
    }
}
