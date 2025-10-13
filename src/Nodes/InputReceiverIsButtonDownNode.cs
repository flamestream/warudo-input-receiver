using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.InputReceiverIsButtonDown",
    Title = "NODE_TITLE_INPUT_RECEIVER_IS_BUTTON_DOWN",
    Category = "FS_NODE_CATEGORY_INPUT_RECEIVER"
)]
    public class InputReceiverIsButtonDownNode: Node {

        [DataInput]
        public InputReceiverAsset Receiver;

        [DataInput]
        public int ButtonIndex;

        [DataOutput]
	    public bool IsDown() => Receiver?.IsButtonDown(ButtonIndex) ?? false;

        [Markdown]
        public string ErrorMessage;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(ButtonIndex), OnButtonIndexChange);
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
