using Warudo.Core.Attributes;
using Warudo.Plugins.Core.Assets.Prop;

namespace FlameStream {
    [AssetType(Id = "Flamestream.Asset.DrawingScreen", Title = "DRAWING_SCREEN", Category = "CATEGORY_INTERNAL")]
    public class DrawingScreenAsset : ScreenAsset {

        public DrawingScreenAsset() {
            ContentType = ScreenContentType.Display;
            Bend = false;
        }

        protected override void OnCreate() {
            base.OnCreate();
            GetDataInputPort(nameof(ContentType)).Properties.alwaysDisabled = true;
            GetDataInputPort(nameof(ContentType)).Properties.disabled = true;
            GetDataInputPort(nameof(DisplayName)).Properties.alwaysDisabled = true;
            GetDataInputPort(nameof(DisplayName)).Properties.disabled = true;
            GetDataInputPort(nameof(DisplayName)).Properties.description = "This property should be set in the FS Pointer Input Receiver Asset";
        }
    }
}
