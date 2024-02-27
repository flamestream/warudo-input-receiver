using System;
using DG.Tweening;
using UnityEngine;

namespace FlameStream {
    public partial class GamepadReceiverAsset : ReceiverAsset {

        Tween gamepadRotationTween;
        Tween gamepadPositionTween;

        void PerformShakingMotionLoop() {
            if (!IsShakingEnabled) {
                return;
            }

            var anchor = GamepadAnchor;
            if (anchor == null) {
                return;
            }

            var displacement = Vector3.zero;
            if (IsAnyFaceButtonActivated) {
                displacement += Vector3.down;
            }
            if (IsAnyFaceButtonDown) {
                displacement += Vector3.down;
            }
            if (IsAnyShoulderButtonActivated) {
                displacement += Vector3.back;
            }
            if (IsAnyShoulderButtonDown) {
                displacement += Vector3.back;
            }

            var influenceX = LeftStickX + RightStickX;
            var influenceY = LeftStickY + RightStickY;

            switch(DPad) {
                case 1:
                    influenceX += -1f;
                    influenceY += 1f;
                    break;
                case 2:
                    influenceY += -2f;
                    break;
                case 3:
                    influenceX += 1f;
                    influenceY += 1f;
                    break;
                case 4:
                    influenceX += -2f;
                    break;
                case 6:
                    influenceX += 2f;
                    break;
                case 7:
                    influenceX += -1f;
                    influenceY += -1f;
                    break;
                case 8:
                    influenceY += -2f;
                    break;
                case 9:
                    influenceX += 1f;
                    influenceY += -1f;
                    break;
            }

            var tilt = new Vector3(-influenceY, 0, -influenceX);


            gamepadPositionTween?.Kill();
            gamepadPositionTween = DOTween.To(
                () => anchor.Transform.Position,
                delegate(Vector3 it) { anchor.Transform.Position = it; },
                RootAnchorPosition + displacement * 0.001f * DisplacementInfluenceFactor,
                0.1f
            ).SetEase(Ease.OutBack);

            gamepadRotationTween?.Kill();
            gamepadRotationTween = DOTween.To(
                () => anchor.Transform.Rotation,
                delegate(Vector3 it) { anchor.Transform.Rotation = it; },
                RootAnchorRotation + tilt * TiltInfluenceFactor,
                0.1f
            ).SetEase(Ease.Linear);
        }
    }
}
