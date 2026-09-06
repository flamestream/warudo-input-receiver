using System;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class VirtualKeyDefinition : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData, IKeyListeningTarget
    {
        [DataInput(5)]
        [Label("DISPLAY_LABEL")]
        public string DisplayLabel = "";

        [DataInput(10)]
        [Label("KEY")]
        public VKCode Key = VKCode.A;

        [Trigger(15)]
        [Label("IDENTIFY_KEY")]
        public void TriggerIdentifyKey() {
            Parent?.ToggleKeyListening(this);
        }

        public bool IsListeningForKey { get; private set; }

        public void SetListeningForKey(bool listening) {
            IsListeningForKey = listening;
            var port = GetTriggerPort(nameof(TriggerIdentifyKey));
            if (port != null && port.Properties != null) {
                port.Properties.label = listening ? "IDENTIFYING_KEY" : "IDENTIFY_KEY";
                BroadcastTriggerProperties(nameof(TriggerIdentifyKey));
            }
        }

        public void OnKeyCaptured(VKCode newKey) {
            Key = newKey;
            SetDataInput(nameof(Key), newKey, true);
            BroadcastDataInput(nameof(Key));
            BroadcastDataInput(nameof(Transform));
            if (Parent != null) {
                Parent.RebuildVirtualKeyMesh(this);
            }
        }

        [DataInput(30)]
        [Label("ATTACHMENT_ANCHOR")]
        public VirtualKeyAnchor AttachmentAnchor = VirtualKeyAnchor.LeftHand;

        [DataInput(40)]
        [Label("TRANSFORM")]
        [Description("VIRTUAL_KEY_TRANSFORM_DESC")]
        public VisualSetupTransform Transform;

        [DataInput(60)]
        [Label("USE_CUSTOM_APPEARANCE")]
        public bool UseCustomAppearance = false;

        [DataInput(65)]
        [Label("CUSTOM_APPEARANCE")]
        public VirtualKeyAppearance CustomAppearance;

        [Trigger(120)]
        [Label("LOCATE_KEY")]
        public void TriggerLocateKey() {
            if (Parent != null) {
                Parent.ToggleIdentifyVirtualKey(this);
            }
        }

        [Trigger(130)]
        [Label("DEBUG_FORCE_KEY")]
        public void TriggerDebugForceKey() {
            Parent?.ToggleDebugVirtualKey(this);
        }

        public bool IsDebugForced => Parent != null && Parent.IsKeyDebugForced(Key);

        public void UpdateDebugButtonState() {
            var port = GetTriggerPort(nameof(TriggerDebugForceKey));
            if (port == null || port.Properties == null) return;
            bool forced = IsDebugForced;
            port.Properties.label = forced ? "DEBUG_RELEASE_KEY" : "DEBUG_FORCE_KEY";
            port.Properties.description = forced ? "DEBUG_RELEASE_KEY_DESC".Localized() : "DEBUG_FORCE_KEY_DESC".Localized();
            BroadcastTriggerProperties(nameof(TriggerDebugForceKey));
            BroadcastDataInput(nameof(Transform));
        }

        public bool IsLocating => Parent != null && Parent.IdentifiedVirtualKey == this;

        public void UpdateLocateButtonLabel(bool isLocating) {
            var port = GetTriggerPort(nameof(TriggerLocateKey));
            if (port != null && port.Properties != null) {
                port.Properties.label = isLocating ? "STOP_LOCATING_KEY" : "LOCATE_KEY";
                BroadcastTriggerProperties(nameof(TriggerLocateKey));
            }
        }

        // Backward compatibility for internal callers
        public bool IsIdentifying => IsLocating;
        public void UpdateIdentifyButtonLabel(bool isIdentifying) => UpdateLocateButtonLabel(isIdentifying);

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
            var listeningTag = IsListeningForKey ? " ⏳[LISTENING]" : "";
            var locateTag = IsLocating ? " ✨[LOCATING]" : "";
            var debugTag = IsDebugForced ? " ⚡[DEBUG]" : "";
            return $"[{label}] ➔ [{anchorTag}] Pos({pos.x:F3}, {pos.y:F3}, {pos.z:F3}){listeningTag}{locateTag}{debugTag}";
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

            Transform.HideScale();
            SetupTransformCallbacks();
            SyncUI();
            UpdateDebugButtonState();

            Watch(nameof(Transform), delegate {
                SetupTransformCallbacks();
                if (KeyGameObject != null && Transform != null) {
                    KeyGameObject.transform.localPosition = Transform.Position;
                    KeyGameObject.transform.localRotation = Quaternion.Euler(Transform.Rotation);
                    KeyGameObject.transform.localScale = Transform.Scale;
                }
            });
            Watch(nameof(Key), delegate { 
                UpdateDebugButtonState();
                BroadcastDataInput(nameof(Transform)); 
            });
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
            if (Parent != null && Parent.ActiveKeyListeningTarget == (IKeyListeningTarget)this) {
                Parent.CancelKeyListening();
            }
            Transform?.DestroyAnchor();
            DestroyVisuals();
        }

        public void SetupTransformCallbacks() {
            if (Transform == null) return;
            Transform.HideScale();
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
                string originName = AttachmentAnchor == VirtualKeyAnchor.LeftHand ? "Left Key Origin" : (AttachmentAnchor == VirtualKeyAnchor.RightHand ? "Right Key Origin" : "Center Key Origin");
                setupAnchor.Name = $"⌛⚓-VKey {label}";
                setupAnchor.Instructions = $"Position, rotate, and scale virtual key **{label}**, relative to the **{originName}**.\nThe red sphere indicates the origin location ({originName}).\nClick **[Apply Transform]** when finished.";
                setupAnchor.Transform.Broadcast();
            };

            Transform.OnResetAnchor = (tr, setupAnchor) => {
                setupAnchor.Transform.Position = Vector3.zero;
                setupAnchor.Transform.Rotation = Vector3.zero;
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Transform.Broadcast();
                if (KeyGameObject != null) {
                    KeyGameObject.transform.localPosition = Vector3.zero;
                    KeyGameObject.transform.localRotation = Quaternion.identity;
                    KeyGameObject.transform.localScale = Vector3.one;
                }
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
                if (!isApplied && KeyGameObject != null) {
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
