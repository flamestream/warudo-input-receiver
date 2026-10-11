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
        [DataInput(10)]
        [Label("KEY_BINDING_GENERATOR")]
        public KeyboardKeyBindingGenerator Generator;

        [DataInput(20)]
        [Label("KEY_MOVEMENT_DEFINITIONS")]
        public KeyboardKeyBinding[] MovementDefinitions = new KeyboardKeyBinding[0];

        // Compatibility forwarding to Generator
        public KeyGenerationMode GenerationMode {
            get => Generator?.GenerationMode ?? KeyGenerationMode.TiltToKeySurface;
            set {
                if (Generator != null) {
                    Generator.GenerationMode = value;
                    Generator.UpdateStatus();
                }
            }
        }

        public KeyboardFingertipOffsets FingertipOffsets => Generator?.FingertipOffsets;

        public void TriggerAutoGenerateKeyBindings() {
            Generator?.TriggerAutoGenerateKeyBindings();
        }

        public string GetHeader() {
            int count = MovementDefinitions != null ? MovementDefinitions.Length : 0;
            return $"{"CLICK_TO_CONFIGURE".Localized()} ({count})";
        }

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            if (Generator == null) {
                Generator = StructuredData.Create<KeyboardKeyBindingGenerator>();
                Generator.Parent = Parent;
                Generator.CollapsedSelf = true;
            }
            Watch(nameof(Generator), delegate {
                if (Generator != null) {
                    Generator.Parent = Parent;
                    Generator.UpdateStatus();
                }
            });
            Watch(nameof(MovementDefinitions), delegate {
                Parent?.RefreshKeyBindingsParent();
            });
        }
    }
}
