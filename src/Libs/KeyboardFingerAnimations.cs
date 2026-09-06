using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class KeyboardFingerAnimations : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("LEFT_PINKY_ANIMATION")]
        public string LeftPinky;

        [DataInput(20)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("LEFT_RING_ANIMATION")]
        public string LeftRing;

        [DataInput(30)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("LEFT_MIDDLE_ANIMATION")]
        public string LeftMiddle;

        [DataInput(40)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("LEFT_INDEX_ANIMATION")]
        public string LeftIndex;

        [DataInput(50)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("LEFT_THUMB_ANIMATION")]
        public string LeftThumb;

        [DataInput(60)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("RIGHT_THUMB_ANIMATION")]
        public string RightThumb;

        [DataInput(70)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("RIGHT_INDEX_ANIMATION")]
        public string RightIndex;

        [DataInput(80)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("RIGHT_MIDDLE_ANIMATION")]
        public string RightMiddle;

        [DataInput(90)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("RIGHT_RING_ANIMATION")]
        public string RightRing;

        [DataInput(100)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("RIGHT_PINKY_ANIMATION")]
        public string RightPinky;

        public string GetHeader() => "FINGER_ANIMATIONS".Localized();

        public string GetAnimation(KeyboardFinger finger) {
            switch (finger) {
                case KeyboardFinger.LeftPinky: return LeftPinky;
                case KeyboardFinger.LeftRing: return LeftRing;
                case KeyboardFinger.LeftMiddle: return LeftMiddle;
                case KeyboardFinger.LeftIndex: return LeftIndex;
                case KeyboardFinger.LeftThumb: return LeftThumb;
                case KeyboardFinger.RightThumb: return RightThumb;
                case KeyboardFinger.RightIndex: return RightIndex;
                case KeyboardFinger.RightMiddle: return RightMiddle;
                case KeyboardFinger.RightRing: return RightRing;
                case KeyboardFinger.RightPinky: return RightPinky;
                default: return null;
            }
        }

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;

            Watch(nameof(LeftPinky), delegate { Parent?.SynchronizeFingerAnimation(KeyboardFinger.LeftPinky); });
            Watch(nameof(LeftRing), delegate { Parent?.SynchronizeFingerAnimation(KeyboardFinger.LeftRing); });
            Watch(nameof(LeftMiddle), delegate { Parent?.SynchronizeFingerAnimation(KeyboardFinger.LeftMiddle); });
            Watch(nameof(LeftIndex), delegate { Parent?.SynchronizeFingerAnimation(KeyboardFinger.LeftIndex); });
            Watch(nameof(LeftThumb), delegate { Parent?.SynchronizeFingerAnimation(KeyboardFinger.LeftThumb); });
            Watch(nameof(RightThumb), delegate { Parent?.SynchronizeFingerAnimation(KeyboardFinger.RightThumb); });
            Watch(nameof(RightIndex), delegate { Parent?.SynchronizeFingerAnimation(KeyboardFinger.RightIndex); });
            Watch(nameof(RightMiddle), delegate { Parent?.SynchronizeFingerAnimation(KeyboardFinger.RightMiddle); });
            Watch(nameof(RightRing), delegate { Parent?.SynchronizeFingerAnimation(KeyboardFinger.RightRing); });
            Watch(nameof(RightPinky), delegate { Parent?.SynchronizeFingerAnimation(KeyboardFinger.RightPinky); });
        }
    }
}
