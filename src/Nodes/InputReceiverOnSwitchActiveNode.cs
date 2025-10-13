using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.InputReceiverOnSwitchActive",
    Title = "NODE_TITLE_INPUT_RECEIVER_ON_SWITCH_ACTIVE",
    Category = "FS_NODE_CATEGORY_INPUT_RECEIVER"
)]
    public class InputReceiverOnSwitchActiveNode: Node {

        [DataInput]
        public InputReceiverAsset Receiver;

        [DataInput]
        public int SwitchIndex;

        [DataOutput]
        public int Value() {

            if (Receiver == null) return 0;

            if (SwitchIndex < 0 || SwitchIndex >= InputReceiverAsset.MAX_SWITCH_COUNT) return 0;

            return Receiver.GetSwitchSubIndex(SwitchIndex);
        }

        [FlowOutput]
	    public Continuation OnActive;

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

            if (!Receiver.IsSwitchJustActive(SwitchIndex)) return;

            InvokeFlow(nameof(OnActive));
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
