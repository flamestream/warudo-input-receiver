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
        public FingertipOffsetVisualSetupTransform UnifiedOffset;

        [DataInput(30)]
        [Label("LEFT_THUMB_OFFSET")]
        public FingertipOffsetVisualSetupTransform LeftThumb;

        [DataInput(31)]
        [Label("LEFT_INDEX_OFFSET")]
        public FingertipOffsetVisualSetupTransform LeftIndex;

        [DataInput(32)]
        [Label("LEFT_MIDDLE_OFFSET")]
        public FingertipOffsetVisualSetupTransform LeftMiddle;

        [DataInput(33)]
        [Label("LEFT_RING_OFFSET")]
        public FingertipOffsetVisualSetupTransform LeftRing;

        [DataInput(34)]
        [Label("LEFT_PINKY_OFFSET")]
        public FingertipOffsetVisualSetupTransform LeftPinky;

        [DataInput(40)]
        [Label("RIGHT_THUMB_OFFSET")]
        public FingertipOffsetVisualSetupTransform RightThumb;

        [DataInput(41)]
        [Label("RIGHT_INDEX_OFFSET")]
        public FingertipOffsetVisualSetupTransform RightIndex;

        [DataInput(42)]
        [Label("RIGHT_MIDDLE_OFFSET")]
        public FingertipOffsetVisualSetupTransform RightMiddle;

        [DataInput(43)]
        [Label("RIGHT_RING_OFFSET")]
        public FingertipOffsetVisualSetupTransform RightRing;

        [DataInput(44)]
        [Label("RIGHT_PINKY_OFFSET")]
        public FingertipOffsetVisualSetupTransform RightPinky;

        private static readonly string[] FingerPortNames = new[] {
            nameof(LeftThumb), nameof(LeftIndex), nameof(LeftMiddle), nameof(LeftRing), nameof(LeftPinky),
            nameof(RightThumb), nameof(RightIndex), nameof(RightMiddle), nameof(RightRing), nameof(RightPinky)
        };

        public string GetHeader() {
            if (Mode == FingertipOffsetMode.Unified) {
                float z = UnifiedOffset != null ? UnifiedOffset.Position.z : 0f;
                return $"{"FINGERTIP_OFFSETS".Localized()} (Unified: {z * 1000f:F1}mm)";
            }
            return $"{"FINGERTIP_OFFSETS".Localized()} (Per-Finger)";
        }

        public Vector3 GetOffset(KeyboardFinger finger) {
            switch (finger) {
                case KeyboardFinger.LeftThumb: return LeftThumb != null ? LeftThumb.Position : Vector3.zero;
                case KeyboardFinger.LeftIndex: return LeftIndex != null ? LeftIndex.Position : Vector3.zero;
                case KeyboardFinger.LeftMiddle: return LeftMiddle != null ? LeftMiddle.Position : Vector3.zero;
                case KeyboardFinger.LeftRing: return LeftRing != null ? LeftRing.Position : Vector3.zero;
                case KeyboardFinger.LeftPinky: return LeftPinky != null ? LeftPinky.Position : Vector3.zero;
                case KeyboardFinger.RightThumb: return RightThumb != null ? RightThumb.Position : Vector3.zero;
                case KeyboardFinger.RightIndex: return RightIndex != null ? RightIndex.Position : Vector3.zero;
                case KeyboardFinger.RightMiddle: return RightMiddle != null ? RightMiddle.Position : Vector3.zero;
                case KeyboardFinger.RightRing: return RightRing != null ? RightRing.Position : Vector3.zero;
                case KeyboardFinger.RightPinky: return RightPinky != null ? RightPinky.Position : Vector3.zero;
                default:
                    var uPos = UnifiedOffset != null ? UnifiedOffset.Position : Vector3.zero;
                    return finger.IsLeftHand()
                        ? uPos
                        : new Vector3(-uPos.x, uPos.y, uPos.z);
            }
        }

        public void SyncUnifiedToIndividual() {
            if (UnifiedOffset == null) return;
            Vector3 leftPos = UnifiedOffset.Position;
            Vector3 rightPos = new Vector3(-leftPos.x, leftPos.y, leftPos.z);

            if (LeftThumb != null) LeftThumb.Position = leftPos;
            if (LeftIndex != null) LeftIndex.Position = leftPos;
            if (LeftMiddle != null) LeftMiddle.Position = leftPos;
            if (LeftRing != null) LeftRing.Position = leftPos;
            if (LeftPinky != null) LeftPinky.Position = leftPos;

            if (RightThumb != null) RightThumb.Position = rightPos;
            if (RightIndex != null) RightIndex.Position = rightPos;
            if (RightMiddle != null) RightMiddle.Position = rightPos;
            if (RightRing != null) RightRing.Position = rightPos;
            if (RightPinky != null) RightPinky.Position = rightPos;

            SetDataInput(nameof(LeftThumb), LeftThumb, true);
            SetDataInput(nameof(LeftIndex), LeftIndex, true);
            SetDataInput(nameof(LeftMiddle), LeftMiddle, true);
            SetDataInput(nameof(LeftRing), LeftRing, true);
            SetDataInput(nameof(LeftPinky), LeftPinky, true);

            SetDataInput(nameof(RightThumb), RightThumb, true);
            SetDataInput(nameof(RightIndex), RightIndex, true);
            SetDataInput(nameof(RightMiddle), RightMiddle, true);
            SetDataInput(nameof(RightRing), RightRing, true);
            SetDataInput(nameof(RightPinky), RightPinky, true);

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

        public FingertipOffsetVisualSetupTransform GetFingerTransform(KeyboardFinger finger) {
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
                default: return null;
            }
        }

        public void EnsureTransforms() {
            if (UnifiedOffset == null) {
                UnifiedOffset = StructuredData.Create<FingertipOffsetVisualSetupTransform>();
                UnifiedOffset.TargetFinger = KeyboardFinger.LeftIndex;
                UnifiedOffset.LocalizationKey = "FINGERTIP_OFFSET_UNIFIED";
            }
            UnifiedOffset.SetHideMirrorToOppositeHand(true);

            EnsureFingerTransform(ref LeftThumb, KeyboardFinger.LeftThumb, "FINGERTIP_OFFSET_LEFT_THUMB");
            EnsureFingerTransform(ref LeftIndex, KeyboardFinger.LeftIndex, "FINGERTIP_OFFSET_LEFT_INDEX");
            EnsureFingerTransform(ref LeftMiddle, KeyboardFinger.LeftMiddle, "FINGERTIP_OFFSET_LEFT_MIDDLE");
            EnsureFingerTransform(ref LeftRing, KeyboardFinger.LeftRing, "FINGERTIP_OFFSET_LEFT_RING");
            EnsureFingerTransform(ref LeftPinky, KeyboardFinger.LeftPinky, "FINGERTIP_OFFSET_LEFT_PINKY");

            EnsureFingerTransform(ref RightThumb, KeyboardFinger.RightThumb, "FINGERTIP_OFFSET_RIGHT_THUMB");
            EnsureFingerTransform(ref RightIndex, KeyboardFinger.RightIndex, "FINGERTIP_OFFSET_RIGHT_INDEX");
            EnsureFingerTransform(ref RightMiddle, KeyboardFinger.RightMiddle, "FINGERTIP_OFFSET_RIGHT_MIDDLE");
            EnsureFingerTransform(ref RightRing, KeyboardFinger.RightRing, "FINGERTIP_OFFSET_RIGHT_RING");
            EnsureFingerTransform(ref RightPinky, KeyboardFinger.RightPinky, "FINGERTIP_OFFSET_RIGHT_PINKY");

            foreach (var name in FingerPortNames) {
                if (System.Enum.TryParse<KeyboardFinger>(name, out var finger)) {
                    var tr = GetFingerTransform(finger);
                    if (tr != null) {
                        tr.SetHideMirrorToOppositeHand(false);
                    }
                }
            }
        }

        private void EnsureFingerTransform(ref FingertipOffsetVisualSetupTransform tr, KeyboardFinger finger, string locKey) {
            if (tr == null) {
                tr = StructuredData.Create<FingertipOffsetVisualSetupTransform>();
            }
            tr.TargetFinger = finger;
            tr.LocalizationKey = locKey;
        }

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            EnsureTransforms();

            Watch(nameof(Mode), delegate {
                SyncUI();
                RequestUiRefresh();
                Parent?.SetupAllFingertipVisualSetupTransforms();
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
