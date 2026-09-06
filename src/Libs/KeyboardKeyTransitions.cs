using DG.Tweening;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class KeyboardKeyTransitions : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("KEY_PRESS_TRANSITION")]
        public Transition Press;

        [DataInput(20)]
        [Label("KEY_RELEASE_TRANSITION")]
        public Transition Release;

        public string GetHeader() {
            var p = Press != null ? $"{Press.Time:0.##}s" : "0.04s";
            var r = Release != null ? $"{Release.Time:0.##}s" : "0.08s";
            return $"{"KEY_TRANSITIONS".Localized()} (Press: {p}, Release: {r})";
        }

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;

            if (Press == null) {
                Press = StructuredData.Create<Transition>();
                Press.Time = 0.04f;
                Press.Ease = Ease.OutQuad;
            }
            Press.CollapsedSelf = true;

            if (Release == null) {
                Release = StructuredData.Create<Transition>();
                Release.Time = 0.08f;
                Release.Ease = Ease.OutQuad;
            }
            Release.CollapsedSelf = true;
        }
    }
}
