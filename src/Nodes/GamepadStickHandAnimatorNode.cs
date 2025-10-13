using System;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Utils;
using Warudo.Plugins.Core.Assets.Character;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GamepadStickHandAnimator",
    Title = "NODE_TITLE_GAMEPAD_STICK_HAND_ANIMATOR",
    Category = "FS_NODE_CATEGORY_GAMEPAD")]
    public class GamepadStickHandAnimatorNode : GamepadStickAnimatorNode {

        [DataInput]
        public CharacterAsset Character;

        CharacterAsset.OverlappingAnimationData GetOverlappingAnimationData(string layerId) {
            if (layerId.IsNullOrWhiteSpace()) return null;
            return Array.Find(Character.OverlappingAnimations, (a) => a.CustomLayerID == layerId);
        }

        override protected void ProcessAnimation() {
            if (Character == null) return;

            var negativeOverlappingAnimationData = GetOverlappingAnimationData(NegativeLayerId);
            if (negativeOverlappingAnimationData == null) {
                Message = $"Negative Layer ID '{NegativeLayerId}' does not exist";
                return;
            }

            var positiveOverlappingAnimationData = GetOverlappingAnimationData(PositiveLayerId);
            if (positiveOverlappingAnimationData == null) {
                Message = $"Positive Layer ID '{NegativeLayerId}' does not exist";
                return;
            }

            int negativeIdx = Array.IndexOf(Character.OverlappingAnimations, negativeOverlappingAnimationData) + 1;
            int positiveIdx = Array.IndexOf(Character.OverlappingAnimations, positiveOverlappingAnimationData) + 1;

            var animancer = Character.Animancer;
            var animancer2 = Character.CloneAnimancer;

            var negativeWeight = Math.Max(0, AxisValue * -1f);
            negativeOverlappingAnimationData.Weight = negativeWeight;
            negativeOverlappingAnimationData.BroadcastDataInput("Weight");
            animancer.Layers[negativeIdx].SetWeight(negativeWeight);
            animancer2.Layers[negativeIdx].SetWeight(negativeWeight);

            var positiveWeight = Math.Max(0, AxisValue);
            positiveOverlappingAnimationData.Weight = positiveWeight;
            positiveOverlappingAnimationData.BroadcastDataInput("Weight");
            animancer.Layers[positiveIdx].SetWeight(positiveWeight);
            animancer2.Layers[positiveIdx].SetWeight(positiveWeight);

            // negativeOverlappingAnimationData.SetDataInput(nameof(negativeOverlappingAnimationData.Weight),  Math.Max(0, AxisValue * -1f), true);
            // positiveOverlappingAnimationData.SetDataInput(nameof(positiveOverlappingAnimationData.Weight),  Math.Max(0, AxisValue), true);
        }
    }
}
