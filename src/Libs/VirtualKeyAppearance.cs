using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream
{
    public class VirtualKeyStateStyle : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("COLOR")]
        public Color Color = Color.white;

        [DataInput(20)]
        [Label("GLOW_COLOR")]
        public Color GlowColor = Color.white;

        [DataInput(30)]
        [FloatSlider(0f, 5.0f, 0.1f)]
        [Label("GLOW_INTENSITY")]
        public float GlowIntensity = 0f;

        public virtual string GetHeader() {
            var hex = ColorUtility.ToHtmlStringRGBA(Color);
            return $"#{hex} (Alpha: {Color.a:F2}) | Glow: {GlowIntensity:F1}x";
        }

        public void CopyFrom(VirtualKeyStateStyle other) {
            if (other == null) return;
            Color = other.Color;
            GlowColor = other.GlowColor;
            GlowIntensity = other.GlowIntensity;
        }

        public VirtualKeyStateStyle Clone() {
            var clone = StructuredData.Create<VirtualKeyStateStyle>();
            clone.CopyFrom(this);
            return clone;
        }
    }

    public class VirtualKeyElementStyle : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("NORMAL_STATE")]
        public VirtualKeyStateStyle Normal;

        [DataInput(20)]
        [Label("PRESSED_STATE")]
        public VirtualKeyStateStyle Pressed;

        protected override void OnCreate() {
            base.OnCreate();
            if (Normal == null) Normal = StructuredData.Create<VirtualKeyStateStyle>();
            if (Pressed == null) Pressed = StructuredData.Create<VirtualKeyStateStyle>();
        }

        public virtual string GetHeader() {
            var nHex = Normal != null ? ColorUtility.ToHtmlStringRGBA(Normal.Color) : "----";
            var pHex = Pressed != null ? ColorUtility.ToHtmlStringRGBA(Pressed.Color) : "----";
            return $"Normal: #{nHex} ➔ Pressed: #{pHex}";
        }

        public void CopyFrom(VirtualKeyElementStyle other) {
            if (other == null) return;
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
        [FloatSlider(0f, 0.5f, 0.005f)]
        [Label("TRANSITION_DURATION")]
        public float TransitionDuration = 0.05f;

        [Trigger(40)]
        [Label("RESET_TO_DEFAULTS")]
        public void TriggerResetToDefaults() {
            ResetToDefaultTheme();
        }

        protected override void OnCreate() {
            base.OnCreate();
            if (Body == null || Border == null) {
                ResetToDefaultTheme();
            }
        }

        public void ResetToDefaultTheme() {
            if (Body == null) Body = StructuredData.Create<VirtualKeyElementStyle>();
            if (Border == null) Border = StructuredData.Create<VirtualKeyElementStyle>();

            if (Body.Normal == null) Body.Normal = StructuredData.Create<VirtualKeyStateStyle>();
            Body.Normal.Color = new Color(0.21f, 0.52f, 0.89f, 0.85f);
            Body.Normal.GlowColor = new Color(0.21f, 0.52f, 0.89f, 1f);
            Body.Normal.GlowIntensity = 0f;

            if (Body.Pressed == null) Body.Pressed = StructuredData.Create<VirtualKeyStateStyle>();
            Body.Pressed.Color = new Color(1f, 1f, 1f, 1.0f);
            Body.Pressed.GlowColor = new Color(1f, 1f, 1f, 1f);
            Body.Pressed.GlowIntensity = 2.5f;

            if (Border.Normal == null) Border.Normal = StructuredData.Create<VirtualKeyStateStyle>();
            Border.Normal.Color = new Color(0.39f, 0.90f, 1.0f, 1.0f);
            Border.Normal.GlowColor = new Color(0.39f, 0.90f, 1.0f, 1f);
            Border.Normal.GlowIntensity = 0.5f;

            if (Border.Pressed == null) Border.Pressed = StructuredData.Create<VirtualKeyStateStyle>();
            Border.Pressed.Color = new Color(0.85f, 0.96f, 1f, 1.0f);
            Border.Pressed.GlowColor = new Color(0.75f, 0.95f, 1f, 1f);
            Border.Pressed.GlowIntensity = 3.0f;

            TransitionDuration = 0.05f;
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
            TransitionDuration = other.TransitionDuration;
        }

        public VirtualKeyAppearance Clone() {
            var clone = StructuredData.Create<VirtualKeyAppearance>();
            clone.CopyFrom(this);
            return clone;
        }

        public static VirtualKeyAppearance CreateDefault() {
            var app = StructuredData.Create<VirtualKeyAppearance>();
            app.ResetToDefaultTheme();
            return app;
        }
    }
}
