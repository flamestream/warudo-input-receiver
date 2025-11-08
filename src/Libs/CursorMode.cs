using Warudo.Core.Attributes;
using Warudo.Core.Data;
using UnityEngine;

namespace FlameStream
{
    public class CursorMode : StructuredData {
        [DataInput]
        [Label("MODE")]
        public CursorModeValue Mode;
        public enum CursorModeValue {
            [Label("CURSOR_MODE_RAW_TRACKING")]
            RawTracking = 0,
            [Label("CURSOR_MODE_FIXED_DELTA")]
            FixedDelta = 1,
        }

        [DataInput]
        [Label("OOB_HANDLING")]
        public OutOfBoundRawTrackingHandlingValue OutOfBoundRawTrackingHandling;
        public enum OutOfBoundRawTrackingHandlingValue {
            [Label("CURSOR_MODE_RAW_OOB_OVERFLOW")]
            Overflow = 0,
            [Label("CURSOR_MODE_RAW_OOB_CLAMP")]
            Clamp = 1,
            [Label("CURSOR_MODE_RAW_OOB_FREEZE")]
            Freeze = 2,
            [Label("CURSOR_MODE_RAW_OOB_DISABLE_HAND")]
            DisableHand = 3,
        }

        [DataInput]
        [Label("OOB_HANDLING")]
        public OutOfBoundFixedDeltaHandlingValue OutOfBoundFixedDeltaHandling;
        public enum OutOfBoundFixedDeltaHandlingValue {
            [Label("CURSOR_MODE_FIXED_OOB_CLAMP")]
            Clamped = 0,
            [Label("CURSOR_MODE_FIXED_OOB_BACK_TO_CENTER")]
            BackToCenter = 1,
        }

        [DataInput]
        [Label("DISPLACEMENT_INFLUENCE_FACTOR")]
        [FloatSlider(0.1f, 3.0f, 0.1f)]
        public float DisplacementFactor = 1.0f;

        [DataInput]
        [Label("CURSOR_MODE_BOUND_OFFSETS")]
        [Description("CURSOR_MODE_BOUND_OFFSETS_DESC")]
        public Vector4 BoundOffsets = Vector4.zero;

        bool isReady;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Mode), delegate { UpdateDataInputProperties(); });
            Watch(nameof(OutOfBoundRawTrackingHandling), delegate { UpdateOutOfBoundRawTrackingHandlingDescription(); });
            Watch(nameof(OutOfBoundFixedDeltaHandling), delegate { UpdateOutOfBoundFixedDeltaHandlingDescription(); });
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
                    GetDataInputPort(nameof(Mode)).Properties.description = "CURSOR_MODE_FIXED_DELTA_DESC";
                    GetDataInputPort(nameof(OutOfBoundRawTrackingHandling)).Properties.hidden = true;
                    GetDataInputPort(nameof(OutOfBoundFixedDeltaHandling)).Properties.hidden = false;
                    GetDataInputPort(nameof(DisplacementFactor)).Properties.hidden = false;
                    UpdateOutOfBoundFixedDeltaHandlingDescription();
                    break;
                case CursorModeValue.RawTracking:
                default:
                    GetDataInputPort(nameof(Mode)).Properties.description = "CURSOR_MODE_RAW_TRACKING_DESC";
                    GetDataInputPort(nameof(OutOfBoundRawTrackingHandling)).Properties.hidden = false;
                    GetDataInputPort(nameof(OutOfBoundFixedDeltaHandling)).Properties.hidden = true;
                    GetDataInputPort(nameof(DisplacementFactor)).Properties.hidden = true;
                    UpdateOutOfBoundRawTrackingHandlingDescription();
                    break;
            }
            BroadcastDataInputProperties(nameof(Mode));
            BroadcastDataInputProperties(nameof(OutOfBoundRawTrackingHandling));
            BroadcastDataInputProperties(nameof(OutOfBoundFixedDeltaHandling));
            BroadcastDataInputProperties(nameof(DisplacementFactor));
        }

        protected void UpdateOutOfBoundRawTrackingHandlingDescription() {
            string desc = "";
            switch (OutOfBoundRawTrackingHandling) {
                case OutOfBoundRawTrackingHandlingValue.Clamp:
                    desc = "CURSOR_MODE_RAW_OOB_CLAMP_DESC";
                    break;
                case OutOfBoundRawTrackingHandlingValue.Freeze:
                    desc = "CURSOR_MODE_RAW_OOB_FREEZE_DESC";
                    break;
                case OutOfBoundRawTrackingHandlingValue.DisableHand:
                    desc = "CURSOR_MODE_RAW_OOB_DISABLE_HAND_DESC";
                    break;
                case OutOfBoundRawTrackingHandlingValue.Overflow:
                default:
                    desc = "CURSOR_MODE_RAW_OOB_OVERFLOW_DESC";
                    break;
            }
            GetDataInputPort(nameof(OutOfBoundRawTrackingHandling)).Properties.description = desc;
            BroadcastDataInputProperties(nameof(OutOfBoundRawTrackingHandling));
        }

        protected void UpdateOutOfBoundFixedDeltaHandlingDescription() {
            string desc = "";
            switch (OutOfBoundFixedDeltaHandling) {
                case OutOfBoundFixedDeltaHandlingValue.BackToCenter:
                    desc = "CURSOR_MODE_FIXED_OOB_BACK_TO_CENTER_DESC";
                    break;
                case OutOfBoundFixedDeltaHandlingValue.Clamped:
                default:
                    desc = "CURSOR_MODE_FIXED_OOB_CLAMP_DESC";
                    break;
            }
            GetDataInputPort(nameof(OutOfBoundFixedDeltaHandling)).Properties.description = desc;
            BroadcastDataInputProperties(nameof(OutOfBoundFixedDeltaHandling));
        }
    }
}
