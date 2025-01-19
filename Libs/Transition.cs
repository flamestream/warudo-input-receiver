using DG.Tweening;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream
{
    public class Transition : StructuredData {
        [DataInput]
        [Label("TRANSITION_TIME")]
        [FloatSlider(0f, 3f, 0.01f)]
        public float Time = 0.5f;

        [DataInput]
        [Label("TRANSITION_EASING")]
        public Ease Ease = Ease.Linear;

        public string ShortLabel {
            get {
                return $"{Time}s";
            }
        }
    }

    public class ActiveTransition : Transition {
        public ActiveTransition() {
            Time = 0.2f;
            Ease = Ease.Linear;
        }
    }

    public class PoseTransition : Transition {
        public PoseTransition() {
            Time = 0.05f;
            Ease = Ease.Linear;
        }
    }

    public class BodyFollowMovementTransition : Transition {
        public BodyFollowMovementTransition() {
            Time = 3f;
            Ease = Ease.OutCirc;
        }
    }

    public class DelayableTransition : Transition {
        [DataInput]
        [Label("DELAY_TIME")]
        [FloatSlider(0f, 3f, 0.01f)]
        public float DelayTime = 0f;

        public new string ShortLabel {
            get {
                return $"{Time}(+{DelayTime})s";
            }
        }
    }
}
