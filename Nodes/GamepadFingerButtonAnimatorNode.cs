using System;
using DG.Tweening;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Utils;
using Warudo.Plugins.Core.Assets.Character;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GamepadFingerButtonAnimator",
    Title = "Gamepad Finger Button Animator",
    Category = "NODE_CATEGORY")]
    public class GamepadFingerButtonAnimatorNode : GamepadFingerAnimatorNode {

        [DataInput(99)]
        public string HoverLayerId;
        [DataInput(199)]
        public string PressLayerId;

        [DataInput(500)]
        [Description("From receiver Button Press State")]
        public bool IsPressed;

        protected bool lastIsPressed;

        protected override bool IsActivationFrame {
            get {
                return IsPressed && !lastIsPressed;
            }
        }

        protected override void MainLoop() {
            ProcessAnimation(HoverLayerId, PressLayerId, IsHovering, IsPressed);
        }

        protected override void AfterMainLoop() {
            lastIsPressed = IsPressed;
        }
    }
}
