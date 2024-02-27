using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.GamepadButtonHandAnimator",
    Title = "NODE_TITLE_GAMEPAD_BUTTON_HAND_ANIMATOR",
    Category = "NODE_CATEGORY")]
    public class GamepadButtonHandAnimatorNode : GamepadHandAnimatorNode {

        [DataInput(99)]
        [Label("HOVER_LAYER_ID")]
        public string HoverLayerId;
        [DataInput(199)]
        [Label("PRESS_LAYER_ID")]
        public string PressLayerId;

        [DataInput(500)]
        [Description("From receiver Button Press State")]
        [Label("IS_PRESSED")]
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
