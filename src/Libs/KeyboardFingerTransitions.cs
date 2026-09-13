using DG.Tweening;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class KeyboardFingerTransitions : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
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
        [Label("RETURN_TO_REST")]
        public bool ReturnToRest = true;

        [DataInput(45)]
        [Label("REST_TIMEOUT")]
        [FloatSlider(0.1f, 30f)]
        [HiddenIf(nameof(ReturnToRest), Is.False)]
        public float RestTimeout = 3.0f;

        [DataInput(50)]
        [Label("HAND_RETURN_TRANSITION")]
        [HiddenIf(nameof(ReturnToRest), Is.False)]
        public Transition HandReturn;

        [Trigger(60)]
        [Label("RESET_KEY_TRANSITIONS_TO_DEFAULTS")]
        public void TriggerResetToDefaults() {
            ResetToDefaults(true);
        }

        public void ResetToDefaults(bool showToast = true) {
            if (FingerPress == null) {
                FingerPress = StructuredData.Create<Transition>();
            }
            FingerPress.Time = 0.05f;
            FingerPress.Ease = Ease.OutQuad;
            FingerPress.CollapsedSelf = true;
            FingerPress.SetDataInput(nameof(FingerPress.Time), 0.05f, true);
            FingerPress.SetDataInput(nameof(FingerPress.Ease), Ease.OutQuad, true);
            FingerPress.BroadcastDataInput(nameof(FingerPress.Time));
            FingerPress.BroadcastDataInput(nameof(FingerPress.Ease));

            if (FingerRelease == null) {
                FingerRelease = StructuredData.Create<Transition>();
            }
            FingerRelease.Time = 0.08f;
            FingerRelease.Ease = Ease.OutQuad;
            FingerRelease.CollapsedSelf = true;
            FingerRelease.SetDataInput(nameof(FingerRelease.Time), 0.08f, true);
            FingerRelease.SetDataInput(nameof(FingerRelease.Ease), Ease.OutQuad, true);
            FingerRelease.BroadcastDataInput(nameof(FingerRelease.Time));
            FingerRelease.BroadcastDataInput(nameof(FingerRelease.Ease));

            if (HandMovement == null) {
                HandMovement = StructuredData.Create<Transition>();
            }
            HandMovement.Time = 0.12f;
            HandMovement.Ease = Ease.OutCubic;
            HandMovement.CollapsedSelf = true;
            HandMovement.SetDataInput(nameof(HandMovement.Time), 0.12f, true);
            HandMovement.SetDataInput(nameof(HandMovement.Ease), Ease.OutCubic, true);
            HandMovement.BroadcastDataInput(nameof(HandMovement.Time));
            HandMovement.BroadcastDataInput(nameof(HandMovement.Ease));

            ReturnToRest = true;
            SetDataInput(nameof(ReturnToRest), true, true);
            BroadcastDataInput(nameof(ReturnToRest));

            RestTimeout = 3.0f;
            SetDataInput(nameof(RestTimeout), 3.0f, true);
            BroadcastDataInput(nameof(RestTimeout));

            if (HandReturn == null) {
                HandReturn = StructuredData.Create<Transition>();
            }
            HandReturn.Time = 0.30f;
            HandReturn.Ease = Ease.OutCubic;
            HandReturn.CollapsedSelf = true;
            HandReturn.SetDataInput(nameof(HandReturn.Time), 0.30f, true);
            HandReturn.SetDataInput(nameof(HandReturn.Ease), Ease.OutCubic, true);
            HandReturn.BroadcastDataInput(nameof(HandReturn.Time));
            HandReturn.BroadcastDataInput(nameof(HandReturn.Ease));

            SetDataInput(nameof(FingerPress), FingerPress, true);
            SetDataInput(nameof(FingerRelease), FingerRelease, true);
            SetDataInput(nameof(HandMovement), HandMovement, true);
            SetDataInput(nameof(HandReturn), HandReturn, true);

            BroadcastDataInput(nameof(FingerPress));
            BroadcastDataInput(nameof(FingerRelease));
            BroadcastDataInput(nameof(HandMovement));
            BroadcastDataInput(nameof(HandReturn));

            if (showToast && Parent != null) {
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
            var hr = (ReturnToRest && HandReturn != null) ? $", Return: {HandReturn.Time:0.##}s" : "";
            return $"{"FINGER_TRANSITIONS".Localized()} (Finger: {fp}, Move: {hm}{hr})";
        }

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;

            bool isUninitialized = FingerPress == null || FingerRelease == null || HandMovement == null || HandReturn == null ||
                (FingerPress.Time == 0.5f && FingerPress.Ease == Ease.Linear &&
                 FingerRelease.Time == 0.5f && FingerRelease.Ease == Ease.Linear &&
                 HandMovement.Time == 0.5f && HandMovement.Ease == Ease.Linear &&
                 HandReturn.Time == 0.5f && HandReturn.Ease == Ease.Linear);

            if (isUninitialized) {
                ResetToDefaults(false);
            } else {
                if (FingerPress == null) {
                    FingerPress = Press ?? StructuredData.Create<Transition>();
                    if (Press == null) {
                        FingerPress.Time = 0.05f;
                        FingerPress.Ease = Ease.OutQuad;
                        FingerPress.SetDataInput(nameof(FingerPress.Time), 0.05f, true);
                        FingerPress.SetDataInput(nameof(FingerPress.Ease), Ease.OutQuad, true);
                    }
                    SetDataInput(nameof(FingerPress), FingerPress, true);
                }
                if (FingerRelease == null) {
                    FingerRelease = Release ?? StructuredData.Create<Transition>();
                    if (Release == null) {
                        FingerRelease.Time = 0.08f;
                        FingerRelease.Ease = Ease.OutQuad;
                        FingerRelease.SetDataInput(nameof(FingerRelease.Time), 0.08f, true);
                        FingerRelease.SetDataInput(nameof(FingerRelease.Ease), Ease.OutQuad, true);
                    }
                    SetDataInput(nameof(FingerRelease), FingerRelease, true);
                }
                if (HandMovement == null) {
                    HandMovement = StructuredData.Create<Transition>();
                    HandMovement.Time = 0.12f;
                    HandMovement.Ease = Ease.OutCubic;
                    HandMovement.SetDataInput(nameof(HandMovement.Time), 0.12f, true);
                    HandMovement.SetDataInput(nameof(HandMovement.Ease), Ease.OutCubic, true);
                    SetDataInput(nameof(HandMovement), HandMovement, true);
                }
                if (HandReturn == null) {
                    HandReturn = StructuredData.Create<Transition>();
                    HandReturn.Time = 0.30f;
                    HandReturn.Ease = Ease.OutCubic;
                    HandReturn.SetDataInput(nameof(HandReturn.Time), 0.30f, true);
                    HandReturn.SetDataInput(nameof(HandReturn.Ease), Ease.OutCubic, true);
                    SetDataInput(nameof(HandReturn), HandReturn, true);
                }
            }

            if (FingerPress != null) FingerPress.CollapsedSelf = true;
            if (FingerRelease != null) FingerRelease.CollapsedSelf = true;
            if (HandMovement != null) HandMovement.CollapsedSelf = true;
            if (HandReturn != null) HandReturn.CollapsedSelf = true;

            Watch(nameof(ReturnToRest), delegate {
                BroadcastDataInput(nameof(ReturnToRest));
            });
        }
    }
}
