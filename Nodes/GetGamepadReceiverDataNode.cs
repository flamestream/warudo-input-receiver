using System;
using System.Linq;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GetGamepadReceiverDataNode",
    Title = "NODE_TITLE_GAMEPAD_SWITCH",
    Category = "NODE_CATEGORY"
)]
    public class GetGamepadReceiverDataNode : Node {

        public enum BUTTON_ID: int
        {
            None = 0,
            B = 1,
            A = 2,
            Y = 3,
            X = 4,
            L = 5,
            R = 6,
            ZL = 7,
            ZR = 8,
            Plus = 9,
            Minus = 10,
            LeftStick = 11,
            RightStick = 12,
            Home = 13,
            Capture = 14,
        };

        public static readonly BUTTON_ID[] LEFT_BUTTON_IDS = {
            BUTTON_ID.LeftStick,
            BUTTON_ID.Plus,
            BUTTON_ID.Capture,
        };

        public static readonly BUTTON_ID[] RIGHT_BUTTON_IDS = {
            BUTTON_ID.A,
            BUTTON_ID.B,
            BUTTON_ID.X,
            BUTTON_ID.Y,
            BUTTON_ID.RightStick,
            BUTTON_ID.Minus,
            BUTTON_ID.Home,
        };

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
        public bool A() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.A - 1);

        [DataOutput]
        [Label("NODE_B_BUTTON")]
        public bool B() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.B - 1);

        [DataOutput]
        [Label("NODE_X_BUTTON")]
        public bool X() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.X - 1);

        [DataOutput]
        [Label("NODE_Y_BUTTON")]
        public bool Y() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.Y - 1);

        [DataOutput]
        [Label("NODE_L_BUTTON")]
        public bool L() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.L - 1);

        [DataOutput]
        [Label("NODE_R_BUTTON")]
        public bool R() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.R - 1);

        [DataOutput]
        [Label("NODE_ZL_BUTTON")]
        public bool ZL() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.ZL - 1);

        [DataOutput]
        [Label("NODE_ZR_BUTTON")]
        public bool ZR() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.ZR - 1);

        [DataOutput]
        [Label("NODE_LEFT_STICK")]
        public bool LeftStick() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.LeftStick - 1);

        [DataOutput]
        [Label("NODE_RIGHT_STICK")]
        public bool RightStick() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.RightStick - 1);

        [DataOutput]
        [Label("NODE_PLUS_BUTTON")]
        public bool Plus() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.Plus - 1);

        [DataOutput]
        [Label("NODE_MINUS_BUTTON")]
        public bool Minus() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.Minus - 1);

        [DataOutput]
        [Label("NODE_HOME_BUTTON")]
        public bool Home() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.Home - 1);

        [DataOutput]
        [Label("NODE_CAPTURE_BUTTON")]
        public bool Capture() => Receiver != null && Receiver.ButtonFlag((int)BUTTON_ID.Capture - 1);

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
        [Label("NODE_ANY_FACE_BUTTON")]
        public bool AnyFaceButton() =>  A() || B() || X() || Y() || Plus() || Minus() || LeftStick() || RightStick() || Home() || Capture() || ControlPad() != 5;

        [DataOutput]
        [Label("NODE_ANY_SHOULDER_BUTTON")]
        public bool AnyShoulderButton() => L() || R() || ZL() || ZR();

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
                var buttonId = Array.Find(LEFT_BUTTON_IDS, b => Receiver.JustDownButtonFlag((int)b - 1));
                if (buttonId != BUTTON_ID.None) {
                    _LeftFaceHoverInputId = buttonId.ToString();
                }
            }

            // Right Face
            if (RightStickActive()) {
                _RightFaceHoverInputId = "RightStick";
            } else {
                var buttonId = Array.Find(RIGHT_BUTTON_IDS, b => Receiver.JustDownButtonFlag((int)b - 1));
                if (buttonId != BUTTON_ID.None) {
                    _RightFaceHoverInputId = buttonId.ToString();
                }
            }
        }
    }
}
