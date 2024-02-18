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
        [Label("NODE_IS_ACTIVE")]
        public bool IsActive() => Receiver != null && Receiver.Active;

        [DataOutput]
        [Label("NODE_IS_RECEIVING")]
        public bool IsReceiving() => Receiver != null && Receiver.IsReceiving;

        [DataOutput]
        public int X() => Receiver == null ? 0 : Receiver.X;

        [DataOutput]
        public int Y() => Receiver == null ? 0 : Receiver.Y;

        [DataOutput]
        [Label("NODE_POINTER_SOURCE")]
        public int Source() => Receiver == null ? 0 : Receiver.Source;

        [DataOutput]
        [Label("NODE_POINTER_BUTTON_1")]
        public bool Button1() => Receiver != null && Receiver.Button1;

        [DataOutput]
        [Label("NODE_POINTER_BUTTON_2")]
        public bool Button2() => Receiver != null && Receiver.Button2;
    }
}
