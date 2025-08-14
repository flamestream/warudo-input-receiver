using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Utils;
using Warudo.Plugins.Core.Assets.Character;
using WebSocketSharp;
using static Warudo.Plugins.Core.Assets.Character.CharacterAsset;

namespace FlameStream {
    public partial class InputReceiverAsset : ReceiverAsset {

        bool IsCharacterAnimationChangeRequested = false;

        virtual protected void OnCreateSignalDefinition() {
            Watch(nameof(Character), delegate {
                RequestCharacterAnimationChange();
            }, false);
            Watch(nameof(ButtonDefinitions), delegate {
                Array.Clear(ButtonDefinitionsReference, 0, ButtonDefinitionsReference.Length);
                ButtonDefinitions.ForEach(d => {
                    if (d.IsValid) {
                        d.Parent = this;
                        d.CharacterAnimation.AnimationData.Parent = this;
                        d.CharacterAnimation.AnimationData.HoverAnimationData.Parent = this;
                        d.CharacterAnimation.AnimationData.DownAnimationData.Parent = this;
                        d.PropAnimation.Parent = this;
                        d.PropAnimation.CheckPropAnimatorLayerName();
                        ButtonDefinitionsReference[d.Index] = d;
                    }
                });
                // Context.Service.Toast(Warudo.Core.Server.ToastSeverity.Info, "ButtonDefinitions Watcher", "Changed!");
            }, false);
            Watch(nameof(SwitchDefinitions), delegate {
                Array.Clear(SwitchDefinitionsReference, 0, SwitchDefinitionsReference.Length);
                SwitchDefinitions.ForEach(d => {
                    if (d.IsValid) {
                        d.Parent = this;
                        d.CharacterAnimation.AnimationDataD1.HoverAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD1.DownAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD2.HoverAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD2.DownAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD3.HoverAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD3.DownAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD4.HoverAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD4.DownAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD5.HoverAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD5.DownAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD6.HoverAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD6.DownAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD7.HoverAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD7.DownAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD8.HoverAnimation.Parent = this;
                        d.CharacterAnimation.AnimationDataD8.DownAnimation.Parent = this;
                        d.CharacterAnimation.Base.Parent = this;
                        d.PropAnimation.Parent = this;
                        d.PropAnimation.D1.Parent = this;
                        d.PropAnimation.D2.Parent = this;
                        d.PropAnimation.D3.Parent = this;
                        d.PropAnimation.D4.Parent = this;
                        d.PropAnimation.D5.Parent = this;
                        d.PropAnimation.D6.Parent = this;
                        d.PropAnimation.D7.Parent = this;
                        d.PropAnimation.D8.Parent = this;
                        d.PropAnimation.D1.CheckPropAnimatorLayerName();
                        d.PropAnimation.D2.CheckPropAnimatorLayerName();
                        d.PropAnimation.D3.CheckPropAnimatorLayerName();
                        d.PropAnimation.D4.CheckPropAnimatorLayerName();
                        d.PropAnimation.D5.CheckPropAnimatorLayerName();
                        d.PropAnimation.D6.CheckPropAnimatorLayerName();
                        d.PropAnimation.D7.CheckPropAnimatorLayerName();
                        d.PropAnimation.D8.CheckPropAnimatorLayerName();
                        SwitchDefinitionsReference[d.Index] = d;
                    }
                });
                RequestCharacterAnimationChange();
            }, false);
            Watch(nameof(AxisDefinitions), delegate {
                Array.Clear(AxisDefinitionsReference, 0, AxisDefinitionsReference.Length);
                AxisDefinitions.ForEach(d => {
                    if (d.IsValid) {
                        d.Parent = this;
                        d.CharacterAnimation.Base.Parent = this;
                        d.CharacterAnimation.Max.Parent = this;
                        d.CharacterAnimation.Min.Parent = this;
                        d.PropAnimation.Min.Parent = this;
                        d.PropAnimation.Max.Parent = this;
                        d.PropAnimation.Min.CheckPropAnimatorLayerName();
                        d.PropAnimation.Max.CheckPropAnimatorLayerName();
                        AxisDefinitionsReference[d.Index] = d;
                    }
                });
            }, false);
            OnGeneratedProfileSelectionChange(SignalDefinitionGeneration.Profile);
        }

        void OnUpdateSignalDefinition() {
            if (Character == null) return;

            if (IsCharacterAnimationChangeRequested) {
                IsCharacterAnimationChangeRequested = false;
                HandleCharacterAnimationChange();
            }
        }

        virtual protected void OnGeneratedProfileSelectionChange(SignalProfileType profile) {
            // Do nothing
        }

        protected void RequestCharacterAnimationChange() {
            IsCharacterAnimationChangeRequested = true;
        }

        void HandleCharacterAnimationChange() {

            if (Character == null) return;

            var unmanagedLayers = new List<OverlappingAnimationData>();
            var managedLayers = new List<OverlappingAnimationData>();
            foreach (var d in Character.OverlappingAnimations) {
                if (d.Animation.IsNullOrEmpty()) continue;
                if (d.CustomLayerID.IsNullOrEmpty()) continue;
                // Check if the layer ID starts with the prefix(CHARACTER_ANIM_LAYER_ID_PREFIX)}");
                if (d.CustomLayerID.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX, StringComparison.Ordinal)) {
                    managedLayers.Add(d);
                } else {
                    unmanagedLayers.Add(d);
                }
            }
            var newManagedLayers = new List<OverlappingAnimationData>();

            var baseAnimationInfo = SavedBindingData?.BaseAnimationLayer;
            if (baseAnimationInfo != null && IsEnabled && IsControlEnabled) {
                // NOTE: Ideally this would be managed by SignalDefinition classs watcher, but it is not behaving as expected
                SignalDefinition.Registry.Clear();

                // Base idle layer (Must be the top one)
                var baseLayer = managedLayers.FirstOrDefault(l => l.CustomLayerID == baseAnimationInfo.CustomLayerID) ?? StructuredData.Create<OverlappingAnimationData>();
                baseLayer.Additive = baseAnimationInfo.Additive;
                baseLayer.Animation = baseAnimationInfo.Animation;
                baseLayer.CustomLayerID = baseAnimationInfo.CustomLayerID;
                baseLayer.Enabled = true;
                baseLayer.Looping = baseAnimationInfo.Looping;
                baseLayer.Masked = baseAnimationInfo.MaskedBodyParts != null && baseAnimationInfo.MaskedBodyParts.Length > 0;
                baseLayer.MaskedBodyParts = baseAnimationInfo.MaskedBodyParts;
                baseLayer.Parent = Character;
                baseLayer.Speed = baseAnimationInfo.Speed;
                baseLayer.Weight = baseAnimationInfo.Weight;
                newManagedLayers.Add(baseLayer);

                // Button animations
                ButtonDefinitions?.ForEach(d => {
                    if (!d.IsValid) return;

                    SignalDefinition.Registry[d.GlobalLayerId] = d;

                    var animationData = d.CharacterAnimation.AnimationData;
                    var hoverAnimationData = animationData.HoverAnimationData;
                    if (hoverAnimationData.IsValidLayer) {
                        var hoverLayer = managedLayers.FirstOrDefault(l => l.CustomLayerID == d.CharacterAnimation.HoverLayerId) ?? StructuredData.Create<OverlappingAnimationData>();
                        hoverLayer.CustomLayerID = d.CharacterAnimation.HoverLayerId;
                        hoverLayer.Weight = 0f;
                        hoverLayer.Speed = 1f;
                        hoverLayer.Masked = d.CharacterAnimation.MaskedBodyParts != null && d.CharacterAnimation.MaskedBodyParts.Length > 0;
                        hoverLayer.MaskedBodyParts = d.CharacterAnimation.MaskedBodyParts;
                        hoverLayer.Additive = true;
                        hoverLayer.Looping = false;
                        hoverLayer.Animation = hoverAnimationData.Source;
                        hoverLayer.Parent = Character;
                        newManagedLayers.Add(hoverLayer);
                        animationData.HoverAnimationData.OverlappingAnimationData = hoverLayer;
                    }

                    var downAnimationData = animationData.DownAnimationData;
                    if (downAnimationData.IsValidLayer) {
                        var pressLayer = managedLayers.FirstOrDefault(l => l.CustomLayerID == d.CharacterAnimation.PressLayerId) ?? StructuredData.Create<OverlappingAnimationData>();
                        pressLayer.CustomLayerID = d.CharacterAnimation.PressLayerId;
                        pressLayer.Weight = 0f;
                        pressLayer.Speed = 1f;
                        pressLayer.Masked = d.CharacterAnimation.MaskedBodyParts != null && d.CharacterAnimation.MaskedBodyParts.Length > 0;
                        pressLayer.MaskedBodyParts = d.CharacterAnimation.MaskedBodyParts;
                        pressLayer.Additive = true;
                        pressLayer.Looping = false;
                        pressLayer.Animation = downAnimationData.Source;
                        pressLayer.Parent = Character;
                        newManagedLayers.Add(pressLayer);
                        animationData.DownAnimationData.OverlappingAnimationData = pressLayer;
                    }
                });

                // Switch animations
                SwitchDefinitions?.ForEach(d => {
                    if (!d.IsValid) return;

                    SignalDefinition.Registry[d.GlobalLayerId] = d;

                    if (d.CharacterAnimation.Base.IsValidLayer) {
                        var baseSwitchLayer = managedLayers.FirstOrDefault(l => l.CustomLayerID == d.CharacterAnimation.BaseLayerId) ?? StructuredData.Create<OverlappingAnimationData>();
                        baseSwitchLayer.CustomLayerID = d.CharacterAnimation.BaseLayerId;
                        baseSwitchLayer.Weight = 0f;
                        baseSwitchLayer.Speed = 1f;
                        baseSwitchLayer.Masked = d.CharacterAnimation.MaskedBodyParts != null && d.CharacterAnimation.MaskedBodyParts.Length > 0;
                        baseSwitchLayer.MaskedBodyParts = d.CharacterAnimation.MaskedBodyParts;
                        baseSwitchLayer.Additive = true;
                        baseSwitchLayer.Looping = false;
                        baseSwitchLayer.Animation = d.CharacterAnimation.Base.Source;
                        baseSwitchLayer.Parent = Character;
                        newManagedLayers.Add(baseSwitchLayer);
                        d.CharacterAnimation.Base.OverlappingAnimationData = baseSwitchLayer;
                    }

                    var animationDataList = new[] {
                        d.CharacterAnimation.AnimationDataD1,
                        d.CharacterAnimation.AnimationDataD2,
                        d.CharacterAnimation.AnimationDataD3,
                        d.CharacterAnimation.AnimationDataD4,
                        d.CharacterAnimation.AnimationDataD5,
                        d.CharacterAnimation.AnimationDataD6,
                        d.CharacterAnimation.AnimationDataD7,
                        d.CharacterAnimation.AnimationDataD8
                    };

                    animationDataList.ForEach(animationData => {
                        if (animationData.IsHoverDefined) {
                            var hoverLayer = managedLayers.FirstOrDefault(l => l.CustomLayerID == animationData.HoverCustomLayerId) ?? StructuredData.Create<OverlappingAnimationData>();
                            hoverLayer.CustomLayerID = animationData.HoverCustomLayerId;
                            hoverLayer.Weight = 0f;
                            hoverLayer.Speed = 1f;
                            hoverLayer.Masked = d.CharacterAnimation.MaskedBodyParts != null && d.CharacterAnimation.MaskedBodyParts.Length > 0;
                            hoverLayer.MaskedBodyParts = d.CharacterAnimation.MaskedBodyParts;
                            hoverLayer.Additive = true;
                            hoverLayer.Looping = false;
                            hoverLayer.Animation = animationData.HoverAnimation.Source;
                            hoverLayer.Parent = Character;
                            newManagedLayers.Add(hoverLayer);
                            animationData.HoverAnimation.OverlappingAnimationData = hoverLayer;
                        }
                        if (animationData.IsDownDefined) {
                            var activeLayer = managedLayers.FirstOrDefault(l => l.CustomLayerID == animationData.DownCustomLayerId) ?? StructuredData.Create<OverlappingAnimationData>();
                            activeLayer.CustomLayerID = animationData.DownCustomLayerId;
                            activeLayer.Weight = 0f;
                            activeLayer.Speed = 1f;
                            activeLayer.Masked = d.CharacterAnimation.MaskedBodyParts != null && d.CharacterAnimation.MaskedBodyParts.Length > 0;
                            activeLayer.MaskedBodyParts = d.CharacterAnimation.MaskedBodyParts;
                            activeLayer.Additive = true;
                            activeLayer.Looping = false;
                            activeLayer.Animation = animationData.DownAnimation.Source;
                            activeLayer.Parent = Character;
                            newManagedLayers.Add(activeLayer);
                            animationData.DownAnimation.OverlappingAnimationData = activeLayer;
                        }
                    });
                });

                // Axis animations
                AxisDefinitions?.ForEach(d => {
                    if (!d.IsValid) return;

                    SignalDefinition.Registry[d.GlobalLayerId] = d;

                    var characterAnimation = d.CharacterAnimation;
                    var baseAnimationData = characterAnimation.Base;
                    if (baseAnimationData.IsValidLayer) {
                        var baseAxisLayer = managedLayers.FirstOrDefault(l => l.CustomLayerID == characterAnimation.BaseLayerId) ?? StructuredData.Create<OverlappingAnimationData>();
                        baseAxisLayer.CustomLayerID = characterAnimation.BaseLayerId;
                        baseAxisLayer.Weight = 0f;
                        baseAxisLayer.Speed = 1f;
                        baseAxisLayer.Masked = characterAnimation.MaskedBodyParts != null && characterAnimation.MaskedBodyParts.Length > 0;
                        baseAxisLayer.MaskedBodyParts = characterAnimation.MaskedBodyParts;
                        baseAxisLayer.Additive = true;
                        baseAxisLayer.Looping = false;
                        baseAxisLayer.Animation = baseAnimationData.Source;
                        baseAxisLayer.Parent = Character;
                        newManagedLayers.Add(baseAxisLayer);
                        baseAnimationData.OverlappingAnimationData = baseAxisLayer;
                    }

                    var maxAnimationData = characterAnimation.Max;
                    if (maxAnimationData.IsValidLayer) {
                        var maxLayer = managedLayers.FirstOrDefault(l => l.CustomLayerID == characterAnimation.MaxLayerId) ?? StructuredData.Create<OverlappingAnimationData>();
                        maxLayer.CustomLayerID = characterAnimation.MaxLayerId;
                        maxLayer.Weight = 0f;
                        maxLayer.Speed = 1f;
                        maxLayer.Masked = characterAnimation.MaskedBodyParts != null && characterAnimation.MaskedBodyParts.Length > 0;
                        maxLayer.MaskedBodyParts = characterAnimation.MaskedBodyParts;
                        maxLayer.Additive = true;
                        maxLayer.Looping = false;
                        maxLayer.Animation = maxAnimationData.Source;
                        maxLayer.Parent = Character;
                        newManagedLayers.Add(maxLayer);
                        maxAnimationData.OverlappingAnimationData = maxLayer;
                    }

                    var minAnimationData = characterAnimation.Min;
                    if (minAnimationData.IsValidLayer) {
                        var minLayer = managedLayers.FirstOrDefault(l => l.CustomLayerID == characterAnimation.MinLayerId) ?? StructuredData.Create<OverlappingAnimationData>();
                        minLayer.CustomLayerID = characterAnimation.MinLayerId;
                        minLayer.Weight = 0f;
                        minLayer.Speed = 1f;
                        minLayer.Masked = characterAnimation.MaskedBodyParts != null && characterAnimation.MaskedBodyParts.Length > 0;
                        minLayer.MaskedBodyParts = characterAnimation.MaskedBodyParts;
                        minLayer.Additive = true;
                        minLayer.Looping = false;
                        minLayer.Animation = minAnimationData.Source;
                        minLayer.Parent = Character;
                        newManagedLayers.Add(minLayer);
                        minAnimationData.OverlappingAnimationData = minLayer;
                    }
                });

                unmanagedLayers.AddRange(newManagedLayers);
            }

            if (newManagedLayers.Count > 0) {
                Context.Service.Toast(Warudo.Core.Server.ToastSeverity.Info, Name, $"CHARACTER_ANIMATION_LAYERS_SETUP_NOTIFICATION".Localized(new object[] {newManagedLayers.Count}));
            } else {
                Context.Service.Toast(Warudo.Core.Server.ToastSeverity.Info, Name, $"CHARACTER_ANIMATION_LAYERS_CLEANED_NOTIFICATION");
            }

            Character.SetDataInput($"{nameof(Character.OverlappingAnimations)}", unmanagedLayers.ToArray(), true);
            Character.Broadcast();
        }

        public class SignalDefinitionGenerationSection : StructuredData<InputReceiverAsset>, ICollapsibleStructuredData {

            [Markdown]
            public string AnimationInstructions = @"It is recommended to generate the signal definitions from one of the profile below.
You can then customize the animations and transitions for each signal definition.";

            [DataInput]
            public SignalProfileType Profile;

            [Trigger]
            [Label("GENERATE_FROM_PROFILE")]
            public async void TriggerGenerateProfile() {

                // Validate supported profiles
                var supportedProfiles = Parent.SupportedProfileTypes;
                if (!supportedProfiles.Contains(Profile)) {
                    Context.Service.PromptMessage("ERROR", $"Profile definition for {Profile.Localized()} is not supported by this asset at the moment. Please select one of following instead:\n\n{string.Join("\n", supportedProfiles.Select(p => p.Localized()))}");
                    return;
                }

                bool replaceWanted = await Context.Service.PromptConfirmation("WARNING", $"This will overwrite current signal definitions with a {Profile.Localized()} profile. Do you want to continue?");
                if (!replaceWanted) return;

                Parent.GenerateButtonDefinitions(Profile);
            }

            public string GetHeader() {
                return "SIGNAL_DEFINITION_GENERATOR";
            }

            protected override void OnCreate() {
                base.OnCreate();
                Watch(nameof(Profile), delegate {
                    Parent.OnGeneratedProfileSelectionChange(Profile);
                }, false);
            }

            public void SetProfileDescription(string msg) {
                GetDataInputPort(nameof(Profile)).Properties.description = msg;
                BroadcastDataInputProperties(nameof(Profile));
            }
        }

        public abstract class SignalDefinition : StructuredData<InputReceiverAsset>, ICollapsibleStructuredData {

            public static readonly Dictionary<string, SignalDefinition> Registry = new Dictionary<string, SignalDefinition>();

            [Hidden]
            [DataInput]
            public bool IsValid;

            [Hidden]
            [DataInput]
            public int Index = -1;

            [DataInput(0)]
            public string Label;

            [DataInput(10)]
            [Label("ASSIGNED_CHARACTER_LAYER")]
            public Layer AssignedCharacterLayer;

            protected override void OnCreate() {
                base.OnCreate();
                Watch(nameof(IsValid), () => {
                    if (!IsValid) return;
                    if (Index < 0) return;

                    Registry[GlobalLayerId] = this;
                });
                Watch(nameof(Index), () => {
                    if (!IsValid) return;
                    if (Index < 0) return;

                    Registry[GlobalLayerId] = this;
                });
            }

            protected override void OnDestroy() {
                base.OnDestroy();
                Registry.TryGetValue(GlobalLayerId, out var existing);

                if (existing != this) return;
                Registry.Remove(GlobalLayerId);
            }

            abstract public string GetHeader();
            abstract public string GlobalLayerId { get; }
            abstract public bool IsActive { get; }
            abstract public bool IsAnimationTriggerFrame { get; }
            abstract public bool IsAnimationStopFrame { get; }
            abstract public AnimationData GetCharacterHoverAnimationData();
            abstract public AnimationData GetCharacterActiveAnimationData();
            abstract public Transition GetCharacterHoverTransition();
            abstract public Transition GetCharacterActiveTransition();
            abstract public Transition GetCharacterInactiveTransition();

            abstract public PropAnimationDefinition GetPropAnimationData();
            abstract public Transition GetPropActiveTransition();
            abstract public Transition GetPropInactiveTransition();
        }
        public class ButtonDefinition : SignalDefinition {

            [DataInput(20)]
            [Label("CHARACTER_ANIMATION")]
            public CharacterButtonAnimationDefinition CharacterAnimation;

            [DataInput(30)]
            [Label("PROP_ANIMATION")]
            public TransitionablePropAnimationDefinition PropAnimation;

            [DataInput(50)]
            [Label("PROP_MOTION_FACTOR")]
            public PropMotionDefinition PropMotion;

            override protected void OnCreate() {
                base.OnCreate();
                Watch(nameof(CharacterAnimation), () => {
                    CharacterAnimation.Parent = this;
                });
                Watch(nameof(PropAnimation), () => {
                    PropAnimation.CheckPropAnimatorLayerName();
                });
            }

            public override string GetHeader() {
                if (!IsValid) {
                    return "❌ Invalid. Delete this and click button below instead.";
                }
                string icon = "🌑";
                if (IsActive) {
                    icon = "🌕";
                }
                string name = $"[{Index}] {icon} {AssignedCharacterLayer.Localized()} Button - {Label}";
                return name;
            }

            public override bool IsActive => Parent?.IsButtonDown(Index) == true;

            public override bool IsAnimationTriggerFrame => Parent?.IsButtonJustDown(Index) == true;

            public override bool IsAnimationStopFrame => Parent?.IsButtonJustUp(Index) == true;

            public override string GlobalLayerId {
                get {
                    return IsValid ? $"btn{Index}" : null;
                }
            }

            public override AnimationData GetCharacterHoverAnimationData() {
                return CharacterAnimation.AnimationData.HoverAnimationData;
            }

            public override AnimationData GetCharacterActiveAnimationData() {
                return CharacterAnimation.AnimationData.DownAnimationData;
            }

            public override Transition GetCharacterHoverTransition() {
                return CharacterAnimation.AnimationData.HoverAnimationData.Transition;
            }

            public override Transition GetCharacterActiveTransition() {
                return CharacterAnimation.AnimationData.DownAnimationData.Transition;
            }

            public override Transition GetCharacterInactiveTransition() {
                return CharacterAnimation.AnimationData.DownAnimationData.UpTransition;
            }

            public override PropAnimationDefinition GetPropAnimationData() {
                return PropAnimation;
            }

            public override Transition GetPropActiveTransition() {
                return PropAnimation.TransitionDown;
            }

            public override Transition GetPropInactiveTransition() {
                return PropAnimation.TransitionUp;
            }
        }

        public class SwitchDefinition : SignalDefinition {

            [DataInput(20)]
            [Label("CHARACTER_ANIMATION")]
            public SwitchCharacterAnimationDefinition CharacterAnimation;

            [DataInput(30)]
            [Label("PROP_ANIMATION")]
            public SwitchPropAnimationDefinition PropAnimation;

            [DataInput(40)]
            [Label("PROP_MOTION_FACTOR")]
            public SwitchPropMotionDefinition PropMotionSet;

            override protected void OnCreate() {
                base.OnCreate();
                Watch(nameof(CharacterAnimation), () => {
                    CharacterAnimation.Parent = this;
                }, true);
                Watch(nameof(PropAnimation), () => {
                    PropAnimation.D1.CheckPropAnimatorLayerName();
                    PropAnimation.D2.CheckPropAnimatorLayerName();
                    PropAnimation.D3.CheckPropAnimatorLayerName();
                    PropAnimation.D4.CheckPropAnimatorLayerName();
                    PropAnimation.D5.CheckPropAnimatorLayerName();
                    PropAnimation.D6.CheckPropAnimatorLayerName();
                    PropAnimation.D7.CheckPropAnimatorLayerName();
                    PropAnimation.D8.CheckPropAnimatorLayerName();
                }, false);
            }

            public override string GetHeader() {
                if (!IsValid) {
                    return "❌ Invalid. Delete this and click button below instead.";
                }
                string icon = "🌑";
                switch (Parent?.GetSwitchSubIndex(Index)) {
                    case 1:
                        icon = "⬆️";
                        break;
                    case 2:
                        icon = "↗️";
                        break;
                    case 3:
                        icon = "➡️";
                        break;
                    case 4:
                        icon = "↘️";
                        break;
                    case 5:
                        icon = "⬇️";
                        break;
                    case 6:
                        icon = "↙️";
                        break;
                    case 7:
                        icon = "⬅️";
                        break;
                    case 8:
                        icon = "↖️";
                        break;
                }
                string name = $"[{Index}] {icon} {AssignedCharacterLayer.Localized()} Switch - {Label}";
                return name;
            }

            public override bool IsActive {
                get {
                    return Parent?.IsSwitchActive(Index) == true;
                }
            }

            public override bool IsAnimationTriggerFrame => Parent?.IsSwitchJustActive(Index) == true;

            public override bool IsAnimationStopFrame => Parent?.IsSwitchJustInactive(Index) == true;

            public override string GlobalLayerId {
                get {
                    return IsValid ? $"switch{Index}" : null;
                }
            }

            public override AnimationData GetCharacterHoverAnimationData() {
                return CharacterAnimation.Base;
            }

            public int Value {
                get {
                    return Parent?.GetSwitchSubIndex(Index) ?? 0;
                }
            }

            public AnimationData GetCharacterHoverSubAnimationData(int subIndex) {
                switch (subIndex) {
                    case 1:
                        return CharacterAnimation.AnimationDataD1.HoverAnimation;
                    case 2:
                        return CharacterAnimation.AnimationDataD2.HoverAnimation;
                    case 3:
                        return CharacterAnimation.AnimationDataD3.HoverAnimation;
                    case 4:
                        return CharacterAnimation.AnimationDataD4.HoverAnimation;
                    case 5:
                        return CharacterAnimation.AnimationDataD5.HoverAnimation;
                    case 6:
                        return CharacterAnimation.AnimationDataD6.HoverAnimation;
                    case 7:
                        return CharacterAnimation.AnimationDataD7.HoverAnimation;
                    case 8:
                        return CharacterAnimation.AnimationDataD8.HoverAnimation;
                    default:
                        return null;
                }
            }

            public override AnimationData GetCharacterActiveAnimationData() {
                return null;
            }

            public AnimationData GetCharacterActiveSubAnimationData(int index) {
                switch (index) {
                    case 1:
                        return CharacterAnimation.AnimationDataD1.DownAnimation;
                    case 2:
                        return CharacterAnimation.AnimationDataD2.DownAnimation;
                    case 3:
                        return CharacterAnimation.AnimationDataD3.DownAnimation;
                    case 4:
                        return CharacterAnimation.AnimationDataD4.DownAnimation;
                    case 5:
                        return CharacterAnimation.AnimationDataD5.DownAnimation;
                    case 6:
                        return CharacterAnimation.AnimationDataD6.DownAnimation;
                    case 7:
                        return CharacterAnimation.AnimationDataD7.DownAnimation;
                    case 8:
                        return CharacterAnimation.AnimationDataD8.DownAnimation;
                    default:
                        return null;
                }
            }

            public override Transition GetCharacterHoverTransition() {
                return CharacterAnimation.TransitionHover;
            }

            public override Transition GetCharacterActiveTransition() {
                return CharacterAnimation.TransitionDown;
            }

            public override Transition GetCharacterInactiveTransition() {
                return CharacterAnimation.TransitionUp;
            }

            public override PropAnimationDefinition GetPropAnimationData() {
                return null;
            }

            public TransitionablePropAnimationDefinition GetPropSubAnimationData(int subIndex = 0) {
                switch (subIndex) {
                    case 1:
                        return PropAnimation.D1;
                    case 2:
                        return PropAnimation.D2;
                    case 3:
                        return PropAnimation.D3;
                    case 4:
                        return PropAnimation.D4;
                    case 5:
                        return PropAnimation.D5;
                    case 6:
                        return PropAnimation.D6;
                    case 7:
                        return PropAnimation.D7;
                    case 8:
                        return PropAnimation.D8;
                    default:
                        return null;
                }
            }

            public override Transition GetPropActiveTransition() {
                return null;
            }

            public override Transition GetPropInactiveTransition() {
                return null;
            }

            public Transition GetPropActiveSubTransition(int subIndex = 0) {
                return GetPropSubAnimationData(subIndex)?.TransitionDown;
            }

            public Transition GetPropInactiveSubTransition(int subIndex = 0) {
                return (GetPropSubAnimationData(subIndex) as TransitionablePropAnimationDefinition)?.TransitionUp;
            }
        }

        public class AxisDefinition : SignalDefinition {

            [DataInput(20)]
            [Label("ASSIGNED_AXIS_GROUP")]
            public AxisGroup AssignedGroup;

            protected override void OnCreate() {
                base.OnCreate();
                Watch(nameof(NeutralState), RefreshMinDataInputVisibility);
                RefreshMinDataInputVisibility();
            }

            [DataInput(30)]
            [Label("DEADZONE_THRESHOLD")]
            public float DeadzoneThreshold = DEFAULT_DEADZONE_RADIUS;

            [DataInput(40)]
            [Label("RANGE")]
            public AxisNeutralState NeutralState;

            [DataInput(50)]
            [Label("CHARACTER_ANIMATION")]
            public AxisCharacterAnimationDefinition CharacterAnimation;

            [DataInput(60)]
            [Label("PROP_ANIMATION")]
            public AxisPropAnimationDefinition PropAnimation;


            [DataInput(70)]
            [Label("PROP_MOTION")]
            public AxisPropMotionDefinition PropMotionSet;

            public override string GetHeader() {
                if (!IsValid) {
                    return "❌ Invalid. Delete this and click button below instead.";
                }
                string value = AdjustedValue.ToString("0.000");
                string icon = IsActive ? "🌕" : "🌑";
                string groupInfo = AssignedGroup == AxisGroup.Solo ? "" : $"@G{(int)AssignedGroup}";

                string name = $"[{Index}] {icon} [{value}] {AssignedCharacterLayer.Localized()} Axis{groupInfo} - {Label}";
                return name;
            }

            public override string GlobalLayerId {
                get {
                    return IsValid ? $"Axis{Index}" : null;
                }
            }

            public void RefreshMinDataInputVisibility() {
                CharacterAnimation.SetMinDataInputVisibility(NeutralState == AxisNeutralState.Midpoint);
                PropAnimation.SetMinDataInputVisibility(NeutralState == AxisNeutralState.Midpoint);
            }

            public override bool IsActive {
                get {
                    if (Parent == null) return false;

                    return Parent.IsAxisActive(Index);
                }
            }

            public bool IsValueActive(float value) {
                switch (NeutralState) {
                    case AxisNeutralState.Midpoint:
                        return Math.Abs(value - 0.5f) > DeadzoneThreshold;

                    case AxisNeutralState.Zero:
                    default:
                        return value > DeadzoneThreshold;
                }
            }

            public override bool IsAnimationTriggerFrame => Parent?.IsAxisActive(Index) == true;

            public override bool IsAnimationStopFrame => Parent?.IsAxisJustInactive(Index) == true;

            public float AdjustedValue {
                get {
                    if (Parent == null) return 0;

                    return Parent.GetAxisAdjustedValue(Index);
                }
            }

            public float CalculateAdjustedValue(float value) {
                switch (NeutralState) {
                    case AxisNeutralState.Midpoint:
                        return (value - 0.5f) * 2;

                    case AxisNeutralState.Zero:
                    default:
                        return value;
                }
            }

            public override AnimationData GetCharacterHoverAnimationData() {
                return CharacterAnimation.Base;
            }

            public override AnimationData GetCharacterActiveAnimationData() {
                return CharacterAnimation.Max;
            }

            public override Transition GetCharacterHoverTransition() {
                return null;
            }

            public override Transition GetCharacterActiveTransition() {
                return null;
            }

            public override Transition GetCharacterInactiveTransition() {
                return null;
            }

            public override PropAnimationDefinition GetPropAnimationData() {
                return PropAnimation.Max;
            }

            public override Transition GetPropActiveTransition() {
                return null;
            }

            public override Transition GetPropInactiveTransition() {
                return null;
            }
        }

        public class CharacterButtonAnimationDefinition : StructuredData<ButtonDefinition>, ICollapsibleStructuredData {

            [DataInput(0)]
            public HoverableStatefulTransitionAnimationData AnimationData;

            [DataInput(10)]
            [Label("MASKED_BODY_PARTS")]
            public AnimationMaskedBodyPart[] MaskedBodyParts;

            protected override void OnCreate() {
                base.OnCreate();
                Watch(nameof(MaskedBodyParts), () => {
                    Parent?.Parent?.RequestCharacterAnimationChange();
                }, true);
            }

            public virtual string GetHeader() {
                var maskedIcon = MaskedBodyParts.Length > 0 ? "🌕" : "🌑";
                return $"{AnimationData.GetHeader()} | {maskedIcon} Masked";
            }


            public string HoverLayerId {
                get {
                    return $"{Parent?.Parent?.CHARACTER_ANIM_LAYER_ID_PREFIX} {Parent?.GlobalLayerId}@hover";
                }
            }

            public string PressLayerId {
                get {
                    return $"{Parent?.Parent?.CHARACTER_ANIM_LAYER_ID_PREFIX} {Parent?.GlobalLayerId}@press";
                }
            }

        }

        public class SwitchCharacterAnimationDefinition : StructuredData<SwitchDefinition>, ICollapsibleStructuredData {

            [DataInput]
            [Label("DIRECTION_UP")]
            public SwitchStateAnimationData AnimationDataD1;

            [DataInput]
            [Label("DIRECTION_UP_RIGHT")]
            public SwitchStateAnimationData AnimationDataD2;

            [DataInput]
            [Label("DIRECTION_RIGHT")]
            public SwitchStateAnimationData AnimationDataD3;

            [DataInput]
            [Label("DIRECTION_DOWN_RIGHT")]
            public SwitchStateAnimationData AnimationDataD4;

            [DataInput]
            [Label("DIRECTION_DOWN")]
            public SwitchStateAnimationData AnimationDataD5;

            [DataInput]
            [Label("DIRECTION_DOWN_LEFT")]
            public SwitchStateAnimationData AnimationDataD6;

            [DataInput]
            [Label("DIRECTION_LEFT")]
            public SwitchStateAnimationData AnimationDataD7;

            [DataInput]
            [Label("DIRECTION_UP_LEFT")]
            public SwitchStateAnimationData AnimationDataD8;

            [DataInput]
            public BaseRevertibleAnimationData Base;

            [DataInput]
            public Transition TransitionHover;

            [DataInput]
            public Transition TransitionDown;

            [DataInput]
            public Transition TransitionUp;

            [DataInput]
            [Label("MASKED_BODY_PARTS")]
            public AnimationMaskedBodyPart[] MaskedBodyParts;

            public string BaseLayerId {
                get {
                    return $"{LayerIdPrefix} Base";
                }
            }

            protected override void OnCreate() {
                base.OnCreate();
                AnimationDataD1.Parent = this;
                AnimationDataD2.Parent = this;
                AnimationDataD3.Parent = this;
                AnimationDataD4.Parent = this;
                AnimationDataD5.Parent = this;
                AnimationDataD6.Parent = this;
                AnimationDataD7.Parent = this;
                AnimationDataD8.Parent = this;

                Watch(nameof(MaskedBodyParts), () => {
                    Parent?.Parent?.RequestCharacterAnimationChange();
                }, true);
            }

            public string GetHeader() {
                int definedCount = 0;
                if (AnimationDataD1.IsHoverDefined) ++definedCount;
                if (AnimationDataD1.IsDownDefined) ++definedCount;
                if (AnimationDataD2.IsHoverDefined) ++definedCount;
                if (AnimationDataD2.IsDownDefined) ++definedCount;
                if (AnimationDataD3.IsHoverDefined) ++definedCount;
                if (AnimationDataD3.IsDownDefined) ++definedCount;
                if (AnimationDataD4.IsHoverDefined) ++definedCount;
                if (AnimationDataD4.IsDownDefined) ++definedCount;
                if (AnimationDataD5.IsHoverDefined) ++definedCount;
                if (AnimationDataD5.IsDownDefined) ++definedCount;
                if (AnimationDataD6.IsHoverDefined) ++definedCount;
                if (AnimationDataD6.IsDownDefined) ++definedCount;
                if (AnimationDataD7.IsHoverDefined) ++definedCount;
                if (AnimationDataD7.IsDownDefined) ++definedCount;
                if (AnimationDataD8.IsHoverDefined) ++definedCount;
                if (AnimationDataD8.IsDownDefined) ++definedCount;

                var baseLayerKeyword = (Base?.IsDefined ?? false) ? Base.IsValidLayer ? "Has" : "Fallback" : "No";

                return $"{definedCount} Defined ({baseLayerKeyword} base layer) | {TransitionHover.Time}s | {TransitionDown.Time}s/{TransitionUp.Time}s";
            }

            public SwitchStateAnimationData GetAnimationData(int index) {
                switch (index) {
                    case 1:
                        return AnimationDataD1;
                    case 2:
                        return AnimationDataD2;
                    case 3:
                        return AnimationDataD3;
                    case 4:
                        return AnimationDataD4;
                    case 5:
                        return AnimationDataD5;
                    case 6:
                        return AnimationDataD6;
                    case 7:
                        return AnimationDataD7;
                    case 8:
                        return AnimationDataD8;
                    default:
                        return null;
                }
            }

            public string LayerIdPrefix {
                get {
                    return $"{Parent?.Parent?.CHARACTER_ANIM_LAYER_ID_PREFIX} {Parent?.GlobalLayerId}";
                }
            }
        }

        public class AxisCharacterAnimationDefinition : StructuredData<AxisDefinition>, ICollapsibleStructuredData {

            [DataInput]
            [Label("ANIMATION_BASE")]
            public BaseRevertibleAnimationData Base;

            [DataInput]
            [Label("ANIMATION_MAX")]
            public AnimationData Max;

            [DataInput]
            [Label("ANIMATION_MIN")]
            public AnimationData Min;

            [DataInput]
            [Label("MASKED_BODY_PARTS")]
            public AnimationMaskedBodyPart[] MaskedBodyParts;

            public string MaxLayerId {
                get {
                    return $"{LayerIdPrefix} Max";
                }
            }

            public string MinLayerId {
                get {
                    return $"{LayerIdPrefix} Min";
                }
            }

            public string BaseLayerId {
                get {
                    return $"{LayerIdPrefix} Base";
                }
            }

            protected override void OnCreate() {
                base.OnCreate();
                Watch(nameof(MaskedBodyParts), () => {
                    Parent?.Parent?.RequestCharacterAnimationChange();
                }, true);
            }

            public string GetHeader() {
                var maxIcon = Max.IsDefined ? "✅" : "❌";
                var baseIcon = Base.IsDefined ? "✅" : "❌";

                var str = $"{baseIcon} Base | {maxIcon} Max";
                if (!GetDataInputPort(nameof(Min)).Properties.hidden) {
                    var minIcon = Min.IsDefined ? "✅" : "❌";
                    str += $" | {minIcon} Min";
                }

                return str;
            }

            public string LayerIdPrefix {
                get {
                    return $"{Parent?.Parent?.CHARACTER_ANIM_LAYER_ID_PREFIX} {Parent?.GlobalLayerId}";
                }
            }

            public void SetMinDataInputVisibility(bool value) {
                GetDataInputPort(nameof(Min)).Properties.hidden = !value;
                BroadcastDataInputProperties(nameof(Min));
            }
        }

        public class PropAnimationDefinition : StructuredData<InputReceiverAsset>, ICollapsibleStructuredData {

            [Markdown(0)]
            public string ErrorMessage;

            [DataInput(10)]
            public string AnimatorLayerName;

            protected override void OnCreate() {
                base.OnCreate();
                Watch(nameof(AnimatorLayerName), CheckPropAnimatorLayerName);
            }

            public bool IsValid {
                get {
                    return string.IsNullOrEmpty(ErrorMessage) && !string.IsNullOrEmpty(AnimatorLayerName);
                }
            }

            public virtual string GetHeader() {
                string icon = string.IsNullOrEmpty(AnimatorLayerName) ? "🌑" : ErrorMessage != null ? "❌" : "✅";
                return $"{icon} {AnimatorLayerName}";
            }

            public void CheckPropAnimatorLayerName() {
                if (string.IsNullOrEmpty(AnimatorLayerName)) {
                    SetDataInput(nameof(ErrorMessage), null, true);
                    return;
                };

                if (Parent == null) {
                    SetDataInput(nameof(ErrorMessage), "⚠️ Parent not defined (Linking issue)", true);
                    return;
                }

                var heldProp = Parent.HeldProp;
                if (heldProp == null) {
                    SetDataInput(nameof(ErrorMessage), "⚠️ Held prop not defined", true);
                    return;
                }

                var gameObject = heldProp.GameObject;
                if (gameObject == null) {
                    SetDataInput(nameof(ErrorMessage), "⚠️ Held prop gameObject not defined (Timing issue)", true);
                    return;
                }

                var animator = gameObject.GetComponent<Animator>();
                if (animator == null) {
                    SetDataInput(nameof(ErrorMessage), "⚠️ Held prop does not have any Animator component", true);
                    return;
                }
                var layerIdx = animator.GetLayerIndex(AnimatorLayerName);
                if (layerIdx < 0) {
                    SetDataInput(nameof(ErrorMessage), $"⚠️ Held prop Animator does not contain Layer named '{AnimatorLayerName}'", true);
                    return;
                }

                SetDataInput(nameof(ErrorMessage), null, true);
            }
        }

        public class TransitionablePropAnimationDefinition : PropAnimationDefinition {

            public Tween AnimatorLayerWeightTween;

            [DataInput(30)]
            public DelayableTransition TransitionDown;

            [DataInput(30)]
            public DelayableTransition TransitionUp;

            public override string GetHeader() {
                return $"{base.GetHeader()} | Down {TransitionDown?.ShortLabel} | Up {TransitionUp?.ShortLabel}";
            }
        }

        public class SwitchPropAnimationDefinition : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [Label("DIRECTION_UP")]
            public TransitionablePropAnimationDefinition D1;

            [DataInput]
            [Label("DIRECTION_UP_RIGHT")]
            public TransitionablePropAnimationDefinition D2;

            [DataInput]
            [Label("DIRECTION_RIGHT")]
            public TransitionablePropAnimationDefinition D3;

            [DataInput]
            [Label("DIRECTION_DOWN_RIGHT")]
            public TransitionablePropAnimationDefinition D4;

            [DataInput]
            [Label("DIRECTION_DOWN")]
            public TransitionablePropAnimationDefinition D5;

            [DataInput]
            [Label("DIRECTION_DOWN_LEFT")]
            public TransitionablePropAnimationDefinition D6;

            [DataInput]
            [Label("DIRECTION_LEFT")]
            public TransitionablePropAnimationDefinition D7;

            [DataInput]
            [Label("DIRECTION_UP_LEFT")]
            public TransitionablePropAnimationDefinition D8;

            public string GetHeader() {
                int definedCount = 0;
                if (D1.IsValid) ++definedCount;
                if (D2.IsValid) ++definedCount;
                if (D3.IsValid) ++definedCount;
                if (D4.IsValid) ++definedCount;
                if (D5.IsValid) ++definedCount;
                if (D6.IsValid) ++definedCount;
                if (D7.IsValid) ++definedCount;
                if (D8.IsValid) ++definedCount;

                return $"{definedCount}/8 Defined";
            }
        }

        public class AxisPropAnimationDefinition : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            public PropAnimationDefinition Max;

            [DataInput]
            public PropAnimationDefinition Min;

            public string GetHeader() {
                var maxIcon = string.IsNullOrEmpty(Max.AnimatorLayerName) ? "🌑" : string.IsNullOrEmpty(Max.ErrorMessage) ? "✅" : "❌";
                var str = $"{maxIcon} Max";

                if (!GetDataInputPort(nameof(Min)).Properties.hidden) {
                    var minIcon = string.IsNullOrEmpty(Min.AnimatorLayerName) ? "🌑" : string.IsNullOrEmpty(Max.ErrorMessage) ? "✅" : "❌";
                    str += $" | {minIcon} Min";
                }

                return str;
            }

            public void SetMinDataInputVisibility(bool value) {
                GetDataInputPort(nameof(Min)).Properties.hidden = !value;
                BroadcastDataInputProperties(nameof(Min));
            }
        }

        public enum Layer {
            [Label("UNASSIGNED")]
            Unassigned,
            [Label("FINGER_LEFT_THUMB")]
            LeftThumb,
            [Label("FINGER_RIGHT_THUMB")]
            RightThumb,
            [Label("FINGER_LEFT_INDEX")]
            LeftIndex,
            [Label("FINGER_RIGHT_INDEX")]
            RightIndex,
            [Label("FINGER_LEFT_MIDDLE")]
            LeftMiddle,
            [Label("FINGER_RIGHT_MIDDLE")]
            RightMiddle,
            [Label("FINGER_LEFT_RING")]
            LeftRing,
            [Label("FINGER_RIGHT_RING")]
            RightRing,
            [Label("FINGER_LEFT_PINKY")]
            LeftPinky,
            [Label("FINGER_RIGHT_PINKY")]
            RightPinky,
        }

        public enum AxisGroup {
            Solo,
            Group1,
            Group2,
            Group3,
            Group4,
            Group5,
            Group6,
            Group7
        }

        public enum AxisNeutralState {
            [Label("AXIS_NEUTRAL_ZERO")]
            Zero,
            [Label("AXIS_NEUTRAL_MIDPOINT")]
            Midpoint,
        }

        public class BaseRevertibleTransitionAnimationData : BaseRevertibleAnimationData {
            [DataInput(20)]
            [Label("TRANSITION")]
            public Transition Transition;
        }

        public class BaseRevertibleAnimationData : AnimationData {
            [DataInput]
            [Hidden]
            public bool IsReturnToBaseWanted;

            [Trigger(15)]
            [Label("TRANSITION_TO_BASE")]
            public void TriggerReturnToBaseWanted() {
                SetDataInput(nameof(IsReturnToBaseWanted), true, true);
            }

            [Markdown(15)]
            public string BaseMessage;

            [Trigger(15)]
            [Hidden]
            [Label("DEFINE_TRANSITION")]
            public void TriggerNotReturnToBaseWanted() {
                SetDataInput(nameof(IsReturnToBaseWanted), false, true);
            }

            protected override void OnCreate() {
                base.OnCreate();
                Watch(nameof(IsReturnToBaseWanted), () => {
                    GetTriggerPort(nameof(TriggerReturnToBaseWanted)).Properties.hidden = IsReturnToBaseWanted;
                    BroadcastTriggerProperties(nameof(TriggerReturnToBaseWanted));
                    GetTriggerPort(nameof(TriggerNotReturnToBaseWanted)).Properties.hidden = !IsReturnToBaseWanted;
                    BroadcastTriggerProperties(nameof(TriggerNotReturnToBaseWanted));
                    GetDataInputPort(nameof(Source)).Properties.hidden = IsReturnToBaseWanted;
                    BroadcastDataInputProperties(nameof(Source));
                    SetDataInput(nameof(BaseMessage), IsReturnToBaseWanted ? "🔙 Transitioning to base layer" : null, true);
                });
            }

            public override bool IsDefined {
                get {
                    return IsReturnToBaseWanted || base.IsDefined;
                }
            }
        }

        public class TransitionAnimationData : AnimationData {
            [DataInput(20)]
            [Label("TRANSITION")]
            public Transition Transition;
        }

        public class AnimationData : StructuredData<InputReceiverAsset> {

            public Tween AnimancerTween;
            public Tween AnimancerCloneTween;
            public OverlappingAnimationData OverlappingAnimationData;

            [DataInput(10)]
            [PreviewGallery]
            [AutoCompleteResource("CharacterAnimation", null)]
            [Label("SOURCE")]
            public string Source;

            protected override void OnCreate() {
                base.OnCreate();
                Watch(nameof(Source), () => {
                    Parent?.RequestCharacterAnimationChange();
                });
            }

            public bool IsValidLayer {
                get {
                    return !string.IsNullOrEmpty(Source);
                }
            }

            public virtual bool IsDefined {
                get {
                    return !string.IsNullOrEmpty(Source);
                }
            }

            public string AnimationName {
                get {
                    if (!IsValidLayer) {
                        return null;
                    }

                    var pathParts = Source.Split('/');
                    return pathParts[pathParts.Length - 1];
                }
            }

            public bool IsValidOverlappingAnimationData(CharacterAsset character) {
                if (character == null) return false;

                if (OverlappingAnimationData == null) return false;

                return Array.IndexOf(character.OverlappingAnimations, OverlappingAnimationData) >= 0;
            }
        }

        public class StatefulTransitionAnimationData : TransitionAnimationData {

            [DataInput]
            [Label("UP_TRANSITION")]
            public Transition UpTransition;
        }

        public class HoverableStatefulTransitionAnimationData : StructuredData<InputReceiverAsset>, ICollapsibleStructuredData {

            [DataInput]
            public BaseRevertibleTransitionAnimationData HoverAnimationData;

            [DataInput]
            public StatefulTransitionAnimationData DownAnimationData;

            public virtual string GetHeader() {
                var hoverIcon = HoverAnimationData.IsDefined ? "✅" : "❌";
                var pressIcon = DownAnimationData.IsDefined ? "✅" : "❌";

                return $"{hoverIcon} Hover {HoverAnimationData.Transition.ShortLabel} | {pressIcon} Down/Up {DownAnimationData.Transition.ShortLabel}";
            }
        }

        public class SwitchStateAnimationData : StructuredData<SwitchCharacterAnimationDefinition>, ICollapsibleStructuredData {

            [Hidden]
            [DataInput]
            public int Index = -1;

            [DataInput]
            public AnimationData HoverAnimation;

            [DataInput]
            public AnimationData DownAnimation;

            public string GetHeader() {
                var hoverIcon = IsHoverDefined ? "✅" : "❌";
                var pressIcon = IsDownDefined ? "✅" : "❌";

                return $"{hoverIcon} Hover | {pressIcon} Press";
            }

            public string GlobalLayerId {
                get {
                    return $"{Parent?.LayerIdPrefix}:{Index}";
                }
            }

            public string HoverCustomLayerId {
                get {
                    return $"{GlobalLayerId}@hover";
                }
            }

            public string DownCustomLayerId {
                get {
                    return $"{GlobalLayerId}@down";
                }
            }

            public bool IsHoverDefined {
                get {
                    return HoverAnimation.IsDefined;
                }
            }

            public bool IsDownDefined {
                get {
                    return DownAnimation.IsDefined;
                }
            }
        }

        public class PropMotionDefinition : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [Label("TRANSLATION_FACTOR")]
            // [Description("(Right, Up, Forward)")]
            public Vector3 TranslationFactor;

            [DataInput]
            [Label("ROTATION_FACTOR")]
            // [Description("(Pitch, Yaw, Roll)")]
            public Vector3 RotationFactor;

            public string GetHeader() {

                var iconT = TranslationFactor == Vector3.zero ? "🌑" : "🌕";
                var iconR = RotationFactor == Vector3.zero ? "🌑" : "🌕";

                return $"{iconT} T {TranslationFactor} | {iconR} R {RotationFactor}";
            }

            public bool IsDefined {
                get {
                    return TranslationFactor != Vector3.zero || RotationFactor != Vector3.zero;
                }
            }
        }

        public class SwitchPropMotionDefinition : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [Label("DIRECTION_UP")]
            public PropMotionDefinition D1;

            [DataInput]
            [Label("DIRECTION_UP_RIGHT")]
            public PropMotionDefinition D2;

            [DataInput]
            [Label("DIRECTION_RIGHT")]
            public PropMotionDefinition D3;

            [DataInput]
            [Label("DIRECTION_DOWN_RIGHT")]
            public PropMotionDefinition D4;

            [DataInput]
            [Label("DIRECTION_DOWN")]
            public PropMotionDefinition D5;

            [DataInput]
            [Label("DIRECTION_DOWN_LEFT")]
            public PropMotionDefinition D6;

            [DataInput]
            [Label("DIRECTION_LEFT")]
            public PropMotionDefinition D7;

            [DataInput]
            [Label("DIRECTION_UP_LEFT")]
            public PropMotionDefinition D8;

            public string GetHeader() {

                var definedCount = 0;
                if (D1.IsDefined) ++definedCount;
                if (D2.IsDefined) ++definedCount;
                if (D3.IsDefined) ++definedCount;
                if (D4.IsDefined) ++definedCount;
                if (D5.IsDefined) ++definedCount;
                if (D6.IsDefined) ++definedCount;
                if (D7.IsDefined) ++definedCount;
                if (D8.IsDefined) ++definedCount;

                return $"{definedCount}/8 Defined";
            }
        }

        public class AxisPropMotionDefinition : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [Label("MAX_INPUT_INFLUENCE")]
            public PropMotionDefinition Max;

            [DataInput]
            [Label("MIN_INPUT_INFLUENCE")]
            public PropMotionDefinition Min;

            public string GetHeader() {

                var maxIcon = Max.IsDefined ? "🌕" : "🌑";
                var str = $"{maxIcon} Max";

                if (!GetDataInputPort(nameof(Min)).Properties.hidden) {
                    var minIcon = Min.IsDefined ? "🌕" : "🌑";
                    str += $" | {minIcon} Min";
                }

                return str;
            }
        }
    }
}
