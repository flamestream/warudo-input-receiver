using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Plugins.Core.Assets;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GamepadDPadPropAnimator",
    Title = "NODE_TITLE_GAMEPAD_DPAD_PROP_ANIMATOR",
    Category = "FS_NODE_CATEGORY_GAMEPAD")]
    public class GamepadDPadPropAnimatorNode : Node {

        protected override void OnCreate() {
            previousIsActiveRegistry = new bool[8];
            targetWeightRegistry = new float[8];
            timeToAnimationPlayRegistry = new float[8];
            currentDampingVelocityRegistry = new float[8];
            targetDampingTimeRegistry = new float[8];
        }

        [FlowInput]
        [Label("ENTER")]
        public Continuation Enter() {
            BroadcastDataInput(nameof(Message));

            Message = null;
            if (Controller == null) return Exit;

            animator = Controller.GameObject.GetComponent<Animator>();
            if (animator == null) {
                Message = "Animator not found";
                return Exit;
            }

            processAnimation(0, D1LayerId, DPad == 1);
            processAnimation(1, D2LayerId, DPad == 2);
            processAnimation(2, D3LayerId, DPad == 3);
            processAnimation(3, D4LayerId, DPad == 4);
            processAnimation(4, D6LayerId, DPad == 6);
            processAnimation(5, D7LayerId, DPad == 7);
            processAnimation(6, D8LayerId, DPad == 8);
            processAnimation(7, D9LayerId, DPad == 9);

            return Exit;
        }


        [FlowOutput]
        [Label("EXIT")]
        public Continuation Exit;
        [DataInput]
        [Label("CONTROLLER")]
        public GameObjectAsset Controller;
        [DataInput]
        [Label("D1_PRESS_LAYER_ID")]
        public string D1LayerId = "D1";
        [DataInput]
        [Label("D2_PRESS_LAYER_ID")]
        public string D2LayerId = "D2";
        [DataInput]
        [Label("D3_PRESS_LAYER_ID")]
        public string D3LayerId = "D3";
        [DataInput]
        [Label("D4_PRESS_LAYER_ID")]
        public string D4LayerId = "D4";
        [DataInput]
        [Label("D6_PRESS_LAYER_ID")]
        public string D6LayerId = "D6";
        [DataInput]
        [Label("D7_PRESS_LAYER_ID")]
        public string D7LayerId = "D7";
        [DataInput]
        [Label("D8_PRESS_LAYER_ID")]
        public string D8LayerId = "D8";
        [DataInput]
        [Label("D9_PRESS_LAYER_ID")]
        public string D9LayerId = "D9";
        [DataInput]
        [Label("PRESS_IN_TRANSITION_TIME")]
        public float PressInTransitionTime = 0.05f;
        [DataInput]
        [Label("PRESS_IN_DELAY_TIME")]
        public float PressInDelayTime = 0f;
        [DataInput]
        [Label("PRESS_OUT_TRANSITION_TIME")]
        public float PressOutTransitionTime = 0.05f;
        [DataInput]
        [Label("PRESS_OUT_DELAY_TIME")]
        public float PressOutDelayTime = 0f;
        [DataInput]
        [Label("DPAD")]
        public int DPad;
        [Markdown]
        [Label("MESSAGE")]
        public string Message;

        Animator animator;
        bool[] previousIsActiveRegistry;
        float[] targetWeightRegistry;
        float[] timeToAnimationPlayRegistry;
        float[] currentDampingVelocityRegistry;
        float[] targetDampingTimeRegistry;

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

                timeToAnimationPlayRegistry[idx] = PressInDelayTime;
                targetDampingTimeRegistry[idx] = PressInTransitionTime;
                targetWeightRegistry[idx] = 1;

            } else if (!isActive && previousIsActiveRegistry[idx]) {

                timeToAnimationPlayRegistry[idx] = PressOutDelayTime;
                targetDampingTimeRegistry[idx] = PressOutTransitionTime;
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
    }
}
