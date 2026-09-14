using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class KeyboardAutoSetup : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [Markdown(10)]
        public string Instructions = "KEYBOARD_AUTO_SETUP_INSTRUCTIONS".Localized();

        [Trigger(20)]
        [Label("AUTO_SETUP_FROM_CHARACTER_POSE")]
        public void TriggerAutoSetup() {
            Parent?.AutoSetupFromCharacterPose();
        }

        public string GetHeader() => "CLICK_TO_SETUP_CHARACTER".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            Instructions = "KEYBOARD_AUTO_SETUP_INSTRUCTIONS".Localized();
            UpdateButtonState();
        }

        public void UpdateButtonState() {
            var port = GetTriggerPort(nameof(TriggerAutoSetup));
            if (port == null || port.Properties == null) return;

            Instructions = "KEYBOARD_AUTO_SETUP_INSTRUCTIONS".Localized();

            if (Parent == null || Parent.Character == null || !Parent.Character.Active) {
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
