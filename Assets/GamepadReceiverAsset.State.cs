using System.Linq;
using Warudo.Core.Attributes;
using Warudo.Core.Scenes;

namespace FlameStream
{
    public partial class GamepadReceiverAsset : ReceiverAsset {

        public const float DEAD_ZONE_RADIUS = 0.1f;
        const ushort PROTOCOL_VERSION = 2;
        const int DEFAULT_PORT = 40611;

        public enum ControlPadDirection : int {
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

        public uint ButtonFlags;
        public ushort LX;
        public ushort LY;
        public ushort LZ;
        public ushort LrX;
        public ushort LrY;
        public ushort LrZ;
        public byte Pad;

        public uint LastButtonFlags;
        public uint ActivatedButtonFlags;
        public uint DeactivatedButtonFlags;

        public ushort LastLX;
        public ushort LastLY;
        public ushort LastLZ;
        public ushort LastLrX;
        public ushort LastLrY;
        public ushort LastLrZ;
        public byte LastPad;

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
            LastPad = Pad;

            uint.TryParse(parts[1], out ButtonFlags);
            ushort.TryParse(parts[2], out LX);
            ushort.TryParse(parts[3], out LY);
            ushort.TryParse(parts[4], out LZ);
            ushort.TryParse(parts[5], out LrX);
            ushort.TryParse(parts[6], out LrY);
            ushort.TryParse(parts[7], out LrZ);
            byte.TryParse(parts[8], out Pad);
        }

        public bool ButtonFlag(int idx) {
            return (ButtonFlags >> idx & 1) == 1;
        }

        public bool ButtonFlag(SwitchProButton val) {
            return ButtonFlag((int) val - 1);
        }

        public bool LastButtonFlag(int idx) {
            return (LastButtonFlags >> idx & 1) == 1;
        }

        public bool ActivatedButtonFlag(int idx) {
            return (ActivatedButtonFlags >> idx & 1) == 1;
        }
        public bool ActivatedButtonFlag(SwitchProButton val) {
            return ActivatedButtonFlag((int) val - 1);
        }

        public bool DeactivatedButtonFlag(int idx) {
            return (DeactivatedButtonFlags >> idx & 1) == 1;
        }
        public bool DeactivatedButtonFlag(SwitchProButton val) {
            return DeactivatedButtonFlag((int) val - 1);
        }

        protected override void Log(string msg) {
            UnityEngine.Debug.Log($"[FlameStream.Asset.GamepadReceiver] {msg}");
        }
    }
}
