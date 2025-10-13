using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GetInputReceiverData",
    Title = "NODE_TITLE_INPUT_RECEIVER",
    Category = "FS_NODE_CATEGORY_INPUT_RECEIVER"
)]
    public class GetGameInputReceiverDataNode : Node {

        [DataInput]
        public InputReceiverAsset Receiver;

        [DataOutput]
        [Label("IS_ACTIVE")]
        public bool IsActive() => Receiver != null && Receiver.Active;

        [DataOutput]
        [Label("IS_RECEIVING")]
        public bool IsReceiving() => Receiver != null && Receiver.IsReceiving;
    }
}
