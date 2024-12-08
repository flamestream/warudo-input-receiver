using Warudo.Core.Attributes;
using Warudo.Core.Plugins;

namespace FlameStream {
    [PluginType(
        Id = "FlameStream.PointerReceiver",
        Name = "PLUGIN_NAME",
        Description = "PLUGIN_DESCRIPTION",
        Author = "FlameStream",
        Version = "3.5.0",
        AssetTypes = new[] {
            typeof(DrawingScreenAsset),
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
            typeof(GetGamepadReceiverDataGenericAxisNode),
            typeof(GetGamepadReceiverDataPs5Node),
            typeof(GetGamepadReceiverDataSwitchNode),
            typeof(GetGamepadReceiverDataXbox360Node),
            typeof(GetKeyboardReceiverKeyNode),
            typeof(GetPointerReceiverDataNode),
            typeof(KeyboardReceiverIsKeyDownNode),
            typeof(KeyboardReceiverOnKeyDownNode),
            typeof(KeyboardReceiverOnKeyUpNode),
        })]
    public class PointerReceiverPlugin : Plugin {}
}
