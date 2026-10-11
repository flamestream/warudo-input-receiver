using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream {
    public class GuideSlabTransform : StructuredData {
        [DataInput(10)]
        [Label("POSITION")]
        public Vector3 Position = Vector3.zero;

        [DataInput(20)]
        [Label("ROTATION")]
        public Vector3 Rotation = Vector3.zero;

        [DataInput(30)]
        [Label("SCALE")]
        public Vector3 Scale = Vector3.one;

        public Action OnChange;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Position), () => OnChange?.Invoke());
            Watch(nameof(Rotation), () => OnChange?.Invoke());
            Watch(nameof(Scale), () => OnChange?.Invoke());
        }
    }
}
