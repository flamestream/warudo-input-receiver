using DG.Tweening;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class Transition : StructuredData, ICollapsibleStructuredData {
        [DataInput]
        [Label("TRANSITION_TIME")]
        [FloatSlider(0f, 3f, 0.01f)]
        public float Time = 0.5f;

        [DataInput]
        [Label("TRANSITION_EASING")]
        public Ease Ease = Ease.Linear;

        [DataInput]
        [Label("TRANSITION_DELAY")]
        [FloatSlider(0f, 1f, 0.1f)]
        public float Delay = 0f;

        protected override void OnCreate() {
            base.OnCreate();
            var delayPort = GetDataInputPort(nameof(Delay));
            if (delayPort?.Properties != null) {
                delayPort.Properties.hidden = true;
            }
        }

        public string ShortLabel {
            get {
                return Delay > 0 ? $"{Time}(+{Delay})s" : $"{Time}s";
            }
        }

        public virtual string GetHeader() {
            return Delay > 0 ? $"{Time:0.##}s (+{Delay:0.#}s) ({Ease})" : $"{Time:0.##}s ({Ease})";
        }
    }

    public class ActiveTransition : Transition {
        public ActiveTransition() {
            Time = 0.5f;
            Ease = Ease.Linear;
            Delay = 0.1f;
        }
    }

    public class InactiveTransition : Transition {
        public InactiveTransition() {
            Time = 0.5f;
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
