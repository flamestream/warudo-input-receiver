using Cysharp.Threading.Tasks;
using System.Linq;
using uDesktopDuplication;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets.Character;

namespace FlameStream {
    [AssetType(
        Id = "FlameStream.Asset.PointerReceiver",
        Title = "ASSET_TITLE_POINTER",
        Category = "CATEGORY_INPUT"
    )]
    public partial class PointerReceiverAsset : ReceiverAsset {
        /// <summary>
        /// BASIC SETUP
        /// </summary>
        [Section("BASIC_SETUP")]

        [DataInput]
        [Label("ENABLE")]
        public bool IsHandEnabled;

        [DataInput]
        [Label("CHARACTER")]
        [Description("CHARACTER_DESC")]
        public CharacterAsset Character;


        [DataInput]
        [Label("RIGHT_HANDED")]
        public bool IsRightHanded = true;

        [DataInput]
        [AutoComplete(nameof(AutoCompleteDisplayName), true, "")]
        [Label("SCREEN_DISPLAY")]
        public string DisplayName;
        async UniTask<AutoCompleteList> AutoCompleteDisplayName() {
            return AutoCompleteList.Single(Manager.monitors.Select((Monitor it) => new AutoCompleteEntry
            {
                label = it.name,
                value = it.name
            }).ToList());
        }

        [DataInput]
        [Label("CURSOR_SMOOTHNESS")]
        [FloatSlider(0f, 0.1f, 0.01f)]
        public float CursorSmoothness = 0.05f;

        /// <summary>
        /// HAND MOVEMENT
        /// </summary>
        [Section("HAND_MOVEMENT")]
        [DataInput]
        [Label("NEUTRAL_POSITION")]
        public NaturalPositionVisualSetupTransform NeutralHandPosition;

        [DataInput]
        [Label("MOUSE_MODE")]
        public MouseHandState HandMouseMode;

        [DataInput]
        [Label("PEN_MODE")]
        public PenHandState HandPenMode;

        [DataInput]
        [Label("DYNAMIC_ROTATION")]
        public DynamicVector3 HandDynamicRotation = StructuredData.Create<DynamicVector3>((dv) => {
            dv.Z = StructuredData.Create<DistanceInputMathExpression>((me) => {
                me.Expression = "min(2 * abs(dx), 1) * (atan2(abs(dy), abs(dx)) * (180 / pi) - 90) * -sign(dx)";
            });
        });

        /// <summary>
        /// HAND PROP CONTROL
        /// </summary>
        [Section("HAND_PROP_CONTROL")]
        [DataInput]
        [Label("ENABLE")]
        public bool IsPropEnabled;

        [DataInput]
        [Label("PROP_SOURCE")]
        [AutoComplete(nameof(GetPropSources), true, "NONE")]
        public string PropSource;

        [DataInput]
        [Label("MOUSE_MODE")]
        public MousePropState PropMouseMode;

        [DataInput]
        [Label("PEN_MODE")]
        public PenPropState PropPenMode;

        /// <summary>
        /// MOVE BODY
        /// </summary>
        [Section("BODY_MOVEMENT")]
        [DataInput]
        [Label("ENABLE")]
        public bool IsBodyEnabled;

        [DataInput]
        [Label("MOUSE_MODE")]
        public MouseBodyState BodyMouseMode;

        [DataInput]
        [Label("PEN_MODE")]
        public PenBodyState BodyPenMode;

        [DataInput]
        [Label("MOVEMENT_TYPE")]
        public Movement BodyMovementType;

        [DataInput]
        [Label("MINIMUM_MOVEMENT_DELTA")]
        [FloatSlider(0f, 5f, 0.1f)]
        public float BodyMinimumMovementDelta = 0f;

        [DataInput]
        [Label("DYNAMIC_ROTATION")]
        public DynamicVector3 BodyDynamicRotation = StructuredData.Create<DynamicVector3>((dv) => {
            dv.Z = StructuredData.Create<DistanceInputMathExpression>((me) => {
                me.Expression = "min(0.15 * abs(dx), 1) * atan2(-abs(dx), abs(dy)) * 4 / pi * (180 / pi) * sign(dx)";
            });
        });

        /// <summary>
        /// ADVANCED
        /// </summary>
        [Section("ADVANCED")]
        [DataInput]
        [Label("POINTER_FACTOR_CORRECTION")]
        [Description("POINTER_FACTOR_CORRECTION_DESC")]
        [FloatSlider(0.01f, 2f, 0.01f)]
        public float PointerFactorCorrection = 1;

        [DataInput]
        [Label("DEBUG_MODE_SCALE_FACTOR")]
        [FloatSlider(0.01f, 10f, 0.01f)]
        public float DebugSphereScaleFactor = 1;

        [Trigger]
        [Label("FORCE_DEBUG_MODE")]
        public void TriggerForceDebugSpheres() {
            DisplayDebugSpheres(true);
            OnDebugSettingChange();
        }

        [Trigger]
        [Hidden]
        [Label("DISABLE_DEBUG_MODE")]
        public void TriggerDestroyDebugSpheres() {
            DestroyDebugSpheres(true);
            OnDebugSettingChange();
        }
    }
}
