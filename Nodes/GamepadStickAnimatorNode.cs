using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream
{
    public abstract class GamepadStickAnimatorNode : Node {

        [FlowInput]
        [Label("ENTER")]
        public Continuation Enter() {
            BroadcastDataInput(nameof(Message));
            Message = null;
            ProcessAnimation();
            return Exit;
        }

        [FlowOutput]
        [Label("EXIT")]
        public Continuation Exit;
        [DataInput]
        [Label("NEGATIVE_LAYER_ID")]
        public string NegativeLayerId;
        [DataInput]
        [Label("POSITIVE_LAYER_ID")]
        public string PositiveLayerId;
        [DataInput]
        [Label("AXIS_VALUE")]
        public float AxisValue;
        [Markdown]
        [Label("MESSAGE")]
        public string Message;

        abstract protected void ProcessAnimation();
    }
}
