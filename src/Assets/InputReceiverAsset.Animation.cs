using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Plugins.Core.Assets.Character;
using WebSocketSharp;

namespace FlameStream
{
    public partial class InputReceiverAsset : ReceiverAsset {

        public Dictionary<Layer, bool> NamespacedGlobalLayerJustDownChangedRegistry = new Dictionary<Layer, bool>();
        public Dictionary<Layer, SignalDefinition>[] NamespacedGlobalLayerJustDownHistoryRegistry = new Dictionary<Layer, SignalDefinition>[3];
        public Dictionary<AxisGroup, bool> HandledAxisGroupRegistry = new Dictionary<AxisGroup, bool>();

        void OnCreateAnimation() {
            for (var i = 0; i < NamespacedGlobalLayerJustDownHistoryRegistry.Length; ++i) {
                NamespacedGlobalLayerJustDownHistoryRegistry[i] = new Dictionary<Layer, SignalDefinition>();
            }
        }

        void OnUpdateAnimation() {

            NamespacedGlobalLayerJustDownChangedRegistry.Clear();
            HandledAxisGroupRegistry.Clear();
            foreach (var currentSignal in SignalDefinition.Registry.Values) {

                if (currentSignal.IsAnimationTriggerFrame) {
                    // Process layer changes
                    var newLayerSignalCandidate = currentSignal;
                    var currentLayer = currentSignal.AssignedCharacterLayer;
                    NamespacedGlobalLayerJustDownHistoryRegistry[0].TryGetValue(currentLayer, out var justDownSignal);
                    if (currentSignal is AxisDefinition currentAxisSignal && currentAxisSignal.AssignedGroup != AxisGroup.Solo) {
                        // TODO: Optimize?
                        newLayerSignalCandidate = AxisDefinitions.First((ad) => ad.AssignedGroup == currentAxisSignal.AssignedGroup);
                    }

                    if (newLayerSignalCandidate != justDownSignal) {
                        NamespacedGlobalLayerJustDownChangedRegistry[currentLayer] = true;
                        NamespacedGlobalLayerJustDownHistoryRegistry[1].TryGetValue(currentLayer, out var lastSignalDefinition);
                        NamespacedGlobalLayerJustDownHistoryRegistry[2][currentLayer] = lastSignalDefinition;
                        NamespacedGlobalLayerJustDownHistoryRegistry[1][currentLayer] = justDownSignal;
                        NamespacedGlobalLayerJustDownHistoryRegistry[0][currentLayer] = newLayerSignalCandidate;
                    }

                    // Process active animation
                    AnimationData activeAnimationData = null;
                    Transition activeTransition = currentSignal.GetCharacterActiveTransition();
                    bool setAnimationtoEnd = false;
                    float targetWeight = 1f;
                    switch (currentSignal) {
                        case ButtonDefinition buttonDefinition: {
                            activeAnimationData = buttonDefinition.GetCharacterActiveAnimationData();
                            break;
                        }

                        case SwitchDefinition switchDefinition: {
                            var activeSubSwitchIndex = GetSwitchLastChangeSubIndex(switchDefinition.Index, 0);
                            // There is no base active animation for switch, so defer genetric animation logic to subswitch data
                            activeAnimationData = switchDefinition.GetCharacterActiveSubAnimationData(activeSubSwitchIndex);
                            activeTransition = switchDefinition.GetCharacterActiveTransition();

                            var inactiveSubSwitchIndex = GetSwitchLastChangeSubIndex(switchDefinition.Index, 1);
                            setAnimationtoEnd = inactiveSubSwitchIndex != 0;

                            var subHoverAnimationData = switchDefinition.GetCharacterHoverSubAnimationData(activeSubSwitchIndex);
                            var subHoverTransition = switchDefinition.GetCharacterHoverTransition();
                            // Subswitch hover/unhover only if hover animation is valid
                            if (subHoverAnimationData?.IsValidOverlappingAnimationData(Character) == true) {

                                // Hover over subswitch
                                ProcessAnimation(subHoverAnimationData, subHoverTransition, 1f);

                                // De-activate/Unhover previous sub switch
                                var inactiveSubActiveAnimation = switchDefinition.GetCharacterActiveSubAnimationData(inactiveSubSwitchIndex);
                                var inactiveSubHoverAnimation = switchDefinition.GetCharacterHoverSubAnimationData(inactiveSubSwitchIndex);
                                ProcessAnimation(inactiveSubActiveAnimation, activeTransition, 0f);
                                ProcessAnimation(inactiveSubHoverAnimation, subHoverTransition, 0f);


                                // Switch keeps on hovering over non-0 subswitches, so we need to unhover them too
                                var inactiveSubSwitchIndex2 = GetSwitchLastChangeSubIndex(switchDefinition.Index, 2);
                                if (inactiveSubSwitchIndex2 != activeSubSwitchIndex) {
                                    var inactiveSubHoverAnimationData2 = switchDefinition.GetCharacterHoverSubAnimationData(inactiveSubSwitchIndex2);
                                    ProcessAnimation(inactiveSubHoverAnimationData2, subHoverTransition, 0f);
                                }
                            }
                            ProcessPropAnimation(switchDefinition.GetPropSubAnimationData(activeSubSwitchIndex), switchDefinition.GetPropActiveSubTransition(activeSubSwitchIndex), 1f);
                            ProcessPropAnimation(switchDefinition.GetPropSubAnimationData(inactiveSubSwitchIndex), switchDefinition.GetPropInactiveSubTransition(inactiveSubSwitchIndex), 0f);
                            break;
                        }
                        case AxisDefinition axisDefinition: {
                            var adjustedValue = axisDefinition.AdjustedValue;
                            activeAnimationData = axisDefinition.CharacterAnimation.Max;
                            targetWeight = Mathf.Max(0f, adjustedValue);
                            setAnimationtoEnd = true;

                            if (axisDefinition.NeutralState == AxisNeutralState.Midpoint) {
                                var minAnimationData = axisDefinition.CharacterAnimation.Min;
                                var minWeight = Mathf.Abs(Mathf.Min(0f, adjustedValue));
                                ProcessAnimation(
                                    minAnimationData,
                                    null,
                                    minWeight,
                                    true,
                                    true
                                );
                                ProcessPropAnimation(
                                    axisDefinition.PropAnimation.Min,
                                    null,
                                    minWeight
                                );
                            }
                            break;
                        }
                    }
                    ProcessAnimation(
                        activeAnimationData,
                        activeTransition,
                        targetWeight,
                        true,
                        setAnimationtoEnd
                    );
                    ProcessPropAnimation(
                        currentSignal.GetPropAnimationData(),
                        currentSignal.GetPropActiveTransition(),
                        targetWeight
                    );

                    // Deactivate previous signal (Handles multi-press issue)
                    if (justDownSignal != null && justDownSignal != currentSignal && !(justDownSignal is AxisDefinition justDownAxisSignal)) {
                        ProcessAnimation(
                            justDownSignal.GetCharacterActiveAnimationData(),
                            justDownSignal.GetCharacterInactiveTransition(),
                            0f
                        );
                        ProcessPropAnimation(
                            justDownSignal.GetPropAnimationData(),
                            justDownSignal.GetPropInactiveTransition(),
                            0f
                        );
                    }

                } else if (currentSignal.IsAnimationStopFrame) {

                    // Process inactive animation
                    ProcessAnimation(
                        currentSignal.GetCharacterActiveAnimationData(),
                        currentSignal.GetCharacterInactiveTransition(),
                        0f
                    );
                    ProcessPropAnimation(
                        currentSignal.GetPropAnimationData(),
                        currentSignal.GetPropInactiveTransition(),
                        0f
                    );

                    // Special additional operations
                    switch (currentSignal) {
                        case SwitchDefinition switchDefinition:
                            // Special sub-switch sub-index handling
                            var lastSubSwitchIndex = GetSwitchSubIndex(switchDefinition.Index, 1);
                            var activeSubAnimationData = switchDefinition.GetCharacterActiveSubAnimationData(lastSubSwitchIndex);
                            var activeSubTransition = switchDefinition.GetCharacterActiveTransition();
                            ProcessAnimation(activeSubAnimationData, activeSubTransition, 0f);
                            ProcessPropAnimation(
                                switchDefinition.GetPropSubAnimationData(lastSubSwitchIndex),
                                switchDefinition.GetPropInactiveTransition(),
                                0f
                            );
                            break;
                        case AxisDefinition axisDefinition:
                            ProcessAnimation(
                                axisDefinition.CharacterAnimation.Min,
                                null,
                                0f,
                                true
                            );
                            ProcessPropAnimation(
                                axisDefinition.PropAnimation.Min,
                                null,
                                0
                            );
                            break;
                    }
                }
            }
            // Process Hover animation
            foreach (var kvp in NamespacedGlobalLayerJustDownChangedRegistry) {
                // Process only on change
                if (!kvp.Value) return;
                var changedLayer = kvp.Key;

                var changedSignalDefinition = NamespacedGlobalLayerJustDownHistoryRegistry[0][changedLayer];
                var lastSignalDefinition = NamespacedGlobalLayerJustDownHistoryRegistry[1][changedLayer];
                var lastLastSignalDefinition = NamespacedGlobalLayerJustDownHistoryRegistry[2][changedLayer];

                if (changedSignalDefinition == null) continue;
                SignalDefinition.Registry.TryGetValue(changedSignalDefinition.GlobalLayerId, out var justDownSignalDefinition);

                SignalDefinition lastJustDownSignalDefinition = null;
                if (lastSignalDefinition != null) {
                    SignalDefinition.Registry.TryGetValue(lastSignalDefinition.GlobalLayerId, out lastJustDownSignalDefinition);
                }
                SignalDefinition lastLastJustDownSignalDefinition = null;
                if (lastLastSignalDefinition != null) {
                    SignalDefinition.Registry.TryGetValue(lastLastSignalDefinition.GlobalLayerId, out lastLastJustDownSignalDefinition);
                }

                // Special: Axis part of group will only trigger hover if it's the only one in the group
                if (changedSignalDefinition is AxisDefinition axisDefinition) {
                    var axisGroup = axisDefinition.AssignedGroup;
                    if (axisGroup != AxisGroup.Solo) {
                        if (HandledAxisGroupRegistry.ContainsKey(axisGroup)) {
                            continue;
                        } else {
                            HandledAxisGroupRegistry[axisGroup] = true;
                        }
                    }
                }

                Transition targetHoverTransition = changedSignalDefinition.GetCharacterHoverTransition();
                AnimationData targetHoverAnimation = changedSignalDefinition.GetCharacterHoverAnimationData();
                var isReturnToBaseWanted = false;
                if (targetHoverAnimation is BaseRevertibleAnimationData baseConvertibleUnhoverAnimationData) {
                    isReturnToBaseWanted = baseConvertibleUnhoverAnimationData.IsReturnToBaseWanted;
                }
                float timeFactor = 1f;
                if (targetHoverAnimation?.IsValidOverlappingAnimationData(Character) == true || isReturnToBaseWanted) {

                    // Special case: short-cut transition time when flip-floping between two same signals
                    if  (changedSignalDefinition == lastLastSignalDefinition) {
                        timeFactor = 1f - (targetHoverAnimation?.OverlappingAnimationData?.Weight ?? 0f);
                    }

                    ProcessAnimation(
                        targetHoverAnimation,
                        new Transition() {
                            Time = targetHoverTransition?.Time ?? 0f * timeFactor,
                            Ease = targetHoverTransition?.Ease ?? Ease.Linear
                        },
                        1f
                    );

                    var unhoverTime = targetHoverTransition?.Time;
                    var unhoverEase = targetHoverTransition?.Ease;

                    // Unhover all over inputs on same layer
                    foreach (var unhoverSignalDefinition in SignalDefinition.Registry.Values) {
                        // Same layer check
                        if (unhoverSignalDefinition.AssignedCharacterLayer != changedLayer) continue;

                        // Ignore the one that just changed
                        if (unhoverSignalDefinition == changedSignalDefinition) continue;

                        AnimationData unhoverAnimationData = unhoverSignalDefinition.GetCharacterHoverAnimationData();
                        Transition unhoverTransition = unhoverSignalDefinition.GetCharacterHoverTransition();

                        ProcessAnimation(
                            unhoverAnimationData,
                            new Transition() {
                                Time = (unhoverTime ?? unhoverTransition?.Time ?? 0f) * timeFactor,
                                Ease = unhoverEase ?? unhoverTransition?.Ease ?? Ease.Linear
                            },
                            0f
                        );

                        // Unhover all subswitches of unhovered switch
                        switch (unhoverSignalDefinition) {
                            case SwitchDefinition switchDefinition:
                                var animationDataList = new[] {
                                    switchDefinition.GetCharacterHoverSubAnimationData(0),
                                    switchDefinition.GetCharacterHoverSubAnimationData(1),
                                    switchDefinition.GetCharacterHoverSubAnimationData(2),
                                    switchDefinition.GetCharacterHoverSubAnimationData(3),
                                    switchDefinition.GetCharacterHoverSubAnimationData(4),
                                    switchDefinition.GetCharacterHoverSubAnimationData(5),
                                    switchDefinition.GetCharacterHoverSubAnimationData(6),
                                    switchDefinition.GetCharacterHoverSubAnimationData(7)
                                };
                                foreach (var unhoverSubSwitchAnimationData in animationDataList) {
                                    if (unhoverSubSwitchAnimationData == null) continue;
                                    ProcessAnimation(
                                        unhoverSubSwitchAnimationData,
                                        new Transition() {
                                            Time = (unhoverTime ?? unhoverTransition?.Time ?? 0f) * timeFactor,
                                            Ease = unhoverEase ?? unhoverTransition?.Ease ?? Ease.Linear
                                        },
                                        0f
                                    );
                                }
                                break;
                        }
                    }
                }
            }
        }

        void ProcessAnimation(AnimationData ad, Transition transition, float targetWeight, bool killTween = false, bool toEnd = false) {

            if (Character == null) return;

            // In case of sync issue
            var overlappingAnimationData = GetOverlappingAnimationData(ad?.OverlappingAnimationData?.CustomLayerID);
            if (overlappingAnimationData == null) return;

            var tween = ad.AnimancerTween;
            var tweenClone = ad.AnimancerCloneTween;

            // Animate hand
            // NOTE: This is very fragile, but it comes from the original code
            var pressAnimancerLayerIndex = 1 + Array.IndexOf(Character.OverlappingAnimations, overlappingAnimationData);
            if (killTween) {
                ResetAnimation(pressAnimancerLayerIndex, toEnd);
                tween?.Kill(true);
                tweenClone?.Kill(true);
            }
            ApplyAnimancerPropertyWeight(
                ref tween,
                ref tweenClone,
                pressAnimancerLayerIndex,
                targetWeight,
                transition?.Time ?? 0f,
                transition?.Ease ?? Ease.Linear,
                overlappingAnimationData
            );
            ad.AnimancerTween = tween;
            ad.AnimancerCloneTween = tweenClone;
        }

        void ProcessPropAnimation(PropAnimationDefinition pad, Transition transition, float targetWeight) {
            if (HeldProp == null) return;

            var animatorLayerName = pad?.AnimatorLayerName;
            if (animatorLayerName.IsNullOrEmpty()) return;

            var animator = HeldProp.GameObject.GetComponent<UnityEngine.Animator>();
            if (!animator) return;

            var animatorLayerIndex = animator.GetLayerIndex(animatorLayerName);
            if (animatorLayerIndex < 0) return;

            var currentWeight = animator.GetLayerWeight(animatorLayerIndex);

            if (transition != null && pad is TransitionablePropAnimationDefinition tpad) {

                var adjustedTransitionTime = transition.Time * Math.Abs(targetWeight - currentWeight);
                tpad.AnimatorLayerWeightTween?.Kill(false);

                tpad.AnimatorLayerWeightTween = DOTween.To(
                    () => animator.GetLayerWeight(animatorLayerIndex),
                    v => animator.SetLayerWeight(animatorLayerIndex, v),
                    targetWeight,
                    adjustedTransitionTime
                ).SetEase(transition.Ease);

                if (transition is DelayableTransition dt) {
                    tpad.AnimatorLayerWeightTween.SetDelay(dt.DelayTime);
                }

            } else {

                animator.SetLayerWeight(animatorLayerIndex, targetWeight);
            }
        }

        void ResetAnimation(int idx, bool toEnd = false) {
            ResetAnimationLow(CharacterAnimancer, idx, toEnd);
            ResetAnimationLow(CharacterAnimancerClone, idx, toEnd);
        }

        void ResetAnimationLow(Animancer.AnimancerComponent animancer, int idx, bool toEnd) {
            var layer = animancer.Layers[idx];
            if (layer == null) return;
            var state = layer.Play(layer.CurrentState);
            state.Time = toEnd ? state.Length : 0;
        }

        CharacterAsset.OverlappingAnimationData GetOverlappingAnimationData(string layerId) {
            if (layerId.IsNullOrEmpty()) return null;
            return Array.Find(Character.OverlappingAnimations, (a) => a.CustomLayerID == layerId);
        }

        void ApplyAnimancerPropertyWeight(
            ref Tween tween,
            ref Tween tween2,
            int idx,
            float targetWeight,
            float transitionTime,
            Ease transitionEasing,
            CharacterAsset.OverlappingAnimationData overlappingAnimationData = null
        ) {
            ApplyAnimancerPropertyWeightLow(ref tween, CharacterAnimancer, idx, targetWeight, transitionTime, transitionEasing, overlappingAnimationData);
            ApplyAnimancerPropertyWeightLow(ref tween2, CharacterAnimancerClone, idx, targetWeight, transitionTime, transitionEasing);
        }

        // NOTE: This code comes from SetCharacterOverlappingAnimationWeightNode#ApplyAnimancerProperty
        void ApplyAnimancerPropertyWeightLow(
            ref Tween tween,
            Animancer.AnimancerComponent animancer,
            int idx,
            float targetWeight,
            float transitionTime,
            Ease transitionEasing,
            CharacterAsset.OverlappingAnimationData overlappingAnimationData = null
        ) {
            if (transitionTime > 0f) {
                tween?.Kill(false);
                tween = DOTween.To(
                    () => animancer.Layers[idx].Weight,
                    delegate(float v) {
                        animancer.Layers[idx].SetWeight(v);
                        // NOTE: Do NOT do this! It will cause very strange behaviour, even without broadcasting
                        // overlappingAnimationData?.SetDataInput(nameof(overlappingAnimationData.Weight), v, false);
                    },
                    targetWeight,
                    transitionTime
                ).SetEase(transitionEasing);
            } else {
                tween?.Kill(true);
                animancer.Layers[idx].SetWeight(targetWeight);
                // NOTE: Do NOT do this! It will cause very strange behaviour, even without broadcasting
                // overlappingAnimationData?.SetDataInput(nameof(overlappingAnimationData.Weight), targetWeight, false);
            }
        }

        Animancer.AnimancerComponent CharacterAnimancer {
            get {
                return Character?.Animancer;
            }
        }

        Animancer.AnimancerComponent CharacterAnimancerClone {
            get {
                return Character?.CloneAnimancer;
            }
        }
    }
}
