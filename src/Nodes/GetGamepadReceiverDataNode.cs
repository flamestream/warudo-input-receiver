using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream
{
    public abstract class GetGamepadReceiverDataNode : Node {

        [DataInput]
        public GamepadReceiverAsset Receiver;

        [DataOutput]
        [Label("IS_ACTIVE")]
        public bool IsActive() => Receiver != null && Receiver.Active;

        [DataOutput]
        [Label("IS_RECEIVING")]
        public bool IsReceiving() => Receiver != null && Receiver.IsReceiving;

        [DataOutput(100)]
        [Label("LEFT_STICK_X")]
        public float LeftStickX() => Receiver?.LeftStickX ?? 0.5f;

        [DataOutput(100)]
        [Label("LEFT_STICK_Y")]
        public float LeftStickY() => Receiver?.LeftStickY ?? 0.5f;

        [DataOutput(100)]
        [Label("RIGHT_STICK_X")]
        public float RightStickX() => Receiver?.RightStickX ?? 0.5f;

        [DataOutput(100)]
        [Label("RIGHT_STICK_Y")]
        public float RightStickY() => Receiver?.RightStickY ?? 0.5f;

        [DataOutput(100)]
        [Label("DPAD")]
        public int DPad() => Receiver?.DPad ?? 5;

        [DataOutput(100)]
        [Label("HOVER_LEFT_FACE_INPUT_ID")]
        public string LeftFaceHoverInputId() => Receiver?.LeftFaceHoverInputId;

        [DataOutput(100)]
        [Label("HOVER_RIGHT_FACE_INPUT_ID")]
        public string RightFaceHoverInputId() => Receiver?.RightFaceHoverInputId;
    }
}
