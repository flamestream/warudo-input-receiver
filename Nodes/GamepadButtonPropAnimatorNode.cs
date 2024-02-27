using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Plugins.Core.Assets;

namespace FlameStream
{
    [NodeType(
    Id = "FlameStream.Node.GamepadButtonPropAnimatorNode",
    Title = "NODE_TITLE_GAMEPAD_BUTTON_PROP_ANIMATOR",
    Category ="NODE_CATEGORY")]
    public class GamepadButtonPropAnimatorNode : Node {

        [FlowInput]
        [Label("ENTER")]
        public Continuation Enter() {
            BroadcastDataInput(nameof(Message));

            Message = null;
            if (Controller == null) return Exit;

            var animator = Controller.GameObject.GetComponent<Animator>();
            if (!animator) {
                Message = "Animator not found";
                return Exit;
            }

            var idx = animator.GetLayerIndex(PressLayerId);
            if (idx < 0) {
                Message = $"Layer name '{PressLayerId}' not found";
                return Exit;
            }

            if (IsPressed && !previousIsActive) {

                timeToAnimationPlay = PressInDelayTime;
                targetDampingTime = PressInTransitionTime;
                targetWeight = 1;

            } else if (!IsPressed && previousIsActive) {

                timeToAnimationPlay = PressOutDelayTime;
                targetDampingTime = PressOutTransitionTime;
                targetWeight = 0;

            } else if (timeToAnimationPlay > 0) {

                timeToAnimationPlay = Math.Max(0, timeToAnimationPlay - Time.deltaTime);
            }

            if (timeToAnimationPlay == 0) {
                var currentWeight = animator.GetLayerWeight(idx);
                animator.SetLayerWeight(idx, Mathf.SmoothDamp(currentWeight, targetWeight, ref currentDampingVelocity, targetDampingTime));
                if (currentWeight == targetWeight) {
                    timeToAnimationPlay = -1;
                }
            }

            previousIsActive = IsPressed;
            return Exit;
        }

        [FlowOutput]
        [Label("EXIT")]
        public Continuation Exit;

        [DataInput]
        [Label("CONTROLLER")]
        public GameObjectAsset Controller;
        [DataInput]
        [Label("PRESS_LAYER_ID")]
        public string PressLayerId;
        [DataInput]
        [Label("PRESS_IN_TRANSITION_TIME")]
        public float PressInTransitionTime = 0.05f;
        [DataInput]
        [Label("PRESS_IN_DELAY_TIME")]
        public float PressInDelayTime = 0.2f;
        [DataInput]
        [Label("PRESS_OUT_TRANSITION_TIME")]
        public float PressOutTransitionTime = 0.05f;
        [DataInput]
        [Label("PRESS_OUT_DELAY_TIME")]
        public float PressOutDelayTime = 0f;
        [DataInput]
        [Label("IS_PRESSED")]
        public bool IsPressed;
        [Markdown]
        [Label("MESSAGE")]
        public string Message;

        bool previousIsActive;
        float targetWeight;
        float timeToAnimationPlay;
        float currentDampingVelocity;
        float targetDampingTime;
    }
}
