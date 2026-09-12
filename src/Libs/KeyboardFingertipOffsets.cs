using Cysharp.Threading.Tasks;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Serializations;

namespace FlameStream
{
    public enum FingertipOffsetMode
    {
        [Label("FINGERTIP_OFFSET_MODE_UNIFIED")]
        Unified,
        [Label("FINGERTIP_OFFSET_MODE_PER_FINGER")]
        PerFinger
    }

    public class KeyboardFingertipOffsets : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        private long uiVersion = 0;
        public override long GetVersion() => base.GetVersion() + uiVersion;

        public override SerializedStructuredData Serialize() {
            var serialized = base.Serialize();
            serialized.version = GetVersion();
            return serialized;
        }

        public async void RequestUiRefresh() {
            await UniTask.Yield();
            uiVersion++;
            Broadcast();
        }

        [DataInput(10)]
        [Label("FINGERTIP_OFFSET_MODE")]
        public FingertipOffsetMode Mode = FingertipOffsetMode.Unified;

        [DataInput(20)]
        [Label("UNIFIED_FINGERTIP_OFFSET")]
        [Description("FINGERTIP_OFFSET_DESC")]
        public Vector3 UnifiedOffset = new Vector3(0f, 0f, 0.012f);

        [DataInput(30)]
        [Label("LEFT_THUMB_OFFSET")]
        public Vector3 LeftThumb = new Vector3(0f, 0f, 0.012f);

        [DataInput(31)]
        [Label("LEFT_INDEX_OFFSET")]
        public Vector3 LeftIndex = new Vector3(0f, 0f, 0.012f);

        [DataInput(32)]
        [Label("LEFT_MIDDLE_OFFSET")]
        public Vector3 LeftMiddle = new Vector3(0f, 0f, 0.012f);

        [DataInput(33)]
        [Label("LEFT_RING_OFFSET")]
        public Vector3 LeftRing = new Vector3(0f, 0f, 0.012f);

        [DataInput(34)]
        [Label("LEFT_PINKY_OFFSET")]
        public Vector3 LeftPinky = new Vector3(0f, 0f, 0.012f);

        [DataInput(40)]
        [Label("RIGHT_THUMB_OFFSET")]
        public Vector3 RightThumb = new Vector3(0f, 0f, 0.012f);

        [DataInput(41)]
        [Label("RIGHT_INDEX_OFFSET")]
        public Vector3 RightIndex = new Vector3(0f, 0f, 0.012f);

        [DataInput(42)]
        [Label("RIGHT_MIDDLE_OFFSET")]
        public Vector3 RightMiddle = new Vector3(0f, 0f, 0.012f);

        [DataInput(43)]
        [Label("RIGHT_RING_OFFSET")]
        public Vector3 RightRing = new Vector3(0f, 0f, 0.012f);

        [DataInput(44)]
        [Label("RIGHT_PINKY_OFFSET")]
        public Vector3 RightPinky = new Vector3(0f, 0f, 0.012f);

        private static readonly string[] FingerPortNames = new[] {
            nameof(LeftThumb), nameof(LeftIndex), nameof(LeftMiddle), nameof(LeftRing), nameof(LeftPinky),
            nameof(RightThumb), nameof(RightIndex), nameof(RightMiddle), nameof(RightRing), nameof(RightPinky)
        };

        public string GetHeader() {
            if (Mode == FingertipOffsetMode.Unified) {
                return $"{"FINGERTIP_OFFSETS".Localized()} (Unified: {UnifiedOffset.z * 1000f:F1}mm)";
            }
            return $"{"FINGERTIP_OFFSETS".Localized()} (Per-Finger)";
        }

        public Vector3 GetOffset(KeyboardFinger finger) {
            switch (finger) {
                case KeyboardFinger.LeftThumb: return LeftThumb;
                case KeyboardFinger.LeftIndex: return LeftIndex;
                case KeyboardFinger.LeftMiddle: return LeftMiddle;
                case KeyboardFinger.LeftRing: return LeftRing;
                case KeyboardFinger.LeftPinky: return LeftPinky;
                case KeyboardFinger.RightThumb: return RightThumb;
                case KeyboardFinger.RightIndex: return RightIndex;
                case KeyboardFinger.RightMiddle: return RightMiddle;
                case KeyboardFinger.RightRing: return RightRing;
                case KeyboardFinger.RightPinky: return RightPinky;
                default:
                    return finger.IsLeftHand()
                        ? UnifiedOffset
                        : new Vector3(-UnifiedOffset.x, UnifiedOffset.y, UnifiedOffset.z);
            }
        }

        public void SyncUnifiedToIndividual() {
            Vector3 leftOffset = UnifiedOffset;
            Vector3 rightOffset = new Vector3(-UnifiedOffset.x, UnifiedOffset.y, UnifiedOffset.z);

            LeftThumb = leftOffset;
            LeftIndex = leftOffset;
            LeftMiddle = leftOffset;
            LeftRing = leftOffset;
            LeftPinky = leftOffset;

            RightThumb = rightOffset;
            RightIndex = rightOffset;
            RightMiddle = rightOffset;
            RightRing = rightOffset;
            RightPinky = rightOffset;

            SetDataInput(nameof(LeftThumb), leftOffset, true);
            SetDataInput(nameof(LeftIndex), leftOffset, true);
            SetDataInput(nameof(LeftMiddle), leftOffset, true);
            SetDataInput(nameof(LeftRing), leftOffset, true);
            SetDataInput(nameof(LeftPinky), leftOffset, true);

            SetDataInput(nameof(RightThumb), rightOffset, true);
            SetDataInput(nameof(RightIndex), rightOffset, true);
            SetDataInput(nameof(RightMiddle), rightOffset, true);
            SetDataInput(nameof(RightRing), rightOffset, true);
            SetDataInput(nameof(RightPinky), rightOffset, true);

            foreach (var name in FingerPortNames) {
                BroadcastDataInput(name);
            }
        }

        public void SyncUI() {
            try {
                bool isUnified = Mode == FingertipOffsetMode.Unified;

                var unifiedPort = GetDataInputPort(nameof(UnifiedOffset));
                if (unifiedPort != null) {
                    unifiedPort.Properties.hidden = !isUnified;
                    BroadcastDataInputProperties(nameof(UnifiedOffset));
                }

                foreach (var name in FingerPortNames) {
                    var port = GetDataInputPort(name);
                    if (port != null) {
                        port.Properties.hidden = isUnified;
                        BroadcastDataInputProperties(name);
                    }
                }
            } catch { }
        }

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;

            Watch(nameof(Mode), delegate {
                SyncUI();
                RequestUiRefresh();
            });

            Watch(nameof(UnifiedOffset), delegate {
                if (Mode == FingertipOffsetMode.Unified) {
                    SyncUnifiedToIndividual();
                }
                RequestUiRefresh();
            });

            SyncUI();
        }
    }
}
