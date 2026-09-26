using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Serializations;

namespace FlameStream {
    public class PointerElbowPoleSetup : StructuredData<PointerReceiverAsset>, ICollapsibleStructuredData {
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
        [Label("USE_POLE")]
        [Description("USE_POLE_DESC")]
        public bool UsePole = false;

        [DataInput(20)]
        [Label("PARENT")]
        [Description("ELBOW_POLE_PARENT_DESC")]
        public RestHandParentType ParentType = RestHandParentType.Character;

        [DataInput(30)]
        [Label("INITIAL_POLE_TRANSFORM")]
        [Description("INITIAL_POLE_TRANSFORM_DESC")]
        public CollapsibleVisualSetupTransform Pole;

        public bool IsActiveHand;
        public Action OnPoleSettingChange;
        public Action OnParentTypeChange;
        public Action OnPoleTransformChange;

        public string GetHeader() => "ELBOW_POLE_SETUP".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;

            if (Pole == null) {
                Pole = StructuredData.Create<CollapsibleVisualSetupTransform>();
                Pole.Position = Vector3.zero;
                Pole.Rotation = Vector3.zero;
                Pole.Scale = Vector3.one;
                Pole.SetDataInput(nameof(Pole.Scale), Vector3.one, true);
            }
            Pole.HideScale();
            Pole.CollapsedSelf = true;
            Pole.ShowOriginIndicator = false;

            Watch(nameof(UsePole), delegate {
                SyncUI();
                OnPoleSettingChange?.Invoke();
            });
            Watch(nameof(ParentType), delegate {
                OnParentTypeChange?.Invoke();
            });
            Watch(nameof(Pole), delegate {
                OnPoleTransformChange?.Invoke();
            });

            SyncUI();
        }

        public void SyncUI() {
            var parentPort = GetDataInputPort(nameof(ParentType));
            var polePort = GetDataInputPort(nameof(Pole));

            if (parentPort != null) {
                parentPort.Properties.hidden = !UsePole;
                BroadcastDataInputProperties(nameof(ParentType));
            }
            if (polePort != null) {
                polePort.Properties.hidden = !UsePole;
                BroadcastDataInputProperties(nameof(Pole));
            }

            RequestUiRefresh();
            Parent?.RequestUiRefresh();
        }
    }

    public class PointerActiveHandSetup : StructuredData<PointerReceiverAsset>, ICollapsibleStructuredData {
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
        [Label("RIGHT_HANDED")]
        public bool IsRightHanded = true;

        [DataInput(20)]
        [Label("ELBOW_POLE_SETUP")]
        [Description("ELBOW_POLE_SETUP_DESC")]
        public PointerElbowPoleSetup ElbowPole;

        public string GetHeader() => "ACTIVE_HAND_SETUP".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = false;

            if (ElbowPole == null) {
                ElbowPole = StructuredData.Create<PointerElbowPoleSetup>();
                ElbowPole.Parent = Parent;
                ElbowPole.IsActiveHand = true;
                SetDataInput(nameof(ElbowPole), ElbowPole, true);
            }
            ElbowPole.Parent = Parent;
            ElbowPole.IsActiveHand = true;

            Watch(nameof(IsRightHanded), delegate {
                if (Parent != null) {
                    Parent.IsRightHanded = IsRightHanded;
                    Parent.SetDataInput(nameof(Parent.IsRightHanded), IsRightHanded, true);
                    Parent.OnHandSideChange();
                    Parent.OnInputAffectingHandSetupChange();
                    Parent.OnRestHandSettingChange();
                }
            });
            Watch(nameof(ElbowPole), delegate {
                if (ElbowPole != null) {
                    ElbowPole.Parent = Parent;
                    ElbowPole.IsActiveHand = true;
                }
                Parent?.SetupActiveHandPoleTransform();
            });
        }
    }
}
