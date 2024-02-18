using System;
using System.Data;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GamepadPropAxisAnimator",
    Title = "Gamepad Prop Axis Animator",
    Category ="NODE_CATEGORY")]
    public class GamepadPropAxisAnimatorNode : Node {

        [FlowInput]
        public Continuation Enter() {
            BroadcastDataInput(nameof(Message));

            Message = null;
            if (Gamepad == null) return Exit;

            var animator = Gamepad.GameObject.GetComponent<Animator>();
            if (!animator) {
                Message = "Animator not found";
                return Exit;
            }

            var idxNeg = animator.GetLayerIndex(NegativeLayerId);
            if (idxNeg < 0) {
                Message = $"Negative layer name '{NegativeLayerId}' not found";
                return Exit;
            }

            var idxPos = animator.GetLayerIndex(PositiveLayerId);
            if (idxPos < 0) {
                Message = $"Positive layer name '{PositiveLayerId}' not found";
                return Exit;
            }

            animator.SetLayerWeight(idxNeg, Math.Max(0, AxisValue * -1f));
            animator.SetLayerWeight(idxPos, Math.Max(0, AxisValue));

            return Exit;
        }

        [FlowOutput]
        public Continuation Exit;
        [DataInput]
        public GameObjectAsset Gamepad;
        [DataInput]
        public string NegativeLayerId;
        [DataInput]
        public string PositiveLayerId;
        [DataInput]
        public float AxisValue;
        [Markdown]
        public string Message;
    }
}
