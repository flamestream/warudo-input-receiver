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

        protected override int CURRENT_ASSET_VERSION => 2;

        protected override void OnPreMigrateAsset(int fromVersion, int toVersion, Warudo.Core.Serializations.SerializedAsset rawSerialized) {
            base.OnPreMigrateAsset(fromVersion, toVersion, rawSerialized);
            if (fromVersion < 1) {
                // Cleanly strip unreleased experimental ports from early development scenes
                MigrateRemovePort(rawSerialized, "MovementFactor");
                MigrateRemovePort(rawSerialized, "DefaultCornerRadius");
                MigrateRemovePort(rawSerialized, "DefaultBorderWidth");
            }
            if (fromVersion < 2) {
                MigrateRemovePort(rawSerialized, "LeftKeysetTransform");
                MigrateRemovePort(rawSerialized, "RightKeysetTransform");
                MigrateRemovePort(rawSerialized, "RootKeysetTransform");
                MigrateRemovePort(rawSerialized, "VirtualKeys");
                MigrateRemovePort(rawSerialized, "KeysLayoutPreset");
            }
        }

        protected override void OnPostMigrateAsset(int fromVersion, int toVersion) {
            base.OnPostMigrateAsset(fromVersion, toVersion);
            if (fromVersion < 1) {
                // Auto-heal any 0 scales from early prototype scenes
                if (LeftHandRestTransform != null && LeftHandRestTransform.Scale == UnityEngine.Vector3.zero) {
                    LeftHandRestTransform.SetDataInput(nameof(LeftHandRestTransform.Scale), UnityEngine.Vector3.one, true);
                }
                if (RightHandRestTransform != null && RightHandRestTransform.Scale == UnityEngine.Vector3.zero) {
                    RightHandRestTransform.SetDataInput(nameof(RightHandRestTransform.Scale), UnityEngine.Vector3.one, true);
                }
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

        protected override void OnCreate() {
            base.OnCreate();
            OnCreateBasicSetup();
            OnCreateFingerMovement();
            OnCreateVirtualKeys();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            OnDestroyBasicSetup();
            OnDestroyFingerMovement();
            OnDestroyVirtualKeys();
        }

        public override void OnUpdate() {
            base.OnUpdate();
            OnUpdateState();
            OnUpdateBasicSetup();
            OnUpdateFingerMovement();
            OnUpdateVirtualKeys();
        }
    }
}
