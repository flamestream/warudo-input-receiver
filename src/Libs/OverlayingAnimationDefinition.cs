using Warudo.Core.Attributes;
using Warudo.Core.Data;
using static Warudo.Plugins.Core.Assets.Character.CharacterAsset;

namespace FlameStream {
    public class OverlayingAnimationDefinition : StructuredData<InputReceiverAsset>, ICollapsibleStructuredData {

        [DataInput]
        [PreviewGallery]
        [AutoCompleteResource("CharacterAnimation", null)]
        [Label("ANIMATION")]
        public string Animation;

        [DataInput]
        [Label("WEIGHT")]
        [FloatSlider(0.0f, 1.0f, 0.01f)]
        public float Weight = 1f;

        [DataInput]
        [Label("SPEED")]
        [FloatSlider(0.0f, 1.0f, 0.01f)]
        public float Speed = 1f;

        [DataInput]
        [Label("MASKED_BODY_PARTS")]
        public AnimationMaskedBodyPart[] MaskedBodyParts;

        [DataInput]
        [Label("ADDITIVE_ANIMATION")]
        public bool Additive;

        [DataInput]
        [Label("LOOPING")]
        public bool Looping;

        public string GetHeader() {

            if (string.IsNullOrEmpty(Animation)) {
                return "(Not Defined)";
            }

            var pathParts = Animation.Split('/');
            return pathParts[pathParts.Length - 1];
        }

        [DataInput]
        [Disabled]
        [Label("CUSTOM_LAYER_ID")]
        public string CustomLayerID = "🔥🎮 Base Layer";

        public bool IsSetUp {
            get {
                return !string.IsNullOrEmpty(Animation);
            }
        }
    }
}
