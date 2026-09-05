using System;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream {
    public class KeyboardKeyBinding : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData {

        [DataInput(10)]
        [Label("KEY")]
        public VKCode Key;

        [DataInput(20)]
        [Label("ASSIGNED_FINGER")]
        public KeyboardFinger Finger;

        [DataInput(30)]
        [Label("TRANSFORM")]
        public VisualSetupTransform Transform;

        [DataInput(40)]
        [Label("ENABLE_HAND_MOVEMENT")]
        public bool EnableHandMovement = true;

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
        GameObject debugOrbitCircle;
        Material debugRestSphereMat;
        Material debugOrbitCircleMat;

        public string GetHeader() {
            var handSide = Finger.IsLeftHand() ? "L" : "R";
            var handMovementTag = EnableHandMovement ? "" : " (No Hand Move)";
            var pos = Transform != null ? Transform.Position : DeltaPosition;
            return $"[{Key}] ➔ [{handSide}] {Finger} ΔPos({pos.x:F3}, {pos.y:F3}, {pos.z:F3}){handMovementTag}";
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

            Watch(nameof(Transform), delegate {
                SetupTransformCallbacks();
            });
            Watch(nameof(Key), delegate {
                BroadcastDataInput(nameof(Transform));
            });
            Watch(nameof(Finger), delegate {
                BroadcastDataInput(nameof(Transform));
            });
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            DestroyDebugVisuals();
            Transform?.DestroyAnchor();
        }

        public void SetupTransformCallbacks() {
            if (Transform == null) return;
            Transform.HideScale();
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
                var dynRotDef = isLeft ? Parent.LeftHandDynamicRotation : Parent.RightHandDynamicRotation;

                Vector3 handScale = isLeft ? Parent.GetLeftHandMovementScale() : Parent.GetRightHandMovementScale();
                Vector3 deltaHand = Vector3.Scale(tr.Position, handScale);
                Vector3 initialPos = restPos + deltaHand;
                Vector3 initialRot = restRot + tr.Rotation;

                setupAnchor.Transform.Position = initialPos;
                setupAnchor.Transform.Rotation = initialRot;
                setupAnchor.Name = $"⌛⚓-Key [{Key}] ({Finger})";
                setupAnchor.Instructions = $@"
### Key Binding Setup: {Key} ({Finger})

Position and rotate this anchor where the **{Finger}** should be placed when pressing key **[{Key}]**.
The avatar's hand and finger will follow this anchor in real-time.
Click **[Apply Transform]** when finished.
";
                setupAnchor.Transform.Broadcast();

                Transform parentTr = root != null && root.GameObject != null ? root.GameObject.transform : null;
                Vector3 sphereCenter = (dynRotDef != null && dynRotDef.SphereCenterOffset != Vector3.zero)
                    ? (restPos + dynRotDef.SphereCenterOffset)
                    : (restPos - new Vector3(0f, 0f, 0.3f));

                bool isDynRotEnabled = dynRotDef != null && dynRotDef.Enabled;
                CreateDebugVisuals(restPos, initialPos, sphereCenter, parentTr, isDynRotEnabled);
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

        public void CreateDebugVisuals(Vector3 restPos, Vector3 keyPos, Vector3 sphereCenter, Transform parentTransform, bool isDynRotEnabled) {
            DestroyDebugVisuals();

            // 1. 5cm red resting sphere at restPos
            debugRestSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugRestSphere.name = "KeyBinding_DebugRestSphere";
            var col1 = debugRestSphere.GetComponent("Collider");
            if (col1 != null) UnityEngine.Object.Destroy(col1);

            if (parentTransform != null) {
                debugRestSphere.transform.SetParent(parentTransform, false);
            }
            debugRestSphere.transform.localPosition = restPos;
            debugRestSphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f); // 5cm diameter

            debugRestSphereMat = new Material(Shader.Find("Standard"));
            debugRestSphereMat.color = new Color(1f, 0f, 0f, 0.9f);
            debugRestSphereMat.EnableKeyword("_EMISSION");
            debugRestSphereMat.SetColor("_EmissionColor", new Color(0.8f, 0f, 0f));
            debugRestSphere.GetComponent<Renderer>().material = debugRestSphereMat;

            // 2. Dimmed red circle representing the rotation offset arc (only created if dynamic rotation is enabled)
            if (isDynRotEnabled) {
                debugOrbitCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                debugOrbitCircle.name = "KeyBinding_DebugOrbitCircle";
                var col2 = debugOrbitCircle.GetComponent("Collider");
                if (col2 != null) UnityEngine.Object.Destroy(col2);

                if (parentTransform != null) {
                    debugOrbitCircle.transform.SetParent(parentTransform, false);
                }

                debugOrbitCircleMat = new Material(Shader.Find("Standard"));
                debugOrbitCircleMat.SetFloat("_Mode", 3);
                debugOrbitCircleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                debugOrbitCircleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                debugOrbitCircleMat.SetInt("_ZWrite", 0);
                debugOrbitCircleMat.DisableKeyword("_ALPHATEST_ON");
                debugOrbitCircleMat.EnableKeyword("_ALPHABLEND_ON");
                debugOrbitCircleMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                debugOrbitCircleMat.renderQueue = 3000;
                debugOrbitCircleMat.color = new Color(1f, 0f, 0f, 0.35f); // Dimmed red circle
                debugOrbitCircle.GetComponent<Renderer>().material = debugOrbitCircleMat;
            }

            UpdateDebugVisuals();
        }

        public void UpdateDebugVisuals() {
            if (Parent == null || debugRestSphere == null || Transform == null || Transform.setupAnchor == null) return;

            bool isLeft = Finger.IsLeftHand();
            Vector3 restPos = isLeft ? Parent.LeftHandRestPosition : Parent.RightHandRestPosition;
            Vector3 anchorPos = Transform.setupAnchor.Transform.Position;
            var dynRotDef = isLeft ? Parent.LeftHandDynamicRotation : Parent.RightHandDynamicRotation;
            bool isDynRotEnabled = dynRotDef != null && dynRotDef.Enabled;

            debugRestSphere.transform.localPosition = restPos;

            if (!isDynRotEnabled) {
                if (debugOrbitCircle != null) {
                    debugOrbitCircle.SetActive(false);
                }
                return;
            }

            if (debugOrbitCircle != null) {
                debugOrbitCircle.SetActive(true);
            }

            Vector3 sphereCenter = (dynRotDef != null && dynRotDef.SphereCenterOffset != Vector3.zero)
                ? (restPos + dynRotDef.SphereCenterOffset)
                : (restPos - new Vector3(0f, 0f, 0.3f));

            if (debugOrbitCircle != null) {
                debugOrbitCircle.transform.localPosition = sphereCenter;

                Vector3 delta = restPos - sphereCenter;
                float radius = delta.magnitude;
                float diameter = Mathf.Max(radius * 2f, 0.05f);

                debugOrbitCircle.transform.localScale = new Vector3(diameter, 0.0005f, diameter);

                if (delta.sqrMagnitude > 0.0001f) {
                    Vector3 normal = Vector3.Cross(delta, Vector3.right);
                    if (normal.sqrMagnitude < 0.001f) {
                        normal = Vector3.Cross(delta, Vector3.up);
                    }
                    if (normal.sqrMagnitude > 0.001f) {
                        debugOrbitCircle.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal.normalized);
                    } else {
                        debugOrbitCircle.transform.localRotation = Quaternion.identity;
                    }
                } else {
                    debugOrbitCircle.transform.localRotation = Quaternion.identity;
                }
            }
        }

        public void DestroyDebugVisuals() {
            if (debugRestSphere != null) {
                UnityEngine.Object.Destroy(debugRestSphere);
                debugRestSphere = null;
            }
            if (debugOrbitCircle != null) {
                UnityEngine.Object.Destroy(debugOrbitCircle);
                debugOrbitCircle = null;
            }
            if (debugRestSphereMat != null) {
                UnityEngine.Object.Destroy(debugRestSphereMat);
                debugRestSphereMat = null;
            }
            if (debugOrbitCircleMat != null) {
                UnityEngine.Object.Destroy(debugOrbitCircleMat);
                debugOrbitCircleMat = null;
            }
        }
    }
}
