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

    public enum DrawingScreenRenderMode {
        Raw = 0,
        Outline = 1,
        OutlineAndRaw = 2
    }

    public class PointerDrawingScreenAppearanceSetup : StructuredData<PointerReceiverAsset>, ICollapsibleStructuredData {
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
        [Label("DRAWING_SCREEN_RENDER_MODE")]
        [Description("DRAWING_SCREEN_RENDER_MODE_DESC")]
        public DrawingScreenRenderMode RenderMode = DrawingScreenRenderMode.Raw;

        [DataInput(20)]
        [Section("VISUAL_BORDER_SECTION")]
        [SectionHiddenIf(nameof(IsRawMode))]
        [Label("BORDER_COLOR")]
        [HiddenIf(nameof(IsRawMode))]
        public Color BorderColor = new Color(0.3373f, 1.0000f, 1.0000f, 1.0000f);

        [DataInput(21)]
        [Label("BORDER_SELF_ILLUMINATION")]
        [FloatSlider(0f, 5f)]
        [HiddenIf(nameof(IsRawMode))]
        public float BorderSelfIllumination = 1f;

        [DataInput(22)]
        [Label("BORDER_THICKNESS")]
        [FloatSlider(0.005f, 0.5f, 0.005f)]
        [HiddenIf(nameof(HideUniformBorder))]
        public float BorderThickness = 0.08f;

        [DataInput(23)]
        [Label("USE_INDIVIDUAL_BORDERS")]
        [HiddenIf(nameof(IsRawMode))]
        public bool UseIndividualBorders = false;

        [DataInput(24)]
        [Label("BORDER_THICKNESSES")]
        [HiddenIf(nameof(HideIndividualBorders))]
        public Vector4 BorderThicknesses = new Vector4(0.08f, 0.08f, 0.08f, 0.08f);

        [DataInput(30)]
        [Section("VISUAL_CORNER_SECTION")]
        [SectionHiddenIf(nameof(IsRawMode))]
        [Label("CORNER_RADIUS")]
        [FloatSlider(0f, 2.0f, 0.05f)]
        [HiddenIf(nameof(HideUniformCorner))]
        public float CornerRadius = 0.3f;

        [DataInput(31)]
        [Label("USE_INDIVIDUAL_CORNERS")]
        [HiddenIf(nameof(IsRawMode))]
        public bool UseIndividualCorners = false;

        [DataInput(32)]
        [Label("CORNER_RADII")]
        [HiddenIf(nameof(HideIndividualCorners))]
        public Vector4 CornerRadii = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);

        [DataInput(33)]
        [Label("CORNER_SEGMENTS")]
        [IntegerSlider(2, 32)]
        [HiddenIf(nameof(IsRawMode))]
        public int CornerSegments = 12;

        [DataInput(40)]
        [Section("VISUAL_FILL_SECTION")]
        [SectionHiddenIf(nameof(IsRawMode))]
        [Label("ENABLE_FILL")]
        [HiddenIf(nameof(IsRawMode))]
        public bool EnableFill = true;

        [DataInput(41)]
        [Label("FILL_COLOR")]
        [HiddenIf(nameof(HideFillSettings))]
        public Color FillColor = new Color(0.0000f, 0.1961f, 0.8588f, 0.5020f);

        [DataInput(42)]
        [Label("FILL_SELF_ILLUMINATION")]
        [FloatSlider(0f, 5f)]
        [HiddenIf(nameof(HideFillSettings))]
        public float FillSelfIllumination = 1.0f;

        [DataInput(50)]
        [Section("VISUAL_GEOMETRY_SECTION")]
        [SectionHiddenIf(nameof(IsRawMode))]
        [Label("BOUND_OFFSET")]
        [FloatSlider(-1.0f, 1.0f, 0.05f)]
        [HiddenIf(nameof(HideUniformBoundOffset))]
        public float BoundOffset = 0f;

        [DataInput(51)]
        [Label("USE_INDIVIDUAL_BOUND_OFFSETS")]
        [HiddenIf(nameof(IsRawMode))]
        public bool UseIndividualBoundOffsets = false;

        [DataInput(52)]
        [Label("BOUND_OFFSETS")]
        [HiddenIf(nameof(HideIndividualBoundOffsets))]
        public Vector4 BoundOffsets = Vector4.zero;

        [DataInput(53)]
        [Label("DEPTH_OFFSET")]
        [FloatSlider(-0.05f, 0.05f, 0.001f)]
        [HiddenIf(nameof(IsRawMode))]
        public float DepthOffset = 0.001f;

        [Trigger(60)]
        [Label("RESET_TO_DEFAULTS")]
        [HiddenIf(nameof(IsRawMode))]
        public void TriggerResetToDefaults() {
            BorderColor = new Color(0.3373f, 1.0000f, 1.0000f, 1.0000f);
            BorderSelfIllumination = 1f;
            BorderThickness = 0.08f;
            UseIndividualBorders = false;
            BorderThicknesses = new Vector4(0.08f, 0.08f, 0.08f, 0.08f);
            CornerRadius = 0.3f;
            UseIndividualCorners = false;
            CornerRadii = new Vector4(0.3f, 0.3f, 0.3f, 0.3f);
            CornerSegments = 12;
            EnableFill = true;
            FillColor = new Color(0.0000f, 0.1961f, 0.8588f, 0.5020f);
            FillSelfIllumination = 1.0f;
            BoundOffset = 0f;
            UseIndividualBoundOffsets = false;
            BoundOffsets = Vector4.zero;
            DepthOffset = 0.001f;

            SetDataInput(nameof(BorderColor), BorderColor, true);
            SetDataInput(nameof(BorderSelfIllumination), BorderSelfIllumination, true);
            SetDataInput(nameof(BorderThickness), BorderThickness, true);
            SetDataInput(nameof(UseIndividualBorders), UseIndividualBorders, true);
            SetDataInput(nameof(BorderThicknesses), BorderThicknesses, true);
            SetDataInput(nameof(CornerRadius), CornerRadius, true);
            SetDataInput(nameof(UseIndividualCorners), UseIndividualCorners, true);
            SetDataInput(nameof(CornerRadii), CornerRadii, true);
            SetDataInput(nameof(CornerSegments), CornerSegments, true);
            SetDataInput(nameof(EnableFill), EnableFill, true);
            SetDataInput(nameof(FillColor), FillColor, true);
            SetDataInput(nameof(FillSelfIllumination), FillSelfIllumination, true);
            SetDataInput(nameof(BoundOffset), BoundOffset, true);
            SetDataInput(nameof(UseIndividualBoundOffsets), UseIndividualBoundOffsets, true);
            SetDataInput(nameof(BoundOffsets), BoundOffsets, true);
            SetDataInput(nameof(DepthOffset), DepthOffset, true);

            SyncUI();
            Parent?.UpdateDrawingScreenAppearance();
            RequestUiRefresh();
        }

        public void SyncUI() {
            try {
                bool isRaw = IsRawMode();
                void UpdateSection(string portName) {
                    var port = GetDataInputPort(portName);
                    if (port?.Properties != null) {
                        port.Properties.sectionHidden = isRaw;
                        BroadcastDataInputProperties(portName);
                    }
                }
                UpdateSection(nameof(BorderColor));
                UpdateSection(nameof(CornerRadius));
                UpdateSection(nameof(EnableFill));
                UpdateSection(nameof(BoundOffset));
            } catch { }
        }

        public Vector4 GetBorderWidths() => UseIndividualBorders ? BorderThicknesses : BorderVector4Extensions.Uniform(BorderThickness);
        public Vector4 GetCornerRadii() => UseIndividualCorners ? CornerRadii : BorderVector4Extensions.Uniform(CornerRadius);
        public Vector4 GetBoundOffsets() => UseIndividualBoundOffsets ? BoundOffsets : BorderVector4Extensions.Uniform(BoundOffset);

        public bool IsRawMode() => RenderMode == DrawingScreenRenderMode.Raw;
        public bool HideIndividualBorders() => IsRawMode() || !UseIndividualBorders;
        public bool HideUniformBorder() => IsRawMode() || UseIndividualBorders;
        public bool HideIndividualCorners() => IsRawMode() || !UseIndividualCorners;
        public bool HideUniformCorner() => IsRawMode() || UseIndividualCorners;
        public bool HideIndividualBoundOffsets() => IsRawMode() || !UseIndividualBoundOffsets;
        public bool HideUniformBoundOffset() => IsRawMode() || UseIndividualBoundOffsets;
        public bool HideFillSettings() => IsRawMode() || !EnableFill;

        public string GetHeader() => "DRAWING_SCREEN_APPEARANCE".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            SyncUI();

            Watch(nameof(RenderMode), delegate {
                SyncUI();
                RequestUiRefresh();
                Parent?.UpdateDrawingScreenAppearance();
            });
            Watch(nameof(BorderColor), delegate { Parent?.UpdateDrawingScreenAppearance(); });
            Watch(nameof(BorderSelfIllumination), delegate { Parent?.UpdateDrawingScreenAppearance(); });
            Watch(nameof(BorderThickness), delegate { Parent?.UpdateDrawingScreenAppearance(); });
            Watch(nameof(UseIndividualBorders), delegate {
                RequestUiRefresh();
                Parent?.UpdateDrawingScreenAppearance();
            });
            Watch(nameof(BorderThicknesses), delegate { Parent?.UpdateDrawingScreenAppearance(); });
            Watch(nameof(CornerRadius), delegate { Parent?.UpdateDrawingScreenAppearance(); });
            Watch(nameof(UseIndividualCorners), delegate {
                RequestUiRefresh();
                Parent?.UpdateDrawingScreenAppearance();
            });
            Watch(nameof(CornerRadii), delegate { Parent?.UpdateDrawingScreenAppearance(); });
            Watch(nameof(CornerSegments), delegate { Parent?.UpdateDrawingScreenAppearance(); });
            Watch(nameof(EnableFill), delegate {
                RequestUiRefresh();
                Parent?.UpdateDrawingScreenAppearance();
            });
            Watch(nameof(FillColor), delegate { Parent?.UpdateDrawingScreenAppearance(); });
            Watch(nameof(FillSelfIllumination), delegate { Parent?.UpdateDrawingScreenAppearance(); });
            Watch(nameof(BoundOffset), delegate { Parent?.UpdateDrawingScreenAppearance(); });
            Watch(nameof(UseIndividualBoundOffsets), delegate {
                RequestUiRefresh();
                Parent?.UpdateDrawingScreenAppearance();
            });
            Watch(nameof(BoundOffsets), delegate { Parent?.UpdateDrawingScreenAppearance(); });
            Watch(nameof(DepthOffset), delegate { Parent?.UpdateDrawingScreenAppearance(); });
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

        [DataInput(18)]
        [Label("SHOW_SCREEN_ON_ENABLE")]
        [Description("SHOW_SCREEN_ON_ENABLE_DESC")]
        public bool ShowOnEnable = true;

        [DataInput(20)]
        [Label("REMOVE_SCREEN_ON_DISABLE")]
        [Description("REMOVE_SCREEN_ON_DISABLE_DESC")]
        public bool RemoveOnDisable = false;

        [DataInput(30)]
        [Label("SCREEN_TRANSFORM")]
        [Description("SCREEN_TRANSFORM_DESC")]
        public CollapsibleVisualSetupTransform Transform;

        [DataInput(40)]
        [Label("DRAWING_SCREEN_APPEARANCE")]
        public PointerDrawingScreenAppearanceSetup Appearance;

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

            Watch(nameof(ShowOnEnable), delegate {
                Parent?.OnDrawingScreenSettingChange();
            });

            Watch(nameof(RemoveOnDisable), delegate {
                Parent?.OnDrawingScreenSettingChange();
            });

            Watch(nameof(Transform), delegate {
                Parent?.SetupDrawingScreenTransform();
            });

            if (Appearance == null) {
                Appearance = StructuredData.Create<PointerDrawingScreenAppearanceSetup>();
                Appearance.Parent = Parent;
                Appearance.CollapsedSelf = true;
                SetDataInput(nameof(Appearance), Appearance, true);
            }
            Watch(nameof(Appearance), delegate {
                if (Appearance != null) {
                    Appearance.Parent = Parent;
                }
                Parent?.UpdateDrawingScreenAppearance();
            });
        }
    }
}
