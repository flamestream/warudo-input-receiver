using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.InputReceiverOnSwitchInactive",
    Title = "NODE_TITLE_INPUT_RECEIVER_ON_SWITCH_INACTIVE",
    Category = "FS_NODE_CATEGORY_INPUT_RECEIVER"
)]
    public class InputReceiverOnSwitchInactiveNode: Node {

        [DataInput]
        public InputReceiverAsset Receiver;

        [DataInput]
        public int SwitchIndex;

        [FlowOutput]
	    public Continuation OnInactive;

        [Markdown]
        public string ErrorMessage;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(SwitchIndex), OnSwitchIndexChange);
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (ErrorMessage != null) return;

            if (Receiver == null) return;

            if (!Receiver.IsReceiving) return;

            if (!Receiver.IsSwitchJustInactive(SwitchIndex)) return;

            InvokeFlow(nameof(OnInactive));
        }

        void OnSwitchIndexChange() {
            if (Receiver == null) return;

            if (SwitchIndex < 0 || SwitchIndex >= InputReceiverAsset.MAX_SWITCH_COUNT) {
                ErrorMessage = $"Switch index out of range (Max: {InputReceiverAsset.MAX_SWITCH_COUNT - 1})";
            } else {
                ErrorMessage = null;
            }
            BroadcastDataInput(nameof(ErrorMessage));
        }
    }
}
