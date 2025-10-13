using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.InputReceiverOnLastInputAtLayerChange",
    Title = "NODE_TITLE_INPUT_RECEIVER_ON_LAST_INPUT_AT_LAYER_CHANGE",
    Category = "FS_NODE_CATEGORY_INPUT_RECEIVER"
)]
    public class InputReceiverOnLastInputAtLayerChangeNode: Node {

        [DataInput]
        public InputReceiverAsset Receiver;

        [DataInput]
        public InputReceiverAsset.Layer AssignedLayer;

        [DataOutput]
        public string Value() {

            if (Receiver == null) return null;

            Receiver.NamespacedGlobalLayerJustDownHistoryRegistry[0].TryGetValue(AssignedLayer, out var definition);

            return definition?.GlobalLayerId;
        }

        [FlowOutput]
	    public Continuation OnChange;

        public override void OnUpdate() {
            base.OnUpdate();

            if (Receiver == null) return;

            if (!Receiver.IsReceiving) return;

            if (!Receiver.NamespacedGlobalLayerJustDownChangedRegistry.TryGetValue(AssignedLayer, out var changed) || !changed) return;

            InvokeFlow(nameof(OnChange));
        }
    }
}
