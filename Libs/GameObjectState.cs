using System;
using System.Diagnostics;
using DG.Tweening;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Utils;

namespace FlameStream {
    public class GameObjectState : StructuredData {
        [Markdown(0)]
        public string Info;

        [DataInput(10)]
        [Label("ENABLE")]
        public bool Enabled = false;

        [DataInput(20)]
        [Label("TRANSFORM")]
        public VisualSetupTransform Transform;

        [DataInput(30)]
        [Label("ENTER_TRANSITION")]
        public PoseTransition EnterTransition;

        [DataInput(40)]
        [Hidden]
        public string Label;

        bool isReady;

        public GameObjectState() {}
        public GameObjectState(string label) {
            Label = label;
            RefreshInfo(false);
        }

        public Action<GameObjectState, VisualSetupTransform, VisualSetupAnchorAsset> OnApplyAnchor;
        public Action<GameObjectState, VisualSetupTransform, VisualSetupAnchorAsset> OnCreateAnchor;
        public Action<GameObjectState> OnEnterVisualSetup;
        public Action<GameObjectState, bool> OnExitVisualSetup;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Enabled), delegate {
                SyncEnabledUI();
            });
            Watch(nameof(Label), delegate {
                Transform.LocalizationKey = GetLabelLocalizationKey();
            });
            SyncEnabledUI();
        }

        protected virtual void OnReady() {
            Transform.LocalizationKey = GetLabelLocalizationKey();
            Transform.OnApplyAnchor = (tr,  a) => { OnApplyAnchor?.Invoke(this, tr, a); };
            Transform.OnCreateAnchor = (tr, a) => { OnCreateAnchor?.Invoke(this, tr, a); };
            Transform.OnEnterVisualSetup = (tr) => { OnEnterVisualSetup?.Invoke(this); };
            Transform.OnExitVisualSetup = (ia) => { OnExitVisualSetup?.Invoke(this, ia); };
        }

        protected override void OnUpdate() {
            base.OnUpdate();

            // OnReady: One-time execution before update events are sent
            // This is needed because assets are loaded in order, triggering OnCreate events immediately
            // which may be an issue if some of them are dependent on other assets that are yet to be loaded.
            if (!isReady) {
                isReady = true;
                OnReady();
            }
        }

        public virtual void SyncEnabledUI() {
            GetDataInputPort(nameof(Transform)).Properties.hidden = !Enabled;
            GetDataInputPort(nameof(EnterTransition)).Properties.hidden = !Enabled;
            BroadcastDataInputProperties(nameof(Transform));
            BroadcastDataInputProperties(nameof(EnterTransition));
        }

        public string GetLabelLocalizationKey() {
            if (Label == null) return null;
            return string.Join("_", Label.Split()).ToUpper();
        }

        public void RefreshInfo(bool broadcast) {
            Info = $"POSE_INFO_{GetLabelLocalizationKey()}".ToUpper().Localized();
            if (broadcast) BroadcastDataInput(nameof(Info));
        }

        public string GetAnimationLayerId(string prefix = "") {
            if (Label == null) return null;
            return $"{prefix} {Label}";
        }
    }

    public class BaseState : GameObjectState {

        [DataInput]
        [Label("ACTIVE_STATE")]
        public ActiveGameObjectState ActiveState = Create<ActiveGameObjectState>(st => {
            st.Enabled = true;
        });

        public BaseState() {}
        public BaseState(string label) : base(label) {}

        protected override void OnReady() {
            base.OnReady();
            ActiveState.Label = $"{Label} Active";
            ActiveState.RefreshInfo(true);
            ActiveState.OnApplyAnchor = (p, tr,  a) => { OnApplyAnchor?.Invoke(p, tr, a); };
            ActiveState.OnCreateAnchor = (p, tr, a) => { OnCreateAnchor?.Invoke(p, tr, a); };
            ActiveState.OnEnterVisualSetup = (p) => { OnEnterVisualSetup?.Invoke(p); };
            ActiveState.OnExitVisualSetup = (p, ia) => { OnExitVisualSetup?.Invoke(p, ia); };
            ActiveState.SyncEnabledUI();
        }

        public override void SyncEnabledUI() {
            base.SyncEnabledUI();
            GetDataInputPort(nameof(ActiveState)).Properties.hidden = !Enabled;
            BroadcastDataInputProperties(nameof(ActiveState));
        }
    }

    public class ActiveGameObjectState : GameObjectState, ICollapsibleStructuredData {

        public string GetHeader() {
            return (Enabled ? "ENABLED" : "DISABLED").Localized();
        }
    }

    public class PoseState : GameObjectState {

        [DataInput(11)]
        [PreviewGallery]
        [AutoCompleteResource("CharacterAnimation", null)]
        [Label("ANIMATION")]
        public string Animation;

        public PoseState() {}
        public PoseState(string label) : base(label) {}

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Animation), delegate {
                OnAnimationChange?.Invoke(this);
            });
            Watch(nameof(Transform), delegate {
                if (Transform == null) return;
                Transform.GetDataInputPort(nameof(Transform.Scale)).Properties.hidden = true;
                Transform.BroadcastDataInputProperties(nameof(Transform.Scale));
            });
        }

        public override void SyncEnabledUI() {
            base.SyncEnabledUI();
            GetDataInputPort(nameof(Animation)).Properties.hidden = !Enabled;
            BroadcastDataInputProperties(nameof(Animation));
        }

        float animationWeight;
        Tween animationWeightTween;

        public Action<GameObjectState> OnAnimationChange;
        public Action<GameObjectState, float, bool> OnAnimationWeightChange;

        public void StartAnimationTransition(bool inTransition) {
            var targetWeight = inTransition ? 1f : 0f;
            if (animationWeight == targetWeight) return;

            animationWeightTween?.Kill(true);
            if (EnterTransition.Time > 0f) {
                OnAnimationWeightChange?.Invoke(this, animationWeight, inTransition);
                animationWeightTween = DOTween.To(
                    () => animationWeight,
                    delegate(float it) {
                        animationWeight = it;
                        OnAnimationWeightChange?.Invoke(this, animationWeight, false);
                    },
                    targetWeight,
                    EnterTransition.Time
                ).SetEase(EnterTransition.Ease);
            } else {
                animationWeight = targetWeight;
                OnAnimationWeightChange?.Invoke(this, targetWeight, inTransition);
            }
        }
    }

    public class BasePoseState : PoseState {

        [DataInput]
        [Label("ACTIVE_STATE")]
        public ActivePoseState ActiveState = Create<ActivePoseState>(st => {
            st.Enabled = true;
            st.SyncEnabledUI();
        });

        public BasePoseState() {}
        public BasePoseState(string label) : base(label) {}

        protected override void OnReady() {
            base.OnReady();
            ActiveState.Label = $"{Label} Active";
            ActiveState.RefreshInfo(true);
            ActiveState.OnAnimationChange = (p) => { OnAnimationChange?.Invoke(p); };
            ActiveState.OnAnimationWeightChange = (p, w, r) => { OnAnimationWeightChange?.Invoke(p, w, r); };
            ActiveState.OnApplyAnchor = (p, tr,  a) => { OnApplyAnchor?.Invoke(p, tr, a); };
            ActiveState.OnCreateAnchor = (p, tr, a) => { OnCreateAnchor?.Invoke(p, tr, a); };
            ActiveState.OnEnterVisualSetup = (p) => { OnEnterVisualSetup?.Invoke(p); };
            ActiveState.OnExitVisualSetup = (p, ia) => { OnExitVisualSetup?.Invoke(p, ia); };
        }

        public override void SyncEnabledUI() {
            base.SyncEnabledUI();
            GetDataInputPort(nameof(ActiveState)).Properties.hidden = !Enabled;
            BroadcastDataInputProperties(nameof(ActiveState));
        }
    }

    public class ActivePoseState : PoseState, ICollapsibleStructuredData {

        public string GetHeader() {
            return (Enabled ? "ENABLED" : "DISABLED").Localized();
        }
    }

    public class MouseHandState : BasePoseState {
        public MouseHandState() : base("Mouse Hand") {}

            protected override void OnCreate() {
            Enabled = true;
            base.OnCreate();
            GetDataInputPort(nameof(Enabled)).Properties.hidden = true;
            BroadcastDataInputProperties(nameof(Enabled));;
        }
    }

    public class PenHandState : BasePoseState {
        public PenHandState() : base("Pen Hand") {}
    }

    public class MouseBodyState : BasePoseState {
        public MouseBodyState() : base("Mouse Body") {}

        protected override void OnCreate() {
            Enabled = true;
            base.OnCreate();
            GetDataInputPort(nameof(Enabled)).Properties.hidden = true;
            BroadcastDataInputProperties(nameof(Enabled));
        }
    }

    public class PenBodyState : BasePoseState {
        public PenBodyState() : base("Pen Body") {}
    }

    public class MousePropState : BaseState {
        public MousePropState() : base("Mouse Prop") {}
    }

    public class PenPropState : BaseState {
        public PenPropState() : base("Pen Prop") {}
    }
}
