using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream {
    public class GuideSlab : StructuredData, ICollapsibleStructuredData {
        [DataInput(10)]
        [Label("COLOR")]
        public Color Color = new Color(1f, 1f, 1f, 0.5f);

        [DataInput(20)]
        [Label("GLOW")]
        public bool Glow = false;

        [DataInput(30)]
        [Label("TRANSFORM")]
        public GuideSlabTransform Transform;

        public Action OnChange;

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;

            if (Transform == null) {
                Transform = StructuredData.Create<GuideSlabTransform>();
            }
            Transform.OnChange = () => OnChange?.Invoke();

            SetDataInput(nameof(Color), Color, true);
            SetDataInput(nameof(Glow), Glow, true);

            Watch(nameof(Color), () => OnChange?.Invoke());
            Watch(nameof(Glow), () => OnChange?.Invoke());
            Watch(nameof(Transform), () => {
                if (Transform != null) {
                    Transform.OnChange = () => OnChange?.Invoke();
                }
                OnChange?.Invoke();
            });
        }

        public string GetHeader() {
            var scale = Transform?.Scale ?? Vector3.one;
            return $"Slab (Scale: {scale.x:F1}x{scale.z:F1}, Glow: {(Glow ? "On" : "Off")})";
        }

        public void SetDefaults(Color defaultColor, bool defaultGlow, Vector3 defaultPos, Vector3 defaultRot, Vector3 defaultScale) {
            Color = defaultColor;
            Glow = defaultGlow;

            SetDataInput(nameof(Color), defaultColor, true);
            SetDataInput(nameof(Glow), defaultGlow, true);

            if (Transform == null) {
                Transform = StructuredData.Create<GuideSlabTransform>();
            }
            Transform.Position = defaultPos;
            Transform.Rotation = defaultRot;
            Transform.Scale = defaultScale;

            Transform.SetDataInput(nameof(Transform.Position), defaultPos, true);
            Transform.SetDataInput(nameof(Transform.Rotation), defaultRot, true);
            Transform.SetDataInput(nameof(Transform.Scale), defaultScale, true);
        }
    }

    public class HandSlabTransform : GuideSlabTransform {
        public HandSlabTransform() {
            Position = new Vector3(0f, 0f, 0.025f);
            Rotation = Vector3.zero;
            Scale = Vector3.one;
        }

        protected override void OnCreate() {
            base.OnCreate();
            SetDataInput(nameof(Position), Position, true);
            SetDataInput(nameof(Rotation), Rotation, true);
            SetDataInput(nameof(Scale), Scale, true);
        }
    }

    public class HandGuideSlab : GuideSlab {
        public HandGuideSlab() {
            Color = new Color(0.15f, 0.85f, 0.25f, 0.5f);
            Glow = false;
        }

        protected override void OnCreate() {
            if (Transform == null) {
                Transform = StructuredData.Create<HandSlabTransform>();
            }
            base.OnCreate();
        }
    }

    public class KeyGuideSlab : GuideSlab {
        public KeyGuideSlab() {
            Color = new Color(0.9f, 0.2f, 0.15f, 0.5f);
            Glow = false;
        }

        protected override void OnCreate() {
            if (Transform == null) {
                Transform = StructuredData.Create<GuideSlabTransform>();
            }
            base.OnCreate();
        }
    }
}
