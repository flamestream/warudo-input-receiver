using DG.Tweening;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class KeyboardKeyTransitions : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("FINGER_PRESS_TRANSITION")]
        public Transition FingerPress;

        [DataInput(20)]
        [Label("FINGER_RELEASE_TRANSITION")]
        public Transition FingerRelease;

        [DataInput(30)]
        [Label("HAND_MOVEMENT_TRANSITION")]
        public Transition HandMovement;

        [DataInput(40)]
        [Label("HAND_RETURN_TRANSITION")]
        public Transition HandReturn;

        [Trigger(50)]
        [Label("RESET_KEY_TRANSITIONS_TO_DEFAULTS")]
        public void TriggerResetToDefaults() {
            ResetToDefaults();
        }

        public void ResetToDefaults() {
            if (FingerPress == null) {
                FingerPress = StructuredData.Create<Transition>();
            }
            FingerPress.Time = 0.05f;
            FingerPress.Ease = Ease.OutQuad;
            FingerPress.SetDataInput(nameof(FingerPress.Time), 0.05f, true);
            FingerPress.SetDataInput(nameof(FingerPress.Ease), Ease.OutQuad, true);
            FingerPress.BroadcastDataInput(nameof(FingerPress.Time));
            FingerPress.BroadcastDataInput(nameof(FingerPress.Ease));

            if (FingerRelease == null) {
                FingerRelease = StructuredData.Create<Transition>();
            }
            FingerRelease.Time = 0.08f;
            FingerRelease.Ease = Ease.OutQuad;
            FingerRelease.SetDataInput(nameof(FingerRelease.Time), 0.08f, true);
            FingerRelease.SetDataInput(nameof(FingerRelease.Ease), Ease.OutQuad, true);
            FingerRelease.BroadcastDataInput(nameof(FingerRelease.Time));
            FingerRelease.BroadcastDataInput(nameof(FingerRelease.Ease));

            if (HandMovement == null) {
                HandMovement = StructuredData.Create<Transition>();
            }
            HandMovement.Time = 0.12f;
            HandMovement.Ease = Ease.OutCubic;
            HandMovement.SetDataInput(nameof(HandMovement.Time), 0.12f, true);
            HandMovement.SetDataInput(nameof(HandMovement.Ease), Ease.OutCubic, true);
            HandMovement.BroadcastDataInput(nameof(HandMovement.Time));
            HandMovement.BroadcastDataInput(nameof(HandMovement.Ease));

            if (HandReturn == null) {
                HandReturn = StructuredData.Create<Transition>();
            }
            HandReturn.Time = 0.30f;
            HandReturn.Ease = Ease.OutCubic;
            HandReturn.SetDataInput(nameof(HandReturn.Time), 0.30f, true);
            HandReturn.SetDataInput(nameof(HandReturn.Ease), Ease.OutCubic, true);
            HandReturn.BroadcastDataInput(nameof(HandReturn.Time));
            HandReturn.BroadcastDataInput(nameof(HandReturn.Ease));

            BroadcastDataInput(nameof(FingerPress));
            BroadcastDataInput(nameof(FingerRelease));
            BroadcastDataInput(nameof(HandMovement));
            BroadcastDataInput(nameof(HandReturn));

            if (Parent != null) {
                Context.Service?.Toast(
                    Warudo.Core.Server.ToastSeverity.Info,
                    Parent.Name,
                    "Key transitions reset to defaults."
                );
            }
        }

        [DataInput(100)]
        [Hidden]
        public Transition Press;

        [DataInput(101)]
        [Hidden]
        public Transition Release;

        public string GetHeader() {
            var fp = FingerPress != null ? $"{FingerPress.Time:0.##}s" : "0.05s";
            var hm = HandMovement != null ? $"{HandMovement.Time:0.##}s" : "0.12s";
            var hr = HandReturn != null ? $"{HandReturn.Time:0.##}s" : "0.3s";
            return $"{"KEY_TRANSITIONS".Localized()} (Finger: {fp}, Move: {hm}, Return: {hr})";
        }

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;

            if (FingerPress == null) {
                FingerPress = Press ?? StructuredData.Create<Transition>();
                if (Press == null) {
                    FingerPress.Time = 0.05f;
                    FingerPress.Ease = Ease.OutQuad;
                }
            }
            FingerPress.CollapsedSelf = true;

            if (FingerRelease == null) {
                FingerRelease = Release ?? StructuredData.Create<Transition>();
                if (Release == null) {
                    FingerRelease.Time = 0.08f;
                    FingerRelease.Ease = Ease.OutQuad;
                }
            }
            FingerRelease.CollapsedSelf = true;

            if (HandMovement == null) {
                HandMovement = StructuredData.Create<Transition>();
                HandMovement.Time = 0.12f;
                HandMovement.Ease = Ease.OutCubic;
            }
            HandMovement.CollapsedSelf = true;

            if (HandReturn == null) {
                HandReturn = StructuredData.Create<Transition>();
                HandReturn.Time = 0.30f;
                HandReturn.Ease = Ease.OutCubic;
            }
            HandReturn.CollapsedSelf = true;
        }
    }
}
