using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;
using static Warudo.Plugins.Core.Assets.Character.CharacterAsset;

namespace FlameStream
{
    [AssetType(
        Id = "FlameStream.Asset.XInputReceiver",
        Title = "FS_ASSET_TITLE_XINPUT",
        Category = "FS_ASSET_CATEGORY_INPUT"
    )]
    public class XInputReceiverAsset : InputReceiverAsset {

        protected override ushort PROTOCOL_VERSION {
            get {
                return 1;
            }
        }

        protected override string PROTOCOL_ID {
            get {
                return "X";
            }
        }

        protected override int DEFAULT_PORT {
            get {
                return 40614;
            }
        }

        protected override string CHARACTER_ANIM_LAYER_ID_PREFIX {
            get {
                return "🔥🎮";
            }
        }

        protected override SignalProfileType[] SupportedProfileTypes {
            get {
                return new SignalProfileType[] {};
            }
        }

        protected override void GenerateButtonDefinitions(SignalProfileType profile) {

            // Create a list of button definitions based on profile
            switch (profile) {
                default:
                    Context.Service.Toast(
                        Warudo.Core.Server.ToastSeverity.Error,
                        Name,
                        $"Profile definition for {profile.Localized()} is not implemented for this asset. Please choose a different one."
                    );
                    return;
            }

            RequestCharacterAnimationChange();
        }
    }
}
