using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
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
        public Action ResetTriggerAction;

        [Markdown(0)]
        public string Instructions;

        [Trigger(1)]
        [Label("APPLY_TRANSFORM")]
        public void ApplyTrigger() {
            if (ApplyTriggerAction != null) {
                ApplyTriggerAction.Invoke();
            } else {
                Parent?.Apply(this);
            }
        }

        [Trigger(2)]
        [Label("RESET_TO_ORIGIN")]
        public void ResetTrigger() {
            if (ResetTriggerAction != null) {
                ResetTriggerAction.Invoke();
            } else {
                Parent?.ResetAnchorToOrigin();
            }
        }

        [Trigger(3)]
        [Label("CANCEL_TRANSFORM")]
        public void CancelTrigger() {
            if (CancelTriggerAction != null) {
                CancelTriggerAction.Invoke();
            } else {
                Parent?.ExitVisualSetup(false);
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

        [DataInput(120)]
        [Label("ORIGIN_SPHERE")]
        public OriginGuideSphere OriginSphere;

        public Action<bool> OnShowOriginChange;
        public Action<bool> OnAlwaysOnTopChange;
        public Action OnOriginSphereChange;

        protected override void OnCreate() {
            base.OnCreate();

            if (OriginSphere == null) {
                OriginSphere = StructuredData.Create<OriginGuideSphere>();
            }
            OriginSphere.OnChange = () => OnOriginSphereChange?.Invoke();

            Watch(nameof(Animation), delegate {
                OnAnimationChange?.Invoke(Animation);
            });
            Watch(nameof(ShowOrigin), delegate {
                UpdateOriginPortsVisibility();
                OnShowOriginChange?.Invoke(ShowOrigin);
            });
            Watch(nameof(AlwaysOnTop), delegate {
                OnAlwaysOnTopChange?.Invoke(AlwaysOnTop);
            });
            Watch(nameof(OriginSphere), delegate {
                if (OriginSphere != null) {
                    OriginSphere.OnChange = () => OnOriginSphereChange?.Invoke();
                }
                OnOriginSphereChange?.Invoke();
            });

            UpdateOriginPortsVisibility();
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

        public void UpdateOriginPortsVisibility() {
            var topPort = GetDataInputPort(nameof(AlwaysOnTop));
            if (topPort != null && topPort.Properties != null) {
                topPort.Properties.hidden = !ShowOrigin;
                BroadcastDataInputProperties(nameof(AlwaysOnTop));
            }
            var spherePort = GetDataInputPort(nameof(OriginSphere));
            if (spherePort != null && spherePort.Properties != null) {
                spherePort.Properties.hidden = !ShowOrigin;
                BroadcastDataInputProperties(nameof(OriginSphere));
            }
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
