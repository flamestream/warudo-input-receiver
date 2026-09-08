using System;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream {
    public abstract class AlignmentThreshold : StructuredData, ICollapsibleStructuredData {
        public Action OnChange;

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            Watch("WhiteThreshold", () => OnChange?.Invoke());
            Watch("TransitionSpan", () => OnChange?.Invoke());
        }

        public abstract float GetWhiteThreshold();
        public abstract float GetTransitionSpan();
        public abstract string GetHeader();
    }

    public class DistanceAlignmentThreshold : AlignmentThreshold {
        [DataInput(10)]
        [FloatSlider(0.001f, 0.02f, 0.001f)]
        [Label("WHITE_THRESHOLD")]
        [Description("DISTANCE_WHITE_THRESHOLD_DESC")]
        public float WhiteThreshold = 0.006f;

        [DataInput(20)]
        [FloatSlider(0.002f, 0.08f, 0.001f)]
        [Label("TRANSITION_SPAN")]
        [Description("DISTANCE_TRANSITION_SPAN_DESC")]
        public float TransitionSpan = 0.024f;

        protected override void OnCreate() {
            base.OnCreate();
            SetDataInput(nameof(WhiteThreshold), WhiteThreshold, true);
            SetDataInput(nameof(TransitionSpan), TransitionSpan, true);
        }

        public override float GetWhiteThreshold() => WhiteThreshold;
        public override float GetTransitionSpan() => TransitionSpan;

        public override string GetHeader() => $"White: {WhiteThreshold * 1000f:F1}mm, Span: {TransitionSpan * 1000f:F1}mm";
    }

    public class SphereAlignmentThreshold : DistanceAlignmentThreshold {
        public SphereAlignmentThreshold() {
            WhiteThreshold = 0.006f;
            TransitionSpan = 0.024f;
        }
    }

    public class LineAlignmentThreshold : DistanceAlignmentThreshold {
        public LineAlignmentThreshold() {
            WhiteThreshold = 0.006f;
            TransitionSpan = 0.024f;
        }
    }

    public class AngleAlignmentThreshold : AlignmentThreshold {
        [DataInput(10)]
        [FloatSlider(0.5f, 10.0f, 0.5f)]
        [Label("WHITE_THRESHOLD")]
        [Description("ANGLE_WHITE_THRESHOLD_DESC")]
        public float WhiteThreshold = 3.0f;

        [DataInput(20)]
        [FloatSlider(1.0f, 30.0f, 1.0f)]
        [Label("TRANSITION_SPAN")]
        [Description("ANGLE_TRANSITION_SPAN_DESC")]
        public float TransitionSpan = 12.0f;

        protected override void OnCreate() {
            base.OnCreate();
            SetDataInput(nameof(WhiteThreshold), WhiteThreshold, true);
            SetDataInput(nameof(TransitionSpan), TransitionSpan, true);
        }

        public override float GetWhiteThreshold() => WhiteThreshold;
        public override float GetTransitionSpan() => TransitionSpan;

        public override string GetHeader() => $"White: {WhiteThreshold:F1}°, Span: {TransitionSpan:F1}°";
    }

    public class SlabAlignmentThreshold : AngleAlignmentThreshold {
        public SlabAlignmentThreshold() {
            WhiteThreshold = 3.0f;
            TransitionSpan = 12.0f;
        }
    }
}
