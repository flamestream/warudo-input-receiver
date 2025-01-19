using Warudo.Core.Attributes;
using Warudo.Plugins.Core.Assets.Utility;

namespace FlameStream {
    [AssetType(
        Id = "Flamestream.Asset.Test",
        Title = "TEST",
        Category = "FS_ASSET_CATEGORY_INTERNAL"
    )]
    public class TestAsset : ReceiverAsset {
        [DataInput]
        [Label("MOUSE_MODE")]
        public MouseHandState HandMouseMode;

        protected override void Log(string msg) {
            throw new System.NotImplementedException();
        }

        protected override void OnCreate() {
            base.OnCreate();
        }
    }
}
