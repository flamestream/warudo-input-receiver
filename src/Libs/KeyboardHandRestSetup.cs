using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class KeyboardHandRestSetup : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("RETURN_TO_REST")]
        public bool ReturnToRest = true;

        [DataInput(20)]
        [Label("REST_TIMEOUT")]
        [FloatSlider(0.1f, 30f)]
        public float RestTimeout = 3.0f;

        [DataInput(30)]
        [Label("LEFT_HAND_REST")]
        [Description("LEFT_HAND_REST_DESC")]
        public CollapsibleVisualSetupTransform Left;

        [DataInput(40)]
        [Label("RIGHT_HAND_REST")]
        [Description("RIGHT_HAND_REST_DESC")]
        public CollapsibleVisualSetupTransform Right;

        public string GetHeader() => "HAND_REST_POSES".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            if (Left == null) {
                Left = StructuredData.Create<CollapsibleVisualSetupTransform>();
                Left.Position = new Vector3(-0.07f, 0.02f, 0f);
                Left.Rotation = Vector3.zero;
                Left.Scale = Vector3.one;
            }
            Left.CollapsedSelf = true;

            if (Right == null) {
                Right = StructuredData.Create<CollapsibleVisualSetupTransform>();
                Right.Position = new Vector3(0.07f, 0.02f, 0f);
                Right.Rotation = Vector3.zero;
                Right.Scale = Vector3.one;
            }
            Right.CollapsedSelf = true;

            Watch(nameof(Left), delegate {
                Parent?.SetupLeftHandRestTransform();
            });
            Watch(nameof(Right), delegate {
                Parent?.SetupRightHandRestTransform();
            });
        }
    }
}
