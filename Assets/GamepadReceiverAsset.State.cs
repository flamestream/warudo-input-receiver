using System;
using System.Linq;

namespace FlameStream
{
    public partial class GamepadReceiverAsset : ReceiverAsset {

        public const float DEAD_ZONE_RADIUS = 0.1f;
        const ushort PROTOCOL_VERSION = 2;
        const int DEFAULT_PORT = 40611;

        public enum DPadDirection : int {
            None = 0,
            DownLeft = 1,
            Down = 2,
            DownRight = 3,
            Left = 4,
            Neutral = 5,
            Right = 6,
            UpLeft = 7,
            Up = 8,
            UpRight = 9,
        };

        public uint ButtonFlags;
        public ushort LX;
        public ushort LY;
        public ushort LZ;
        public ushort LrX;
        public ushort LrY;
        public ushort LrZ;
        public byte DPad;

        public uint LastButtonFlags;
        public uint ActivatedButtonFlags;
        public uint DeactivatedButtonFlags;

        public ushort LastLX;
        public ushort LastLY;
        public ushort LastLZ;
        public ushort LastLrX;
        public ushort LastLrY;
        public ushort LastLrZ;
        public byte LastDPad;

        public string LeftFaceHoverInputId = "LeftStick";

        public string RightFaceHoverInputId = "RightStick";

        void PerformStateUpdateLoop() {
            if (lastState == null) return;

            var parts = lastState.Split(';');
            byte.TryParse(parts[0], out byte protocolVersion);
            if (protocolVersion != PROTOCOL_VERSION) {
                StopReceiver();
                SetMessage($"Invalid gamepad protocol '{protocolVersion}'. Expected '{PROTOCOL_VERSION}'\n\nPlease download compatible version of emitter at https://github.com/flamestream/input-device-emitter/releases");
                return;
            }

            ActivatedButtonFlags = (ButtonFlags ^ LastButtonFlags) & ButtonFlags;
            DeactivatedButtonFlags = (ButtonFlags ^ LastButtonFlags) & ~ButtonFlags;
            LastButtonFlags = ButtonFlags;

            LastLX = LX;
            LastLY = LY;
            LastLZ = LZ;
            LastLrX = LrX;
            LastLrY = LrY;
            LastLrZ = LrZ;
            LastDPad = DPad;

            uint.TryParse(parts[1], out ButtonFlags);
            ushort.TryParse(parts[2], out LX);
            ushort.TryParse(parts[3], out LY);
            ushort.TryParse(parts[4], out LZ);
            ushort.TryParse(parts[5], out LrX);
            ushort.TryParse(parts[6], out LrY);
            ushort.TryParse(parts[7], out LrZ);
            byte.TryParse(parts[8], out DPad);

            // Left Face
            if (IsLeftStickActive()) {
                LeftFaceHoverInputId = "LeftStick";
            } else if (DPad != 5) {
                LeftFaceHoverInputId = DPadLabelName(DPad);
            } else {
                var buttonId = Array.Find(LeftFaceButtonIdsSwitch, b => ActivatedButtonFlag(b));
                if (buttonId != 0) {
                    LeftFaceHoverInputId = buttonId.ToString();
                }
            }

            // Right Face
            if (IsRightStickActive()) {
                RightFaceHoverInputId = "RightStick";
            } else {
                var buttonId = Array.Find(RightFaceButtonIdsSwitch, b => ActivatedButtonFlag(b));
                if (buttonId != 0) {
                    RightFaceHoverInputId = buttonId.ToString();
                }
            }
        }

        public string DPadLabelName(byte v) {
            return $"D{v}";
        }

        public string LeftFaceButtonId {
            get {
                switch(TargetGamepadType) {
                    case GamepadType.SwitchProController:
                        if (!IsLeftStickActive()) {
                            if (DPad != 5) {
                                return DPadLabelName(DPad);
                            } else {
                                var buttonId = Array.Find(LeftFaceButtonIdsSwitch, b => ActivatedButtonFlag(b));
                                if (buttonId != 0) {
                                    return buttonId.ToString();
                                }
                            }
                        }
                        break;
                    case GamepadType.PS5Controller:
                        if (!IsLeftStickActive()) {
                            if (DPad != 5) {
                                return DPadLabelName(DPad);
                            } else {
                                var buttonId = Array.Find(LeftFaceButtonIdsPS5, b => ActivatedButtonFlag(b));
                                if (buttonId != 0) {
                                    return buttonId.ToString();
                                }
                            }
                        }
                        break;
                    case GamepadType.Xbox360Controller:
                        if (!IsLeftStickActive()) {
                            if (DPad != 5) {
                                return DPadLabelName(DPad);
                            } else {
                                var buttonId = Array.Find(LeftFaceButtonIdsXbox360, b => ActivatedButtonFlag(b));
                                if (buttonId != 0) {
                                    return buttonId.ToString();
                                }
                            }
                        }
                        break;

                }
                return "LeftStick";
            }
        }

        public string RightFaceButtonId {
            get {
                switch(TargetGamepadType) {
                    case GamepadType.SwitchProController:
                        if (!IsRightStickActive()) {
                            var buttonId = Array.Find(RightFaceButtonIdsSwitch, b => ActivatedButtonFlag(b));
                            if (buttonId != 0) {
                                return buttonId.ToString();
                            }
                        }
                        break;
                    case GamepadType.PS5Controller:
                        if (!IsRightStickActive()) {
                            var buttonId = Array.Find(RightFaceButtonIdsPS5, b => ActivatedButtonFlag(b));
                            if (buttonId != 0) {
                                return buttonId.ToString();
                            }
                        }
                        break;
                    case GamepadType.Xbox360Controller:
                        if (!IsRightStickActive()) {
                            var buttonId = Array.Find(RightFaceButtonIdsXbox360, b => ActivatedButtonFlag(b));
                            if (buttonId != 0) {
                                return buttonId.ToString();
                            }
                        }
                        break;
                }
                return "RightStick";
            }
        }

        public bool ButtonFlag(int idx) {
            return (ButtonFlags >> idx & 1) == 1;
        }
        public bool ButtonFlag(SwitchProButton val) {
            return ButtonFlag((int) val - 1);
        }
        public bool ButtonFlag(PS5Button val) {
            return ButtonFlag((int) val - 1);
        }
        public bool ButtonFlag(Xbox360Button val) {
            switch(val) {
                case Xbox360Button.LT:
                    return LZ > 32767;
                case Xbox360Button.RT:
                    return LZ < 32767;
            }
            return ButtonFlag((int) val - 1);
        }

        public bool LastButtonFlag(int idx) {
            return (LastButtonFlags >> idx & 1) == 1;
        }
        public bool LastButtonFlag(Xbox360Button val) {
            switch(val) {
                case Xbox360Button.LT:
                    return LastLZ > 32767;
                case Xbox360Button.RT:
                    return LastLZ < 32767;
            }
            return LastButtonFlag((int) val - 1);
        }

        public bool ActivatedButtonFlag(int idx) {
            return (ActivatedButtonFlags >> idx & 1) == 1;
        }
        public bool ActivatedButtonFlag(SwitchProButton val) {
            return ActivatedButtonFlag((int) val - 1);
        }
        public bool ActivatedButtonFlag(PS5Button val) {
            return ActivatedButtonFlag((int) val - 1);
        }
        public bool ActivatedButtonFlag(Xbox360Button val) {
            switch(val) {
                case Xbox360Button.LT:
                case Xbox360Button.RT:
                    return ButtonFlag(val) && !LastButtonFlag(val);
            }
            return ActivatedButtonFlag((int) val - 1);
        }

        public bool DeactivatedButtonFlag(int idx) {
            return (DeactivatedButtonFlags >> idx & 1) == 1;
        }
        public bool DeactivatedButtonFlag(SwitchProButton val) {
            return DeactivatedButtonFlag((int) val - 1);
        }
        public bool DeactivatedButtonFlag(PS5Button val) {
            return DeactivatedButtonFlag((int) val - 1);
        }
        public bool DeactivatedButtonFlag(Xbox360Button val) {
            switch(val) {
                case Xbox360Button.LT:
                case Xbox360Button.RT:
                    return !ButtonFlag(val) && LastButtonFlag(val);
            }
            return DeactivatedButtonFlag((int) val - 1);
        }

        public float LeftStickX {
            get {
                switch(TargetGamepadType) {
                    case GamepadType.SwitchProController:
                    case GamepadType.PS5Controller:
                    case GamepadType.Xbox360Controller:
                        return (LX / (float)ushort.MaxValue - 0.5f) * 2f;

                }
                return 0;
            }
        }

        public float LeftStickY {
            get {
                switch(TargetGamepadType) {
                    case GamepadType.SwitchProController:
                    case GamepadType.PS5Controller:
                    case GamepadType.Xbox360Controller:
                        return (LY / (float)ushort.MaxValue - 0.5f) * -2f;
                }
                return 0;
            }
        }

        public float RightStickX {
            get {
                switch(TargetGamepadType) {
                    case GamepadType.SwitchProController:
                    case GamepadType.Xbox360Controller:
                        return (LrX / (float)ushort.MaxValue - 0.5f) * 2f;
                    case GamepadType.PS5Controller:
                        return (LZ / (float)ushort.MaxValue - 0.5f) * 2f;
                }
                return 0;
            }
        }

        public float RightStickY {
            get {
                switch(TargetGamepadType) {
                    case GamepadType.SwitchProController:
                    case GamepadType.Xbox360Controller:
                        return (LrY / (float)ushort.MaxValue - 0.5f) * -2f;
                    case GamepadType.PS5Controller:
                        return (LrZ / (float)ushort.MaxValue - 0.5f) * -2f;
                }
                return 0;
            }
        }

        public bool IsLeftStickActive() {
            return Math.Abs(LeftStickX) >= DEAD_ZONE_RADIUS
                || Math.Abs(LeftStickY) >= DEAD_ZONE_RADIUS;
        }

        public bool IsRightStickActive() {
            return Math.Abs(RightStickX) >= DEAD_ZONE_RADIUS
                || Math.Abs(RightStickY) >= DEAD_ZONE_RADIUS;
        }

        public bool IsAnyFaceButtonActivated {
            get {
                switch(TargetGamepadType) {
                    case GamepadType.SwitchProController:
                        return Array.Find(FaceButtonIdsSwitch, v => ActivatedButtonFlag(v)) != 0;
                    case GamepadType.PS5Controller:
                        return Array.Find(FaceButtonIdsPS5, v => ActivatedButtonFlag(v)) != 0;
                    case GamepadType.Xbox360Controller:
                        return Array.Find(FaceButtonIdsXbox360, v => ActivatedButtonFlag(v)) != 0;

                }
                return false;
            }
        }

        public bool IsAnyFaceButtonDown {
            get {
                switch(TargetGamepadType) {
                    case GamepadType.SwitchProController:
                        return Array.Find(FaceButtonIdsSwitch, v => ButtonFlag(v)) != 0;
                    case GamepadType.PS5Controller:
                        return Array.Find(FaceButtonIdsPS5, v => ButtonFlag(v)) != 0;
                    case GamepadType.Xbox360Controller:
                        return Array.Find(FaceButtonIdsXbox360, v => ButtonFlag(v)) != 0;
                }
                return false;
            }
        }

        public bool IsAnyShoulderButtonActivated {
            get {
                switch(TargetGamepadType) {
                    case GamepadType.SwitchProController:
                        return Array.Find(ShoulderButtonIdsSwitch, v => ActivatedButtonFlag(v)) != 0;
                    case GamepadType.PS5Controller:
                        return Array.Find(ShoulderButtonIdsPS5, v => ActivatedButtonFlag(v)) != 0;
                    case GamepadType.Xbox360Controller:
                        return Array.Find(ShoulderButtonIdsXbox360, v => ActivatedButtonFlag(v)) != 0;
                }
                return false;
            }
        }

        public bool IsAnyShoulderButtonDown {
            get {
                switch(TargetGamepadType) {
                    case GamepadType.SwitchProController:
                        return Array.Find(ShoulderButtonIdsSwitch, v => ButtonFlag(v)) != 0;
                    case GamepadType.PS5Controller:
                        return Array.Find(ShoulderButtonIdsPS5, v => ButtonFlag(v)) != 0;
                    case GamepadType.Xbox360Controller:
                        return Array.Find(ShoulderButtonIdsXbox360, v => ButtonFlag(v)) != 0;
                }
                return false;
            }
        }

        protected override void Log(string msg) {
            UnityEngine.Debug.Log($"[FlameStream.Asset.GamepadReceiver] {msg}");
        }

        // NOTE: Buttons indices are offset by one to allow dummy default at 0./
        public enum SwitchProButton : int {
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

        public static readonly SwitchProButton[] LeftFaceButtonIdsSwitch = {
            SwitchProButton.LeftStick,
            SwitchProButton.Plus,
            SwitchProButton.Capture,
        };

        public static readonly SwitchProButton[] RightFaceButtonIdsSwitch = {
            SwitchProButton.A,
            SwitchProButton.B,
            SwitchProButton.X,
            SwitchProButton.Y,
            SwitchProButton.RightStick,
            SwitchProButton.Minus,
            SwitchProButton.Home,
        };

        public static readonly SwitchProButton[] FaceButtonIdsSwitch = LeftFaceButtonIdsSwitch.Union(RightFaceButtonIdsSwitch).ToArray();

        public static readonly SwitchProButton[] LeftShoulderButtonIdsSwitch = {
            SwitchProButton.L,
            SwitchProButton.ZL,
        };

        public static readonly SwitchProButton[] RightShoulderButtonIdsSwitch = {
            SwitchProButton.R,
            SwitchProButton.ZR,
        };

        public static readonly SwitchProButton[] ShoulderButtonIdsSwitch = LeftShoulderButtonIdsSwitch.Union(RightShoulderButtonIdsSwitch).ToArray();

        public enum PS5Button : int {
            None = 0,
            Square = 1,
            Cross = 2,
            Circle = 3,
            Triangle = 4,
            L1 = 5,
            R1 = 6,
            L2 = 7,
            R2 = 8,
            Select = 9,
            Start = 10,
            L3 = 11,
            R3 = 12,
            Home = 13,
            Touch = 14,
        };

        public static readonly PS5Button[] LeftFaceButtonIdsPS5 = {
            PS5Button.L3,
            PS5Button.Start,
        };

        public static readonly PS5Button[] RightFaceButtonIdsPS5 = {
            PS5Button.Square,
            PS5Button.Cross,
            PS5Button.Circle,
            PS5Button.Triangle,
            PS5Button.R3,
            PS5Button.Select,
            PS5Button.Home,
        };

        public static readonly PS5Button[] FaceButtonIdsPS5 = LeftFaceButtonIdsPS5.Union(RightFaceButtonIdsPS5).ToArray();

        public static readonly PS5Button[] LeftShoulderButtonIdsPS5 = {
            PS5Button.L1,
            PS5Button.L2,
        };

        public static readonly PS5Button[] RightShoulderButtonIdsPS5 = {
            PS5Button.R1,
            PS5Button.R2,
        };

        public static readonly PS5Button[] ShoulderButtonIdsPS5 = LeftShoulderButtonIdsPS5.Union(RightShoulderButtonIdsPS5).ToArray();

        public enum Xbox360Button : int {
            None = 0,
            A = 1,
            B = 2,
            X = 3,
            Y = 4,
            LB = 5,
            RB = 6,
            LT = 100,
            RT = 101,
            Back = 7,
            Start = 8,
            LeftStick = 9,
            RightStick = 10,
        };

        public static readonly Xbox360Button[] LeftFaceButtonIdsXbox360 = {
            Xbox360Button.LeftStick,
            Xbox360Button.Back,
        };

        public static readonly Xbox360Button[] RightFaceButtonIdsXbox360 = {
            Xbox360Button.A,
            Xbox360Button.B,
            Xbox360Button.X,
            Xbox360Button.Y,
            Xbox360Button.RightStick,
            Xbox360Button.Start,
        };

        public static readonly Xbox360Button[] FaceButtonIdsXbox360 = LeftFaceButtonIdsXbox360.Union(RightFaceButtonIdsXbox360).ToArray();

        public static readonly Xbox360Button[] LeftShoulderButtonIdsXbox360 = {
            Xbox360Button.LB,
            Xbox360Button.LT,
        };

        public static readonly Xbox360Button[] RightShoulderButtonIdsXbox360 = {
            Xbox360Button.RB,
            Xbox360Button.RT,
        };

        public static readonly Xbox360Button[] ShoulderButtonIdsXbox360 = LeftShoulderButtonIdsXbox360.Union(RightShoulderButtonIdsXbox360).ToArray();
    }
}
