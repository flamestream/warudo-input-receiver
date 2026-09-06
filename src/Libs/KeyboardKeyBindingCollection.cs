using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class KeyboardLayoutPresetConfig : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("LAYOUT_PRESET")]
        public KeyboardLayoutPreset Preset = KeyboardLayoutPreset.StandardQWERTY;

        [Trigger(20)]
        [Label("APPLY_LAYOUT_PRESET")]
        [Description("APPLY_LAYOUT_PRESET_DESC")]
        public void TriggerApplyLayoutPreset() {
            Parent?.ApplyPreset(Preset);
        }

        public string GetHeader() => "LAYOUT_PRESET".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
        }
    }

    public class KeyboardKeyBindingCollection : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("LAYOUT_PRESET")]
        public KeyboardLayoutPresetConfig LayoutPreset;

        [DataInput(20)]
        [Label("KEY_BINDINGS")]
        public KeyboardKeyBinding[] KeyBindings = new KeyboardKeyBinding[0];

        public string GetHeader() {
            int count = KeyBindings != null ? KeyBindings.Length : 0;
            return $"{"KEY_BINDINGS_LAYOUT".Localized()} ({count})";
        }

        public KeyboardLayoutPreset Preset {
            get => LayoutPreset?.Preset ?? KeyboardLayoutPreset.StandardQWERTY;
            set {
                if (LayoutPreset != null) {
                    LayoutPreset.Preset = value;
                }
            }
        }

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            if (LayoutPreset == null) {
                LayoutPreset = StructuredData.Create<KeyboardLayoutPresetConfig>();
                LayoutPreset.Parent = Parent;
                LayoutPreset.CollapsedSelf = true;
            }
            Watch(nameof(KeyBindings), delegate {
                Parent?.RefreshKeyBindingsParent();
            });
            Watch(nameof(LayoutPreset), delegate {
                if (LayoutPreset != null) {
                    LayoutPreset.Parent = Parent;
                }
            });
        }
    }
}
