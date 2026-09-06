using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets.Utility;

namespace FlameStream
{
    public partial class KeyboardReceiverAsset : ReceiverAsset
    {
        [Section("FLOATING_KEYS")]

        [DataInput(500)]
        [Label("ENABLE_FLOATING_KEYS")]
        public bool EnableFloatingKeys = false;

        // Base Look & Feel
        [DataInput(510)]
        [Label("DEFAULT_KEY_APPEARANCE")]
        public VirtualKeyAppearance DefaultAppearance;

        public Vector2 BaseKeySize => DefaultAppearance?.Shape?.Normal?.Size ?? new Vector2(0.032f, 0.032f);

        // Floating Key Layout (Collapsible StructuredData)
        [DataInput(520)]
        [Label("FLOATING_KEY_LAYOUT")]
        public VirtualKeyCollection VirtualKeysCollection;

        public IKeyListeningTarget ActiveKeyListeningTarget { get; private set; }

        public void ToggleKeyListening(IKeyListeningTarget target) {
            if (target == null) return;
            if (ActiveKeyListeningTarget == target) {
                CancelKeyListening();
                return;
            }
            if (ActiveKeyListeningTarget != null) {
                ActiveKeyListeningTarget.SetListeningForKey(false);
            }
            ActiveKeyListeningTarget = target;
            ActiveKeyListeningTarget.SetListeningForKey(true);
        }

        public void CancelKeyListening() {
            if (ActiveKeyListeningTarget != null) {
                ActiveKeyListeningTarget.SetListeningForKey(false);
                ActiveKeyListeningTarget = null;
            }
        }

        // Key Origins (Collapsible StructuredData)
        [DataInput(530)]
        [Label("KEY_ORIGINS")]
        public VirtualKeysetTransforms KeysetTransforms;

        public CollapsibleVisualSetupTransform LeftKeysetTransform => KeysetTransforms?.Left;
        public CollapsibleVisualSetupTransform RightKeysetTransform => KeysetTransforms?.Right;
        public CollapsibleVisualSetupTransform RootKeysetTransform => KeysetTransforms?.Root;

        public VirtualKeyDefinition[] VirtualKeys {
            get => VirtualKeysCollection?.Keys ?? new VirtualKeyDefinition[0];
            set {
                if (VirtualKeysCollection == null) {
                    VirtualKeysCollection = StructuredData.Create<VirtualKeyCollection>();
                    VirtualKeysCollection.Parent = this;
                }
                VirtualKeysCollection.Keys = value;
            }
        }

        public VirtualKeyLayoutPreset KeysLayoutPreset {
            get => VirtualKeysCollection?.Preset ?? VirtualKeyLayoutPreset.GamingWASDExtended;
            set {
                if (VirtualKeysCollection != null) {
                    VirtualKeysCollection.Preset = value;
                }
            }
        }

        public void TriggerApplyKeysLayoutPreset() {
            ApplyVirtualKeyPreset(KeysLayoutPreset);
        }

        public void TriggerClearIdentifiedKey() {
            ClearIdentifiedKey();
        }

        // Runtime objects
        public VirtualKeyDefinition ActiveVirtualKeyInVisualSetup;
        public VirtualKeyDefinition IdentifiedVirtualKey;

        GameObject leftKeysGroupGo;
        GameObject rightKeysGroupGo;
        GameObject rootKeysGroupGo;

        GameObject identifyPulseGo;
        MeshFilter identifyPulseBodyMf;
        MeshRenderer identifyPulseBodyMr;
        Material identifyPulseBodyMat;
        MeshFilter identifyPulseFrameMf;
        MeshRenderer identifyPulseFrameMr;
        Material identifyPulseFrameMat;

        void OnCreateVirtualKeys() {
            if (VirtualKeysCollection == null) {
                VirtualKeysCollection = StructuredData.Create<VirtualKeyCollection>();
                VirtualKeysCollection.CollapsedSelf = true;
            }
            VirtualKeysCollection.Parent = this;

            if (KeysetTransforms == null) {
                KeysetTransforms = StructuredData.Create<VirtualKeysetTransforms>();
                KeysetTransforms.CollapsedSelf = true;
            }
            KeysetTransforms.Parent = this;
            SetupKeysetTransforms();

            if (DefaultAppearance == null) {
                DefaultAppearance = VirtualKeyAppearance.CreateDefault();
            }
            DefaultAppearance.OnAppearanceChanged = () => {
                RebuildAllVirtualKeyMeshes();
            };

            Watch(nameof(EnableFloatingKeys), delegate {
                UpdateFloatingKeysState();
            });
            Watch(nameof(IsEnabled), delegate {
                UpdateFloatingKeysState();
            });
            Watch(nameof(VirtualKeysCollection), delegate {
                if (VirtualKeysCollection != null) {
                    VirtualKeysCollection.Parent = this;
                }
                RefreshVirtualKeysParent();
                RebuildAllVirtualKeyMeshes();
            });
            Watch(nameof(KeysetTransforms), delegate {
                if (KeysetTransforms != null) {
                    KeysetTransforms.Parent = this;
                }
                SetupKeysetTransforms();
            });

            Watch(nameof(DefaultAppearance), delegate {
                if (DefaultAppearance != null) {
                    DefaultAppearance.OnAppearanceChanged = () => { RebuildAllVirtualKeyMeshes(); };
                }
                RebuildAllVirtualKeyMeshes();
            });

            RefreshVirtualKeysParent();
            if (VirtualKeys == null || VirtualKeys.Length == 0) {
                ApplyVirtualKeyPreset(KeysLayoutPreset, false);
            }

            if (EnableFloatingKeys && IsEnabled) {
                UpdateFloatingKeysState();
            }
        }

        public void OnVirtualKeysChanged() {
            RefreshVirtualKeysParent();
            RebuildAllVirtualKeyMeshes();
        }

        void OnDestroyVirtualKeys() {
            CancelKeyListening();
            ActiveVirtualKeyInVisualSetup = null;
            DestroyAllVirtualKeyVisuals();
        }

        public void SetupKeysetTransforms() {
            if (KeysetTransforms == null) {
                KeysetTransforms = StructuredData.Create<VirtualKeysetTransforms>();
                KeysetTransforms.Parent = this;
            }
            KeysetTransforms.CollapsedSelf = true;
            SetupLeftKeysetTransform();
            SetupRightKeysetTransform();
            SetupRootKeysetTransform();
        }

        void SetupLeftKeysetTransform() {
            var transforms = KeysetTransforms;
            if (transforms == null) return;
            transforms.CollapsedSelf = true;
            if (transforms.Left == null) {
                transforms.Left = StructuredData.Create<CollapsibleVisualSetupTransform>();
                transforms.Left.Position = Vector3.zero;
                transforms.Left.Rotation = Vector3.zero;
                transforms.Left.Scale = Vector3.one;
            }
            transforms.Left.CollapsedSelf = true;
            LeftKeysetTransform.LocalizationKey = "LEFT_KEY_ORIGIN";
            LeftKeysetTransform.OnEnterVisualSetup = (tr) => {
                EnsureAnchors();
                tr.OriginIndicatorOffset = LeftHandRestPosition;
            };
            LeftKeysetTransform.OnCreateAnchor = (tr, setupAnchor) => {
                var parent = RootAnchor;
                if (parent != null) {
                    setupAnchor.Attachable.Parent = parent;
                    if (parent.GameObject != null) {
                        setupAnchor.GameObject.transform.SetParent(parent.GameObject.transform, false);
                    }
                }
                var basePos = LeftHandRestPosition;
                var baseRot = Quaternion.Euler(LeftHandRestRotation);
                setupAnchor.Transform.Position = basePos + baseRot * tr.Position;
                setupAnchor.Transform.Rotation = Helper.NormalizeRotationAngles((baseRot * Quaternion.Euler(tr.Rotation)).eulerAngles);
                setupAnchor.Transform.Scale = tr.Scale;
                setupAnchor.Name = "⌛⚓-Left Key Origin";
                setupAnchor.Instructions = "Position, rotate, and scale the entire **Left Key Origin**, relative to the **Left Hand Rest Pose**.\nThe red sphere indicates the origin location (Left Hand Rest Pose).\nClick **[Apply Transform]** when finished.";
                setupAnchor.Transform.Broadcast();
            };
            LeftKeysetTransform.OnApplyAnchor = (tr, setupAnchor) => {
                var basePos = LeftHandRestPosition;
                var baseRot = Quaternion.Euler(LeftHandRestRotation);
                tr.Position = Quaternion.Inverse(baseRot) * (setupAnchor.Transform.Position - basePos);
                tr.Rotation = Helper.NormalizeRotationAngles((Quaternion.Inverse(baseRot) * Quaternion.Euler(setupAnchor.Transform.Rotation)).eulerAngles);
                tr.Scale = setupAnchor.Transform.Scale;
                UpdateGroupTransforms();
            };
            LeftKeysetTransform.OnResetAnchor = (tr, setupAnchor) => {
                var basePos = LeftHandRestPosition;
                var baseRot = Quaternion.Euler(LeftHandRestRotation);
                setupAnchor.Transform.Position = basePos;
                setupAnchor.Transform.Rotation = Helper.NormalizeRotationAngles(baseRot.eulerAngles);
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Transform.Broadcast();
                UpdateGroupTransforms();
            };
            LeftKeysetTransform.OnReset = () => {
                UpdateGroupTransforms();
            };
            LeftKeysetTransform.OnExitVisualSetup = (isApplied) => {
                UpdateGroupTransforms();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };
        }

        void SetupRightKeysetTransform() {
            var transforms = KeysetTransforms;
            if (transforms == null) return;
            transforms.CollapsedSelf = true;
            if (transforms.Right == null) {
                transforms.Right = StructuredData.Create<CollapsibleVisualSetupTransform>();
                transforms.Right.Position = Vector3.zero;
                transforms.Right.Rotation = Vector3.zero;
                transforms.Right.Scale = Vector3.one;
            }
            transforms.Right.CollapsedSelf = true;
            RightKeysetTransform.LocalizationKey = "RIGHT_KEY_ORIGIN";
            RightKeysetTransform.OnEnterVisualSetup = (tr) => {
                EnsureAnchors();
                tr.OriginIndicatorOffset = RightHandRestPosition;
            };
            RightKeysetTransform.OnCreateAnchor = (tr, setupAnchor) => {
                var parent = RootAnchor;
                if (parent != null) {
                    setupAnchor.Attachable.Parent = parent;
                    if (parent.GameObject != null) {
                        setupAnchor.GameObject.transform.SetParent(parent.GameObject.transform, false);
                    }
                }
                var basePos = RightHandRestPosition;
                var baseRot = Quaternion.Euler(RightHandRestRotation);
                setupAnchor.Transform.Position = basePos + baseRot * tr.Position;
                setupAnchor.Transform.Rotation = Helper.NormalizeRotationAngles((baseRot * Quaternion.Euler(tr.Rotation)).eulerAngles);
                setupAnchor.Transform.Scale = tr.Scale;
                setupAnchor.Name = "⌛⚓-Right Key Origin";
                setupAnchor.Instructions = "Position, rotate, and scale the entire **Right Key Origin**, relative to the **Right Hand Rest Pose**.\nThe red sphere indicates the origin location (Right Hand Rest Pose).\nClick **[Apply Transform]** when finished.";
                setupAnchor.Transform.Broadcast();
            };
            RightKeysetTransform.OnApplyAnchor = (tr, setupAnchor) => {
                var basePos = RightHandRestPosition;
                var baseRot = Quaternion.Euler(RightHandRestRotation);
                tr.Position = Quaternion.Inverse(baseRot) * (setupAnchor.Transform.Position - basePos);
                tr.Rotation = Helper.NormalizeRotationAngles((Quaternion.Inverse(baseRot) * Quaternion.Euler(setupAnchor.Transform.Rotation)).eulerAngles);
                tr.Scale = setupAnchor.Transform.Scale;
                UpdateGroupTransforms();
            };
            RightKeysetTransform.OnResetAnchor = (tr, setupAnchor) => {
                var basePos = RightHandRestPosition;
                var baseRot = Quaternion.Euler(RightHandRestRotation);
                setupAnchor.Transform.Position = basePos;
                setupAnchor.Transform.Rotation = Helper.NormalizeRotationAngles(baseRot.eulerAngles);
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Transform.Broadcast();
                UpdateGroupTransforms();
            };
            RightKeysetTransform.OnReset = () => {
                UpdateGroupTransforms();
            };
            RightKeysetTransform.OnExitVisualSetup = (isApplied) => {
                UpdateGroupTransforms();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };
        }

        void SetupRootKeysetTransform() {
            var transforms = KeysetTransforms;
            if (transforms == null) return;
            transforms.CollapsedSelf = true;
            if (transforms.Root == null) {
                transforms.Root = StructuredData.Create<CollapsibleVisualSetupTransform>();
                transforms.Root.Position = Vector3.zero;
                transforms.Root.Rotation = Vector3.zero;
                transforms.Root.Scale = Vector3.one;
            }
            transforms.Root.CollapsedSelf = true;
            RootKeysetTransform.LocalizationKey = "CENTER_KEY_ORIGIN";
            RootKeysetTransform.OnEnterVisualSetup = (tr) => { EnsureAnchors(); };
            RootKeysetTransform.OnCreateAnchor = (tr, setupAnchor) => {
                var parent = RootAnchor;
                if (parent != null) {
                    setupAnchor.Attachable.Parent = parent;
                    if (parent.GameObject != null) {
                        setupAnchor.GameObject.transform.SetParent(parent.GameObject.transform, false);
                    }
                }
                setupAnchor.Transform.Position = tr.Position;
                setupAnchor.Transform.Rotation = tr.Rotation;
                setupAnchor.Transform.Scale = tr.Scale;
                setupAnchor.Name = "⌛⚓-Center Key Origin";
                setupAnchor.Instructions = "Position, rotate, and scale the entire **Center Key Origin**, relative to the **Keyboard Rig Origin**.\nThe red sphere indicates the origin location (Keyboard Rig Origin).\nClick **[Apply Transform]** when finished.";
                setupAnchor.Transform.Broadcast();
            };
            RootKeysetTransform.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                tr.Scale = setupAnchor.Transform.Scale;
                UpdateGroupTransforms();
            };
            RootKeysetTransform.OnResetAnchor = (tr, setupAnchor) => {
                setupAnchor.Transform.Position = Vector3.zero;
                setupAnchor.Transform.Rotation = Vector3.zero;
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Transform.Broadcast();
                UpdateGroupTransforms();
            };
            RootKeysetTransform.OnReset = () => {
                UpdateGroupTransforms();
            };
            RootKeysetTransform.OnExitVisualSetup = (isApplied) => {
                UpdateGroupTransforms();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };
        }

        public void RefreshVirtualKeysParent() {
            if (VirtualKeysCollection != null) {
                VirtualKeysCollection.Parent = this;
            }
            if (VirtualKeys == null) return;
            for (int i = 0; i < VirtualKeys.Length; i++) {
                if (VirtualKeys[i] != null) {
                    VirtualKeys[i].Parent = this;
                    VirtualKeys[i].SetupTransformCallbacks();
                }
            }
        }

        public void ToggleLocateVirtualKey(VirtualKeyDefinition keyDef) => ToggleIdentifyVirtualKey(keyDef);

        public void ToggleIdentifyVirtualKey(VirtualKeyDefinition keyDef) {
            if (keyDef == null) return;
            if (IdentifiedVirtualKey == keyDef) {
                ClearIdentifiedKey();
            } else {
                SetIdentifiedVirtualKey(keyDef);
            }
        }

        public void SetIdentifiedVirtualKey(VirtualKeyDefinition keyDef) {
            var previous = IdentifiedVirtualKey;
            IdentifiedVirtualKey = keyDef;
            if (previous != null && previous != keyDef) {
                previous.UpdateIdentifyButtonLabel(false);
                UpdateVirtualKeyColors(previous);
            }
            if (keyDef != null) {
                keyDef.UpdateIdentifyButtonLabel(true);
            }
            UpdateIdentifyVisuals();
            if (keyDef != null) {
                var label = string.IsNullOrEmpty(keyDef.DisplayLabel) ? keyDef.Key.ToString() : keyDef.DisplayLabel;
                Context.Service?.Toast(
                    Warudo.Core.Server.ToastSeverity.Info,
                    Name,
                    $"Identifying Virtual Key: [{label}] ({keyDef.AttachmentAnchor})"
                );
            }
        }

        public void ClearIdentifiedKey() {
            var previous = IdentifiedVirtualKey;
            IdentifiedVirtualKey = null;
            if (identifyPulseGo != null) {
                identifyPulseGo.SetActive(false);
            }
            if (previous != null) {
                previous.UpdateIdentifyButtonLabel(false);
                UpdateVirtualKeyColors(previous);
            }
        }

        public void DetachIdentifyPulseIfParentedTo(VirtualKeyDefinition keyDef) {
            if (identifyPulseGo != null && keyDef != null && keyDef.KeyGameObject != null) {
                if (identifyPulseGo.transform.parent == keyDef.KeyGameObject.transform) {
                    identifyPulseGo.transform.SetParent(null, false);
                    identifyPulseGo.SetActive(false);
                }
            }
        }

        void EnsureIdentifyPulseObjects() {
            if (identifyPulseGo != null) return;

            identifyPulseGo = new GameObject("IdentifyPulse");

            var bodyGo = new GameObject("PulseBody");
            bodyGo.transform.SetParent(identifyPulseGo.transform, false);
            bodyGo.transform.localPosition = new Vector3(0f, 0.0012f, 0f);
            identifyPulseBodyMf = bodyGo.AddComponent<MeshFilter>();
            identifyPulseBodyMr = bodyGo.AddComponent<MeshRenderer>();
            identifyPulseBodyMat = CreateKeyMaterial();
            identifyPulseBodyMat.renderQueue = 3050;
            identifyPulseBodyMr.material = identifyPulseBodyMat;

            var frameGo = new GameObject("PulseFrame");
            frameGo.transform.SetParent(identifyPulseGo.transform, false);
            frameGo.transform.localPosition = new Vector3(0f, 0.0015f, 0f);
            identifyPulseFrameMf = frameGo.AddComponent<MeshFilter>();
            identifyPulseFrameMr = frameGo.AddComponent<MeshRenderer>();
            identifyPulseFrameMat = CreateKeyMaterial();
            identifyPulseFrameMat.renderQueue = 3051;
            identifyPulseFrameMr.material = identifyPulseFrameMat;
        }

        public void UpdateIdentifyVisuals() {
            if (IdentifiedVirtualKey == null || IdentifiedVirtualKey.KeyGameObject == null || !EnableFloatingKeys || !IsEnabled) {
                if (identifyPulseGo != null) {
                    identifyPulseGo.SetActive(false);
                }
                return;
            }

            EnsureIdentifyPulseObjects();
            identifyPulseGo.transform.SetParent(IdentifiedVirtualKey.KeyGameObject.transform, false);
            identifyPulseGo.transform.localPosition = Vector3.zero;
            identifyPulseGo.transform.localRotation = Quaternion.identity;
            identifyPulseGo.transform.localScale = Vector3.one;

            identifyPulseBodyMf.sharedMesh = IdentifiedVirtualKey.BodyMesh;
            identifyPulseFrameMf.sharedMesh = IdentifiedVirtualKey.FrameMesh;

            identifyPulseGo.SetActive(true);
        }

        void UpdateIdentifyPulseAnimation() {
            if (IdentifiedVirtualKey == null || IdentifiedVirtualKey.KeyGameObject == null) {
                if (identifyPulseGo != null && identifyPulseGo.activeSelf) {
                    identifyPulseGo.SetActive(false);
                }
                return;
            }

            if (identifyPulseGo == null || !identifyPulseGo.activeSelf) {
                UpdateIdentifyVisuals();
                if (identifyPulseGo == null || !identifyPulseGo.activeSelf) return;
            }

            // Sync shared meshes in case the identified key's shape changed/morphed
            if (identifyPulseBodyMf != null && identifyPulseBodyMf.sharedMesh != IdentifiedVirtualKey.BodyMesh) {
                identifyPulseBodyMf.sharedMesh = IdentifiedVirtualKey.BodyMesh;
            }
            if (identifyPulseFrameMf != null && identifyPulseFrameMf.sharedMesh != IdentifiedVirtualKey.FrameMesh) {
                identifyPulseFrameMf.sharedMesh = IdentifiedVirtualKey.FrameMesh;
            }

            // Pulse cycle: 0.85 seconds
            float cycle = 0.85f;
            float t = (Time.time % cycle) / cycle;

            // Ease out expansion for shockwave effect (scale from 1.0x to 1.85x on X and Z)
            float easedT = DOVirtual.EasedValue(0f, 1f, t, Ease.OutQuad);
            float scale = Mathf.Lerp(1.0f, 1.85f, easedT);
            identifyPulseGo.transform.localScale = new Vector3(scale, 1.0f, scale);

            // Ease in fade out (fades smoothly to transparent)
            float alphaProgress = DOVirtual.EasedValue(0f, 1f, t, Ease.InQuad);
            float alpha = Mathf.Lerp(0.9f, 0.0f, alphaProgress);

            // Electric cyan/blue coloring ensures extreme contrast on pure white, dark, or colored keys
            Color cyanFrame = new Color(0f, 0.85f, 1.0f, alpha);
            Color cyanFrameEmission = new Color(0f, 0.85f, 1.0f) * (alpha * 3.5f);

            Color cyanBody = new Color(0.1f, 0.75f, 1.0f, alpha * 0.35f);
            Color cyanBodyEmission = new Color(0.1f, 0.75f, 1.0f) * (alpha * 0.35f * 1.5f);

            if (identifyPulseFrameMat != null) {
                identifyPulseFrameMat.color = cyanFrame;
                identifyPulseFrameMat.SetColor("_EmissionColor", cyanFrameEmission);
            }
            if (identifyPulseBodyMat != null) {
                identifyPulseBodyMat.color = cyanBody;
                identifyPulseBodyMat.SetColor("_EmissionColor", cyanBodyEmission);
            }
        }

        public AnchorAsset GetAnchorForVirtualKey(VirtualKeyAnchor anchorType) {
            switch (anchorType) {
                case VirtualKeyAnchor.LeftHand: return LeftHandAnchor;
                case VirtualKeyAnchor.RightHand: return RightHandAnchor;
                case VirtualKeyAnchor.KeyboardRoot: return RootAnchor;
                default: return RootAnchor;
            }
        }

        public GameObject GetGroupGameObject(VirtualKeyAnchor anchorType) {
            switch (anchorType) {
                case VirtualKeyAnchor.LeftHand:
                    if (leftKeysGroupGo == null) {
                        leftKeysGroupGo = new GameObject("KeysetGroup_LeftHand");
                    }
                    return leftKeysGroupGo;

                case VirtualKeyAnchor.RightHand:
                    if (rightKeysGroupGo == null) {
                        rightKeysGroupGo = new GameObject("KeysetGroup_RightHand");
                    }
                    return rightKeysGroupGo;

                case VirtualKeyAnchor.KeyboardRoot:
                default:
                    if (rootKeysGroupGo == null) {
                        rootKeysGroupGo = new GameObject("KeysetGroup_Root");
                    }
                    return rootKeysGroupGo;
            }
        }

        void UpdateGroupTransforms() {
            if (!EnableFloatingKeys || !IsEnabled) return;

            // 1. Left Group
            if (leftKeysGroupGo != null) {
                var anchor = RootAnchor;
                if (anchor != null && anchor.GameObject != null) {
                    if (leftKeysGroupGo.transform.parent != anchor.GameObject.transform) {
                        leftKeysGroupGo.transform.SetParent(anchor.GameObject.transform, false);
                    }
                }
                if (LeftKeysetTransform != null && LeftKeysetTransform.InVisualSetupMode && LeftKeysetTransform.setupAnchor != null) {
                    leftKeysGroupGo.transform.localPosition = LeftKeysetTransform.setupAnchor.Transform.Position;
                    leftKeysGroupGo.transform.localRotation = Quaternion.Euler(LeftKeysetTransform.setupAnchor.Transform.Rotation);
                    leftKeysGroupGo.transform.localScale = LeftKeysetTransform.setupAnchor.Transform.Scale;
                } else {
                    var basePos = LeftHandRestPosition;
                    var baseRot = Quaternion.Euler(LeftHandRestRotation);
                    var pos = LeftKeysetTransform?.Position ?? Vector3.zero;
                    var rot = LeftKeysetTransform?.Rotation ?? Vector3.zero;
                    var scale = LeftKeysetTransform?.Scale ?? Vector3.one;
                    leftKeysGroupGo.transform.localPosition = basePos + baseRot * pos;
                    leftKeysGroupGo.transform.localRotation = baseRot * Quaternion.Euler(rot);
                    leftKeysGroupGo.transform.localScale = scale;
                }
            }

            // 2. Right Group
            if (rightKeysGroupGo != null) {
                var anchor = RootAnchor;
                if (anchor != null && anchor.GameObject != null) {
                    if (rightKeysGroupGo.transform.parent != anchor.GameObject.transform) {
                        rightKeysGroupGo.transform.SetParent(anchor.GameObject.transform, false);
                    }
                }
                if (RightKeysetTransform != null && RightKeysetTransform.InVisualSetupMode && RightKeysetTransform.setupAnchor != null) {
                    rightKeysGroupGo.transform.localPosition = RightKeysetTransform.setupAnchor.Transform.Position;
                    rightKeysGroupGo.transform.localRotation = Quaternion.Euler(RightKeysetTransform.setupAnchor.Transform.Rotation);
                    rightKeysGroupGo.transform.localScale = RightKeysetTransform.setupAnchor.Transform.Scale;
                } else {
                    var basePos = RightHandRestPosition;
                    var baseRot = Quaternion.Euler(RightHandRestRotation);
                    var pos = RightKeysetTransform?.Position ?? Vector3.zero;
                    var rot = RightKeysetTransform?.Rotation ?? Vector3.zero;
                    var scale = RightKeysetTransform?.Scale ?? Vector3.one;
                    rightKeysGroupGo.transform.localPosition = basePos + baseRot * pos;
                    rightKeysGroupGo.transform.localRotation = baseRot * Quaternion.Euler(rot);
                    rightKeysGroupGo.transform.localScale = scale;
                }
            }

            // 3. Root Group
            if (rootKeysGroupGo != null) {
                var anchor = RootAnchor;
                if (anchor != null && anchor.GameObject != null) {
                    if (rootKeysGroupGo.transform.parent != anchor.GameObject.transform) {
                        rootKeysGroupGo.transform.SetParent(anchor.GameObject.transform, false);
                    }
                }
                var pos = RootKeysetTransform?.Position ?? Vector3.zero;
                var rot = RootKeysetTransform?.Rotation ?? Vector3.zero;
                var scale = RootKeysetTransform?.Scale ?? Vector3.one;
                if (RootKeysetTransform != null && RootKeysetTransform.InVisualSetupMode && RootKeysetTransform.setupAnchor != null) {
                    pos = RootKeysetTransform.setupAnchor.Transform.Position;
                    rot = RootKeysetTransform.setupAnchor.Transform.Rotation;
                    scale = RootKeysetTransform.setupAnchor.Transform.Scale;
                }
                rootKeysGroupGo.transform.localPosition = pos;
                rootKeysGroupGo.transform.localRotation = Quaternion.Euler(rot);
                rootKeysGroupGo.transform.localScale = scale;
            }
        }

        void UpdateFloatingKeysState() {
            if (EnableFloatingKeys && IsEnabled) {
                EnsureAnchors();
                UpdateGroupTransforms();
                RebuildAllVirtualKeyMeshes();
            } else {
                DestroyAllVirtualKeyVisuals();
            }
        }

        void OnUpdateVirtualKeys() {
            if (!EnableFloatingKeys || !IsEnabled) return;

            // Auto-restore meshes if hot-reloaded and visuals haven't been built yet
            if (VirtualKeys != null && VirtualKeys.Length > 0 && VirtualKeys[0] != null && VirtualKeys[0].KeyGameObject == null) {
                UpdateFloatingKeysState();
            }

            // Ensure parent references
            if (VirtualKeys != null && VirtualKeys.Length > 0 && VirtualKeys[0] != null && VirtualKeys[0].Parent == null) {
                RefreshVirtualKeysParent();
            }

            UpdateGroupTransforms();

            if (VirtualKeys == null) return;

            float dt = Time.deltaTime;

            for (int i = 0; i < VirtualKeys.Length; i++) {
                var keyDef = VirtualKeys[i];
                if (keyDef == null) continue;

                // Check visual setup synchronization
                if (ActiveVirtualKeyInVisualSetup == keyDef && keyDef.Transform?.setupAnchor != null && keyDef.KeyGameObject != null) {
                    keyDef.KeyGameObject.transform.localPosition = keyDef.Transform.setupAnchor.Transform.Position;
                    keyDef.KeyGameObject.transform.localRotation = Quaternion.Euler(keyDef.Transform.setupAnchor.Transform.Rotation);
                    keyDef.KeyGameObject.transform.localScale = keyDef.Transform.setupAnchor.Transform.Scale;
                }

                // Input evaluation & glow
                bool isDown = Down((int)keyDef.Key);

                ApplyKeyGlowState(keyDef, isDown, dt);
            }

            UpdateIdentifyPulseAnimation();
        }

        void ApplyKeyGlowState(VirtualKeyDefinition keyDef, bool isDown, float dt = 0f) {
            if (keyDef.BorderMaterial == null || keyDef.BodyMaterial == null) return;

            bool isIdentifying = (keyDef == IdentifiedVirtualKey);
            var customApp = (keyDef.UseCustomAppearance && keyDef.CustomAppearance != null) ? keyDef.CustomAppearance : null;
            var baseApp = DefaultAppearance;

            // 1. Resolve Shape Normal and Pressed styles
            bool customShapeEnabled = (customApp?.Shape != null && customApp.Shape.Enabled);
            var shapeNormal = (customShapeEnabled && customApp.Shape.Normal != null && customApp.Shape.Normal.Enabled)
                ? customApp.Shape.Normal
                : baseApp?.Shape?.Normal;

            VirtualKeyShapeStateStyle shapePressed = null;
            if (customShapeEnabled) {
                if (customApp.Shape.EnablePressedState && customApp.Shape.Pressed != null && customApp.Shape.Pressed.Enabled) {
                    shapePressed = customApp.Shape.Pressed;
                } else if (!customApp.Shape.EnablePressedState) {
                    shapePressed = shapeNormal; // User explicitly disabled press squish for this key
                } else if (baseApp?.Shape != null && baseApp.Shape.EnablePressedState && baseApp.Shape.Pressed != null && baseApp.Shape.Pressed.Enabled) {
                    shapePressed = baseApp.Shape.Pressed;
                } else {
                    shapePressed = shapeNormal;
                }
            } else {
                bool baseHasPressed = (baseApp?.Shape != null && baseApp.Shape.EnablePressedState && baseApp.Shape.Pressed != null && baseApp.Shape.Pressed.Enabled);
                shapePressed = baseHasPressed ? baseApp.Shape.Pressed : shapeNormal;
            }

            // 2. Resolve Body Normal and Pressed styles
            // Note: During Identify, bypass EnablePressedState = false so body color/glow always pulses.
            bool customBodyEnabled = (customApp?.Body != null && customApp.Body.Enabled);
            var bodyNormal = (customBodyEnabled && customApp.Body.Normal != null && customApp.Body.Normal.Enabled)
                ? customApp.Body.Normal
                : baseApp?.Body?.Normal;

            VirtualKeyMaterialStateStyle bodyPressed = null;
            if (customBodyEnabled) {
                if ((customApp.Body.EnablePressedState || isIdentifying) && customApp.Body.Pressed != null && customApp.Body.Pressed.Enabled) {
                    bodyPressed = customApp.Body.Pressed;
                } else if (!customApp.Body.EnablePressedState && !isIdentifying) {
                    bodyPressed = bodyNormal; // User explicitly disabled press state for this key
                } else if (baseApp?.Body != null && (baseApp.Body.EnablePressedState || isIdentifying) && baseApp.Body.Pressed != null && baseApp.Body.Pressed.Enabled) {
                    bodyPressed = baseApp.Body.Pressed;
                } else {
                    bodyPressed = bodyNormal;
                }
            } else {
                bool baseHasPressed = (baseApp?.Body != null && (baseApp.Body.EnablePressedState || isIdentifying) && baseApp.Body.Pressed != null && baseApp.Body.Pressed.Enabled);
                bodyPressed = baseHasPressed ? baseApp.Body.Pressed : bodyNormal;
            }

            // 3. Resolve Border Normal and Pressed styles
            // Note: During Identify, bypass EnablePressedState = false so border color/glow always pulses.
            bool customBorderEnabled = (customApp?.Border != null && customApp.Border.Enabled);
            var borderNormal = (customBorderEnabled && customApp.Border.Normal != null && customApp.Border.Normal.Enabled)
                ? customApp.Border.Normal
                : baseApp?.Border?.Normal;

            VirtualKeyMaterialStateStyle borderPressed = null;
            if (customBorderEnabled) {
                if ((customApp.Border.EnablePressedState || isIdentifying) && customApp.Border.Pressed != null && customApp.Border.Pressed.Enabled) {
                    borderPressed = customApp.Border.Pressed;
                } else if (!customApp.Border.EnablePressedState && !isIdentifying) {
                    borderPressed = borderNormal; // User explicitly disabled press state for this key
                } else if (baseApp?.Border != null && (baseApp.Border.EnablePressedState || isIdentifying) && baseApp.Border.Pressed != null && baseApp.Border.Pressed.Enabled) {
                    borderPressed = baseApp.Border.Pressed;
                } else {
                    borderPressed = borderNormal;
                }
            } else {
                bool baseHasPressed = (baseApp?.Border != null && (baseApp.Border.EnablePressedState || isIdentifying) && baseApp.Border.Pressed != null && baseApp.Border.Pressed.Enabled) ;
                borderPressed = baseHasPressed ? baseApp.Border.Pressed : borderNormal;
            }

            // Resolve transitions (onPress / onRelease)
            var pressIn = customApp?.PressInTransition ?? baseApp?.PressInTransition;
            float pressInTime = pressIn?.Time ?? 0.02f;
            Ease pressInEase = pressIn?.Ease ?? Ease.Linear;

            var pressOut = customApp?.PressOutTransition ?? baseApp?.PressOutTransition;
            float pressOutTime = pressOut?.Time ?? 0.12f;
            Ease pressOutEase = pressOut?.Ease ?? Ease.OutCubic;

            // Physical press progress interpolation
            if (isDown) {
                if (pressInTime <= 0.001f || dt <= 0f) {
                    keyDef.CurrentPressProgress = 1.0f;
                } else {
                    keyDef.CurrentPressProgress = Mathf.MoveTowards(keyDef.CurrentPressProgress, 1.0f, dt / pressInTime);
                }
            } else {
                if (pressOutTime <= 0.001f || dt <= 0f) {
                    keyDef.CurrentPressProgress = 0.0f;
                } else {
                    keyDef.CurrentPressProgress = Mathf.MoveTowards(keyDef.CurrentPressProgress, 0.0f, dt / pressOutTime);
                }
            }

            float pressFactor = isDown
                ? DOVirtual.EasedValue(0f, 1f, keyDef.CurrentPressProgress, pressInEase)
                : DOVirtual.EasedValue(0f, 1f, keyDef.CurrentPressProgress, pressOutEase);

            // Shape factor: Only driven by physical key press, NEVER morphed by Identify
            float shapeFactor = pressFactor;

            // Glow factor: Pulses during Identify, otherwise follows physical key press
            float glowFactor = isIdentifying ? Mathf.PingPong(Time.time * 6f, 1f) : pressFactor;

            // Calculate animated dimensions (driven purely by shapeFactor)
            Vector2 normalSize = shapeNormal != null ? shapeNormal.Size : BaseKeySize;
            Vector2 pressedSize = shapePressed != null ? shapePressed.Size : normalSize;
            Vector2 currentSize = Vector2.Lerp(normalSize, pressedSize, shapeFactor);

            Vector4 normalCorners = shapeNormal != null ? shapeNormal.GetCornerRadii() : BorderVector4Extensions.Uniform(0.25f);
            Vector4 pressedCorners = shapePressed != null ? shapePressed.GetCornerRadii() : normalCorners;
            Vector4 currentCorners = Vector4.Lerp(normalCorners, pressedCorners, shapeFactor);

            Vector4 normalBorders = shapeNormal != null ? shapeNormal.GetBorderWidths() : BorderVector4Extensions.Uniform(0.0025f);
            Vector4 pressedBorders = shapePressed != null ? shapePressed.GetBorderWidths() : normalBorders;
            Vector4 currentBorders = Vector4.Lerp(normalBorders, pressedBorders, shapeFactor);

            float maxRadius = Mathf.Min(currentSize.x, currentSize.y) * 0.5f;
            Vector4 absoluteCorners = currentCorners * maxRadius;

            // In-place mesh vertex morphing during press transitions
            if (Vector2.Distance(keyDef.CurrentSize, currentSize) > 0.0001f ||
                Vector4.Distance(keyDef.CurrentBorderWidths, currentBorders) > 0.00001f ||
                Vector4.Distance(keyDef.CurrentCornerRadii, currentCorners) > 0.0001f) {

                keyDef.CurrentSize = currentSize;
                keyDef.CurrentBorderWidths = currentBorders;
                keyDef.CurrentCornerRadii = currentCorners;

                if (keyDef.BodyMesh != null) {
                    VirtualKeyMeshBuilder.UpdateRoundedRectVertices(keyDef.BodyMesh, currentSize.x, currentSize.y, absoluteCorners);
                }
                if (keyDef.FrameMesh != null) {
                    VirtualKeyMeshBuilder.UpdateRoundedRectFrameVertices(keyDef.FrameMesh, currentSize.x, currentSize.y, absoluteCorners, currentBorders);
                }
            }

            // Apply Body (Color & Glow)
            if (bodyNormal != null) {
                var targetPressed = bodyPressed ?? bodyNormal;
                Color targetBodyColor = targetPressed.Color;
                float targetBodySelfIllum = targetPressed.SelfIllumination;
                Color targetBodyGlowColor = targetPressed.GlowColor;
                float targetBodyGlowIntensity = targetPressed.GlowIntensity;

                if (isIdentifying) {
                    // Guaranteed prominent pulse during Identify even if press state has 0 glow
                    if (targetBodyGlowIntensity <= 0.2f) {
                        targetBodyGlowIntensity = Mathf.Max(bodyNormal.GlowIntensity + 2.0f, 2.0f);
                    }
                    if (targetBodyGlowColor == Color.clear || targetBodyGlowColor.a <= 0.01f) {
                        targetBodyGlowColor = Color.white;
                    }
                }

                Color bodyColor = Color.Lerp(bodyNormal.Color, targetBodyColor, glowFactor);
                float bodySelfIllum = Mathf.Clamp01(Mathf.Lerp(bodyNormal.SelfIllumination, targetBodySelfIllum, glowFactor));
                Color bodyGlowColor = Color.Lerp(bodyNormal.GlowColor, targetBodyGlowColor, glowFactor);
                float bodyGlowIntensity = Mathf.Lerp(bodyNormal.GlowIntensity, targetBodyGlowIntensity, glowFactor);

                // When SelfIllumination = 1, diffuse albedo is dimmed to 0 so scene lighting/shadows cannot darken or tint it.
                // The base color is self-emitted, with any additional glow/bloom added on top.
                Color albedo = new Color(bodyColor.r * (1f - bodySelfIllum), bodyColor.g * (1f - bodySelfIllum), bodyColor.b * (1f - bodySelfIllum), bodyColor.a);
                Color emission = (bodyColor * bodySelfIllum) + (bodyGlowColor * bodyGlowIntensity);

                keyDef.BodyMaterial.color = albedo;
                keyDef.BodyMaterial.SetColor("_EmissionColor", emission);
            }

            // Apply Border (Color & Glow)
            if (borderNormal != null) {
                var targetPressed = borderPressed ?? borderNormal;
                Color targetBorderColor = targetPressed.Color;
                float targetBorderSelfIllum = targetPressed.SelfIllumination;
                Color targetBorderGlowColor = targetPressed.GlowColor;
                float targetBorderGlowIntensity = targetPressed.GlowIntensity;

                if (isIdentifying) {
                    // Guaranteed prominent pulse during Identify even if press state has 0 glow
                    if (targetBorderGlowIntensity <= 0.2f) {
                        targetBorderGlowIntensity = Mathf.Max(borderNormal.GlowIntensity + 2.5f, 2.5f);
                    }
                    if (targetBorderGlowColor == Color.clear || targetBorderGlowColor.a <= 0.01f) {
                        targetBorderGlowColor = Color.white;
                    }
                }

                Color borderColor = Color.Lerp(borderNormal.Color, targetBorderColor, glowFactor);
                float borderSelfIllum = Mathf.Clamp01(Mathf.Lerp(borderNormal.SelfIllumination, targetBorderSelfIllum, glowFactor));
                Color borderGlowColor = Color.Lerp(borderNormal.GlowColor, targetBorderGlowColor, glowFactor);
                float borderGlowIntensity = Mathf.Lerp(borderNormal.GlowIntensity, targetBorderGlowIntensity, glowFactor);

                Color albedo = new Color(borderColor.r * (1f - borderSelfIllum), borderColor.g * (1f - borderSelfIllum), borderColor.b * (1f - borderSelfIllum), borderColor.a);
                Color emission = (borderColor * borderSelfIllum) + (borderGlowColor * borderGlowIntensity);

                keyDef.BorderMaterial.color = albedo;
                keyDef.BorderMaterial.SetColor("_EmissionColor", emission);
            }
        }

        public void UpdateVirtualKeyParenting(VirtualKeyDefinition keyDef) {
            if (keyDef == null || keyDef.KeyGameObject == null) return;
            var group = GetGroupGameObject(keyDef.AttachmentAnchor);
            if (group != null) {
                keyDef.KeyGameObject.transform.SetParent(group.transform, false);
                var pos = keyDef.Transform?.Position ?? Vector3.zero;
                var rot = keyDef.Transform?.Rotation ?? Vector3.zero;
                var scale = keyDef.Transform?.Scale ?? Vector3.one;
                keyDef.KeyGameObject.transform.localPosition = pos;
                keyDef.KeyGameObject.transform.localRotation = Quaternion.Euler(rot);
                keyDef.KeyGameObject.transform.localScale = scale;
            }
        }

        public void RebuildVirtualKeyMesh(VirtualKeyDefinition keyDef) {
            if (keyDef == null || !EnableFloatingKeys || !IsEnabled) return;

            keyDef.DestroyVisuals();

            var group = GetGroupGameObject(keyDef.AttachmentAnchor);

            var keyGo = new GameObject($"VKey_{keyDef.Key}_{keyDef.DisplayLabel}");
            if (group != null) {
                keyGo.transform.SetParent(group.transform, false);
            }
            var pos = keyDef.Transform?.Position ?? Vector3.zero;
            var rot = keyDef.Transform?.Rotation ?? Vector3.zero;
            var scale = keyDef.Transform?.Scale ?? Vector3.one;
            keyGo.transform.localPosition = pos;
            keyGo.transform.localRotation = Quaternion.Euler(rot);
            keyGo.transform.localScale = scale;

            // Calculate dimensions
            var customApp = (keyDef.UseCustomAppearance && keyDef.CustomAppearance != null) ? keyDef.CustomAppearance : null;
            var baseApp = DefaultAppearance;

            bool customShapeEnabled = (customApp?.Shape != null && customApp.Shape.Enabled && customApp.Shape.Normal != null && customApp.Shape.Normal.Enabled);
            var shapeNormal = customShapeEnabled
                ? customApp.Shape.Normal
                : baseApp?.Shape?.Normal;

            Vector2 initialSize = shapeNormal != null ? shapeNormal.Size : BaseKeySize;
            Vector4 initialBorders = shapeNormal != null ? shapeNormal.GetBorderWidths() : BorderVector4Extensions.Uniform(0.0025f);
            Vector4 initialCorners = shapeNormal != null ? shapeNormal.GetCornerRadii() : BorderVector4Extensions.Uniform(0.25f);

            float maxRadius = Mathf.Min(initialSize.x, initialSize.y) * 0.5f;
            Vector4 absoluteCorners = initialCorners * maxRadius;

            // 1. Inner Body Mesh & Material
            var bodyMesh = VirtualKeyMeshBuilder.CreateRoundedRectMesh(initialSize.x, initialSize.y, absoluteCorners);
            bodyMesh.MarkDynamic();
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(keyGo.transform, false);
            bodyGo.transform.localPosition = new Vector3(0f, 0.0001f, 0f); // Slight elevation above base

            var bodyMf = bodyGo.AddComponent<MeshFilter>();
            bodyMf.sharedMesh = bodyMesh;
            var bodyMr = bodyGo.AddComponent<MeshRenderer>();

            var bodyMat = CreateKeyMaterial();
            bodyMat.renderQueue = 3000;
            bodyMr.material = bodyMat;

            // 2. Hollow Border Frame Mesh & Material
            var frameMesh = VirtualKeyMeshBuilder.CreateRoundedRectFrameMesh(initialSize.x, initialSize.y, absoluteCorners, initialBorders);
            frameMesh.MarkDynamic();
            var frameGo = new GameObject("Border");
            frameGo.transform.SetParent(keyGo.transform, false);
            frameGo.transform.localPosition = new Vector3(0f, 0.0006f, 0f); // Cleanly above body

            var frameMf = frameGo.AddComponent<MeshFilter>();
            frameMf.sharedMesh = frameMesh;
            var frameMr = frameGo.AddComponent<MeshRenderer>();

            var borderMat = CreateKeyMaterial();
            borderMat.renderQueue = 3001;
            frameMr.material = borderMat;

            // Cache on definition
            keyDef.KeyGameObject = keyGo;
            keyDef.BodyMesh = bodyMesh;
            keyDef.FrameMesh = frameMesh;
            keyDef.BodyMaterial = bodyMat;
            keyDef.BorderMaterial = borderMat;

            if (keyDef == IdentifiedVirtualKey) {
                UpdateIdentifyVisuals();
            }

            bool isDown = Down((int)keyDef.Key);
            ApplyKeyGlowState(keyDef, isDown, 0f);
        }

        Material CreateKeyMaterial() {
            var shader = Shader.Find("Standard");
            var mat = new Material(shader);

            // Setup transparent rendering
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            mat.SetFloat("_Glossiness", 0.2f);
            mat.SetFloat("_Metallic", 0.0f);

            // Always enable emission keyword on both Body & Border materials
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetColor("_EmissionColor", Color.black);

            return mat;
        }

        public void RebuildAllVirtualKeyMeshes() {
            if (!EnableFloatingKeys || !IsEnabled || VirtualKeys == null) return;
            for (int i = 0; i < VirtualKeys.Length; i++) {
                if (VirtualKeys[i] != null) {
                    RebuildVirtualKeyMesh(VirtualKeys[i]);
                }
            }
        }

        public void UpdateVirtualKeyColors(VirtualKeyDefinition keyDef) {
            if (keyDef == null) return;
            ApplyKeyGlowState(keyDef, false, 0f);
        }

        public void UpdateAllVirtualKeyColors() {
            if (VirtualKeys == null) return;
            for (int i = 0; i < VirtualKeys.Length; i++) {
                if (VirtualKeys[i] != null) {
                    UpdateVirtualKeyColors(VirtualKeys[i]);
                }
            }
        }

        public void DestroyAllVirtualKeyVisuals() {
            if (identifyPulseGo != null) {
                UnityEngine.Object.Destroy(identifyPulseGo);
                identifyPulseGo = null;
                identifyPulseBodyMf = null;
                identifyPulseBodyMr = null;
                identifyPulseBodyMat = null;
                identifyPulseFrameMf = null;
                identifyPulseFrameMr = null;
                identifyPulseFrameMat = null;
            }

            if (VirtualKeys != null) {
                for (int i = 0; i < VirtualKeys.Length; i++) {
                    VirtualKeys[i]?.DestroyVisuals();
                }
            }

            if (leftKeysGroupGo != null) {
                UnityEngine.Object.Destroy(leftKeysGroupGo);
                leftKeysGroupGo = null;
            }
            if (rightKeysGroupGo != null) {
                UnityEngine.Object.Destroy(rightKeysGroupGo);
                rightKeysGroupGo = null;
            }
            if (rootKeysGroupGo != null) {
                UnityEngine.Object.Destroy(rootKeysGroupGo);
                rootKeysGroupGo = null;
            }
        }

        public void ApplyVirtualKeyPreset(VirtualKeyLayoutPreset preset, bool showToast = true) {
            ClearIdentifiedKey();
            var list = new List<VirtualKeyDefinition>();

            float pitch = 0.036f; // Standard key spacing (32mm key + 4mm gap)

            void AddKey(VKCode key, string label, VirtualKeyAnchor anchor, float posX, float posZ, float unitWidth = 1f, float unitHeight = 1f, float rotY = 0f, bool isCircle = false) {
                var def = StructuredData.Create<VirtualKeyDefinition>(d => {
                    d.Parent = this;
                    d.Key = key;
                    d.DisplayLabel = label;
                    d.AttachmentAnchor = anchor;
                    d.Transform.Position = new Vector3(posX, 0f, posZ);
                    d.Transform.Rotation = new Vector3(0f, rotY, 0f);
                    d.Transform.Scale = Vector3.one;
                    if (unitWidth != 1f || unitHeight != 1f || isCircle) {
                        d.UseCustomAppearance = true;
                        d.CustomAppearance = VirtualKeyAppearance.CreateForOverride();
                        d.CustomAppearance.Shape.Enabled = true;
                        d.CustomAppearance.Shape.Normal.Enabled = true;
                        float baseW = BaseKeySize.x;
                        float baseH = BaseKeySize.y;
                        float w = (unitWidth * pitch) - (pitch - baseW);
                        float h = (unitHeight * pitch) - (pitch - baseH);
                        d.CustomAppearance.Shape.Normal.Size = new Vector2(w, h);
                        if (isCircle) {
                            d.CustomAppearance.Shape.Normal.Corners.Radius = 0.5f;
                        }
                        d.CustomAppearance.Shape.EnablePressedState = true;
                        d.CustomAppearance.Shape.Pressed.Enabled = true;
                        d.CustomAppearance.Shape.Pressed.Size = new Vector2(w, h);
                        if (isCircle) {
                            d.CustomAppearance.Shape.Pressed.Corners.Radius = 0.5f;
                        }
                    }
                    d.SetupTransformCallbacks();
                });
                list.Add(def);
            }

            switch (preset) {
                case VirtualKeyLayoutPreset.GamingWASDExtended: {
                    // Extended gaming left-hand cluster:
                    // ` 1 2 3 4 5 6
                    // Tab Q W E R T
                    // Caps A S D F G
                    // Shift Z X C V B
                    // Ctrl Win Alt Space
                    var anchor = VirtualKeyAnchor.LeftHand;
                    float startX = -2.5f * pitch;
                    float z = 2f * pitch;

                    // Number row: ` (OEM_3 / 192), 1, 2, 3, 4, 5, 6
                    AddKey(VKCode.OEM_3, "`", anchor, startX + 0 * pitch, z);
                    AddKey(VKCode.KEY_1, "1", anchor, startX + 1 * pitch, z);
                    AddKey(VKCode.KEY_2, "2", anchor, startX + 2 * pitch, z);
                    AddKey(VKCode.KEY_3, "3", anchor, startX + 3 * pitch, z);
                    AddKey(VKCode.KEY_4, "4", anchor, startX + 4 * pitch, z);
                    AddKey(VKCode.KEY_5, "5", anchor, startX + 5 * pitch, z);
                    AddKey(VKCode.KEY_6, "6", anchor, startX + 6 * pitch, z);

                    // Row 1: Tab (1.5u), Q, W, E, R, T
                    z -= pitch;
                    AddKey(VKCode.TAB, "Tab", anchor, startX + 0.25f * pitch, z, 1.5f, 1f);
                    AddKey(VKCode.Q, "Q", anchor, startX + 1.5f * pitch, z);
                    AddKey(VKCode.W, "W", anchor, startX + 2.5f * pitch, z);
                    AddKey(VKCode.E, "E", anchor, startX + 3.5f * pitch, z);
                    AddKey(VKCode.R, "R", anchor, startX + 4.5f * pitch, z);
                    AddKey(VKCode.T, "T", anchor, startX + 5.5f * pitch, z);

                    // Row 2: Caps (1.75u), A, S, D, F, G
                    z -= pitch;
                    AddKey(VKCode.CAPITAL, "Caps", anchor, startX + 0.375f * pitch, z, 1.75f, 1f);
                    AddKey(VKCode.A, "A", anchor, startX + 1.75f * pitch, z);
                    AddKey(VKCode.S, "S", anchor, startX + 2.75f * pitch, z);
                    AddKey(VKCode.D, "D", anchor, startX + 3.75f * pitch, z);
                    AddKey(VKCode.F, "F", anchor, startX + 4.75f * pitch, z);
                    AddKey(VKCode.G, "G", anchor, startX + 5.75f * pitch, z);

                    // Row 3: Shift (2.25u), Z, X, C, V, B
                    z -= pitch;
                    AddKey(VKCode.LSHIFT, "Shift", anchor, startX + 0.625f * pitch, z, 2.25f, 1f);
                    AddKey(VKCode.Z, "Z", anchor, startX + 2.25f * pitch, z);
                    AddKey(VKCode.X, "X", anchor, startX + 3.25f * pitch, z);
                    AddKey(VKCode.C, "C", anchor, startX + 4.25f * pitch, z);
                    AddKey(VKCode.V, "V", anchor, startX + 5.25f * pitch, z);
                    AddKey(VKCode.B, "B", anchor, startX + 6.25f * pitch, z);

                    // Bottom Row: Ctrl (1.25u), Win (1.25u), Alt (1.25u), Space (3.5u)
                    z -= pitch;
                    AddKey(VKCode.LCONTROL, "Ctrl", anchor, startX + 0.125f * pitch, z, 1.25f, 1f);
                    AddKey(VKCode.LWIN, "Win", anchor, startX + 1.375f * pitch, z, 1.25f, 1f);
                    AddKey(VKCode.LMENU, "Alt", anchor, startX + 2.625f * pitch, z, 1.25f, 1f);
                    AddKey(VKCode.SPACE, "Space", anchor, startX + 5.0f * pitch, z, 3.5f, 1f);
                    break;
                }

                case VirtualKeyLayoutPreset.GamingWASD: {
                    var anchor = VirtualKeyAnchor.LeftHand;
                    // Compact WASD cluster
                    AddKey(VKCode.KEY_1, "1", anchor, -1 * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_2, "2", anchor, 0, 2 * pitch);
                    AddKey(VKCode.KEY_3, "3", anchor, 1 * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_4, "4", anchor, 2 * pitch, 2 * pitch);

                    AddKey(VKCode.Q, "Q", anchor, -1 * pitch, 1 * pitch);
                    AddKey(VKCode.W, "W", anchor, 0, 1 * pitch);
                    AddKey(VKCode.E, "E", anchor, 1 * pitch, 1 * pitch);
                    AddKey(VKCode.R, "R", anchor, 2 * pitch, 1 * pitch);

                    AddKey(VKCode.A, "A", anchor, -1 * pitch, 0);
                    AddKey(VKCode.S, "S", anchor, 0, 0);
                    AddKey(VKCode.D, "D", anchor, 1 * pitch, 0);
                    AddKey(VKCode.F, "F", anchor, 2 * pitch, 0);

                    AddKey(VKCode.LSHIFT, "Shift", anchor, -1.25f * pitch, -1 * pitch, 1.75f, 1f);
                    AddKey(VKCode.SPACE, "Space", anchor, 0.75f * pitch, -1 * pitch, 3.0f, 1f);
                    break;
                }

                case VirtualKeyLayoutPreset.SplitQWERTY: {
                    // Left Hand
                    var L = VirtualKeyAnchor.LeftHand;
                    AddKey(VKCode.KEY_1, "1", L, -2 * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_2, "2", L, -1 * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_3, "3", L, 0, 2 * pitch);
                    AddKey(VKCode.KEY_4, "4", L, 1 * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_5, "5", L, 2 * pitch, 2 * pitch);

                    AddKey(VKCode.Q, "Q", L, -2 * pitch, 1 * pitch);
                    AddKey(VKCode.W, "W", L, -1 * pitch, 1 * pitch);
                    AddKey(VKCode.E, "E", L, 0, 1 * pitch);
                    AddKey(VKCode.R, "R", L, 1 * pitch, 1 * pitch);
                    AddKey(VKCode.T, "T", L, 2 * pitch, 1 * pitch);

                    AddKey(VKCode.A, "A", L, -2 * pitch, 0);
                    AddKey(VKCode.S, "S", L, -1 * pitch, 0);
                    AddKey(VKCode.D, "D", L, 0, 0);
                    AddKey(VKCode.F, "F", L, 1 * pitch, 0);
                    AddKey(VKCode.G, "G", L, 2 * pitch, 0);

                    AddKey(VKCode.Z, "Z", L, -2 * pitch, -1 * pitch);
                    AddKey(VKCode.X, "X", L, -1 * pitch, -1 * pitch);
                    AddKey(VKCode.C, "C", L, 0, -1 * pitch);
                    AddKey(VKCode.V, "V", L, 1 * pitch, -1 * pitch);
                    AddKey(VKCode.B, "B", L, 2 * pitch, -1 * pitch);

                    AddKey(VKCode.SPACE, "Space", L, 1 * pitch, -2 * pitch, 2.5f, 1f);

                    // Right Hand
                    var R = VirtualKeyAnchor.RightHand;
                    AddKey(VKCode.KEY_6, "6", R, -2 * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_7, "7", R, -1 * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_8, "8", R, 0, 2 * pitch);
                    AddKey(VKCode.KEY_9, "9", R, 1 * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_0, "0", R, 2 * pitch, 2 * pitch);

                    AddKey(VKCode.Y, "Y", R, -2 * pitch, 1 * pitch);
                    AddKey(VKCode.U, "U", R, -1 * pitch, 1 * pitch);
                    AddKey(VKCode.I, "I", R, 0, 1 * pitch);
                    AddKey(VKCode.O, "O", R, 1 * pitch, 1 * pitch);
                    AddKey(VKCode.P, "P", R, 2 * pitch, 1 * pitch);

                    AddKey(VKCode.H, "H", R, -2 * pitch, 0);
                    AddKey(VKCode.J, "J", R, -1 * pitch, 0);
                    AddKey(VKCode.K, "K", R, 0, 0);
                    AddKey(VKCode.L, "L", R, 1 * pitch, 0);
                    AddKey(VKCode.RETURN, "Enter", R, 2.25f * pitch, 0, 1.75f, 1f);

                    AddKey(VKCode.N, "N", R, -2 * pitch, -1 * pitch);
                    AddKey(VKCode.M, "M", R, -1 * pitch, -1 * pitch);
                    AddKey(VKCode.OEM_COMMA, ",", R, 0, -1 * pitch);
                    AddKey(VKCode.OEM_PERIOD, ".", R, 1 * pitch, -1 * pitch);
                    AddKey(VKCode.RSHIFT, "Shift", R, 2.25f * pitch, -1 * pitch, 1.75f, 1f);

                    AddKey(VKCode.SPACE, "Space", R, -1 * pitch, -2 * pitch, 2.5f, 1f);
                    break;
                }

                case VirtualKeyLayoutPreset.Rhythm4Key: {
                    var L = VirtualKeyAnchor.LeftHand;
                    var R = VirtualKeyAnchor.RightHand;
                    AddKey(VKCode.D, "D", L, -0.6f * pitch, 0, 1.2f, 1.3f);
                    AddKey(VKCode.F, "F", L, 0.6f * pitch, 0, 1.2f, 1.3f);

                    AddKey(VKCode.J, "J", R, -0.6f * pitch, 0, 1.2f, 1.3f);
                    AddKey(VKCode.K, "K", R, 0.6f * pitch, 0, 1.2f, 1.3f);
                    break;
                }

                case VirtualKeyLayoutPreset.Rhythm7Key: {
                    var L = VirtualKeyAnchor.LeftHand;
                    var R = VirtualKeyAnchor.RightHand;
                    AddKey(VKCode.S, "S", L, -1.1f * pitch, 0, 1.1f, 1.25f);
                    AddKey(VKCode.D, "D", L, 0, 0, 1.1f, 1.25f);
                    AddKey(VKCode.F, "F", L, 1.1f * pitch, 0, 1.1f, 1.25f);

                    AddKey(VKCode.SPACE, "Space", VirtualKeyAnchor.KeyboardRoot, 0, -0.05f, 3.5f, 1.1f);

                    AddKey(VKCode.J, "J", R, -1.1f * pitch, 0, 1.1f, 1.25f);
                    AddKey(VKCode.K, "K", R, 0, 0, 1.1f, 1.25f);
                    AddKey(VKCode.L, "L", R, 1.1f * pitch, 0, 1.1f, 1.25f);
                    break;
                }

                case VirtualKeyLayoutPreset.LeverlessArcade: {
                    // Left hand directions
                    var L = VirtualKeyAnchor.LeftHand;
                    AddKey(VKCode.A, "Left", L, -1 * pitch, 0, 1.15f, 1.15f);
                    AddKey(VKCode.S, "Down", L, 0, 0, 1.15f, 1.15f);
                    AddKey(VKCode.D, "Right", L, 1 * pitch, 0, 1.15f, 1.15f);
                    AddKey(VKCode.SPACE, "Up (Jump)", L, 0.5f * pitch, -1.2f * pitch, 1.5f, 1.5f);

                    // Right hand 8 attack buttons
                    var R = VirtualKeyAnchor.RightHand;
                    AddKey(VKCode.U, "LP", R, -1.5f * pitch, 0.6f * pitch, 1.15f, 1.15f);
                    AddKey(VKCode.I, "MP", R, -0.5f * pitch, 0.7f * pitch, 1.15f, 1.15f);
                    AddKey(VKCode.O, "HP", R, 0.5f * pitch, 0.6f * pitch, 1.15f, 1.15f);
                    AddKey(VKCode.P, "3P", R, 1.5f * pitch, 0.4f * pitch, 1.15f, 1.15f);

                    AddKey(VKCode.J, "LK", R, -1.5f * pitch, -0.4f * pitch, 1.15f, 1.15f);
                    AddKey(VKCode.K, "MK", R, -0.5f * pitch, -0.3f * pitch, 1.15f, 1.15f);
                    AddKey(VKCode.L, "HK", R, 0.5f * pitch, -0.4f * pitch, 1.15f, 1.15f);
                    AddKey(VKCode.OEM_1, "3K", R, 1.5f * pitch, -0.6f * pitch, 1.15f, 1.15f);
                    break;
                }

                case VirtualKeyLayoutPreset.Numpad: {
                    var R = VirtualKeyAnchor.RightHand;
                    // Row 4: NumLock, /, *, -
                    AddKey(VKCode.NUMLOCK, "Num", R, -1.5f * pitch, 2f * pitch);
                    AddKey(VKCode.DIVIDE, "/", R, -0.5f * pitch, 2f * pitch);
                    AddKey(VKCode.MULTIPLY, "*", R, 0.5f * pitch, 2f * pitch);
                    AddKey(VKCode.SUBTRACT, "-", R, 1.5f * pitch, 2f * pitch);

                    // Row 3: 7, 8, 9, + (2u vertical)
                    AddKey(VKCode.NUMPAD7, "7", R, -1.5f * pitch, 1f * pitch);
                    AddKey(VKCode.NUMPAD8, "8", R, -0.5f * pitch, 1f * pitch);
                    AddKey(VKCode.NUMPAD9, "9", R, 0.5f * pitch, 1f * pitch);
                    AddKey(VKCode.ADD, "+", R, 1.5f * pitch, 0.5f * pitch, 1.0f, 2.0f);

                    // Row 2: 4, 5, 6
                    AddKey(VKCode.NUMPAD4, "4", R, -1.5f * pitch, 0);
                    AddKey(VKCode.NUMPAD5, "5", R, -0.5f * pitch, 0);
                    AddKey(VKCode.NUMPAD6, "6", R, 0.5f * pitch, 0);

                    // Row 1: 1, 2, 3, Enter (2u vertical)
                    AddKey(VKCode.NUMPAD1, "1", R, -1.5f * pitch, -1f * pitch);
                    AddKey(VKCode.NUMPAD2, "2", R, -0.5f * pitch, -1f * pitch);
                    AddKey(VKCode.NUMPAD3, "3", R, 0.5f * pitch, -1f * pitch);
                    AddKey(VKCode.RETURN, "Enter", R, 1.5f * pitch, -1.5f * pitch, 1.0f, 2.0f);

                    // Row 0: 0 (2u horizontal), .
                    AddKey(VKCode.NUMPAD0, "0", R, -1.0f * pitch, -2f * pitch, 2.0f, 1.0f);
                    AddKey(VKCode.DECIMAL, ".", R, 0.5f * pitch, -2f * pitch);
                    break;
                }

                case VirtualKeyLayoutPreset.Standard60Percent: {
                    var anchor = VirtualKeyAnchor.KeyboardRoot;
                    float startX = -6.5f * pitch;
                    float z = 2f * pitch;

                    // Row 4: Number row: Esc, 1-0, Back (2u)
                    AddKey(VKCode.ESCAPE, "Esc", anchor, startX + 0 * pitch, z);
                    AddKey(VKCode.KEY_1, "1", anchor, startX + 1 * pitch, z);
                    AddKey(VKCode.KEY_2, "2", anchor, startX + 2 * pitch, z);
                    AddKey(VKCode.KEY_3, "3", anchor, startX + 3 * pitch, z);
                    AddKey(VKCode.KEY_4, "4", anchor, startX + 4 * pitch, z);
                    AddKey(VKCode.KEY_5, "5", anchor, startX + 5 * pitch, z);
                    AddKey(VKCode.KEY_6, "6", anchor, startX + 6 * pitch, z);
                    AddKey(VKCode.KEY_7, "7", anchor, startX + 7 * pitch, z);
                    AddKey(VKCode.KEY_8, "8", anchor, startX + 8 * pitch, z);
                    AddKey(VKCode.KEY_9, "9", anchor, startX + 9 * pitch, z);
                    AddKey(VKCode.KEY_0, "0", anchor, startX + 10 * pitch, z);
                    AddKey(VKCode.BACK, "Back", anchor, startX + 11.5f * pitch, z, 2.0f, 1f);

                    // Row 3: Tab row: Tab (1.5u), Q-P
                    z -= pitch;
                    AddKey(VKCode.TAB, "Tab", anchor, startX + 0.25f * pitch, z, 1.5f, 1f);
                    AddKey(VKCode.Q, "Q", anchor, startX + 1.5f * pitch, z);
                    AddKey(VKCode.W, "W", anchor, startX + 2.5f * pitch, z);
                    AddKey(VKCode.E, "E", anchor, startX + 3.5f * pitch, z);
                    AddKey(VKCode.R, "R", anchor, startX + 4.5f * pitch, z);
                    AddKey(VKCode.T, "T", anchor, startX + 5.5f * pitch, z);
                    AddKey(VKCode.Y, "Y", anchor, startX + 6.5f * pitch, z);
                    AddKey(VKCode.U, "U", anchor, startX + 7.5f * pitch, z);
                    AddKey(VKCode.I, "I", anchor, startX + 8.5f * pitch, z);
                    AddKey(VKCode.O, "O", anchor, startX + 9.5f * pitch, z);
                    AddKey(VKCode.P, "P", anchor, startX + 10.5f * pitch, z);

                    // Row 2: Home row: Caps (1.75u), A-L, Enter (2.25u)
                    z -= pitch;
                    AddKey(VKCode.CAPITAL, "Caps", anchor, startX + 0.375f * pitch, z, 1.75f, 1f);
                    AddKey(VKCode.A, "A", anchor, startX + 1.75f * pitch, z);
                    AddKey(VKCode.S, "S", anchor, startX + 2.75f * pitch, z);
                    AddKey(VKCode.D, "D", anchor, startX + 3.75f * pitch, z);
                    AddKey(VKCode.F, "F", anchor, startX + 4.75f * pitch, z);
                    AddKey(VKCode.G, "G", anchor, startX + 5.75f * pitch, z);
                    AddKey(VKCode.H, "H", anchor, startX + 6.75f * pitch, z);
                    AddKey(VKCode.J, "J", anchor, startX + 7.75f * pitch, z);
                    AddKey(VKCode.K, "K", anchor, startX + 8.75f * pitch, z);
                    AddKey(VKCode.L, "L", anchor, startX + 9.75f * pitch, z);
                    AddKey(VKCode.RETURN, "Enter", anchor, startX + 11.375f * pitch, z, 2.25f, 1f);

                    // Row 1: Shift row: LShift (2.25u), Z-M, ,, ., RShift (2.5u)
                    z -= pitch;
                    AddKey(VKCode.LSHIFT, "Shift", anchor, startX + 0.625f * pitch, z, 2.25f, 1f);
                    AddKey(VKCode.Z, "Z", anchor, startX + 2.25f * pitch, z);
                    AddKey(VKCode.X, "X", anchor, startX + 3.25f * pitch, z);
                    AddKey(VKCode.C, "C", anchor, startX + 4.25f * pitch, z);
                    AddKey(VKCode.V, "V", anchor, startX + 5.25f * pitch, z);
                    AddKey(VKCode.B, "B", anchor, startX + 6.25f * pitch, z);
                    AddKey(VKCode.N, "N", anchor, startX + 7.25f * pitch, z);
                    AddKey(VKCode.M, "M", anchor, startX + 8.25f * pitch, z);
                    AddKey(VKCode.OEM_COMMA, ",", anchor, startX + 9.25f * pitch, z);
                    AddKey(VKCode.OEM_PERIOD, ".", anchor, startX + 10.25f * pitch, z);
                    AddKey(VKCode.RSHIFT, "Shift", anchor, startX + 11.75f * pitch, z, 2.5f, 1f);

                    // Row 0: Bottom row: Ctrl (1.25u), Win (1.25u), Alt (1.25u), Space (6.25u), Alt (1.25u), Ctrl (1.25u)
                    z -= pitch;
                    AddKey(VKCode.LCONTROL, "Ctrl", anchor, startX + 0.125f * pitch, z, 1.25f, 1f);
                    AddKey(VKCode.LWIN, "Win", anchor, startX + 1.375f * pitch, z, 1.25f, 1f);
                    AddKey(VKCode.LMENU, "Alt", anchor, startX + 2.625f * pitch, z, 1.25f, 1f);
                    AddKey(VKCode.SPACE, "Space", anchor, startX + 6.375f * pitch, z, 6.25f, 1f);
                    AddKey(VKCode.RMENU, "Alt", anchor, startX + 10.125f * pitch, z, 1.25f, 1f);
                    AddKey(VKCode.RCONTROL, "Ctrl", anchor, startX + 11.375f * pitch, z, 1.25f, 1f);
                    break;
                }

                case VirtualKeyLayoutPreset.KeychronQ11: {
                    var L = VirtualKeyAnchor.LeftHand;
                    var R = VirtualKeyAnchor.RightHand;

                    // ===== LEFT HALF (45 keys + 1 knob) =====
                    // Macro Column (far left, X = -4.75 * pitch)
                    AddKey(VKCode.VOLUME_MUTE, "Knob 1", L, -4.75f * pitch, 3 * pitch, 1f, 1f, 0f, true); // Rotary Encoder
                    AddKey(VKCode.F13, "M1", L, -4.75f * pitch, 2 * pitch);
                    AddKey(VKCode.F14, "M2", L, -4.75f * pitch, 1 * pitch);
                    AddKey(VKCode.F15, "M3", L, -4.75f * pitch, 0f);
                    AddKey(VKCode.F16, "M4", L, -4.75f * pitch, -1 * pitch);
                    AddKey(VKCode.F17, "M5", L, -4.75f * pitch, -2 * pitch);

                    // Row 3 (Function Row)
                    AddKey(VKCode.ESCAPE, "Esc", L, -3.5f * pitch, 3 * pitch);
                    AddKey(VKCode.F1, "F1", L, -2.5f * pitch, 3 * pitch);
                    AddKey(VKCode.F2, "F2", L, -1.5f * pitch, 3 * pitch);
                    AddKey(VKCode.F3, "F3", L, -0.5f * pitch, 3 * pitch);
                    AddKey(VKCode.F4, "F4", L, 0.5f * pitch, 3 * pitch);
                    AddKey(VKCode.F5, "F5", L, 1.5f * pitch, 3 * pitch);
                    AddKey(VKCode.F6, "F6", L, 2.5f * pitch, 3 * pitch);

                    // Row 2 (Number Row)
                    AddKey(VKCode.OEM_3, "`", L, -3.5f * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_1, "1", L, -2.5f * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_2, "2", L, -1.5f * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_3, "3", L, -0.5f * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_4, "4", L, 0.5f * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_5, "5", L, 1.5f * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_6, "6", L, 2.5f * pitch, 2 * pitch);

                    // Row 1 (Q Row)
                    AddKey(VKCode.TAB, "Tab", L, -3.25f * pitch, 1 * pitch, 1.5f, 1f);
                    AddKey(VKCode.Q, "Q", L, -2.0f * pitch, 1 * pitch);
                    AddKey(VKCode.W, "W", L, -1.0f * pitch, 1 * pitch);
                    AddKey(VKCode.E, "E", L, 0.0f * pitch, 1 * pitch);
                    AddKey(VKCode.R, "R", L, 1.0f * pitch, 1 * pitch);
                    AddKey(VKCode.T, "T", L, 2.0f * pitch, 1 * pitch);

                    // Row 0 (Home Row)
                    AddKey(VKCode.CAPITAL, "Caps", L, -3.375f * pitch, 0f, 1.75f, 1f);
                    AddKey(VKCode.A, "A", L, -2.0f * pitch, 0f);
                    AddKey(VKCode.S, "S", L, -1.0f * pitch, 0f);
                    AddKey(VKCode.D, "D", L, 0.0f * pitch, 0f);
                    AddKey(VKCode.F, "F", L, 1.0f * pitch, 0f);
                    AddKey(VKCode.G, "G", L, 2.0f * pitch, 0f);

                    // Row -1 (Z Row)
                    AddKey(VKCode.LSHIFT, "Shift", L, -3.125f * pitch, -1 * pitch, 2.25f, 1f);
                    AddKey(VKCode.Z, "Z", L, -1.5f * pitch, -1 * pitch);
                    AddKey(VKCode.X, "X", L, -0.5f * pitch, -1 * pitch);
                    AddKey(VKCode.C, "C", L, 0.5f * pitch, -1 * pitch);
                    AddKey(VKCode.V, "V", L, 1.5f * pitch, -1 * pitch);
                    AddKey(VKCode.B, "B", L, 2.5f * pitch, -1 * pitch);

                    // Row -2 (Bottom Row)
                    AddKey(VKCode.LCONTROL, "Ctrl", L, -3.625f * pitch, -2 * pitch, 1.25f, 1f);
                    AddKey(VKCode.LWIN, "Win", L, -2.375f * pitch, -2 * pitch, 1.25f, 1f);
                    AddKey(VKCode.LMENU, "Alt", L, -1.125f * pitch, -2 * pitch, 1.25f, 1f);
                    AddKey(VKCode.SPACE, "Space", L, 0.875f * pitch, -2 * pitch, 2.25f, 1f);

                    // ===== RIGHT HALF (44 keys + 1 knob) =====
                    // Row 3 (Function Row)
                    AddKey(VKCode.F7, "F7", R, -2.5f * pitch, 3 * pitch);
                    AddKey(VKCode.F8, "F8", R, -1.5f * pitch, 3 * pitch);
                    AddKey(VKCode.F9, "F9", R, -0.5f * pitch, 3 * pitch);
                    AddKey(VKCode.F10, "F10", R, 0.5f * pitch, 3 * pitch);
                    AddKey(VKCode.F11, "F11", R, 1.5f * pitch, 3 * pitch);
                    AddKey(VKCode.F12, "F12", R, 2.5f * pitch, 3 * pitch);
                    AddKey(VKCode.SNAPSHOT, "PrtSc", R, 4.0f * pitch, 3 * pitch);
                    AddKey(VKCode.MEDIA_PLAY_PAUSE, "Knob 2", R, 6.0f * pitch, 3 * pitch, 1f, 1f, 0f, true); // Rotary Encoder

                    // Row 2 (Number Row)
                    AddKey(VKCode.KEY_7, "7", R, -2.5f * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_8, "8", R, -1.5f * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_9, "9", R, -0.5f * pitch, 2 * pitch);
                    AddKey(VKCode.KEY_0, "0", R, 0.5f * pitch, 2 * pitch);
                    AddKey(VKCode.OEM_MINUS, "-", R, 1.5f * pitch, 2 * pitch);
                    AddKey(VKCode.OEM_PLUS, "=", R, 2.5f * pitch, 2 * pitch);
                    AddKey(VKCode.BACK, "Backspace", R, 4.0f * pitch, 2 * pitch, 2f, 1f);
                    AddKey(VKCode.DELETE, "Del", R, 6.0f * pitch, 2 * pitch);

                    // Row 1 (Q Row)
                    AddKey(VKCode.Y, "Y", R, -2.0f * pitch, 1 * pitch);
                    AddKey(VKCode.U, "U", R, -1.0f * pitch, 1 * pitch);
                    AddKey(VKCode.I, "I", R, 0.0f * pitch, 1 * pitch);
                    AddKey(VKCode.O, "O", R, 1.0f * pitch, 1 * pitch);
                    AddKey(VKCode.P, "P", R, 2.0f * pitch, 1 * pitch);
                    AddKey(VKCode.OEM_4, "[", R, 3.0f * pitch, 1 * pitch);
                    AddKey(VKCode.OEM_6, "]", R, 4.0f * pitch, 1 * pitch);
                    AddKey(VKCode.OEM_5, "\\", R, 5.25f * pitch, 1 * pitch, 1.5f, 1f);
                    AddKey(VKCode.PRIOR, "PgUp", R, 6.0f * pitch, 1 * pitch);

                    // Row 0 (Home Row)
                    AddKey(VKCode.H, "H", R, -2.0f * pitch, 0f);
                    AddKey(VKCode.J, "J", R, -1.0f * pitch, 0f);
                    AddKey(VKCode.K, "K", R, 0.0f * pitch, 0f);
                    AddKey(VKCode.L, "L", R, 1.0f * pitch, 0f);
                    AddKey(VKCode.OEM_1, ";", R, 2.0f * pitch, 0f);
                    AddKey(VKCode.OEM_7, "'", R, 3.0f * pitch, 0f);
                    AddKey(VKCode.RETURN, "Enter", R, 4.625f * pitch, 0f, 2.25f, 1f);
                    AddKey(VKCode.NEXT, "PgDn", R, 6.0f * pitch, 0f);

                    // Row -1 (Z Row)
                    AddKey(VKCode.N, "N", R, -1.5f * pitch, -1 * pitch);
                    AddKey(VKCode.M, "M", R, -0.5f * pitch, -1 * pitch);
                    AddKey(VKCode.OEM_COMMA, ",", R, 0.5f * pitch, -1 * pitch);
                    AddKey(VKCode.OEM_PERIOD, ".", R, 1.5f * pitch, -1 * pitch);
                    AddKey(VKCode.OEM_2, "/", R, 2.5f * pitch, -1 * pitch);
                    AddKey(VKCode.RSHIFT, "Shift", R, 3.875f * pitch, -1 * pitch, 1.75f, 1f);
                    AddKey(VKCode.UP, "Up", R, 5.0f * pitch, -1 * pitch);
                    AddKey(VKCode.END, "End", R, 6.0f * pitch, -1 * pitch);

                    // Row -2 (Bottom Row)
                    AddKey(VKCode.SPACE, "Space", R, -1.375f * pitch, -2 * pitch, 2.75f, 1f);
                    AddKey(VKCode.RMENU, "Alt", R, 0.625f * pitch, -2 * pitch);
                    AddKey(VKCode.RWIN, "Fn", R, 1.625f * pitch, -2 * pitch);
                    AddKey(VKCode.RCONTROL, "Ctrl", R, 2.625f * pitch, -2 * pitch);
                    AddKey(VKCode.LEFT, "Left", R, 4.0f * pitch, -2 * pitch);
                    AddKey(VKCode.DOWN, "Down", R, 5.0f * pitch, -2 * pitch);
                    AddKey(VKCode.RIGHT, "Right", R, 6.0f * pitch, -2 * pitch);
                    break;
                }
            }

            DestroyAllVirtualKeyVisuals();
            VirtualKeys = list.ToArray();
            VirtualKeysCollection?.BroadcastDataInput(nameof(VirtualKeysCollection.Keys));
            BroadcastDataInput(nameof(VirtualKeysCollection));

            if (EnableFloatingKeys && IsEnabled) {
                UpdateFloatingKeysState();
            }

            if (showToast) {
                Context.Service?.Toast(
                    Warudo.Core.Server.ToastSeverity.Success,
                    Name,
                    $"Applied virtual keys layout preset: {preset}"
                );
            }
        }
    }
}
