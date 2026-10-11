using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream {
    public class GuideLine : StructuredData, ICollapsibleStructuredData {
        [DataInput(10)]
        [Label("COLOR")]
        public Color Color = Color.white;

        [DataInput(20)]
        [FloatSlider(0.1f, 5.0f, 0.05f)]
        [Label("LENGTH_FACTOR")]
        public float LengthFactor = 1.0f;

        [DataInput(30)]
        [FloatSlider(0.1f, 5.0f, 0.05f)]
        [Label("THICKNESS_FACTOR")]
        public float ThicknessFactor = 1.0f;

        [DataInput(40)]
        [Label("GLOW")]
        public bool Glow = true;

        public Action OnChange;

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;

            SetDataInput(nameof(Color), Color, true);
            SetDataInput(nameof(LengthFactor), LengthFactor, true);
            SetDataInput(nameof(ThicknessFactor), ThicknessFactor, true);
            SetDataInput(nameof(Glow), Glow, true);

            Watch(nameof(Color), () => OnChange?.Invoke());
            Watch(nameof(LengthFactor), () => OnChange?.Invoke());
            Watch(nameof(ThicknessFactor), () => OnChange?.Invoke());
            Watch(nameof(Glow), () => OnChange?.Invoke());
        }

        public string GetHeader() {
            return $"Line (L: {LengthFactor:F2}x, T: {ThicknessFactor:F2}x, Glow: {(Glow ? "On" : "Off")})";
        }

        public void SetDefaults(Color defaultColor, float defaultLength, float defaultThickness, bool defaultGlow) {
            Color = defaultColor;
            LengthFactor = defaultLength;
            ThicknessFactor = defaultThickness;
            Glow = defaultGlow;

            SetDataInput(nameof(Color), defaultColor, true);
            SetDataInput(nameof(LengthFactor), defaultLength, true);
            SetDataInput(nameof(ThicknessFactor), defaultThickness, true);
            SetDataInput(nameof(Glow), defaultGlow, true);
        }
    }

    public class HandGuideLine : GuideLine {
        public HandGuideLine() {
            Color = new Color(0.1f, 0.45f, 0.95f, 0.9f);
            LengthFactor = 1.0f;
            ThicknessFactor = 1.0f;
            Glow = true;
        }
    }

    public class ElbowGuideLine : GuideLine {
        public ElbowGuideLine() {
            Color = new Color(0.1f, 0.45f, 0.95f, 0.9f);
            LengthFactor = 1.0f;
            ThicknessFactor = 1.0f;
            Glow = true;
        }
    }
}
