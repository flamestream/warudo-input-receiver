using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream
{
    public partial class InputReceiverAsset : ReceiverAsset {

        Tween leftHandPositionTween;
        Tween leftHandRotationTween;

        Tween rightHandPositionTween;
        Tween rightHandRotationTween;

        // Indicates which hand was tracked first, to determines which hand gets to hold controller
        // -1: Left
        //  0: None
        //  1: Right
        short priorityTrackedHand;
        bool LastIsLeftHandTracked;
        bool LastIsRightHandTracked;

        // Called by node, since I don't really know how to associate tracker asset (like LeapMotion)
        public short ProcessHandTracking(bool IsLeftHandTracked, bool IsRightHandTracked) {

            if (priorityTrackedHand == 0) {
                if (IsLeftHandTracked) {
                    priorityTrackedHand = -1;
                } else if (IsRightHandTracked) {
                    priorityTrackedHand = 1;
                }
            } else {
                if (!IsLeftHandTracked && !IsRightHandTracked) {
                    priorityTrackedHand = 0;
                }
            }

            if (IsLeftHandTracked && !LastIsLeftHandTracked && !IsRightHandTracked) {
                // Ensure gamepad is on right hand
                TransitionRightHandAnchorToHoldState();
            } else if (IsRightHandTracked && !LastIsRightHandTracked && !IsLeftHandTracked) {
                // Ensure gamepad is on left hand
                TransitionLeftHandAnchorToHoldState();
            } else if (!IsLeftHandTracked && !IsRightHandTracked && (LastIsLeftHandTracked || LastIsRightHandTracked)) {
                TransitionHandAnchorsToResetState();
            }

            LastIsLeftHandTracked = IsLeftHandTracked;
            LastIsRightHandTracked = IsRightHandTracked;

            return priorityTrackedHand;
        }

        public void TransitionLeftHandAnchorToHoldState() {
            var leftAnchor = LeftHandAnchor;
            var rightAnchor = RightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            AttachGamepad(false);

            var leftHandAnchor = SavedBindingData.LeftHandAnchor;

            leftHandPositionTween?.Kill();
            leftHandPositionTween = DOTween.To(
                () => leftAnchor.Transform.Position,
                delegate(Vector3 it) { leftAnchor.Transform.Position = it; },
                leftHandAnchor.Position + UntrackedLeftHandMotion.Translation,
                UntrackedLeftHandMotion.TranslationTransition.Time
            ).SetEase(UntrackedLeftHandMotion.TranslationTransition.Ease);
            if (UntrackedLeftHandMotion.TranslationTransition.Delay > 0) {
                leftHandPositionTween.SetDelay(UntrackedLeftHandMotion.TranslationTransition.Delay);
            }

            leftHandRotationTween?.Kill();
            leftHandRotationTween = DOTween.To(
                () => leftAnchor.Transform.Rotation,
                delegate(Vector3 it) { leftAnchor.Transform.Rotation = it; },
                leftHandAnchor.Rotation + UntrackedLeftHandMotion.Rotation,
                UntrackedLeftHandMotion.RotationTransition.Time
            ).SetEase(UntrackedLeftHandMotion.RotationTransition.Ease);
            if (UntrackedLeftHandMotion.RotationTransition.Delay > 0) {
                leftHandRotationTween.SetDelay(UntrackedLeftHandMotion.RotationTransition.Delay);
            }
        }

        public void TransitionRightHandAnchorToHoldState() {
            var leftAnchor = LeftHandAnchor;
            var rightAnchor = RightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            AttachGamepad(true);

            var rightHandAnchor = SavedBindingData.RightHandAnchor;

            rightHandPositionTween?.Kill();
            rightHandPositionTween = DOTween.To(
                () => rightAnchor.Transform.Position,
                delegate(Vector3 it) { rightAnchor.Transform.Position = it; },
                rightHandAnchor.Position + UntrackedRightHandMotion.Translation,
                UntrackedRightHandMotion.TranslationTransition.Time
            ).SetEase(UntrackedRightHandMotion.TranslationTransition.Ease);
            if (UntrackedRightHandMotion.TranslationTransition.Delay > 0) {
                rightHandPositionTween.SetDelay(UntrackedRightHandMotion.TranslationTransition.Delay);
            }

            rightHandRotationTween?.Kill();
            rightHandRotationTween = DOTween.To(
                () => rightAnchor.Transform.Rotation,
                delegate(Vector3 it) { rightAnchor.Transform.Rotation = it; },
                rightHandAnchor.Rotation + UntrackedRightHandMotion.Rotation,
                UntrackedRightHandMotion.RotationTransition.Time
            ).SetEase(UntrackedRightHandMotion.RotationTransition.Ease);
            if (UntrackedRightHandMotion.RotationTransition.Delay > 0) {
                rightHandRotationTween.SetDelay(UntrackedRightHandMotion.RotationTransition.Delay);
            }
        }

        public void TransitionHandAnchorsToResetState() {
            var leftAnchor = LeftHandAnchor;
            var rightAnchor = RightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            var rightHandAnchor = SavedBindingData.RightHandAnchor;

            rightHandPositionTween?.Kill();
            rightHandPositionTween = DOTween.To(
                () => rightAnchor.Transform.Position,
                delegate(Vector3 it) { rightAnchor.Transform.Position = it; },
                rightHandAnchor.Position,
                UntrackedRightHandMotion.ReturnTransition.Time
            ).SetEase(UntrackedRightHandMotion.ReturnTransition.Ease);
            if (UntrackedRightHandMotion.ReturnTransition.Delay > 0) {
                rightHandPositionTween.SetDelay(UntrackedRightHandMotion.ReturnTransition.Delay);
            }

            rightHandRotationTween?.Kill();
            rightHandRotationTween = DOTween.To(
                () => rightAnchor.Transform.Rotation,
                delegate(Vector3 it) { rightAnchor.Transform.Rotation = it; },
                rightHandAnchor.Rotation,
                UntrackedRightHandMotion.ReturnTransition.Time
            ).SetEase(UntrackedRightHandMotion.ReturnTransition.Ease);
            if (UntrackedRightHandMotion.ReturnTransition.Delay > 0) {
                rightHandRotationTween.SetDelay(UntrackedRightHandMotion.ReturnTransition.Delay);
            }

            var leftHandAnchor = SavedBindingData.LeftHandAnchor;

            leftHandPositionTween?.Kill();
            leftHandPositionTween = DOTween.To(
                () => leftAnchor.Transform.Position,
                delegate(Vector3 it) { leftAnchor.Transform.Position = it; },
                leftHandAnchor.Position,
                UntrackedLeftHandMotion.ReturnTransition.Time
            ).SetEase(UntrackedLeftHandMotion.ReturnTransition.Ease);
            if (UntrackedLeftHandMotion.ReturnTransition.Delay > 0) {
                leftHandPositionTween.SetDelay(UntrackedLeftHandMotion.ReturnTransition.Delay);
            }

            leftHandRotationTween?.Kill();
            leftHandRotationTween = DOTween.To(
                () => leftAnchor.Transform.Rotation,
                delegate(Vector3 it) { leftAnchor.Transform.Rotation = it; },
                leftHandAnchor.Rotation,
                UntrackedLeftHandMotion.ReturnTransition.Time
            ).SetEase(UntrackedLeftHandMotion.ReturnTransition.Ease);
            if (UntrackedLeftHandMotion.ReturnTransition.Delay > 0) {
                leftHandRotationTween.SetDelay(UntrackedLeftHandMotion.ReturnTransition.Delay);
            }
        }

        public class UntrackedHandMotionSet : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [Label("TRANSLATION")]
            public Vector3 Translation = new Vector3(0f, -0.05f, -0.02f);

            [DataInput]
            [Label("TRANSLATION_TRANSITION")]
            public UntrackedHandTranslationTransitionDefinition TranslationTransition;

            [DataInput]
            [Label("ROTATION")]
            public Vector3 Rotation = new Vector3(-30f, 10f, 0f);

            [DataInput]
            [Label("ROTATION_TRANSITION")]
            public UntrackedHandRotationTransitionDefinition RotationTransition;

            [DataInput]
            [Label("RETURN_TRANSITION")]
            public UntrackedHandReturnTransitionDefinition ReturnTransition;

            public string GetHeader() {
                return $"T {Translation} | R {Rotation}";
            }
        }

        public class UntrackedHandTranslationTransitionDefinition : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [Label("TIME")]
            [FloatSlider(0f, 2f, 0.01f)]
            public float Time = 1f;

            [DataInput]
            [Label("DELAY")]
            [FloatSlider(0f, 2f, 0.01f)]
            public float Delay = 0f;

            [DataInput]
            [Label("EASE")]
            public Ease Ease = Ease.OutCubic;

            public string GetHeader() {
                return $"{Ease} in {Time}(+{Delay})s";
            }
        }

        public class UntrackedHandRotationTransitionDefinition : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [Label("TIME")]
            [FloatSlider(0f, 2f, 0.01f)]
            public float Time = 1.5f;

            [DataInput]
            [Label("DELAY")]
            [FloatSlider(0f, 2f, 0.01f)]
            public float Delay = 0f;

            [DataInput]
            [Label("EASE")]
            public Ease Ease = Ease.OutBack;

            public string GetHeader() {
                return $"{Ease} in {Time}(+{Delay})s";
            }
        }

        public class UntrackedHandReturnTransitionDefinition : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [Label("TIME")]
            [FloatSlider(0f, 2f, 0.01f)]
            public float Time = 0.2f;

            [DataInput]
            [Label("DELAY")]
            [FloatSlider(0f, 2f, 0.01f)]
            public float Delay = 0f;

            [DataInput]
            [Label("EASE")]
            public Ease Ease = Ease.OutBack;

            public string GetHeader() {
                return $"{Ease} in {Time}(+{Delay})s";
            }
        }
    }
}
