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
        Title = "FS_ASSET_TITLE_POINTER",
        Category = "FS_ASSET_CATEGORY_INPUT"
    )]
    public partial class PointerReceiverAsset : ReceiverAsset {

        protected override string CHARACTER_ANIM_LAYER_ID_PREFIX {
            get {
                return "🔥🖱️";
            }
        }

        protected override ushort PROTOCOL_VERSION {
            get {
                return 1;
            }
        }

        protected override string PROTOCOL_ID {
            get {
                return "P";
            }
        }

        protected override int DEFAULT_PORT {
            get {
                return 40610;
            }
        }

        /// <summary>
        /// BASIC SETUP
        /// </summary>
        [Section("BASIC_SETUP")]

        [DataInput]
        [Label("ENABLE")]
        public bool IsHandEnabled;

        [DataInput]
        [Label("ACTIVE_TRANSITION")]
        public ActiveTransition ActiveTransition;

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

        [DataInput]
        [Label("CURSOR_MODE")]
        public CursorMode CursorMode;

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
        public HandRotationDynamicVector3 HandDynamicRotation;

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
        public BodyRotationDynamicVector3 BodyDynamicRotation;

        /// <summary>
        /// ADVANCED
        /// </summary>
        [DataInput(1000)]
        [Label("POINTER_RECEIVER_CONTROL_SCREEN_VISIBILITY")]
        public bool IsScreenDisableBasedOnAssetStateWanted;

        [DataInput(1000)]
        [Label("POINTER_FACTOR_CORRECTION")]
        [Description("POINTER_FACTOR_CORRECTION_DESC")]
        [FloatSlider(0.01f, 2f, 0.01f)]
        public float PointerFactorCorrection = 1;

        [DataInput(1000)]
        [Label("DEBUG_MODE_SCALE_FACTOR")]
        [FloatSlider(0.01f, 10f, 0.01f)]
        public float DebugSphereScaleFactor = 1;

        [Trigger(1000)]
        [Label("FORCE_DEBUG_MODE")]
        public void TriggerForceDebugSpheres() {
            DisplayDebugSpheres(true);
            OnDebugSettingChange();
        }

        [Trigger(1000)]
        [Hidden]
        [Label("DISABLE_DEBUG_MODE")]
        public void TriggerDestroyDebugSpheres() {
            DestroyDebugSpheres(true);
            OnDebugSettingChange();
        }
    }
}
