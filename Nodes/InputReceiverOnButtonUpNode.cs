using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.InputReceiverOnButtonUp",
    Title = "NODE_TITLE_INPUT_RECEIVER_ON_BUTTON_UP",
    Category = "FS_NODE_CATEGORY_INPUT_RECEIVER"
)]
    public class InputReceiverOnButtonUpNode: Node {

        [DataInput]
        public InputReceiverAsset Receiver;

        [DataInput]
        public int ButtonIndex;

        [FlowOutput]
	    public Continuation OnUp;

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

            if (!Receiver.IsButtonJustUp(ButtonIndex)) return;

            InvokeFlow(nameof(OnUp));
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
