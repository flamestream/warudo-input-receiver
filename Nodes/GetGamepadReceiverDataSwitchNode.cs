using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using static FlameStream.GamepadReceiverAsset;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.GetGamepadReceiverDataNode",
    Title = "NODE_TITLE_GAMEPAD_RECEIVER_SWITCH",
    Category = "NODE_CATEGORY"
)]
    public class GetGamepadReceiverDataSwitchNode: GetGamepadReceiverDataNode {

        [DataOutput(50)]
        [Label("A_BUTTON")]
        public bool A() => Receiver?.ButtonFlag(SwitchProButton.A) ?? false;

        [DataOutput(50)]
        [Label("B_BUTTON")]
        public bool B() => Receiver?.ButtonFlag(SwitchProButton.B) ?? false;

        [DataOutput(50)]
        [Label("X_BUTTON")]
        public bool X() => Receiver?.ButtonFlag(SwitchProButton.X) ?? false;

        [DataOutput(50)]
        [Label("Y_BUTTON")]
        public bool Y() => Receiver?.ButtonFlag(SwitchProButton.Y) ?? false;

        [DataOutput(50)]
        [Label("L_BUTTON")]
        public bool L() => Receiver?.ButtonFlag(SwitchProButton.L) ?? false;

        [DataOutput(50)]
        [Label("R_BUTTON")]
        public bool R() => Receiver?.ButtonFlag(SwitchProButton.R) ?? false;

        [DataOutput(50)]
        [Label("ZL_BUTTON")]
        public bool ZL() => Receiver?.ButtonFlag(SwitchProButton.ZL) ?? false;

        [DataOutput(50)]
        [Label("ZR_BUTTON")]
        public bool ZR() => Receiver?.ButtonFlag(SwitchProButton.ZR) ?? false;

        [DataOutput(50)]
        [Label("LEFT_STICK")]
        public bool LeftStick() => Receiver?.ButtonFlag(SwitchProButton.LeftStick) ?? false;

        [DataOutput(50)]
        [Label("RIGHT_STICK")]
        public bool RightStick() => Receiver?.ButtonFlag(SwitchProButton.RightStick) ?? false;

        [DataOutput(50)]
        [Label("PLUS_BUTTON")]
        public bool Plus() => Receiver?.ButtonFlag(SwitchProButton.Plus) ?? false;

        [DataOutput(50)]
        [Label("MINUS_BUTTON")]
        public bool Minus() => Receiver?.ButtonFlag(SwitchProButton.Minus) ?? false;

        [DataOutput(50)]
        [Label("HOME_BUTTON")]
        public bool Home() => Receiver?.ButtonFlag(SwitchProButton.Home) ?? false;

        [DataOutput(50)]
        [Label("CAPTURE_BUTTON")]
        public bool Capture() => Receiver?.ButtonFlag(SwitchProButton.Capture) ?? false;
    }
}
