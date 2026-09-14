using UnityEngine;
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

            if (SavedData == null) {
                SavedData = Warudo.Core.Data.StructuredData.Create<KeyboardBasicSetupSavedData>();
                SavedData.Parent = this;
                SavedData.CollapsedSelf = true;
            }

            if (serialized.dataInputs != null) {
                if (serialized.dataInputs.TryGetValue("SavedData", out var sdPort) && !string.IsNullOrEmpty(sdPort?.value)) {
                    try {
                        var sd = Newtonsoft.Json.JsonConvert.DeserializeObject<SerializedStructuredData>(sdPort.value);
                        SavedData.Deserialize(sd);
                    } catch { }
                } else if (serialized.dataInputs.TryGetValue("Advanced", out var advPort) && !string.IsNullOrEmpty(advPort?.value)) {
                    try {
                        var sd = Newtonsoft.Json.JsonConvert.DeserializeObject<SerializedStructuredData>(advPort.value);
                        SavedData.Deserialize(sd);
                    } catch { }
                }

                if (serialized.dataInputs.TryGetValue("RootTransform", out var rtPort) && !string.IsNullOrEmpty(rtPort?.value)) {
                    try {
                        var sd = Newtonsoft.Json.JsonConvert.DeserializeObject<SerializedStructuredData>(rtPort.value);
                        if (SavedData.RootTransform == null) SavedData.RootTransform = Warudo.Core.Data.StructuredData.Create<CollapsibleVisualSetupTransform>();
                        SavedData.RootTransform.Deserialize(sd);
                    } catch { }
                }
                if (serialized.dataInputs.TryGetValue("LeftHandRestTransform", out var ltPort) && !string.IsNullOrEmpty(ltPort?.value)) {
                    try {
                        var sd = Newtonsoft.Json.JsonConvert.DeserializeObject<SerializedStructuredData>(ltPort.value);
                        if (SavedData.LeftHandRestTransform == null) SavedData.LeftHandRestTransform = Warudo.Core.Data.StructuredData.Create<CollapsibleVisualSetupTransform>();
                        SavedData.LeftHandRestTransform.Deserialize(sd);
                    } catch { }
                }
                if (serialized.dataInputs.TryGetValue("RightHandRestTransform", out var rtPort2) && !string.IsNullOrEmpty(rtPort2?.value)) {
                    try {
                        var sd = Newtonsoft.Json.JsonConvert.DeserializeObject<SerializedStructuredData>(rtPort2.value);
                        if (SavedData.RightHandRestTransform == null) SavedData.RightHandRestTransform = Warudo.Core.Data.StructuredData.Create<CollapsibleVisualSetupTransform>();
                        SavedData.RightHandRestTransform.Deserialize(sd);
                    } catch { }
                }
                if (serialized.dataInputs.TryGetValue("BaseHandAnimation", out var baPort) && !string.IsNullOrEmpty(baPort?.value)) {
                    SavedData.BaseHandAnimation = baPort.value;
                    SavedData.SetDataInput(nameof(SavedData.BaseHandAnimation), baPort.value, true);
                }
                if (serialized.dataInputs.TryGetValue("IsBasicSetupComplete", out var compPort) && !string.IsNullOrEmpty(compPort?.value)) {
                    if (bool.TryParse(compPort.value, out bool b) && b) {
                        SavedData.IsBasicSetupDone = true;
                        SavedData.SetDataInput(nameof(SavedData.IsBasicSetupDone), true, true);
                    }
                }
            }

            if (SavedData != null) {
                if (SavedData.RootTransform != null) {
                    SavedData.SetDataInput(nameof(SavedData.RootTransform), SavedData.RootTransform, true);
                }
                if (SavedData.LeftHandRestTransform != null) {
                    SavedData.SetDataInput(nameof(SavedData.LeftHandRestTransform), SavedData.LeftHandRestTransform, true);
                }
                if (SavedData.RightHandRestTransform != null) {
                    SavedData.SetDataInput(nameof(SavedData.RightHandRestTransform), SavedData.RightHandRestTransform, true);
                }
                SavedData.SetDataInput(nameof(SavedData.IsBasicSetupDone), SavedData.IsBasicSetupDone, true);
                SetDataInput(nameof(SavedData), SavedData, true);
            }

            if (!IsBasicSetupComplete && Character != null && (RootTransform != null || LeftHandRestTransform != null)) {
                if ((RootTransform != null && RootTransform.Position != Vector3.zero) || 
                    (LeftHandRestTransform != null && LeftHandRestTransform.Position != Vector3.zero)) {
                    IsBasicSetupComplete = true;
                    if (string.IsNullOrEmpty(BaseHandAnimation) && !string.IsNullOrEmpty(Character.DefaultIdleAnimation)) {
                        BaseHandAnimation = Character.DefaultIdleAnimation;
                    }
                }
            }
        }
    }
}
