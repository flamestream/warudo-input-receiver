using Warudo.Core.Attributes;
using Warudo.Core.Plugins;

namespace FlameStream {
    [PluginType(
        Id = "FlameStream.PointerReceiver",
        Name = "PLUGIN_NAME",
        Description = "PLUGIN_DESCRIPTION",
        Author = "FlameStream",
        Version = "1.0",
        AssetTypes = new[] {
            typeof(GamepadReceiverAsset),
            typeof(PointerReceiverAsset)
        },
        NodeTypes = new[] {
            typeof(CharacterAssetReferenceNode),
            typeof(GameObjectAssetReferenceNode),
            typeof(GetPointerReceiverDataNode),
            typeof(GetGamepadReceiverDataNode),
            typeof(GetPs5GamepadReceiverDataNode),
        })]
    public class PointerReceiverPlugin : Plugin {}
}
