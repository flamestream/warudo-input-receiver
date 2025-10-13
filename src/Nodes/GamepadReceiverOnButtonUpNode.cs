using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using static FlameStream.GamepadReceiverAsset;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.GamepadReceiverOnButtonUp",
    Title = "NODE_TITLE_GAMEPAD_RECEIVER_ON_BUTTON_UP",
    Category = "FS_NODE_CATEGORY_GAMEPAD"
)]
    public class GamepadReceiverOnButtonUpNode: Node {

        [DataInput]
        public GamepadReceiverAsset Receiver;

        [DataInput]
        public Button Button;

        [FlowOutput]
	    public Continuation Exit;

        public override void OnUpdate() {
            base.OnUpdate();

            if (Receiver == null) return;

            if (!Receiver.IsReceiving) return;

            if (!Receiver.DeactivatedButtonFlag((int)Button)) return;

            InvokeFlow(nameof(Exit));
        }
    }
}
