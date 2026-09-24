using Cysharp.Threading.Tasks;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Serializations;

namespace FlameStream {
    public enum RestHandParentType {
        [Label("REST_HAND_PARENT_WORLD")]
        World,
        [Label("REST_HAND_PARENT_CHARACTER")]
        Character,
        [Label("REST_HAND_PARENT_SCREEN")]
        Screen
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
        public RestHandParentType ParentType = RestHandParentType.Character;

        [DataInput(20)]
        [Label("REST_HAND_TARGET")]
        [Description("REST_HAND_TARGET_DESC")]
        public CollapsibleVisualSetupTransform Target;

        [DataInput(25)]
        [Label("USE_POLE")]
        [Description("USE_POLE_DESC")]
        public bool UsePole = false;

        [DataInput(30)]
        [Label("REST_HAND_POLE")]
        [Description("REST_HAND_POLE_DESC")]
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

            if (Pole == null) {
                Pole = StructuredData.Create<CollapsibleVisualSetupTransform>();
                Pole.Position = rightHanded ? new Vector3(-0.4f, 0.65f, -0.2f) : new Vector3(0.4f, 0.65f, -0.2f);
                Pole.Rotation = Vector3.zero;
                Pole.Scale = Vector3.one;
                Pole.SetDataInput(nameof(Pole.Scale), Vector3.one, true);
            }
            Pole.HideScale();
            Pole.CollapsedSelf = true;
            Pole.ShowOriginIndicator = false;

            Watch(nameof(Enabled), delegate {
                SyncUI();
                Parent?.OnRestHandSettingChange();
            });
            Watch(nameof(ParentType), delegate {
                Parent?.OnRestHandParentTypeChange();
            });
            Watch(nameof(UsePole), delegate {
                SyncUI();
                Parent?.OnRestHandSettingChange();
            });
            Watch(nameof(Target), delegate {
                Parent?.SetupRestHandTransform();
            });
            Watch(nameof(Pole), delegate {
                Parent?.SetupRestHandPoleTransform();
            });

            SyncUI();
        }

        public void SyncUI() {
            var parentTypePort = GetDataInputPort(nameof(ParentType));
            var targetPort = GetDataInputPort(nameof(Target));
            var usePolePort = GetDataInputPort(nameof(UsePole));
            var polePort = GetDataInputPort(nameof(Pole));

            if (parentTypePort != null) {
                parentTypePort.Properties.hidden = !Enabled;
                BroadcastDataInputProperties(nameof(ParentType));
            }
            if (targetPort != null) {
                targetPort.Properties.hidden = !Enabled;
                BroadcastDataInputProperties(nameof(Target));
            }
            if (usePolePort != null) {
                usePolePort.Properties.hidden = !Enabled;
                BroadcastDataInputProperties(nameof(UsePole));
            }
            if (polePort != null) {
                polePort.Properties.hidden = !Enabled || !UsePole;
                BroadcastDataInputProperties(nameof(Pole));
            }

            RequestUiRefresh();
            Parent?.RequestUiRefresh();
        }
    }
}
