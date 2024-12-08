using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using static FlameStream.KeyboardReceiverAsset;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.KeyboardReceiverIsKeyDown",
    Title = "NODE_TITLE_KEYBOARD_RECEIVER_IS_KEY_DOWN",
    Category = "FS_NODE_CATEGORY_KEYBOARD"
)]
    public class KeyboardReceiverIsKeyDownNode: Node {

        [DataInput]
        public KeyboardReceiverAsset Receiver;

        [DataInput]
        public VKCode KeyCode;

        [DataOutput]
	    public bool IsDown() => Receiver?.Down((int)KeyCode) ?? false;
    }
}
