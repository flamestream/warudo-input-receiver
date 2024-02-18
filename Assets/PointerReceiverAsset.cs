
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Scenes;

namespace FlameStream {
    [AssetType(
        Id = "FlameStream.Asset.PointerReceiver",
        Title = "ASSET_TITLE_POINTER"
    )]
    public class PointerReceiverAsset : ReceiverAsset {

        const ushort PROTOCOL_VERSION = 1;
        const int DEFAULT_PORT = 40610;

        public int X;
        public int Y;
        /// <summary>
        /// Pointer Source.
        ///
        /// 0 = Mouse
        /// 1 = Touch
        /// 2 = Pen
        /// </summary>
        public int Source;
        public bool Button1;
        public bool Button2;

        protected override void OnCreate() {

            if (Port == 0) Port = DEFAULT_PORT;

            base.OnCreate();
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (lastState == null) return;

            var parts = lastState.Split(';');
            byte.TryParse(parts[0], out byte protocolVersion);
            if (protocolVersion != PROTOCOL_VERSION) {
                StopReceiver();
                SetMessage($"Invalid pointer protocol '{protocolVersion}'. Expected '{PROTOCOL_VERSION}'\n\nPlease download compatible version of emitter at https://github.com/flamestream/input-device-emitter/releases");
                return;
            }

            int.TryParse(parts[1], out X);
            int.TryParse(parts[2], out Y);
            int.TryParse(parts[3], out Source);
            Button1 = parts[4] == "1";
            Button2 = parts[5] == "1";
        }

        protected override void Log(string msg) {
            Debug.Log($"[FlameStream.Asset.PointerReceiver] {msg}");
        }
    }
}
