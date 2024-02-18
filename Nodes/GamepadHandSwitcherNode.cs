using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GamepadHandSwitcher",
    Title = "🔥🎮 Hand Switcher",
    Category = "NODE_CATEGORY")
]
    public class GamepadHandSwitcherNode : Node {

        [DataInput]
        public GamepadReceiverAsset Receiver;
        [DataInput]
        public bool IsLeftHandTracked;
        [DataInput]
        public bool IsRightHandTracked;

        [FlowInput]
        public Continuation Enter() {
            MainLoop();
            return Exit;
        }
        [FlowOutput]
        public Continuation Exit;

        bool LastIsLeftHandTracked;
        bool LastIsRightHandTracked;
        // -1: Left
        //  0: None
        //  1: Right
        short firstTracked;

        void MainLoop() {
            if (Receiver == null) return;
            Receiver.ProcessHandTracking(IsLeftHandTracked, IsRightHandTracked);
        }
    }
}
