using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public enum KeyGenerationMode {
        [Label("GENERATION_MODE_TILT_TO_SURFACE")]
        TiltToKeySurface,
        [Label("GENERATION_MODE_MAINTAIN_REST")]
        MaintainRestOrientation
    }

    public class KeyboardFingerMovementCollection : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(5)]
        [Label("KEY_GENERATION_MODE")]
        public KeyGenerationMode GenerationMode = KeyGenerationMode.TiltToKeySurface;

        [DataInput(6)]
        [Label("FINGERTIP_OFFSETS")]
        public KeyboardFingertipOffsets FingertipOffsets;

        [Trigger(10)]
        [Label("AUTO_GENERATE_KEY_BINDINGS")]
        [Description("AUTO_GENERATE_KEY_BINDINGS_DESC")]
        public void TriggerAutoGenerateKeyBindings() {
            Parent?.AutoGenerateKeyBindingsFromFloatingKeys();
        }

        [DataInput(20)]
        [Label("KEY_MOVEMENT_DEFINITIONS")]
        public KeyboardKeyBinding[] MovementDefinitions = new KeyboardKeyBinding[0];

        public string GetHeader() {
            int count = MovementDefinitions != null ? MovementDefinitions.Length : 0;
            return $"{"FINGER_MOVEMENT_DEFINITIONS".Localized()} ({count})";
        }

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            if (FingertipOffsets == null) {
                FingertipOffsets = StructuredData.Create<KeyboardFingertipOffsets>();
                FingertipOffsets.Parent = Parent;
                FingertipOffsets.CollapsedSelf = true;
            }
            Watch(nameof(MovementDefinitions), delegate {
                Parent?.RefreshKeyBindingsParent();
            });
            Watch(nameof(FingertipOffsets), delegate {
                if (FingertipOffsets != null) {
                    FingertipOffsets.Parent = Parent;
                }
            });
        }
    }
}
