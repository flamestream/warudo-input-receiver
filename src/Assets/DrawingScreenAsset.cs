using System.Linq;
using uDesktopDuplication;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Plugins.Core.Assets.Prop;

namespace FlameStream {
    [AssetType(Id = "Flamestream.Asset.DrawingScreen", Title = "DRAWING_SCREEN", Category = "FS_ASSET_CATEGORY_INTERNAL")]
    public class DrawingScreenAsset : ScreenAsset {

        /// <summary>
        /// Correction factor for pointer position to match the drawing screen in legacy stable environment.
        /// </summary>
        const float LEGACY_SCREEN_PIXEL_TO_WORLD_FACTOR = 0.00177f;

        public DrawingScreenAsset() {
            ContentType = ScreenContentType.Display;
            Bend = false;
        }

        protected override void OnCreate() {
            base.OnCreate();
            GetDataInputPort(nameof(ContentType)).Properties.alwaysDisabled = true;
            GetDataInputPort(nameof(ContentType)).Properties.disabled = true;
            GetDataInputPort(nameof(DisplayName)).Properties.alwaysDisabled = true;
            GetDataInputPort(nameof(DisplayName)).Properties.disabled = true;
            GetDataInputPort(nameof(DisplayName)).Properties.description = "This property should be set in the FS Pointer Input Receiver Asset";

            Watch(nameof(DisplayName), UpdateCachedMonitor);
        }

        Monitor cachedMonitor = null;
        void UpdateCachedMonitor() {
            cachedMonitor = Manager.monitors.FirstOrDefault(m => m.name == DisplayName);
            ApplyAppearance();
        }

        public Monitor CachedMonitor => cachedMonitor;

        GameObject screenAppearanceGo = null;
        GameObject frameGo = null;
        GameObject bodyGo = null;

        MeshFilter frameMf = null;
        MeshRenderer frameMr = null;
        Material frameMat = null;
        Mesh frameMesh = null;

        MeshFilter bodyMf = null;
        MeshRenderer bodyMr = null;
        Material bodyMat = null;
        Mesh bodyMesh = null;

        PointerDrawingScreenAppearanceSetup cachedAppearance = null;
        Vector3 lastDisplayScale = Vector3.zero;

        Transform GetDisplayTransform() {
            return GameObject != null ? GameObject.transform.Find("Display") : null;
        }

        MeshRenderer GetDisplayRenderer() {
            var tr = GetDisplayTransform();
            return tr != null ? tr.GetComponent<MeshRenderer>() : null;
        }

        public void UpdateAppearance(PointerDrawingScreenAppearanceSetup appearanceSetup) {
            cachedAppearance = appearanceSetup;
            ApplyAppearance();
        }

        public void ApplyAppearance() {
            if (GameObject == null) return;

            var displayTr = GetDisplayTransform();
            var displayMr = GetDisplayRenderer();

            if (cachedAppearance == null || cachedAppearance.RenderMode == DrawingScreenRenderMode.Raw) {
                if (displayMr != null) displayMr.enabled = true;
                if (screenAppearanceGo != null) screenAppearanceGo.SetActive(false);
                return;
            }

            // Outline or OutlineAndRaw
            if (cachedAppearance.RenderMode == DrawingScreenRenderMode.Outline) {
                if (displayMr != null) displayMr.enabled = false;
            } else if (cachedAppearance.RenderMode == DrawingScreenRenderMode.OutlineAndRaw) {
                if (displayMr != null) displayMr.enabled = true;
            }

            EnsureAppearanceGameObjects();
            if (screenAppearanceGo == null) return;
            screenAppearanceGo.SetActive(true);

            // Update position and rotation of appearance relative to display to avoid z-fighting and mirror display orientation
            float depthOffset = cachedAppearance.DepthOffset;
            if (displayTr != null) {
                screenAppearanceGo.transform.localPosition = displayTr.localPosition + displayTr.localRotation * new Vector3(0f, 0f, -depthOffset);
                screenAppearanceGo.transform.localRotation = displayTr.localRotation;
            } else {
                screenAppearanceGo.transform.localPosition = new Vector3(0f, 0f, -depthOffset);
                screenAppearanceGo.transform.localRotation = Quaternion.identity;
            }

            // Get live dimensions of display (accounting for the display mesh bounds, e.g. uDD_Board has bounds size 10x10)
            float meshFactorX = 10f;
            float meshFactorY = 10f;
            if (displayTr != null) {
                var mf = displayTr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) {
                    meshFactorX = mf.sharedMesh.bounds.size.x;
                    meshFactorY = mf.sharedMesh.bounds.size.y;
                }
            }

            float width = 19.2f;
            float height = 10.8f;
            if (displayTr != null && displayTr.localScale.x > 0.0001f && displayTr.localScale.y > 0.0001f) {
                width = displayTr.localScale.x * meshFactorX;
                height = displayTr.localScale.y * meshFactorY;
                lastDisplayScale = displayTr.localScale;
            } else if (cachedMonitor != null) {
                width = cachedMonitor.widthMeter * meshFactorX;
                height = cachedMonitor.heightMeter * meshFactorY;
            }

            Vector4 cornerRadii = cachedAppearance.GetCornerRadii();
            Vector4 borderWidths = cachedAppearance.GetBorderWidths();
            Vector4 boundOffsets = cachedAppearance.GetBoundOffsets();
            int segments = Mathf.Max(2, cachedAppearance.CornerSegments);

            // 1. Frame
            if (frameMesh == null) {
                frameMesh = DrawingScreenMeshBuilder.CreateRoundedRectFrameMesh(width, height, cornerRadii, borderWidths, boundOffsets, segments);
                frameMesh.MarkDynamic();
                if (frameMf != null) frameMf.sharedMesh = frameMesh;
            } else {
                DrawingScreenMeshBuilder.UpdateRoundedRectFrameVertices(frameMesh, width, height, cornerRadii, borderWidths, boundOffsets, segments);
            }

            ApplyMaterialColors(frameMat, cachedAppearance.BorderColor, cachedAppearance.BorderSelfIllumination);

            // 2. Body (Fill)
            if (cachedAppearance.EnableFill) {
                if (bodyGo != null) bodyGo.SetActive(true);
                if (bodyMesh == null) {
                    bodyMesh = DrawingScreenMeshBuilder.CreateRoundedRectMesh(width, height, cornerRadii, boundOffsets, segments);
                    bodyMesh.MarkDynamic();
                    if (bodyMf != null) bodyMf.sharedMesh = bodyMesh;
                } else {
                    DrawingScreenMeshBuilder.UpdateRoundedRectVertices(bodyMesh, width, height, cornerRadii, boundOffsets, segments);
                }
                ApplyMaterialColors(bodyMat, cachedAppearance.FillColor, cachedAppearance.FillSelfIllumination);
            } else {
                if (bodyGo != null) bodyGo.SetActive(false);
            }
        }

        void EnsureAppearanceGameObjects() {
            if (GameObject == null) return;

            if (screenAppearanceGo == null) {
                screenAppearanceGo = new GameObject("ScreenAppearance");
                screenAppearanceGo.transform.SetParent(GameObject.transform, false);
                screenAppearanceGo.transform.localPosition = Vector3.zero;
                screenAppearanceGo.transform.localRotation = Quaternion.identity;
                screenAppearanceGo.transform.localScale = Vector3.one;
            }

            if (bodyGo == null) {
                bodyGo = new GameObject("Body");
                bodyGo.transform.SetParent(screenAppearanceGo.transform, false);
                bodyGo.transform.localPosition = Vector3.zero;
                bodyGo.transform.localRotation = Quaternion.identity;
                bodyGo.transform.localScale = Vector3.one;

                bodyMf = bodyGo.AddComponent<MeshFilter>();
                bodyMr = bodyGo.AddComponent<MeshRenderer>();
                bodyMat = CreateAppearanceMaterial(3000);
                bodyMr.material = bodyMat;
            }

            if (frameGo == null) {
                frameGo = new GameObject("Frame");
                frameGo.transform.SetParent(screenAppearanceGo.transform, false);
                frameGo.transform.localPosition = new Vector3(0f, 0f, -0.00005f);
                frameGo.transform.localRotation = Quaternion.identity;
                frameGo.transform.localScale = Vector3.one;

                frameMf = frameGo.AddComponent<MeshFilter>();
                frameMr = frameGo.AddComponent<MeshRenderer>();
                frameMat = CreateAppearanceMaterial(3001);
                frameMr.material = frameMat;
            }
        }

        Material CreateAppearanceMaterial(int renderQueue) {
            var shader = Shader.Find("Standard");
            var mat = new Material(shader);

            // Transparent rendering
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = renderQueue;

            mat.SetFloat("_Glossiness", 0.0f);
            mat.SetFloat("_Metallic", 0.0f);

            // Emission keyword enabled
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetColor("_Color", Color.white);
            mat.SetColor("_EmissionColor", Color.black);

            return mat;
        }

        void ApplyMaterialColors(Material mat, Color color, float selfIllumination) {
            if (mat == null) return;
            float selfIllum = Mathf.Clamp(selfIllumination, 0f, 5f);
            float dimFactor = Mathf.Clamp01(1f - Mathf.Min(1f, selfIllum));
            Color albedo = new Color(color.r * dimFactor, color.g * dimFactor, color.b * dimFactor, color.a);
            Color emission = color * selfIllum;

            mat.SetColor("_Color", albedo);
            mat.SetColor("_EmissionColor", emission);
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (cachedAppearance != null && cachedAppearance.RenderMode != DrawingScreenRenderMode.Raw) {
                var displayTr = GetDisplayTransform();
                if (displayTr != null && displayTr.localScale != lastDisplayScale) {
                    ApplyAppearance();
                }
            }
        }

        protected override void OnDestroy() {
            base.OnDestroy();

            if (frameMesh != null) {
                UnityEngine.Object.Destroy(frameMesh);
                frameMesh = null;
            }
            if (bodyMesh != null) {
                UnityEngine.Object.Destroy(bodyMesh);
                bodyMesh = null;
            }
            if (frameMat != null) {
                UnityEngine.Object.Destroy(frameMat);
                frameMat = null;
            }
            if (bodyMat != null) {
                UnityEngine.Object.Destroy(bodyMat);
                bodyMat = null;
            }
            if (screenAppearanceGo != null) {
                UnityEngine.Object.Destroy(screenAppearanceGo);
                screenAppearanceGo = null;
            }
        }

        /// <summary>
        /// Converts screen coordinates (x-positive to right, y-positive to down) to local position of Screen child (x-positive to left, y-positive to up)
        /// </summary>
        /// <param name="x">Screen X coordinate (pixels from left edge)</param>
        /// <param name="y">Screen Y coordinate (pixels from top edge)</param>
        /// <returns>Local position in child GameObject space</returns>
        public Vector3 GetChildCursorPosition(int x, int y) {
            if (cachedMonitor == null) return Vector3.zero;

            Vector2? screenSize = this.ScreenSize;
            float xDisplacement;
            float yDisplacement;
            if (screenSize == null) {

                // Legacy Stable environment without ScreenSize property
                var cursorToScreenCenterOffset = new Vector2(
                    (cachedMonitor.left + cachedMonitor.right) * 0.5f,
                    (cachedMonitor.bottom + cachedMonitor.top) * 0.5f
                );
                xDisplacement = -(x - cursorToScreenCenterOffset.x) * Transform.Scale.x * LEGACY_SCREEN_PIXEL_TO_WORLD_FACTOR * 100f;
                yDisplacement = -(y - cursorToScreenCenterOffset.y) * Transform.Scale.y * LEGACY_SCREEN_PIXEL_TO_WORLD_FACTOR * 100f;

            } else {

                // Calculate displacement ratio from middle point from screen coordinates
                int width = cachedMonitor.right - cachedMonitor.left;
                int height = cachedMonitor.bottom - cachedMonitor.top;
                float xRatio = 1f - (float)(x - cachedMonitor.left) / width;
                float yRatio = 1f - (float)(y - cachedMonitor.top) / height;
                float xRatioFromCenter = xRatio - 0.5f;
                float yRatioFromCenter = yRatio - 0.5f;

                float monitorAspectRatio = (float)width / height;

                float definedScreenSizeFactor = Mathf.Min(screenSize.Value.x, screenSize.Value.y * monitorAspectRatio);
                xDisplacement = xRatioFromCenter * Transform.Scale.x * definedScreenSizeFactor;
                yDisplacement = yRatioFromCenter * Transform.Scale.y * definedScreenSizeFactor / monitorAspectRatio;
            }

            return new Vector3(xDisplacement, yDisplacement, 0) * 0.01f;
        }
    }
}
