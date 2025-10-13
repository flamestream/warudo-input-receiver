using System;
using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream
{
    public partial class InputReceiverAsset : ReceiverAsset {

        Tween propPositionTween;
        Tween propRotationTween;

        void OnUpdatePropMotion() {
            if (!IsPropMotionWanted) {
                return;
            }

            var targetAnchor = TargetAnchor;
            if (targetAnchor == null) {
                return;
            }

            var targetPosition = Vector3.zero;
            var targetRotation = Vector3.zero;
            foreach (var currentSignal in SignalDefinition.Registry.Values) {

                if (!currentSignal.IsValid) continue;

                if (!currentSignal.IsActive) continue;

                var factor = 1f;
                PropMotionDefinition propMotion = null;
                switch (currentSignal) {
                    case ButtonDefinition buttonDefinition:
                        propMotion = buttonDefinition.PropMotion;
                        break;

                    case SwitchDefinition switchDefinition:
                    var propMotionSet = switchDefinition.PropMotionSet;
                        switch (switchDefinition.Value) {
                            case 1:
                                propMotion = switchDefinition.PropMotionSet.D1;
                                break;
                            case 2:
                                propMotion = switchDefinition.PropMotionSet.D2;
                                break;
                            case 3:
                                propMotion = switchDefinition.PropMotionSet.D3;
                                break;
                            case 4:
                                propMotion = switchDefinition.PropMotionSet.D4;
                                break;
                            case 5:
                                propMotion = switchDefinition.PropMotionSet.D5;
                                break;
                            case 6:
                                propMotion = switchDefinition.PropMotionSet.D6;
                                break;
                            case 7:
                                propMotion = switchDefinition.PropMotionSet.D7;
                                break;
                            case 8:
                                propMotion = switchDefinition.PropMotionSet.D8;
                                break;
                        }
                        break;

                    case AxisDefinition axisDefinition:
                        factor = axisDefinition.AdjustedValue;
                        switch (axisDefinition.NeutralState) {
                            case AxisNeutralState.Midpoint:
                                propMotion = factor < 0 ? axisDefinition.PropMotionSet.Min : axisDefinition.PropMotionSet.Max;
                                factor = Math.Abs(factor);
                                break;
                            case AxisNeutralState.Zero:
                                propMotion = axisDefinition.PropMotionSet.Max;
                                break;
                        }
                        break;
                }

                targetPosition += propMotion.TranslationFactor * factor;
                targetRotation += propMotion.RotationFactor * factor;
            }

            targetPosition *= PropMotionTranslationFactorSet.ScaleFactor * 0.001f; // To mm
            targetRotation *= PropMotionRotationFactorSet.ScaleFactor;

            var displacementAmount = Math.Abs(targetPosition.magnitude - targetAnchor.Transform.Position.magnitude);
            var displacementTime = displacementAmount * PropMotionTranslationFactorSet.TimeFactor * 3f;

            var rotationAmount = Math.Abs(targetRotation.magnitude - targetAnchor.Transform.Rotation.magnitude);
            var rotationTime = rotationAmount * PropMotionRotationFactorSet.TimeFactor * 0.003f;

            propPositionTween?.Kill();
            propPositionTween = DOTween.To(
                () => targetAnchor.Transform.Position,
                delegate(Vector3 it) { targetAnchor.Transform.Position = it; },
                targetPosition,
                displacementTime
            ).SetEase(PropMotionTranslationFactorSet.Ease);

            propRotationTween?.Kill();
            propRotationTween = DOTween.To(
                () => targetAnchor.Transform.Rotation,
                delegate(Vector3 it) { targetAnchor.Transform.Rotation = it; },
                targetRotation,
                rotationTime
            ).SetEase(PropMotionRotationFactorSet.Ease);
        }

        public class PropMotionFactorSet : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [FloatSlider(0.01f, 10f, 0.01f)]
            [Label("SCALE_FACTOR")]
            public float ScaleFactor = 1.0f;

            [DataInput]
            [FloatSlider(0.1f, 5f, 0.1f)]
            [Label("TIME_FACTOR")]
            public float TimeFactor = 1.0f;

            [DataInput]
            [Label("EASE")]
            public Ease Ease = Ease.Linear;

            public string GetHeader() {
                return $"Scale: {ScaleFactor} | Time: {TimeFactor} | Ease: {Ease}";
            }
        }
    }
}
