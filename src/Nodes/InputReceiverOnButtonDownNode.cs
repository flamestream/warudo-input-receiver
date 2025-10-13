using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.InputReceiverOnButtonDown",
    Title = "NODE_TITLE_INPUT_RECEIVER_ON_BUTTON_DOWN",
    Category = "FS_NODE_CATEGORY_INPUT_RECEIVER"
)]
    public class InputReceiverOnButtonDownNode: Node {

        [DataInput]
        public InputReceiverAsset Receiver;

        [DataInput]
        public int ButtonIndex;

        [FlowOutput]
	    public Continuation OnDown;

        [Markdown]
        public string ErrorMessage;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(ButtonIndex), OnButtonIndexChange);
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (ErrorMessage != null) return;

            if (Receiver == null) return;

            if (!Receiver.IsReceiving) return;

            if (!Receiver.IsButtonJustDown(ButtonIndex)) return;

            InvokeFlow(nameof(OnDown));
        }

        void OnButtonIndexChange() {
            if (ButtonIndex < 0 || ButtonIndex >= InputReceiverAsset.MAX_BUTTON_COUNT) {
                ErrorMessage = "Index must be between 0 and " + (InputReceiverAsset.MAX_BUTTON_COUNT - 1);
            } else {
                ErrorMessage = null;
            }
        }
    }
}
