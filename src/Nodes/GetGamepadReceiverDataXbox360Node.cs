using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using static FlameStream.GamepadReceiverAsset;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.GetGamepadReceiverDataXbox360",
    Title = "NODE_TITLE_GAMEPAD_RECEIVER_XBOX360",
    Category = "FS_NODE_CATEGORY_GAMEPAD"
)]
    public class GetGamepadReceiverDataXbox360Node: GetGamepadReceiverDataNode {

        [DataOutput(50)]
        [Label("A_BUTTON")]
        public bool A() => Receiver?.ButtonFlag(Xbox360Button.A) ?? false;

        [DataOutput(50)]
        [Label("B_BUTTON")]
        public bool B() => Receiver?.ButtonFlag(Xbox360Button.B) ?? false;

        [DataOutput(50)]
        [Label("X_BUTTON")]
        public bool X() => Receiver?.ButtonFlag(Xbox360Button.X) ?? false;

        [DataOutput(50)]
        [Label("Y_BUTTON")]
        public bool Y() => Receiver?.ButtonFlag(Xbox360Button.Y) ?? false;

        [DataOutput(50)]
        [Label("LB_BUTTON")]
        public bool LB() => Receiver?.ButtonFlag(Xbox360Button.LB) ?? false;

        [DataOutput(50)]
        [Label("RB_BUTTON")]
        public bool RB() => Receiver?.ButtonFlag(Xbox360Button.RB) ?? false;

        [DataOutput(50)]
        [Label("LT_BUTTON")]
        public bool LT() => Receiver?.ButtonFlag(Xbox360Button.LT) ?? false;

        [DataOutput(50)]
        [Label("RT_BUTTON")]
        public bool RT() => Receiver?.ButtonFlag(Xbox360Button.RT) ?? false;

        [DataOutput(50)]
        [Label("LEFT_STICK")]
        public bool LeftStick() => Receiver?.ButtonFlag(Xbox360Button.LeftStick) ?? false;

        [DataOutput(50)]
        [Label("RIGHT_STICK")]
        public bool RightStick() => Receiver?.ButtonFlag(Xbox360Button.RightStick) ?? false;

        [DataOutput(50)]
        [Label("BACK_BUTTON")]
        public bool Back() => Receiver?.ButtonFlag(Xbox360Button.Back) ?? false;

        [DataOutput(50)]
        [Label("START_BUTTON")]
        public bool Start() => Receiver?.ButtonFlag(Xbox360Button.Start) ?? false;
    }
}
