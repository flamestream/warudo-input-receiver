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
