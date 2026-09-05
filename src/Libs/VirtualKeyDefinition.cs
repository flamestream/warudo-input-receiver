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
        [Label("USE_INDIVIDUAL_CORNERS")]
        public bool UseIndividualCorners = false;

        [DataInput(54)]
        [Label("INDIVIDUAL_CORNER_RADII")]
        public Vector4 IndividualCornerRadii = new Vector4(0.28f, 0.28f, 0.28f, 0.28f);

        [DataInput(55)]
        [FloatSlider(0f, 0.5f, 0.01f)]
        [Label("PRESSED_CORNER_ROUNDNESS")]
        public float PressedCornerRadius = 0.28f;

        [DataInput(56)]
        [FloatSlider(0.0005f, 0.015f, 0.0002f)]
        [Label("BORDER_WIDTH")]
        public float BorderWidth = 0.0025f;

        [DataInput(57)]
        [Label("USE_INDIVIDUAL_BORDERS")]
        public bool UseIndividualBorders = false;

        [DataInput(58)]
        [Label("INDIVIDUAL_BORDER_WIDTHS")]
        public Vector4 IndividualBorderWidths = new Vector4(0.0025f, 0.0025f, 0.0025f, 0.0025f);

        [DataInput(59)]
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
        public Vector4 CurrentBorderWidths { get; internal set; } = new Vector4(-1f, -1f, -1f, -1f);
        public Vector4 CurrentCornerRadii { get; internal set; } = new Vector4(-1f, -1f, -1f, -1f);

        public Vector4 GetBorderWidths() => UseIndividualBorders ? IndividualBorderWidths : BorderVector4Extensions.Uniform(BorderWidth);
        public Vector4 GetCornerRadii() => UseIndividualCorners ? IndividualCornerRadii : BorderVector4Extensions.Uniform(CornerRadius);

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
            Watch(nameof(UseIndividualCorners), delegate {
                SyncUI();
                if (Parent != null) Parent.RebuildVirtualKeyMesh(this);
            });
            Watch(nameof(UseIndividualBorders), delegate {
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
            Watch(nameof(IndividualCornerRadii), delegate { if (Parent != null) Parent.RebuildVirtualKeyMesh(this); });
            Watch(nameof(PressedCornerRadius), delegate { if (Parent != null) Parent.UpdateVirtualKeyColors(this); });
            Watch(nameof(BorderWidth), delegate { if (Parent != null) Parent.RebuildVirtualKeyMesh(this); });
            Watch(nameof(IndividualBorderWidths), delegate { if (Parent != null) Parent.RebuildVirtualKeyMesh(this); });
            Watch(nameof(PressedBorderWidth), delegate { if (Parent != null) Parent.UpdateVirtualKeyColors(this); });
        }

        public void SyncUI() {
            try {
                bool hideSize = !UseCustomSize;
                void SetHidden(string portName, bool hidden) {
                    var port = GetDataInputPort(portName);
                    if (port != null) { port.Properties.hidden = hidden; BroadcastDataInputProperties(portName); }
                }

                SetHidden(nameof(Size), hideSize);
                SetHidden(nameof(CornerRadius), hideSize || UseIndividualCorners);
                SetHidden(nameof(UseIndividualCorners), hideSize);
                SetHidden(nameof(IndividualCornerRadii), hideSize || !UseIndividualCorners);
                SetHidden(nameof(PressedCornerRadius), hideSize);

                SetHidden(nameof(BorderWidth), hideSize || UseIndividualBorders);
                SetHidden(nameof(UseIndividualBorders), hideSize);
                SetHidden(nameof(IndividualBorderWidths), hideSize || !UseIndividualBorders);
                SetHidden(nameof(PressedBorderWidth), hideSize);

                bool hideApp = !UseCustomAppearance;
                SetHidden(nameof(CustomAppearance), hideApp);
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
                var group = Parent.GetGroupGameObject(AttachmentAnchor);
                if (group != null) {
                    setupAnchor.GameObject.transform.SetParent(group.transform, false);
                }
                setupAnchor.Transform.Position = tr.Position;
                setupAnchor.Transform.Rotation = tr.Rotation;
                setupAnchor.Transform.Scale = tr.Scale;

                var label = string.IsNullOrEmpty(DisplayLabel) ? Key.ToString() : DisplayLabel;
                setupAnchor.Name = $"⌛⚓-VKey {label}";
                setupAnchor.Instructions = $"Position, rotate, and scale virtual key **{label}**.\nClick **[Apply Transform]** when finished.";
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
            CurrentBorderWidths = new Vector4(-1f, -1f, -1f, -1f);
            CurrentCornerRadii = new Vector4(-1f, -1f, -1f, -1f);
        }
    }
}
