using System;
using System.Collections.Generic;
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

        [DataInput(510)]
        [Label("KEYS_LAYOUT_PRESET")]
        public VirtualKeyLayoutPreset KeysLayoutPreset = VirtualKeyLayoutPreset.GamingWASDExtended;

        [Trigger(520)]
        [Label("APPLY_KEYS_LAYOUT_PRESET")]
        public void TriggerApplyKeysLayoutPreset() {
            ApplyVirtualKeyPreset(KeysLayoutPreset);
        }

        // Keyset Group Locations & Scale
        [DataInput(530)]
        [Label("LEFT_KEYSET_TRANSFORM")]
        public VisualSetupTransform LeftKeysetTransform;

        [DataInput(540)]
        [Label("RIGHT_KEYSET_TRANSFORM")]
        public VisualSetupTransform RightKeysetTransform;

        [DataInput(550)]
        [Label("ROOT_KEYSET_TRANSFORM")]
        public VisualSetupTransform RootKeysetTransform;

        // Base Look & Feel
        [DataInput(560)]
        [Label("DEFAULT_KEY_SIZE")]
        public Vector2 DefaultKeySize = new Vector2(0.032f, 0.032f);

        [DataInput(565)]
        [FloatSlider(0f, 0.5f, 0.01f)]
        [Label("DEFAULT_CORNER_ROUNDNESS")]
        public float DefaultCornerRadius = 0.28f;

        [DataInput(568)]
        [FloatSlider(0.0005f, 0.01f, 0.0005f)]
        [Label("DEFAULT_BORDER_WIDTH")]
        public float DefaultBorderWidth = 0.0025f;

        [DataInput(570)]
        [Label("DEFAULT_KEY_APPEARANCE")]
        public VirtualKeyAppearance DefaultAppearance;

        [DataInput(610)]
        [Label("VIRTUAL_KEYS")]
        public VirtualKeyDefinition[] VirtualKeys = new VirtualKeyDefinition[0];

        // Runtime objects
        public VirtualKeyDefinition ActiveVirtualKeyInVisualSetup;

        GameObject leftKeysGroupGo;
        GameObject rightKeysGroupGo;
        GameObject rootKeysGroupGo;

        void OnCreateVirtualKeys() {
            SetupKeysetTransforms();

            if (DefaultAppearance == null) {
                DefaultAppearance = VirtualKeyAppearance.CreateDefault();
            }
            DefaultAppearance.OnAppearanceChanged = () => {
                UpdateAllVirtualKeyColors();
            };

            Watch(nameof(EnableFloatingKeys), delegate {
                UpdateFloatingKeysState();
            });
            Watch(nameof(IsEnabled), delegate {
                UpdateFloatingKeysState();
            });
            Watch(nameof(KeysLayoutPreset), delegate { });
            Watch(nameof(VirtualKeys), delegate {
                RefreshVirtualKeysParent();
                RebuildAllVirtualKeyMeshes();
            });

            Watch(nameof(LeftKeysetTransform), delegate { SetupLeftKeysetTransform(); });
            Watch(nameof(RightKeysetTransform), delegate { SetupRightKeysetTransform(); });
            Watch(nameof(RootKeysetTransform), delegate { SetupRootKeysetTransform(); });

            Watch(nameof(DefaultKeySize), delegate { RebuildAllVirtualKeyMeshes(); });
            Watch(nameof(DefaultCornerRadius), delegate { RebuildAllVirtualKeyMeshes(); });
            Watch(nameof(DefaultBorderWidth), delegate { RebuildAllVirtualKeyMeshes(); });
            Watch(nameof(DefaultAppearance), delegate {
                if (DefaultAppearance != null) {
                    DefaultAppearance.OnAppearanceChanged = () => { UpdateAllVirtualKeyColors(); };
                }
                UpdateAllVirtualKeyColors();
            });

            RefreshVirtualKeysParent();
            if (VirtualKeys == null || VirtualKeys.Length == 0) {
                ApplyVirtualKeyPreset(KeysLayoutPreset, false);
            }

            if (EnableFloatingKeys && IsEnabled) {
                UpdateFloatingKeysState();
            }
        }

        void OnDestroyVirtualKeys() {
            ActiveVirtualKeyInVisualSetup = null;
            DestroyAllVirtualKeyVisuals();
        }

        void SetupKeysetTransforms() {
            SetupLeftKeysetTransform();
            SetupRightKeysetTransform();
            SetupRootKeysetTransform();
        }

        void SetupLeftKeysetTransform() {
            if (LeftKeysetTransform == null) {
                LeftKeysetTransform = StructuredData.Create<VisualSetupTransform>();
                LeftKeysetTransform.Position = Vector3.zero;
                LeftKeysetTransform.Rotation = Vector3.zero;
                LeftKeysetTransform.Scale = Vector3.one;
            }
            LeftKeysetTransform.LocalizationKey = "LEFT_KEYSET_CLUSTER";
            LeftKeysetTransform.OnEnterVisualSetup = (tr) => { EnsureAnchors(); };
            LeftKeysetTransform.OnCreateAnchor = (tr, setupAnchor) => {
                var parent = LeftHandAnchor;
                if (parent != null) {
                    setupAnchor.Attachable.Parent = parent;
                    if (parent.GameObject != null) {
                        setupAnchor.GameObject.transform.SetParent(parent.GameObject.transform, false);
                    }
                }
                setupAnchor.Transform.Position = tr.Position;
                setupAnchor.Transform.Rotation = tr.Rotation;
                setupAnchor.Transform.Scale = tr.Scale;
                setupAnchor.Name = "⌛⚓-Left Keyset Cluster";
                setupAnchor.Instructions = "Position, rotate, and scale the entire **Left Hand Keyset** cluster.\nClick **[Apply Transform]** when finished.";
                setupAnchor.Transform.Broadcast();
            };
            LeftKeysetTransform.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                tr.Scale = setupAnchor.Transform.Scale;
                UpdateGroupTransforms();
            };
            LeftKeysetTransform.OnExitVisualSetup = (isApplied) => {
                UpdateGroupTransforms();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };
        }

        void SetupRightKeysetTransform() {
            if (RightKeysetTransform == null) {
                RightKeysetTransform = StructuredData.Create<VisualSetupTransform>();
                RightKeysetTransform.Position = Vector3.zero;
                RightKeysetTransform.Rotation = Vector3.zero;
                RightKeysetTransform.Scale = Vector3.one;
            }
            RightKeysetTransform.LocalizationKey = "RIGHT_KEYSET_CLUSTER";
            RightKeysetTransform.OnEnterVisualSetup = (tr) => { EnsureAnchors(); };
            RightKeysetTransform.OnCreateAnchor = (tr, setupAnchor) => {
                var parent = RightHandAnchor;
                if (parent != null) {
                    setupAnchor.Attachable.Parent = parent;
                    if (parent.GameObject != null) {
                        setupAnchor.GameObject.transform.SetParent(parent.GameObject.transform, false);
                    }
                }
                setupAnchor.Transform.Position = tr.Position;
                setupAnchor.Transform.Rotation = tr.Rotation;
                setupAnchor.Transform.Scale = tr.Scale;
                setupAnchor.Name = "⌛⚓-Right Keyset Cluster";
                setupAnchor.Instructions = "Position, rotate, and scale the entire **Right Hand Keyset** cluster.\nClick **[Apply Transform]** when finished.";
                setupAnchor.Transform.Broadcast();
            };
            RightKeysetTransform.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                tr.Scale = setupAnchor.Transform.Scale;
                UpdateGroupTransforms();
            };
            RightKeysetTransform.OnExitVisualSetup = (isApplied) => {
                UpdateGroupTransforms();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };
        }

        void SetupRootKeysetTransform() {
            if (RootKeysetTransform == null) {
                RootKeysetTransform = StructuredData.Create<VisualSetupTransform>();
                RootKeysetTransform.Position = Vector3.zero;
                RootKeysetTransform.Rotation = Vector3.zero;
                RootKeysetTransform.Scale = Vector3.one;
            }
            RootKeysetTransform.LocalizationKey = "ROOT_KEYSET_CLUSTER";
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
                setupAnchor.Name = "⌛⚓-Root Keyset Cluster";
                setupAnchor.Instructions = "Position, rotate, and scale the entire **Root Keyset** cluster.\nClick **[Apply Transform]** when finished.";
                setupAnchor.Transform.Broadcast();
            };
            RootKeysetTransform.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                tr.Scale = setupAnchor.Transform.Scale;
                UpdateGroupTransforms();
            };
            RootKeysetTransform.OnExitVisualSetup = (isApplied) => {
                UpdateGroupTransforms();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };
        }

        public void RefreshVirtualKeysParent() {
            if (VirtualKeys == null) return;
            for (int i = 0; i < VirtualKeys.Length; i++) {
                if (VirtualKeys[i] != null) {
                    VirtualKeys[i].Parent = this;
                    VirtualKeys[i].SetupTransformCallbacks();
                }
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

        GameObject GetGroupGameObject(VirtualKeyAnchor anchorType) {
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
                var anchor = LeftHandAnchor;
                if (anchor != null && anchor.GameObject != null) {
                    if (leftKeysGroupGo.transform.parent != anchor.GameObject.transform) {
                        leftKeysGroupGo.transform.SetParent(anchor.GameObject.transform, false);
                    }
                }
                var pos = LeftKeysetTransform?.Position ?? Vector3.zero;
                var rot = LeftKeysetTransform?.Rotation ?? Vector3.zero;
                var scale = LeftKeysetTransform?.Scale ?? Vector3.one;
                if (LeftKeysetTransform != null && LeftKeysetTransform.InVisualSetupMode && LeftKeysetTransform.setupAnchor != null) {
                    pos = LeftKeysetTransform.setupAnchor.Transform.Position;
                    rot = LeftKeysetTransform.setupAnchor.Transform.Rotation;
                    scale = LeftKeysetTransform.setupAnchor.Transform.Scale;
                }
                leftKeysGroupGo.transform.localPosition = pos;
                leftKeysGroupGo.transform.localRotation = Quaternion.Euler(rot);
                leftKeysGroupGo.transform.localScale = scale;
            }

            // 2. Right Group
            if (rightKeysGroupGo != null) {
                var anchor = RightHandAnchor;
                if (anchor != null && anchor.GameObject != null) {
                    if (rightKeysGroupGo.transform.parent != anchor.GameObject.transform) {
                        rightKeysGroupGo.transform.SetParent(anchor.GameObject.transform, false);
                    }
                }
                var pos = RightKeysetTransform?.Position ?? Vector3.zero;
                var rot = RightKeysetTransform?.Rotation ?? Vector3.zero;
                var scale = RightKeysetTransform?.Scale ?? Vector3.one;
                if (RightKeysetTransform != null && RightKeysetTransform.InVisualSetupMode && RightKeysetTransform.setupAnchor != null) {
                    pos = RightKeysetTransform.setupAnchor.Transform.Position;
                    rot = RightKeysetTransform.setupAnchor.Transform.Rotation;
                    scale = RightKeysetTransform.setupAnchor.Transform.Scale;
                }
                rightKeysGroupGo.transform.localPosition = pos;
                rightKeysGroupGo.transform.localRotation = Quaternion.Euler(rot);
                rightKeysGroupGo.transform.localScale = scale;
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

                if (keyDef.IdentifyGlowTimer > 0f) {
                    keyDef.IdentifyGlowTimer = Mathf.Max(0f, keyDef.IdentifyGlowTimer - dt);
                }

                ApplyKeyGlowState(keyDef, isDown, dt);
            }
        }

        void ApplyKeyGlowState(VirtualKeyDefinition keyDef, bool isDown, float dt = 0f) {
            if (keyDef.BorderMaterial == null || keyDef.BodyMaterial == null) return;

            var customApp = (keyDef.UseCustomAppearance && keyDef.CustomAppearance != null) ? keyDef.CustomAppearance : null;
            var baseApp = DefaultAppearance;

            // Resolve Body Normal style
            var bodyNormal = (customApp?.Body?.Normal != null && customApp.Body.Normal.Enabled)
                ? customApp.Body.Normal
                : baseApp?.Body?.Normal;

            // Resolve Body Pressed style
            bool customHasPressed = (customApp?.Body != null && customApp.Body.EnablePressedState && customApp.Body.Pressed != null && customApp.Body.Pressed.Enabled);
            bool baseHasPressed = (baseApp?.Body != null && baseApp.Body.EnablePressedState && baseApp.Body.Pressed != null && baseApp.Body.Pressed.Enabled);

            VirtualKeyStateStyle bodyPressed = null;
            if (customHasPressed) {
                bodyPressed = customApp.Body.Pressed;
            } else if (customApp != null && customApp.Body != null && !customApp.Body.EnablePressedState) {
                bodyPressed = bodyNormal;
            } else if (baseHasPressed) {
                bodyPressed = baseApp.Body.Pressed;
            } else {
                bodyPressed = bodyNormal;
            }

            // Resolve Border Normal style
            var borderNormal = (customApp?.Border?.Normal != null && customApp.Border.Normal.Enabled)
                ? customApp.Border.Normal
                : baseApp?.Border?.Normal;

            // Resolve Border Pressed style
            bool customBorderHasPressed = (customApp?.Border != null && customApp.Border.EnablePressedState && customApp.Border.Pressed != null && customApp.Border.Pressed.Enabled);
            bool baseBorderHasPressed = (baseApp?.Border != null && baseApp.Border.EnablePressedState && baseApp.Border.Pressed != null && baseApp.Border.Pressed.Enabled);

            VirtualKeyStateStyle borderPressed = null;
            if (customBorderHasPressed) {
                borderPressed = customApp.Border.Pressed;
            } else if (customApp != null && customApp.Border != null && !customApp.Border.EnablePressedState) {
                borderPressed = borderNormal;
            } else if (baseBorderHasPressed) {
                borderPressed = baseApp.Border.Pressed;
            } else {
                borderPressed = borderNormal;
            }

            // Resolve transition duration
            float transDuration = (customApp != null && customApp.TransitionDuration >= 0f)
                ? customApp.TransitionDuration
                : (baseApp?.TransitionDuration ?? 0.05f);

            // Progress interpolation
            float factor = 0f;
            if (keyDef.IdentifyGlowTimer > 0f) {
                factor = Mathf.PingPong(Time.time * 6f, 1f);
            } else {
                float target = isDown ? 1.0f : 0.0f;
                if (transDuration <= 0.001f || dt <= 0f) {
                    keyDef.CurrentPressProgress = target;
                } else {
                    keyDef.CurrentPressProgress = Mathf.MoveTowards(keyDef.CurrentPressProgress, target, dt / transDuration);
                }
                factor = keyDef.CurrentPressProgress;
            }

            // Calculate animated dimensions
            Vector2 size = (keyDef.UseCustomSize && keyDef.Size.x > 0f && keyDef.Size.y > 0f) ? keyDef.Size : DefaultKeySize;

            float normalBorderWidth = keyDef.UseCustomSize
                ? keyDef.BorderWidth
                : (borderNormal != null ? borderNormal.BorderWidth : DefaultBorderWidth);
            float pressedBorderWidth = keyDef.UseCustomSize
                ? keyDef.PressedBorderWidth
                : (borderPressed != null ? borderPressed.BorderWidth : normalBorderWidth);
            float currentBorderWidth = Mathf.Lerp(normalBorderWidth, pressedBorderWidth, factor);

            float normalRadius = keyDef.UseCustomSize
                ? keyDef.CornerRadius
                : (customApp?.CornerRadius ?? baseApp?.CornerRadius ?? DefaultCornerRadius);
            float pressedRadius = keyDef.UseCustomSize
                ? keyDef.PressedCornerRadius
                : (customApp?.PressedCornerRadius ?? baseApp?.PressedCornerRadius ?? normalRadius);
            float currentRadius = Mathf.Lerp(normalRadius, pressedRadius, factor);

            float maxRadius = Mathf.Min(size.x, size.y) * 0.5f;
            float absoluteRadius = currentRadius * maxRadius;

            // In-place mesh vertex morphing during press transitions
            if (Mathf.Abs(keyDef.CurrentBorderWidth - currentBorderWidth) > 0.00001f || Mathf.Abs(keyDef.CurrentCornerRadius - currentRadius) > 0.0001f) {
                keyDef.CurrentBorderWidth = currentBorderWidth;
                keyDef.CurrentCornerRadius = currentRadius;

                if (keyDef.BodyMesh != null) {
                    VirtualKeyMeshBuilder.UpdateRoundedRectVertices(keyDef.BodyMesh, size.x, size.y, absoluteRadius);
                }
                if (keyDef.FrameMesh != null) {
                    VirtualKeyMeshBuilder.UpdateRoundedRectFrameVertices(keyDef.FrameMesh, size.x, size.y, absoluteRadius, currentBorderWidth);
                }
            }

            // Apply Body
            if (bodyNormal != null) {
                var targetPressed = bodyPressed ?? bodyNormal;
                Color bodyColor = Color.Lerp(bodyNormal.Color, targetPressed.Color, factor);
                float bodySelfIllum = Mathf.Clamp01(Mathf.Lerp(bodyNormal.SelfIllumination, targetPressed.SelfIllumination, factor));
                Color bodyGlowColor = Color.Lerp(bodyNormal.GlowColor, targetPressed.GlowColor, factor);
                float bodyGlowIntensity = Mathf.Lerp(bodyNormal.GlowIntensity, targetPressed.GlowIntensity, factor);

                // When SelfIllumination = 1, diffuse albedo is dimmed to 0 so scene lighting/shadows cannot darken or tint it.
                // The base color is self-emitted, with any additional glow/bloom added on top.
                Color albedo = new Color(bodyColor.r * (1f - bodySelfIllum), bodyColor.g * (1f - bodySelfIllum), bodyColor.b * (1f - bodySelfIllum), bodyColor.a);
                Color emission = (bodyColor * bodySelfIllum) + (bodyGlowColor * bodyGlowIntensity);

                keyDef.BodyMaterial.color = albedo;
                keyDef.BodyMaterial.SetColor("_EmissionColor", emission);
            }

            // Apply Border
            if (borderNormal != null) {
                var targetPressed = borderPressed ?? borderNormal;
                Color borderColor = Color.Lerp(borderNormal.Color, targetPressed.Color, factor);
                float borderSelfIllum = Mathf.Clamp01(Mathf.Lerp(borderNormal.SelfIllumination, targetPressed.SelfIllumination, factor));
                Color borderGlowColor = Color.Lerp(borderNormal.GlowColor, targetPressed.GlowColor, factor);
                float borderGlowIntensity = Mathf.Lerp(borderNormal.GlowIntensity, targetPressed.GlowIntensity, factor);

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
            Vector2 size = (keyDef.UseCustomSize && keyDef.Size.x > 0f && keyDef.Size.y > 0f) ? keyDef.Size : DefaultKeySize;
            float cornerRadius = keyDef.UseCustomSize ? keyDef.CornerRadius : DefaultCornerRadius;
            float borderWidth = keyDef.UseCustomSize ? keyDef.BorderWidth : DefaultBorderWidth;

            float maxRadius = Mathf.Min(size.x, size.y) * 0.5f;
            float absoluteRadius = cornerRadius * maxRadius;

            // 1. Inner Body Mesh & Material
            var bodyMesh = VirtualKeyMeshBuilder.CreateRoundedRectMesh(size.x, size.y, absoluteRadius);
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
            var frameMesh = VirtualKeyMeshBuilder.CreateRoundedRectFrameMesh(size.x, size.y, absoluteRadius, borderWidth);
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
            var list = new List<VirtualKeyDefinition>();

            float pitch = 0.036f; // Standard key spacing (32mm key + 4mm gap)

            void AddKey(VKCode key, string label, VirtualKeyAnchor anchor, float posX, float posZ, float unitWidth = 1f, float unitHeight = 1f, float rotY = 0f) {
                var def = StructuredData.Create<VirtualKeyDefinition>(d => {
                    d.Parent = this;
                    d.Key = key;
                    d.DisplayLabel = label;
                    d.AttachmentAnchor = anchor;
                    d.Transform.Position = new Vector3(posX, 0f, posZ);
                    d.Transform.Rotation = new Vector3(0f, rotY, 0f);
                    d.Transform.Scale = Vector3.one;
                    if (unitWidth != 1f || unitHeight != 1f) {
                        d.UseCustomSize = true;
                        float w = (unitWidth * pitch) - (pitch - DefaultKeySize.x);
                        float h = (unitHeight * pitch) - (pitch - DefaultKeySize.y);
                        d.Size = new Vector2(w, h);
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
            }

            DestroyAllVirtualKeyVisuals();
            VirtualKeys = list.ToArray();
            BroadcastDataInput(nameof(VirtualKeys));

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
