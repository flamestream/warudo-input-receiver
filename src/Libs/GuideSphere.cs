using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream {
    public class GuideSphere : StructuredData, ICollapsibleStructuredData {
        [DataInput(10)]
        [Label("COLOR")]
        public Color Color = Color.white;

        [DataInput(20)]
        [FloatSlider(0.1f, 5.0f, 0.05f)]
        [Label("RADIUS_FACTOR")]
        public float RadiusFactor = 1.0f;

        [DataInput(30)]
        [Label("GLOW")]
        public bool Glow = true;

        public Action OnChange;

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;

            SetDataInput(nameof(Color), Color, true);
            SetDataInput(nameof(RadiusFactor), RadiusFactor, true);
            SetDataInput(nameof(Glow), Glow, true);

            Watch(nameof(Color), () => OnChange?.Invoke());
            Watch(nameof(RadiusFactor), () => OnChange?.Invoke());
            Watch(nameof(Glow), () => OnChange?.Invoke());
        }

        public string GetHeader() {
            return $"Sphere (R: {RadiusFactor:F2}x, Glow: {(Glow ? "On" : "Off")})";
        }

        public void SetDefaults(Color defaultColor, float defaultRadius, bool defaultGlow) {
            Color = defaultColor;
            RadiusFactor = defaultRadius;
            Glow = defaultGlow;

            SetDataInput(nameof(Color), defaultColor, true);
            SetDataInput(nameof(RadiusFactor), defaultRadius, true);
            SetDataInput(nameof(Glow), defaultGlow, true);
        }
    }

    public class FingerTipGuideSphere : GuideSphere {
        public FingerTipGuideSphere() {
            Color = new Color(0.1f, 0.95f, 0.2f, 0.9f);
            RadiusFactor = 1.0f;
            Glow = true;
        }
    }

    public class KeyCenterGuideSphere : GuideSphere {
        public KeyCenterGuideSphere() {
            Color = new Color(0.95f, 0.15f, 0.15f, 0.9f);
            RadiusFactor = 1.0f;
            Glow = true;
        }
    }

    public class OriginGuideSphere : GuideSphere {
        public OriginGuideSphere() {
            Color = new Color(1f, 0.1f, 0.1f, 0.95f);
            RadiusFactor = 1.0f;
            Glow = true;
        }
    }
}
