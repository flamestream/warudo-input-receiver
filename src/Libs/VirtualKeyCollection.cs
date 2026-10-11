using System;
using System.Linq;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class VirtualKeyPresetConfig : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [Markdown(5)]
        public string Notice = $"⚠️ *{"APPLY_KEYS_LAYOUT_PRESET_NOTICE".Localized()}*";

        [DataInput(10)]
        [Label("KEYS_LAYOUT_PRESET")]
        public VirtualKeyLayoutPreset Preset = VirtualKeyLayoutPreset.FullSize100;

        [Trigger(20)]
        [Label("APPLY_KEYS_LAYOUT_PRESET")]
        public void TriggerApplyKeysLayoutPreset() {
            var target = Parent ?? Context.OpenedScene?.GetAssets<KeyboardReceiverAsset>()?.FirstOrDefault();
            target?.ApplyVirtualKeyPreset(Preset);
        }

        public string GetHeader() => "CLICK_TO_CONFIGURE".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            Notice = $"⚠️ *{"APPLY_KEYS_LAYOUT_PRESET_NOTICE".Localized()}*";
        }
    }

    public class VirtualKeyCollection : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("KEYS_LAYOUT_PRESET")]
        public VirtualKeyPresetConfig LayoutPreset;

        [DataInput(20)]
        [Label("KEY_DEFINITIONS")]
        public VirtualKeyDefinition[] Keys = new VirtualKeyDefinition[0];

        [Trigger(30)]
        [Label("CLEAR_IDENTIFIED_KEY")]
        public void TriggerClearIdentifiedKey() {
            Parent?.ClearIdentifiedKey();
        }

        public string GetHeader() {
            int count = Keys != null ? Keys.Length : 0;
            return $"{"CLICK_TO_CONFIGURE".Localized()} ({count})";
        }

        public VirtualKeyLayoutPreset Preset {
            get => LayoutPreset?.Preset ?? VirtualKeyLayoutPreset.FullSize100;
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
                SetDataInput(nameof(LayoutPreset), LayoutPreset, true);
            } else if (LayoutPreset.Parent == null) {
                LayoutPreset.Parent = Parent;
            }
            Watch(nameof(Keys), delegate {
                Parent?.OnVirtualKeysChanged();
            });
            Watch(nameof(LayoutPreset), delegate {
                if (LayoutPreset != null) {
                    LayoutPreset.Parent = Parent;
                }
            });
        }
    }
}
