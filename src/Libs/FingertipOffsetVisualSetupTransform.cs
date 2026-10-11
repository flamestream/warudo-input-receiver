using System;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream {
    public class FingertipOffsetVisualSetupTransform : VisualSetupTransform {
        public KeyboardFinger? TargetFinger;
        public Action<FingertipOffsetVisualSetupTransform> OnMirrorToOppositeHand;

        protected override void OnCreate() {
            base.OnCreate();
            Position = new Vector3(0f, 0f, 0.012f);

            var rotPort = GetDataInputPort(nameof(Rotation));
            if (rotPort != null) {
                rotPort.Properties.hidden = true;
                rotPort.Properties.alwaysHidden = true;
            }

            var scalePort = GetDataInputPort(nameof(Scale));
            if (scalePort != null) {
                scalePort.Properties.hidden = true;
                scalePort.Properties.alwaysHidden = true;
            }

            var normPort = GetTriggerPort(nameof(NormalizeRotationAnglesTrigger));
            if (normPort != null) {
                normPort.Properties.hidden = true;
                normPort.Properties.alwaysHidden = true;
            }

            ShowOriginIndicator = false;
        }

        [Trigger]
        [Label("APPLY_TO_OPPOSITE_HAND_FINGER")]
        [Description("APPLY_TO_OPPOSITE_HAND_FINGER_DESC")]
        public void ApplyToOppositeHandTrigger() {
            OnMirrorToOppositeHand?.Invoke(this);
        }

        public void SetHideMirrorToOppositeHand(bool hide) {
            var port = GetTriggerPort(nameof(ApplyToOppositeHandTrigger));
            if (port != null) {
                port.Properties.hidden = hide;
                port.Properties.alwaysHidden = hide;
                BroadcastTriggerProperties(nameof(ApplyToOppositeHandTrigger));
            }
        }

        public override void EnterVisualSetup(bool isNavigationWanted) {
            if (InVisualSetupMode) return;
            InVisualSetupMode = true;

            OnEnterVisualSetup?.Invoke(this);

            var a = CreateAnchor();
            if (isNavigationWanted) {
                Context.Service.NavigateToAsset(a.Id);
                Context.Service.BroadcastOpenedScene();
            }
            GetDataInputPort(nameof(Position)).Properties.disabled = true;
            GetDataInputPort(nameof(Rotation)).Properties.disabled = true;
            GetDataInputPort(nameof(Scale)).Properties.disabled = true;
            GetTriggerPort(nameof(EnterVisualSetupTrigger)).Properties.hidden = true;
            GetTriggerPort(nameof(CancelVisualSetupTrigger)).Properties.hidden = false;
            GetTriggerPort(nameof(CancelVisualSetupTrigger)).Properties.description = $"Currently being set up by asset [{a.Name}]";
            BroadcastDataInputProperties(nameof(Position));
            BroadcastDataInputProperties(nameof(Rotation));
            BroadcastDataInputProperties(nameof(Scale));
            BroadcastTriggerProperties(nameof(EnterVisualSetupTrigger));
            BroadcastTriggerProperties(nameof(CancelVisualSetupTrigger));
        }

        public override VisualSetupAnchorAsset CreateAnchor() {
            var anchor = base.CreateAnchor();
            if (anchor != null) {
                anchor.SetHideScale(true);
                string fingerName = TargetFinger.HasValue ? TargetFinger.Value.ToString() : "Unified (Left Index)";
                anchor.Name = $"⌛⚓-Fingertip ({fingerName})";
                anchor.Instructions = $@"
### {"FINGERTIP_CALIBRATION_INSTRUCTIONS_TITLE".Localized()}

{"FINGERTIP_CALIBRATION_INSTRUCTIONS_BODY".Localized()}
";
                anchor.Broadcast();
            }
            return anchor;
        }
    }
}
