using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Serializations;
using Cysharp.Threading.Tasks;

namespace FlameStream {
    public class KeyboardKeyBinding : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData, IKeyListeningTarget {

        [DataInput(10)]
        [Label("KEY")]
        public VKCode Key;

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
                port.Properties.label = listening ? "IDENTIFYING_KEY".Localized() : "IDENTIFY_KEY".Localized();
                BroadcastTriggerProperties(nameof(TriggerIdentifyKey));
                RequestUiRefresh();
            }
        }

        public void OnKeyCaptured(VKCode newKey) {
            Key = newKey;
            SetDataInput(nameof(Key), newKey, true);
            BroadcastDataInput(nameof(Key));
            BroadcastDataInput(nameof(Transform));
        }

        [DataInput(20)]
        [Label("ASSIGNED_FINGER")]
        public KeyboardFinger Finger;

        [DataInput(25)]
        [Label("ENABLE_HAND_MOVEMENT")]
        public bool EnableHandMovement = true;

        [DataInput(30)]
        [Label("TRANSFORM")]
        [Description("KEYBOARD_KEY_BINDING_TRANSFORM_DESC")]
        public KeyBindingVisualSetupTransform Transform;

        [Trigger(50)]
        [Label("DEBUG_FORCE_KEY")]
        public void TriggerDebugForceKey() {
            Parent?.ToggleDebugKey(Key, Finger);
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

        public Vector3 DeltaPosition {
            get => Transform != null ? Transform.Position : deltaPosition;
            set {
                deltaPosition = value;
                if (Transform != null) {
                    Transform.Position = value;
                }
            }
        }
        private Vector3 deltaPosition;

        public Vector3 DeltaRotation {
            get => Transform != null ? Transform.Rotation : deltaRotation;
            set {
                deltaRotation = value;
                if (Transform != null) {
                    Transform.Rotation = value;
                }
            }
        }
        private Vector3 deltaRotation;

        // Visual alignment guide objects during key setup
        GameObject debugKeyCenterSphere;
        GameObject debugFingerTipSphere;
        GameObject debugHandPlaneQuad;
        GameObject debugKeyPlaneQuad;
        GameObject debugElbowLine;
        GameObject debugHandLine;
        Material debugKeyCenterSphereMat;
        Material debugFingerTipSphereMat;
        Material debugHandPlaneMat;
        Material debugKeyPlaneMat;
        Material debugElbowLineMat;
        Material debugHandLineMat;
        readonly List<Material> debugMaterials = new List<Material>();
        Vector3 recordedElbowPosWorld = Vector3.zero;
        bool hasRecordedElbow = false;
        bool hasRecordedKey = false;

        public bool VisualGuidesVisible = true;
        public bool VisualGuidesAlwaysOnTop = true;

        public string GetHeader() {
            var handSide = Finger.IsLeftHand() ? "L" : "R";
            var handMovementTag = EnableHandMovement ? "" : " (No Hand Move)";
            var pos = Transform != null ? Transform.Position : DeltaPosition;
            var listeningTag = IsListeningForKey ? " ⏳[LISTENING]" : "";
            var debugTag = IsDebugForced ? " ⚡[DEBUG]" : "";
            return $"[{Key}] ➔ [{handSide}] {Finger} ΔPos({pos.x:F3}, {pos.y:F3}, {pos.z:F3}){handMovementTag}{listeningTag}{debugTag}";
        }

        public void SyncUI() {
            try {
                var port = GetDataInputPort(nameof(Transform));
                if (port != null && port.Properties != null) {
                    port.Properties.hidden = !EnableHandMovement;
                    BroadcastDataInputProperties(nameof(Transform));
                }
            } catch { }
        }

        protected override void OnCreate() {
            base.OnCreate();
            if (Transform == null) {
                Transform = StructuredData.Create<KeyBindingVisualSetupTransform>();
                Transform.Position = deltaPosition;
                Transform.Rotation = deltaRotation;
            }
            Transform.KeyBinding = this;
            Transform.HideScale();
            SetupTransformCallbacks();
            SyncUI();
            UpdateDebugButtonState();

            Watch(nameof(Transform), delegate {
                if (Transform != null) Transform.KeyBinding = this;
                SetupTransformCallbacks();
            });
            Watch(nameof(Key), delegate {
                UpdateDebugButtonState();
                BroadcastDataInput(nameof(Transform));
            });
            Watch(nameof(Finger), delegate {
                BroadcastDataInput(nameof(Transform));
            });
            Watch(nameof(EnableHandMovement), delegate {
                SyncUI();
                BroadcastDataInput(nameof(Transform));
            });
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            if (Parent != null && Parent.ActiveKeyListeningTarget == (IKeyListeningTarget)this) {
                Parent.CancelKeyListening();
            }
            DestroyDebugVisuals();
            Transform?.DestroyAnchor();
        }

        public void SetupTransformCallbacks() {
            if (Transform == null) return;
            Transform.KeyBinding = this;
            Transform.HideScale();
            Transform.ShowOriginIndicator = false;
            Transform.OriginAlwaysOnTop = VisualGuidesAlwaysOnTop;
            Transform.LocalizationKey = "KEYBOARD_KEY_BINDING";

            Transform.OnEnterVisualSetup = (tr) => {
                if (Parent != null) {
                    Parent.ActiveKeyBindingInVisualSetup = this;
                    Parent.EnsureAnchors();
                }
            };

            Transform.OnCreateAnchor = (tr, setupAnchor) => {
                if (Parent == null) return;

                bool isLeft = Finger.IsLeftHand();
                var root = Parent.GetRootAnchor(false);
                if (root != null) {
                    setupAnchor.Attachable.Parent = root;
                    if (root.GameObject != null) {
                        setupAnchor.GameObject.transform.SetParent(root.GameObject.transform, false);
                    }
                }

                Vector3 restPos = isLeft ? Parent.LeftHandRestPosition : Parent.RightHandRestPosition;
                Vector3 restRot = isLeft ? Parent.LeftHandRestRotation : Parent.RightHandRestRotation;

                Vector3 handScale = isLeft ? Parent.GetLeftHandMovementScale() : Parent.GetRightHandMovementScale();
                Vector3 deltaHand = Vector3.Scale(tr.Position, handScale);
                Vector3 initialPos = restPos + deltaHand;
                Vector3 initialRot = restRot + tr.Rotation;

                setupAnchor.Transform.Position = initialPos;
                setupAnchor.Transform.Rotation = initialRot;
                setupAnchor.Name = $"⌛⚓-Key [{Key}] ({Finger})";
                setupAnchor.Instructions = $@"
### Key Binding Visual Setup: {Key} ({Finger})

Adjust the hand gizmo using the 6 visual alignment guides:
1. **Target Key Center**: Small Red Sphere
2. **Target Fingertip**: Small Green Sphere (tracks live curled fingertip)
3. **Hand Surface Plane**: 10cm x 10cm Green translucent slab (5mm thick) attached to hand
4. **Keycap Surface Plane**: 10cm x 10cm Red translucent slab (5mm thick) attached to key
5. **Elbow Vertical Reference**: Vertical Blue line through resting elbow
6. **Hand Forearm Reference**: Blue line extending 1m back from the hand

**Alignment Goals & Glow Feedback**:
- **Position**: Overlap the **Green Sphere** with the **Red Sphere** (glows brighter as they approach, snaps to **pure white** upon contact).
- **Tilt (Pitch/Roll)**: Orient the hand so the **Green Slab** is parallel with the **Red Slab** (glows brighter as angle reduces, snaps to **pure white** when parallel).
- **Yaw (Forearm Pivot)**: Rotate hand yaw until the **Hand Blue Line** aligns with/crosses the **Elbow Vertical Blue Line** (glows cyan as they near, snaps to **pure white** when intersecting).

Click **[Apply Transform]** when finished.
";
                setupAnchor.Transform.Broadcast();

                Transform parentTr = root != null && root.GameObject != null ? root.GameObject.transform : null;
                CreateDebugVisuals(restPos, parentTr);

                var kbAnchor = setupAnchor as KeyBindingVisualSetupAnchorAsset;
                if (kbAnchor != null) {
                    kbAnchor.OnAlignHandWithSurface = AlignHandWithButtonSurface;
                    kbAnchor.OnMoveFingerToButton = MoveFingerToButton;
                    kbAnchor.OnRotateHandToNaturalRotation = RotateHandToNaturalRotation;
                    kbAnchor.OnSettingsChanged = () => {
                        if (VisualGuidesVisible != kbAnchor.ShowVisualGuides || VisualGuidesAlwaysOnTop != kbAnchor.GuidesAlwaysOnTop) {
                            SetVisualGuidesVisible(kbAnchor.ShowVisualGuides);
                            SetVisualGuidesAlwaysOnTop(kbAnchor.GuidesAlwaysOnTop);
                        }
                        UpdateDebugVisuals();
                    };
                    VisualGuidesVisible = kbAnchor.ShowVisualGuides;
                    VisualGuidesAlwaysOnTop = kbAnchor.GuidesAlwaysOnTop;
                }
            };

            Transform.OnResetAnchor = (tr, setupAnchor) => {
                if (Parent == null) return;
                bool isLeft = Finger.IsLeftHand();
                Vector3 restPos = isLeft ? Parent.LeftHandRestPosition : Parent.RightHandRestPosition;
                Vector3 restRot = isLeft ? Parent.LeftHandRestRotation : Parent.RightHandRestRotation;
                setupAnchor.Transform.Position = restPos;
                setupAnchor.Transform.Rotation = restRot;
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Transform.Broadcast();
            };

            Transform.OnApplyAnchor = (tr, setupAnchor) => {
                if (Parent == null) return;

                bool isLeft = Finger.IsLeftHand();
                Vector3 restPos = isLeft ? Parent.LeftHandRestPosition : Parent.RightHandRestPosition;
                Vector3 restRot = isLeft ? Parent.LeftHandRestRotation : Parent.RightHandRestRotation;

                Vector3 anchorPos = setupAnchor.Transform.Position;
                Vector3 anchorRot = setupAnchor.Transform.Rotation;
                Vector3 deltaHand = anchorPos - restPos;

                Vector3 handScale = isLeft ? Parent.GetLeftHandMovementScale() : Parent.GetRightHandMovementScale();
                Vector3 unscaledDelta = new Vector3(
                    handScale.x != 0f ? deltaHand.x / handScale.x : 0f,
                    handScale.y != 0f ? deltaHand.y / handScale.y : 0f,
                    handScale.z != 0f ? deltaHand.z / handScale.z : 0f
                );
                Vector3 deltaRot = Helper.NormalizeRotationAngles(anchorRot - restRot);

                tr.Position = unscaledDelta;
                tr.Rotation = deltaRot;
                deltaPosition = tr.Position;
                deltaRotation = tr.Rotation;
                EnableHandMovement = true;

                tr.BroadcastDataInput(nameof(tr.Position));
                tr.BroadcastDataInput(nameof(tr.Rotation));
                BroadcastDataInput(nameof(Transform));
                BroadcastDataInput(nameof(EnableHandMovement));
            };

            Transform.OnExitVisualSetup = (isApplied) => {
                DestroyDebugVisuals();
                if (Parent != null) {
                    if (Parent.ActiveKeyBindingInVisualSetup == this) {
                        Parent.ActiveKeyBindingInVisualSetup = null;
                    }
                    Parent.ResetActiveKeyValues();
                    Parent.ResetFingerWeights();
                    Context.Service.NavigateToAsset(Parent.Id);
                    Context.Service.BroadcastOpenedScene();
                }
            };
        }

        public void SetVisualGuidesAlwaysOnTop(bool alwaysOnTop) {
            VisualGuidesAlwaysOnTop = alwaysOnTop;
            RebuildMaterials();
        }

        public void SetVisualGuidesVisible(bool visible) {
            VisualGuidesVisible = visible;
            if (debugKeyCenterSphere != null) debugKeyCenterSphere.SetActive(visible && hasRecordedKey);
            if (debugFingerTipSphere != null) debugFingerTipSphere.SetActive(visible);
            if (debugHandPlaneQuad != null) debugHandPlaneQuad.SetActive(visible);
            if (debugKeyPlaneQuad != null) debugKeyPlaneQuad.SetActive(visible && hasRecordedKey);
            if (debugElbowLine != null) debugElbowLine.SetActive(visible);
            if (debugHandLine != null) debugHandLine.SetActive(visible);
        }

        private static void StripCollider(GameObject go) {
            if (go == null) return;
            var col = go.GetComponent("Collider");
            if (col != null) UnityEngine.Object.Destroy(col);
        }

        private Material CreateOverlayMaterial(Color color, bool transparent = true, int queuePriority = 1000) {
            Material mat;
            if (VisualGuidesAlwaysOnTop) {
                var overlayShader = Shader.Find("GUI/Text Shader") ?? Shader.Find("Hidden/Internal-Colored");
                if (overlayShader != null) {
                    mat = new Material(overlayShader);
                    mat.color = color;
                } else {
                    mat = new Material(Shader.Find("Standard"));
                    mat.color = color;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 0.8f);
                    mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                    if (transparent) {
                        mat.SetFloat("_Mode", 3);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.DisableKeyword("_ALPHATEST_ON");
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    }
                }
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay + queuePriority;
            } else {
                mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.8f);
                if (transparent) {
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + queuePriority;
                } else {
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry + queuePriority;
                }
            }
            debugMaterials.Add(mat);
            return mat;
        }

        private void RebuildMaterials() {
            for (int i = 0; i < debugMaterials.Count; i++) {
                if (debugMaterials[i] != null) UnityEngine.Object.Destroy(debugMaterials[i]);
            }
            debugMaterials.Clear();
            debugElbowLineMat = null;
            debugHandLineMat = null;
            debugKeyCenterSphereMat = null;
            debugFingerTipSphereMat = null;
            debugHandPlaneMat = null;
            debugKeyPlaneMat = null;

            var kbAnchor = Transform?.setupAnchor as KeyBindingVisualSetupAnchorAsset;
            Color handSlabColor = kbAnchor?.HandSlab?.Color ?? new Color(0.15f, 0.85f, 0.25f, 0.5f);
            Color keySlabColor = kbAnchor?.KeySlab?.Color ?? new Color(0.9f, 0.2f, 0.15f, 0.5f);
            Color elbowLineColor = kbAnchor?.ElbowLine?.Color ?? new Color(0.1f, 0.45f, 0.95f, 0.9f);
            Color handLineColor = kbAnchor?.HandLine?.Color ?? new Color(0.1f, 0.45f, 0.95f, 0.9f);
            Color keyCenterColor = kbAnchor?.KeyCenterSphere?.Color ?? new Color(0.95f, 0.15f, 0.15f, 0.9f);
            Color fingerTipColor = kbAnchor?.FingerTipSphere?.Color ?? new Color(0.1f, 0.95f, 0.2f, 0.9f);

            // Render order in Always On Top mode:
            // 3. Planes/Slabs (background): queuePriority 1000
            // 2. Lines (middle): queuePriority 2000
            // 1. Spheres (foreground/on top): queuePriority 3000
            debugHandPlaneMat = CreateOverlayMaterial(handSlabColor, true, 1000);
            debugKeyPlaneMat = CreateOverlayMaterial(keySlabColor, true, 1000);
            debugElbowLineMat = CreateOverlayMaterial(elbowLineColor, true, 2000);
            debugHandLineMat = CreateOverlayMaterial(handLineColor, true, 2000);
            debugKeyCenterSphereMat = CreateOverlayMaterial(keyCenterColor, true, 3000);
            debugFingerTipSphereMat = CreateOverlayMaterial(fingerTipColor, true, 3000);

            if (debugHandPlaneQuad != null) {
                var rend = debugHandPlaneQuad.GetComponent<Renderer>();
                if (rend != null) { rend.material = debugHandPlaneMat; rend.sortingOrder = 1000; }
            }
            if (debugKeyPlaneQuad != null) {
                var rend = debugKeyPlaneQuad.GetComponent<Renderer>();
                if (rend != null) { rend.material = debugKeyPlaneMat; rend.sortingOrder = 1000; }
            }
            if (debugElbowLine != null) {
                var rend = debugElbowLine.GetComponent<Renderer>();
                if (rend != null) { rend.material = debugElbowLineMat; rend.sortingOrder = 2000; }
            }
            if (debugHandLine != null) {
                var rend = debugHandLine.GetComponent<Renderer>();
                if (rend != null) { rend.material = debugHandLineMat; rend.sortingOrder = 2000; }
            }
            if (debugKeyCenterSphere != null) {
                var rend = debugKeyCenterSphere.GetComponent<Renderer>();
                if (rend != null) { rend.material = debugKeyCenterSphereMat; rend.sortingOrder = 3000; }
            }
            if (debugFingerTipSphere != null) {
                var rend = debugFingerTipSphere.GetComponent<Renderer>();
                if (rend != null) { rend.material = debugFingerTipSphereMat; rend.sortingOrder = 3000; }
            }
        }

        public void CreateDebugVisuals(Vector3 restPos, Transform parentTransform) {
            DestroyDebugVisuals();

            Vector3 keyCenterWorld = Vector3.zero;
            Quaternion keyRotWorld = Quaternion.identity;
            hasRecordedKey = false;
            if (Parent?.VirtualKeysCollection?.Keys != null) {
                var keyDef = Parent.VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.Key == this.Key && k.AssignedFinger == this.Finger)
                          ?? Parent.VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.Key == this.Key);
                if (keyDef != null) {
                    hasRecordedKey = true;
                    if (keyDef.KeyGameObject != null) {
                        keyCenterWorld = keyDef.KeyGameObject.transform.position;
                        keyRotWorld = keyDef.KeyGameObject.transform.rotation;
                    } else if (Parent.RootAnchor?.GameObject != null) {
                        var rootTr = Parent.RootAnchor.GameObject.transform;
                        var pos = keyDef.Transform?.Position ?? Vector3.zero;
                        var rot = keyDef.Transform?.Rotation ?? Vector3.zero;
                        keyCenterWorld = rootTr.TransformPoint(pos);
                        keyRotWorld = rootTr.rotation * Quaternion.Euler(rot);
                    }
                }
            }

            bool isLeft = Finger.IsLeftHand();
            var animator = Parent?.Character?.Animator;
            if (animator != null) {
                var elbowBone = animator.GetBoneTransform(isLeft ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
                if (elbowBone != null) {
                    recordedElbowPosWorld = elbowBone.position;
                    hasRecordedElbow = true;
                }
            }
            if (!hasRecordedElbow) {
                Vector3 fallbackWrist = isLeft ? Parent.LeftHandRestPosition : Parent.RightHandRestPosition;
                recordedElbowPosWorld = fallbackWrist - new Vector3(isLeft ? 0.05f : -0.05f, -0.05f, 0.25f);
            }

            // 1. Target Key Center Sphere (priority 3000: foreground)
            debugKeyCenterSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugKeyCenterSphere.name = "KeyBinding_DebugKeyCenterSphere";
            StripCollider(debugKeyCenterSphere);
            debugKeyCenterSphere.transform.localScale = new Vector3(0.0075f, 0.0075f, 0.0075f);
            if (hasRecordedKey) {
                debugKeyCenterSphere.transform.position = keyCenterWorld;
                debugKeyCenterSphere.SetActive(VisualGuidesVisible);
            } else {
                debugKeyCenterSphere.SetActive(false);
            }

            // 2. Target Fingertip Sphere (priority 3000: foreground)
            debugFingerTipSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugFingerTipSphere.name = "KeyBinding_DebugFingerTipSphere";
            StripCollider(debugFingerTipSphere);
            debugFingerTipSphere.transform.localScale = new Vector3(0.0075f, 0.0075f, 0.0075f);
            if (animator != null) {
                debugFingerTipSphere.transform.position = Parent.GetFingerTipWorldPosition(animator, Finger);
            }
            debugFingerTipSphere.SetActive(VisualGuidesVisible);

            // 3. Hand Surface Slab (priority 1000: background, renders behind lines)
            debugHandPlaneQuad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debugHandPlaneQuad.name = "KeyBinding_DebugHandPlane";
            StripCollider(debugHandPlaneQuad);
            debugHandPlaneQuad.transform.localScale = new Vector3(0.05f, 0.0025f, 0.05f);
            debugHandPlaneQuad.SetActive(VisualGuidesVisible);

            // 4. Key Surface Slab (priority 1000: background, renders behind lines)
            debugKeyPlaneQuad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debugKeyPlaneQuad.name = "KeyBinding_DebugKeyPlane";
            StripCollider(debugKeyPlaneQuad);
            debugKeyPlaneQuad.transform.localScale = new Vector3(0.05f, 0.0025f, 0.05f);
            if (hasRecordedKey) {
                Vector3 keyNormal = keyRotWorld * Vector3.up;
                debugKeyPlaneQuad.transform.position = keyCenterWorld;
                debugKeyPlaneQuad.transform.rotation = Quaternion.FromToRotation(Vector3.up, keyNormal);
                debugKeyPlaneQuad.SetActive(VisualGuidesVisible);
            } else {
                debugKeyPlaneQuad.SetActive(false);
            }

            // 5. Elbow Line (priority 2000: middle)
            debugElbowLine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            debugElbowLine.name = "KeyBinding_DebugElbowLine";
            StripCollider(debugElbowLine);
            debugElbowLine.transform.localScale = new Vector3(0.0015f, 0.5f, 0.0015f);
            debugElbowLine.transform.position = recordedElbowPosWorld;
            debugElbowLine.transform.rotation = Quaternion.identity;
            debugElbowLine.SetActive(VisualGuidesVisible);

            // 6. Hand Line (priority 2000: middle)
            debugHandLine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            debugHandLine.name = "KeyBinding_DebugHandLine";
            StripCollider(debugHandLine);
            debugHandLine.transform.localScale = new Vector3(0.0015f, 0.25f, 0.0015f);
            debugHandLine.SetActive(VisualGuidesVisible);

            RebuildMaterials();
            UpdateDebugVisuals();
        }

        public void AlignHandWithButtonSurface() {
            if (Parent == null || Transform == null || Transform.setupAnchor == null || Transform.setupAnchor.GameObject == null) return;
            var anchorTr = Transform.setupAnchor.GameObject.transform;
            Quaternion handRot = anchorTr.rotation;
            Vector3 handNormal = handRot * Vector3.up;

            Vector3 keyNormal = Vector3.up;
            if (Parent?.VirtualKeysCollection?.Keys != null) {
                var keyDef = Parent.VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.Key == this.Key && k.AssignedFinger == this.Finger)
                          ?? Parent.VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.Key == this.Key);
                if (keyDef != null && keyDef.KeyGameObject != null) {
                    keyNormal = keyDef.KeyGameObject.transform.rotation * Vector3.up;
                } else if (keyDef != null && Parent.RootAnchor?.GameObject != null) {
                    var rootTr = Parent.RootAnchor.GameObject.transform;
                    var rot = keyDef.Transform?.Rotation ?? Vector3.zero;
                    keyNormal = rootTr.rotation * Quaternion.Euler(rot) * Vector3.up;
                }
            }

            Quaternion tilt = Quaternion.FromToRotation(handNormal, keyNormal);
            Quaternion newHandRot = tilt * handRot;

            Quaternion newLocalRot = anchorTr.parent != null
                ? Quaternion.Inverse(anchorTr.parent.rotation) * newHandRot
                : newHandRot;

            Transform.setupAnchor.Transform.Rotation = Helper.NormalizeRotationAngles(newLocalRot.eulerAngles);
            Transform.setupAnchor.Transform.Broadcast();
            UpdateDebugVisuals();
        }

        public void MoveFingerToButton() {
            if (Parent == null || Transform == null || Transform.setupAnchor == null || Transform.setupAnchor.GameObject == null) return;
            var animator = Parent?.Character?.Animator;
            if (animator == null) return;

            Vector3 tipWorld = Parent.GetFingerTipWorldPosition(animator, Finger);
            Vector3 keyCenterWorld = Vector3.zero;
            bool foundKey = false;
            if (Parent?.VirtualKeysCollection?.Keys != null) {
                var keyDef = Parent.VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.Key == this.Key && k.AssignedFinger == this.Finger)
                          ?? Parent.VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.Key == this.Key);
                if (keyDef != null && keyDef.KeyGameObject != null) {
                    keyCenterWorld = keyDef.KeyGameObject.transform.position;
                    foundKey = true;
                } else if (keyDef != null && Parent.RootAnchor?.GameObject != null) {
                    var rootTr = Parent.RootAnchor.GameObject.transform;
                    var pos = keyDef.Transform?.Position ?? Vector3.zero;
                    keyCenterWorld = rootTr.TransformPoint(pos);
                    foundKey = true;
                }
            }
            if (!foundKey) return;

            Transform parentTr = Transform.setupAnchor.GameObject.transform.parent;
            Vector3 tipInParent = parentTr != null ? parentTr.InverseTransformPoint(tipWorld) : tipWorld;
            Vector3 keyInParent = parentTr != null ? parentTr.InverseTransformPoint(keyCenterWorld) : keyCenterWorld;

            Vector3 delta = keyInParent - tipInParent;
            Transform.setupAnchor.Transform.Position += delta;
            Transform.setupAnchor.Transform.Broadcast();
            UpdateDebugVisuals();
        }

        public void RotateHandToNaturalRotation() {
            if (Parent == null || Transform == null || Transform.setupAnchor == null || Transform.setupAnchor.GameObject == null) return;
            var anchorTr = Transform.setupAnchor.GameObject.transform;
            Vector3 handPos = anchorTr.position;
            Quaternion handRot = anchorTr.rotation;
            Vector3 handNormal = handRot * Vector3.up;

            Vector3 elbowPos = recordedElbowPosWorld;
            Vector3 toElbow = elbowPos - handPos;
            Vector3 targetBackOnPlane = Vector3.ProjectOnPlane(toElbow, handNormal).normalized;
            if (targetBackOnPlane.sqrMagnitude < 0.001f) return;

            // Current backward direction of the hand projected onto the palm/key plane
            Vector3 currentBack = handRot * Vector3.back;
            Vector3 currentBackOnPlane = Vector3.ProjectOnPlane(currentBack, handNormal).normalized;
            if (currentBackOnPlane.sqrMagnitude < 0.001f) currentBackOnPlane = targetBackOnPlane;

            // Compute minimal rotation angle around handNormal (shortest arc in [-180, 180])
            float signedAngle = Vector3.SignedAngle(currentBackOnPlane, targetBackOnPlane, handNormal);
            Quaternion rotDelta = Quaternion.AngleAxis(signedAngle, handNormal);
            Quaternion newRot = rotDelta * handRot;

            Quaternion newLocalRot = anchorTr.parent != null
                ? Quaternion.Inverse(anchorTr.parent.rotation) * newRot
                : newRot;

            Transform.setupAnchor.Transform.Rotation = Helper.NormalizeRotationAngles(newLocalRot.eulerAngles);
            Transform.setupAnchor.Transform.Broadcast();
            UpdateDebugVisuals();
        }

        public void UpdateDebugVisuals() {
            if (Parent == null || Transform == null || Transform.setupAnchor == null || Transform.setupAnchor.GameObject == null) return;

            if (!VisualGuidesVisible) {
                if (debugKeyCenterSphere != null && debugKeyCenterSphere.activeSelf) debugKeyCenterSphere.SetActive(false);
                if (debugFingerTipSphere != null && debugFingerTipSphere.activeSelf) debugFingerTipSphere.SetActive(false);
                if (debugHandPlaneQuad != null && debugHandPlaneQuad.activeSelf) debugHandPlaneQuad.SetActive(false);
                if (debugKeyPlaneQuad != null && debugKeyPlaneQuad.activeSelf) debugKeyPlaneQuad.SetActive(false);
                if (debugElbowLine != null && debugElbowLine.activeSelf) debugElbowLine.SetActive(false);
                if (debugHandLine != null && debugHandLine.activeSelf) debugHandLine.SetActive(false);
                return;
            }

            var kbAnchor = Transform.setupAnchor as KeyBindingVisualSetupAnchorAsset;

            // Guides structured data
            var fingerSphereData = kbAnchor?.FingerTipSphere;
            var keySphereData = kbAnchor?.KeyCenterSphere;
            var handSlabData = kbAnchor?.HandSlab;
            var keySlabData = kbAnchor?.KeySlab;
            var handLineData = kbAnchor?.HandLine;
            var elbowLineData = kbAnchor?.ElbowLine;

            // Thresholds
            float lineWhiteThresh = kbAnchor?.LineThreshold != null ? kbAnchor.LineThreshold.GetWhiteThreshold() : 0.006f;
            float lineBrightSpan = kbAnchor?.LineThreshold != null ? kbAnchor.LineThreshold.GetTransitionSpan() : 0.024f;
            float sphereWhiteThresh = kbAnchor?.SphereThreshold != null ? kbAnchor.SphereThreshold.GetWhiteThreshold() : 0.006f;
            float sphereBrightSpan = kbAnchor?.SphereThreshold != null ? kbAnchor.SphereThreshold.GetTransitionSpan() : 0.024f;
            float slabWhiteAngleThresh = kbAnchor?.SlabThreshold != null ? kbAnchor.SlabThreshold.GetWhiteThreshold() : 3.0f;
            float slabBrightAngleSpan = kbAnchor?.SlabThreshold != null ? kbAnchor.SlabThreshold.GetTransitionSpan() : 12.0f;

            // Factors
            float fingerSphereRadiusFactor = fingerSphereData != null ? fingerSphereData.RadiusFactor : 1.0f;
            float keySphereRadiusFactor = keySphereData != null ? keySphereData.RadiusFactor : 1.0f;
            float handLineLengthFactor = handLineData != null ? handLineData.LengthFactor : 1.0f;
            float handLineThicknessFactor = handLineData != null ? handLineData.ThicknessFactor : 1.0f;
            float elbowLineLengthFactor = elbowLineData != null ? elbowLineData.LengthFactor : 1.0f;
            float elbowLineThicknessFactor = elbowLineData != null ? elbowLineData.ThicknessFactor : 1.0f;

            // Custom slab transforms
            GuideSlabTransform handSlabTr = handSlabData?.Transform;
            GuideSlabTransform keySlabTr = keySlabData?.Transform;

            var anchorTr = Transform.setupAnchor.GameObject.transform;
            Vector3 handPos = anchorTr.position;
            Quaternion handRot = anchorTr.rotation;
            Vector3 handNormal = handRot * Vector3.up;

            Vector3 keyCenterWorld = Vector3.zero;
            Vector3 keyNormal = Vector3.up;
            Quaternion keyRotWorld = Quaternion.identity;
            if (Parent?.VirtualKeysCollection?.Keys != null) {
                var keyDef = Parent.VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.Key == this.Key && k.AssignedFinger == this.Finger)
                          ?? Parent.VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.Key == this.Key);
                if (keyDef != null && keyDef.KeyGameObject != null) {
                    keyCenterWorld = keyDef.KeyGameObject.transform.position;
                    keyRotWorld = keyDef.KeyGameObject.transform.rotation;
                    keyNormal = keyRotWorld * Vector3.up;
                    hasRecordedKey = true;

                    if (debugKeyCenterSphere != null) {
                        debugKeyCenterSphere.transform.position = keyCenterWorld;
                        debugKeyCenterSphere.transform.localScale = new Vector3(0.0075f, 0.0075f, 0.0075f) * keySphereRadiusFactor;
                        if (!debugKeyCenterSphere.activeSelf) debugKeyCenterSphere.SetActive(true);
                    }
                    if (debugKeyPlaneQuad != null) {
                        Vector3 keySlabOffset = keySlabTr != null ? keySlabTr.Position : Vector3.zero;
                        Vector3 keySlabRot = keySlabTr != null ? keySlabTr.Rotation : Vector3.zero;
                        Vector3 keyScaleFactor = keySlabTr != null ? keySlabTr.Scale : Vector3.one;
                        if (keyScaleFactor.x <= 0.06f && keyScaleFactor.x > 0.0001f && keyScaleFactor.z <= 0.06f) keyScaleFactor = Vector3.one;
                        Vector3 keySlabScale = Vector3.Scale(new Vector3(0.05f, 0.0025f, 0.05f), keyScaleFactor);

                        Vector3 keyFwdOnPlane = Vector3.ProjectOnPlane(keyRotWorld * Vector3.forward, keyNormal).normalized;
                        if (keyFwdOnPlane.sqrMagnitude < 0.001f) keyFwdOnPlane = Vector3.forward;
                        Vector3 keyRightOnPlane = Vector3.Cross(keyNormal, keyFwdOnPlane).normalized;

                        Vector3 keyPlanePos = keyCenterWorld + (keyRightOnPlane * keySlabOffset.x) + (keyNormal * keySlabOffset.y) + (keyFwdOnPlane * keySlabOffset.z);
                        Quaternion keyPlaneRot = Quaternion.FromToRotation(Vector3.up, keyNormal) * Quaternion.Euler(keySlabRot);

                        debugKeyPlaneQuad.transform.position = keyPlanePos;
                        debugKeyPlaneQuad.transform.rotation = keyPlaneRot;
                        debugKeyPlaneQuad.transform.localScale = keySlabScale;
                        if (!debugKeyPlaneQuad.activeSelf) debugKeyPlaneQuad.SetActive(true);
                    }
                }
            }

            if (debugHandPlaneQuad != null) {
                Vector3 handSlabOffset = handSlabTr != null ? handSlabTr.Position : new Vector3(0f, 0f, 0.025f);
                Vector3 handSlabRot = handSlabTr != null ? handSlabTr.Rotation : Vector3.zero;
                Vector3 handScaleFactor = handSlabTr != null ? handSlabTr.Scale : Vector3.one;
                if (handScaleFactor.x <= 0.06f && handScaleFactor.x > 0.0001f && handScaleFactor.z <= 0.06f) handScaleFactor = Vector3.one;
                Vector3 handSlabScale = Vector3.Scale(new Vector3(0.05f, 0.0025f, 0.05f), handScaleFactor);

                Vector3 handFwdOnPlane = Vector3.ProjectOnPlane(handRot * Vector3.forward, handNormal).normalized;
                if (handFwdOnPlane.sqrMagnitude < 0.001f) handFwdOnPlane = Vector3.forward;
                Vector3 handRightOnPlane = Vector3.Cross(handNormal, handFwdOnPlane).normalized;

                Vector3 handPlanePos = handPos + (handRightOnPlane * handSlabOffset.x) + (handNormal * handSlabOffset.y) + (handFwdOnPlane * handSlabOffset.z);
                Quaternion handPlaneRot = Quaternion.FromToRotation(Vector3.up, handNormal) * Quaternion.Euler(handSlabRot);

                debugHandPlaneQuad.transform.position = handPlanePos;
                debugHandPlaneQuad.transform.rotation = handPlaneRot;
                debugHandPlaneQuad.transform.localScale = handSlabScale;
                if (!debugHandPlaneQuad.activeSelf) debugHandPlaneQuad.SetActive(true);
            }

            Vector3 backwardDir = handRot * Vector3.back;
            float handLineHalfLength = 0.25f * handLineLengthFactor;
            if (debugHandLine != null) {
                debugHandLine.transform.position = handPos + (backwardDir * handLineHalfLength);
                debugHandLine.transform.rotation = Quaternion.LookRotation(backwardDir, handNormal) * Quaternion.Euler(90f, 0f, 0f);
                if (!debugHandLine.activeSelf) debugHandLine.SetActive(true);
            }
            if (debugElbowLine != null && !debugElbowLine.activeSelf) debugElbowLine.SetActive(true);

            Vector2 u2D = new Vector2(backwardDir.x, backwardDir.z);
            float uLen = u2D.magnitude;
            float lineDist = float.MaxValue;
            if (uLen > 0.0001f) {
                u2D /= uLen;
                float deltaX = recordedElbowPosWorld.x - handPos.x;
                float deltaZ = recordedElbowPosWorld.z - handPos.z;
                lineDist = Mathf.Abs(deltaX * u2D.y - deltaZ * u2D.x);
            }

            // Line Proximity (Milestone 3-breakpoint logic)
            float lineOuter = lineWhiteThresh + lineBrightSpan;
            float lineBrightBlend = 0f;
            if (lineDist < lineWhiteThresh) {
                lineBrightBlend = 1f;
            } else if (lineDist <= lineOuter && lineBrightSpan > 0.0001f) {
                lineBrightBlend = Mathf.Clamp01((lineOuter - lineDist) / lineBrightSpan);
            }
            float lineWhiteSnap = (lineDist <= lineWhiteThresh && lineWhiteThresh > 0.0001f)
                ? Mathf.Pow(Mathf.Clamp01((lineWhiteThresh - lineDist) / lineWhiteThresh), 0.5f)
                : 0f;

            Color baseHandLineColor = handLineData != null ? handLineData.Color : new Color(0.1f, 0.45f, 0.95f, 0.9f);
            Color brightHandLineColor = Color.Lerp(baseHandLineColor, Color.white, 0.5f);
            brightHandLineColor.a = baseHandLineColor.a;
            Color whiteHandLineColor = new Color(1f, 1f, 1f, baseHandLineColor.a);
            Color currentHandLineColor = Color.Lerp(Color.Lerp(baseHandLineColor, brightHandLineColor, lineBrightBlend), whiteHandLineColor, lineWhiteSnap);

            Color baseElbowLineColor = elbowLineData != null ? elbowLineData.Color : new Color(0.1f, 0.45f, 0.95f, 0.9f);
            Color brightElbowLineColor = Color.Lerp(baseElbowLineColor, Color.white, 0.5f);
            brightElbowLineColor.a = baseElbowLineColor.a;
            Color whiteElbowLineColor = new Color(1f, 1f, 1f, baseElbowLineColor.a);
            Color currentElbowLineColor = Color.Lerp(Color.Lerp(baseElbowLineColor, brightElbowLineColor, lineBrightBlend), whiteElbowLineColor, lineWhiteSnap);

            if (debugHandLineMat != null) {
                debugHandLineMat.color = currentHandLineColor;
                if (handLineData != null && handLineData.Glow) {
                    debugHandLineMat.EnableKeyword("_EMISSION");
                    Color handEmission = Color.Lerp(Color.Lerp(baseHandLineColor * 0.7f, brightHandLineColor * 2.0f, lineBrightBlend), new Color(3f, 3f, 3f), lineWhiteSnap);
                    debugHandLineMat.SetColor("_EmissionColor", handEmission);
                } else {
                    debugHandLineMat.DisableKeyword("_EMISSION");
                    debugHandLineMat.SetColor("_EmissionColor", Color.black);
                }
            }

            if (debugElbowLineMat != null) {
                debugElbowLineMat.color = currentElbowLineColor;
                if (elbowLineData != null && elbowLineData.Glow) {
                    debugElbowLineMat.EnableKeyword("_EMISSION");
                    Color elbowEmission = Color.Lerp(Color.Lerp(baseElbowLineColor * 0.7f, brightElbowLineColor * 2.0f, lineBrightBlend), new Color(3f, 3f, 3f), lineWhiteSnap);
                    debugElbowLineMat.SetColor("_EmissionColor", elbowEmission);
                } else {
                    debugElbowLineMat.DisableKeyword("_EMISSION");
                    debugElbowLineMat.SetColor("_EmissionColor", Color.black);
                }
            }

            float baseHandRod = 0.0015f * handLineThicknessFactor;
            float rodThickness = Mathf.Lerp(baseHandRod, baseHandRod * 2f, lineBrightBlend);
            if (lineWhiteSnap > 0f) rodThickness = Mathf.Lerp(baseHandRod * 2f, baseHandRod * 2.7f, lineWhiteSnap);

            float baseElbowRod = 0.0015f * elbowLineThicknessFactor;
            float elbowRodThickness = Mathf.Lerp(baseElbowRod, baseElbowRod * 2f, lineBrightBlend);
            if (lineWhiteSnap > 0f) elbowRodThickness = Mathf.Lerp(baseElbowRod * 2f, baseElbowRod * 2.7f, lineWhiteSnap);

            if (debugElbowLine != null) debugElbowLine.transform.localScale = new Vector3(elbowRodThickness, 0.5f * elbowLineLengthFactor, elbowRodThickness);
            if (debugHandLine != null) debugHandLine.transform.localScale = new Vector3(rodThickness, handLineHalfLength, rodThickness);

            Vector3 tipWorld = Vector3.zero;
            var animator = Parent?.Character?.Animator;
            if (animator != null) {
                tipWorld = Parent.GetFingerTipWorldPosition(animator, Finger);
                if (debugFingerTipSphere != null) {
                    debugFingerTipSphere.transform.position = tipWorld;
                    debugFingerTipSphere.transform.localScale = new Vector3(0.0075f, 0.0075f, 0.0075f) * fingerSphereRadiusFactor;
                    if (!debugFingerTipSphere.activeSelf) debugFingerTipSphere.SetActive(true);
                }
            }

            // Sphere Proximity (Milestone 3-breakpoint logic)
            float sphereDist = (hasRecordedKey && animator != null) ? Vector3.Distance(keyCenterWorld, tipWorld) : float.MaxValue;
            float sphereOuter = sphereWhiteThresh + sphereBrightSpan;
            float sphereBrightBlend = 0f;
            if (sphereDist < sphereWhiteThresh) {
                sphereBrightBlend = 1f;
            } else if (sphereDist <= sphereOuter && sphereBrightSpan > 0.0001f) {
                sphereBrightBlend = Mathf.Clamp01((sphereOuter - sphereDist) / sphereBrightSpan);
            }
            float sphereWhiteSnap = (sphereDist <= sphereWhiteThresh && sphereWhiteThresh > 0.0001f)
                ? Mathf.Pow(Mathf.Clamp01((sphereWhiteThresh - sphereDist) / sphereWhiteThresh), 0.5f)
                : 0f;

            Color baseKeySphereColor = keySphereData != null ? keySphereData.Color : new Color(0.95f, 0.15f, 0.15f, 0.9f);
            Color brightKeySphereColor = Color.Lerp(baseKeySphereColor, Color.white, 0.5f);
            brightKeySphereColor.a = baseKeySphereColor.a;
            Color whiteKeySphereColor = new Color(1f, 1f, 1f, baseKeySphereColor.a);
            Color currentKeySphereColor = Color.Lerp(Color.Lerp(baseKeySphereColor, brightKeySphereColor, sphereBrightBlend), whiteKeySphereColor, sphereWhiteSnap);

            if (debugKeyCenterSphereMat != null) {
                debugKeyCenterSphereMat.color = currentKeySphereColor;
                if (keySphereData != null && keySphereData.Glow) {
                    debugKeyCenterSphereMat.EnableKeyword("_EMISSION");
                    Color keyEmission = Color.Lerp(Color.Lerp(baseKeySphereColor * 0.8f, brightKeySphereColor * 2.0f, sphereBrightBlend), new Color(3f, 3f, 3f), sphereWhiteSnap);
                    debugKeyCenterSphereMat.SetColor("_EmissionColor", keyEmission);
                } else {
                    debugKeyCenterSphereMat.DisableKeyword("_EMISSION");
                    debugKeyCenterSphereMat.SetColor("_EmissionColor", Color.black);
                }
            }

            Color baseFingerSphereColor = fingerSphereData != null ? fingerSphereData.Color : new Color(0.1f, 0.95f, 0.2f, 0.9f);
            Color brightFingerSphereColor = Color.Lerp(baseFingerSphereColor, Color.white, 0.5f);
            brightFingerSphereColor.a = baseFingerSphereColor.a;
            Color whiteFingerSphereColor = new Color(1f, 1f, 1f, baseFingerSphereColor.a);
            Color currentFingerSphereColor = Color.Lerp(Color.Lerp(baseFingerSphereColor, brightFingerSphereColor, sphereBrightBlend), whiteFingerSphereColor, sphereWhiteSnap);

            if (debugFingerTipSphereMat != null) {
                debugFingerTipSphereMat.color = currentFingerSphereColor;
                if (fingerSphereData != null && fingerSphereData.Glow) {
                    debugFingerTipSphereMat.EnableKeyword("_EMISSION");
                    Color fingerEmission = Color.Lerp(Color.Lerp(baseFingerSphereColor * 0.8f, brightFingerSphereColor * 2.0f, sphereBrightBlend), new Color(3f, 3f, 3f), sphereWhiteSnap);
                    debugFingerTipSphereMat.SetColor("_EmissionColor", fingerEmission);
                } else {
                    debugFingerTipSphereMat.DisableKeyword("_EMISSION");
                    debugFingerTipSphereMat.SetColor("_EmissionColor", Color.black);
                }
            }

            // Slab Parallelism (Milestone 3-breakpoint logic)
            float angleDeg = hasRecordedKey ? Vector3.Angle(handNormal, keyNormal) : 90f;
            float slabOuter = slabWhiteAngleThresh + slabBrightAngleSpan;
            float slabBrightBlend = 0f;
            if (angleDeg < slabWhiteAngleThresh) {
                slabBrightBlend = 1f;
            } else if (angleDeg <= slabOuter && slabBrightAngleSpan > 0.01f) {
                slabBrightBlend = Mathf.Clamp01((slabOuter - angleDeg) / slabBrightAngleSpan);
            }
            float slabWhiteSnap = (angleDeg <= slabWhiteAngleThresh && slabWhiteAngleThresh > 0.01f)
                ? Mathf.Pow(Mathf.Clamp01((slabWhiteAngleThresh - angleDeg) / slabWhiteAngleThresh), 0.5f)
                : 0f;

            Color baseHandSlabColor = handSlabData != null ? handSlabData.Color : new Color(0.15f, 0.85f, 0.25f, 0.5f);
            Color brightHandSlabColor = Color.Lerp(baseHandSlabColor, Color.white, 0.45f);
            brightHandSlabColor.a = baseHandSlabColor.a;
            Color whiteHandSlabColor = new Color(1f, 1f, 1f, baseHandSlabColor.a);
            Color currentHandSlabColor = Color.Lerp(Color.Lerp(baseHandSlabColor, brightHandSlabColor, slabBrightBlend), whiteHandSlabColor, slabWhiteSnap);

            if (debugHandPlaneMat != null) {
                debugHandPlaneMat.color = currentHandSlabColor;
                if (handSlabData != null && handSlabData.Glow) {
                    debugHandPlaneMat.EnableKeyword("_EMISSION");
                    Color handEmission = Color.Lerp(Color.Lerp(baseHandSlabColor * 0.4f, brightHandSlabColor * 1.5f, slabBrightBlend), new Color(2f, 2f, 2f), slabWhiteSnap);
                    debugHandPlaneMat.SetColor("_EmissionColor", handEmission);
                } else {
                    debugHandPlaneMat.DisableKeyword("_EMISSION");
                    debugHandPlaneMat.SetColor("_EmissionColor", Color.black);
                }
            }

            Color baseKeySlabColor = keySlabData != null ? keySlabData.Color : new Color(0.9f, 0.2f, 0.15f, 0.5f);
            Color brightKeySlabColor = Color.Lerp(baseKeySlabColor, Color.white, 0.45f);
            brightKeySlabColor.a = baseKeySlabColor.a;
            Color whiteKeySlabColor = new Color(1f, 1f, 1f, baseKeySlabColor.a);
            Color currentKeySlabColor = Color.Lerp(Color.Lerp(baseKeySlabColor, brightKeySlabColor, slabBrightBlend), whiteKeySlabColor, slabWhiteSnap);

            if (debugKeyPlaneMat != null) {
                debugKeyPlaneMat.color = currentKeySlabColor;
                if (keySlabData != null && keySlabData.Glow) {
                    debugKeyPlaneMat.EnableKeyword("_EMISSION");
                    Color keyEmission = Color.Lerp(Color.Lerp(baseKeySlabColor * 0.4f, brightKeySlabColor * 1.5f, slabBrightBlend), new Color(2f, 2f, 2f), slabWhiteSnap);
                    debugKeyPlaneMat.SetColor("_EmissionColor", keyEmission);
                } else {
                    debugKeyPlaneMat.DisableKeyword("_EMISSION");
                    debugKeyPlaneMat.SetColor("_EmissionColor", Color.black);
                }
            }
        }

        public void DestroyDebugVisuals() {
            if (debugKeyCenterSphere != null) { UnityEngine.Object.Destroy(debugKeyCenterSphere); debugKeyCenterSphere = null; }
            if (debugFingerTipSphere != null) { UnityEngine.Object.Destroy(debugFingerTipSphere); debugFingerTipSphere = null; }
            if (debugHandPlaneQuad != null) { UnityEngine.Object.Destroy(debugHandPlaneQuad); debugHandPlaneQuad = null; }
            if (debugKeyPlaneQuad != null) { UnityEngine.Object.Destroy(debugKeyPlaneQuad); debugKeyPlaneQuad = null; }
            if (debugElbowLine != null) { UnityEngine.Object.Destroy(debugElbowLine); debugElbowLine = null; }
            if (debugHandLine != null) { UnityEngine.Object.Destroy(debugHandLine); debugHandLine = null; }

            for (int i = 0; i < debugMaterials.Count; i++) {
                if (debugMaterials[i] != null) {
                    UnityEngine.Object.Destroy(debugMaterials[i]);
                }
            }
            debugMaterials.Clear();
            debugKeyCenterSphereMat = null;
            debugFingerTipSphereMat = null;
            debugHandPlaneMat = null;
            debugKeyPlaneMat = null;
            debugElbowLineMat = null;
            debugHandLineMat = null;
            hasRecordedElbow = false;
            hasRecordedKey = false;
        }
    }
}
