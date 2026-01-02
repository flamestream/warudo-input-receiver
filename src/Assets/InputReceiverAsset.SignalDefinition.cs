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
        bool _isInitialValidationNeeded = false;

        // Track previous animation state to detect enable/disable transitions
        bool PreviousAnimationEnabled = false;
        bool HasInitializedAnimationState = false;

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
            OnGeneratedProfileSelectionChange(SignalDefinitionGenerationSectionInstance.SignalDefinitionGenerationFromTemplateSectionInstance.Template);

            // Also watch for HeldProp changes to revalidate animator layer names
            Watch(nameof(HeldProp), delegate {
                ValidateAllPropAnimatorLayerNames();
            }, false);

            // Request initial validation to happen after asset is fully loaded
            _isInitialValidationNeeded = true;
        }

        void OnUpdateSignalDefinition() {
            if (Character == null) return;

            if (IsCharacterAnimationChangeRequested) {
                IsCharacterAnimationChangeRequested = false;
                HandleCharacterAnimationChange();
            }

            // Perform initial validation once after asset load
            if (_isInitialValidationNeeded) {
                _isInitialValidationNeeded = false;
                ValidateAllPropAnimatorLayerNames();
            }
        }

        protected void ValidateAllPropAnimatorLayerNames() {
            // Only validate if we have basic requirements
            if (HeldProp == null) return;

            // Validate all button definitions
            ButtonDefinitions?.ForEach(d => {
                if (d != null && d.IsValid && d.PropAnimation != null) {
                    d.PropAnimation.CheckPropAnimatorLayerName();
                }
            });

            // Validate all switch definitions
            SwitchDefinitions?.ForEach(d => {
                if (d != null && d.IsValid && d.PropAnimation != null) {
                    d.PropAnimation.D1?.CheckPropAnimatorLayerName();
                    d.PropAnimation.D2?.CheckPropAnimatorLayerName();
                    d.PropAnimation.D3?.CheckPropAnimatorLayerName();
                    d.PropAnimation.D4?.CheckPropAnimatorLayerName();
                    d.PropAnimation.D5?.CheckPropAnimatorLayerName();
                    d.PropAnimation.D6?.CheckPropAnimatorLayerName();
                    d.PropAnimation.D7?.CheckPropAnimatorLayerName();
                    d.PropAnimation.D8?.CheckPropAnimatorLayerName();
                }
            });

            // Validate all axis definitions
            AxisDefinitions?.ForEach(d => {
                if (d != null && d.IsValid && d.PropAnimation != null) {
                    d.PropAnimation.Min?.CheckPropAnimatorLayerName();
                    d.PropAnimation.Max?.CheckPropAnimatorLayerName();
                }
            });
        }

        virtual protected void OnGeneratedProfileSelectionChange(SignalTemplateType profile) {
            // Do nothing
        }

        protected void RequestCharacterAnimationChange() {
            IsCharacterAnimationChangeRequested = true;
        }

        void HandleCharacterAnimationChange() {

            if (Character == null) return;

            // Handle enable/disable state transitions for animation layers
            var currentAnimationEnabled = IsEnabled && IsControlEnabled;

            if (!HasInitializedAnimationState) {
                // On startup, just initialize tracking without processing
                PreviousAnimationEnabled = currentAnimationEnabled;
                HasInitializedAnimationState = true;
                if (!currentAnimationEnabled) {
                    return; // Don't process anything if disabled on startup
                }
            } else if (!currentAnimationEnabled) {
                // Only clear layers if we transitioned from enabled to disabled
                if (PreviousAnimationEnabled) {
                    ClearManagedLayers();
                }
                PreviousAnimationEnabled = currentAnimationEnabled;
                return;
            }

            // Update state tracking
            PreviousAnimationEnabled = currentAnimationEnabled;

            var unmanagedLayers = new List<OverlappingAnimationData>();
            var managedLayers = new List<OverlappingAnimationData>();
            foreach (var d in Character.OverlappingAnimations) {
                if (d.Animation.IsNullOrEmpty()) continue;
                if (d.CustomLayerID.IsNullOrEmpty()) continue;
                // Check if the layer ID starts with the prefix
                if (d.CustomLayerID.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX, StringComparison.Ordinal)) {
                    managedLayers.Add(d);
                } else {
                    unmanagedLayers.Add(d);
                }
            }

            // If SavedBindingData is null (during startup), preserve existing layers and return early
            var baseAnimationInfo = SavedBindingData?.BaseAnimationLayer;
            if (SavedBindingData == null) {
                // Don't modify anything during startup when binding data isn't loaded yet
                return;
            }

            var newManagedLayers = new List<OverlappingAnimationData>();

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
            } else {
                // When not enabled or no base animation, we should still preserve existing unmanaged layers
                // but clear any managed layers by not adding them back
            }

            if (newManagedLayers.Count > 0) {
                ShowToast($"CHARACTER_ANIMATION_LAYERS_SETUP_NOTIFICATION".Localized(new object[] {newManagedLayers.Count}));
            } else if (IsEnabled && IsControlEnabled) {
                ShowToast($"CHARACTER_ANIMATION_LAYERS_CLEANED_NOTIFICATION");
            }

            Character.SetDataInput($"{nameof(Character.OverlappingAnimations)}", unmanagedLayers.ToArray(), true);
            Character.Broadcast();
        }

        void ClearManagedLayers() {
            if (Character == null) return;

            var unmanagedLayers = new List<OverlappingAnimationData>();
            foreach (var d in Character.OverlappingAnimations) {
                // Skip empty animations entirely
                if (d.Animation.IsNullOrEmpty()) continue;

                // Keep layers that either:
                // 1. Have no CustomLayerID (unmanaged by any system)
                // 2. Have CustomLayerID but don't start with our prefix (managed by other systems)
                if (d.CustomLayerID.IsNullOrEmpty() ||
                    !d.CustomLayerID.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX, StringComparison.Ordinal)) {
                    unmanagedLayers.Add(d);
                }
            }

            // Clear the SignalDefinition registry when disabling
            SignalDefinition.Registry.Clear();

            // Update character with only unmanaged layers
            Character.SetDataInput($"{nameof(Character.OverlappingAnimations)}", unmanagedLayers.ToArray(), true);
            Character.Broadcast();

            ShowToast("CHARACTER_ANIMATION_LAYERS_CLEANED_NOTIFICATION".Localized());
        }

        public class SignalDefinitionGenerationSection : StructuredData<InputReceiverAsset>, ICollapsibleStructuredData {

            [Markdown]
            public string Instructions = "To start, it is recommended to generate the signal definitions from one of the method below.";

            [DataInput]
            [Label("")]
            public SignalDefinitionGenerationFromProp SignalDefinitionGenerationFromPropInstance;

            [DataInput]
            [Label("")]
            public SignalDefinitionGenerationFromTemplateSection SignalDefinitionGenerationFromTemplateSectionInstance;

            protected override void OnCreate() {
                base.OnCreate();
                SignalDefinitionGenerationFromTemplateSectionInstance.Parent = this;
                SignalDefinitionGenerationFromPropInstance.Parent = this;
            }

            public string GetHeader() {
                return "SIGNAL_DEFINITION_GENERATORS";
            }
        }

        public class SignalDefinitionGenerationFromProp : StructuredData<SignalDefinitionGenerationSection>, ICollapsibleStructuredData {

            [Trigger]
            [Label("IMPORT_SIGNAL_DEFINITIONS_FROM_PROP")]
            [Description("IMPORT_SIGNAL_DEFINITIONS_FROM_PROP_DESCRIPTION")]
            public async void TriggerImportSignalDefinitionsFromProp() {
                await Parent.Parent.ImportSignalDefinitionsFromProp();
            }

            [Trigger]
            [Label("EXPORT_SIGNAL_DEFINITIONS")]
            [Description("EXPORT_SIGNAL_DEFINITIONS_DESCRIPTION")]
            public void TriggerExportSignalDefinitions() {
                Parent.Parent.ExportSignalDefinitions();
            }

            public string GetHeader() {
                return "SIGNAL_DEFINITION_FROM_PROP_GENERATOR";
            }
        }

        public class SignalDefinitionGenerationFromTemplateSection : StructuredData<SignalDefinitionGenerationSection>, ICollapsibleStructuredData {

            [Markdown]
            public string Instructions = "If your prop does not come with signal definitions, you can start from a template profile for your controller type. This will create basic button definitions that you can further customize.";

            [DataInput]
            public SignalTemplateType Template;

            [Trigger]
            [Label("GENERATE_SIGNAL_DEFINITIONS_FROM_TEMPLATE")]
            public async void TriggerGenerateFromTemplate() {

                // Validate supported profiles
                var supportedProfiles = Parent.Parent.SupportedProfileTypes;
                if (!supportedProfiles.Contains(Template)) {
                    Context.Service.PromptMessage("ERROR", $"Profile definition for {Template.Localized()} is not supported by this asset at the moment. Please select one of following instead:\n\n{string.Join("\n", supportedProfiles.Select(p => p.Localized()))}");
                    return;
                }

                bool replaceWanted = await Context.Service.PromptConfirmation("WARNING", $"This will overwrite current signal definitions with a {Template.Localized()} profile. Do you want to continue?");
                if (!replaceWanted) return;

                Parent.Parent.GenerateButtonDefinitions(Template);
            }

            public string GetHeader() {
                return "SIGNAL_DEFINITION_FROM_TEMPLATE_GENERATOR";
            }

            protected override void OnCreate() {
                base.OnCreate();
                Watch(nameof(Template), delegate {
                    Parent.Parent.OnGeneratedProfileSelectionChange(Template);
                }, false);
            }

            public void SetProfileDescription(string msg) {
                GetDataInputPort(nameof(Template)).Properties.description = msg;
                BroadcastDataInputProperties(nameof(Template));
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

            [DataInput(50)]
            [Label("SWITCH_VIRTUAL_DEFINITION")]
            public SwitchVirtualDefinition VirtualDefinitionSet;

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
                string type = VirtualDefinitionSet.Enabled ? "Virtual Switch" : "Switch";
                string name = $"[{Index}] {icon} {AssignedCharacterLayer.Localized()} {type} - {Label}";
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
                Watch(nameof(PropAnimation), () => {
                    PropAnimation?.Min?.CheckPropAnimatorLayerName();
                    PropAnimation?.Max?.CheckPropAnimatorLayerName();
                }, false);
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
                AnimationDataD1.Index = 1;
                AnimationDataD2.Parent = this;
                AnimationDataD2.Index = 2;
                AnimationDataD3.Parent = this;
                AnimationDataD3.Index = 3;
                AnimationDataD4.Parent = this;
                AnimationDataD4.Index = 4;
                AnimationDataD5.Parent = this;
                AnimationDataD5.Index = 5;
                AnimationDataD6.Parent = this;
                AnimationDataD6.Index = 6;
                AnimationDataD7.Parent = this;
                AnimationDataD7.Index = 7;
                AnimationDataD8.Parent = this;
                AnimationDataD8.Index = 8;

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
            public Vector3 TranslationFactor;

            [DataInput]
            [Label("ROTATION_FACTOR")]
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

        // Define class used by switch definition to define "virtual switches". Where the idea is to use 4 button IDs (up, down, left, right) to define a switch direction.
        // This is to be used inside a switch signal definition to define which buttons correspond to which switch direction.
        public class SwitchVirtualDefinition : StructuredData, ICollapsibleStructuredData {

            [Markdown(10)]
            public string Description = "SWITCH_VIRTUAL_DESCRIPTION".Localized();

            [DataInput(20)]
            public bool Enabled;

            [DataInput(30)]
            [Label("SWITCH_VIRTUAL_BUTTON_UP_LABEL")]
            public int UpId = 0;

            [DataInput(40)]
            [Label("SWITCH_VIRTUAL_BUTTON_DOWN_LABEL")]
            public int DownId = 1;

            [DataInput(50)]
            [Label("SWITCH_VIRTUAL_BUTTON_LEFT_LABEL")]
            public int LeftId = 2;

            [DataInput(60)]
            [Label("SWITCH_VIRTUAL_BUTTON_RIGHT_LABEL")]
            public int RightId = 3;

            public string GetHeader() {
                return Enabled ? "✅ Enabled and overriding slot" : "❌ Disabled";
            }
        }

        ///
        /// -- Signal Definition Export/Import Functions --
        ///

        private async System.Threading.Tasks.Task ImportSignalDefinitionsFromProp() {
            try {
                // Get AnimSettingsReader component from prop gameObject
                if (HeldProp == null) {
                    Context.Service.PromptMessage("Import Error", "❌ No prop is currently bound. Please bind a prop first.");
                    return;
                }

                var gameObject = HeldProp.GameObject;
                if (gameObject == null) {
                    Context.Service.PromptMessage("Import Error", "❌ Prop gameObject not available. Please check prop binding.");
                    return;
                }

                Newtonsoft.Json.Linq.JObject animationSettingsData = null;

                // Attempt to get animation settings JSON
                try {
                    Component animSettingsReader = null;

                    // Look for any component with "AnimSettingsReader" in its name
                    var allComponents = gameObject.GetComponents<Component>();
                    foreach (var component in allComponents) {
                        if (component.GetType().Name.Contains("AnimSettingsReader")) {
                            animSettingsReader = component;
                            break;
                        }
                    }

                    if (animSettingsReader == null) {
                        Context.Service.PromptMessage("Import Error", "❌ Prop does not have an AnimSettingsReader component.");
                        return;
                    }

                    // Get animation settings
                    var getRawJsonMethod = animSettingsReader.GetType().GetMethod("getRawJson");
                    if (getRawJsonMethod != null) {
                        var jsonString = getRawJsonMethod.Invoke(animSettingsReader, null) as string;
                        if (string.IsNullOrEmpty(jsonString)) {
                            Context.Service.PromptMessage("Import Error", "❌ AnimSettingsReader returned empty JSON string.");
                            return;
                        }
                        // Parse JSON string to JObject
                        animationSettingsData = Newtonsoft.Json.Linq.JObject.Parse(jsonString);
                    } else {
                        Context.Service.PromptMessage("Import Error", "❌ AnimSettingsReader component does not have a 'getRawJson' method.");
                        return;
                    }
                } catch (Newtonsoft.Json.JsonException jsonEx) {
                    Context.Service.PromptMessage("Import Error", $"❌ Failed to parse JSON from AnimSettingsReader: {jsonEx.Message}");
                    return;
                } catch (System.Exception reflectionEx) {
                    Context.Service.PromptMessage("Import Error", $"❌ Reflection approach failed: {reflectionEx.Message}");
                    return;
                }

                // Validation
                if (animationSettingsData == null) {
                    Context.Service.PromptMessage("Import Error", "❌ AnimSettingsReader returned null data.");
                    return;
                }
                Newtonsoft.Json.Linq.JObject importData;
                try {
                    if (animationSettingsData["version"] != null && animationSettingsData["data"] != null) {
                        importData = animationSettingsData["data"] as Newtonsoft.Json.Linq.JObject;

                        if (importData == null) {
                            Context.Service.PromptMessage("Import Error", "❌ Failed to parse 'data' field as JObject.");
                            return;
                        }

                        // Check assetClass compatibility
                        if (animationSettingsData["assetClass"] != null) {
                            var exportedAssetClass = animationSettingsData["assetClass"].ToString();
                            var currentAssetClass = GetType().Name;

                            if (exportedAssetClass != currentAssetClass) {
                                var warningMessage = $"⚠️ **Asset Class Mismatch Detected**\n\n" +
                                    $"The prop was configured for a different asset:\n" +
                                    $"   • Prop asset class: **{exportedAssetClass}**\n" +
                                    $"   • Current asset class: **{currentAssetClass}**\n\n" +
                                    $"This may cause compatibility issues. The signal definitions may not match your current setup.\n\n" +
                                    $"**Recommended action:**\n" +
                                    $"1. First, generate signal definitions from a template that matches your controller\n" +
                                    $"2. Then, rerun the prop import function to apply settings based on existing definitions, matching by label\n\n" +
                                    $"⚠️ **Note:** Even if you follow the recommended steps, further manual adjustments may be needed to ensure full compatibility.";

                                Context.Service.PromptMessage("Asset Class Mismatch", warningMessage);
                            }
                        }
                    } else {
                        // Legacy format - treat entire root as data
                        importData = animationSettingsData;
                    }
                } catch (Exception ex) {
                    Context.Service.PromptMessage("Import Error", $"❌ Failed to process animation settings data:\n{ex.Message}");
                    return;
                }

                // // Scan signal definition labels and validate
                // var importLabels = importData.Properties().Select(p => p.Name).ToList();
                // var existingLabels = new HashSet<string>();

                // // Collect all existing signal definition labels
                // if (ButtonDefinitions != null) {
                //     foreach (var def in ButtonDefinitions) {
                //         if (def.IsValid && !string.IsNullOrEmpty(def.Label)) {
                //             existingLabels.Add(def.Label);
                //         }
                //     }
                // }

                // if (SwitchDefinitions != null) {
                //     foreach (var def in SwitchDefinitions) {
                //         if (def.IsValid && !string.IsNullOrEmpty(def.Label)) {
                //             existingLabels.Add(def.Label);
                //         }
                //     }
                // }

                // if (AxisDefinitions != null) {
                //     foreach (var def in AxisDefinitions) {
                //         if (def.IsValid && !string.IsNullOrEmpty(def.Label)) {
                //             existingLabels.Add(def.Label);
                //         }
                //     }
                // }

                // // Find missing labels
                // var missingLabels = importLabels.Where(label => !existingLabels.Contains(label)).ToList();

                // // Handle validation results
                // if (missingLabels.Count > 0) {
                //     var errorMessage = "❌ **Missing Signal Definitions**\n\n" +
                //                      "The following labels from the prop animation settings do not have corresponding signal definitions:\n\n" +
                //                      string.Join("\n", missingLabels.Select(l => $"   • '{l}'")) + "\n\n" +
                //                      "Please create signal definitions with these labels first, or ensure the labels match exactly.";
                //     Context.Service.PromptMessage("Import Validation Failed", errorMessage);
                //     return;
                // }
                // var matchedCount = importLabels.Count;

                // Show confirmation dialog
                var confirmMessage = $"Signal definitions found on prop. This will overwrite existing animation settings for all matched signal definitions. Do you want to continue?";

                bool confirmed = await Context.Service.PromptConfirmation("Import Signal Definition from Prop", confirmMessage);
                if (!confirmed) return;

                // Use existing ImportSignalDefinitions method
                ImportSignalDefinitions(importData);

            } catch (Exception ex) {
                Context.Service.PromptMessage("Import Error", $"❌ An unexpected error occurred during import:\n{ex.Message}");
            }
        }

        private void ExportSignalDefinitions() {
            var signalData = new Dictionary<string, object>();
            var duplicateLabels = new List<string>();
            var unlabeledSignals = new List<string>();

            // Export Button Definitions
            if (ButtonDefinitions != null && ButtonDefinitions.Length > 0) {
                foreach (var buttonDef in ButtonDefinitions) {
                    if (!buttonDef.IsValid) continue;

                    if (string.IsNullOrEmpty(buttonDef.Label)) {
                        unlabeledSignals.Add($"Button[{buttonDef.Index}]");
                        continue;
                    }

                    if (signalData.ContainsKey(buttonDef.Label)) {
                        if (!duplicateLabels.Contains(buttonDef.Label)) {
                            duplicateLabels.Add(buttonDef.Label);
                        }
                    } else {
                        var buttonData = SerializeButtonDefinition(buttonDef);
                        // Add character animation data to button definition
                        ((Dictionary<string, object>)buttonData)["characterAnimation"] = SerializeCharacterButtonAnimation(buttonDef.CharacterAnimation);
                        signalData[buttonDef.Label] = buttonData;
                    }
                }
            }

            // Export Switch Definitions
            if (SwitchDefinitions != null && SwitchDefinitions.Length > 0) {
                foreach (var switchDef in SwitchDefinitions) {
                    if (!switchDef.IsValid) continue;

                    if (string.IsNullOrEmpty(switchDef.Label)) {
                        unlabeledSignals.Add($"Switch[{switchDef.Index}]");
                        continue;
                    }

                    if (signalData.ContainsKey(switchDef.Label)) {
                        if (!duplicateLabels.Contains(switchDef.Label)) {
                            duplicateLabels.Add(switchDef.Label);
                        }
                    } else {
                        var switchData = SerializeSwitchDefinition(switchDef);
                        signalData[switchDef.Label] = switchData;
                    }
                }
            }

            // Export Axis Definitions
            if (AxisDefinitions != null && AxisDefinitions.Length > 0) {
                foreach (var axisDef in AxisDefinitions) {
                    if (!axisDef.IsValid) continue;

                    if (string.IsNullOrEmpty(axisDef.Label)) {
                        unlabeledSignals.Add($"Axis[{axisDef.Index}]");
                        continue;
                    }

                    if (signalData.ContainsKey(axisDef.Label)) {
                        if (!duplicateLabels.Contains(axisDef.Label)) {
                            duplicateLabels.Add(axisDef.Label);
                        }
                    } else {
                        var axisData = SerializeAxisDefinition(axisDef);
                        // Add character animation data to axis definition
                        ((Dictionary<string, object>)axisData)["characterAnimation"] = SerializeAxisCharacterAnimation(axisDef.CharacterAnimation);
                        signalData[axisDef.Label] = axisData;
                    }
                }
            }

            // Create versioned export structure
            var exportData = new Dictionary<string, object> {
                ["version"] = 1,
                ["assetClass"] = GetType().Name,
                ["data"] = signalData
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(exportData, Newtonsoft.Json.Formatting.Indented);

            var totalCount = signalData.Count;

            var errorInfo = "";
            if (unlabeledSignals.Count > 0) {
                errorInfo += $"\n\n❌ **Unlabeled Signals (not exported):**\n{string.Join("\n", unlabeledSignals.Select(s => $"   • {s} - missing label"))}\n";
            }
            if (duplicateLabels.Count > 0) {
                errorInfo += $"\n\n⚠️ **Duplicate Labels:**\n{string.Join("\n", duplicateLabels.Select(l => $"   • '{l}' - appears multiple times, only first occurrence exported"))}\n";
            }

            var hasErrors = unlabeledSignals.Count > 0 || duplicateLabels.Count > 0;
            string header = "Signal Definitions Export";
            string message = "";

            if (hasErrors) {
                message = $"⚠️ **Export Error Found. Please review the issues below and try again.**\n{errorInfo}";
            } else {
                message = $"ℹ️ Instructions\n" +
                          $"Copy the JSON below to the Prop settings in Unity. See the documentation at https://github.com/flamestream/warudo-input-receiver/wiki for more details.\n\n" +
                          $"{json}";
            }

            Context.Service.PromptMessage(header, message, true);
        }

        private void ImportSignalDefinitions(Newtonsoft.Json.Linq.JObject importData) {
            if (importData == null || !importData.HasValues) {
                return;
            }

            try {

                var importedCount = 0;
                var buttonList = new List<(string label, Newtonsoft.Json.Linq.JObject data)>();
                var switchList = new List<(string label, Newtonsoft.Json.Linq.JObject data)>();
                var axisList = new List<(string label, Newtonsoft.Json.Linq.JObject data)>();

                foreach (var property in importData.Properties()) {
                    var label = property.Name;
                    var signalData = property.Value as Newtonsoft.Json.Linq.JObject;
                    if (signalData == null) continue;

                    if (signalData["assignedCharacterLayer"] != null) {
                        // Determine type based on structure
                        if (signalData["propAnimation"] != null) {
                            // Button
                            buttonList.Add((label, signalData));
                        } else if (signalData["propAnimations"] != null) {
                            var propAnims = signalData["propAnimations"] as Newtonsoft.Json.Linq.JObject;
                            if (propAnims != null && propAnims["d1"] != null) {
                                // Switch (has d1, d2, etc.)
                                switchList.Add((label, signalData));
                            } else {
                                // Axis (has max/min)
                                axisList.Add((label, signalData));
                            }
                        }
                    }
                }

                // Import the collected definitions
                if (buttonList.Count > 0) {
                    importedCount += ImportButtonDefinitions(buttonList);
                }
                if (switchList.Count > 0) {
                    importedCount += ImportSwitchDefinitions(switchList);
                }
                if (axisList.Count > 0) {
                    importedCount += ImportAxisDefinitions(axisList);
                }

                IsCharacterAnimationChangeRequested = true;
                _isInitialValidationNeeded = true;

                Context.Service.PromptMessage("Import Complete", $"✅ Successfully imported {importedCount} signal definitions");
            }
            catch (Exception ex) {
                Context.Service.PromptMessage("Import Error", $"❌ Failed to import signal definitions:\n{ex.Message}");
            }
        }

        private object SerializeButtonDefinition(ButtonDefinition buttonDef) {
            return new Dictionary<string, object> {
                ["index"] = buttonDef.Index,
                ["label"] = buttonDef.Label ?? "",
                ["assignedCharacterLayer"] = buttonDef.AssignedCharacterLayer.ToString(),
                ["propAnimation"] = SerializePropAnimation(buttonDef.PropAnimation),
                ["propMotion"] = SerializePropMotion(buttonDef.PropMotion)
            };
        }

        private object SerializeSwitchDefinition(SwitchDefinition switchDef) {
            return new Dictionary<string, object> {
                ["index"] = switchDef.Index,
                ["label"] = switchDef.Label ?? "",
                ["assignedCharacterLayer"] = switchDef.AssignedCharacterLayer.ToString(),
                ["propAnimations"] = new Dictionary<string, object> {
                    ["d1"] = SerializePropAnimation(switchDef.PropAnimation?.D1),
                    ["d2"] = SerializePropAnimation(switchDef.PropAnimation?.D2),
                    ["d3"] = SerializePropAnimation(switchDef.PropAnimation?.D3),
                    ["d4"] = SerializePropAnimation(switchDef.PropAnimation?.D4),
                    ["d5"] = SerializePropAnimation(switchDef.PropAnimation?.D5),
                    ["d6"] = SerializePropAnimation(switchDef.PropAnimation?.D6),
                    ["d7"] = SerializePropAnimation(switchDef.PropAnimation?.D7),
                    ["d8"] = SerializePropAnimation(switchDef.PropAnimation?.D8)
                },
                ["propMotions"] = new Dictionary<string, object> {
                    ["d1"] = SerializePropMotion(switchDef.PropMotionSet?.D1),
                    ["d2"] = SerializePropMotion(switchDef.PropMotionSet?.D2),
                    ["d3"] = SerializePropMotion(switchDef.PropMotionSet?.D3),
                    ["d4"] = SerializePropMotion(switchDef.PropMotionSet?.D4),
                    ["d5"] = SerializePropMotion(switchDef.PropMotionSet?.D5),
                    ["d6"] = SerializePropMotion(switchDef.PropMotionSet?.D6),
                    ["d7"] = SerializePropMotion(switchDef.PropMotionSet?.D7),
                    ["d8"] = SerializePropMotion(switchDef.PropMotionSet?.D8)
                },
                ["characterAnimation"] = SerializeSwitchCharacterAnimation(switchDef.CharacterAnimation),
                ["virtualDefinition"] = SerializeVirtualSwitchDefinition(switchDef.VirtualDefinitionSet)
            };
        }

        private object SerializeAxisDefinition(AxisDefinition axisDef) {
            return new Dictionary<string, object> {
                ["index"] = axisDef.Index,
                ["label"] = axisDef.Label ?? "",
                ["assignedCharacterLayer"] = axisDef.AssignedCharacterLayer.ToString(),
                ["assignedGroup"] = axisDef.AssignedGroup.ToString(),
                ["deadzoneThreshold"] = axisDef.DeadzoneThreshold,
                ["neutralState"] = axisDef.NeutralState.ToString(),
                ["propAnimations"] = new Dictionary<string, object> {
                    ["max"] = SerializePropAnimation(axisDef.PropAnimation?.Max),
                    ["min"] = SerializePropAnimation(axisDef.PropAnimation?.Min)
                },
                ["propMotions"] = new Dictionary<string, object> {
                    ["max"] = SerializePropMotion(axisDef.PropMotionSet?.Max),
                    ["min"] = SerializePropMotion(axisDef.PropMotionSet?.Min)
                }
            };
        }

        private object SerializePropAnimation(PropAnimationDefinition propAnim) {
            if (propAnim == null) return null;

            var result = new Dictionary<string, object> {
                ["animatorLayerName"] = propAnim.AnimatorLayerName ?? ""
            };

            if (propAnim is TransitionablePropAnimationDefinition transitionable) {
                result["transitionDown"] = SerializeTransition(transitionable.TransitionDown);
                result["transitionUp"] = SerializeTransition(transitionable.TransitionUp);
            }

            return result;
        }

        private object SerializeTransition(Transition transition) {
            if (transition == null) return null;

            var result = new Dictionary<string, object> {
                ["time"] = transition.Time,
                ["ease"] = transition.Ease.ToString()
            };

            if (transition is DelayableTransition delayable) {
                result["delay"] = delayable.DelayTime;
            }

            return result;
        }

        private object SerializePropMotion(PropMotionDefinition propMotion) {
            if (propMotion == null) return null;

            return new Dictionary<string, object> {
                ["translationFactor"] = new Dictionary<string, object> {
                    ["x"] = propMotion.TranslationFactor.x,
                    ["y"] = propMotion.TranslationFactor.y,
                    ["z"] = propMotion.TranslationFactor.z
                },
                ["rotationFactor"] = new Dictionary<string, object> {
                    ["x"] = propMotion.RotationFactor.x,
                    ["y"] = propMotion.RotationFactor.y,
                    ["z"] = propMotion.RotationFactor.z
                }
            };
        }

        private object SerializeVirtualSwitchDefinition(SwitchVirtualDefinition virtualDef) {
            if (virtualDef == null) return null;

            return new Dictionary<string, object> {
                ["enabled"] = virtualDef.Enabled,
                ["upId"] = virtualDef.UpId,
                ["downId"] = virtualDef.DownId,
                ["leftId"] = virtualDef.LeftId,
                ["rightId"] = virtualDef.RightId
            };
        }

        private object SerializeCharacterButtonAnimation(CharacterButtonAnimationDefinition charAnim) {
            if (charAnim == null) return null;

            return new Dictionary<string, object> {
                ["animationData"] = SerializeHoverableStatefulTransitionAnimationData(charAnim.AnimationData),
                ["maskedBodyParts"] = charAnim.MaskedBodyParts?.Select(mbp => mbp.ToString()).ToArray() ?? new string[0]
            };
        }

        private object SerializeSwitchCharacterAnimation(SwitchCharacterAnimationDefinition charAnim) {
            if (charAnim == null) return null;

            return new Dictionary<string, object> {
                ["directions"] = new Dictionary<string, object> {
                    ["d1"] = SerializeSwitchStateAnimationData(charAnim.AnimationDataD1),
                    ["d2"] = SerializeSwitchStateAnimationData(charAnim.AnimationDataD2),
                    ["d3"] = SerializeSwitchStateAnimationData(charAnim.AnimationDataD3),
                    ["d4"] = SerializeSwitchStateAnimationData(charAnim.AnimationDataD4),
                    ["d5"] = SerializeSwitchStateAnimationData(charAnim.AnimationDataD5),
                    ["d6"] = SerializeSwitchStateAnimationData(charAnim.AnimationDataD6),
                    ["d7"] = SerializeSwitchStateAnimationData(charAnim.AnimationDataD7),
                    ["d8"] = SerializeSwitchStateAnimationData(charAnim.AnimationDataD8)
                },
                ["base"] = SerializeBaseRevertibleAnimationData(charAnim.Base),
                ["transitions"] = new Dictionary<string, object> {
                    ["hover"] = SerializeTransition(charAnim.TransitionHover),
                    ["down"] = SerializeTransition(charAnim.TransitionDown),
                    ["up"] = SerializeTransition(charAnim.TransitionUp)
                },
                ["maskedBodyParts"] = charAnim.MaskedBodyParts?.Select(mbp => mbp.ToString()).ToArray() ?? new string[0]
            };
        }

        private object SerializeAxisCharacterAnimation(AxisCharacterAnimationDefinition charAnim) {
            if (charAnim == null) return null;

            return new Dictionary<string, object> {
                ["base"] = SerializeBaseRevertibleAnimationData(charAnim.Base),
                ["max"] = SerializeAnimationData(charAnim.Max),
                ["min"] = SerializeAnimationData(charAnim.Min),
                ["maskedBodyParts"] = charAnim.MaskedBodyParts?.Select(mbp => mbp.ToString()).ToArray() ?? new string[0]
            };
        }

        private object SerializeHoverableStatefulTransitionAnimationData(HoverableStatefulTransitionAnimationData animData) {
            if (animData == null) return null;

            return new Dictionary<string, object> {
                ["hoverAnimationData"] = SerializeBaseRevertibleTransitionAnimationData(animData.HoverAnimationData),
                ["downAnimationData"] = SerializeStatefulTransitionAnimationData(animData.DownAnimationData)
            };
        }

        private object SerializeSwitchStateAnimationData(SwitchStateAnimationData animData) {
            if (animData == null) return null;

            return new Dictionary<string, object> {
                ["hoverAnimation"] = SerializeAnimationData(animData.HoverAnimation),
                ["downAnimation"] = SerializeAnimationData(animData.DownAnimation)
            };
        }

        private object SerializeBaseRevertibleAnimationData(BaseRevertibleAnimationData animData) {
            if (animData == null) return null;

            var result = new Dictionary<string, object> {
                ["source"] = animData.Source ?? "",
                ["isReturnToBaseWanted"] = animData.IsReturnToBaseWanted
            };

            if (animData is BaseRevertibleTransitionAnimationData transitionData) {
                result["transition"] = SerializeTransition(transitionData.Transition);
            }

            return result;
        }

        private object SerializeStatefulTransitionAnimationData(StatefulTransitionAnimationData animData) {
            if (animData == null) return null;

            return new Dictionary<string, object> {
                ["source"] = animData.Source ?? "",
                ["transition"] = SerializeTransition(animData.Transition),
                ["upTransition"] = SerializeTransition(animData.UpTransition)
            };
        }

        private object SerializeBaseRevertibleTransitionAnimationData(BaseRevertibleTransitionAnimationData animData) {
            if (animData == null) return null;

            var result = new Dictionary<string, object> {
                ["source"] = animData.Source ?? "",
                ["isReturnToBaseWanted"] = animData.IsReturnToBaseWanted
            };

            if (animData is BaseRevertibleTransitionAnimationData transitionData) {
                result["transition"] = SerializeTransition(transitionData.Transition);
            }

            return result;
        }

        private object SerializeAnimationData(AnimationData animData) {
            if (animData == null) return null;

            return new Dictionary<string, object> {
                ["source"] = animData.Source ?? ""
            };
        }

        private int ImportButtonDefinitions(List<(string label, Newtonsoft.Json.Linq.JObject data)> buttonList) {
            var importedCount = 0;

            var currentDefinitions = ButtonDefinitions.ToList();

            foreach (var (label, buttonData) in buttonList) {
                var existing = currentDefinitions.FirstOrDefault(d => d.IsValid && d.Label == label);

                if (existing == null) {
                    // Create new definition if it doesn't exist
                    var newDef = StructuredData.Create<ButtonDefinition>();
                    newDef.IsValid = true;
                    newDef.Label = label;

                    // Read index from import data
                    if (buttonData["index"] != null) {
                        newDef.Index = buttonData["index"].ToObject<int>();
                    }

                    currentDefinitions.Add(newDef);
                    existing = newDef;
                }

                if (Enum.TryParse<Layer>(buttonData["assignedCharacterLayer"]?.ToString(), out var layer)) {
                    existing.AssignedCharacterLayer = layer;
                }

                if (buttonData["propAnimation"] != null) {
                    ImportPropAnimation(buttonData["propAnimation"], existing.PropAnimation);
                }

                if (buttonData["propMotion"] != null) {
                    ImportPropMotion(buttonData["propMotion"], existing.PropMotion);
                }

                // Import character animation data if present
                if (buttonData["characterAnimation"] != null) {
                    ImportCharacterButtonAnimation(buttonData["characterAnimation"], existing.CharacterAnimation);
                }

                importedCount++;
            }

            SetDataInput(nameof(ButtonDefinitions), currentDefinitions.ToArray(), true);

            return importedCount;
        }

        private void EnsureButtonDefinitionsUpTo(int maxIndex) {
            if (maxIndex >= MAX_BUTTON_COUNT) return;

            var newButtonDefinitions = new List<ButtonDefinition>();
            for (int i = 0; i <= maxIndex; i++) {
                var old = ButtonDefinitions?.FirstOrDefault(d => d.IsValid && d.Index == i);
                if (old != null) {
                    newButtonDefinitions.Add(old);
                } else {
                    var d = StructuredData.Create<ButtonDefinition>();
                    d.IsValid = true;
                    d.Index = i;
                    newButtonDefinitions.Add(d);
                }
            }
            SetDataInput(nameof(ButtonDefinitions), newButtonDefinitions.ToArray(), true);
        }

        private int ImportSwitchDefinitions(List<(string label, Newtonsoft.Json.Linq.JObject data)> switchList) {
            var importedCount = 0;

            var currentDefinitions = SwitchDefinitions.ToList();

            foreach (var (label, switchData) in switchList) {
                var existing = currentDefinitions.FirstOrDefault(d => d.IsValid && d.Label == label);

                if (existing == null) {
                    // Create new definition if it doesn't exist
                    var newDef = StructuredData.Create<SwitchDefinition>();
                    newDef.IsValid = true;
                    newDef.Label = label;

                    // Read index from import data
                    if (switchData["index"] != null) {
                        newDef.Index = switchData["index"].ToObject<int>();
                    }

                    currentDefinitions.Add(newDef);
                    existing = newDef;
                }

                if (Enum.TryParse<Layer>(switchData["assignedCharacterLayer"]?.ToString(), out var layer)) {
                    existing.AssignedCharacterLayer = layer;
                }

                if (switchData["propAnimations"] != null) {
                    var propAnims = switchData["propAnimations"] as Newtonsoft.Json.Linq.JObject;
                    if (propAnims != null) {
                        ImportPropAnimation(propAnims["d1"], existing.PropAnimation?.D1);
                        ImportPropAnimation(propAnims["d2"], existing.PropAnimation?.D2);
                        ImportPropAnimation(propAnims["d3"], existing.PropAnimation?.D3);
                        ImportPropAnimation(propAnims["d4"], existing.PropAnimation?.D4);
                        ImportPropAnimation(propAnims["d5"], existing.PropAnimation?.D5);
                        ImportPropAnimation(propAnims["d6"], existing.PropAnimation?.D6);
                        ImportPropAnimation(propAnims["d7"], existing.PropAnimation?.D7);
                        ImportPropAnimation(propAnims["d8"], existing.PropAnimation?.D8);
                    }
                }

                if (switchData["propMotions"] != null) {
                    var propMotions = switchData["propMotions"] as Newtonsoft.Json.Linq.JObject;
                    if (propMotions != null) {
                        ImportPropMotion(propMotions["d1"], existing.PropMotionSet?.D1);
                        ImportPropMotion(propMotions["d2"], existing.PropMotionSet?.D2);
                        ImportPropMotion(propMotions["d3"], existing.PropMotionSet?.D3);
                        ImportPropMotion(propMotions["d4"], existing.PropMotionSet?.D4);
                        ImportPropMotion(propMotions["d5"], existing.PropMotionSet?.D5);
                        ImportPropMotion(propMotions["d6"], existing.PropMotionSet?.D6);
                        ImportPropMotion(propMotions["d7"], existing.PropMotionSet?.D7);
                        ImportPropMotion(propMotions["d8"], existing.PropMotionSet?.D8);
                    }
                }

                // Import character animation data if present
                if (switchData["characterAnimation"] != null) {
                    ImportSwitchCharacterAnimation(switchData["characterAnimation"], existing.CharacterAnimation);
                }

                // Import virtual definition if present
                if (switchData["virtualDefinition"] != null) {
                    ImportVirtualSwitchDefinition(switchData["virtualDefinition"], existing.VirtualDefinitionSet);
                }

                importedCount++;
            }

            SetDataInput(nameof(SwitchDefinitions), currentDefinitions.ToArray(), true);

            return importedCount;
        }

        private void EnsureSwitchDefinitionsUpTo(int maxIndex) {
            if (maxIndex >= MAX_SWITCH_COUNT) return;

            var newSwitchDefinitions = new List<SwitchDefinition>();
            for (int i = 0; i <= maxIndex; i++) {
                var old = SwitchDefinitions?.FirstOrDefault(d => d.IsValid && d.Index == i);
                if (old != null) {
                    newSwitchDefinitions.Add(old);
                } else {
                    var d = StructuredData.Create<SwitchDefinition>();
                    d.IsValid = true;
                    d.Index = i;
                    newSwitchDefinitions.Add(d);
                }
            }
            SetDataInput(nameof(SwitchDefinitions), newSwitchDefinitions.ToArray(), true);
        }

        private int ImportAxisDefinitions(List<(string label, Newtonsoft.Json.Linq.JObject data)> axisList) {
            var importedCount = 0;

            var currentDefinitions = AxisDefinitions.ToList();

            foreach (var (label, axisData) in axisList) {
                var existing = currentDefinitions.FirstOrDefault(d => d.IsValid && d.Label == label);

                if (existing == null) {
                    // Create new definition if it doesn't exist
                    var newDef = StructuredData.Create<AxisDefinition>();
                    newDef.IsValid = true;
                    newDef.Label = label;

                    // Read index from import data
                    if (axisData["index"] != null) {
                        newDef.Index = axisData["index"].ToObject<int>();
                    }

                    currentDefinitions.Add(newDef);
                    existing = newDef;
                }

                if (Enum.TryParse<Layer>(axisData["assignedCharacterLayer"]?.ToString(), out var layer)) {
                    existing.AssignedCharacterLayer = layer;
                }
                if (Enum.TryParse<AxisGroup>(axisData["assignedGroup"]?.ToString(), out var group)) {
                    existing.AssignedGroup = group;
                }
                if (axisData["deadzoneThreshold"] != null) {
                    existing.DeadzoneThreshold = axisData["deadzoneThreshold"].ToObject<float>();
                }
                if (Enum.TryParse<AxisNeutralState>(axisData["neutralState"]?.ToString(), out var neutralState)) {
                    existing.NeutralState = neutralState;
                }

                if (axisData["propAnimations"] != null) {
                    var propAnims = axisData["propAnimations"] as Newtonsoft.Json.Linq.JObject;
                    if (propAnims != null) {
                        ImportPropAnimation(propAnims["max"], existing.PropAnimation?.Max);
                        ImportPropAnimation(propAnims["min"], existing.PropAnimation?.Min);
                    }
                }

                if (axisData["propMotions"] != null) {
                    var propMotions = axisData["propMotions"] as Newtonsoft.Json.Linq.JObject;
                    if (propMotions != null) {
                        ImportPropMotion(propMotions["max"], existing.PropMotionSet?.Max);
                        ImportPropMotion(propMotions["min"], existing.PropMotionSet?.Min);
                    }
                }

                // Import character animation data if present
                if (axisData["characterAnimation"] != null) {
                    ImportAxisCharacterAnimation(axisData["characterAnimation"], existing.CharacterAnimation);
                }

                importedCount++;
            }

            SetDataInput(nameof(AxisDefinitions), currentDefinitions.ToArray(), true);

            return importedCount;
        }

        private void EnsureAxisDefinitionsUpTo(int maxIndex) {
            if (maxIndex >= MAX_AXIS_COUNT) return;

            var newAxisDefinitions = new List<AxisDefinition>();
            for (int i = 0; i <= maxIndex; i++) {
                var old = AxisDefinitions?.FirstOrDefault(d => d.IsValid && d.Index == i);
                if (old != null) {
                    newAxisDefinitions.Add(old);
                } else {
                    var d = StructuredData.Create<AxisDefinition>();
                    d.IsValid = true;
                    d.Index = i;
                    newAxisDefinitions.Add(d);
                }
            }
            SetDataInput(nameof(AxisDefinitions), newAxisDefinitions.ToArray(), true);
        }

        private void ImportPropAnimation(Newtonsoft.Json.Linq.JToken propAnimData, PropAnimationDefinition target) {
            if (propAnimData == null || target == null) return;

            var propObj = propAnimData as Newtonsoft.Json.Linq.JObject;
            if (propObj == null) return;

            if (propObj["animatorLayerName"] != null) {
                target.AnimatorLayerName = propObj["animatorLayerName"].ToString();
            }

            if (target is TransitionablePropAnimationDefinition transitionable) {
                if (propObj["transitionDown"] != null) {
                    ImportTransition(propObj["transitionDown"], transitionable.TransitionDown);
                }
                if (propObj["transitionUp"] != null) {
                    ImportTransition(propObj["transitionUp"], transitionable.TransitionUp);
                }
            }
        }

        private void ImportTransition(Newtonsoft.Json.Linq.JToken transitionData, Transition target) {
            if (transitionData == null || target == null) return;

            var transObj = transitionData as Newtonsoft.Json.Linq.JObject;
            if (transObj == null) return;

            if (transObj["time"] != null) {
                target.Time = transObj["time"].ToObject<float>();
            }
            if (transObj["ease"] != null && Enum.TryParse<Ease>(transObj["ease"].ToString(), out var ease)) {
                target.Ease = ease;
            }
            if (target is DelayableTransition delayable && transObj["delay"] != null) {
                delayable.DelayTime = transObj["delay"].ToObject<float>();
            }
        }

        private void ImportPropMotion(Newtonsoft.Json.Linq.JToken propMotionData, PropMotionDefinition target) {
            if (propMotionData == null || target == null) return;

            var motionObj = propMotionData as Newtonsoft.Json.Linq.JObject;
            if (motionObj == null) return;

            if (motionObj["translationFactor"] != null) {
                var transFactor = motionObj["translationFactor"] as Newtonsoft.Json.Linq.JObject;
                if (transFactor != null) {
                    target.TranslationFactor = new Vector3(
                        transFactor["x"]?.ToObject<float>() ?? 0,
                        transFactor["y"]?.ToObject<float>() ?? 0,
                        transFactor["z"]?.ToObject<float>() ?? 0
                    );
                }
            }

            if (motionObj["rotationFactor"] != null) {
                var rotFactor = motionObj["rotationFactor"] as Newtonsoft.Json.Linq.JObject;
                if (rotFactor != null) {
                    target.RotationFactor = new Vector3(
                        rotFactor["x"]?.ToObject<float>() ?? 0,
                        rotFactor["y"]?.ToObject<float>() ?? 0,
                        rotFactor["z"]?.ToObject<float>() ?? 0
                    );
                }
            }
        }

        private void ImportVirtualSwitchDefinition(Newtonsoft.Json.Linq.JToken virtualDefData, SwitchVirtualDefinition target) {
            if (virtualDefData == null || target == null) return;

            var virtualObj = virtualDefData as Newtonsoft.Json.Linq.JObject;
            if (virtualObj == null) return;

            if (virtualObj["enabled"] != null) {
                target.Enabled = virtualObj["enabled"].ToObject<bool>();
            }
            if (virtualObj["upId"] != null) {
                target.UpId = virtualObj["upId"].ToObject<int>();
            }
            if (virtualObj["downId"] != null) {
                target.DownId = virtualObj["downId"].ToObject<int>();
            }
            if (virtualObj["leftId"] != null) {
                target.LeftId = virtualObj["leftId"].ToObject<int>();
            }
            if (virtualObj["rightId"] != null) {
                target.RightId = virtualObj["rightId"].ToObject<int>();
            }
        }

        private void ImportCharacterButtonAnimation(Newtonsoft.Json.Linq.JToken charAnimData, CharacterButtonAnimationDefinition target) {
            if (charAnimData == null || target == null) return;

            var charAnimDict = charAnimData as Newtonsoft.Json.Linq.JObject;
            if (charAnimDict == null) return;

            if (charAnimDict["animationData"] != null) {
                ImportHoverableStatefulTransitionAnimationData(charAnimDict["animationData"], target.AnimationData);
            }

            if (charAnimDict["maskedBodyParts"] != null) {
                var maskedPartsArray = charAnimDict["maskedBodyParts"].ToObject<string[]>();
                var maskedParts = new List<AnimationMaskedBodyPart>();
                foreach (var part in maskedPartsArray) {
                    if (Enum.TryParse<AnimationMaskedBodyPart>(part, out var bodyPart)) {
                        maskedParts.Add(bodyPart);
                    }
                }
                target.MaskedBodyParts = maskedParts.ToArray();
            }
        }

        private void ImportSwitchCharacterAnimation(Newtonsoft.Json.Linq.JToken charAnimData, SwitchCharacterAnimationDefinition target) {
            if (charAnimData == null || target == null) return;

            var charAnimDict = charAnimData as Newtonsoft.Json.Linq.JObject;
            if (charAnimDict == null) return;

            if (charAnimDict["directions"] != null) {
                var directions = charAnimDict["directions"] as Newtonsoft.Json.Linq.JObject;
                if (directions != null) {
                    ImportSwitchStateAnimationData(directions["d1"], target.AnimationDataD1);
                    ImportSwitchStateAnimationData(directions["d2"], target.AnimationDataD2);
                    ImportSwitchStateAnimationData(directions["d3"], target.AnimationDataD3);
                    ImportSwitchStateAnimationData(directions["d4"], target.AnimationDataD4);
                    ImportSwitchStateAnimationData(directions["d5"], target.AnimationDataD5);
                    ImportSwitchStateAnimationData(directions["d6"], target.AnimationDataD6);
                    ImportSwitchStateAnimationData(directions["d7"], target.AnimationDataD7);
                    ImportSwitchStateAnimationData(directions["d8"], target.AnimationDataD8);
                }
            }

            if (charAnimDict["base"] != null) {
                ImportBaseRevertibleAnimationData(charAnimDict["base"], target.Base);
            }

            if (charAnimDict["transitions"] != null) {
                var transitions = charAnimDict["transitions"] as Newtonsoft.Json.Linq.JObject;
                if (transitions != null) {
                    ImportTransition(transitions["hover"], target.TransitionHover);
                    ImportTransition(transitions["down"], target.TransitionDown);
                    ImportTransition(transitions["up"], target.TransitionUp);
                }
            }

            if (charAnimDict["maskedBodyParts"] != null) {
                var maskedPartsArray = charAnimDict["maskedBodyParts"].ToObject<string[]>();
                var maskedParts = new List<AnimationMaskedBodyPart>();
                foreach (var part in maskedPartsArray) {
                    if (Enum.TryParse<AnimationMaskedBodyPart>(part, out var bodyPart)) {
                        maskedParts.Add(bodyPart);
                    }
                }
                target.MaskedBodyParts = maskedParts.ToArray();
            }
        }

        private void ImportAxisCharacterAnimation(Newtonsoft.Json.Linq.JToken charAnimData, AxisCharacterAnimationDefinition target) {
            if (charAnimData == null || target == null) return;

            var charAnimDict = charAnimData as Newtonsoft.Json.Linq.JObject;
            if (charAnimDict == null) return;

            if (charAnimDict["base"] != null) {
                ImportBaseRevertibleAnimationData(charAnimDict["base"], target.Base);
            }

            if (charAnimDict["max"] != null) {
                ImportAnimationData(charAnimDict["max"], target.Max);
            }

            if (charAnimDict["min"] != null) {
                ImportAnimationData(charAnimDict["min"], target.Min);
            }

            if (charAnimDict["maskedBodyParts"] != null) {
                var maskedPartsArray = charAnimDict["maskedBodyParts"].ToObject<string[]>();
                var maskedParts = new List<AnimationMaskedBodyPart>();
                foreach (var part in maskedPartsArray) {
                    if (Enum.TryParse<AnimationMaskedBodyPart>(part, out var bodyPart)) {
                        maskedParts.Add(bodyPart);
                    }
                }
                target.MaskedBodyParts = maskedParts.ToArray();
            }
        }

        private void ImportHoverableStatefulTransitionAnimationData(Newtonsoft.Json.Linq.JToken animData, HoverableStatefulTransitionAnimationData target) {
            if (animData == null || target == null) return;

            var animDict = animData as Newtonsoft.Json.Linq.JObject;
            if (animDict == null) return;

            if (animDict["hoverAnimationData"] != null) {
                ImportBaseRevertibleTransitionAnimationData(animDict["hoverAnimationData"], target.HoverAnimationData);
            }

            if (animDict["downAnimationData"] != null) {
                ImportStatefulTransitionAnimationData(animDict["downAnimationData"], target.DownAnimationData);
            }
        }

        private void ImportSwitchStateAnimationData(Newtonsoft.Json.Linq.JToken animData, SwitchStateAnimationData target) {
            if (animData == null || target == null) return;

            var animDict = animData as Newtonsoft.Json.Linq.JObject;
            if (animDict == null) return;

            if (animDict["hoverAnimation"] != null) {
                ImportAnimationData(animDict["hoverAnimation"], target.HoverAnimation);
            }

            if (animDict["downAnimation"] != null) {
                ImportAnimationData(animDict["downAnimation"], target.DownAnimation);
            }
        }

        private void ImportBaseRevertibleAnimationData(Newtonsoft.Json.Linq.JToken animData, BaseRevertibleAnimationData target) {
            if (animData == null || target == null) return;

            var animDict = animData as Newtonsoft.Json.Linq.JObject;
            if (animDict == null) return;

            if (animDict["source"] != null) {
                target.Source = animDict["source"].ToString();
            }

            if (animDict["isReturnToBaseWanted"] != null) {
                target.IsReturnToBaseWanted = animDict["isReturnToBaseWanted"].ToObject<bool>();
            }

            if (target is BaseRevertibleTransitionAnimationData transitionTarget && animDict.ContainsKey("transition")) {
                ImportTransition(animDict["transition"], transitionTarget.Transition);
            }
        }

        private void ImportStatefulTransitionAnimationData(Newtonsoft.Json.Linq.JToken animData, StatefulTransitionAnimationData target) {
            if (animData == null || target == null) return;

            var animDict = animData as Newtonsoft.Json.Linq.JObject;
            if (animDict == null) return;

            if (animDict["source"] != null) {
                target.Source = animDict["source"].ToString();
            }

            if (animDict["transition"] != null) {
                ImportTransition(animDict["transition"], target.Transition);
            }

            if (animDict["upTransition"] != null) {
                ImportTransition(animDict["upTransition"], target.UpTransition);
            }
        }

        private void ImportBaseRevertibleTransitionAnimationData(Newtonsoft.Json.Linq.JToken animData, BaseRevertibleTransitionAnimationData target) {
            if (animData == null || target == null) return;

            var animDict = animData as Newtonsoft.Json.Linq.JObject;
            if (animDict == null) return;

            if (animDict["source"] != null) {
                target.Source = animDict["source"].ToString();
            }

            if (animDict["isReturnToBaseWanted"] != null) {
                target.IsReturnToBaseWanted = animDict["isReturnToBaseWanted"].ToObject<bool>();
            }

            if (target is BaseRevertibleTransitionAnimationData transitionTarget && animDict["transition"] != null) {
                ImportTransition(animDict["transition"], transitionTarget.Transition);
            }
        }

        private void ImportAnimationData(Newtonsoft.Json.Linq.JToken animData, AnimationData target) {
            if (animData == null || target == null) return;

            var animDict = animData as Newtonsoft.Json.Linq.JObject;
            if (animDict == null) return;

            if (animDict["source"] != null) {
                target.Source = animDict["source"].ToString();
            }
        }
    }
}
