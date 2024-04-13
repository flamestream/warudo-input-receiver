using System;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream {
    public class VisualSetupTransform : StructuredData {
        [DataInput]
        [Label("DISPLACEMENT")]
        public Vector3 Position = Vector3.zero;

        [DataInput]
        [Label("ROTATION")]
        public Vector3 Rotation = Vector3.zero;

        [DataInput]
        [Label("SCALE")]
        public Vector3 Scale = Vector3.one;

        [Trigger]
        [Label("MINIMIZE_ROTATION")]
        public void NormalizeRotationAnglesTrigger() {
            NormalizeRotationAngles();
        }

        [Trigger]
        [Hidden]
        public void CopyTrigger() {
            OnCopy?.Invoke();
        }

        [Trigger]
        [Label("ENTER_VISUAL_SETUP")]
        public void EnterVisualSetupTrigger() {
            EnterVisualSetup(true);
        }

        [Trigger]
        [Label("CANCEL_VISUAL_SETUP")]
        [Hidden]
        public void CancelVisualSetupTrigger() {
            ExitVisualSetup(false);
        }

        public bool InVisualSetupMode;
        public string LocalizationKey;
        public VisualSetupAnchorAsset setupAnchor;
        public Action<VisualSetupTransform, VisualSetupAnchorAsset> OnCreateAnchor;
        public Action<VisualSetupTransform, VisualSetupAnchorAsset> OnApplyAnchor;
        public Action<VisualSetupTransform> OnEnterVisualSetup;
        public Action<bool> OnExitVisualSetup;
        public Action OnCopy;
        public Action<string> OnAnimationChange;

        protected override void OnDestroy() {
            base.OnDestroy();
            DestroyAnchor();
        }

        public VisualSetupAnchorAsset CreateAnchor() {
            DestroyAnchor();
            setupAnchor = Scene.AddAsset<VisualSetupAnchorAsset>();
            setupAnchor.Parent = this;
            var title = $"SETUP_ANCHOR_NAME_{LocalizationKey}".Localized();
            setupAnchor.Name = $"⌛⚓-{title}";
            setupAnchor.Instructions = $@"
### Instructions

{$"SETUP_ANCHOR_INSTRUCTIONS_{LocalizationKey}".Localized()}
";
            setupAnchor.Transform.Position = Position;
            setupAnchor.Transform.Rotation = Rotation;
            setupAnchor.Transform.Scale = Scale;
            OnCreateAnchor?.Invoke(this, setupAnchor);
            setupAnchor.Broadcast();
            Scene.UpdateNewAssetName(setupAnchor);

            setupAnchor.OnAnimationChange = (a) => {
                UnityEngine.Debug.Log($"VST ANIMATION CHANGE {OnAnimationChange}");
                OnAnimationChange.Invoke(a);
            };

            return setupAnchor;
        }

        public void DestroyAnchor() {
            if (setupAnchor == null) return;
            try {
                Scene.RemoveAsset(setupAnchor.Id);
            } catch {}
        }

        public void EnterVisualSetup(bool isNavigationWanted) {
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
        public void ExitVisualSetup(bool isApplied) {
            if (!InVisualSetupMode) return;
            InVisualSetupMode = false;
            GetDataInputPort(nameof(Position)).Properties.disabled = false;
            GetDataInputPort(nameof(Rotation)).Properties.disabled = false;
            GetDataInputPort(nameof(Scale)).Properties.disabled = false;
            GetTriggerPort(nameof(EnterVisualSetupTrigger)).Properties.hidden = false;
            GetTriggerPort(nameof(CancelVisualSetupTrigger)).Properties.hidden = true;
            DestroyAnchor();
            BroadcastDataInputProperties(nameof(Position));
            BroadcastDataInputProperties(nameof(Rotation));
            BroadcastDataInputProperties(nameof(Scale));
            BroadcastTriggerProperties(nameof(EnterVisualSetupTrigger));
            BroadcastTriggerProperties(nameof(CancelVisualSetupTrigger));
            OnExitVisualSetup?.Invoke(isApplied);
        }

        public void Apply(VisualSetupAnchorAsset a) {

            Position = a.Transform.Position;
            Rotation = a.Transform.Rotation;
            Scale = a.Transform.Scale;
            OnApplyAnchor?.Invoke(this, a);
            BroadcastDataInput(nameof(Position));
            BroadcastDataInput(nameof(Rotation));
            BroadcastDataInput(nameof(Scale));
            ExitVisualSetup(true);
        }

        public void HideScale() {
            GetDataInputPort(nameof(Scale)).Properties.hidden = true;
            BroadcastDataInputProperties(nameof(Scale));
        }

        public void SetCopyTriggerLabel(string label) {
            GetTriggerPort(nameof(CopyTrigger)).Properties.label = label;
            // GetTriggerPort(nameof(CopyTrigger)).Properties.hidden = false;
            BroadcastTriggerProperties(nameof(CopyTrigger));
        }

        public void NormalizeRotationAngles() {
            SetDataInput(nameof(Rotation), Helper.NormalizeRotationAngles(Rotation), true);
        }
    }

    public class NaturalPositionVisualSetupTransform : VisualSetupTransform {
        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Position), delegate {
                Position.z = 0;
                BroadcastDataInput(nameof(Position));
            });
            GetDataInputPort(nameof(Position)).Properties.label = "COORDINATES".Localized();
            GetDataInputPort(nameof(Rotation)).Properties.hidden = true;
            GetDataInputPort(nameof(Scale)).Properties.hidden = true;
            BroadcastDataInputProperties(nameof(Position));
            BroadcastDataInputProperties(nameof(Rotation));

            GetTriggerPort(nameof(NormalizeRotationAnglesTrigger)).Properties.hidden = true;
            BroadcastTriggerProperties(nameof(NormalizeRotationAnglesTrigger));
        }

        protected override void OnUpdate() {
            base.OnUpdate();
            if (!InVisualSetupMode) return;
            // Clamp to screen surface
            setupAnchor.Transform.Position.z = 0;
            setupAnchor.Transform.Rotation = Vector3.zero;
            setupAnchor.BroadcastDataInput(nameof(setupAnchor.Transform));
        }
    }
}
