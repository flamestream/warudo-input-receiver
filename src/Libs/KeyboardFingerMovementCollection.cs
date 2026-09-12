using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class KeyboardFingerMovementCollection : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [Trigger(5)]
        [Label("AUTO_GENERATE_KEY_BINDINGS")]
        [Description("AUTO_GENERATE_KEY_BINDINGS_DESC")]
        public void TriggerAutoGenerateKeyBindings() {
            Parent?.AutoGenerateKeyBindingsFromFloatingKeys();
        }

        [DataInput(20)]
        [Label("KEY_MOVEMENT_DEFINITIONS")]
        public KeyboardKeyBinding[] MovementDefinitions = new KeyboardKeyBinding[0];

        public string GetHeader() {
            int count = MovementDefinitions != null ? MovementDefinitions.Length : 0;
            return $"{"FINGER_MOVEMENT_DEFINITIONS".Localized()} ({count})";
        }

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            Watch(nameof(MovementDefinitions), delegate {
                Parent?.RefreshKeyBindingsParent();
            });
        }
    }
}
