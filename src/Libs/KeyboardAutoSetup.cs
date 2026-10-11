using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class KeyboardAutoSetup : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [Markdown(10)]
        public string Instructions = "KEYBOARD_AUTO_SETUP_INSTRUCTIONS".Localized();

        [DataInput(15)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("KEYBOARD_NEUTRAL_POSE")]
        [Description("KEYBOARD_NEUTRAL_POSE_DESC")]
        [DisabledIf(nameof(IsCharacterMissing))]
        public string NeutralPose;

        [Trigger(20)]
        [Label("AUTO_SETUP_FROM_CHARACTER_POSE")]
        public void TriggerAutoSetup() {
            Parent?.AutoSetupFromCharacterPose();
        }

        public string GetHeader() => "CLICK_TO_SETUP_CHARACTER".Localized();

        public bool IsCharacterMissing() => Parent == null || Parent.Character == null || !Parent.Character.Active;

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            Instructions = "KEYBOARD_AUTO_SETUP_INSTRUCTIONS".Localized();

            Watch(nameof(NeutralPose), delegate {
                if (Parent != null && Parent.Character != null && !string.IsNullOrEmpty(NeutralPose)) {
                    Helper.ApplyCharacterIdleAnimation(Parent.Character, NeutralPose);
                }
            });

            UpdateButtonState();
        }

        public void OnCharacterChanged() {
            if (Parent != null && Parent.Character != null && !string.IsNullOrEmpty(Parent.Character.DefaultIdleAnimation)) {
                NeutralPose = Parent.Character.DefaultIdleAnimation;
                SetDataInput(nameof(NeutralPose), NeutralPose, true);
                BroadcastDataInput(nameof(NeutralPose));
            }
            UpdateButtonState();
        }

        public void UpdateButtonState() {
            Instructions = "KEYBOARD_AUTO_SETUP_INSTRUCTIONS".Localized();

            bool missingChar = IsCharacterMissing();

            var neutralPosePort = GetDataInputPort(nameof(NeutralPose));
            if (neutralPosePort != null && neutralPosePort.Properties != null) {
                neutralPosePort.Properties.disabled = missingChar;
                BroadcastDataInputProperties(nameof(NeutralPose));
            }

            if (!missingChar && Parent.Character != null) {
                if (string.IsNullOrEmpty(NeutralPose) && !string.IsNullOrEmpty(Parent.Character.DefaultIdleAnimation)) {
                    NeutralPose = Parent.Character.DefaultIdleAnimation;
                    SetDataInput(nameof(NeutralPose), NeutralPose, true);
                    BroadcastDataInput(nameof(NeutralPose));
                }
            }

            var port = GetTriggerPort(nameof(TriggerAutoSetup));
            if (port == null || port.Properties == null) return;

            if (missingChar) {
                port.Properties.disabled = true;
                port.Properties.description = "AUTO_SETUP_BLOCK_NO_CHARACTER".Localized();
                BroadcastTriggerProperties(nameof(TriggerAutoSetup));
                return;
            }

            if (Parent.Character.Animator == null) {
                port.Properties.disabled = true;
                port.Properties.description = "AUTO_SETUP_NO_ANIMATOR".Localized();
                BroadcastTriggerProperties(nameof(TriggerAutoSetup));
                return;
            }

            if (Parent.IsHandEnabled) {
                port.Properties.disabled = true;
                port.Properties.description = "AUTO_SETUP_BLOCK_HANDS_ENABLED".Localized();
                BroadcastTriggerProperties(nameof(TriggerAutoSetup));
                return;
            }

            port.Properties.disabled = false;
            port.Properties.description = "AUTO_SETUP_BUTTON_DESC".Localized();
            BroadcastTriggerProperties(nameof(TriggerAutoSetup));
        }
    }
}
