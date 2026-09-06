using System;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class VirtualKeyPresetConfig : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("KEYS_LAYOUT_PRESET")]
        public VirtualKeyLayoutPreset Preset = VirtualKeyLayoutPreset.GamingWASDExtended;

        [Trigger(20)]
        [Label("APPLY_KEYS_LAYOUT_PRESET")]
        [Description("APPLY_KEYS_LAYOUT_PRESET_DESC")]
        public void TriggerApplyKeysLayoutPreset() {
            Parent?.ApplyVirtualKeyPreset(Preset);
        }

        public string GetHeader() => "KEYS_LAYOUT_PRESET".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
        }
    }

    public class VirtualKeyCollection : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("KEYS_LAYOUT_PRESET")]
        public VirtualKeyPresetConfig LayoutPreset;

        [DataInput(20)]
        [Label("KEYSET_TRANSFORMS")]
        public VirtualKeysetTransforms KeysetTransforms;

        [DataInput(30)]
        [Label("VIRTUAL_KEYS")]
        public VirtualKeyDefinition[] Keys = new VirtualKeyDefinition[0];

        [Trigger(40)]
        [Label("CLEAR_IDENTIFIED_KEY")]
        public void TriggerClearIdentifiedKey() {
            Parent?.ClearIdentifiedKey();
        }

        public string GetHeader() {
            int count = Keys != null ? Keys.Length : 0;
            return $"{"FLOATING_KEY_LAYOUT".Localized()} ({count})";
        }

        public VirtualKeyLayoutPreset Preset {
            get => LayoutPreset?.Preset ?? VirtualKeyLayoutPreset.GamingWASDExtended;
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
                LayoutPreset = StructuredData.Create<VirtualKeyPresetConfig>();
                LayoutPreset.Parent = Parent;
                LayoutPreset.CollapsedSelf = true;
            }
            if (KeysetTransforms == null) {
                KeysetTransforms = StructuredData.Create<VirtualKeysetTransforms>();
                KeysetTransforms.Parent = Parent;
                KeysetTransforms.CollapsedSelf = true;
            }
            Watch(nameof(Keys), delegate {
                Parent?.OnVirtualKeysChanged();
            });
            Watch(nameof(LayoutPreset), delegate {
                if (LayoutPreset != null) {
                    LayoutPreset.Parent = Parent;
                }
            });
            Watch(nameof(KeysetTransforms), delegate {
                if (KeysetTransforms != null) {
                    KeysetTransforms.Parent = Parent;
                }
                Parent?.SetupKeysetTransforms();
            });
        }
    }
}
