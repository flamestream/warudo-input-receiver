using System;
using Warudo.Core.Attributes;
using Warudo.Plugins.Core.Assets.Utility;

namespace FlameStream {
    [AssetType(
        Id = "Flamestream.Asset.VisualSetupAnchor",
        Title = "VISUAL_SETUP_TEMP_ANCHOR",
        Category = "FS_ASSET_CATEGORY_INTERNAL"
    )]
    public class VisualSetupAnchorAsset : AnchorAsset {
        public VisualSetupTransform Parent;
        public Action ApplyTriggerAction;
        public Action CancelTriggerAction;

        [Markdown(0)]
        public string Instructions;

        [Trigger(0)]
        [Label("CANCEL_TRANSFORM")]
        public void CancelTrigger() {
            if (CancelTriggerAction != null) {
                CancelTriggerAction.Invoke();
            } else {
                Parent?.ExitVisualSetup(false);
            }
        }

        [Trigger(0)]
        [Label("APPLY_TRANSFORM")]
        public void ApplyTrigger() {
            if (ApplyTriggerAction != null) {
                ApplyTriggerAction.Invoke();
            } else {
                Parent?.Apply(this);
            }
        }

        [DataInput(11)]
        [PreviewGallery]
        [AutoCompleteResource("CharacterAnimation", null)]
        [Label("ANIMATION")]
        public string Animation;

        public Action<string> OnAnimationChange;

        [Section("ADVANCED")]

        [DataInput(100)]
        [Label("SHOW_ORIGIN")]
        [Description("SHOW_ORIGIN_DESC")]
        public bool ShowOrigin = true;

        [DataInput(110)]
        [Label("ALWAYS_ON_TOP")]
        [Description("ALWAYS_ON_TOP_DESC")]
        public bool AlwaysOnTop = false;

        public Action<bool> OnShowOriginChange;
        public Action<bool> OnAlwaysOnTopChange;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Animation), delegate {
                OnAnimationChange?.Invoke(Animation);
            });
            Watch(nameof(ShowOrigin), delegate {
                GetDataInputPort(nameof(AlwaysOnTop)).Properties.disabled = !ShowOrigin;
                BroadcastDataInputProperties(nameof(AlwaysOnTop));
                OnShowOriginChange?.Invoke(ShowOrigin);
            });
            Watch(nameof(AlwaysOnTop), delegate {
                OnAlwaysOnTopChange?.Invoke(AlwaysOnTop);
            });
            GetDataInputPort(nameof(Enabled)).Properties.alwaysHidden = true;
            GetDataInputPort(nameof(Enabled)).Properties.hidden = true;
            GetDataInputPort(nameof(Attachable.Parent)).Properties.alwaysDisabled = true;
            GetDataInputPort(nameof(Attachable.Parent)).Properties.disabled = true;
            GetTriggerPort(nameof(TeleportToActiveCamera)).Properties.alwaysHidden = true;
            GetTriggerPort(nameof(TeleportToActiveCamera)).Properties.hidden = true;

            // Hide UseElasticity and spring/lag settings from visual calibration anchor
            HidePortIfExists(nameof(Attachable.UseElasticity));
            HidePortIfExists(nameof(Attachable.StartElasticityDistance));
            HidePortIfExists(nameof(Attachable.StopElasticityDistance));
            HidePortIfExists(nameof(Attachable.ElasticityStiffness));
            HidePortIfExists(nameof(Attachable.ElasticityDamping));
            HidePortIfExists(nameof(Attachable.ElasticityMaxSpeed));
        }

        public void HidePortIfExists(string portName) {
            var port = GetDataInputPort(portName);
            if (port != null && port.Properties != null) {
                port.Properties.alwaysHidden = true;
                port.Properties.hidden = true;
                BroadcastDataInputProperties(portName);
            }
        }

        public void HideAnimation() {
            HidePortIfExists(nameof(Animation));
        }

        public void SetHideScale(bool hide) {
            if (Transform != null) {
                Transform.HideScale = hide;
                var port = Transform.GetDataInputPort(nameof(Transform.Scale));
                if (port != null && port.Properties != null) {
                    port.Properties.hidden = hide;
                    port.Properties.alwaysHidden = hide;
                    Transform.BroadcastDataInputProperties(nameof(Transform.Scale));
                }
            }
        }
    }
}
