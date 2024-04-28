using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GetPointerReceiverDataNode",
    Title = "NODE_TITLE_POINTER",
    Category = "NODE_CATEGORY"
)]
    public class GetPointerReceiverDataNode : Node {

        [DataInput]
        public PointerReceiverAsset Receiver;

        [DataOutput]
        [Label("IS_ACTIVE")]
        public bool IsActive() => Receiver != null && Receiver.Active;

        [DataOutput]
        [Label("IS_RECEIVING")]
        public bool IsReceiving() => Receiver != null && Receiver.IsReceiving;

        [DataOutput]
        public int X() => Receiver == null ? 0 : Receiver.X;

        [DataOutput]
        public int Y() => Receiver == null ? 0 : Receiver.Y;

        [DataOutput]
        public int AdjustedX() => Receiver == null ? 0 : Receiver.adjustedX;

        [DataOutput]
        public int AdjustedY() => Receiver == null ? 0 : Receiver.adjustedY;

        [DataOutput]
        public bool IsOutOfBound() => Receiver != null && Receiver.isOutOfBound;

        [DataOutput]
        [Label("POINTER_SOURCE")]
        public int Source() => Receiver == null ? 0 : Receiver.Source;

        [DataOutput]
        [Label("POINTER_BUTTON_1")]
        public bool Button1() => Receiver != null && Receiver.Button1;

        [DataOutput]
        [Label("POINTER_BUTTON_2")]
        public bool Button2() => Receiver != null && Receiver.Button2;

        [DataOutput]
        [Label("HAND_STATE")]
        public string HandState() => Receiver?.handState?.Label;

        [DataOutput]
        [Label("BODY_STATE")]
        public string BodyState() => Receiver?.bodyState?.Label;

        [DataOutput]
        [Label("PROP_STATE")]
        public string PropState() => Receiver?.propState?.Label;
    }
}
