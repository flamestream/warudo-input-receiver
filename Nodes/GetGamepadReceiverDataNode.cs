using System;
using System.Linq;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using static FlameStream.GamepadReceiverAsset;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GetGamepadReceiverDataNode",
    Title = "NODE_TITLE_GAMEPAD_SWITCH",
    Category = "NODE_CATEGORY"
)]
    public class GetGamepadReceiverDataNode : Node {

        [DataInput]
        public GamepadReceiverAsset Receiver;

        [DataOutput]
        [Label("NODE_IS_ACTIVE")]
        public bool IsActive() => Receiver != null && Receiver.Active;

        [DataOutput]
        [Label("NODE_IS_RECEIVING")]
        public bool IsReceiving() => Receiver != null && Receiver.IsReceiving;

        [DataOutput]
        [Label("NODE_A_BUTTON")]
        public bool A() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.A);

        [DataOutput]
        [Label("NODE_B_BUTTON")]
        public bool B() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.B);

        [DataOutput]
        [Label("NODE_X_BUTTON")]
        public bool X() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.X);

        [DataOutput]
        [Label("NODE_Y_BUTTON")]
        public bool Y() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.Y);

        [DataOutput]
        [Label("NODE_L_BUTTON")]
        public bool L() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.L);

        [DataOutput]
        [Label("NODE_R_BUTTON")]
        public bool R() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.R);

        [DataOutput]
        [Label("NODE_ZL_BUTTON")]
        public bool ZL() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.ZL);

        [DataOutput]
        [Label("NODE_ZR_BUTTON")]
        public bool ZR() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.ZR);

        [DataOutput]
        [Label("NODE_LEFT_STICK")]
        public bool LeftStick() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.LeftStick);

        [DataOutput]
        [Label("NODE_RIGHT_STICK")]
        public bool RightStick() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.RightStick);

        [DataOutput]
        [Label("NODE_PLUS_BUTTON")]
        public bool Plus() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.Plus);

        [DataOutput]
        [Label("NODE_MINUS_BUTTON")]
        public bool Minus() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.Minus);

        [DataOutput]
        [Label("NODE_HOME_BUTTON")]
        public bool Home() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.Home);

        [DataOutput]
        [Label("NODE_CAPTURE_BUTTON")]
        public bool Capture() => Receiver != null && Receiver.ButtonFlag(SwitchProButton.Capture);

        [DataOutput]
        [Label("NODE_LEFT_STICK_X")]
        public float LeftStickX() => Receiver == null ? 0.5f : (Receiver.LX / (float)ushort.MaxValue - 0.5f) * 2f;

        [DataOutput]
        [Label("NODE_LEFT_STICK_Y")]
        public float LeftStickY() => Receiver == null ? 0.5f : (Receiver.LY / (float)ushort.MaxValue - 0.5f) * 2f;

        [DataOutput]
        [Label("NODE_RIGHT_STICK_X")]
        public float RightStickX() => Receiver == null ? 0.5f : (Receiver.LrX / (float)ushort.MaxValue - 0.5f) * 2f;

        [DataOutput]
        [Label("NODE_RIGHT_STICK_Y")]
        public float RightStickY() => Receiver == null ? 0.5f : (Receiver.LrY / (float)ushort.MaxValue - 0.5f) * 2f;

        [DataOutput]
        [Label("NODE_CONTROL_PAD")]
        public int ControlPad() => Receiver == null ? 5 : Receiver.Pad;

        [DataOutput]
        [Label("NODE_HOVER_LEFT_FACE")]
        public string LeftFaceHoverInputId() => _LeftFaceHoverInputId;
        string _LeftFaceHoverInputId;

        [DataOutput]
        [Label("NODE_HOVER_RIGHT_FACE")]
        public string RightFaceHoverInputId() => _RightFaceHoverInputId;
        string _RightFaceHoverInputId;

        public bool LeftStickActive() {
            return Math.Abs(LeftStickX()) >= GamepadReceiverAsset.DEAD_ZONE_RADIUS
                || Math.Abs(LeftStickY()) >= GamepadReceiverAsset.DEAD_ZONE_RADIUS;
        }

        public bool RightStickActive() {
            return Math.Abs(RightStickX()) >= GamepadReceiverAsset.DEAD_ZONE_RADIUS
                || Math.Abs(RightStickY()) >= GamepadReceiverAsset.DEAD_ZONE_RADIUS;
        }

        public override void OnUpdate() {

            if (Receiver == null) return;

            // Left Face
            if (LeftStickActive()) {
                _LeftFaceHoverInputId = "LeftStick";
            } else if (ControlPad() != 5) {
                _LeftFaceHoverInputId = $"D{ControlPad()}";
            } else {
                var buttonId = Array.Find(LeftFaceButtonIdsSwitch, b => Receiver.ActivatedButtonFlag(b));
                if (buttonId != SwitchProButton.None) {
                    _LeftFaceHoverInputId = buttonId.ToString();
                }
            }

            // Right Face
            if (RightStickActive()) {
                _RightFaceHoverInputId = "RightStick";
            } else {
                var buttonId = Array.Find(RightFaceButtonIdsSwitch, b => Receiver.ActivatedButtonFlag(b));
                if (buttonId != SwitchProButton.None) {
                    _RightFaceHoverInputId = buttonId.ToString();
                }
            }
        }
    }
}
