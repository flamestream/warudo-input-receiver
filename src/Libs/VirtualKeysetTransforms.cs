using System;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class VirtualKeysetTransforms : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("LEFT_KEY_ORIGIN")]
        [Description("LEFT_KEY_ORIGIN_DESC")]
        public CollapsibleVisualSetupTransform Left;

        [DataInput(20)]
        [Label("RIGHT_KEY_ORIGIN")]
        [Description("RIGHT_KEY_ORIGIN_DESC")]
        public CollapsibleVisualSetupTransform Right;

        [DataInput(30)]
        [Label("ROOT_KEY_ORIGIN")]
        [Description("ROOT_KEY_ORIGIN_DESC")]
        public CollapsibleVisualSetupTransform Root;

        public string GetHeader() => "KEY_ORIGINS".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            if (Left != null) Left.CollapsedSelf = true;
            if (Right != null) Right.CollapsedSelf = true;
            if (Root != null) Root.CollapsedSelf = true;
        }
    }
}
