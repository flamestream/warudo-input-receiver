using System;
using System.Globalization;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Plugins.Core.Assets.Utility;

namespace FlameStream {
    public class DynamicVector3 : StructuredData {

        [DataInput(10)]
        [Label("ENABLE")]
        public bool Enabled;

        [DataInput(15)]
        [Label("DYNAMIC_ROTATION_AUTO")]
        [Description("DYNAMIC_ROTATION_AUTO_DESC")]
        public bool Auto = true;

        [DataInput(16)]
        [Label("AUTO_ROTATION_MAX_SPEED")]
        [Description("AUTO_ROTATION_MAX_SPEED_DESC")]
        [FloatSlider(30f, 720f, 10f)]
        public float AutoMaxSpeed = 360f;

        [DataInput(17)]
        [Label("AUTO_ROTATION_SETTLE_FRAMES")]
        [Description("AUTO_ROTATION_SETTLE_FRAMES_DESC")]
        [IntegerSlider(0, 30)]
        public int AutoSettleFrames = 16;

        public virtual bool HasAutoMode => false;
        public Func<Vector3, Vector3> AutoRotationEvaluator;

        [Trigger]
        [Label("ENTER_VISUAL_SETUP")]
        public void EnterVisualSetupTrigger() {
            EnterVisualSetup(true);
        }

        [Trigger]
        [Label("CANCEL_VISUAL_SETUP")]
        [Hidden]
        public void CancelVisualSetupTrigger() {
            ExitVisualSetup(false);
        }

        [DataInput(20)]
        [Hidden]
        public DistanceInputMathExpression X;
        [DataInput(30)]
        [Hidden]
        public DistanceInputMathExpression Y;
        [DataInput(40)]
        [Hidden]
        public DistanceInputMathExpression Z;

        [DataInput]
        [Hidden]
        public Vector3 SphereCenterOffset = Vector3.zero;

        public bool InVisualSetupMode;
        public VisualSetupAnchorAsset setupAnchor;
        public string LocalizationKey;
        public Func<Vector3> GetRestPosition;
        public Func<AnchorAsset> GetParentAnchor;
        public Action<DynamicVector3> OnEnterVisualSetup;
        public Action<bool> OnExitVisualSetup;

        // Visual guide objects
        GameObject debugRestSphere;
        GameObject debugOrbitCircle;
        Material debugRestSphereMat;
        Material debugOrbitCircleMat;

        public Vector3 Evaluate(Vector3 v) {
            Vector3 result = Vector3.zero;
            if (Enabled) {
                if (HasAutoMode && Auto && AutoRotationEvaluator != null) {
                    result = AutoRotationEvaluator(v);
                } else {
                    result = new Vector3(
                        X != null ? X.Evaluate(v) : 0f,
                        Y != null ? Y.Evaluate(v) : 0f,
                        Z != null ? Z.Evaluate(v) : 0f
                    );
                }
            }
            if (debugMode) {
                DebugMessage = $@"
dx: {v.x:F3}

dy: {v.y:F3}

dz: {v.z:F3}

result: {result}
";
                BroadcastDataInput(nameof(DebugMessage));
            }
            return result;
        }

        [Markdown]
        public string DebugMessage;

        [Trigger]
        [Hidden]
        [Label("TOGGLE_DEBUG_MODE")]
        public void ToggleDebugModeTrigger() {
            debugMode = !debugMode;
            if (!debugMode) {
                DebugMessage = null;
                BroadcastDataInput(nameof(DebugMessage));
            }
        }
        bool debugMode;

        protected override void OnCreate() {
            base.OnCreate();
            if (X == null) X = StructuredData.Create<DistanceInputMathExpression>();
            if (Y == null) Y = StructuredData.Create<DistanceInputMathExpression>();
            if (Z == null) Z = StructuredData.Create<DistanceInputMathExpression>();
            Watch(nameof(Enabled), delegate { OnEnabledChanged(); });
            Watch(nameof(Auto), delegate { OnEnabledChanged(); });
            OnEnabledChanged();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            DestroyAnchor();
            DestroyDebugVisuals();
        }

        public void EnterVisualSetup(bool isNavigationWanted) {
            if (InVisualSetupMode) return;
            InVisualSetupMode = true;
            OnEnterVisualSetup?.Invoke(this);

            var parentAnchor = GetParentAnchor?.Invoke();
            Vector3 restPos = GetRestPosition != null ? GetRestPosition() : Vector3.zero;

            Vector3 initialAnchorPos = (SphereCenterOffset != Vector3.zero)
                ? (restPos + SphereCenterOffset)
                : restPos;

            DestroyAnchor();
            setupAnchor = Scene.AddAssetToGroup<VisualSetupAnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            setupAnchor.Parent = null;
            var title = !string.IsNullOrEmpty(LocalizationKey) ? $"SETUP_ANCHOR_NAME_{LocalizationKey}".Localized() : "Dynamic Rotation Pivot";
            setupAnchor.Name = $"⌛⚓-{title}";
            setupAnchor.Instructions = $@"
### Instructions

{(!string.IsNullOrEmpty(LocalizationKey) ? $"SETUP_ANCHOR_INSTRUCTIONS_{LocalizationKey}".Localized() : "Position this anchor at the sphere center of rotation.")}
";
            if (parentAnchor != null) {
                setupAnchor.Attachable.Parent = parentAnchor;
                if (parentAnchor.GameObject != null) {
                    setupAnchor.GameObject.transform.SetParent(parentAnchor.GameObject.transform, false);
                }
            }

            setupAnchor.Transform.Position = initialAnchorPos;
            setupAnchor.Transform.Rotation = Vector3.zero;
            setupAnchor.Transform.Broadcast();
            Scene.UpdateNewAssetName(setupAnchor);

            setupAnchor.ApplyTriggerAction = () => Apply(setupAnchor);
            setupAnchor.CancelTriggerAction = () => ExitVisualSetup(false);

            Transform parentTr = parentAnchor != null && parentAnchor.GameObject != null ? parentAnchor.GameObject.transform : null;
            CreateDebugVisuals(restPos, initialAnchorPos, parentTr);

            if (isNavigationWanted) {
                Context.Service.NavigateToAsset(setupAnchor.Id);
                Context.Service.BroadcastOpenedScene();
            }

            GetTriggerPort(nameof(EnterVisualSetupTrigger)).Properties.hidden = true;
            GetTriggerPort(nameof(CancelVisualSetupTrigger)).Properties.hidden = false;
            GetTriggerPort(nameof(CancelVisualSetupTrigger)).Properties.description = $"Currently being set up by asset [{setupAnchor.Name}]";
            BroadcastTriggerProperties(nameof(EnterVisualSetupTrigger));
            BroadcastTriggerProperties(nameof(CancelVisualSetupTrigger));
        }

        public void ExitVisualSetup(bool isApplied) {
            if (!InVisualSetupMode) return;
            InVisualSetupMode = false;
            DestroyAnchor();
            DestroyDebugVisuals();

            GetTriggerPort(nameof(EnterVisualSetupTrigger)).Properties.hidden = false;
            GetTriggerPort(nameof(CancelVisualSetupTrigger)).Properties.hidden = true;
            BroadcastTriggerProperties(nameof(EnterVisualSetupTrigger));
            BroadcastTriggerProperties(nameof(CancelVisualSetupTrigger));

            OnExitVisualSetup?.Invoke(isApplied);
        }

        public void DestroyAnchor() {
            if (setupAnchor == null) return;
            try {
                Scene.RemoveAsset(setupAnchor.Id);
            } catch {}
            setupAnchor = null;
        }

        public void Apply(VisualSetupAnchorAsset a) {
            Vector3 restPos = GetRestPosition != null ? GetRestPosition() : Vector3.zero;
            Vector3 anchorPos = a.Transform.Position;

            SphereCenterOffset = anchorPos - restPos;
            BroadcastDataInput(nameof(SphereCenterOffset));

            // R is vector from sphere center to resting hand
            Vector3 R = restPos - anchorPos;
            if (R.sqrMagnitude < 0.0001f) {
                R = new Vector3(0f, 0f, 0.3f);
            }

            string rx = R.x.ToString("0.######", CultureInfo.InvariantCulture);
            string ry = R.y.ToString("0.######", CultureInfo.InvariantCulture);
            string rz = R.z.ToString("0.######", CultureInfo.InvariantCulture);

            if (X == null) X = StructuredData.Create<DistanceInputMathExpression>();
            if (Y == null) Y = StructuredData.Create<DistanceInputMathExpression>();
            if (Z == null) Z = StructuredData.Create<DistanceInputMathExpression>();

            // Pitch (X): sagittal vertical tilt in Y-Z plane
            X.Expression = $"-(atan2({ry} + dy, {rz} + dz) - atan2({ry}, {rz})) * (180 / pi)";
            X.OnExpressionChange();
            X.BroadcastDataInput(nameof(X.Expression));

            // Yaw (Y): horizontal sweep in X-Z plane
            Y.Expression = $"(atan2({rx} + dx, {rz} + dz) - atan2({rx}, {rz})) * (180 / pi)";
            Y.OnExpressionChange();
            Y.BroadcastDataInput(nameof(Y.Expression));

            // Roll (Z): frontal bank in X-Y plane
            Z.Expression = $"-(atan2({ry} + dy, {rx} + dx) - atan2({ry}, {rx})) * (180 / pi)";
            Z.OnExpressionChange();
            Z.BroadcastDataInput(nameof(Z.Expression));

            Enabled = true;
            BroadcastDataInput(nameof(Enabled));
            BroadcastDataInput(nameof(X));
            BroadcastDataInput(nameof(Y));
            BroadcastDataInput(nameof(Z));
            OnEnabledChanged();

            ExitVisualSetup(true);
        }

        void CreateDebugVisuals(Vector3 restPos, Vector3 anchorPos, Transform parentTransform) {
            DestroyDebugVisuals();

            // 1. 5cm red resting sphere at restPos
            debugRestSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            debugRestSphere.name = "DynamicRotation_DebugRestSphere";
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

            // 2. 80% opacity red circle centered at anchorPos
            debugOrbitCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            debugOrbitCircle.name = "DynamicRotation_DebugOrbitCircle";
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
            debugOrbitCircleMat.color = new Color(1f, 0f, 0f, 0.8f);
            debugOrbitCircle.GetComponent<Renderer>().material = debugOrbitCircleMat;

            UpdateDebugVisuals();
        }

        public void UpdateDebugVisuals() {
            if (debugOrbitCircle == null || debugRestSphere == null) return;

            Vector3 restPos = GetRestPosition != null ? GetRestPosition() : Vector3.zero;
            Vector3 anchorPos = setupAnchor != null ? setupAnchor.Transform.Position : restPos;

            debugRestSphere.transform.localPosition = restPos;
            debugOrbitCircle.transform.localPosition = anchorPos;

            Vector3 delta = restPos - anchorPos;
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

        protected virtual void OnEnabledChanged() {
            bool showAuto = Enabled && HasAutoMode;
            GetDataInputPort(nameof(Auto)).Properties.hidden = !showAuto;

            bool showAutoSettings = showAuto && Auto;
            GetDataInputPort(nameof(AutoMaxSpeed)).Properties.hidden = !showAutoSettings;
            GetDataInputPort(nameof(AutoSettleFrames)).Properties.hidden = !showAutoSettings;

            bool showManual = Enabled && (!HasAutoMode || !Auto);
            GetDataInputPort(nameof(X)).Properties.hidden = !showManual;
            GetDataInputPort(nameof(Y)).Properties.hidden = !showManual;
            GetDataInputPort(nameof(Z)).Properties.hidden = !showManual;
            GetDataInputPort(nameof(DebugMessage)).Properties.hidden = !showManual;
            GetTriggerPort(nameof(ToggleDebugModeTrigger)).Properties.hidden = !showManual;
            GetTriggerPort(nameof(EnterVisualSetupTrigger)).Properties.hidden = !showManual;

            BroadcastDataInputProperties(nameof(Auto));
            BroadcastDataInputProperties(nameof(AutoMaxSpeed));
            BroadcastDataInputProperties(nameof(AutoSettleFrames));
            BroadcastDataInputProperties(nameof(X));
            BroadcastDataInputProperties(nameof(Y));
            BroadcastDataInputProperties(nameof(Z));
            BroadcastDataInputProperties(nameof(DebugMessage));
            BroadcastTriggerProperties(nameof(ToggleDebugModeTrigger));
            BroadcastTriggerProperties(nameof(EnterVisualSetupTrigger));
        }
    }

    public class HandRotationDynamicVector3 : DynamicVector3 {
        public override bool HasAutoMode => true;

        public HandRotationDynamicVector3() {
            X = StructuredData.Create<DistanceInputMathExpression>((me) => {
                me.Expression = "min(2 * abs(dx), 1) * (atan2(abs(dy), abs(dx)) * (180 / pi) - 90) * sign(dx)";
            });
            X.OnExpressionChange();
        }
    }

    public class BodyRotationDynamicVector3 : DynamicVector3 {
        public BodyRotationDynamicVector3() {
            Z = StructuredData.Create<DistanceInputMathExpression>((me) => {
                me.Expression = "min(0.15 * abs(dx), 1) * atan2(-abs(dx), abs(dy)) * 4 / pi * (180 / pi) * sign(dx)";
            });
            Z.OnExpressionChange();
        }
    }
}
