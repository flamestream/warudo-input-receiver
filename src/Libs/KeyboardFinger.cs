using Warudo.Core.Attributes;

namespace FlameStream {
    public enum KeyboardFinger {
        [Label("LEFT_PINKY")]
        LeftPinky = 0,
        [Label("LEFT_RING")]
        LeftRing = 1,
        [Label("LEFT_MIDDLE")]
        LeftMiddle = 2,
        [Label("LEFT_INDEX")]
        LeftIndex = 3,
        [Label("LEFT_THUMB")]
        LeftThumb = 4,
        [Label("RIGHT_THUMB")]
        RightThumb = 5,
        [Label("RIGHT_INDEX")]
        RightIndex = 6,
        [Label("RIGHT_MIDDLE")]
        RightMiddle = 7,
        [Label("RIGHT_RING")]
        RightRing = 8,
        [Label("RIGHT_PINKY")]
        RightPinky = 9
    }

    public static class KeyboardFingerExtensions {
        public static bool IsLeftHand(this KeyboardFinger finger) {
            return finger >= KeyboardFinger.LeftPinky && finger <= KeyboardFinger.LeftThumb;
        }

        public static bool IsRightHand(this KeyboardFinger finger) {
            return finger >= KeyboardFinger.RightThumb && finger <= KeyboardFinger.RightPinky;
        }

        public static string GetLayerSuffix(this KeyboardFinger finger) {
            switch (finger) {
                case KeyboardFinger.LeftPinky: return "L_Pinky";
                case KeyboardFinger.LeftRing: return "L_Ring";
                case KeyboardFinger.LeftMiddle: return "L_Middle";
                case KeyboardFinger.LeftIndex: return "L_Index";
                case KeyboardFinger.LeftThumb: return "L_Thumb";
                case KeyboardFinger.RightThumb: return "R_Thumb";
                case KeyboardFinger.RightIndex: return "R_Index";
                case KeyboardFinger.RightMiddle: return "R_Middle";
                case KeyboardFinger.RightRing: return "R_Ring";
                case KeyboardFinger.RightPinky: return "R_Pinky";
                default: return finger.ToString();
            }
        }
    }
}
