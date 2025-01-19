using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using static FlameStream.KeyboardReceiverAsset;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.KeyboardReceiverOnKeyUp",
    Title = "NODE_TITLE_KEYBOARD_RECEIVER_ON_KEY_UP",
    Category = "FS_NODE_CATEGORY_KEYBOARD"
)]
    public class KeyboardReceiverOnKeyUpNode: Node {

        [DataInput]
        public KeyboardReceiverAsset Receiver;

        [DataInput]
        public VKCode KeyCode;

        [FlowOutput]
	    public Continuation Exit;

        public override void OnUpdate() {
            base.OnUpdate();

            if (Receiver == null) return;

            if (!Receiver.IsReceiving) return;

            if (!Receiver.Deactivated((int)KeyCode)) return;

            InvokeFlow(nameof(Exit));
        }
    }
}
