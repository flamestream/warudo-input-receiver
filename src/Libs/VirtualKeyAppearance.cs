using System;
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
        [FloatSlider(0.0005f, 0.015f, 0.0002f)]
        [Label("BORDER_WIDTH")]
        public float BorderWidth = 0.0025f;

        // Whether to display the "Enabled" toggle port in the Warudo inspector
        public bool ShowEnabledToggle = true;

        // Whether to display the BorderWidth slider (true only for Border styles)
        public bool ShowBorderWidth = false;

        protected override void OnCreate() {
            base.OnCreate();
            SyncUI();
            Watch(nameof(Enabled), delegate { SyncUI(); });
        }

        public void SyncUI() {
            try {
                var enabledPort = GetDataInputPort(nameof(Enabled));
                if (enabledPort != null) {
                    enabledPort.Properties.hidden = !ShowEnabledToggle;
                    BroadcastDataInputProperties(nameof(Enabled));
                }

                bool hideFields = ShowEnabledToggle && !Enabled;
                var colorPort = GetDataInputPort(nameof(Color));
                if (colorPort != null) {
                    colorPort.Properties.hidden = hideFields;
                    BroadcastDataInputProperties(nameof(Color));
                }

                var selfIllumPort = GetDataInputPort(nameof(SelfIllumination));
                if (selfIllumPort != null) {
                    selfIllumPort.Properties.hidden = hideFields;
                    BroadcastDataInputProperties(nameof(SelfIllumination));
                }

                var glowColorPort = GetDataInputPort(nameof(GlowColor));
                if (glowColorPort != null) {
                    glowColorPort.Properties.hidden = hideFields;
                    BroadcastDataInputProperties(nameof(GlowColor));
                }

                var glowIntensityPort = GetDataInputPort(nameof(GlowIntensity));
                if (glowIntensityPort != null) {
                    glowIntensityPort.Properties.hidden = hideFields;
                    BroadcastDataInputProperties(nameof(GlowIntensity));
                }

                var bwPort = GetDataInputPort(nameof(BorderWidth));
                if (bwPort != null) {
                    bwPort.Properties.hidden = !ShowBorderWidth || hideFields;
                    BroadcastDataInputProperties(nameof(BorderWidth));
                }
            } catch { }
        }

        public void BroadcastAll() {
            try {
                BroadcastDataInput(nameof(Enabled));
                BroadcastDataInput(nameof(Color));
                BroadcastDataInput(nameof(SelfIllumination));
                BroadcastDataInput(nameof(GlowColor));
                BroadcastDataInput(nameof(GlowIntensity));
                BroadcastDataInput(nameof(BorderWidth));
                SyncUI();
            } catch { }
        }

        public virtual string GetHeader() {
            if (ShowEnabledToggle && !Enabled) return "(Default / Inherited)";
            var hex = ColorUtility.ToHtmlStringRGBA(Color);
            if (ShowBorderWidth) {
                return $"#{hex} | Border: {BorderWidth * 1000f:F1}mm | Glow: {GlowIntensity:F1}x";
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
            BorderWidth = other.BorderWidth;
            ShowBorderWidth = other.ShowBorderWidth;
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
                Normal.ShowBorderWidth = isBorder;
                Normal.SyncUI();
            }
            if (Pressed != null) {
                // Pressed state toggle is controlled by EnablePressedState on the parent, so hide redundant nested toggle
                Pressed.ShowEnabledToggle = false;
                Pressed.ShowBorderWidth = isBorder;
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

        [DataInput(25)]
        [FloatSlider(0f, 1f, 0.01f)]
        [Label("CORNER_ROUNDNESS")]
        public float CornerRadius = 0.25f;

        [DataInput(26)]
        [FloatSlider(0f, 1f, 0.01f)]
        [Label("PRESSED_CORNER_ROUNDNESS")]
        public float PressedCornerRadius = 0.25f;

        [DataInput(30)]
        [FloatSlider(0f, 0.5f, 0.005f)]
        [Label("TRANSITION_DURATION")]
        public float TransitionDuration = 0.05f;

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
            if (Body == null || Border == null) {
                ResetToDefaultTheme();
            }
            ConfigureElements();

            Watch(nameof(CornerRadius), delegate { OnAppearanceChanged?.Invoke(); });
            Watch(nameof(PressedCornerRadius), delegate { OnAppearanceChanged?.Invoke(); });
            Watch(nameof(TransitionDuration), delegate { OnAppearanceChanged?.Invoke(); });
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
                BroadcastDataInput(nameof(CornerRadius));
                BroadcastDataInput(nameof(PressedCornerRadius));
                BroadcastDataInput(nameof(TransitionDuration));
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

            if (Border.Pressed == null) Border.Pressed = StructuredData.Create<VirtualKeyStateStyle>();
            Border.Pressed.Enabled = true;
            // Glowing white border on press with dynamic thickening
            Border.Pressed.Color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            Border.Pressed.SelfIllumination = 1.0f;
            Border.Pressed.GlowColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            Border.Pressed.GlowIntensity = 2.5f;
            Border.Pressed.BorderWidth = 0.0035f;

            CornerRadius = 0.25f;
            PressedCornerRadius = 0.25f;
            TransitionDuration = 0.05f;
            ConfigureElements();
        }

        public virtual string GetHeader() {
            return $"Key Appearance (Transition: {TransitionDuration:F3}s)";
        }

        public void CopyFrom(VirtualKeyAppearance other) {
            if (other == null) return;
            if (Body == null) Body = StructuredData.Create<VirtualKeyElementStyle>();
            if (Border == null) Border = StructuredData.Create<VirtualKeyElementStyle>();
            Body.CopyFrom(other.Body);
            Border.CopyFrom(other.Border);
            CornerRadius = other.CornerRadius;
            PressedCornerRadius = other.PressedCornerRadius;
            TransitionDuration = other.TransitionDuration;
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
