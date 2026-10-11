using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class KeyboardBasicSetupSavedData : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("BASE_HAND_ANIMATION")]
        [Description("BASE_HAND_ANIMATION_DESC")]
        public string BaseHandAnimation;

        [DataInput(20)]
        [Label("KEYBOARD_RIG_ORIGIN")]
        [Description("KEYBOARD_RIG_ORIGIN_DESC")]
        public CollapsibleVisualSetupTransform RootTransform;

        [DataInput(30)]
        [Label("LEFT_HAND_REST")]
        [Description("LEFT_HAND_REST_DESC")]
        public CollapsibleVisualSetupTransform LeftHandRestTransform;

        [DataInput(40)]
        [Label("RIGHT_HAND_REST")]
        [Description("RIGHT_HAND_REST_DESC")]
        public CollapsibleVisualSetupTransform RightHandRestTransform;

        [DataInput(50)]
        [Label("BASIC_SETUP_DONE")]
        [Description("BASIC_SETUP_DONE_DESC")]
        public bool IsBasicSetupDone;

        public string GetHeader() => "CLICK_TO_VIEW_SAVED_DATA".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;

            Watch(nameof(BaseHandAnimation), delegate {
                Parent?.SynchronizeAllFingerAnimations();
            });
            Watch(nameof(RootTransform), delegate {
                Parent?.SetupRootTransform();
            });
            Watch(nameof(LeftHandRestTransform), delegate {
                Parent?.SetupLeftHandRestTransform();
            });
            Watch(nameof(RightHandRestTransform), delegate {
                Parent?.SetupRightHandRestTransform();
            });
            Watch(nameof(IsBasicSetupDone), delegate {
                Parent?.RefreshSetupPorts();
            });
        }
    }
}
