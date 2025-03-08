using Warudo.Core.Attributes;
using Warudo.Core.Plugins;

namespace FlameStream {
    [PluginType(
        Id = "FlameStream.PointerReceiver",
        Name = "PLUGIN_NAME",
        Description = "PLUGIN_DESCRIPTION",
        Author = "FlameStream",
        Version = "4.0.2",
        AssetTypes = new[] {
            typeof(DrawingScreenAsset),
            typeof(DirectInputReceiverAsset),
            typeof(GameInputReceiverAsset),
            typeof(GamepadReceiverAsset),
            typeof(KeyboardReceiverAsset),
            typeof(PointerReceiverAsset),
            typeof(VisualSetupAnchorAsset)
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
    public class PointerReceiverPlugin : Plugin {}
}
