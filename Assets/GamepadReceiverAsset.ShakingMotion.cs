using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;

namespace FlameStream {
    public partial class GamepadReceiverAsset : ReceiverAsset {

        [Section("Shaking Motion")]

        [Markdown]
        [HiddenIf(nameof(CanConfigureShaking))]
        public string ShakingMotionInstructions = "Allows game controller to shake in response to input.";

        [DataInput]
        [Label("Enable")]
        [DisabledIf(nameof(IsSetupIncomplete))]
        public bool IsShakingEnabled;

        public bool CanConfigureShaking() {
            return IsShakingEnabled;
        }

        public bool CannotConfigureShaking() {
            return !IsShakingEnabled;
        }

        public enum ControllerType {
            SwitchProController,
            PS5Controller,
        }

        [DataInput]
        [HiddenIf(nameof(CannotConfigureShaking))]
        [FloatSlider(0.1f, 10f, 0.01f)]
        float TiltInfluenceFactor = 1.0f;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureShaking))]
        [FloatSlider(0.1f, 10f, 0.01f)]
        float DisplacementInfluenceFactor = 1.0f;

        Tween rotationTween;
        Tween positionTween;

        bool LastAnyFaceButton;

        void PerformPropMovement() {
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

            switch(Pad) {
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


            positionTween?.Kill();
            positionTween = DOTween.To(
                () => anchor.Transform.Position,
                delegate(Vector3 it) { anchor.Transform.Position = it; },
                GamepadAnchorPosition + displacement * 0.001f * DisplacementInfluenceFactor,
                0.1f
            ).SetEase(Ease.OutBack);

            rotationTween?.Kill();
            rotationTween = DOTween.To(
                () => anchor.Transform.Rotation,
                delegate(Vector3 it) { anchor.Transform.Rotation = it; },
                GamepadAnchorRotation + tilt * TiltInfluenceFactor,
                0.1f
            ).SetEase(Ease.Linear);
        }

        float LeftStickX {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return (LX / (float)ushort.MaxValue - 0.5f) * 2f;
                }
                return 0.5f;
            }
        }

        float LeftStickY {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return (LY / (float)ushort.MaxValue - 0.5f) * 2f;
                }
                return 0.5f;
            }
        }

        float RightStickX {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return (LrX / (float)ushort.MaxValue - 0.5f) * 2f;
                }
                return 0.5f;
            }
        }

        float RightStickY {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return (LrY / (float)ushort.MaxValue - 0.5f) * 2f;
                }
                return 0.5f;
            }
        }

        bool IsAnyFaceButtonActivated {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return Array.Find(FaceButtonIdsSwitch, v => ActivatedButtonFlag(v)) != 0;
                }
                return false;
            }
        }

        bool IsAnyFaceButtonDown {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return Array.Find(FaceButtonIdsSwitch, v => ButtonFlag(v)) != 0;
                }
                return false;
            }
        }

        bool IsAnyShoulderButtonActivated {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return Array.Find(ShoulderButtonIdsSwitch, v => ActivatedButtonFlag(v)) != 0;
                }
                return false;
            }
        }

        bool IsAnyShoulderButtonDown {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return Array.Find(ShoulderButtonIdsSwitch, v => ButtonFlag(v)) != 0;
                }
                return false;
            }
        }

        // NOTE: Buttons indices are offset by one to allow dummy default at 0./
        public enum SwitchProButton: int
        {
            None = 0,
            B = 1,
            A = 2,
            Y = 3,
            X = 4,
            L = 5,
            R = 6,
            ZL = 7,
            ZR = 8,
            Plus = 9,
            Minus = 10,
            LeftStick = 11,
            RightStick = 12,
            Home = 13,
            Capture = 14,
        };

        public static readonly SwitchProButton[] LeftFaceButtonIdsSwitch = {
            SwitchProButton.LeftStick,
            SwitchProButton.Plus,
            SwitchProButton.Capture,
        };

        public static readonly SwitchProButton[] RightFaceButtonIdsSwitch = {
            SwitchProButton.A,
            SwitchProButton.B,
            SwitchProButton.X,
            SwitchProButton.Y,
            SwitchProButton.RightStick,
            SwitchProButton.Minus,
            SwitchProButton.Home,
        };

        public static readonly SwitchProButton[] FaceButtonIdsSwitch = LeftFaceButtonIdsSwitch.Union(RightFaceButtonIdsSwitch).ToArray();

        public static readonly SwitchProButton[] LeftShoulderButtonIdsSwitch = {
            SwitchProButton.L,
            SwitchProButton.ZL,
        };

        public static readonly SwitchProButton[] RightShoulderButtonIdsSwitch = {
            SwitchProButton.R,
            SwitchProButton.ZR,
        };

        public static readonly SwitchProButton[] ShoulderButtonIdsSwitch = LeftShoulderButtonIdsSwitch.Union(RightShoulderButtonIdsSwitch).ToArray();
    }
}