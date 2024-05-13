using DG.Tweening;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream
{
    public class CursorMode : StructuredData {
        [DataInput]
        [Label("MODE")]
        public CursorModeValue Mode;
        public enum CursorModeValue {
            RawTracking = 0,
            FixedDelta = 1,
        }

        [DataInput]
        [Label("OOB_HANDLING")]
        public OutOfBoundRawTrackingHandlingValue OutOfBoundRawTrackingHandling;
        public enum OutOfBoundRawTrackingHandlingValue {
            Overflow = 0,
            Clamp = 1,
            Freeze = 2,
            DisableHand = 3,
        }

        [DataInput]
        [Label("OOB_HANDLING")]
        public OutOfBoundFixedDeltaHandlingValue OutOfBoundFixedDeltaHandling;
        public enum OutOfBoundFixedDeltaHandlingValue {
            Clamped = 0,
            BackToCenter = 1
        }

        [DataInput]
        [Label("DISPLACEMENT_INFLUENCE_FACTOR")]
        [FloatSlider(0.1f, 3.0f, 0.1f)]
        public float DisplacementFactor = 1.0f;

        bool isReady;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Mode), delegate { UpdateDataInputProperties(); });
        }

        protected override void OnUpdate() {
            base.OnUpdate();
            if (!isReady) {
                isReady = true;
                UpdateDataInputProperties();
            }
        }

        protected void UpdateDataInputProperties() {
            switch (Mode) {
                    case CursorModeValue.FixedDelta:
                    GetDataInputPort(nameof(OutOfBoundRawTrackingHandling)).Properties.hidden = true;
                    GetDataInputPort(nameof(OutOfBoundFixedDeltaHandling)).Properties.hidden = false;
                    GetDataInputPort(nameof(DisplacementFactor)).Properties.hidden = false;
                    break;
                case CursorModeValue.RawTracking:
                default:
                    GetDataInputPort(nameof(OutOfBoundRawTrackingHandling)).Properties.hidden = false;
                    GetDataInputPort(nameof(OutOfBoundFixedDeltaHandling)).Properties.hidden = true;
                    GetDataInputPort(nameof(DisplacementFactor)).Properties.hidden = true;
                    break;
            }
            BroadcastDataInputProperties(nameof(OutOfBoundRawTrackingHandling));
            BroadcastDataInputProperties(nameof(OutOfBoundFixedDeltaHandling));
            BroadcastDataInputProperties(nameof(DisplacementFactor));
        }
    }
}
