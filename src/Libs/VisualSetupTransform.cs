using System;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Utils;

namespace FlameStream {
    public class VisualSetupTransform : StructuredData {
        [DataInput]
        [Label("DISPLACEMENT")]
        public Vector3 Position = Vector3.zero;

        [DataInput]
        [Label("ROTATION")]
        public Vector3 Rotation = Vector3.zero;

        [DataInput]
        [Label("SCALE")]
        public Vector3 Scale = Vector3.one;

        [Trigger]
        [Label("MINIMIZE_ROTATION")]
        public void NormalizeRotationAnglesTrigger() {
            NormalizeRotationAngles();
        }

        [Trigger]
        [Hidden]
        public void CopyTrigger() {
            OnCopy?.Invoke();
        }

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

        public bool InVisualSetupMode;
        public string LocalizationKey;
        public VisualSetupAnchorAsset setupAnchor;
        public Action<VisualSetupTransform, VisualSetupAnchorAsset> OnCreateAnchor;
        public Action<VisualSetupTransform, VisualSetupAnchorAsset> OnApplyAnchor;
        public Action<VisualSetupTransform, VisualSetupAnchorAsset> OnResetAnchor;
        public Action<VisualSetupTransform> OnEnterVisualSetup;
        public Action<bool> OnExitVisualSetup;
        public Action OnReset;
        public Action OnCopy;
        public Action<string> OnAnimationChange;

        public bool ShowOriginIndicator = true;
        public bool OriginAlwaysOnTop = false;
        public float OriginIndicatorSize = 0.035f;
        public Vector3 OriginIndicatorOffset = Vector3.zero;
        public GameObject originIndicatorGo;
        public Material originIndicatorMat;
        public bool ShowAnimation = false;

        public void SetOriginIndicatorVisible(bool visible) {
            ShowOriginIndicator = visible;
            if (originIndicatorGo != null) {
                originIndicatorGo.SetActive(visible);
            } else if (visible && setupAnchor != null && setupAnchor.GameObject != null && setupAnchor.GameObject.transform.parent != null) {
                CreateOriginIndicator(setupAnchor.GameObject.transform.parent, OriginIndicatorOffset);
            }
        }

        public void SetOriginAlwaysOnTop(bool alwaysOnTop) {
            OriginAlwaysOnTop = alwaysOnTop;
            UpdateOriginIndicatorMaterial();
        }

        public void UpdateOriginIndicatorMaterial() {
            if (originIndicatorGo == null) return;
            if (originIndicatorMat != null) {
                UnityEngine.Object.Destroy(originIndicatorMat);
                originIndicatorMat = null;
            }

            if (OriginAlwaysOnTop) {
                var overlayShader = Shader.Find("GUI/Text Shader") ?? Shader.Find("Hidden/Internal-Colored");
                if (overlayShader != null) {
                    originIndicatorMat = new Material(overlayShader);
                    originIndicatorMat.color = new Color(1f, 0.1f, 0.1f, 0.95f);
                } else {
                    originIndicatorMat = new Material(Shader.Find("Standard"));
                    originIndicatorMat.color = new Color(1f, 0f, 0f, 0.95f);
                    originIndicatorMat.EnableKeyword("_EMISSION");
                    originIndicatorMat.SetColor("_EmissionColor", new Color(0.85f, 0.05f, 0.05f));
                    originIndicatorMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                }
                originIndicatorMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay + 1000;
            } else {
                originIndicatorMat = new Material(Shader.Find("Standard"));
                originIndicatorMat.color = new Color(1f, 0f, 0f, 0.85f);
                originIndicatorMat.EnableKeyword("_EMISSION");
                originIndicatorMat.SetColor("_EmissionColor", new Color(0.85f, 0.05f, 0.05f));
            }

            var rend = originIndicatorGo.GetComponent<Renderer>();
            if (rend != null) rend.material = originIndicatorMat;
        }

        public void CreateOriginIndicator(Transform parentTransform, Vector3? localPos = null) {
            DestroyOriginIndicator();
            if (parentTransform == null) return;

            originIndicatorGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            originIndicatorGo.name = $"OriginIndicator_{setupAnchor?.Name ?? "Transform"}";
            var col = originIndicatorGo.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);

            originIndicatorGo.transform.SetParent(parentTransform, false);
            originIndicatorGo.transform.localPosition = localPos ?? OriginIndicatorOffset;
            originIndicatorGo.transform.localRotation = Quaternion.identity;
            originIndicatorGo.transform.localScale = Vector3.one * OriginIndicatorSize;

            UpdateOriginIndicatorMaterial();
            originIndicatorGo.SetActive(ShowOriginIndicator);
        }

        public void DestroyOriginIndicator() {
            if (originIndicatorGo != null) {
                UnityEngine.Object.Destroy(originIndicatorGo);
                originIndicatorGo = null;
            }
            if (originIndicatorMat != null) {
                UnityEngine.Object.Destroy(originIndicatorMat);
                originIndicatorMat = null;
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            DestroyOriginIndicator();
            DestroyAnchor();
        }

        public VisualSetupAnchorAsset CreateAnchor() {
            DestroyAnchor();
            setupAnchor = Scene.AddAssetToGroup<VisualSetupAnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            setupAnchor.Parent = this;
            var title = $"SETUP_ANCHOR_NAME_{LocalizationKey}".Localized();
            setupAnchor.Name = $"⌛⚓-{title}";
            setupAnchor.Instructions = $@"
### Instructions

{$"SETUP_ANCHOR_INSTRUCTIONS_{LocalizationKey}".Localized()}
";
            setupAnchor.Transform.Position = Position;
            setupAnchor.Transform.Rotation = Rotation;
            setupAnchor.Transform.Scale = Scale;
            bool isScaleHidden = GetDataInputPort(nameof(Scale))?.Properties?.hidden ?? false;
            setupAnchor.SetHideScale(isScaleHidden);
            setupAnchor.ShowOrigin = ShowOriginIndicator;
            setupAnchor.AlwaysOnTop = OriginAlwaysOnTop;
            setupAnchor.OnShowOriginChange = (show) => {
                SetOriginIndicatorVisible(show);
            };
            setupAnchor.OnAlwaysOnTopChange = (onTop) => {
                SetOriginAlwaysOnTop(onTop);
            };

            OnCreateAnchor?.Invoke(this, setupAnchor);
            setupAnchor.Broadcast();
            Scene.UpdateNewAssetName(setupAnchor);

            if (ShowOriginIndicator && setupAnchor != null && setupAnchor.GameObject != null && setupAnchor.GameObject.transform.parent != null) {
                CreateOriginIndicator(setupAnchor.GameObject.transform.parent);
            }

            setupAnchor.OnAnimationChange = (a) => {
                OnAnimationChange?.Invoke(a);
            };

            if (!ShowAnimation && OnAnimationChange == null) {
                setupAnchor.HideAnimation();
            }

            return setupAnchor;
        }

        public void DestroyAnchor() {
            DestroyOriginIndicator();
            if (setupAnchor == null) return;
            try {
                Scene.RemoveAsset(setupAnchor.Id);
            } catch {}
            setupAnchor = null;
        }

        public void EnterVisualSetup(bool isNavigationWanted) {
            if (InVisualSetupMode) return;
            InVisualSetupMode = true;
            OnEnterVisualSetup?.Invoke(this);
            var a = CreateAnchor();
            if (isNavigationWanted) {
                Context.Service.NavigateToAsset(a.Id);
                Context.Service.BroadcastOpenedScene();
            }
            GetDataInputPort(nameof(Position)).Properties.disabled = true;
            GetDataInputPort(nameof(Rotation)).Properties.disabled = true;
            GetDataInputPort(nameof(Scale)).Properties.disabled = true;
            GetTriggerPort(nameof(EnterVisualSetupTrigger)).Properties.hidden = true;
            GetTriggerPort(nameof(CancelVisualSetupTrigger)).Properties.hidden = false;
            GetTriggerPort(nameof(CancelVisualSetupTrigger)).Properties.description = $"Currently being set up by asset [{a.Name}]";
            BroadcastDataInputProperties(nameof(Position));
            BroadcastDataInputProperties(nameof(Rotation));
            BroadcastDataInputProperties(nameof(Scale));
            BroadcastTriggerProperties(nameof(EnterVisualSetupTrigger));
            BroadcastTriggerProperties(nameof(CancelVisualSetupTrigger));
        }
        public void ExitVisualSetup(bool isApplied) {
            if (!InVisualSetupMode) return;
            InVisualSetupMode = false;
            GetDataInputPort(nameof(Position)).Properties.disabled = false;
            GetDataInputPort(nameof(Rotation)).Properties.disabled = false;
            GetDataInputPort(nameof(Scale)).Properties.disabled = false;
            GetTriggerPort(nameof(EnterVisualSetupTrigger)).Properties.hidden = false;
            GetTriggerPort(nameof(CancelVisualSetupTrigger)).Properties.hidden = true;
            DestroyAnchor();
            BroadcastDataInputProperties(nameof(Position));
            BroadcastDataInputProperties(nameof(Rotation));
            BroadcastDataInputProperties(nameof(Scale));
            BroadcastTriggerProperties(nameof(EnterVisualSetupTrigger));
            BroadcastTriggerProperties(nameof(CancelVisualSetupTrigger));
            OnExitVisualSetup?.Invoke(isApplied);
        }

        public void Apply(VisualSetupAnchorAsset a) {

            Position = a.Transform.Position;
            Rotation = a.Transform.Rotation;
            Scale = a.Transform.Scale;
            OnApplyAnchor?.Invoke(this, a);
            BroadcastDataInput(nameof(Position));
            BroadcastDataInput(nameof(Rotation));
            BroadcastDataInput(nameof(Scale));
            ExitVisualSetup(true);
        }

        public void HideScale() {
            GetDataInputPort(nameof(Scale)).Properties.hidden = true;
            BroadcastDataInputProperties(nameof(Scale));
        }

        public void SetCopyTriggerLabel(string label) {
            GetTriggerPort(nameof(CopyTrigger)).Properties.label = label;
            // GetTriggerPort(nameof(CopyTrigger)).Properties.hidden = false;
            BroadcastTriggerProperties(nameof(CopyTrigger));
        }

        public void NormalizeRotationAngles() {
            SetDataInput(nameof(Rotation), Helper.NormalizeRotationAngles(Rotation), true);
        }

        public void ResetAnchorToOrigin() {
            if (!InVisualSetupMode || setupAnchor == null) return;
            if (OnResetAnchor != null) {
                OnResetAnchor.Invoke(this, setupAnchor);
            } else {
                setupAnchor.Transform.Position = Vector3.zero;
                setupAnchor.Transform.Rotation = Vector3.zero;
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Transform.Broadcast();
            }
        }

        public void ResetToOrigin() {
            ResetAnchorToOrigin();
        }
    }

    public class CollapsibleVisualSetupTransform : VisualSetupTransform, ICollapsibleStructuredData {
        public virtual string GetHeader() {
            var title = !string.IsNullOrEmpty(LocalizationKey) ? LocalizationKey.Localized() : "TRANSFORM".Localized();
            return $"{title} Pos({Position.x:F3}, {Position.y:F3}, {Position.z:F3})";
        }
    }

    public class NaturalPositionVisualSetupTransform : CollapsibleVisualSetupTransform {
        public override string GetHeader() {
            var title = !string.IsNullOrEmpty(LocalizationKey) ? LocalizationKey.Localized() : "COORDINATES".Localized();
            return $"{title} ({Position.x:F3}, {Position.y:F3})";
        }

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Position), delegate {
                Position.z = 0;
                BroadcastDataInput(nameof(Position));
            });
            GetDataInputPort(nameof(Position)).Properties.label = "COORDINATES".Localized();
            GetDataInputPort(nameof(Rotation)).Properties.hidden = true;
            GetDataInputPort(nameof(Scale)).Properties.hidden = true;
            BroadcastDataInputProperties(nameof(Position));
            BroadcastDataInputProperties(nameof(Rotation));

            GetTriggerPort(nameof(NormalizeRotationAnglesTrigger)).Properties.hidden = true;
            BroadcastTriggerProperties(nameof(NormalizeRotationAnglesTrigger));
        }

        protected override void OnUpdate() {
            base.OnUpdate();
            if (!InVisualSetupMode) return;
            // Clamp to screen surface
            setupAnchor.Transform.Position.z = 0;
            setupAnchor.Transform.Rotation = Vector3.zero;
            setupAnchor.BroadcastDataInput(nameof(setupAnchor.Transform));
        }
    }
}
