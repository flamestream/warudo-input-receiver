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

        [DataInput(60)]
        [Label("USE_CUSTOM_APPEARANCE")]
        public bool UseCustomAppearance = false;

        [DataInput(65)]
        [Label("CUSTOM_APPEARANCE")]
        public VirtualKeyAppearance CustomAppearance;

        [Trigger(120)]
        [Label("IDENTIFY_KEY")]
        public void TriggerIdentifyKey() {
            if (Parent != null) {
                Parent.ToggleIdentifyVirtualKey(this);
            }
        }

        public bool IsIdentifying => Parent != null && Parent.IdentifiedVirtualKey == this;

        public void UpdateIdentifyButtonLabel(bool isIdentifying) {
            var port = GetTriggerPort(nameof(TriggerIdentifyKey));
            if (port != null && port.Properties != null) {
                port.Properties.label = isIdentifying ? "STOP_IDENTIFY_KEY" : "IDENTIFY_KEY";
                BroadcastTriggerProperties(nameof(TriggerIdentifyKey));
            }
        }

        // Runtime rendering components
        public GameObject KeyGameObject { get; internal set; }
        public Mesh BodyMesh { get; internal set; }
        public Mesh FrameMesh { get; internal set; }
        public Material BodyMaterial { get; internal set; }
        public Material BorderMaterial { get; internal set; }

        public float CurrentPressProgress { get; internal set; } = 0f;
        public Vector2 CurrentSize { get; internal set; } = new Vector2(-1f, -1f);
        public Vector4 CurrentBorderWidths { get; internal set; } = new Vector4(-1f, -1f, -1f, -1f);
        public Vector4 CurrentCornerRadii { get; internal set; } = new Vector4(-1f, -1f, -1f, -1f);

        public Vector4 GetBorderWidths() {
            if (UseCustomAppearance && CustomAppearance?.Shape?.Normal != null && CustomAppearance.Shape.Normal.Enabled) {
                return CustomAppearance.Shape.Normal.GetBorderWidths();
            }
            return Parent?.DefaultAppearance?.Shape?.Normal?.GetBorderWidths() ?? BorderVector4Extensions.Uniform(0.0025f);
        }

        public Vector4 GetCornerRadii() {
            if (UseCustomAppearance && CustomAppearance?.Shape?.Normal != null && CustomAppearance.Shape.Normal.Enabled) {
                return CustomAppearance.Shape.Normal.GetCornerRadii();
            }
            return Parent?.DefaultAppearance?.Shape?.Normal?.GetCornerRadii() ?? BorderVector4Extensions.Uniform(0.25f);
        }

        public string GetHeader() {
            var label = string.IsNullOrEmpty(DisplayLabel) ? Key.ToString() : $"{Key} (\"{DisplayLabel}\")";
            var anchorTag = AttachmentAnchor == VirtualKeyAnchor.LeftHand ? "🫲 L" :
                            AttachmentAnchor == VirtualKeyAnchor.RightHand ? "🫱 R" : "⌨️ Root";
            var pos = Transform != null ? Transform.Position : Vector3.zero;
            var identifyTag = IsIdentifying ? " 🔍[IDENTIFYING]" : "";
            return $"[{label}] ➔ [{anchorTag}] Pos({pos.x:F3}, {pos.y:F3}, {pos.z:F3}){identifyTag}";
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
            CustomAppearance.GetBaseAppearance = () => Parent?.DefaultAppearance;
            CustomAppearance.OnAppearanceChanged = () => {
                if (Parent != null) Parent.RebuildVirtualKeyMesh(this);
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
            Watch(nameof(UseCustomAppearance), delegate {
                SyncUI();
                if (Parent != null) {
                    Parent.RebuildVirtualKeyMesh(this);
                }
            });
            Watch(nameof(CustomAppearance), delegate {
                if (CustomAppearance != null) {
                    CustomAppearance.IsBaseAppearance = false;
                    CustomAppearance.GetBaseAppearance = () => Parent?.DefaultAppearance;
                    CustomAppearance.OnAppearanceChanged = () => {
                        if (Parent != null) Parent.RebuildVirtualKeyMesh(this);
                    };
                }
                if (Parent != null) {
                    Parent.RebuildVirtualKeyMesh(this);
                }
            });
        }

        public void SyncUI() {
            try {
                var port = GetDataInputPort(nameof(CustomAppearance));
                if (port != null) {
                    port.Properties.hidden = !UseCustomAppearance;
                    BroadcastDataInputProperties(nameof(CustomAppearance));
                }
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
            Parent?.DetachIdentifyPulseIfParentedTo(this);
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
            CurrentSize = new Vector2(-1f, -1f);
            CurrentBorderWidths = new Vector4(-1f, -1f, -1f, -1f);
            CurrentCornerRadii = new Vector4(-1f, -1f, -1f, -1f);
        }
    }
}
