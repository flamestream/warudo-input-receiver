using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Plugins.Core.Assets;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GamepadPropControlPadAnimator",
    Title = "Gamepad Prop Control Pad Animator",
    Category ="NODE_CATEGORY")]
    public class GamepadControlPadAnimatorNode : Node {

        protected override void OnCreate() {
            previousIsActiveRegistry = new bool[8];
            targetWeightRegistry = new float[8];
            timeToAnimationPlayRegistry = new float[8];
            currentDampingVelocityRegistry = new float[8];
            targetDampingTimeRegistry = new float[8];
        }

        [FlowInput]
        public Continuation Enter() {
            BroadcastDataInput(nameof(Message));

            Message = null;
            if (Controller == null) return Exit;

            animator = Controller.GameObject.GetComponent<Animator>();
            if (animator == null) {
                Message = "Animator not found";
                return Exit;
            }

            processAnimation(0, D1LayerId, ControlPadState == 1);
            processAnimation(1, D2LayerId, ControlPadState == 2);
            processAnimation(2, D3LayerId, ControlPadState == 3);
            processAnimation(3, D4LayerId, ControlPadState == 4);
            processAnimation(4, D6LayerId, ControlPadState == 6);
            processAnimation(5, D7LayerId, ControlPadState == 7);
            processAnimation(6, D8LayerId, ControlPadState == 8);
            processAnimation(7, D9LayerId, ControlPadState == 9);

            return Exit;
        }

        void processAnimation(int idx, string animatorLayerId, bool isActive) {

            if (animatorLayerId == null) {
                return;
            }

            var layerIdx = animator.GetLayerIndex(animatorLayerId);
            if (layerIdx < 0) {
                Message = $"Layer name '{animatorLayerId}' not found";
                return;
            }

            if (isActive && !previousIsActiveRegistry[idx]) {

                timeToAnimationPlayRegistry[idx] = OnDelay;
                targetDampingTimeRegistry[idx] = OnDampingTime;
                targetWeightRegistry[idx] = 1;

            } else if (!isActive && previousIsActiveRegistry[idx]) {

                timeToAnimationPlayRegistry[idx] = OffDelay;
                targetDampingTimeRegistry[idx] = OffDampingTime;
                targetWeightRegistry[idx] = 0;

            } else if (timeToAnimationPlayRegistry[idx] > 0) {

                timeToAnimationPlayRegistry[idx] = Math.Max(0, timeToAnimationPlayRegistry[idx] - Time.deltaTime);
            }

            if (timeToAnimationPlayRegistry[idx] == 0) {
                var currentWeight = animator.GetLayerWeight(layerIdx);
                animator.SetLayerWeight(layerIdx, Mathf.SmoothDamp(currentWeight, targetWeightRegistry[idx], ref currentDampingVelocityRegistry[idx], targetDampingTimeRegistry[idx]));
                if (currentWeight == targetWeightRegistry[idx]) {
                    timeToAnimationPlayRegistry[idx] = -1;
                }
            }

            previousIsActiveRegistry[idx] = isActive;
        }

        Animator animator;
        bool[] previousIsActiveRegistry;
        float[] targetWeightRegistry;
        float[] timeToAnimationPlayRegistry;
        float[] currentDampingVelocityRegistry;
        float[] targetDampingTimeRegistry;

        [FlowOutput]
        public Continuation Exit;
        [DataInput]
        public GameObjectAsset Controller;
        [DataInput]
        public string D1LayerId = "d1";
        [DataInput]
        public string D2LayerId = "d2";
        [DataInput]
        public string D3LayerId = "d3";
        [DataInput]
        public string D4LayerId = "d4";
        [DataInput]
        public string D6LayerId = "d6";
        [DataInput]
        public string D7LayerId = "d7";
        [DataInput]
        public string D8LayerId = "d8";
        [DataInput]
        public string D9LayerId = "d9";
        [DataInput]
        public float OnDampingTime = 0.05f;
        [DataInput]
        public float OnDelay = 0f;
        [DataInput]
        public float OffDampingTime = 0.05f;
        [DataInput]
        public float OffDelay = 0f;
        [DataInput]
        public int ControlPadState;
        [Markdown]
        public string Message;
    }
}
