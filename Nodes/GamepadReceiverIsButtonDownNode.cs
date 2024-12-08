using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using static FlameStream.GamepadReceiverAsset;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.GamepadReceiverIsButtonDown",
    Title = "NODE_TITLE_GAMEPAD_RECEIVER_IS_BUTTON_DOWN",
    Category = "FS_NODE_CATEGORY_GAMEPAD"
)]
    public class GamepadReceiverIsButtonDownNode: Node {

        [DataInput]
        public GamepadReceiverAsset Receiver;

        [DataInput]
        public Button Button;

        [DataOutput]
	    public bool IsDown() => Receiver?.ButtonFlag((int)Button) ?? false;
    }
}
