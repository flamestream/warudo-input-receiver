using System;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Localization;

namespace FlameStream {
    public class KeyBindingVisualSetupTransform : VisualSetupTransform {
        public KeyboardKeyBinding KeyBinding;

        public override VisualSetupAnchorAsset CreateAnchor() {
            DestroyAnchor();
            var anchor = Scene.AddAssetToGroup<KeyBindingVisualSetupAnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            setupAnchor = anchor;
            anchor.Parent = this;
            var title = $"SETUP_ANCHOR_NAME_{LocalizationKey}".Localized();
            anchor.Name = $"⌛⚓-{title}";
            anchor.Instructions = $@"
### Instructions

{$"SETUP_ANCHOR_INSTRUCTIONS_{LocalizationKey}".Localized()}
";
            anchor.Transform.Position = Position;
            anchor.Transform.Rotation = Rotation;
            anchor.Transform.Scale = Scale;
            bool isScaleHidden = GetDataInputPort(nameof(Scale))?.Properties?.hidden ?? false;
            anchor.SetHideScale(isScaleHidden);
            anchor.ShowOrigin = ShowOriginIndicator;
            anchor.AlwaysOnTop = OriginAlwaysOnTop;
            anchor.OnShowOriginChange = (show) => SetOriginIndicatorVisible(show);
            anchor.OnAlwaysOnTopChange = (onTop) => SetOriginAlwaysOnTop(onTop);
            anchor.OnOriginSphereChange = () => {
                if (originIndicatorGo != null) {
                    float r = anchor.OriginSphere?.RadiusFactor ?? 1.0f;
                    originIndicatorGo.transform.localScale = Vector3.one * (OriginIndicatorSize * r);
                }
                UpdateOriginIndicatorMaterial();
            };

            if (KeyBinding != null) {
                anchor.OnAutoAlignAll = () => KeyBinding.AutoAlignKeyBinding();
                anchor.OnAlignHandWithSurface = () => KeyBinding.AlignHandWithButtonSurface();
                anchor.OnMoveFingerToButton = () => KeyBinding.MoveFingerToButton();
                anchor.OnRotateHandToNaturalRotation = () => KeyBinding.RotateHandToNaturalRotation();
                anchor.OnSettingsChanged = () => KeyBinding.UpdateDebugVisuals();
            }

            OnCreateAnchor?.Invoke(this, anchor);
            anchor.Broadcast();
            Scene.UpdateNewAssetName(anchor);

            if (ShowOriginIndicator && anchor.GameObject != null && anchor.GameObject.transform.parent != null) {
                CreateOriginIndicator(anchor.GameObject.transform.parent);
            }

            anchor.OnAnimationChange = (a) => OnAnimationChange?.Invoke(a);
            if (!ShowAnimation && OnAnimationChange == null) {
                anchor.HideAnimation();
            }

            return anchor;
        }
    }
}
