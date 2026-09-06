using System;
using DG.Tweening;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream
{
    public class CornerRadiiData : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [FloatSlider(0f, 1f, 0.01f)]
        [Label("CORNER_ROUNDNESS")]
        public float Radius = 0.25f;

        [DataInput(20)]
        [Label("USE_INDIVIDUAL_CORNERS")]
        public bool UseIndividualCorners = false;

        [DataInput(30)]
        [Label("TOP_CORNERS")] // TL, TR
        public Vector2 TopCorners = new Vector2(0.25f, 0.25f);

        [DataInput(40)]
        [Label("BOTTOM_CORNERS")] // BL, BR
        public Vector2 BottomCorners = new Vector2(0.25f, 0.25f);

        public Action OnChanged;

        protected override void OnCreate() {
            base.OnCreate();
            SyncUI();
            Watch(nameof(Radius), delegate { OnChanged?.Invoke(); });
            Watch(nameof(UseIndividualCorners), delegate {
                if (UseIndividualCorners) {
                    TopCorners = new Vector2(Radius, Radius);
                    BottomCorners = new Vector2(Radius, Radius);
                    BroadcastDataInput(nameof(TopCorners));
                    BroadcastDataInput(nameof(BottomCorners));
                }
                SyncUI();
                OnChanged?.Invoke();
            });
            Watch(nameof(TopCorners), delegate { OnChanged?.Invoke(); });
            Watch(nameof(BottomCorners), delegate { OnChanged?.Invoke(); });
        }

        public Vector4 GetRadii() {
            if (UseIndividualCorners) {
                // Return TL, TR, BR, BL order
                return new Vector4(TopCorners.x, TopCorners.y, BottomCorners.y, BottomCorners.x);
            }
            return BorderVector4Extensions.Uniform(Radius);
        }

        public void SyncUI() {
            try {
                void SetHidden(string portName, bool hidden) {
                    var port = GetDataInputPort(portName);
                    if (port != null) {
                        port.Properties.hidden = hidden;
                        BroadcastDataInputProperties(portName);
                    }
                }
                SetHidden(nameof(Radius), UseIndividualCorners);
                SetHidden(nameof(TopCorners), !UseIndividualCorners);
                SetHidden(nameof(BottomCorners), !UseIndividualCorners);
            } catch { }
        }

        public void BroadcastAll() {
            try {
                BroadcastDataInput(nameof(Radius));
                BroadcastDataInput(nameof(UseIndividualCorners));
                BroadcastDataInput(nameof(TopCorners));
                BroadcastDataInput(nameof(BottomCorners));
                SyncUI();
            } catch { }
        }

        public virtual string GetHeader() {
            if (!UseIndividualCorners) return $"Round: {Radius:F2}";
            return $"Corners (TL:{TopCorners.x:F2} TR:{TopCorners.y:F2} BL:{BottomCorners.x:F2} BR:{BottomCorners.y:F2})";
        }

        public void CopyFrom(CornerRadiiData other) {
            if (other == null) return;
            Radius = other.Radius;
            UseIndividualCorners = other.UseIndividualCorners;
            TopCorners = other.TopCorners;
            BottomCorners = other.BottomCorners;
        }

        public CornerRadiiData Clone() {
            var clone = StructuredData.Create<CornerRadiiData>();
            clone.CopyFrom(this);
            return clone;
        }
    }

    public class BorderThicknessData : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [FloatSlider(0.0005f, 0.015f, 0.0002f)]
        [Label("BORDER_WIDTH")]
        public float Width = 0.0025f;

        [DataInput(20)]
        [Label("USE_INDIVIDUAL_BORDERS")]
        public bool UseIndividualBorders = false;

        [DataInput(30)]
        [Label("TOP_BOTTOM_BORDERS")] // Top, Bottom
        public Vector2 TopBottomBorders = new Vector2(0.0025f, 0.0025f);

        [DataInput(40)]
        [Label("LEFT_RIGHT_BORDERS")] // Left, Right
        public Vector2 LeftRightBorders = new Vector2(0.0025f, 0.0025f);

        public Action OnChanged;

        protected override void OnCreate() {
            base.OnCreate();
            SyncUI();
            Watch(nameof(Width), delegate { OnChanged?.Invoke(); });
            Watch(nameof(UseIndividualBorders), delegate {
                if (UseIndividualBorders) {
                    TopBottomBorders = new Vector2(Width, Width);
                    LeftRightBorders = new Vector2(Width, Width);
                    BroadcastDataInput(nameof(TopBottomBorders));
                    BroadcastDataInput(nameof(LeftRightBorders));
                }
                SyncUI();
                OnChanged?.Invoke();
            });
            Watch(nameof(TopBottomBorders), delegate { OnChanged?.Invoke(); });
            Watch(nameof(LeftRightBorders), delegate { OnChanged?.Invoke(); });
        }

        public Vector4 GetWidths() {
            if (UseIndividualBorders) {
                // Return Top, Right, Bottom, Left order
                return new Vector4(TopBottomBorders.x, LeftRightBorders.y, TopBottomBorders.y, LeftRightBorders.x);
            }
            return BorderVector4Extensions.Uniform(Width);
        }

        public void SyncUI() {
            try {
                void SetHidden(string portName, bool hidden) {
                    var port = GetDataInputPort(portName);
                    if (port != null) {
                        port.Properties.hidden = hidden;
                        BroadcastDataInputProperties(portName);
                    }
                }
                SetHidden(nameof(Width), UseIndividualBorders);
                SetHidden(nameof(TopBottomBorders), !UseIndividualBorders);
                SetHidden(nameof(LeftRightBorders), !UseIndividualBorders);
            } catch { }
        }

        public void BroadcastAll() {
            try {
                BroadcastDataInput(nameof(Width));
                BroadcastDataInput(nameof(UseIndividualBorders));
                BroadcastDataInput(nameof(TopBottomBorders));
                BroadcastDataInput(nameof(LeftRightBorders));
                SyncUI();
            } catch { }
        }

        public virtual string GetHeader() {
            if (!UseIndividualBorders) return $"Border: {Width * 1000f:F1}mm";
            return $"Borders (T:{TopBottomBorders.x * 1000f:F1} B:{TopBottomBorders.y * 1000f:F1} L:{LeftRightBorders.x * 1000f:F1} R:{LeftRightBorders.y * 1000f:F1}mm)";
        }

        public void CopyFrom(BorderThicknessData other) {
            if (other == null) return;
            Width = other.Width;
            UseIndividualBorders = other.UseIndividualBorders;
            TopBottomBorders = other.TopBottomBorders;
            LeftRightBorders = other.LeftRightBorders;
        }

        public BorderThicknessData Clone() {
            var clone = StructuredData.Create<BorderThicknessData>();
            clone.CopyFrom(this);
            return clone;
        }
    }

    public class VirtualKeyShapeStateStyle : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(5)]
        [Label("ENABLED")]
        public bool Enabled = true;

        [DataInput(10)]
        [Label("SIZE")]
        public Vector2 Size = new Vector2(0.032f, 0.032f);

        [DataInput(20)]
        [Label("CORNERS")]
        public CornerRadiiData Corners;

        [DataInput(30)]
        [Label("BORDERS")]
        public BorderThicknessData Borders;

        public bool ShowEnabledToggle = true;
        public Action OnChanged;

        protected override void OnCreate() {
            base.OnCreate();
            if (Corners == null) Corners = StructuredData.Create<CornerRadiiData>();
            if (Borders == null) Borders = StructuredData.Create<BorderThicknessData>();

            Corners.OnChanged = () => OnChanged?.Invoke();
            Borders.OnChanged = () => OnChanged?.Invoke();

            SyncUI();
            Watch(nameof(Enabled), delegate { SyncUI(); OnChanged?.Invoke(); });
            Watch(nameof(Size), delegate { OnChanged?.Invoke(); });
            Watch(nameof(Corners), delegate {
                if (Corners != null) Corners.OnChanged = () => OnChanged?.Invoke();
                OnChanged?.Invoke();
            });
            Watch(nameof(Borders), delegate {
                if (Borders != null) Borders.OnChanged = () => OnChanged?.Invoke();
                OnChanged?.Invoke();
            });
        }

        public Vector4 GetCornerRadii() => Corners != null ? Corners.GetRadii() : BorderVector4Extensions.Uniform(0.25f);
        public Vector4 GetBorderWidths() => Borders != null ? Borders.GetWidths() : BorderVector4Extensions.Uniform(0.0025f);

        public void SyncUI() {
            try {
                var enabledPort = GetDataInputPort(nameof(Enabled));
                if (enabledPort != null) {
                    enabledPort.Properties.hidden = !ShowEnabledToggle;
                    BroadcastDataInputProperties(nameof(Enabled));
                }

                bool hideFields = ShowEnabledToggle && !Enabled;

                void SetHidden(string portName, bool hidden) {
                    var port = GetDataInputPort(portName);
                    if (port != null) {
                        port.Properties.hidden = hidden;
                        BroadcastDataInputProperties(portName);
                    }
                }

                SetHidden(nameof(Size), hideFields);
                SetHidden(nameof(Corners), hideFields);
                SetHidden(nameof(Borders), hideFields);
                Corners?.SyncUI();
                Borders?.SyncUI();
            } catch { }
        }

        public void BroadcastAll() {
            try {
                BroadcastDataInput(nameof(Enabled));
                BroadcastDataInput(nameof(Size));
                Corners?.BroadcastAll();
                Borders?.BroadcastAll();
                BroadcastDataInput(nameof(Corners));
                BroadcastDataInput(nameof(Borders));
                SyncUI();
            } catch { }
        }

        public virtual string GetHeader() {
            if (ShowEnabledToggle && !Enabled) return "(Default / Inherited)";
            string sizeStr = $"{Size.x * 1000f:F0}x{Size.y * 1000f:F0}mm";
            string roundStr = Corners != null ? Corners.GetHeader() : "Default Corners";
            string borderStr = Borders != null ? Borders.GetHeader() : "Default Border";
            return $"{sizeStr} | {borderStr} | {roundStr}";
        }

        public void CopyFrom(VirtualKeyShapeStateStyle other) {
            if (other == null) return;
            Enabled = other.Enabled;
            Size = other.Size;
            if (Corners == null) Corners = StructuredData.Create<CornerRadiiData>();
            if (Borders == null) Borders = StructuredData.Create<BorderThicknessData>();
            Corners.CopyFrom(other.Corners);
            Borders.CopyFrom(other.Borders);
        }

        public VirtualKeyShapeStateStyle Clone() {
            var clone = StructuredData.Create<VirtualKeyShapeStateStyle>();
            clone.CopyFrom(this);
            return clone;
        }
    }

    public class VirtualKeyShapeStyle : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(5)]
        [Label("ENABLED")]
        public bool Enabled = true;

        [DataInput(10)]
        [Label("NORMAL_STATE")]
        public VirtualKeyShapeStateStyle Normal;

        [DataInput(20)]
        [Label("ENABLE_PRESSED_STATE")]
        public bool EnablePressedState = true;

        [DataInput(30)]
        [Label("PRESSED_STATE")]
        public VirtualKeyShapeStateStyle Pressed;

        public bool IsBaseAppearance = true;
        public Action OnChanged;

        protected override void OnCreate() {
            base.OnCreate();
            if (Normal == null) Normal = StructuredData.Create<VirtualKeyShapeStateStyle>();
            if (Pressed == null) Pressed = StructuredData.Create<VirtualKeyShapeStateStyle>();

            Normal.OnChanged = () => OnChanged?.Invoke();
            Pressed.OnChanged = () => OnChanged?.Invoke();

            ConfigureStates(IsBaseAppearance);
            SyncUI();

            Watch(nameof(Enabled), delegate {
                if (Normal != null) Normal.Enabled = Enabled;
                SyncUI();
                OnChanged?.Invoke();
            });
            Watch(nameof(EnablePressedState), delegate {
                if (Pressed != null) {
                    Pressed.Enabled = EnablePressedState;
                    Pressed.SyncUI();
                }
                SyncUI();
                OnChanged?.Invoke();
            });
            Watch(nameof(Normal), delegate {
                if (Normal != null) Normal.OnChanged = () => OnChanged?.Invoke();
                OnChanged?.Invoke();
            });
            Watch(nameof(Pressed), delegate {
                if (Pressed != null) Pressed.OnChanged = () => OnChanged?.Invoke();
                OnChanged?.Invoke();
            });
        }

        public void ConfigureStates(bool isBaseAppearance = true) {
            IsBaseAppearance = isBaseAppearance;
            var enabledPort = GetDataInputPort(nameof(Enabled));
            if (enabledPort != null) {
                enabledPort.Properties.hidden = isBaseAppearance;
                BroadcastDataInputProperties(nameof(Enabled));
            }

            if (Normal != null) {
                Normal.ShowEnabledToggle = false;
                Normal.SyncUI();
            }
            if (Pressed != null) {
                Pressed.ShowEnabledToggle = false;
                Pressed.SyncUI();
            }
        }

        public void BroadcastAll() {
            try {
                BroadcastDataInput(nameof(Enabled));
                BroadcastDataInput(nameof(EnablePressedState));
                Normal?.BroadcastAll();
                Pressed?.BroadcastAll();
                BroadcastDataInput(nameof(Normal));
                BroadcastDataInput(nameof(Pressed));
                SyncUI();
            } catch { }
        }

        public void SyncUI() {
            try {
                var pressedPort = GetDataInputPort(nameof(Pressed));
                if (pressedPort != null) {
                    pressedPort.Properties.hidden = !EnablePressedState;
                    BroadcastDataInputProperties(nameof(Pressed));
                }
            } catch { }
        }

        public virtual string GetHeader() {
            if (!IsBaseAppearance && !Enabled) return "(Default / Inherited Shape)";
            string normStr = Normal != null ? $"{Normal.Size.x * 1000f:F0}x{Normal.Size.y * 1000f:F0}mm" : "Default";
            if (!EnablePressedState || Pressed == null) {
                return $"Normal Shape: {normStr} (No Press Squish)";
            }
            string pressStr = $"{Pressed.Size.x * 1000f:F0}x{Pressed.Size.y * 1000f:F0}mm";
            return $"Shape: {normStr} ➔ Pressed: {pressStr}";
        }

        public void CopyFrom(VirtualKeyShapeStyle other) {
            if (other == null) return;
            Enabled = other.Enabled;
            EnablePressedState = other.EnablePressedState;
            if (Normal == null) Normal = StructuredData.Create<VirtualKeyShapeStateStyle>();
            if (Pressed == null) Pressed = StructuredData.Create<VirtualKeyShapeStateStyle>();
            Normal.CopyFrom(other.Normal);
            Pressed.CopyFrom(other.Pressed);
        }

        public VirtualKeyShapeStyle Clone() {
            var clone = StructuredData.Create<VirtualKeyShapeStyle>();
            clone.CopyFrom(this);
            return clone;
        }
    }

    public class VirtualKeyMaterialStateStyle : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(5)]
        [Label("ENABLED")]
        public bool Enabled = true;

        [DataInput(10)]
        [Label("COLOR")]
        public Color Color = Color.white;

        [DataInput(20)]
        [FloatSlider(0f, 1.0f, 0.05f)]
        [Label("SELF_ILLUMINATION")]
        public float SelfIllumination = 1.0f;

        [DataInput(30)]
        [Label("GLOW_COLOR")]
        public Color GlowColor = Color.white;

        [DataInput(40)]
        [FloatSlider(0f, 5.0f, 0.1f)]
        [Label("GLOW_INTENSITY")]
        public float GlowIntensity = 0f;

        public bool ShowEnabledToggle = true;
        public Action OnChanged;

        protected override void OnCreate() {
            base.OnCreate();
            SyncUI();
            Watch(nameof(Enabled), delegate { SyncUI(); OnChanged?.Invoke(); });
            Watch(nameof(Color), delegate { OnChanged?.Invoke(); });
            Watch(nameof(SelfIllumination), delegate { OnChanged?.Invoke(); });
            Watch(nameof(GlowColor), delegate { OnChanged?.Invoke(); });
            Watch(nameof(GlowIntensity), delegate { OnChanged?.Invoke(); });
        }

        public void SyncUI() {
            try {
                var enabledPort = GetDataInputPort(nameof(Enabled));
                if (enabledPort != null) {
                    enabledPort.Properties.hidden = !ShowEnabledToggle;
                    BroadcastDataInputProperties(nameof(Enabled));
                }

                bool hideFields = ShowEnabledToggle && !Enabled;

                void SetHidden(string portName, bool hidden) {
                    var port = GetDataInputPort(portName);
                    if (port != null) {
                        port.Properties.hidden = hidden;
                        BroadcastDataInputProperties(portName);
                    }
                }

                SetHidden(nameof(Color), hideFields);
                SetHidden(nameof(SelfIllumination), hideFields);
                SetHidden(nameof(GlowColor), hideFields);
                SetHidden(nameof(GlowIntensity), hideFields);
            } catch { }
        }

        public void BroadcastAll() {
            try {
                BroadcastDataInput(nameof(Enabled));
                BroadcastDataInput(nameof(Color));
                BroadcastDataInput(nameof(SelfIllumination));
                BroadcastDataInput(nameof(GlowColor));
                BroadcastDataInput(nameof(GlowIntensity));
                SyncUI();
            } catch { }
        }

        public virtual string GetHeader() {
            if (ShowEnabledToggle && !Enabled) return "(Default / Inherited)";
            var hex = ColorUtility.ToHtmlStringRGBA(Color);
            return $"#{hex} | Illum: {SelfIllumination:F2} | Glow: {GlowIntensity:F1}x";
        }

        public void CopyFrom(VirtualKeyMaterialStateStyle other) {
            if (other == null) return;
            Enabled = other.Enabled;
            Color = other.Color;
            SelfIllumination = other.SelfIllumination;
            GlowColor = other.GlowColor;
            GlowIntensity = other.GlowIntensity;
        }

        public VirtualKeyMaterialStateStyle Clone() {
            var clone = StructuredData.Create<VirtualKeyMaterialStateStyle>();
            clone.CopyFrom(this);
            return clone;
        }
    }

    public class VirtualKeyMaterialStyle : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(5)]
        [Label("ENABLED")]
        public bool Enabled = true;

        [DataInput(10)]
        [Label("NORMAL_STATE")]
        public VirtualKeyMaterialStateStyle Normal;

        [DataInput(20)]
        [Label("ENABLE_PRESSED_STATE")]
        public bool EnablePressedState = true;

        [DataInput(30)]
        [Label("PRESSED_STATE")]
        public VirtualKeyMaterialStateStyle Pressed;

        public bool IsBaseAppearance = true;
        public Action OnChanged;

        protected override void OnCreate() {
            base.OnCreate();
            if (Normal == null) Normal = StructuredData.Create<VirtualKeyMaterialStateStyle>();
            if (Pressed == null) Pressed = StructuredData.Create<VirtualKeyMaterialStateStyle>();

            Normal.OnChanged = () => OnChanged?.Invoke();
            Pressed.OnChanged = () => OnChanged?.Invoke();

            ConfigureStates(IsBaseAppearance);
            SyncUI();

            Watch(nameof(Enabled), delegate {
                if (Normal != null) Normal.Enabled = Enabled;
                SyncUI();
                OnChanged?.Invoke();
            });
            Watch(nameof(EnablePressedState), delegate {
                if (Pressed != null) {
                    Pressed.Enabled = EnablePressedState;
                    Pressed.SyncUI();
                }
                SyncUI();
                OnChanged?.Invoke();
            });
            Watch(nameof(Normal), delegate {
                if (Normal != null) Normal.OnChanged = () => OnChanged?.Invoke();
                OnChanged?.Invoke();
            });
            Watch(nameof(Pressed), delegate {
                if (Pressed != null) Pressed.OnChanged = () => OnChanged?.Invoke();
                OnChanged?.Invoke();
            });
        }

        public void ConfigureStates(bool isBaseAppearance = true) {
            IsBaseAppearance = isBaseAppearance;
            var enabledPort = GetDataInputPort(nameof(Enabled));
            if (enabledPort != null) {
                enabledPort.Properties.hidden = isBaseAppearance;
                BroadcastDataInputProperties(nameof(Enabled));
            }

            if (Normal != null) {
                Normal.ShowEnabledToggle = false;
                Normal.SyncUI();
            }
            if (Pressed != null) {
                Pressed.ShowEnabledToggle = false;
                Pressed.SyncUI();
            }
        }

        public void BroadcastAll() {
            try {
                BroadcastDataInput(nameof(Enabled));
                BroadcastDataInput(nameof(EnablePressedState));
                Normal?.BroadcastAll();
                Pressed?.BroadcastAll();
                BroadcastDataInput(nameof(Normal));
                BroadcastDataInput(nameof(Pressed));
                SyncUI();
            } catch { }
        }

        public void SyncUI() {
            try {
                var pressedPort = GetDataInputPort(nameof(Pressed));
                if (pressedPort != null) {
                    pressedPort.Properties.hidden = !EnablePressedState;
                    BroadcastDataInputProperties(nameof(Pressed));
                }
            } catch { }
        }

        public virtual string GetHeader() {
            if (!IsBaseAppearance && !Enabled) return "(Default / Inherited)";
            var nHex = (Normal != null && Normal.Enabled) ? ColorUtility.ToHtmlStringRGBA(Normal.Color) : "Default";
            if (!EnablePressedState || Pressed == null || !Pressed.Enabled) {
                return $"Normal: #{nHex} (No Press State)";
            }
            var pHex = ColorUtility.ToHtmlStringRGBA(Pressed.Color);
            return $"Normal: #{nHex} ➔ Pressed: #{pHex}";
        }

        public void CopyFrom(VirtualKeyMaterialStyle other) {
            if (other == null) return;
            Enabled = other.Enabled;
            EnablePressedState = other.EnablePressedState;
            if (Normal == null) Normal = StructuredData.Create<VirtualKeyMaterialStateStyle>();
            if (Pressed == null) Pressed = StructuredData.Create<VirtualKeyMaterialStateStyle>();
            Normal.CopyFrom(other.Normal);
            Pressed.CopyFrom(other.Pressed);
        }

        public VirtualKeyMaterialStyle Clone() {
            var clone = StructuredData.Create<VirtualKeyMaterialStyle>();
            clone.CopyFrom(this);
            return clone;
        }
    }

    public class VirtualKeyAppearance : StructuredData, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("SHAPE_STYLE")]
        public VirtualKeyShapeStyle Shape;

        [DataInput(20)]
        [Label("BODY_STYLE")]
        public VirtualKeyMaterialStyle Body;

        [DataInput(30)]
        [Label("BORDER_STYLE")]
        public VirtualKeyMaterialStyle Border;

        [DataInput(40)]
        [Label("PRESS_IN_TRANSITION")]
        public Transition PressInTransition;

        [DataInput(45)]
        [Label("PRESS_OUT_TRANSITION")]
        public Transition PressOutTransition;

        [Trigger(50)]
        [Label("RESET_TO_DEFAULTS")]
        public void TriggerResetToDefaults() {
            var baseApp = GetBaseAppearance?.Invoke();
            if (!IsBaseAppearance && baseApp != null) {
                CopyFrom(baseApp);
                IsBaseAppearance = false;
                if (Shape != null) {
                    Shape.IsBaseAppearance = false;
                    Shape.Enabled = true;
                }
                if (Body != null) {
                    Body.IsBaseAppearance = false;
                    Body.Enabled = true;
                }
                if (Border != null) {
                    Border.IsBaseAppearance = false;
                    Border.Enabled = true;
                }
            } else {
                ResetToDefaultTheme();
            }
            BroadcastAll();
        }

        public bool IsBaseAppearance = true;
        public Func<VirtualKeyAppearance> GetBaseAppearance;
        public Action OnAppearanceChanged;

        protected override void OnCreate() {
            base.OnCreate();
            if (Shape == null || Body == null || Border == null || PressInTransition == null || PressOutTransition == null) {
                ResetToDefaultTheme();
            }
            ConfigureElements();

            Watch(nameof(Shape), delegate {
                ConfigureElements();
                OnAppearanceChanged?.Invoke();
            });
            Watch(nameof(Body), delegate {
                ConfigureElements();
                OnAppearanceChanged?.Invoke();
            });
            Watch(nameof(Border), delegate {
                ConfigureElements();
                OnAppearanceChanged?.Invoke();
            });
            Watch(nameof(PressInTransition), delegate { OnAppearanceChanged?.Invoke(); });
            Watch(nameof(PressOutTransition), delegate { OnAppearanceChanged?.Invoke(); });
        }

        public void ConfigureElements() {
            if (Shape != null) {
                Shape.IsBaseAppearance = IsBaseAppearance;
                Shape.ConfigureStates(IsBaseAppearance);
                Shape.OnChanged = () => OnAppearanceChanged?.Invoke();
                Shape.SyncUI();
            }
            if (Body != null) {
                Body.IsBaseAppearance = IsBaseAppearance;
                Body.ConfigureStates(IsBaseAppearance);
                Body.OnChanged = () => OnAppearanceChanged?.Invoke();
                Body.SyncUI();
            }
            if (Border != null) {
                Border.IsBaseAppearance = IsBaseAppearance;
                Border.ConfigureStates(IsBaseAppearance);
                Border.OnChanged = () => OnAppearanceChanged?.Invoke();
                Border.SyncUI();
            }
        }

        public void BroadcastAll() {
            try {
                Shape?.BroadcastAll();
                Body?.BroadcastAll();
                Border?.BroadcastAll();
                BroadcastDataInput(nameof(Shape));
                BroadcastDataInput(nameof(Body));
                BroadcastDataInput(nameof(Border));
                BroadcastDataInput(nameof(PressInTransition));
                BroadcastDataInput(nameof(PressOutTransition));
                ConfigureElements();
                OnAppearanceChanged?.Invoke();
            } catch { }
        }

        public void ResetToDefaultTheme() {
            // 1. Shape: Geometry
            if (Shape == null) Shape = StructuredData.Create<VirtualKeyShapeStyle>();
            Shape.IsBaseAppearance = IsBaseAppearance;
            Shape.Enabled = true;
            Shape.EnablePressedState = true;

            if (Shape.Normal == null) Shape.Normal = StructuredData.Create<VirtualKeyShapeStateStyle>();
            Shape.Normal.Enabled = true;
            Shape.Normal.Size = new Vector2(0.032f, 0.032f);
            if (Shape.Normal.Corners == null) Shape.Normal.Corners = StructuredData.Create<CornerRadiiData>();
            Shape.Normal.Corners.Radius = 0.25f;
            Shape.Normal.Corners.UseIndividualCorners = false;
            Shape.Normal.Corners.TopCorners = new Vector2(0.25f, 0.25f);
            Shape.Normal.Corners.BottomCorners = new Vector2(0.25f, 0.25f);

            if (Shape.Normal.Borders == null) Shape.Normal.Borders = StructuredData.Create<BorderThicknessData>();
            Shape.Normal.Borders.Width = 0.0025f;
            Shape.Normal.Borders.UseIndividualBorders = false;
            Shape.Normal.Borders.TopBottomBorders = new Vector2(0.0025f, 0.0025f);
            Shape.Normal.Borders.LeftRightBorders = new Vector2(0.0025f, 0.0025f);

            if (Shape.Pressed == null) Shape.Pressed = StructuredData.Create<VirtualKeyShapeStateStyle>();
            Shape.Pressed.Enabled = true;
            Shape.Pressed.Size = new Vector2(0.032f, 0.032f);
            if (Shape.Pressed.Corners == null) Shape.Pressed.Corners = StructuredData.Create<CornerRadiiData>();
            Shape.Pressed.Corners.Radius = 0.25f;
            Shape.Pressed.Corners.UseIndividualCorners = false;
            Shape.Pressed.Corners.TopCorners = new Vector2(0.25f, 0.25f);
            Shape.Pressed.Corners.BottomCorners = new Vector2(0.25f, 0.25f);

            if (Shape.Pressed.Borders == null) Shape.Pressed.Borders = StructuredData.Create<BorderThicknessData>();
            Shape.Pressed.Borders.Width = 0.0025f; // Same border width as normal by default (2.5mm)
            Shape.Pressed.Borders.UseIndividualBorders = false;
            Shape.Pressed.Borders.TopBottomBorders = new Vector2(0.0025f, 0.0025f);
            Shape.Pressed.Borders.LeftRightBorders = new Vector2(0.0025f, 0.0025f);

            // 2. Body: Surface Fill Material
            if (Body == null) Body = StructuredData.Create<VirtualKeyMaterialStyle>();
            Body.IsBaseAppearance = IsBaseAppearance;
            Body.Enabled = true;
            Body.EnablePressedState = true;

            if (Body.Normal == null) Body.Normal = StructuredData.Create<VirtualKeyMaterialStateStyle>();
            Body.Normal.Enabled = true;
            // Warudo HologramTrackpad fill (#0032DB80) with 0 glow for unlit look
            Body.Normal.Color = new Color(0.0000f, 0.1961f, 0.8588f, 0.5020f);
            Body.Normal.SelfIllumination = 1.0f;
            Body.Normal.GlowColor = new Color(0.0000f, 0.1961f, 0.8588f, 1.0f);
            Body.Normal.GlowIntensity = 0.0f;

            if (Body.Pressed == null) Body.Pressed = StructuredData.Create<VirtualKeyMaterialStateStyle>();
            Body.Pressed.Enabled = true;
            // Slightly brighter electric azure body with glow on press
            Body.Pressed.Color = new Color(0.12f, 0.35f, 0.98f, 0.80f);
            Body.Pressed.SelfIllumination = 1.0f;
            Body.Pressed.GlowColor = new Color(0.15f, 0.45f, 1.0f, 1.0f);
            Body.Pressed.GlowIntensity = 1.5f;

            // 3. Border: Frame Stroke Material
            if (Border == null) Border = StructuredData.Create<VirtualKeyMaterialStyle>();
            Border.IsBaseAppearance = IsBaseAppearance;
            Border.Enabled = true;
            Border.EnablePressedState = true;

            if (Border.Normal == null) Border.Normal = StructuredData.Create<VirtualKeyMaterialStateStyle>();
            Border.Normal.Enabled = true;
            // Warudo HologramTrackpad border (#56FFFFFF)
            Border.Normal.Color = new Color(0.3373f, 1.0000f, 1.0000f, 1.0000f);
            Border.Normal.SelfIllumination = 1.0f;
            Border.Normal.GlowColor = new Color(0.3373f, 1.0000f, 1.0000f, 1.0000f);
            Border.Normal.GlowIntensity = 0.0f;

            if (Border.Pressed == null) Border.Pressed = StructuredData.Create<VirtualKeyMaterialStateStyle>();
            Border.Pressed.Enabled = true;
            // Glowing white border on press
            Border.Pressed.Color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            Border.Pressed.SelfIllumination = 1.0f;
            Border.Pressed.GlowColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            Border.Pressed.GlowIntensity = 2.5f;

            // 4. Transitions
            if (PressInTransition == null) PressInTransition = StructuredData.Create<Transition>();
            PressInTransition.Time = 0.02f;
            PressInTransition.Ease = Ease.Linear;

            if (PressOutTransition == null) PressOutTransition = StructuredData.Create<Transition>();
            PressOutTransition.Time = 0.12f;
            PressOutTransition.Ease = Ease.OutCubic;

            ConfigureElements();
        }

        public virtual string GetHeader() {
            float inT = PressInTransition?.Time ?? 0.02f;
            float outT = PressOutTransition?.Time ?? 0.12f;
            return $"Key Appearance (PressIn: {inT:F2}s, PressOut: {outT:F2}s)";
        }

        public void CopyFrom(VirtualKeyAppearance other) {
            if (other == null) return;
            if (Shape == null) Shape = StructuredData.Create<VirtualKeyShapeStyle>();
            if (Body == null) Body = StructuredData.Create<VirtualKeyMaterialStyle>();
            if (Border == null) Border = StructuredData.Create<VirtualKeyMaterialStyle>();
            Shape.CopyFrom(other.Shape);
            Body.CopyFrom(other.Body);
            Border.CopyFrom(other.Border);
            if (other.PressInTransition != null) {
                if (PressInTransition == null) PressInTransition = StructuredData.Create<Transition>();
                PressInTransition.Time = other.PressInTransition.Time;
                PressInTransition.Ease = other.PressInTransition.Ease;
            }
            if (other.PressOutTransition != null) {
                if (PressOutTransition == null) PressOutTransition = StructuredData.Create<Transition>();
                PressOutTransition.Time = other.PressOutTransition.Time;
                PressOutTransition.Ease = other.PressOutTransition.Ease;
            }
            ConfigureElements();
        }

        public VirtualKeyAppearance Clone() {
            var clone = StructuredData.Create<VirtualKeyAppearance>();
            clone.IsBaseAppearance = IsBaseAppearance;
            clone.CopyFrom(this);
            return clone;
        }

        public static VirtualKeyAppearance CreateDefault() {
            var app = StructuredData.Create<VirtualKeyAppearance>();
            app.IsBaseAppearance = true;
            app.ResetToDefaultTheme();
            return app;
        }

        public static VirtualKeyAppearance CreateForOverride() {
            var app = StructuredData.Create<VirtualKeyAppearance>();
            app.IsBaseAppearance = false;
            app.ResetToDefaultTheme();
            if (app.Shape != null) {
                app.Shape.IsBaseAppearance = false;
                app.Shape.Enabled = false;
                app.Shape.EnablePressedState = false;
                if (app.Shape.Normal != null) app.Shape.Normal.Enabled = false;
                if (app.Shape.Pressed != null) app.Shape.Pressed.Enabled = false;
            }
            if (app.Body != null) {
                app.Body.IsBaseAppearance = false;
                app.Body.Enabled = false;
                app.Body.EnablePressedState = false;
                if (app.Body.Normal != null) app.Body.Normal.Enabled = false;
                if (app.Body.Pressed != null) app.Body.Pressed.Enabled = false;
            }
            if (app.Border != null) {
                app.Border.IsBaseAppearance = false;
                app.Border.Enabled = false;
                app.Border.EnablePressedState = false;
                if (app.Border.Normal != null) app.Border.Normal.Enabled = false;
                if (app.Border.Pressed != null) app.Border.Pressed.Enabled = false;
            }
            app.ConfigureElements();
            return app;
        }
    }
}
