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

        [DataInput(50)]
        [Label("USE_CUSTOM_APPEARANCE")]
        public bool UseCustomAppearance = false;

        [DataInput(60)]
        [Label("KEY_SIZE")]
        public Vector2 Size = new Vector2(0.032f, 0.032f);

        [DataInput(70)]
        [FloatSlider(0f, 0.5f, 0.01f)]
        [Label("CORNER_ROUNDNESS")]
        public float CornerRadius = 0.28f;

        [DataInput(80)]
        [FloatSlider(0.0005f, 0.01f, 0.0005f)]
        [Label("BORDER_WIDTH")]
        public float BorderWidth = 0.0025f;

        [DataInput(55)]
        [Label("CUSTOM_APPEARANCE")]
        public VirtualKeyAppearance CustomAppearance;

        [Trigger(58)]
        [Label("RESET_CUSTOM_APPEARANCE")]
        public void TriggerResetCustomAppearance() {
            if (CustomAppearance == null) {
                CustomAppearance = VirtualKeyAppearance.CreateDefault();
            } else {
                CustomAppearance.ResetToDefaultTheme();
            }
            if (Parent != null) {
                Parent.UpdateVirtualKeyColors(this);
            }
        }

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
                CustomAppearance = VirtualKeyAppearance.CreateDefault();
            }
            SetupTransformCallbacks();

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
            Watch(nameof(UseCustomAppearance), delegate { if (Parent != null) Parent.UpdateVirtualKeyColors(this); });
            Watch(nameof(CustomAppearance), delegate { if (Parent != null) Parent.UpdateVirtualKeyColors(this); });
            Watch(nameof(Size), delegate { if (Parent != null) Parent.RebuildVirtualKeyMesh(this); });
            Watch(nameof(CornerRadius), delegate { if (Parent != null) Parent.RebuildVirtualKeyMesh(this); });
            Watch(nameof(BorderWidth), delegate { if (Parent != null) Parent.RebuildVirtualKeyMesh(this); });
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
        }
    }
}
