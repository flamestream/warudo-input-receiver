using System;
using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream
{
    public class VirtualKeyStateStyle : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(5)]
        [Label("ENABLED")]
        public bool Enabled = true;

        [DataInput(10)]
        [Label("COLOR")]
        public Color Color = Color.white;

        [DataInput(15)]
        [FloatSlider(0f, 1.0f, 0.05f)]
        [Label("SELF_ILLUMINATION")]
        public float SelfIllumination = 1.0f;

        [DataInput(20)]
        [Label("GLOW_COLOR")]
        public Color GlowColor = Color.white;

        [DataInput(30)]
        [FloatSlider(0f, 5.0f, 0.1f)]
        [Label("GLOW_INTENSITY")]
        public float GlowIntensity = 0f;

        [DataInput(35)]
        [FloatSlider(0f, 1f, 0.01f)]
        [Label("CORNER_ROUNDNESS")]
        public float CornerRadius = 0.25f;

        [DataInput(36)]
        [Label("USE_INDIVIDUAL_CORNERS")]
        public bool UseIndividualCorners = false;

        [DataInput(37)]
        [Label("INDIVIDUAL_CORNER_RADII")]
        public Vector4 IndividualCornerRadii = new Vector4(0.25f, 0.25f, 0.25f, 0.25f);

        [DataInput(40)]
        [FloatSlider(0.0005f, 0.015f, 0.0002f)]
        [Label("BORDER_WIDTH")]
        public float BorderWidth = 0.0025f;

        [DataInput(41)]
        [Label("USE_INDIVIDUAL_BORDERS")]
        public bool UseIndividualBorders = false;

        [DataInput(42)]
        [Label("INDIVIDUAL_BORDER_WIDTHS")]
        public Vector4 IndividualBorderWidths = new Vector4(0.0025f, 0.0025f, 0.0025f, 0.0025f);

        // Whether to display the "Enabled" toggle port in the Warudo inspector
        public bool ShowEnabledToggle = true;

        // Whether to display geometry properties (true only for Border styles)
        public bool ShowGeometry = false;

        protected override void OnCreate() {
            base.OnCreate();
            SyncUI();
            Watch(nameof(Enabled), delegate { SyncUI(); });
            Watch(nameof(UseIndividualCorners), delegate { SyncUI(); });
            Watch(nameof(UseIndividualBorders), delegate { SyncUI(); });
        }

        public Vector4 GetBorderWidths() {
            return UseIndividualBorders ? IndividualBorderWidths : BorderVector4Extensions.Uniform(BorderWidth);
        }

        public Vector4 GetCornerRadii() {
            return UseIndividualCorners ? IndividualCornerRadii : BorderVector4Extensions.Uniform(CornerRadius);
        }

        public void SyncUI() {
            try {
                var enabledPort = GetDataInputPort(nameof(Enabled));
                if (enabledPort != null) {
                    enabledPort.Properties.hidden = !ShowEnabledToggle;
                    BroadcastDataInputProperties(nameof(Enabled));
                }

                bool hideFields = ShowEnabledToggle && !Enabled;

                void SetHidden(string portName, bool hidden) {
                    var port = GetDataInputPort(portName);
                    if (port != null) {
                        port.Properties.hidden = hidden;
                        BroadcastDataInputProperties(portName);
                    }
                }

                SetHidden(nameof(Color), hideFields);
                SetHidden(nameof(SelfIllumination), hideFields);
                SetHidden(nameof(GlowColor), hideFields);
                SetHidden(nameof(GlowIntensity), hideFields);

                // Geometry controls (only visible for Border styles)
                bool hideGeometry = !ShowGeometry || hideFields;
                SetHidden(nameof(CornerRadius), hideGeometry || UseIndividualCorners);
                SetHidden(nameof(UseIndividualCorners), hideGeometry);
                SetHidden(nameof(IndividualCornerRadii), hideGeometry || !UseIndividualCorners);

                SetHidden(nameof(BorderWidth), hideGeometry || UseIndividualBorders);
                SetHidden(nameof(UseIndividualBorders), hideGeometry);
                SetHidden(nameof(IndividualBorderWidths), hideGeometry || !UseIndividualBorders);
            } catch { }
        }

        public void BroadcastAll() {
            try {
                BroadcastDataInput(nameof(Enabled));
                BroadcastDataInput(nameof(Color));
                BroadcastDataInput(nameof(SelfIllumination));
                BroadcastDataInput(nameof(GlowColor));
                BroadcastDataInput(nameof(GlowIntensity));
                BroadcastDataInput(nameof(CornerRadius));
                BroadcastDataInput(nameof(UseIndividualCorners));
                BroadcastDataInput(nameof(IndividualCornerRadii));
                BroadcastDataInput(nameof(BorderWidth));
                BroadcastDataInput(nameof(UseIndividualBorders));
                BroadcastDataInput(nameof(IndividualBorderWidths));
                SyncUI();
            } catch { }
        }

        public virtual string GetHeader() {
            if (ShowEnabledToggle && !Enabled) return "(Default / Inherited)";
            var hex = ColorUtility.ToHtmlStringRGBA(Color);
            if (ShowGeometry) {
                return $"#{hex} | Border: {BorderWidth * 1000f:F1}mm | Round: {CornerRadius:F2} | Glow: {GlowIntensity:F1}x";
            }
            return $"#{hex} (Alpha: {Color.a:F2}) | Illum: {SelfIllumination:F2} | Glow: {GlowIntensity:F1}x";
        }

        public void CopyFrom(VirtualKeyStateStyle other) {
            if (other == null) return;
            Enabled = other.Enabled;
            Color = other.Color;
            SelfIllumination = other.SelfIllumination;
            GlowColor = other.GlowColor;
            GlowIntensity = other.GlowIntensity;
            CornerRadius = other.CornerRadius;
            UseIndividualCorners = other.UseIndividualCorners;
            IndividualCornerRadii = other.IndividualCornerRadii;
            BorderWidth = other.BorderWidth;
            UseIndividualBorders = other.UseIndividualBorders;
            IndividualBorderWidths = other.IndividualBorderWidths;
            ShowGeometry = other.ShowGeometry;
        }

        public VirtualKeyStateStyle Clone() {
            var clone = StructuredData.Create<VirtualKeyStateStyle>();
            clone.CopyFrom(this);
            return clone;
        }
    }

    public class VirtualKeyElementStyle : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(5)]
        [Label("ENABLE_PRESSED_STATE")]
        public bool EnablePressedState = true;

        [DataInput(10)]
        [Label("NORMAL_STATE")]
        public VirtualKeyStateStyle Normal;

        [DataInput(20)]
        [Label("PRESSED_STATE")]
        public VirtualKeyStateStyle Pressed;

        public bool IsBaseAppearance = true;
        public bool IsBorder = false;

        protected override void OnCreate() {
            base.OnCreate();
            if (Normal == null) Normal = StructuredData.Create<VirtualKeyStateStyle>();
            if (Pressed == null) Pressed = StructuredData.Create<VirtualKeyStateStyle>();

            ConfigureStates(IsBorder);
            SyncUI();

            Watch(nameof(EnablePressedState), delegate {
                if (Pressed != null) {
                    Pressed.Enabled = EnablePressedState;
                    Pressed.SyncUI();
                }
                SyncUI();
            });
        }

        public void ConfigureStates(bool isBorder = false) {
            IsBorder = isBorder;
            if (Normal != null) {
                Normal.ShowEnabledToggle = !IsBaseAppearance;
                Normal.ShowGeometry = isBorder;
                Normal.SyncUI();
            }
            if (Pressed != null) {
                // Pressed state toggle is controlled by EnablePressedState on the parent
                Pressed.ShowEnabledToggle = false;
                Pressed.ShowGeometry = isBorder;
                Pressed.SyncUI();
            }
        }

        public void BroadcastAll() {
            try {
                BroadcastDataInput(nameof(EnablePressedState));
                Normal?.BroadcastAll();
                Pressed?.BroadcastAll();
                BroadcastDataInput(nameof(Normal));
                BroadcastDataInput(nameof(Pressed));
                SyncUI();
            } catch { }
        }

        public void SyncUI() {
            try {
                var pressedPort = GetDataInputPort(nameof(Pressed));
                if (pressedPort != null) {
                    pressedPort.Properties.hidden = !EnablePressedState;
                    BroadcastDataInputProperties(nameof(Pressed));
                }
            } catch { }
        }

        public virtual string GetHeader() {
            var nHex = (Normal != null && Normal.Enabled) ? ColorUtility.ToHtmlStringRGBA(Normal.Color) : "Default";
            if (!EnablePressedState || Pressed == null || !Pressed.Enabled) {
                return $"Normal: #{nHex} (No Press State)";
            }
            var pHex = ColorUtility.ToHtmlStringRGBA(Pressed.Color);
            return $"Normal: #{nHex} ➔ Pressed: #{pHex}";
        }

        public void CopyFrom(VirtualKeyElementStyle other) {
            if (other == null) return;
            EnablePressedState = other.EnablePressedState;
            if (Normal == null) Normal = StructuredData.Create<VirtualKeyStateStyle>();
            if (Pressed == null) Pressed = StructuredData.Create<VirtualKeyStateStyle>();
            Normal.CopyFrom(other.Normal);
            Pressed.CopyFrom(other.Pressed);
        }

        public VirtualKeyElementStyle Clone() {
            var clone = StructuredData.Create<VirtualKeyElementStyle>();
            clone.CopyFrom(this);
            return clone;
        }
    }

    public class VirtualKeyAppearance : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("BODY_STYLE")]
        public VirtualKeyElementStyle Body;

        [DataInput(20)]
        [Label("BORDER_STYLE")]
        public VirtualKeyElementStyle Border;

        [DataInput(30)]
        [Label("PRESS_IN_TRANSITION")]
        public Transition PressInTransition;

        [DataInput(35)]
        [Label("PRESS_OUT_TRANSITION")]
        public Transition PressOutTransition;

        [Trigger(40)]
        [Label("RESET_TO_DEFAULTS")]
        public void TriggerResetToDefaults() {
            ResetToDefaultTheme();
            BroadcastAll();
        }

        public bool IsBaseAppearance = true;
        public Action OnAppearanceChanged;

        protected override void OnCreate() {
            base.OnCreate();
            if (Body == null || Border == null || PressInTransition == null || PressOutTransition == null) {
                ResetToDefaultTheme();
            }
            ConfigureElements();

            Watch(nameof(PressInTransition), delegate { OnAppearanceChanged?.Invoke(); });
            Watch(nameof(PressOutTransition), delegate { OnAppearanceChanged?.Invoke(); });
        }

        public void ConfigureElements() {
            if (Body != null) {
                Body.IsBaseAppearance = IsBaseAppearance;
                Body.ConfigureStates(false);
                Body.SyncUI();
            }
            if (Border != null) {
                Border.IsBaseAppearance = IsBaseAppearance;
                Border.ConfigureStates(true);
                Border.SyncUI();
            }
        }

        public void BroadcastAll() {
            try {
                Body?.BroadcastAll();
                Border?.BroadcastAll();
                BroadcastDataInput(nameof(Body));
                BroadcastDataInput(nameof(Border));
                BroadcastDataInput(nameof(PressInTransition));
                BroadcastDataInput(nameof(PressOutTransition));
                ConfigureElements();
                OnAppearanceChanged?.Invoke();
            } catch { }
        }

        public void ResetToDefaultTheme() {
            if (Body == null) Body = StructuredData.Create<VirtualKeyElementStyle>();
            if (Border == null) Border = StructuredData.Create<VirtualKeyElementStyle>();

            Body.IsBaseAppearance = IsBaseAppearance;
            Body.EnablePressedState = true;

            if (Body.Normal == null) Body.Normal = StructuredData.Create<VirtualKeyStateStyle>();
            Body.Normal.Enabled = true;
            // Warudo HologramTrackpad fill (#0032DB80) with 0 glow for exact unlit look
            Body.Normal.Color = new Color(0.0000f, 0.1961f, 0.8588f, 0.5020f);
            Body.Normal.SelfIllumination = 1.0f;
            Body.Normal.GlowColor = new Color(0.0000f, 0.1961f, 0.8588f, 1.0f);
            Body.Normal.GlowIntensity = 0.0f;

            if (Body.Pressed == null) Body.Pressed = StructuredData.Create<VirtualKeyStateStyle>();
            Body.Pressed.Enabled = true;
            // Slightly brighter electric azure body with glow on press
            Body.Pressed.Color = new Color(0.12f, 0.35f, 0.98f, 0.80f);
            Body.Pressed.SelfIllumination = 1.0f;
            Body.Pressed.GlowColor = new Color(0.15f, 0.45f, 1.0f, 1.0f);
            Body.Pressed.GlowIntensity = 1.5f;

            Border.IsBaseAppearance = IsBaseAppearance;
            Border.EnablePressedState = true;

            if (Border.Normal == null) Border.Normal = StructuredData.Create<VirtualKeyStateStyle>();
            Border.Normal.Enabled = true;
            // Warudo HologramTrackpad border (#56FFFFFF) with 0 glow for exact unlit look
            Border.Normal.Color = new Color(0.3373f, 1.0000f, 1.0000f, 1.0000f);
            Border.Normal.SelfIllumination = 1.0f;
            Border.Normal.GlowColor = new Color(0.3373f, 1.0000f, 1.0000f, 1.0000f);
            Border.Normal.GlowIntensity = 0.0f;
            Border.Normal.BorderWidth = 0.0025f;
            Border.Normal.UseIndividualBorders = false;
            Border.Normal.IndividualBorderWidths = new Vector4(0.0025f, 0.0025f, 0.0025f, 0.0025f);
            Border.Normal.CornerRadius = 0.25f;
            Border.Normal.UseIndividualCorners = false;
            Border.Normal.IndividualCornerRadii = new Vector4(0.25f, 0.25f, 0.25f, 0.25f);

            if (Border.Pressed == null) Border.Pressed = StructuredData.Create<VirtualKeyStateStyle>();
            Border.Pressed.Enabled = true;
            // Glowing white border on press with dynamic thickening
            Border.Pressed.Color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            Border.Pressed.SelfIllumination = 1.0f;
            Border.Pressed.GlowColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            Border.Pressed.GlowIntensity = 2.5f;
            Border.Pressed.BorderWidth = 0.0035f;
            Border.Pressed.UseIndividualBorders = false;
            Border.Pressed.IndividualBorderWidths = new Vector4(0.0035f, 0.0035f, 0.0035f, 0.0035f);
            Border.Pressed.CornerRadius = 0.25f;
            Border.Pressed.UseIndividualCorners = false;
            Border.Pressed.IndividualCornerRadii = new Vector4(0.25f, 0.25f, 0.25f, 0.25f);

            if (PressInTransition == null) PressInTransition = StructuredData.Create<Transition>();
            PressInTransition.Time = 0.02f;
            PressInTransition.Ease = Ease.Linear;

            if (PressOutTransition == null) PressOutTransition = StructuredData.Create<Transition>();
            PressOutTransition.Time = 0.12f;
            PressOutTransition.Ease = Ease.OutCubic;

            ConfigureElements();
        }

        public virtual string GetHeader() {
            float inT = PressInTransition?.Time ?? 0.02f;
            float outT = PressOutTransition?.Time ?? 0.12f;
            return $"Key Appearance (PressIn: {inT:F2}s, PressOut: {outT:F2}s)";
        }

        public void CopyFrom(VirtualKeyAppearance other) {
            if (other == null) return;
            if (Body == null) Body = StructuredData.Create<VirtualKeyElementStyle>();
            if (Border == null) Border = StructuredData.Create<VirtualKeyElementStyle>();
            Body.CopyFrom(other.Body);
            Border.CopyFrom(other.Border);
            if (other.PressInTransition != null) {
                if (PressInTransition == null) PressInTransition = StructuredData.Create<Transition>();
                PressInTransition.Time = other.PressInTransition.Time;
                PressInTransition.Ease = other.PressInTransition.Ease;
            }
            if (other.PressOutTransition != null) {
                if (PressOutTransition == null) PressOutTransition = StructuredData.Create<Transition>();
                PressOutTransition.Time = other.PressOutTransition.Time;
                PressOutTransition.Ease = other.PressOutTransition.Ease;
            }
            ConfigureElements();
        }

        public VirtualKeyAppearance Clone() {
            var clone = StructuredData.Create<VirtualKeyAppearance>();
            clone.IsBaseAppearance = IsBaseAppearance;
            clone.CopyFrom(this);
            return clone;
        }

        public static VirtualKeyAppearance CreateDefault() {
            var app = StructuredData.Create<VirtualKeyAppearance>();
            app.IsBaseAppearance = true;
            app.ResetToDefaultTheme();
            return app;
        }

        public static VirtualKeyAppearance CreateForOverride() {
            var app = StructuredData.Create<VirtualKeyAppearance>();
            app.IsBaseAppearance = false;
            app.ResetToDefaultTheme();
            // Default overrides: override Body Normal, but keep others inherited from base
            if (app.Body != null) {
                app.Body.IsBaseAppearance = false;
                app.Body.Normal.Enabled = true;
                app.Body.EnablePressedState = false;
                if (app.Body.Pressed != null) app.Body.Pressed.Enabled = false;
            }
            if (app.Border != null) {
                app.Border.IsBaseAppearance = false;
                app.Border.Normal.Enabled = false;
                app.Border.EnablePressedState = false;
                if (app.Border.Pressed != null) app.Border.Pressed.Enabled = false;
            }
            app.ConfigureElements();
            return app;
        }
    }
}
