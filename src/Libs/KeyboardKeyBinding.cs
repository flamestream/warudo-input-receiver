using System;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

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
                port.Properties.label = listening ? "IDENTIFYING_KEY" : "IDENTIFY_KEY";
                BroadcastTriggerProperties(nameof(TriggerIdentifyKey));
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

        // Visual guide objects during key setup
        GameObject debugRestSphere;
        Material debugRestSphereMat;

        public string GetHeader() {
            var handSide = Finger.IsLeftHand() ? "L" : "R";
            var handMovementTag = EnableHandMovement ? "" : " (No Hand Move)";
            var pos = Transform != null ? Transform.Position : DeltaPosition;
            var listeningTag = IsListeningForKey ? " ⏳[LISTENING]" : "";
            return $"[{Key}] ➔ [{handSide}] {Finger} ΔPos({pos.x:F3}, {pos.y:F3}, {pos.z:F3}){handMovementTag}{listeningTag}";
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

            Watch(nameof(Transform), delegate {
                SetupTransformCallbacks();
            });
            Watch(nameof(Key), delegate {
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
                string handName = isLeft ? "Left Hand Rest Pose" : "Right Hand Rest Pose";
                setupAnchor.Instructions = $@"
### Key Binding Setup: {Key} ({Finger})

Position and rotate this anchor where the **{Finger}** should be placed when pressing key **[{Key}]**, relative to the **{handName}**.
The red sphere indicates the resting hand origin ({handName}).
The avatar's hand and finger will follow this anchor in real-time.
Click **[Apply Transform]** when finished.
";
                setupAnchor.Transform.Broadcast();

                Transform parentTr = root != null && root.GameObject != null ? root.GameObject.transform : null;
                CreateDebugVisuals(restPos, parentTr);

                setupAnchor.OnShowOriginChange = (show) => {
                    if (debugRestSphere != null) {
                        debugRestSphere.SetActive(show);
                    }
                };
                setupAnchor.OnAlwaysOnTopChange = (onTop) => {
                    SetDebugSphereAlwaysOnTop(onTop);
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
                tr.Position = new Vector3(
                    handScale.x != 0f ? deltaHand.x / handScale.x : deltaHand.x,
                    handScale.y != 0f ? deltaHand.y / handScale.y : deltaHand.y,
                    handScale.z != 0f ? deltaHand.z / handScale.z : deltaHand.z
                );

                tr.Rotation = anchorRot - restRot;
                tr.NormalizeRotationAngles();

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

        public bool DebugSphereAlwaysOnTop = false;

        public void SetDebugSphereAlwaysOnTop(bool alwaysOnTop) {
            DebugSphereAlwaysOnTop = alwaysOnTop;
            UpdateDebugSphereMaterial();
        }

        public void UpdateDebugSphereMaterial() {
            if (debugRestSphere == null) return;
            if (debugRestSphereMat != null) {
                UnityEngine.Object.Destroy(debugRestSphereMat);
                debugRestSphereMat = null;
            }

            if (DebugSphereAlwaysOnTop) {
                var overlayShader = Shader.Find("GUI/Text Shader") ?? Shader.Find("Hidden/Internal-Colored");
                if (overlayShader != null) {
                    debugRestSphereMat = new Material(overlayShader);
                    debugRestSphereMat.color = new Color(1f, 0.1f, 0.1f, 0.95f);
                } else {
                    debugRestSphereMat = new Material(Shader.Find("Standard"));
                    debugRestSphereMat.color = new Color(1f, 0f, 0f, 0.95f);
                    debugRestSphereMat.EnableKeyword("_EMISSION");
                    debugRestSphereMat.SetColor("_EmissionColor", new Color(0.8f, 0f, 0f));
                    debugRestSphereMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                }
                debugRestSphereMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay + 1000;
            } else {
                debugRestSphereMat = new Material(Shader.Find("Standard"));
                debugRestSphereMat.color = new Color(1f, 0f, 0f, 0.9f);
                debugRestSphereMat.EnableKeyword("_EMISSION");
                debugRestSphereMat.SetColor("_EmissionColor", new Color(0.8f, 0f, 0f));
            }

            var rend = debugRestSphere.GetComponent<Renderer>();
            if (rend != null) rend.material = debugRestSphereMat;
        }

        public void CreateDebugVisuals(Vector3 restPos, Transform parentTransform) {
            DestroyDebugVisuals();

            // 5cm red resting sphere at restPos
            debugRestSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugRestSphere.name = "KeyBinding_DebugRestSphere";
            var col1 = debugRestSphere.GetComponent("Collider");
            if (col1 != null) UnityEngine.Object.Destroy(col1);

            if (parentTransform != null) {
                debugRestSphere.transform.SetParent(parentTransform, false);
            }
            debugRestSphere.transform.localPosition = restPos;
            debugRestSphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f); // 5cm diameter

            UpdateDebugSphereMaterial();
            if (Transform != null && Transform.setupAnchor != null) {
                debugRestSphere.SetActive(Transform.setupAnchor.ShowOrigin);
            }

            UpdateDebugVisuals();
        }

        public void UpdateDebugVisuals() {
            if (Parent == null || debugRestSphere == null || Transform == null || Transform.setupAnchor == null) return;

            bool isLeft = Finger.IsLeftHand();
            Vector3 restPos = isLeft ? Parent.LeftHandRestPosition : Parent.RightHandRestPosition;
            debugRestSphere.transform.localPosition = restPos;
        }

        public void DestroyDebugVisuals() {
            if (debugRestSphere != null) {
                UnityEngine.Object.Destroy(debugRestSphere);
                debugRestSphere = null;
            }
            if (debugRestSphereMat != null) {
                UnityEngine.Object.Destroy(debugRestSphereMat);
                debugRestSphereMat = null;
            }
        }
    }
}
