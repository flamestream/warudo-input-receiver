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

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Animation), delegate {
                OnAnimationChange?.Invoke(Animation);
            });
            GetDataInputPort(nameof(Enabled)).Properties.alwaysHidden = true;
            GetDataInputPort(nameof(Enabled)).Properties.hidden = true;
            GetDataInputPort(nameof(Attachable.Parent)).Properties.alwaysDisabled = true;
            GetDataInputPort(nameof(Attachable.Parent)).Properties.disabled = true;
            GetTriggerPort(nameof(TeleportToActiveCamera)).Properties.alwaysHidden = true;
            GetTriggerPort(nameof(TeleportToActiveCamera)).Properties.hidden = true;
       }
    }
}
