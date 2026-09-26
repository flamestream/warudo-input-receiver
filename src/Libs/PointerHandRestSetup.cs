using Cysharp.Threading.Tasks;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Serializations;

namespace FlameStream {
    public enum AnchorParentType {
        [Label("PARENT_WORLD")]
        World = 0,
        [Label("PARENT_CHARACTER")]
        Character = 1,
        [Label("PARENT_SCREEN")]
        Screen = 2,
        [Label("PARENT_CHARACTER_SHOULDER")]
        CharacterShoulder = 3,
        // Legacy compatibility alias for scenes saved with CharacterUpperArm = 3
        [Hidden]
        CharacterUpperArm = 3
    }

    public enum RestHandParentType {
        [Label("PARENT_WORLD")]
        World = 0,
        [Label("PARENT_CHARACTER")]
        Character = 1,
        [Label("PARENT_SCREEN")]
        Screen = 2,
        [Label("PARENT_CHARACTER_SHOULDER")]
        CharacterShoulder = 3,
        [Hidden]
        CharacterUpperArm = 3
    }

    public class PointerHandRestSetup : StructuredData<PointerReceiverAsset>, ICollapsibleStructuredData {
        private long uiVersion = 0;
        public override long GetVersion() => base.GetVersion() + uiVersion;

        public override SerializedStructuredData Serialize() {
            var serialized = base.Serialize();
            serialized.version = GetVersion();
            return serialized;
        }

        public async void RequestUiRefresh() {
            await UniTask.Yield();
            uiVersion++;
            Broadcast();
        }

        [DataInput(10)]
        [Label("ENABLE")]
        public bool Enabled = false;

        [DataInput(15)]
        [Label("REST_HAND_PARENT")]
        [Description("REST_HAND_PARENT_DESC")]
        public AnchorParentType ParentType = AnchorParentType.World;

        [DataInput(20)]
        [Label("INITIAL_REST_POSITION")]
        [Description("INITIAL_REST_POSITION_DESC")]
        public CollapsibleVisualSetupTransform Target;

        [DataInput(30)]
        [Label("ELBOW_POLE_SETUP")]
        [Description("ELBOW_POLE_SETUP_DESC")]
        public PointerElbowPoleSetup ElbowPole;

        // Legacy compatibility fields for scenes that stored UsePole / Pole on PointerHandRestSetup
        [DataInput]
        [Hidden]
        public bool UsePole = false;

        [DataInput]
        [Hidden]
        public CollapsibleVisualSetupTransform Pole;

        public string GetHeader() => "REST_HAND_SETUP".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            bool rightHanded = Parent?.IsRightHanded ?? true;

            if (Target == null) {
                Target = StructuredData.Create<CollapsibleVisualSetupTransform>();
                Target.Position = rightHanded ? new Vector3(-0.25f, 0.65f, 0.25f) : new Vector3(0.25f, 0.65f, 0.25f);
                Target.Rotation = Vector3.zero;
                Target.Scale = Vector3.one;
                Target.SetDataInput(nameof(Target.Scale), Vector3.one, true);
            }
            Target.HideScale();
            Target.CollapsedSelf = true;
            Target.ShowOriginIndicator = false;

            if (ElbowPole == null) {
                ElbowPole = StructuredData.Create<PointerElbowPoleSetup>();
                ElbowPole.Parent = Parent;
                ElbowPole.IsActiveHand = false;
                ElbowPole.UsePole = UsePole;
                if (Pole != null) {
                    ElbowPole.Pole = Pole;
                } else if (ElbowPole.Pole != null) {
                    ElbowPole.Pole.Position = rightHanded ? new Vector3(-0.4f, 0.65f, -0.2f) : new Vector3(0.4f, 0.65f, -0.2f);
                }
                SetDataInput(nameof(ElbowPole), ElbowPole, true);
            }
            ElbowPole.Parent = Parent;
            ElbowPole.IsActiveHand = false;

            Watch(nameof(UsePole), delegate {
                if (ElbowPole != null && ElbowPole.UsePole != UsePole) {
                    ElbowPole.UsePole = UsePole;
                    ElbowPole.SetDataInput(nameof(ElbowPole.UsePole), UsePole, true);
                    ElbowPole.BroadcastDataInput(nameof(ElbowPole.UsePole));
                }
            });

            Watch(nameof(Enabled), delegate {
                SyncUI();
                Parent?.OnRestHandSettingChange();
            });
            Watch(nameof(ParentType), delegate {
                Parent?.OnRestHandParentTypeChange();
            });
            Watch(nameof(Target), delegate {
                Parent?.SetupRestHandTransform();
            });
            Watch(nameof(ElbowPole), delegate {
                if (ElbowPole != null) {
                    ElbowPole.Parent = Parent;
                    ElbowPole.IsActiveHand = false;
                }
                Parent?.SetupRestHandPoleTransform();
            });

            SyncUI();
        }

        public void SyncUI() {
            var parentTypePort = GetDataInputPort(nameof(ParentType));
            var targetPort = GetDataInputPort(nameof(Target));
            var elbowPolePort = GetDataInputPort(nameof(ElbowPole));

            if (parentTypePort != null) {
                parentTypePort.Properties.hidden = !Enabled;
                BroadcastDataInputProperties(nameof(ParentType));
            }
            if (targetPort != null) {
                targetPort.Properties.hidden = !Enabled;
                BroadcastDataInputProperties(nameof(Target));
            }
            if (elbowPolePort != null) {
                elbowPolePort.Properties.hidden = !Enabled;
                BroadcastDataInputProperties(nameof(ElbowPole));
            }

            ElbowPole?.SyncUI();
            RequestUiRefresh();
            Parent?.RequestUiRefresh();
        }
    }
}
