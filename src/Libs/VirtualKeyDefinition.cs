using System;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class VirtualKeyDefinition : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("KEY")]
        public VKCode Key = VKCode.A;

        [DataInput(20)]
        [Label("DISPLAY_LABEL")]
        public string DisplayLabel = "";

        [DataInput(30)]
        [Label("ATTACHMENT_ANCHOR")]
        public VirtualKeyAnchor AttachmentAnchor = VirtualKeyAnchor.LeftHand;

        [DataInput(40)]
        [Label("TRANSFORM")]
        public VisualSetupTransform Transform;

        [DataInput(48)]
        [Label("USE_CUSTOM_SIZE")]
        public bool UseCustomSize = false;

        [DataInput(50)]
        [Label("KEY_SIZE")]
        public Vector2 Size = new Vector2(0.032f, 0.032f);

        [DataInput(52)]
        [FloatSlider(0f, 0.5f, 0.01f)]
        [Label("CORNER_ROUNDNESS")]
        public float CornerRadius = 0.28f;

        [DataInput(53)]
        [FloatSlider(0f, 0.5f, 0.01f)]
        [Label("PRESSED_CORNER_ROUNDNESS")]
        public float PressedCornerRadius = 0.28f;

        [DataInput(54)]
        [FloatSlider(0.0005f, 0.015f, 0.0002f)]
        [Label("BORDER_WIDTH")]
        public float BorderWidth = 0.0025f;

        [DataInput(55)]
        [FloatSlider(0.0005f, 0.015f, 0.0002f)]
        [Label("PRESSED_BORDER_WIDTH")]
        public float PressedBorderWidth = 0.0035f;

        [DataInput(60)]
        [Label("USE_CUSTOM_APPEARANCE")]
        public bool UseCustomAppearance = false;

        [DataInput(65)]
        [Label("CUSTOM_APPEARANCE")]
        public VirtualKeyAppearance CustomAppearance;

        [Trigger(120)]
        [Label("IDENTIFY_KEY")]
        public void TriggerIdentifyKey() {
            Identify();
        }

        // Runtime rendering components
        public GameObject KeyGameObject { get; internal set; }
        public Mesh BodyMesh { get; internal set; }
        public Mesh FrameMesh { get; internal set; }
        public Material BodyMaterial { get; internal set; }
        public Material BorderMaterial { get; internal set; }

        public float IdentifyGlowTimer { get; internal set; } = 0f;
        public float CurrentPressProgress { get; internal set; } = 0f;
        public float CurrentBorderWidth { get; internal set; } = -1f;
        public float CurrentCornerRadius { get; internal set; } = -1f;

        public void Identify(float duration = 2.5f) {
            IdentifyGlowTimer = duration;
            if (Parent != null) {
                var label = string.IsNullOrEmpty(DisplayLabel) ? Key.ToString() : DisplayLabel;
                Context.Service?.Toast(
                    Warudo.Core.Server.ToastSeverity.Info,
                    Parent.Name,
                    $"Identifying Virtual Key: [{label}] ({AttachmentAnchor})"
                );
            }
        }

        public string GetHeader() {
            var label = string.IsNullOrEmpty(DisplayLabel) ? Key.ToString() : $"{Key} (\"{DisplayLabel}\")";
            var anchorTag = AttachmentAnchor == VirtualKeyAnchor.LeftHand ? "🫲 L" :
                            AttachmentAnchor == VirtualKeyAnchor.RightHand ? "🫱 R" : "⌨️ Root";
            var pos = Transform != null ? Transform.Position : Vector3.zero;
            return $"[{label}] ➔ [{anchorTag}] Pos({pos.x:F3}, {pos.y:F3}, {pos.z:F3})";
        }

        protected override void OnCreate() {
            base.OnCreate();
            if (Transform == null) {
                Transform = StructuredData.Create<VisualSetupTransform>();
                Transform.Position = Vector3.zero;
                Transform.Rotation = Vector3.zero;
                Transform.Scale = Vector3.one;
            }
            if (CustomAppearance == null) {
                CustomAppearance = VirtualKeyAppearance.CreateForOverride();
            } else {
                CustomAppearance.IsBaseAppearance = false;
                CustomAppearance.ConfigureElements();
            }
            CustomAppearance.OnAppearanceChanged = () => {
                if (Parent != null) Parent.UpdateVirtualKeyColors(this);
            };

            SetupTransformCallbacks();
            SyncUI();

            Watch(nameof(Transform), delegate {
                SetupTransformCallbacks();
                if (KeyGameObject != null && Transform != null) {
                    KeyGameObject.transform.localPosition = Transform.Position;
                    KeyGameObject.transform.localRotation = Quaternion.Euler(Transform.Rotation);
                    KeyGameObject.transform.localScale = Transform.Scale;
                }
            });
            Watch(nameof(Key), delegate { BroadcastDataInput(nameof(Transform)); });
            Watch(nameof(DisplayLabel), delegate { BroadcastDataInput(nameof(Transform)); });
            Watch(nameof(AttachmentAnchor), delegate {
                if (Parent != null) {
                    Parent.UpdateVirtualKeyParenting(this);
                }
                BroadcastDataInput(nameof(Transform));
            });
            Watch(nameof(UseCustomSize), delegate {
                SyncUI();
                if (Parent != null) Parent.RebuildVirtualKeyMesh(this);
            });
            Watch(nameof(UseCustomAppearance), delegate {
                SyncUI();
                if (Parent != null) Parent.UpdateVirtualKeyColors(this);
            });
            Watch(nameof(CustomAppearance), delegate {
                if (CustomAppearance != null) {
                    CustomAppearance.OnAppearanceChanged = () => {
                        if (Parent != null) Parent.UpdateVirtualKeyColors(this);
                    };
                }
                if (Parent != null) Parent.UpdateVirtualKeyColors(this);
            });
            Watch(nameof(Size), delegate { if (Parent != null) Parent.RebuildVirtualKeyMesh(this); });
            Watch(nameof(CornerRadius), delegate { if (Parent != null) Parent.RebuildVirtualKeyMesh(this); });
            Watch(nameof(PressedCornerRadius), delegate { if (Parent != null) Parent.UpdateVirtualKeyColors(this); });
            Watch(nameof(BorderWidth), delegate { if (Parent != null) Parent.RebuildVirtualKeyMesh(this); });
            Watch(nameof(PressedBorderWidth), delegate { if (Parent != null) Parent.UpdateVirtualKeyColors(this); });
        }

        public void SyncUI() {
            try {
                bool hideSize = !UseCustomSize;
                var sizePort = GetDataInputPort(nameof(Size));
                if (sizePort != null) { sizePort.Properties.hidden = hideSize; BroadcastDataInputProperties(nameof(Size)); }
                var crPort = GetDataInputPort(nameof(CornerRadius));
                if (crPort != null) { crPort.Properties.hidden = hideSize; BroadcastDataInputProperties(nameof(CornerRadius)); }
                var pcrPort = GetDataInputPort(nameof(PressedCornerRadius));
                if (pcrPort != null) { pcrPort.Properties.hidden = hideSize; BroadcastDataInputProperties(nameof(PressedCornerRadius)); }
                var bwPort = GetDataInputPort(nameof(BorderWidth));
                if (bwPort != null) { bwPort.Properties.hidden = hideSize; BroadcastDataInputProperties(nameof(BorderWidth)); }
                var pbwPort = GetDataInputPort(nameof(PressedBorderWidth));
                if (pbwPort != null) { pbwPort.Properties.hidden = hideSize; BroadcastDataInputProperties(nameof(PressedBorderWidth)); }

                bool hideApp = !UseCustomAppearance;
                var appPort = GetDataInputPort(nameof(CustomAppearance));
                if (appPort != null) { appPort.Properties.hidden = hideApp; BroadcastDataInputProperties(nameof(CustomAppearance)); }
            } catch { }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            Transform?.DestroyAnchor();
            DestroyVisuals();
        }

        public void SetupTransformCallbacks() {
            if (Transform == null) return;
            Transform.LocalizationKey = "VIRTUAL_KEY";

            Transform.OnEnterVisualSetup = (tr) => {
                if (Parent != null) {
                    Parent.ActiveVirtualKeyInVisualSetup = this;
                    Parent.EnsureAnchors();
                }
            };

            Transform.OnCreateAnchor = (tr, setupAnchor) => {
                if (Parent == null) return;

                var parentAnchor = Parent.GetAnchorForVirtualKey(AttachmentAnchor);
                if (parentAnchor != null) {
                    setupAnchor.Attachable.Parent = parentAnchor;
                    if (parentAnchor.GameObject != null) {
                        setupAnchor.GameObject.transform.SetParent(parentAnchor.GameObject.transform, false);
                    }
                }

                setupAnchor.Transform.Position = tr.Position;
                setupAnchor.Transform.Rotation = tr.Rotation;
                setupAnchor.Transform.Scale = tr.Scale;
                var keyLabel = string.IsNullOrEmpty(DisplayLabel) ? Key.ToString() : DisplayLabel;
                setupAnchor.Name = $"⌛⚓-Key [{keyLabel}]";
                setupAnchor.Instructions = $@"
### Virtual Key Setup: {keyLabel}

Position, rotate, and scale this anchor where the virtual key **[{keyLabel}]** should float.
The key will follow this anchor in real-time.
Click **[Apply Transform]** when finished.
";
                setupAnchor.Transform.Broadcast();
            };

            Transform.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                tr.Scale = setupAnchor.Transform.Scale;
                if (KeyGameObject != null) {
                    KeyGameObject.transform.localPosition = tr.Position;
                    KeyGameObject.transform.localRotation = Quaternion.Euler(tr.Rotation);
                    KeyGameObject.transform.localScale = tr.Scale;
                }
            };

            Transform.OnExitVisualSetup = (isApplied) => {
                if (KeyGameObject != null && Transform != null) {
                    KeyGameObject.transform.localPosition = Transform.Position;
                    KeyGameObject.transform.localRotation = Quaternion.Euler(Transform.Rotation);
                    KeyGameObject.transform.localScale = Transform.Scale;
                }
                if (Parent != null) {
                    Parent.ActiveVirtualKeyInVisualSetup = null;
                    Context.Service.NavigateToAsset(Parent.Id);
                    Context.Service.BroadcastOpenedScene();
                }
            };
        }

        public void DestroyVisuals() {
            if (KeyGameObject != null) {
                UnityEngine.Object.Destroy(KeyGameObject);
                KeyGameObject = null;
            }
            if (BodyMesh != null) {
                UnityEngine.Object.Destroy(BodyMesh);
                BodyMesh = null;
            }
            if (FrameMesh != null) {
                UnityEngine.Object.Destroy(FrameMesh);
                FrameMesh = null;
            }
            if (BodyMaterial != null) {
                UnityEngine.Object.Destroy(BodyMaterial);
                BodyMaterial = null;
            }
            if (BorderMaterial != null) {
                UnityEngine.Object.Destroy(BorderMaterial);
                BorderMaterial = null;
            }
            CurrentBorderWidth = -1f;
            CurrentCornerRadius = -1f;
        }
    }
}
