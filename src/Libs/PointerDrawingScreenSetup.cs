using Cysharp.Threading.Tasks;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Serializations;

namespace FlameStream {
    public enum DrawingScreenAutoSetupPreset {
        StandingTilted,
        FlatSurface
    }

    public class PointerDrawingScreenPresetConfig : StructuredData<PointerReceiverAsset>, ICollapsibleStructuredData {
        private long uiVersion = 0;
        public override long GetVersion() => base.GetVersion() + uiVersion;

        public override SerializedStructuredData Serialize() {
            var serialized = base.Serialize();
            serialized.version = GetVersion();
            return serialized;
        }

        public async void RequestUiRefresh() {
            await UniTask.Yield();
            uiVersion++;
            Broadcast();
        }

        [DataInput(10)]
        [Label("SCREEN_AUTO_SETUP_MODE")]
        public DrawingScreenAutoSetupPreset Preset = DrawingScreenAutoSetupPreset.StandingTilted;

        [Markdown(15)]
        public string PresetDescription = "";

        [Trigger(20)]
        [Label("AUTO_SETUP_SCREEN_MAPPING")]
        [Description("")]
        [DisabledIf(nameof(IsAutoSetupDisabled))]
        public void TriggerAutoSetupScreenMapping() {
            Parent?.ApplyScreenAutoSetup(Preset);
        }

        public bool IsAutoSetupDisabled() => Parent == null || Parent.Character == null;

        public string GetHeader() => "SCREEN_AUTO_SETUP_PRESET_HEADER".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = false;

            Watch(nameof(Preset), delegate {
                UpdateDescription();
            });

            UpdateDescription();
        }

        public void UpdateDescription() {
            bool isRight = Parent != null && Parent.IsRightHanded;
            string handStr = isRight ? "RIGHT_HAND".Localized() : "LEFT_HAND".Localized();
            if (handStr != null && handStr.EndsWith("Hand")) {
                handStr = handStr.ToLowerInvariant();
            }

            string desc = "AUTO_SETUP_SCREEN_MAPPING_DESC".Localized() + "\n\n";

            if (Preset == DrawingScreenAutoSetupPreset.StandingTilted) {
                desc += "SCREEN_AUTO_SETUP_DESC_STANDING_TILTED".Localized();
            } else {
                desc += string.Format("SCREEN_AUTO_SETUP_DESC_FLAT_SURFACE".Localized(), handStr);
            }

            PresetDescription = desc;
            try {
                var triggerPort = GetTriggerPort(nameof(TriggerAutoSetupScreenMapping));
                if (triggerPort?.Properties != null) {
                    bool disabled = IsAutoSetupDisabled();
                    triggerPort.Properties.disabled = disabled;
                    triggerPort.Properties.description = disabled
                        ? "SCREEN_AUTO_SETUP_NO_CHARACTER".Localized()
                        : "";
                    BroadcastTriggerProperties(nameof(TriggerAutoSetupScreenMapping));
                }
                var mdPort = GetDataInputPort(nameof(PresetDescription));
                if (mdPort != null) {
                    BroadcastDataInput(nameof(PresetDescription));
                }
            } catch { }

            RequestUiRefresh();
        }
    }

    public class PointerDrawingScreenSetup : StructuredData<PointerReceiverAsset>, ICollapsibleStructuredData {
        private long uiVersion = 0;
        public override long GetVersion() => base.GetVersion() + uiVersion;

        public override SerializedStructuredData Serialize() {
            var serialized = base.Serialize();
            serialized.version = GetVersion();
            return serialized;
        }

        public async void RequestUiRefresh() {
            await UniTask.Yield();
            uiVersion++;
            Broadcast();
        }

        [DataInput(10)]
        [Label("SCREEN_AUTO_SETUP_CONFIG")]
        public PointerDrawingScreenPresetConfig AutoSetup;

        [DataInput(20)]
        [Label("REMOVE_SCREEN_ON_DISABLE")]
        [Description("REMOVE_SCREEN_ON_DISABLE_DESC")]
        public bool RemoveOnDisable = false;

        [DataInput(30)]
        [Label("SCREEN_TRANSFORM")]
        [Description("SCREEN_TRANSFORM_DESC")]
        public CollapsibleVisualSetupTransform Transform;

        public string GetHeader() => "DRAWING_SCREEN_SETUP".Localized();

        public void UpdateAutoSetupStatus() {
            if (AutoSetup != null) {
                AutoSetup.Parent = Parent;
                AutoSetup.UpdateDescription();
                BroadcastDataInput(nameof(AutoSetup));
            }
            RequestUiRefresh();
        }

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = false;

            if (AutoSetup == null) {
                AutoSetup = StructuredData.Create<PointerDrawingScreenPresetConfig>();
                AutoSetup.Parent = Parent;
                AutoSetup.CollapsedSelf = false;
                SetDataInput(nameof(AutoSetup), AutoSetup, true);
            }
            Watch(nameof(AutoSetup), delegate {
                if (AutoSetup != null) {
                    AutoSetup.Parent = Parent;
                    AutoSetup.UpdateDescription();
                }
            });

            if (Transform == null) {
                Transform = StructuredData.Create<CollapsibleVisualSetupTransform>();
                Vector3 defaultPos = new Vector3(0, 0.6f, 1f);
                Vector3 defaultRot = new Vector3(-60f, 180f, 0);
                Vector3 defaultScl = new Vector3(0.03f, 0.03f, 0.03f);
                if (Parent != null && Parent.CalculateScreenPresetTransform(DrawingScreenAutoSetupPreset.StandingTilted, out Vector3 charPos, out Vector3 charRot, out Vector3 charScl)) {
                    defaultPos = charPos;
                    defaultRot = charRot;
                    defaultScl = charScl;
                }
                Transform.Position = defaultPos;
                Transform.Rotation = defaultRot;
                Transform.Scale = defaultScl;
                Transform.SetDataInput(nameof(Transform.Position), defaultPos, true);
                Transform.SetDataInput(nameof(Transform.Rotation), defaultRot, true);
                Transform.SetDataInput(nameof(Transform.Scale), defaultScl, true);
            }
            Transform.LocalizationKey = "SCREEN_TRANSFORM";
            Transform.CollapsedSelf = true;
            Transform.ShowOriginIndicator = false;

            Watch(nameof(RemoveOnDisable), delegate {
                Parent?.OnDrawingScreenSettingChange();
            });

            Watch(nameof(Transform), delegate {
                Parent?.SetupDrawingScreenTransform();
            });
        }
    }
}
