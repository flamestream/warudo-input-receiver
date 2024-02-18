using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.GameObjectAssetReference",
    Title = "NODE_TITLE_GAMEOBJ_ASSET_REF",
    Category = "NODE_CATEGORY")
]
    public class GameObjectAssetReferenceNode : Node {

        [DataInput]
        [Label("GAMEOBJECT_ASSET")]
        public GameObjectAsset a;

        [DataOutput]
        [Label("ASSET")]
        public Asset Asset() {
            return a;
        }

        [DataOutput]
        [Label("GAMEOBJECT_ASSET")]
        public GameObjectAsset GameObjectAsset() {
            return a;
        }
    }
}
