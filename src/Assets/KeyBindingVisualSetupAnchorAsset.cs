using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream {
    [AssetType(
        Id = "Flamestream.Asset.KeyBindingVisualSetupAnchor",
        Title = "KEY_BINDING_VISUAL_SETUP_ANCHOR",
        Category = "FS_ASSET_CATEGORY_INTERNAL"
    )]
    public class KeyBindingVisualSetupAnchorAsset : VisualSetupAnchorAsset {
        public Action OnAlignHandWithSurface;
        public Action OnMoveFingerToButton;
        public Action OnRotateHandToNaturalRotation;
        public Action OnSettingsChanged;

        [Section("AUTOMATION")]

        [Trigger(20)]
        [Label("ALIGN_HAND_WITH_BUTTON_SURFACE")]
        [Description("ALIGN_HAND_WITH_BUTTON_SURFACE_DESC")]
        public void TriggerAlignHandWithSurface() {
            OnAlignHandWithSurface?.Invoke();
        }

        [Trigger(21)]
        [Label("MOVE_FINGER_TO_BUTTON")]
        [Description("MOVE_FINGER_TO_BUTTON_DESC")]
        public void TriggerMoveFingerToButton() {
            OnMoveFingerToButton?.Invoke();
        }

        [Trigger(22)]
        [Label("ROTATE_HAND_TO_NATURAL_ROTATION")]
        [Description("ROTATE_HAND_TO_NATURAL_ROTATION_DESC")]
        public void TriggerRotateHandToNaturalRotation() {
            OnRotateHandToNaturalRotation?.Invoke();
        }

        [Section("VISUAL_GUIDES")]

        [DataInput(40)]
        [Label("SHOW_VISUAL_GUIDES")]
        [Description("SHOW_VISUAL_GUIDES_DESC")]
        public bool ShowVisualGuides = true;

        [DataInput(41)]
        [Label("GUIDES_ALWAYS_ON_TOP")]
        [Description("GUIDES_ALWAYS_ON_TOP_DESC")]
        public bool GuidesAlwaysOnTop = false;

        [DataInput(42)]
        [Label("FINGER_TIP_SPHERE")]
        public FingerTipGuideSphere FingerTipSphere;

        [DataInput(43)]
        [Label("KEY_CENTER_SPHERE")]
        public KeyCenterGuideSphere KeyCenterSphere;

        [DataInput(44)]
        [Label("HAND_SLAB")]
        public HandGuideSlab HandSlab;

        [DataInput(45)]
        [Label("KEY_SLAB")]
        public KeyGuideSlab KeySlab;

        [DataInput(46)]
        [Label("HAND_FOREARM_LINE")]
        public HandGuideLine HandLine;

        [DataInput(47)]
        [Label("ELBOW_REFERENCE_LINE")]
        public ElbowGuideLine ElbowLine;

        [Section("ALIGNMENT_THRESHOLDS")]

        [DataInput(50)]
        [Label("SPHERE_ALIGNMENT_THRESHOLD")]
        public SphereAlignmentThreshold SphereThreshold;

        [DataInput(51)]
        [Label("SLAB_ALIGNMENT_THRESHOLD")]
        public SlabAlignmentThreshold SlabThreshold;

        [DataInput(52)]
        [Label("LINE_ALIGNMENT_THRESHOLD")]
        public LineAlignmentThreshold LineThreshold;

        protected override void OnCreate() {
            base.OnCreate();

            // Default Guide Structures
            if (FingerTipSphere == null) {
                FingerTipSphere = StructuredData.Create<FingerTipGuideSphere>();
            }
            FingerTipSphere.OnChange = () => OnSettingsChanged?.Invoke();

            if (KeyCenterSphere == null) {
                KeyCenterSphere = StructuredData.Create<KeyCenterGuideSphere>();
            }
            KeyCenterSphere.OnChange = () => OnSettingsChanged?.Invoke();

            if (HandSlab == null) {
                HandSlab = StructuredData.Create<HandGuideSlab>();
            }
            HandSlab.OnChange = () => OnSettingsChanged?.Invoke();

            if (KeySlab == null) {
                KeySlab = StructuredData.Create<KeyGuideSlab>();
            }
            KeySlab.OnChange = () => OnSettingsChanged?.Invoke();

            if (HandLine == null) {
                HandLine = StructuredData.Create<HandGuideLine>();
            }
            HandLine.OnChange = () => OnSettingsChanged?.Invoke();

            if (ElbowLine == null) {
                ElbowLine = StructuredData.Create<ElbowGuideLine>();
            }
            ElbowLine.OnChange = () => OnSettingsChanged?.Invoke();

            // Default Threshold Structures
            if (SphereThreshold == null) {
                SphereThreshold = StructuredData.Create<SphereAlignmentThreshold>();
            }
            SphereThreshold.OnChange = () => OnSettingsChanged?.Invoke();

            if (SlabThreshold == null) {
                SlabThreshold = StructuredData.Create<SlabAlignmentThreshold>();
            }
            SlabThreshold.OnChange = () => OnSettingsChanged?.Invoke();

            if (LineThreshold == null) {
                LineThreshold = StructuredData.Create<LineAlignmentThreshold>();
            }
            LineThreshold.OnChange = () => OnSettingsChanged?.Invoke();

            // Guide Toggles
            Watch(nameof(ShowVisualGuides), () => {
                UpdateGuidesPortsVisibility();
                OnSettingsChanged?.Invoke();
            });
            Watch(nameof(GuidesAlwaysOnTop), () => OnSettingsChanged?.Invoke());

            // Guide StructuredData Reference Watchers
            Watch(nameof(FingerTipSphere), () => {
                if (FingerTipSphere != null) FingerTipSphere.OnChange = () => OnSettingsChanged?.Invoke();
                OnSettingsChanged?.Invoke();
            });
            Watch(nameof(KeyCenterSphere), () => {
                if (KeyCenterSphere != null) KeyCenterSphere.OnChange = () => OnSettingsChanged?.Invoke();
                OnSettingsChanged?.Invoke();
            });
            Watch(nameof(HandSlab), () => {
                if (HandSlab != null) HandSlab.OnChange = () => OnSettingsChanged?.Invoke();
                OnSettingsChanged?.Invoke();
            });
            Watch(nameof(KeySlab), () => {
                if (KeySlab != null) KeySlab.OnChange = () => OnSettingsChanged?.Invoke();
                OnSettingsChanged?.Invoke();
            });
            Watch(nameof(HandLine), () => {
                if (HandLine != null) HandLine.OnChange = () => OnSettingsChanged?.Invoke();
                OnSettingsChanged?.Invoke();
            });
            Watch(nameof(ElbowLine), () => {
                if (ElbowLine != null) ElbowLine.OnChange = () => OnSettingsChanged?.Invoke();
                OnSettingsChanged?.Invoke();
            });

            // Threshold StructuredData Reference Watchers
            Watch(nameof(SphereThreshold), () => {
                if (SphereThreshold != null) SphereThreshold.OnChange = () => OnSettingsChanged?.Invoke();
                OnSettingsChanged?.Invoke();
            });
            Watch(nameof(SlabThreshold), () => {
                if (SlabThreshold != null) SlabThreshold.OnChange = () => OnSettingsChanged?.Invoke();
                OnSettingsChanged?.Invoke();
            });
            Watch(nameof(LineThreshold), () => {
                if (LineThreshold != null) LineThreshold.OnChange = () => OnSettingsChanged?.Invoke();
                OnSettingsChanged?.Invoke();
            });

            // Hide Attachable / Transform Attachment ports
            HideAttachablePorts();

            UpdateGuidesPortsVisibility();
        }

        public void UpdateGuidesPortsVisibility() {
            bool visible = ShowVisualGuides;

            SetPortHidden(nameof(GuidesAlwaysOnTop), !visible);
            SetPortHidden(nameof(FingerTipSphere), !visible);
            SetPortHidden(nameof(KeyCenterSphere), !visible);
            SetPortHidden(nameof(HandSlab), !visible);
            SetPortHidden(nameof(KeySlab), !visible);
            SetPortHidden(nameof(HandLine), !visible);
            SetPortHidden(nameof(ElbowLine), !visible);

            SetPortHidden(nameof(SphereThreshold), !visible);
            SetPortHidden(nameof(SlabThreshold), !visible);
            SetPortHidden(nameof(LineThreshold), !visible);
        }

        void SetPortHidden(string portName, bool hidden) {
            var port = GetDataInputPort(portName);
            if (port != null && port.Properties != null) {
                port.Properties.hidden = hidden;
                BroadcastDataInputProperties(portName);
            }
        }

        void HideAttachablePorts() {
            HidePortIfExists(nameof(Attachable.Parent));
            HidePortIfExists(nameof(Attachable.AttachType));
            HidePortIfExists(nameof(Attachable.AttachToBone));
            HidePortIfExists(nameof(Attachable.AttachToBoneDoesNotExist));
            HidePortIfExists(nameof(Attachable.AttachToTransform));
            HidePortIfExists(nameof(Attachable.UseElasticity));
            HidePortIfExists(nameof(Attachable.StartElasticityDistance));
            HidePortIfExists(nameof(Attachable.StopElasticityDistance));
            HidePortIfExists(nameof(Attachable.ElasticityStiffness));
            HidePortIfExists(nameof(Attachable.ElasticityDamping));
            HidePortIfExists(nameof(Attachable.ElasticityMaxSpeed));
        }
    }
}
