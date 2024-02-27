using System;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Utils;
using Warudo.Plugins.Core.Assets.Character;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GamepadDPadHandAnimator",
    Title = "NODE_TITLE_GAMEPAD_DPAD_HAND_ANIMATOR",
    Category = "NODE_CATEGORY")]
    public class GamepadDPadHandAnimatorNode : GamepadHandAnimatorNode {

        [DataInput]
        [Label("D1_HOVER_LAYER_ID")]
        public string D1HoverLayerId;
        [DataInput]
        [Label("D1_PRESS_LAYER_ID")]
        public string D1PressLayerId;
        [DataInput]
        [Label("D2_HOVER_LAYER_ID")]
        public string D2HoverLayerId;
        [DataInput]
        [Label("D2_PRESS_LAYER_ID")]
        public string D2PressLayerId;
        [DataInput]
        [Label("D3_HOVER_LAYER_ID")]
        public string D3HoverLayerId;
        [DataInput]
        [Label("D3_PRESS_LAYER_ID")]
        public string D3PressLayerId;
        [DataInput]
        [Label("D4_HOVER_LAYER_ID")]
        public string D4HoverLayerId;
        [DataInput]
        [Label("D4_PRESS_LAYER_ID")]
        public string D4PressLayerId;
        [DataInput]
        [Label("D6_HOVER_LAYER_ID")]
        public string D6HoverLayerId;
        [DataInput]
        [Label("D6_PRESS_LAYER_ID")]
        public string D6PressLayerId;
        [DataInput]
        [Label("D7_HOVER_LAYER_ID")]
        public string D7HoverLayerId;
        [DataInput]
        [Label("D7_PRESS_LAYER_ID")]
        public string D7PressLayerId;
        [DataInput]
        [Label("D8_HOVER_LAYER_ID")]
        public string D8HoverLayerId;
        [DataInput]
        [Label("D8_PRESS_LAYER_ID")]
        public string D8PressLayerId;
        [DataInput]
        [Label("D9_HOVER_LAYER_ID")]
        public string D9HoverLayerId;
        [DataInput]
        [Label("D9_PRESS_LAYER_ID")]
        public string D9PressLayerId;

        [DataInput(500)]
        [Label("DPAD")]
        public int DPad;

        public GamepadDPadHandAnimatorNode() {
            _HideInputId = true;
        }

        bool lastIsPressed;

        protected override void MainLoop() {
            ProcessAnimation(D1HoverLayerId, D1PressLayerId, HoverInputId == "D1", DPad == 1);
            ProcessAnimation(D2HoverLayerId, D2PressLayerId, HoverInputId == "D2", DPad == 2);
            ProcessAnimation(D3HoverLayerId, D3PressLayerId, HoverInputId == "D3", DPad == 3);
            ProcessAnimation(D4HoverLayerId, D4PressLayerId, HoverInputId == "D4", DPad == 4);
            ProcessAnimation(D6HoverLayerId, D6PressLayerId, HoverInputId == "D6", DPad == 6);
            ProcessAnimation(D7HoverLayerId, D7PressLayerId, HoverInputId == "D7", DPad == 7);
            ProcessAnimation(D8HoverLayerId, D8PressLayerId, HoverInputId == "D8", DPad == 8);
            ProcessAnimation(D9HoverLayerId, D9PressLayerId, HoverInputId == "D9", DPad == 9);
        }

        protected override void AfterMainLoop() {
            lastIsPressed = IsPressed;
        }

        bool IsPressed {
            get {
                return DPad != 5;
            }
        }

        protected override bool IsActivationFrame {
            get {
                return IsPressed && !lastIsPressed;
            }
        }
    }
}
