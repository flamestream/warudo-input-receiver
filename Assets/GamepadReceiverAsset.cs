using Warudo.Core.Attributes;
using Warudo.Core.Scenes;

namespace FlameStream
{
    [AssetType(
        Id = "FlameStream.Asset.GamepadReceiver",
        Title = "ASSET_TITLE_GAMEPAD"
    )]
    public partial class GamepadReceiverAsset : ReceiverAsset {

        public const float DEAD_ZONE_RADIUS = 0.1f;
        const ushort PROTOCOL_VERSION = 2;
        const int DEFAULT_PORT = 40611;

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

        public ushort LastLX;
        public ushort LastLY;
        public ushort LastLZ;
        public ushort LastLrX;
        public ushort LastLrY;
        public ushort LastLrZ;
        public byte LastPad;

        protected override void OnCreate() {
            if (Port == 0) Port = DEFAULT_PORT;
            base.OnCreate();
        }

        public override void OnUpdate() {
            base.OnUpdate();
            PerformButtonStateUpdateLoop();
            PerformPropMovement();
        }

        void PerformButtonStateUpdateLoop() {
            if (lastState == null) return;

            var parts = lastState.Split(';');
            byte.TryParse(parts[0], out byte protocolVersion);
            if (protocolVersion != PROTOCOL_VERSION) {
                StopReceiver();
                SetMessage($"Invalid gamepad protocol '{protocolVersion}'. Expected '{PROTOCOL_VERSION}'\n\nPlease download compatible version of emitter at https://github.com/flamestream/input-device-emitter/releases");
                return;
            }

            ActivatedButtonFlags = (ButtonFlags ^ LastButtonFlags) & ButtonFlags;
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

        public bool JustDownButtonFlag(int idx) {
            return ButtonFlag(idx) && !LastButtonFlag(idx);
        }

        public bool JustUpButtonFlag(int idx) {
            return !ButtonFlag(idx) && LastButtonFlag(idx);
        }

        protected override void Log(string msg) {
            UnityEngine.Debug.Log($"[FlameStream.Asset.GamepadReceiver] {msg}");
        }

        [Section("General Configuration")]
        
        [DataInput]
        [Label("Game Controller Type")]
        public ControllerType TargetControllerType;

        [DataInput]
        [Label("Default Controller Hand")]
        public GamepadAnchorSide DefaultControllerAnchorSide;

        public enum GamepadAnchorSide {
            LeftHand,
            RightHand
        }
    }
}
