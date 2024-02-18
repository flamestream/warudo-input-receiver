using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GetPs5GamepadReceiverDataNode",
    Title = "NODE_TITLE_GAMEPAD_PS5",
    Category = "NODE_CATEGORY"
)]
    public class GetPs5GamepadReceiverDataNode : Node {

        [DataInput]
        public GamepadReceiverAsset Receiver;

        [DataOutput]
        [Label("NODE_IS_ACTIVE")]
        public bool IsActive() => Receiver != null && Receiver.Active;

        [DataOutput]
        [Label("NODE_IS_RECEIVING")]
        public bool IsReceiving() => Receiver != null && Receiver.IsReceiving;

        [DataOutput]
        [Label("NODE_CIRCLE_BUTTON")]
        public bool Circle() => Receiver != null && (Receiver.ButtonFlags >> 2 & 1) == 1;

        [DataOutput]
        [Label("NODE_CROSS_BUTTON")]
        public bool Cross() => Receiver != null && (Receiver.ButtonFlags >> 1 & 1) == 1;

        [DataOutput]
        [Label("NODE_TRIANGLE_BUTTON")]
        public bool Triangle() => Receiver != null && (Receiver.ButtonFlags >> 3 & 1) == 1;

        [DataOutput]
        [Label("NODE_SQUARE_BUTTON")]
        public bool Square() => Receiver != null && (Receiver.ButtonFlags >> 0 & 1) == 1;

        [DataOutput]
        [Label("NODE_L1_BUTTON")]
        public bool L1() => Receiver != null && (Receiver.ButtonFlags >> 4 & 1) == 1;

        [DataOutput]
        [Label("NODE_R1_BUTTON")]
        public bool R1() => Receiver != null && (Receiver.ButtonFlags >> 5 & 1) == 1;

        [DataOutput]
        [Label("NODE_L2_BUTTON")]
        public bool L2() => Receiver != null && (Receiver.ButtonFlags >> 6 & 1) == 1;

        [DataOutput]
        [Label("NODE_R2_BUTTON")]
        public bool R2() => Receiver != null && (Receiver.ButtonFlags >> 7 & 1) == 1;

        [DataOutput]
        [Label("NODE_LEFT_STICK")]
        public bool LeftStick() => Receiver != null && (Receiver.ButtonFlags >> 10 & 1) == 1;

        [DataOutput]
        [Label("NODE_RIGHT_STICK")]
        public bool RightStick() => Receiver != null && (Receiver.ButtonFlags >> 11 & 1) == 1;

        [DataOutput]
        [Label("NODE_SELECT_BUTTON")]
        public bool Select() => Receiver != null && (Receiver.ButtonFlags >> 8 & 1) == 1;

        [DataOutput]
        [Label("NODE_START_BUTTON")]
        public bool Start() => Receiver != null && (Receiver.ButtonFlags >> 9 & 1) == 1;

        [DataOutput]
        [Label("NODE_HOME_BUTTON")]
        public bool Home() => Receiver != null && (Receiver.ButtonFlags >> 12 & 1) == 1;

        [DataOutput]
        [Label("NODE_TOUCHPAD")]
        public bool Touch() => Receiver != null && (Receiver.ButtonFlags >> 13 & 1) == 1;

        [DataOutput]
        [Label("NODE_LEFT_STICK_X")]
        public float LeftStickX() => Receiver == null ? 0.5f : (Receiver.LX / (float)ushort.MaxValue - 0.5f) * 2f;

        [DataOutput]
        [Label("NODE_LEFT_STICK_Y")]
        public float LeftStickY() => Receiver == null ? 0.5f : (Receiver.LY / (float)ushort.MaxValue - 0.5f) * 2f;

        [DataOutput]
        [Label("NODE_RIGHT_STICK_X")]
        public float RightStickX() => Receiver == null ? 0.5f : (Receiver.LZ / (float)ushort.MaxValue - 0.5f) * 2f;

        [DataOutput]
        [Label("NODE_RIGHT_STICK_Y")]
        public float RightStickY() => Receiver == null ? 0.5f : (Receiver.LrZ / (float)ushort.MaxValue - 0.5f) * 2f;

        [DataOutput]
        [Label("NODE_CONTROL_PAD")]
        public int ControlPad() => Receiver == null ? 5 : Receiver.Pad;

        [DataOutput]
        [Label("NODE_L2_PRESS")]
        public float L2Press() => Receiver == null ? 0.5f : (Receiver.LrX / (float)ushort.MaxValue - 0.5f) * 2f;

        [DataOutput]
        [Label("NODE_R2_PRESS")]
        public float R2Press() => Receiver == null ? 0.5f : (Receiver.LrY / (float)ushort.MaxValue - 0.5f) * 2f;

        [DataOutput]
        [Label("NODE_FACE_BUTTON")]
        public bool AnyFaceButton() =>  Circle() || Cross() || Triangle() || Square() || Start() || Select() || LeftStick() || RightStick() || Home() || Touch() || ControlPad() != 5;

        [DataOutput]
        [Label("NODE_SHOULDER_BUTTON")]
        public bool AnyShoulderButton() => L1() || R1() || L2() || R2();
    }
}
