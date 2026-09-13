using Warudo.Core.Attributes;
using Warudo.Core.Scenes;
using Warudo.Core.Serializations;

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

        // As long as the plugin is in v5.5 development, this asset stays at version 1 (no migration code needed)
        protected override int CURRENT_ASSET_VERSION => 1;

        protected override void Log(string msg) {
            UnityEngine.Debug.Log($"[FlameStream.Asset.KeyboardReceiver] {msg}");
        }


        protected override void OnCreate() {
            base.OnCreate();
            OnCreateBasicSetup();
            OnCreateFingerMovement();
            OnCreateVirtualKeys();
            UpdateInputEnableDescriptions();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            CancelKeyListening();
            OnDestroyBasicSetup();
            OnDestroyFingerMovement();
            OnDestroyVirtualKeys();
        }

        public override void OnUpdate() {
            base.OnUpdate();
            if (!IsSelected && ActiveKeyListeningTarget != null) {
                CancelKeyListening();
            }
            OnUpdateState();
            OnUpdateBasicSetup();
            OnUpdateFingerMovement();
            OnUpdateVirtualKeys();
        }

        public override void Deserialize(SerializedAsset serialized) {
            base.Deserialize(serialized);
            if (serialized.dataInputs != null && serialized.dataInputs.TryGetValue("HandRestSetup", out var legacyPort) && !string.IsNullOrEmpty(legacyPort?.value)) {
                try {
                    var legacySetup = Newtonsoft.Json.JsonConvert.DeserializeObject<SerializedStructuredData>(legacyPort.value);
                    if (legacySetup?.dataInputs != null) {
                        if (LeftHandRestTransform == null && legacySetup.dataInputs.TryGetValue("Left", out var leftData) && !string.IsNullOrEmpty(leftData?.value)) {
                            LeftHandRestTransform = Warudo.Core.Data.StructuredData.Create<CollapsibleVisualSetupTransform>();
                            LeftHandRestTransform.Deserialize(Newtonsoft.Json.JsonConvert.DeserializeObject<SerializedStructuredData>(leftData.value));
                        }
                        if (RightHandRestTransform == null && legacySetup.dataInputs.TryGetValue("Right", out var rightData) && !string.IsNullOrEmpty(rightData?.value)) {
                            RightHandRestTransform = Warudo.Core.Data.StructuredData.Create<CollapsibleVisualSetupTransform>();
                            RightHandRestTransform.Deserialize(Newtonsoft.Json.JsonConvert.DeserializeObject<SerializedStructuredData>(rightData.value));
                        }
                        if (FingerTransitions != null) {
                            if (legacySetup.dataInputs.TryGetValue("ReturnToRest", out var rtr) && rtr?.value != null && bool.TryParse(rtr.value, out bool b)) {
                                FingerTransitions.ReturnToRest = b;
                            }
                            if (legacySetup.dataInputs.TryGetValue("RestTimeout", out var rto) && rto?.value != null && float.TryParse(rto.value, out float f)) {
                                FingerTransitions.RestTimeout = f;
                            }
                        }
                    }
                } catch { }
            }
        }
    }
}
