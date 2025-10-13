using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.InputReceiverOnAxisInactive",
    Title = "NODE_TITLE_INPUT_RECEIVER_ON_AXIS_INACTIVE",
    Category = "FS_NODE_CATEGORY_INPUT_RECEIVER"
)]
    public class InputReceiverOnAxisInactive: Node {

        [DataInput]
        public InputReceiverAsset Receiver;

        [DataInput]
        public int AxisIndex;

        [FlowOutput]
	    public Continuation OnInactive;

        [Markdown]
        public string ErrorMessage;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(AxisIndex), OnAxisIndexChange);
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (ErrorMessage != null) return;

            if (Receiver == null) return;

            if (!Receiver.IsReceiving) return;

            if (!Receiver.IsAxisJustInactive(AxisIndex)) return;

            InvokeFlow(nameof(OnInactive));
        }

        void OnAxisIndexChange() {
            if (AxisIndex < 0 || AxisIndex >= InputReceiverAsset.MAX_AXIS_COUNT) {
                ErrorMessage = "Index must be between 0 and " + (InputReceiverAsset.MAX_AXIS_COUNT - 1);
            } else {
                ErrorMessage = null;
            }
        }
    }
}
