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

        public DrawingScreenAsset()
        {
            ContentType = ScreenContentType.Display;
            Bend = false;
        }

        /// <summary>
        /// Attempts to get the ScreenSize property from the base class using dynamic typing.
        /// Returns null if the property doesn't exist (Stable environment) or if an exception occurs.
        /// </summary>
        public Vector2? TryGetScreenSize() {
            try {
                // Try to access ScreenSize property from base class (available in Beta)
                return ((dynamic)this).ScreenSize;
            } catch {
                // ScreenSize property doesn't exist (Stable environment) or threw an exception
                return null;
            }
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
        }

        public Monitor CachedMonitor => cachedMonitor;

        /// <summary>
        /// Converts screen coordinates (x-positive to right, y-positive to down) to local position of Screen child (x-positive to left, y-positive to up)
        /// </summary>
        /// <param name="x">Screen X coordinate (pixels from left edge)</param>
        /// <param name="y">Screen Y coordinate (pixels from top edge)</param>
        /// <returns>Local position in child GameObject space</returns>
        public Vector3 GetChildCursorPosition(int x, int y) {
            if (cachedMonitor == null) return Vector3.zero;

            Vector2? screenSize = TryGetScreenSize();
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
