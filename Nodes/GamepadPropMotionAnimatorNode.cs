using System;
using System.Data;
using System.Runtime.Serialization;
using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GamepadPropMotionAnimator",
    Title = "Gamepad Prop Motion Animator",
    Category ="NODE_CATEGORY")]
    public class GamepadPropMotionAnimatorNode : Node {

        [DataInput]
        public GamepadReceiverAsset Receiver;

        [DataInput]
        public float StickInfluenceFactor = 1.0f;

        [DataInput]
        public float LeftStickX;
        [DataInput]
        public float LeftStickY;
        [DataInput]
        public float RightStickX;
        [DataInput]
        public float RightStickY;
        [DataInput]
        public bool AnyFaceButton;
        [DataInput]
        public bool AnyShoulderButton;

        [FlowInput]
        public Continuation Enter() {
            MainLoop();
            return Exit;
        }

        bool LastAnyFaceButton;
        bool LastAnyShoulderButton;
        protected Tween rotationTween;
        protected Tween positionTween;

        [FlowOutput]
        public Continuation Exit;

        void MainLoop() {
            var anchor = Receiver?.GamepadAnchor;
            if (anchor == null) return;

            var influenceX = LeftStickX + RightStickX;
            var influenceY = LeftStickY + RightStickY;

            var tilt = new Vector3(-influenceY, 0, -influenceX) * StickInfluenceFactor;

            // anchor.Transform.Rotation = Receiver.GamepadAnchorRotation + tilt;
            rotationTween?.Kill();
            rotationTween = DOTween.To(
                () => anchor.Transform.Rotation,
                delegate(Vector3 it) { anchor.Transform.Rotation = (it); },
                Receiver.GamepadAnchorRotation + tilt,
                0.1f
            ).SetEase(Ease.Linear);

            if (AnyFaceButton && !LastAnyFaceButton) {

            }

            rotationTween?.Kill();
            rotationTween = DOTween.To(
                () => anchor.Transform.Rotation,
                delegate(Vector3 it) { anchor.Transform.Rotation = (it); },
                Receiver.GamepadAnchorRotation + tilt,
                0.1f
            ).SetEase(Ease.Linear);

            LastAnyFaceButton = AnyFaceButton;
            LastAnyShoulderButton = AnyShoulderButton;
        }
    }
}
