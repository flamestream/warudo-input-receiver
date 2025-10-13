using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using static FlameStream.GamepadReceiverAsset;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.GetGamepadReceiverDataGenericAxis",
    Title = "NODE_TITLE_GAMEPAD_RECEIVER_GENERIC_AXIS",
    Category = "FS_NODE_CATEGORY_GAMEPAD"
)]
    public class GetGamepadReceiverDataGenericAxisNode: Node {

        [DataInput]
        public GamepadReceiverAsset Receiver;

        [DataOutput]
        [Label("IS_ACTIVE")]
        public bool IsActive() => Receiver != null && Receiver.Active;

        [DataOutput]
        [Label("IS_RECEIVING")]
        public bool IsReceiving() => Receiver != null && Receiver.IsReceiving;

        [DataOutput(50)]
        [Label("LX")]
        public float LX() => ShortToFloatScale(Receiver?.LX);

        [DataOutput(50)]
        [Label("LY")]
        public float LY() => ShortToFloatScale(Receiver?.LY);

        [DataOutput(50)]
        [Label("LZ")]
        public float LZ() => ShortToFloatScale(Receiver?.LZ);

        [DataOutput(50)]
        [Label("LrX")]
        public float LrX() => ShortToFloatScale(Receiver?.LrX);

        [DataOutput(50)]
        [Label("LrY")]
        public float LrY() => ShortToFloatScale(Receiver?.LrY);

        [DataOutput(50)]
        [Label("LrZ")]
        public float LrZ() => ShortToFloatScale(Receiver?.LrZ);

        [DataOutput(50)]
        [Label("DPad")]
        public int DPad() => Receiver?.DPad ?? 5;
    }
}
