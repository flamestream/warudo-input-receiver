using System;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Serializations;
using Cysharp.Threading.Tasks;

namespace FlameStream
{
    public class VirtualKeyDefinition : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(5)]
        [Label("DISPLAY_LABEL")]
        public string DisplayLabel = "";

        [DataInput(10)]
        [Label("KEY_INPUT")]
        public KeyboardKeyInput KeyInput;

        public VKCode Key {
            get => KeyInput != null ? KeyInput.Key : VKCode.A;
            set {
                EnsureKeyInput();
                KeyInput.Key = value;
                KeyInput.SetDataInput(nameof(KeyInput.Key), value, true);
                KeyInput.BroadcastDataInput(nameof(KeyInput.Key));
                SetDataInput(nameof(KeyInput), KeyInput, true);
            }
        }

        public bool IsListeningForKey => KeyInput != null && KeyInput.IsListeningForKey;

        public void EnsureKeyInput() {
            if (KeyInput == null) {
                KeyInput = StructuredData.Create<KeyboardKeyInput>();
                KeyInput.Key = VKCode.A;
                KeyInput.Parent = Parent;
            }
            SetupKeyInputCallbacks();
        }

        public void SetupKeyInputCallbacks() {
            if (KeyInput == null) return;
            KeyInput.Parent = Parent;
            KeyInput.OnKeyChanged = (k) => {
                UpdateDebugButtonState();
                BroadcastDataInput(nameof(Transform));
                if (Parent != null) {
                    Parent.RebuildVirtualKeyMesh(this);
                }
                RequestUiRefresh();
            };
            KeyInput.OnListeningChanged = (listening) => {
                RequestUiRefresh();
            };
        }

        [DataInput(30)]
        [Label("ATTACHMENT_ANCHOR")]
        public VirtualKeyAnchor AttachmentAnchor = VirtualKeyAnchor.LeftHand;

        [DataInput(35)]
        [Label("ASSIGNED_FINGER")]
        public KeyboardFinger AssignedFinger = KeyboardFinger.LeftIndex;

        [DataInput(38)]
        [Label("HAND_ALIGNMENT_REFERENCE")]
        [Description("HAND_ALIGNMENT_REFERENCE_DESC")]
        public bool IsHandAlignmentReference = false;

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

        protected long uiVersion = 0;

        public override long GetVersion() {
            return base.GetVersion() + uiVersion;
        }

        public override SerializedStructuredData Serialize() {
            var serialized = base.Serialize();
            serialized.version = GetVersion();
            return serialized;
        }

        public async void RequestUiRefresh() {
            await UniTask.Yield();
            uiVersion++;
            Broadcast();
        }

        public void UpdateDebugButtonState() {
            var port = GetTriggerPort(nameof(TriggerDebugForceKey));
            if (port == null || port.Properties == null) return;
            bool forced = IsDebugForced;
            string newLabel = forced ? "DEBUG_RELEASE_KEY".Localized() : "DEBUG_FORCE_KEY".Localized();
            string newDesc = forced ? "DEBUG_RELEASE_KEY_DESC".Localized() : "DEBUG_FORCE_KEY_DESC".Localized();
            bool changed = port.Properties.label != newLabel;
            port.Properties.label = newLabel;
            port.Properties.description = newDesc;
            BroadcastTriggerProperties(nameof(TriggerDebugForceKey));
            if (changed) {
                RequestUiRefresh();
            }
            BroadcastDataInput(nameof(Transform));
        }

        public bool IsLocating => Parent != null && Parent.IdentifiedVirtualKey != null && (Parent.IdentifiedVirtualKey == this || Parent.IdentifiedVirtualKey.Key == this.Key);

        public void UpdateLocateButtonLabel(bool isLocating) {
            var port = GetTriggerPort(nameof(TriggerLocateKey));
            if (port != null && port.Properties != null) {
                string newLabel = isLocating ? "STOP_LOCATING_KEY".Localized() : "LOCATE_KEY".Localized();
                bool changed = port.Properties.label != newLabel;
                port.Properties.label = newLabel;
                BroadcastTriggerProperties(nameof(TriggerLocateKey));
                if (changed) {
                    RequestUiRefresh();
                }
            }
        }

        // Backward compatibility for internal callers
        public bool IsIdentifying => IsLocating;
        public void UpdateIdentifyButtonLabel(bool isIdentifying) => UpdateLocateButtonLabel(isIdentifying);

        // Runtime rendering components
        public GameObject KeyGameObject { get; internal set; }
        public Mesh BodyMesh { get; internal set; }
        public Mesh FrameMesh { get; internal set; }
        public MeshRenderer BodyRenderer { get; internal set; }
        public MeshRenderer FrameRenderer { get; internal set; }
        public Material BodyMaterial { get; internal set; }
        public Material BorderMaterial { get; internal set; }

        public float CurrentPressProgress { get; internal set; } = 0f;
        public bool IsGlowResting { get; internal set; } = false;
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
            var anchorTag = AttachmentAnchor == VirtualKeyAnchor.LeftHand ? "🫲" :
                            AttachmentAnchor == VirtualKeyAnchor.RightHand ? "🫱" : "⌨️";
            if (IsHandAlignmentReference) {
                anchorTag += "🎯";
            }
            var pos = Transform != null ? Transform.Position : Vector3.zero;
            var listeningTag = IsListeningForKey ? " ⏳[LISTENING]" : "";
            var locateTag = IsLocating ? " ✨[LOCATING]" : "";
            var debugTag = IsDebugForced ? " ⚡[DEBUG]" : "";
            return $"[{label}] ➔ [{anchorTag}] Pos({pos.x:F3}, {pos.y:F3}, {pos.z:F3}){listeningTag}{locateTag}{debugTag}";
        }

        protected override void OnCreate() {
            base.OnCreate();
            EnsureKeyInput();
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

            Watch(nameof(KeyInput), delegate {
                SetupKeyInputCallbacks();
                UpdateDebugButtonState();
                BroadcastDataInput(nameof(Transform));
                if (Parent != null) {
                    Parent.RebuildVirtualKeyMesh(this);
                }
            });
            Watch(nameof(Transform), delegate {
                SetupTransformCallbacks();
                if (KeyGameObject != null && Transform != null) {
                    KeyGameObject.transform.localPosition = Transform.Position;
                    KeyGameObject.transform.localRotation = Quaternion.Euler(Transform.Rotation);
                    KeyGameObject.transform.localScale = Transform.Scale;
                }
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
            Watch(nameof(IsHandAlignmentReference), delegate {
                if (IsHandAlignmentReference && Parent != null) {
                    Parent.OnVirtualKeyHandAlignmentReferenceChanged(this);
                }
                UpdateDebugButtonState();
                BroadcastDataInput(nameof(Transform));
                Parent?.BindingGenerator?.UpdateStatus();
                RequestUiRefresh();
            });
            Watch(nameof(AssignedFinger), delegate {
                if (IsHandAlignmentReference && Parent != null) {
                    Parent.OnVirtualKeyHandAlignmentReferenceChanged(this);
                }
                Parent?.BindingGenerator?.UpdateStatus();
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

        public override void Deserialize(SerializedStructuredData serialized) {
            base.Deserialize(serialized);
            if (serialized.dataInputs != null && serialized.dataInputs.TryGetValue("Key", out var legacyPort) && !string.IsNullOrEmpty(legacyPort?.value)) {
                if (Enum.TryParse<VKCode>(legacyPort.value, out var legacyVk)) {
                    Key = legacyVk;
                }
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            if (Parent != null && KeyInput != null && Parent.ActiveKeyListeningTarget == (IKeyListeningTarget)KeyInput) {
                Parent.CancelKeyListening();
            }
            if (Parent != null && IsHandAlignmentReference) {
                Parent.BindingGenerator?.UpdateStatus();
            }
            Transform?.DestroyAnchor();
            DestroyVisuals();
        }

        public void SetupTransformCallbacks() {
            if (KeyInput != null) {
                KeyInput.Parent = Parent;
            }
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
            BodyRenderer = null;
            FrameRenderer = null;
            CurrentSize = new Vector2(-1f, -1f);
            CurrentBorderWidths = new Vector4(-1f, -1f, -1f, -1f);
            CurrentCornerRadii = new Vector4(-1f, -1f, -1f, -1f);
            IsGlowResting = false;
        }
    }
}
