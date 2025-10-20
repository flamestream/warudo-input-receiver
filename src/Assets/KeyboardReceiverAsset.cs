using Warudo.Core.Attributes;
using Warudo.Core.Scenes;

namespace FlameStream
{
    [AssetType(
        Id = "FlameStream.Asset.KeyboardReceiver",
        Title = "FS_ASSET_TITLE_KEYBOARD",
        Category = "FS_ASSET_CATEGORY_INPUT"
    )]
    public partial class KeyboardReceiverAsset : ReceiverAsset {

        protected override string CHARACTER_ANIM_LAYER_ID_PREFIX {
            get {
                return "🔥⌨️";
            }
        }

        protected override ushort PROTOCOL_VERSION {
            get {
                return 1;
            }
        }

        protected override string PROTOCOL_ID {
            get {
                return "K";
            }
        }

        protected override int DEFAULT_PORT {
            get {
                return 40612;
            }
        }

        protected override void Log(string msg) {
            UnityEngine.Debug.Log($"[FlameStream.Asset.KeyboardReceiver] {msg}");
        }

        bool IsWaitingForButtonPress = false;

        [Trigger]
        [Label("IDENTIFY_BUTTON")]
        public void TriggerIdentifyButton() {
            IsWaitingForButtonPress = true;
            GetTriggerPort(nameof(TriggerIdentifyButton)).Properties.hidden = IsWaitingForButtonPress;
            GetTriggerPort(nameof(TriggerCancelIdentifyButton)).Properties.hidden = !IsWaitingForButtonPress;
            BroadcastTriggerProperties(nameof(TriggerIdentifyButton));
            BroadcastTriggerProperties(nameof(TriggerCancelIdentifyButton));
        }

        [Trigger]
        [Label("IDENTIFYING_BUTTON")]
        [Hidden]
        public void TriggerCancelIdentifyButton() {
            IsWaitingForButtonPress = false;
            GetTriggerPort(nameof(TriggerIdentifyButton)).Properties.hidden = IsWaitingForButtonPress;
            GetTriggerPort(nameof(TriggerCancelIdentifyButton)).Properties.hidden = !IsWaitingForButtonPress;
            BroadcastTriggerProperties(nameof(TriggerIdentifyButton));
            BroadcastTriggerProperties(nameof(TriggerCancelIdentifyButton));
        }

        /// <summary>
        /// BASIC SETUP
        /// </summary>
        [Section("BASIC_SETUP")]

        [Markdown]
        public string ComingSoonInstructions = @"### Key Press Animator Framework Coming Soon
In the meantime, the nodes should provide enough basic functionality to do anything";
    }
}
