using System;
using System.Data;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GamepadPropButtonAnimator",
    Title = "Gamepad Prop Button Animator",
    Category ="NODE_CATEGORY")]
    public class GamepadPropButtonAnimatorNode : Node {

        [FlowInput]
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

                timeToAnimationPlay = OnDelay;
                targetDampingTime = OnDampingTime;
                targetWeight = 1;

            } else if (!IsPressed && previousIsActive) {

                timeToAnimationPlay = OffDelay;
                targetDampingTime = OffDampingTime;
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

        bool previousIsActive;
        float targetWeight;
        float timeToAnimationPlay;
        float currentDampingVelocity;
        float targetDampingTime;

        [FlowOutput]
        public Continuation Exit;
        [DataInput]
        public GameObjectAsset Controller;
        [DataInput]
        public string PressLayerId;
        [DataInput]
        public float OnDampingTime = 0.05f;
        [DataInput]
        public float OnDelay = 0.2f;
        [DataInput]
        public float OffDampingTime = 0.05f;
        [DataInput]
        public float OffDelay = 0f;
        [DataInput]
        public bool IsPressed;
        [Markdown]
        public string Message;
    }
}
