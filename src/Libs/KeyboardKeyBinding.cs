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
        public VisualSetupTransform Transform;

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
                Transform = StructuredData.Create<VisualSetupTransform>();
                Transform.Position = deltaPosition;
                Transform.Rotation = deltaRotation;
            }
            Transform.HideScale();
            SetupTransformCallbacks();
            SyncUI();
            UpdateDebugButtonState();

            Watch(nameof(Transform), delegate {
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

                setupAnchor.ShowOrigin = VisualGuidesVisible;
                setupAnchor.AlwaysOnTop = VisualGuidesAlwaysOnTop;
                if (setupAnchor.GetDataInputPort(nameof(setupAnchor.AlwaysOnTop))?.Properties != null) {
                    setupAnchor.GetDataInputPort(nameof(setupAnchor.AlwaysOnTop)).Properties.disabled = !VisualGuidesVisible;
                    setupAnchor.BroadcastDataInputProperties(nameof(setupAnchor.AlwaysOnTop));
                }
                setupAnchor.BroadcastDataInput(nameof(setupAnchor.ShowOrigin));
                setupAnchor.BroadcastDataInput(nameof(setupAnchor.AlwaysOnTop));

                setupAnchor.OnShowOriginChange = (show) => {
                    SetVisualGuidesVisible(show);
                };
                setupAnchor.OnAlwaysOnTopChange = (onTop) => {
                    SetVisualGuidesAlwaysOnTop(onTop);
                };
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
                Vector3 deltaRot = anchorRot - restRot;

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

        private Material CreateOverlayMaterial(Color color, bool transparent = false, int queuePriority = 1000) {
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

            // Render order in Always On Top mode:
            // 3. Lines (background): queuePriority 1000
            // 2. Planes/Slabs (middle): queuePriority 2000
            // 1. Spheres (foreground/on top): queuePriority 3000
            debugElbowLineMat = CreateOverlayMaterial(new Color(0.1f, 0.45f, 0.95f, 0.9f), false, 1000);
            debugHandLineMat = CreateOverlayMaterial(new Color(0.1f, 0.45f, 0.95f, 0.9f), false, 1000);
            debugHandPlaneMat = CreateOverlayMaterial(new Color(0.15f, 1f, 0.2f, 0.5f), true, 2000);
            debugKeyPlaneMat = CreateOverlayMaterial(new Color(1f, 0.15f, 0.15f, 0.5f), true, 2000);
            debugKeyCenterSphereMat = CreateOverlayMaterial(new Color(1f, 0.1f, 0.1f, 0.8f), true, 3000);
            debugFingerTipSphereMat = CreateOverlayMaterial(new Color(0.1f, 1f, 0.2f, 0.8f), true, 3000);

            if (debugElbowLine != null) {
                var rend = debugElbowLine.GetComponent<Renderer>();
                if (rend != null) { rend.material = debugElbowLineMat; rend.sortingOrder = 1000; }
            }
            if (debugHandLine != null) {
                var rend = debugHandLine.GetComponent<Renderer>();
                if (rend != null) { rend.material = debugHandLineMat; rend.sortingOrder = 1000; }
            }
            if (debugHandPlaneQuad != null) {
                var rend = debugHandPlaneQuad.GetComponent<Renderer>();
                if (rend != null) { rend.material = debugHandPlaneMat; rend.sortingOrder = 2000; }
            }
            if (debugKeyPlaneQuad != null) {
                var rend = debugKeyPlaneQuad.GetComponent<Renderer>();
                if (rend != null) { rend.material = debugKeyPlaneMat; rend.sortingOrder = 2000; }
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

            // 1. Target Key Center Sphere (priority 3000: renders over planes/slabs)
            debugKeyCenterSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugKeyCenterSphere.name = "KeyBinding_DebugKeyCenterSphere";
            StripCollider(debugKeyCenterSphere);
            debugKeyCenterSphere.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
            debugKeyCenterSphereMat = CreateOverlayMaterial(new Color(1f, 0.1f, 0.1f, 0.8f), true, 3000);
            var rendKeySphere = debugKeyCenterSphere.GetComponent<Renderer>();
            rendKeySphere.material = debugKeyCenterSphereMat;
            rendKeySphere.sortingOrder = 3000;
            if (hasRecordedKey) {
                debugKeyCenterSphere.transform.position = keyCenterWorld;
                debugKeyCenterSphere.SetActive(VisualGuidesVisible);
            } else {
                debugKeyCenterSphere.SetActive(false);
            }

            // 2. Target Fingertip Sphere (priority 3000: renders over planes/slabs)
            debugFingerTipSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugFingerTipSphere.name = "KeyBinding_DebugFingerTipSphere";
            StripCollider(debugFingerTipSphere);
            debugFingerTipSphere.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
            debugFingerTipSphereMat = CreateOverlayMaterial(new Color(0.1f, 1f, 0.2f, 0.8f), true, 3000);
            var rendFingerSphere = debugFingerTipSphere.GetComponent<Renderer>();
            rendFingerSphere.material = debugFingerTipSphereMat;
            rendFingerSphere.sortingOrder = 3000;
            if (animator != null) {
                debugFingerTipSphere.transform.position = Parent.GetFingerTipWorldPosition(animator, Finger);
            }
            debugFingerTipSphere.SetActive(VisualGuidesVisible);

            // 3. Hand Surface Slab (priority 2000: renders under spheres, over lines)
            debugHandPlaneQuad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debugHandPlaneQuad.name = "KeyBinding_DebugHandPlane";
            StripCollider(debugHandPlaneQuad);
            debugHandPlaneQuad.transform.localScale = new Vector3(0.10f, 0.005f, 0.10f);
            debugHandPlaneMat = CreateOverlayMaterial(new Color(0.15f, 1f, 0.2f, 0.5f), true, 2000);
            var rendHandPlane = debugHandPlaneQuad.GetComponent<Renderer>();
            rendHandPlane.material = debugHandPlaneMat;
            rendHandPlane.sortingOrder = 2000;
            debugHandPlaneQuad.SetActive(VisualGuidesVisible);

            // 4. Key Surface Slab (priority 2000: renders under spheres, over lines)
            debugKeyPlaneQuad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debugKeyPlaneQuad.name = "KeyBinding_DebugKeyPlane";
            StripCollider(debugKeyPlaneQuad);
            debugKeyPlaneQuad.transform.localScale = new Vector3(0.10f, 0.005f, 0.10f);
            debugKeyPlaneMat = CreateOverlayMaterial(new Color(1f, 0.15f, 0.15f, 0.5f), true, 2000);
            var rendKeyPlane = debugKeyPlaneQuad.GetComponent<Renderer>();
            rendKeyPlane.material = debugKeyPlaneMat;
            rendKeyPlane.sortingOrder = 2000;
            if (hasRecordedKey) {
                Vector3 keyNormal = keyRotWorld * Vector3.up;
                debugKeyPlaneQuad.transform.position = keyCenterWorld;
                debugKeyPlaneQuad.transform.rotation = Quaternion.FromToRotation(Vector3.up, keyNormal);
                debugKeyPlaneQuad.SetActive(VisualGuidesVisible);
            } else {
                debugKeyPlaneQuad.SetActive(false);
            }

            // 5. Elbow Line (priority 1000: renders in background)
            debugElbowLine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            debugElbowLine.name = "KeyBinding_DebugElbowLine";
            StripCollider(debugElbowLine);
            debugElbowLine.transform.localScale = new Vector3(0.003f, 1.0f, 0.003f);
            debugElbowLineMat = CreateOverlayMaterial(new Color(0.1f, 0.45f, 0.95f, 0.9f), false, 1000);
            var rendElbowLine = debugElbowLine.GetComponent<Renderer>();
            rendElbowLine.material = debugElbowLineMat;
            rendElbowLine.sortingOrder = 1000;
            debugElbowLine.transform.position = recordedElbowPosWorld;
            debugElbowLine.transform.rotation = Quaternion.identity;
            debugElbowLine.SetActive(VisualGuidesVisible);

            // 6. Hand Line (priority 1000: renders in background)
            debugHandLine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            debugHandLine.name = "KeyBinding_DebugHandLine";
            StripCollider(debugHandLine);
            debugHandLine.transform.localScale = new Vector3(0.003f, 0.5f, 0.003f);
            debugHandLineMat = CreateOverlayMaterial(new Color(0.1f, 0.45f, 0.95f, 0.9f), false, 1000);
            var rendHandLine = debugHandLine.GetComponent<Renderer>();
            rendHandLine.material = debugHandLineMat;
            rendHandLine.sortingOrder = 1000;
            debugHandLine.SetActive(VisualGuidesVisible);

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

            var anchorTr = Transform.setupAnchor.GameObject.transform;
            Vector3 handPos = anchorTr.position;
            Quaternion handRot = anchorTr.rotation;
            Vector3 handNormal = handRot * Vector3.up;

            Vector3 keyCenterWorld = Vector3.zero;
            Vector3 keyNormal = Vector3.up;
            if (Parent?.VirtualKeysCollection?.Keys != null) {
                var keyDef = Parent.VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.Key == this.Key && k.AssignedFinger == this.Finger)
                          ?? Parent.VirtualKeysCollection.Keys.FirstOrDefault(k => k != null && k.Key == this.Key);
                if (keyDef != null && keyDef.KeyGameObject != null) {
                    keyCenterWorld = keyDef.KeyGameObject.transform.position;
                    Quaternion keyRot = keyDef.KeyGameObject.transform.rotation;
                    keyNormal = keyRot * Vector3.up;
                    hasRecordedKey = true;

                    if (debugKeyCenterSphere != null) {
                        debugKeyCenterSphere.transform.position = keyCenterWorld;
                        if (!debugKeyCenterSphere.activeSelf) debugKeyCenterSphere.SetActive(true);
                    }
                    if (debugKeyPlaneQuad != null) {
                        debugKeyPlaneQuad.transform.position = keyCenterWorld;
                        debugKeyPlaneQuad.transform.rotation = Quaternion.FromToRotation(Vector3.up, keyNormal);
                        if (!debugKeyPlaneQuad.activeSelf) debugKeyPlaneQuad.SetActive(true);
                    }
                }
            }

            if (debugHandPlaneQuad != null) {
                Vector3 handFwdOnPlane = Vector3.ProjectOnPlane(handRot * Vector3.forward, handNormal).normalized;
                if (handFwdOnPlane.sqrMagnitude < 0.001f) handFwdOnPlane = Vector3.forward;
                Vector3 handPlanePos = handPos + handFwdOnPlane * 0.05f;

                debugHandPlaneQuad.transform.position = handPlanePos;
                debugHandPlaneQuad.transform.rotation = Quaternion.FromToRotation(Vector3.up, handNormal);
                if (!debugHandPlaneQuad.activeSelf) debugHandPlaneQuad.SetActive(true);
            }

            Vector3 backwardDir = handRot * Vector3.back;
            if (debugHandLine != null) {
                debugHandLine.transform.position = handPos + (backwardDir * 0.5f);
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

            float axisGlow = Mathf.Clamp01(1f - (lineDist / 0.025f));
            float axisWhiteSnap = lineDist <= 0.004f ? Mathf.Pow(Mathf.Clamp01(1f - (lineDist / 0.004f)), 0.5f) : 0f;

            Color normalBlue = new Color(0.1f, 0.45f, 0.95f, 0.9f);
            Color glowCyan = new Color(0.1f, 1.0f, 1.0f, 1.0f);
            Color currentLineColor = Color.Lerp(Color.Lerp(normalBlue, glowCyan, axisGlow), Color.white, axisWhiteSnap);

            Color normalEmission = new Color(0.05f, 0.25f, 0.7f);
            Color glowEmission = new Color(0.4f, 2.0f, 2.0f);
            Color currentLineEmission = Color.Lerp(Color.Lerp(normalEmission, glowEmission, axisGlow), new Color(3.0f, 3.0f, 3.0f), axisWhiteSnap);

            if (debugElbowLineMat != null) {
                debugElbowLineMat.color = currentLineColor;
                debugElbowLineMat.SetColor("_EmissionColor", currentLineEmission);
            }
            if (debugHandLineMat != null) {
                debugHandLineMat.color = currentLineColor;
                debugHandLineMat.SetColor("_EmissionColor", currentLineEmission);
            }

            float rodThickness = Mathf.Lerp(0.003f, 0.006f, axisGlow);
            if (axisWhiteSnap > 0f) rodThickness = Mathf.Lerp(0.006f, 0.008f, axisWhiteSnap);
            if (debugElbowLine != null) debugElbowLine.transform.localScale = new Vector3(rodThickness, 1.0f, rodThickness);
            if (debugHandLine != null) debugHandLine.transform.localScale = new Vector3(rodThickness, 0.5f, rodThickness);

            Vector3 tipWorld = Vector3.zero;
            var animator = Parent?.Character?.Animator;
            if (animator != null) {
                tipWorld = Parent.GetFingerTipWorldPosition(animator, Finger);
                if (debugFingerTipSphere != null) {
                    debugFingerTipSphere.transform.position = tipWorld;
                    if (!debugFingerTipSphere.activeSelf) debugFingerTipSphere.SetActive(true);
                }
            }

            float sphereDist = (hasRecordedKey && animator != null) ? Vector3.Distance(keyCenterWorld, tipWorld) : float.MaxValue;
            float sphereGlow = Mathf.Clamp01(1f - (sphereDist / 0.03f));
            float sphereWhiteSnap = sphereDist <= 0.004f ? Mathf.Pow(Mathf.Clamp01(1f - (sphereDist / 0.004f)), 0.5f) : 0f;

            Color baseKeySphereColor = new Color(1f, 0.1f, 0.1f, 0.8f);
            Color glowKeySphereColor = new Color(1f, 0.65f, 0.1f, 0.95f);
            Color currentKeySphereColor = Color.Lerp(Color.Lerp(baseKeySphereColor, glowKeySphereColor, sphereGlow), Color.white, sphereWhiteSnap);
            Color currentKeySphereEmission = Color.Lerp(new Color(0.8f, 0.08f, 0.08f), new Color(3.0f, 3.0f, 3.0f), sphereWhiteSnap > 0 ? sphereWhiteSnap : sphereGlow);

            if (debugKeyCenterSphereMat != null) {
                debugKeyCenterSphereMat.color = currentKeySphereColor;
                debugKeyCenterSphereMat.SetColor("_EmissionColor", currentKeySphereEmission);
            }

            Color baseFingerSphereColor = new Color(0.1f, 1f, 0.2f, 0.8f);
            Color glowFingerSphereColor = new Color(0.2f, 1f, 0.8f, 0.95f);
            Color currentFingerSphereColor = Color.Lerp(Color.Lerp(baseFingerSphereColor, glowFingerSphereColor, sphereGlow), Color.white, sphereWhiteSnap);
            Color currentFingerSphereEmission = Color.Lerp(new Color(0.08f, 0.8f, 0.16f), new Color(3.0f, 3.0f, 3.0f), sphereWhiteSnap > 0 ? sphereWhiteSnap : sphereGlow);

            if (debugFingerTipSphereMat != null) {
                debugFingerTipSphereMat.color = currentFingerSphereColor;
                debugFingerTipSphereMat.SetColor("_EmissionColor", currentFingerSphereEmission);
            }

            float angleDeg = hasRecordedKey ? Vector3.Angle(handNormal, keyNormal) : 90f;
            float parallelGlow = Mathf.Clamp01(1f - (angleDeg / 15.0f));
            float parallelWhiteSnap = angleDeg <= 2.0f ? Mathf.Pow(Mathf.Clamp01(1f - (angleDeg / 2.0f)), 0.5f) : 0f;

            Color baseHandSlabColor = new Color(0.15f, 1f, 0.2f, 0.5f);
            Color glowHandSlabColor = new Color(0.2f, 1f, 0.85f, 0.75f);
            Color currentHandSlabColor = Color.Lerp(Color.Lerp(baseHandSlabColor, glowHandSlabColor, parallelGlow), new Color(1f, 1f, 1f, 0.95f), parallelWhiteSnap);
            Color currentHandSlabEmission = Color.Lerp(new Color(0.1f, 0.7f, 0.15f), new Color(2.5f, 2.5f, 2.5f), parallelWhiteSnap > 0 ? parallelWhiteSnap : parallelGlow);

            if (debugHandPlaneMat != null) {
                debugHandPlaneMat.color = currentHandSlabColor;
                debugHandPlaneMat.SetColor("_EmissionColor", currentHandSlabEmission);
            }

            Color baseKeySlabColor = new Color(1f, 0.15f, 0.15f, 0.5f);
            Color glowKeySlabColor = new Color(1f, 0.65f, 0.2f, 0.75f);
            Color currentKeySlabColor = Color.Lerp(Color.Lerp(baseKeySlabColor, glowKeySlabColor, parallelGlow), new Color(1f, 1f, 1f, 0.95f), parallelWhiteSnap);
            Color currentKeySlabEmission = Color.Lerp(new Color(0.7f, 0.1f, 0.1f), new Color(2.5f, 2.5f, 2.5f), parallelWhiteSnap > 0 ? parallelWhiteSnap : parallelGlow);

            if (debugKeyPlaneMat != null) {
                debugKeyPlaneMat.color = currentKeySlabColor;
                debugKeyPlaneMat.SetColor("_EmissionColor", currentKeySlabEmission);
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
