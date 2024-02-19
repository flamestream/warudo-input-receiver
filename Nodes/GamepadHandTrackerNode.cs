using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GamepadHandTrackerNode",
    Title = "🔥🎮 Hand Tracker",
    Category = "NODE_CATEGORY")
]
    public class GamepadHandTrackerModderNode : Node {

        [DataInput]
        public GamepadReceiverAsset Receiver;

        [DataInput]
        public bool IsLeftHandTracked;

        [DataInput]
        public bool IsRightHandTracked;

        [DataInput]
        public float[] BoneRotationWeights;

        [DataOutput]
        public float[] OutputBoneRotationWeights() {

            var boneWeights = new float[55];

            for (var i = 0; i < 55; ++i) {
                if (i >= BoneRotationWeights.Length) break;
                boneWeights[i] = BoneRotationWeights[i];
                if (Receiver == null || !Receiver.Active || !Receiver.IsHandTrackerEnabled) {
                    continue;
                }
                var min = (int)HumanBodyBones.LeftThumbProximal;
                var max = (int)HumanBodyBones.LeftLittleDistal;
                var min2 = (int)HumanBodyBones.RightThumbProximal;
                var max2 = (int)HumanBodyBones.RightLittleDistal;
                if (i >= min && i <= max) {
                    boneWeights[i] *= LeftWeightFactor;
                } else if (i >= min2 && i <= max2) {
                    boneWeights[i] *= RightWeightFactor;
                }
            }

            return boneWeights;
        }

        [FlowInput]
        public Continuation Enter() {
            MainLoop();
            return Exit;
        }
        [FlowOutput]
        public Continuation Exit;

        void MainLoop() {
            if (Receiver == null) return;
            bool isLeftHandTracked = IsLeftHandTracked;
            bool isRightHandTracked = IsRightHandTracked;
            if (!Receiver.IsHandTrackerEnabled) {
                isLeftHandTracked = false;
                isRightHandTracked = false;
            };
            firstTracked = Receiver.ProcessHandTracking(isLeftHandTracked, isRightHandTracked);
        }

        // -1: Left
        //  0: None
        //  1: Right
        short firstTracked;

        float LeftWeightFactor {
            get {
                if (firstTracked < 0) return 1;
                return 0;
            }
        }

        float RightWeightFactor {
            get {
                if (firstTracked > 0) return 1;
                return 0;
            }
        }
    }
}
