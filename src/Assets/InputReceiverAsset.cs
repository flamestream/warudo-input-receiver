using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Prop;

namespace FlameStream
{
    public abstract partial class InputReceiverAsset : ReceiverAsset {

        protected abstract SignalProfileType[] SupportedProfileTypes { get; }
        protected abstract void GenerateButtonDefinitions(SignalProfileType profile);

        public enum SignalProfileType {
            [Label("CONTROLLER_NINTENDO_SWITCH_PRO")]
            SwitchProController,
            [Label("CONTROLLER_XBOX_SERIES_XS")]
            XboxSeriesXSController,
            [Label("CONTROLLER_XBOX_360")]
            Xbox360Controller,
            [Label("CONTROLLER_PS5")]
            PS5Controller,
            [Label("CONTROLLER_8BITDO_PRO_2")]
            EightBitDoPro2Controller,
            [Label("CONTROLLER_8BITDO_MODKIT_NGC")]
            EightBitDoModKitGameCubeController,
            [Label("CONTROLLER_RETROFIGHTERS_BATTLER_GC_PRO")]
            RetroFightersBattlerGcProController,
            [Label("CONTROLLER_POWERA_GCN_STYLE_FOR_SWITCH")]
            PowerAGameCubeStyleForSwitchController,
        }

        protected Vector3 RotationBackwardLight = new Vector3(-0.5f, 0f, 0f);
        protected Vector3 RotationBackwardStrong = new Vector3(-1f, 0f, 0f);
        protected Vector3 RotationForwardLight = new Vector3(0.5f, 0f, 0f);
        protected Vector3 RotationForwardStrong = new Vector3(1f, 0f, 0f);
        protected Vector3 RotationForwardVeryLight = new Vector3(0.25f, 0f, 0f);
        protected Vector3 RotationLeftLight = new Vector3(0f, 0f, 0.5f);
        protected Vector3 RotationLeftStrong = new Vector3(0f, 0f, 1f);
        protected Vector3 RotationRightLight = new Vector3(0f, 0f, -0.5f);
        protected Vector3 RotationRightStrong = new Vector3(0f, 0f, -1f);
        protected Vector3 TranslationDownLight = new Vector3(0f, -0.5f, 0f);
        protected Vector3 TranslationDownStrong = new Vector3(0f, -1f, 0f);
        protected Vector3 TranslationDownVeryStrong = new Vector3(0f, -2f, 0f);
        protected Vector3 TranslationUpVeryStrong = new Vector3(0f, 2f, 0f);

        const int CURRENT_ASSET_VERSION = 1;

        protected override void OnCreate() {
            base.OnCreate();
            SignalDefinitionGeneration.Parent = this;
            OnCreateSignalDefinition();
            OnCreateAnimation();

            Watch(nameof(IsEnabled), HandleIsControlEnabledChanged);
            Watch(nameof(IsControlEnabled), HandleIsControlEnabledChanged);
        }

        public override void OnUpdate() {
            base.OnUpdate();
            OnUpdateState();
            OnUpdateSignalDefinition();
            OnUpdateBinding();
            OnUpdateAnimation();
            OnUpdatePropMotion();
        }

        protected override void Log(string msg) {
            Debug.Log($"[FlameStream.Asset.GameInputReceiver] {msg}");
        }

        [Hidden]
        [DataInput]
        public int AssetVersion = CURRENT_ASSET_VERSION;

        /// <summary>
        /// SIGNAL DEFINITIONS
        /// </summary>
        [Section("SIGNAL_DEFINITIONS")]

        [Markdown]
        public string AnimationInstructions = "SIGNAL_DEFINITIONS_DESC".Localized();

        [Label("QUICK_SETUP")]
        [DataInput]
        public SignalDefinitionGenerationSection SignalDefinitionGeneration;

        [DataInput]
        [Label("BUTTON_DEFINITIONS")]
        public ButtonDefinition[] ButtonDefinitions;
        public ButtonDefinition[] ButtonDefinitionsReference = new ButtonDefinition[MAX_BUTTON_COUNT];

        [Trigger]
        [Label("ADD_NEW_BUTTON_DEFINITION")]
        public void TriggerAddNewButtonDefinition() {

            // Find biggest button ID in list
            var biggestId = -1;
            if (ButtonDefinitions.Length > 0) {
                biggestId = ButtonDefinitions.Max(d => d.Index);
            }

            var wantedBiggestId = biggestId + 1;
            var targetBiggestId = Math.Min(wantedBiggestId, MAX_BUTTON_COUNT - 1);

            // Regenerate button definitions up to biggest id plus one, taking old data if available
            var newButtonDefinitions = new List<ButtonDefinition>();
            for (int i = 0; i <= targetBiggestId; i++) {
                var old = ButtonDefinitions.FirstOrDefault(d => d.IsValid && d.Index == i);
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

            if (wantedBiggestId != targetBiggestId) {
                Context.Service.PromptMessage("ERROR", $"You may not have more than {MAX_BUTTON_COUNT} buttons defined.");
            }
        }

        bool IsWaitingForButtonPress = false;

        [Trigger]
        [Label("IDENTIFY_BUTTON")]
        public void TriggerIdentifyButton() {
            IsWaitingForButtonPress = true;
            GetTriggerPort(nameof(TriggerIdentifyButton)).Properties.hidden = IsWaitingForButtonPress;
            GetTriggerPort(nameof(TriggerCancelIdentifyButton)).Properties.hidden = !IsWaitingForButtonPress;
            BroadcastTriggerProperties(nameof(TriggerIdentifyButton));
            BroadcastTriggerProperties(nameof(TriggerCancelIdentifyButton));
        }

        [Hidden]
        [Trigger]
        [Label("IDENTIFYING_BUTTON")]
        public void TriggerCancelIdentifyButton() {
            IsWaitingForButtonPress = false;
            GetTriggerPort(nameof(TriggerIdentifyButton)).Properties.hidden = IsWaitingForButtonPress;
            GetTriggerPort(nameof(TriggerCancelIdentifyButton)).Properties.hidden = !IsWaitingForButtonPress;
            BroadcastTriggerProperties(nameof(TriggerIdentifyButton));
            BroadcastTriggerProperties(nameof(TriggerCancelIdentifyButton));
        }

        [DataInput]
        [Label("SWITCH_DEFINITIONS")]
        public SwitchDefinition[] SwitchDefinitions;
        public SwitchDefinition[] SwitchDefinitionsReference = new SwitchDefinition[MAX_SWITCH_COUNT];

        [Trigger]
        [Label("ADD_NEW_SWITCH_DEFINITION")]
        public void TriggerAddNewSwitchDefinition() {

            // Find biggest button id in list
            var biggestId = -1;
            if (SwitchDefinitions.Length > 0) {
                biggestId = SwitchDefinitions.Max(d => d.Index);
            }

            var wantedBiggestId = biggestId + 1;
            var targetBiggestId = Math.Min(wantedBiggestId, InputReceiverAsset.MAX_SWITCH_COUNT - 1);

            // Regenerate button definitions up to biggest id plus one, taking old data if available
            var newSwitchDefinitions = new List<SwitchDefinition>();
            for (int i = 0; i <= targetBiggestId; i++) {
                var old = SwitchDefinitions.FirstOrDefault(d => d.IsValid && d.Index == i);
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

            if (wantedBiggestId != targetBiggestId) {
                Context.Service.PromptMessage("ERROR", $"You may not have more than {InputReceiverAsset.MAX_SWITCH_COUNT} switches defined.");
            }
        }

        [DataInput]
        [Label("AXIS_DEFINITIONS")]
        public AxisDefinition[] AxisDefinitions;
        public AxisDefinition[] AxisDefinitionsReference = new AxisDefinition[MAX_AXIS_COUNT];

        [Trigger]
        [Label("ADD_NEW_AXIS_DEFINITION")]
        public void AddNewAxisDefinition() {

            var largestId = -1;
            if (AxisDefinitions.Length > 0) {
                largestId = AxisDefinitions.Max(d => d.Index);
            }

            var wantedLargestId = largestId + 1;
            var targetLargestId = Math.Min(wantedLargestId, MAX_AXIS_COUNT - 1);

            // Regenerate button definitions up to largest id plus one, taking old data if available
            var newAxisDefinitions = new List<AxisDefinition>();
            for (int i = 0; i <= targetLargestId; i++) {
                var old = AxisDefinitions.FirstOrDefault(d => d.IsValid && d.Index == i);
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

            if (wantedLargestId != targetLargestId) {
                Context.Service.PromptMessage("ERROR", $"You may not have more than {MAX_AXIS_COUNT} axes defined.");
            }
        }


        /// <summary>
        /// BINDING
        /// </summary>
        [Section("BINDING")]

        [Markdown]
        [HiddenIf(nameof(IsStateSaved), Is.True)]
        public string BindingInstructions = "BINDING_DESC".Localized(new object[] {
            "CHARACTER".Localized(),
            "PROP".Localized(),
            "SAVE_STATE".Localized(),
            "ENABLE_CONTROL".Localized(),
        });

        [DataInput]
        [Label("ENABLE_CONTROL")]
        [HiddenIf(nameof(IsStateSaved), Is.False)]
        [Description("ENABLE_CONTROL_DESC")]
        public bool IsControlEnabled;

        [Trigger]
        [HiddenIf(nameof(IsStateSaved), Is.False)]
        [Label("RESET_STATE")]
        public void TriggerResetState() {
            FadeInControl();
        }

        [DataInput]
        [Label("CHARACTER")]
        [Description("BINDING_CHARACTER_DESC")]
        [DisabledIf(nameof(IsStateSaved), Is.True)]
        public CharacterAsset Character;

        [DataInput]
        [Label("PROP")]
        [Description("BINDING_PROP_DESC")]
        [DisabledIf(nameof(IsStateSaved), Is.True)]
        public PropAsset HeldProp;

        [Trigger]
        [DisabledIf(nameof(CheckIsBindingSetupInputMissing))]
        [HiddenIf(nameof(IsStateSaved), Is.True)]
        [Label("SAVE_STATE")]
        public void TriggerSaveState() {
            RecordState();
        }

        [DataInput]
        [Label("EXTRA")]
        [HiddenIf(nameof(IsStateSaved), Is.False)]
        public AdvancedBindings AdvancedOptions;

        [DataInput]
        [Hidden]
        public bool IsStateSaved;

        [DataInput]
        [Label("")]
        [HiddenIf(nameof(IsStateSaved), Is.False)]
        public BindingDefinitions SavedBindingData;

        [Trigger]
        [Label("CLEAR_SAVED_STATE")]
        [HiddenIf(nameof(IsStateSaved), Is.False)]
        public void TriggerClearSavedState() {
            ClearSavedState();
        }

        /// <summary>
        /// PROP MOTION
        /// </summary>
        [Section("PROP_MOTION")]

        [Markdown]
        [HiddenIf(nameof(IsPropMotionWanted), Is.True)]
        public string ShakingMotionInstructions = "PROP_MOTION_DESC".Localized();

        [DataInput]
        [Label("ENABLE")]
        [DisabledIf(nameof(IsControlEnabled), Is.False)]
        public bool IsPropMotionWanted;

        [DataInput]
        [Label("TRANSLATION_FACTORS")]
        [HiddenIf(nameof(IsPropMotionWanted), Is.False)]
        public PropMotionFactorSet PropMotionTranslationFactorSet;

        [DataInput]
        [Label("ROTATION_FACTORS")]
        [HiddenIf(nameof(IsPropMotionWanted), Is.False)]
        public PropMotionFactorSet PropMotionRotationFactorSet;

        /// <summary>
        /// HAND TRACKER
        /// </summary>
        [Section("HAND_TRACKER")]

        [Markdown]
        [HiddenIf(nameof(IsHandTrackerEnabled), Is.True)]
        public string HandTrackerInfo = @"Allows temporarily holding the game controller with the untracked hand while the other hand is being tracked.";

        [DataInput]
        [Label("ENABLE")]
        [DisabledIf(nameof(IsControlEnabled), Is.False)]
        public bool IsHandTrackerEnabled;

        [Markdown]
        [HiddenIf(nameof(IsHandTrackerEnabled), Is.False)]
        public string HandTrackerInstructions = @"### Setup Instructions
Go to your **Pose Tracking** blueprint and insert the **🔥🎮 Hand Tracker** node before the **Override Character Bone Rotations** node's **Bone Rotation Weights** input.";

        [DataInput]
        [HiddenIf(nameof(IsHandTrackerEnabled), Is.False)]
        [Label("UNTRACKED_LEFT_HAND_MOTION")]
        public UntrackedHandMotionSet UntrackedLeftHandMotion;

        [DataInput]
        [HiddenIf(nameof(IsHandTrackerEnabled), Is.False)]
        [Label("UNTRACKED_RIGHT_HAND_MOTION")]
        public UntrackedHandMotionSet UntrackedRightHandMotion;
    }
}
