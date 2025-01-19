using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using static FlameStream.KeyboardReceiverAsset;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.GetKeyboardReceiverData",
    Title = "NODE_TITLE_KEYBOARD_RECEIVER_DATA",
    Category = "FS_NODE_CATEGORY_KEYBOARD"
)]
    public class GetKeyboardReceiverKeyNode: Node {

        [DataInput]
        public KeyboardReceiverAsset Receiver;

        [DataOutput]
        [Label("IS_ACTIVE")]
        public bool IsActive() => Receiver != null && Receiver.Active;

        [DataOutput]
        [Label("IS_RECEIVING")]
        public bool IsReceiving() => Receiver != null && Receiver.IsReceiving;

        [DataOutput(100)]
        [Label("IS_ANY_KEY_DOWN")]
        public bool AnyDown() => Receiver != null && Receiver.AnyDown;

        [DataOutput(100)]
        [Label("LAST_KEY_CODE")]
        public int LastVkCode() => Receiver?.LastVkCode ?? -1;
    }
}
