using Warudo.Core.Attributes;
using Warudo.Core.Plugins;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("FSInputReceiver.Tests")]

namespace FlameStream {
    [PluginType(
        Id = "FlameStream.PointerReceiver",
        Name = "PLUGIN_NAME",
        Description = "PLUGIN_DESCRIPTION",
        Author = "FlameStream",
        Version = "5.4.1",
        AssetTypes = new[] {
            typeof(DrawingScreenAsset),
            typeof(DirectInputReceiverAsset),
            typeof(GameInputReceiverAsset),
            typeof(GamepadReceiverAsset),
            typeof(KeyboardReceiverAsset),
            typeof(MultiInputCoordinatorAsset),
            typeof(PointerReceiverAsset),
            typeof(VisualSetupAnchorAsset),
            typeof(KeyBindingVisualSetupAnchorAsset),
            typeof(XInputReceiverAsset),
        },
        NodeTypes = new[] {
            typeof(CharacterAssetReferenceNode),
            typeof(GameObjectAssetReferenceNode),
            typeof(GamepadButtonHandAnimatorNode),
            typeof(GamepadButtonPropAnimatorNode),
            typeof(GamepadDPadHandAnimatorNode),
            typeof(GamepadDPadPropAnimatorNode),
            typeof(GamepadHandTrackerNode),
            typeof(GamepadReceiverIsButtonDownNode),
            typeof(GamepadReceiverOnButtonDownNode),
            typeof(GamepadReceiverOnButtonUpNode),
            typeof(GamepadStickHandAnimatorNode),
            typeof(GamepadStickPropAnimatorNode),
            typeof(GetGameInputReceiverDataNode),
            typeof(GetGamepadReceiverDataGenericAxisNode),
            typeof(GetGamepadReceiverDataPs5Node),
            typeof(GetGamepadReceiverDataSwitchNode),
            typeof(GetGamepadReceiverDataXbox360Node),
            typeof(GetKeyboardReceiverKeyNode),
            typeof(GetPointerReceiverDataNode),
            typeof(InputReceiverHandTrackerNode),
            typeof(InputReceiverIsButtonDownNode),
            typeof(InputReceiverOnAxisInactive),
            typeof(InputReceiverOnButtonDownNode),
            typeof(InputReceiverOnButtonUpNode),
            typeof(InputReceiverOnLastInputAtLayerChangeNode),
            typeof(InputReceiverWhileAxisActive), // Too late to change the name now...
            typeof(InputReceiverOnSwitchActiveNode),
            typeof(InputReceiverOnSwitchChangeNode),
            typeof(InputReceiverOnSwitchInactiveNode),
            typeof(KeyboardReceiverIsKeyDownNode),
            typeof(KeyboardReceiverOnKeyDownNode),
            typeof(KeyboardReceiverOnKeyUpNode),
        })]
    public class PointerReceiverPlugin : Plugin {
        public static PointerReceiverPlugin Instance { get; private set; }

        protected override void OnCreate() {
            base.OnCreate();
            Instance = this;
        }

        protected override void OnDestroy() {
            Instance = null;
            base.OnDestroy();
        }
    }
}
