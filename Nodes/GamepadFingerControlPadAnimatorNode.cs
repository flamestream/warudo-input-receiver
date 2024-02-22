using System;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Utils;
using Warudo.Plugins.Core.Assets.Character;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GamepadFingerControlPadAnimator",
    Title = "Gamepad Finger Control Pad Animator",
    Category = "NODE_CATEGORY")]
    public class GamepadFingerControlPadAnimatorNode : GamepadFingerAnimatorNode {

        [DataInput]
        public string D1HoverLayerId = "d1h";
        [DataInput]
        public string D1PressLayerId = "d1p";
        [DataInput]
        public string D2HoverLayerId = "d2h";
        [DataInput]
        public string D2PressLayerId = "d2p";
        [DataInput]
        public string D3HoverLayerId = "d3h";
        [DataInput]
        public string D3PressLayerId = "d3p";
        [DataInput]
        public string D4HoverLayerId = "d4h";
        [DataInput]
        public string D4PressLayerId = "d4p";
        [DataInput]
        public string D6HoverLayerId = "d6h";
        [DataInput]
        public string D6PressLayerId = "d6p";
        [DataInput]
        public string D7HoverLayerId = "d7h";
        [DataInput]
        public string D7PressLayerId = "d7p";
        [DataInput]
        public string D8HoverLayerId = "d8h";
        [DataInput]
        public string D8PressLayerId = "d8p";
        [DataInput]
        public string D9HoverLayerId = "d9h";
        [DataInput]
        public string D9PressLayerId = "d9p";

        [DataInput(500)]
        public int ControlPadState;

        public GamepadFingerControlPadAnimatorNode() {
            _HideInputId = true;
        }

        bool lastIsPressed;

        protected override void MainLoop() {
            ProcessAnimation(D1HoverLayerId, D1PressLayerId, HoverInputId == "D1", ControlPadState == 1);
            ProcessAnimation(D2HoverLayerId, D2PressLayerId, HoverInputId == "D2", ControlPadState == 2);
            ProcessAnimation(D3HoverLayerId, D3PressLayerId, HoverInputId == "D3", ControlPadState == 3);
            ProcessAnimation(D4HoverLayerId, D4PressLayerId, HoverInputId == "D4", ControlPadState == 4);
            ProcessAnimation(D6HoverLayerId, D6PressLayerId, HoverInputId == "D6", ControlPadState == 6);
            ProcessAnimation(D7HoverLayerId, D7PressLayerId, HoverInputId == "D7", ControlPadState == 7);
            ProcessAnimation(D8HoverLayerId, D8PressLayerId, HoverInputId == "D8", ControlPadState == 8);
            ProcessAnimation(D9HoverLayerId, D9PressLayerId, HoverInputId == "D9", ControlPadState == 9);
        }

        protected override void AfterMainLoop() {
            lastIsPressed = IsPressed;
        }

        bool IsPressed {
            get {
                return ControlPadState != 5;
            }
        }

        protected override bool IsActivationFrame {
            get {
                return IsPressed && !lastIsPressed;
            }
        }
    }
}
