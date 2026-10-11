using System;
using System.Linq;
using System.Text;
using UnityEngine;
using Warudo.Core.Localization;

namespace FlameStream {
    public partial class MultiInputCoordinatorAsset {

        void OnCreateAnimation() {
        }

        void OnDestroyAnimation() {
        }

        void OnUpdateAnimation() {
            if (IsEnabled && EnableHands && Character != null && KeyboardReceiver != null && KeyboardReceiver.ActiveCoordinator == this) {
                KeyboardReceiver.SetBaseHandPoseWeight(1f);
            }
        }

        void OnTransitionCompleted(bool isLeft, CoordinatorHandState targetState) {
        }

        string GetAnimationLayersOverviewMessage() {
            if (Character == null) {
                return $"**{"ANIMATION_LAYERS_OVERVIEW".Localized()}:**\n- {"ANIMATION_LAYERS_NO_CHAR".Localized()}";
            }
            if (Character.OverlappingAnimations == null || Character.OverlappingAnimations.Length == 0) {
                return $"**{"ANIMATION_LAYERS_OVERVIEW".Localized()}:**\n- {"ANIMATION_LAYERS_NONE".Localized()}";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"**{"ANIMATION_LAYERS_OVERVIEW".Localized()}:**\n");
            sb.AppendLine("| Device        | Layer            | Hand           | Weight       | Status |");
            sb.AppendLine("| :---          | :---             | :---           | :---         | :---:  |");

            var activeLayers = Character.OverlappingAnimations.Where(a => a != null && a.Enabled && a.GetAnimationClip() != null).ToList();

            bool foundAny = false;
            foreach (var layer in Character.OverlappingAnimations) {
                if (layer == null) continue;
                string layerId = layer.CustomLayerID ?? "";
                if (!layerId.StartsWith("🔥")) continue;

                foundAny = true;
                string device = "🎮 Other";
                if (layerId.StartsWith("🔥⌨️")) {
                    device = "⌨️ Keyboard";
                } else if (MouseReceiver != null && layerId.StartsWith(MouseReceiver.LayerPrefix)) {
                    device = "🖱️ Mouse";
                } else if (PenReceiver != null && layerId.StartsWith(PenReceiver.LayerPrefix)) {
                    device = "🖊️ Pen";
                } else if (layerId.StartsWith("🔥🖱️")) {
                    device = "🖱️ Mouse";
                } else if (layerId.StartsWith("🔥🖊️")) {
                    device = "🖊️ Pen";
                }

                string layerName = layerId;
                if (layerName.StartsWith("🔥⌨️") || layerName.StartsWith("🔥🖱️") || layerName.StartsWith("🔥🖊️") || layerName.StartsWith("🔥🎮")) {
                    layerName = layerName.Substring(4).Trim();
                } else if (layerName.StartsWith("🔥")) {
                    layerName = layerName.Substring(2).Trim();
                }
                if (layerName.StartsWith("-")) {
                    layerName = layerName.Substring(1).Trim();
                }

                if (layerName.Length > 15) {
                    layerName = layerName.Substring(0, 14) + "…";
                }

                string hand = "Full Body";
                if (layer.Masked && layer.MaskedBodyParts != null && layer.MaskedBodyParts.Length > 0) {
                    bool hasLeft = layer.MaskedBodyParts.Any(p => p.ToString().IndexOf("Left", StringComparison.OrdinalIgnoreCase) >= 0);
                    bool hasRight = layer.MaskedBodyParts.Any(p => p.ToString().IndexOf("Right", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (hasLeft && hasRight) hand = "Left & Right";
                    else if (hasRight) hand = "Right";
                    else if (hasLeft) hand = "Left";
                    else hand = "Masked";
                }

                float targetWt = layer.Weight;
                float liveWt = targetWt;
                int animancerIdx = activeLayers.IndexOf(layer);
                if (animancerIdx >= 0 && Character.Animancer != null && Character.Animancer.Layers != null) {
                    int realIdx = 1 + animancerIdx;
                    if (realIdx >= 0 && realIdx < Character.Animancer.Layers.Count) {
                        var l = Character.Animancer.Layers[realIdx];
                        if (l != null) {
                            liveWt = l.Weight;
                        }
                    }
                }

                string weightStr;
                if (string.IsNullOrEmpty(layer.Animation)) {
                    weightStr = "-";
                } else if (Mathf.Abs(liveWt - targetWt) > 0.02f) {
                    weightStr = $"{liveWt:F2} ➔ {targetWt:F2}";
                } else {
                    weightStr = $"{liveWt:F2}";
                }

                string status = (!string.IsNullOrEmpty(layer.Animation) && liveWt > 0.01f) ? "🟢" : "⚪";

                sb.AppendLine($"| {device.PadRight(13)} | {layerName.PadRight(16)} | {hand.PadRight(14)} | {weightStr.PadRight(12)} |   {status}   |");
            }

            if (!foundAny) {
                return $"**{"ANIMATION_LAYERS_OVERVIEW".Localized()}:**\n- {"ANIMATION_LAYERS_NONE".Localized()}";
            }

            return sb.ToString().TrimEnd();
        }
    }
}
