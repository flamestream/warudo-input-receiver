using System.Linq;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Serializations;

namespace FlameStream
{
    public class KeyboardKeyBindingGenerator : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [Markdown(5)]
        public string ReferenceStatus = "";

        [DataInput(10)]
        [Label("KEY_GENERATION_MODE")]
        public KeyGenerationMode GenerationMode = KeyGenerationMode.TiltToKeySurface;

        [DataInput(15)]
        [Label("FINGERTIP_OFFSETS")]
        public KeyboardFingertipOffsets FingertipOffsets;

        [Trigger(20)]
        [Label("AUTO_GENERATE_KEY_BINDINGS")]
        public void TriggerAutoGenerateKeyBindings() {
            Parent?.AutoGenerateKeyBindingsFromFloatingKeys();
        }

        public string GetHeader() => "KEY_BINDING_GENERATOR".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            if (FingertipOffsets == null) {
                FingertipOffsets = StructuredData.Create<KeyboardFingertipOffsets>();
                FingertipOffsets.Parent = Parent;
                FingertipOffsets.CollapsedSelf = true;
            }
            Watch(nameof(FingertipOffsets), delegate {
                if (FingertipOffsets != null) {
                    FingertipOffsets.Parent = Parent;
                }
            });
            Watch(nameof(GenerationMode), delegate {
                UpdateStatus();
            });
            UpdateStatus();
        }

        public void UpdateStatus() {
            if (Parent == null) return;

            if (GenerationMode == KeyGenerationMode.TiltToKeySurface) {
                var leftRef = Parent.GetHandAlignmentReferenceKey(true);
                var rightRef = Parent.GetHandAlignmentReferenceKey(false);

                string leftText;
                if (leftRef != null) {
                    leftText = leftRef.IsHandAlignmentReference
                        ? $"**`[{leftRef.Key}]`**"
                        : $"**`[{leftRef.Key}]`** ({"GENERATION_REF_DEFAULT_FIRST_LEFT".Localized()})";
                } else {
                    leftText = $"*{"GENERATION_REF_NONE_BED".Localized()}*";
                }

                string rightText;
                if (rightRef != null) {
                    rightText = rightRef.IsHandAlignmentReference
                        ? $"**`[{rightRef.Key}]`**"
                        : $"**`[{rightRef.Key}]`** ({"GENERATION_REF_DEFAULT_FIRST_RIGHT".Localized()})";
                } else {
                    rightText = $"*{"GENERATION_REF_NONE_BED".Localized()}*";
                }

                ReferenceStatus = $"{"GENERATION_INFO_TILT_TO_SURFACE".Localized()}\n\n" +
                                  $"- 🫲 **{"LEFT_HAND".Localized()}**: {leftText}\n" +
                                  $"- 🫱 **{"RIGHT_HAND".Localized()}**: {rightText}";
            } else {
                ReferenceStatus = "GENERATION_INFO_MAINTAIN_REST".Localized();
            }

            try {
                var mdPort = GetDataInputPort(nameof(ReferenceStatus));
                if (mdPort != null) {
                    BroadcastDataInput(nameof(ReferenceStatus));
                }
            } catch { }
        }
    }
}
