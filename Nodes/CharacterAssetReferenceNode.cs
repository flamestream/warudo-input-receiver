using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets;
using Warudo.Plugins.Core.Assets.Character;

namespace FlameStream {
[NodeType(
    Id = "FlameStream.Node.CharacterAssetReference",
    Title = "NODE_TITLE_CHARA_ASSET_REF",
    Category = "NODE_CATEGORY")]
    public class CharacterAssetReferenceNode : Node {

        [DataInput]
        [Label("CHARACTER_ASSET")]
        public CharacterAsset a;

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

        [DataOutput]
        [Label("CHARACTER_ASSET")]
        public CharacterAsset CharacterAsset() {
            return a;
        }
    }
}
