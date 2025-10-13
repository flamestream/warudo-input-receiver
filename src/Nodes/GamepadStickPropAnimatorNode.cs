using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Plugins.Core.Assets;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.GamepadStickPropAnimator",
    Title = "NODE_TITLE_GAMEPAD_STICK_PROP_ANIMATOR",
    Category = "FS_NODE_CATEGORY_GAMEPAD")]
    public class GamepadStickPropAnimatorNode : GamepadStickAnimatorNode {

        [DataInput]
        [Label("CONTROLLER")]
        public GameObjectAsset Controller;

        override protected void ProcessAnimation() {
            if (Controller == null) return;

            var animator = Controller.GameObject.GetComponent<Animator>();
            if (!animator) {
                Message = "Animator not found";
                return;
            }

            var idxNeg = animator.GetLayerIndex(NegativeLayerId);
            if (idxNeg < 0) {
                Message = $"Negative layer name '{NegativeLayerId}' not found";
                return;
            }

            var idxPos = animator.GetLayerIndex(PositiveLayerId);
            if (idxPos < 0) {
                Message = $"Positive layer name '{PositiveLayerId}' not found";
                return;
            }

            animator.SetLayerWeight(idxNeg, Math.Max(0, AxisValue * -1f));
            animator.SetLayerWeight(idxPos, Math.Max(0, AxisValue));
        }
    }
}
