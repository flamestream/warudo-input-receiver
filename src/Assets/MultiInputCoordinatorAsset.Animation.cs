using System;
using System.Linq;
using UnityEngine;
using Warudo.Core.Data;
using Warudo.Plugins.Core.Assets.Character;

namespace FlameStream {
    public partial class MultiInputCoordinatorAsset {

        void OnCreateAnimation() {
        }

        void OnDestroyAnimation() {
        }

        void OnUpdateAnimation() {
            if (!IsEnabled || Character == null) return;

            // Synchronize prop visibility
            UpdatePropVisibility();
        }

        void OnTransitionCompleted(bool isLeft, CoordinatorHandState targetState) {
            UpdateFingerAnimationLayers();
            UpdatePropVisibility();
        }

        void UpdatePropVisibility() {
            bool isPenActive = (currentLeftState == CoordinatorHandState.Pen && PenReceiver != null && !PenReceiver.IsRightHanded)
                            || (currentRightState == CoordinatorHandState.Pen && PenReceiver != null && PenReceiver.IsRightHanded);

            if (PenReceiver != null && PenReceiver.Prop != null) {
                var prop = PenReceiver.Prop;
                bool shouldEnable = isPenActive && PenReceiver.IsPropEnabled && PenReceiver.IsEnabled;
                if (prop.Enabled != shouldEnable) {
                    prop.SetDataInput(nameof(prop.Enabled), shouldEnable, true);
                    prop.Broadcast();
                }
            }

            bool isMouseActive = (currentLeftState == CoordinatorHandState.Mouse && MouseReceiver != null && !MouseReceiver.IsRightHanded)
                              || (currentRightState == CoordinatorHandState.Mouse && MouseReceiver != null && MouseReceiver.IsRightHanded);

            if (MouseReceiver != null && MouseReceiver.Prop != null) {
                var prop = MouseReceiver.Prop;
                bool shouldEnable = isMouseActive && MouseReceiver.IsPropEnabled && MouseReceiver.IsEnabled;
                if (prop.Enabled != shouldEnable) {
                    prop.SetDataInput(nameof(prop.Enabled), shouldEnable, true);
                    prop.Broadcast();
                }
            }
        }

        void UpdateFingerAnimationLayers() {
            if (Character == null || Character.OverlappingAnimations == null) return;

            bool isLeftKeyboard = currentLeftState == CoordinatorHandState.Keyboard;
            bool isRightKeyboard = currentRightState == CoordinatorHandState.Keyboard;

            bool isLeftMouse = currentLeftState == CoordinatorHandState.Mouse;
            bool isRightMouse = currentRightState == CoordinatorHandState.Mouse;

            bool isLeftPen = currentLeftState == CoordinatorHandState.Pen;
            bool isRightPen = currentRightState == CoordinatorHandState.Pen;

            // Adjust weights of overlapping animation layers on Character
            foreach (var layer in Character.OverlappingAnimations) {
                if (string.IsNullOrEmpty(layer.CustomLayerID)) continue;

                // 1. Keyboard Layers (Prefix: 🔥⌨️)
                if (layer.CustomLayerID.StartsWith("🔥⌨️")) {
                    bool isRightFingerLayer = layer.CustomLayerID.Contains("Right");
                    bool isLeftFingerLayer = layer.CustomLayerID.Contains("Left");

                    if (isRightFingerLayer) {
                        layer.Weight = isRightKeyboard ? 1f : 0f;
                    } else if (isLeftFingerLayer) {
                        layer.Weight = isLeftKeyboard ? 1f : 0f;
                    } else if (layer.CustomLayerID.Contains("Base Hand Pose")) {
                        // Base hand pose layer: keep active if either hand is on keyboard
                        layer.Weight = (isLeftKeyboard || isRightKeyboard) ? 1f : 0f;
                    }
                }

                // 2. Mouse Layers (Prefix: 🔥🖱️)
                if (MouseReceiver != null && layer.CustomLayerID.StartsWith("🔥🖱️") && layer.CustomLayerID.Contains("Mouse")) {
                    bool mouseHand = MouseReceiver.IsRightHanded ? isRightMouse : isLeftMouse;
                    layer.Weight = mouseHand ? 1f : 0f;
                }

                // 3. Pen Layers (Prefix: 🔥🖱️)
                if (PenReceiver != null && layer.CustomLayerID.StartsWith("🔥🖱️") && layer.CustomLayerID.Contains("Pen")) {
                    bool penHand = PenReceiver.IsRightHanded ? isRightPen : isLeftPen;
                    layer.Weight = penHand ? 1f : 0f;
                }
            }
        }
    }
}
