using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using static FlameStream.GamepadReceiverAsset;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GetPs5GamepadReceiverData",
    Title = "NODE_TITLE_GAMEPAD_RECEIVER_PS5",
    Category = "NODE_CATEGORY"
)]
    public class GetGamepadReceiverDataPs5Node : GetGamepadReceiverDataNode {

        [DataOutput(50)]
        [Label("CIRCLE_BUTTON")]
        public bool Circle() => Receiver?.ButtonFlag(PS5Button.Circle) ?? false;

        [DataOutput(50)]
        [Label("CROSS_BUTTON")]
        public bool Cross() => Receiver?.ButtonFlag(PS5Button.Cross) ?? false;

        [DataOutput(50)]
        [Label("TRIANGLE_BUTTON")]
        public bool Triangle() => Receiver?.ButtonFlag(PS5Button.Triangle) ?? false;

        [DataOutput(50)]
        [Label("SQUARE_BUTTON")]
        public bool Square() => Receiver?.ButtonFlag(PS5Button.Square) ?? false;

        [DataOutput(50)]
        [Label("L1_BUTTON")]
        public bool L1() => Receiver?.ButtonFlag(PS5Button.L1) ?? false;

        [DataOutput(50)]
        [Label("R1_BUTTON")]
        public bool R1() => Receiver?.ButtonFlag(PS5Button.R1) ?? false;

        [DataOutput(50)]
        [Label("L2_BUTTON")]
        public bool L2() => Receiver?.ButtonFlag(PS5Button.L2) ?? false;

        [DataOutput(50)]
        [Label("R2_BUTTON")]
        public bool R2() => Receiver?.ButtonFlag(PS5Button.R2) ?? false;

        [DataOutput(50)]
        [Label("LEFT_STICK")]
        public bool L3() => Receiver?.ButtonFlag(PS5Button.L3) ?? false;

        [DataOutput(50)]
        [Label("RIGHT_STICK")]
        public bool R3() => Receiver?.ButtonFlag(PS5Button.R3) ?? false;

        [DataOutput(50)]
        [Label("SELECT_BUTTON")]
        public bool Select() => Receiver?.ButtonFlag(PS5Button.Select) ?? false;

        [DataOutput(50)]
        [Label("START_BUTTON")]
        public bool Start() => Receiver?.ButtonFlag(PS5Button.Start) ?? false;

        [DataOutput(50)]
        [Label("HOME_BUTTON")]
        public bool Home() => Receiver?.ButtonFlag(PS5Button.Home) ?? false;

        [DataOutput(50)]
        [Label("TOUCHPAD")]
        public bool Touch() => Receiver?.ButtonFlag(PS5Button.Touch) ?? false;

        [DataOutput(150)]
        [Label("L2_PRESS")]
        public float L2Press() => Receiver == null ? 0.5f : (Receiver.LrX / (float)ushort.MaxValue - 0.5f) * 2f;

        [DataOutput(150)]
        [Label("R2_PRESS")]
        public float R2Press() => Receiver == null ? 0.5f : (Receiver.LrY / (float)ushort.MaxValue - 0.5f) * 2f;
    }
}
