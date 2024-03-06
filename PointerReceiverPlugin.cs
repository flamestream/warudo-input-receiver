using Warudo.Core.Attributes;
using Warudo.Core.Plugins;

namespace FlameStream {
    [PluginType(
        Id = "FlameStream.PointerReceiver",
        Name = "PLUGIN_NAME",
        Description = "PLUGIN_DESCRIPTION",
        Author = "FlameStream",
        Version = "3.1.0",
        AssetTypes = new[] {
            typeof(GamepadReceiverAsset),
            typeof(PointerReceiverAsset)
        },
        NodeTypes = new[] {
            typeof(CharacterAssetReferenceNode),
            typeof(GameObjectAssetReferenceNode),
            typeof(GamepadButtonHandAnimatorNode),
            typeof(GamepadButtonPropAnimatorNode),
            typeof(GamepadDPadHandAnimatorNode),
            typeof(GamepadDPadPropAnimatorNode),
            typeof(GamepadHandTrackerNode),
            typeof(GamepadStickHandAnimatorNode),
            typeof(GamepadStickPropAnimatorNode),
            typeof(GetGamepadReceiverDataPs5Node),
            typeof(GetGamepadReceiverDataSwitchNode),
            typeof(GetGamepadReceiverDataXbox360Node),
            typeof(GetPointerReceiverDataNode),
        })]
    public class PointerReceiverPlugin : Plugin {}
}
