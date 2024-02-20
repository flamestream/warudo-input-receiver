using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Utils;
using static FlameStream.GamepadReceiverAsset;
using static Warudo.Plugins.Core.Assets.Character.CharacterAsset;

namespace FlameStream {
    public partial class GamepadReceiverAsset : ReceiverAsset {

        void GenerateButtonAnimationTemplate() {
            switch (TargetControllerType) {
                case ControllerType.SwitchProController:
                default:
                    ButtonAnimationData = Enum.GetValues(typeof(SwitchProButton))
                        .Cast<SwitchProButton>()
                        .Where(e => e != SwitchProButton.None)
                        .Select(e => {
                            var old = Array.Find(ButtonAnimationData, d => d.ButtonId == (int)e);

                            var o = StructuredData.Create<GamepadButtonAnimationData>();
                            o.TargetControllerType = ControllerType.SwitchProController;
                            o.ButtonId = (int)e;
                            o.PropLayerName = old?.PropLayerName ?? e.ToString();
                            o.FingerHoverAnimation = old?.FingerHoverAnimation ?? null;
                            o.FingerPressAnimation = old?.FingerPressAnimation ?? null;
                            o.MaskRight = old?.MaskRight ?? false;
                            return o;
                        })
                        .ToArray();
                    break;
            }
            BroadcastDataInput(nameof(ButtonAnimationData));
            Context.Service.PromptMessage("SUCCESS", $"Button animation definition for {nameof(TargetControllerType)} has been succesfully (re)generated.");
        }

        public class GamepadButtonAnimationData : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [PreviewGallery]
            [AutoCompleteResource("CharacterAnimation", null)]
            public string FingerHoverAnimation;

            [DataInput]
            [PreviewGallery]
            [AutoCompleteResource("CharacterAnimation", null)]
            public string FingerPressAnimation;

            [DataInput]
            [Label("Right side mask")]
            public bool MaskRight;

            [DataInput]
            public string PropLayerName;

            [Hidden]
            [DataInput]
            public ControllerType TargetControllerType;

            [Hidden]
            [DataInput]
            public int ButtonId;

            readonly AnimationMaskedBodyPart[] LeftMask = new AnimationMaskedBodyPart[]
            {
                AnimationMaskedBodyPart.LeftFingers,
            };

            readonly AnimationMaskedBodyPart[] RightMask = new AnimationMaskedBodyPart[]
            {
                AnimationMaskedBodyPart.RightFingers,
            };

            public bool IsLooping() { return false; }

            public string GetHeader() {
                string name;
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                    default:
                        name = $"{((SwitchProButton)ButtonId).ToString()} ({nameof(SwitchProButton)})";
                        break;
                }
                if (ButtonId == 0) return "Invalid. Please Generate this list instead.";
                return name;
            }
        }
    }
}
