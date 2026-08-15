using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream {
    public class KeyboardKeyBinding : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData {

        [DataInput]
        [Label("KEY")]
        public VKCode Key;

        [DataInput]
        [Label("ASSIGNED_FINGER")]
        public KeyboardFinger Finger;

        [DataInput]
        [Label("DELTA_POSITION")]
        public Vector3 DeltaPosition;

        [DataInput]
        [Label("DELTA_ROTATION")]
        public Vector3 DeltaRotation;

        [DataInput]
        [Label("ENABLE_HAND_MOVEMENT")]
        public bool EnableHandMovement = true;

        public string GetHeader() {
            var handSide = Finger.IsLeftHand() ? "L" : "R";
            var handMovementTag = EnableHandMovement ? "" : " (No Hand Move)";
            return $"[{Key}] ➔ [{handSide}] {Finger} ΔPos({DeltaPosition.x:F3}, {DeltaPosition.y:F3}, {DeltaPosition.z:F3}){handMovementTag}";
        }
    }
}
