using System;
using DG.Tweening;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Utils;
using Warudo.Plugins.Core.Assets.Character;

namespace FlameStream {

    public abstract class GamepadFingerAnimatorNode : Node {

        [DataOutput]
        public string _HoverInputId() => HoverInputId;

        [DataInput(0)]
        public CharacterAsset Character;

        [DataInput(0)]
        public string InputId;

        [DataInput(100)]
        public float HoverTransitionTime = 0f;
        [DataInput(100)]
        public Ease HoverTransitionEasing = Ease.OutCubic;
        [DataInput(100)]
        public float PressInTransitionTime = 0.5f;
        [DataInput(200)]
        public Ease PressInTransitionEasing = Ease.OutCubic;
        [DataInput(200)]
        public float PressOutTransitionTime = 0.5f;
        [DataInput(200)]
        public Ease PressOutTransitionEasing = Ease.OutCubic;

        [DataInput(500)]
        [Description("From receiver Hover Input ID")]
        public string HoverInputId;

        [Markdown(1000)]
        public string Message;

        [FlowInput]
        public Continuation Enter() {

            Message = "";

            if (Character == null) {
                return Exit;
            }

            MainLoop();
            AfterMainLoop();

            BroadcastDataInput(nameof(Message));

            return Exit;
        }

        [FlowOutput]
        public Continuation Exit;

        protected Tween pressTween;
        protected Tween pressTweenClone;
        protected Tween hoverTween;
        protected Tween hoverTweenClone;

        protected Animancer.AnimancerComponent CharacterAnimancer {
            get {
                return Character?.Animancer;
            }
        }

        protected Animancer.AnimancerComponent CharacterAnimancerClone {
            get {
                return Character?.CloneAnimancer;
            }
        }

        protected CharacterAsset.OverlappingAnimationData GetOverlappingAnimationData(string layerId) {
            if (layerId.IsNullOrWhiteSpace()) return null;
            return Array.Find(Character.OverlappingAnimations, (a) => a.CustomLayerID == layerId);
        }

        protected bool IsHovering {
            get {
                return InputId == HoverInputId;
            }
        }

        protected void ProcessAnimation(string hoverLayerId, string pressLayerId, bool isHovering, bool isPressed) {

            var pressOverlappingAnimationData = GetOverlappingAnimationData(pressLayerId);
            if (pressOverlappingAnimationData == null) {
                Message = $"Press Layer ID '{pressLayerId}' does not exist\n\n";
                return;
            }
            var hoverOverlappingAnimationData = GetOverlappingAnimationData(hoverLayerId);
            if (hoverOverlappingAnimationData == null) {
                Message = $"Hover Layer ID '{hoverLayerId}' does not exist\n\n";
                return;
            }

            int pressIndex = 1 + Array.IndexOf(Character.OverlappingAnimations, pressOverlappingAnimationData);
            int hoverIndex = 1 + Array.IndexOf(Character.OverlappingAnimations, hoverOverlappingAnimationData);

            // Reset animation
            // Do it only if not on dpad or if same button
            if (IsActivationFrame && isPressed) {
                ResetAnimation(pressIndex);
                pressTween?.Kill(true);
                pressTweenClone?.Kill(true);
            }

            var hoverWeight = isHovering ? 1f : 0f;
            if (hoverOverlappingAnimationData.Weight != hoverWeight) {
                hoverOverlappingAnimationData.Weight = hoverWeight;
                hoverOverlappingAnimationData.BroadcastDataInput("Weight");
                ApplyAnimancerPropertyWeight(ref hoverTween, ref hoverTweenClone, hoverIndex, hoverWeight, HoverTransitionTime, HoverTransitionEasing);
            }

            var pressWeight = 0f;
            var pressTransitionTime = PressOutTransitionTime;
            var pressTransitionEasing = PressOutTransitionEasing;
            if (isPressed) {
                pressWeight = 1f;
                pressTransitionTime = PressInTransitionTime;
                pressTransitionEasing = PressInTransitionEasing;
            }
            if (pressOverlappingAnimationData.Weight != pressWeight) {
                pressOverlappingAnimationData.Weight = pressWeight;
                pressOverlappingAnimationData.BroadcastDataInput("Weight");
                ApplyAnimancerPropertyWeight(ref pressTween, ref pressTweenClone, pressIndex, pressWeight, pressTransitionTime, pressTransitionEasing);
            }
        }

        protected void ResetAnimation(int idx) {

            ResetAnimationLow(CharacterAnimancer, idx);
            ResetAnimationLow(CharacterAnimancerClone, idx);
        }

        void ResetAnimationLow(Animancer.AnimancerComponent animancer, int idx) {

            var layer = animancer.Layers[idx];
            layer.Play(layer.CurrentState).Time = 0;
        }

        protected void ApplyAnimancerPropertyWeight(
            ref Tween tween,
            ref Tween tween2,
            int idx,
            float weight,
            float transitionTime,
            Ease transitionEasing
        ) {
            ApplyAnimancerPropertyWeightLow(ref tween, CharacterAnimancer, idx, weight, transitionTime, transitionEasing);
            ApplyAnimancerPropertyWeightLow(ref tween2, CharacterAnimancerClone, idx, weight, transitionTime, transitionEasing);
        }

        void ApplyAnimancerPropertyWeightLow(
            ref Tween tween,
            Animancer.AnimancerComponent animancer,
            int idx,
            float weight,
            float transitionTime,
            Ease transitionEasing
        ) {
            if (transitionTime > 0f) {
                tween = DOTween.To(
                    () => animancer.Layers[idx].Weight,
                    delegate(float it) { animancer.Layers[idx].SetWeight(it); },
                    weight,
                    transitionTime
                ).SetEase(transitionEasing);
            } else {
                animancer.Layers[idx].SetWeight(weight);
            }
        }

        protected abstract bool IsActivationFrame {
            get;
        }
        protected abstract void MainLoop();
        protected virtual void AfterMainLoop() {}
    }
}
