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
                return new SignalProfileType[] {
                    SignalProfileType.SwitchProController,
                    SignalProfileType.Xbox360Controller,
                };
            }
        }

        protected override void GenerateButtonDefinitions(SignalProfileType profile) {

            // All profiles are assumed to be the same for XInput
            SetDataInput(
                nameof(ButtonDefinitions),
                new ButtonDefinition[] {
                    StructuredData.Create<ButtonDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 12;
                        d.Label = "A";
                        d.AssignedCharacterLayer = Layer.RightThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                        d.CharacterAnimation.AnimationData = StructuredData.Create<HoverableStatefulTransitionAnimationData>(ad => {
                            ad.HoverAnimationData = StructuredData.Create<BaseRevertibleTransitionAnimationData>(had => {
                                had.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                            ad.DownAnimationData = StructuredData.Create<StatefulTransitionAnimationData>(sad => {
                                sad.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0f;
                                    t.Ease = Ease.OutCubic;
                                });
                                sad.UpTransition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                        });
                        d.PropAnimation = StructuredData.Create<TransitionablePropAnimationDefinition>(p => {
                            p.AnimatorLayerName = "A";
                            p.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0.2f;
                            });
                            p.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0f;
                            });
                        });
                        d.PropMotion = StructuredData.Create<PropMotionDefinition>(pmd => {
                            if (profile == SignalProfileType.SwitchProController) {
                                pmd.TranslationFactor = TranslationDownLight;
                                pmd.RotationFactor = new Vector3(0.25f, 0f, -0.5f);
                            } else {
                                pmd.TranslationFactor = TranslationDownStrong;
                                pmd.RotationFactor = Vector3.zero;
                            }
                        });
                    }),
                    StructuredData.Create<ButtonDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 13;
                        d.Label = "B";
                        d.AssignedCharacterLayer = Layer.RightThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                        d.CharacterAnimation.AnimationData = StructuredData.Create<HoverableStatefulTransitionAnimationData>(ad => {
                            ad.HoverAnimationData = StructuredData.Create<BaseRevertibleTransitionAnimationData>(had => {
                                had.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                            ad.DownAnimationData = StructuredData.Create<StatefulTransitionAnimationData>(sad => {
                                sad.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0f;
                                    t.Ease = Ease.OutCubic;
                                });
                                sad.UpTransition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                        });
                        d.PropAnimation = StructuredData.Create<TransitionablePropAnimationDefinition>(p => {
                            p.AnimatorLayerName = "B";
                            p.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0.2f;
                            });
                            p.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0f;
                            });
                        });
                        d.PropMotion = StructuredData.Create<PropMotionDefinition>(pmd => {
                            if (profile == SignalProfileType.SwitchProController) {
                                pmd.TranslationFactor = TranslationDownStrong;
                                pmd.RotationFactor = Vector3.zero;
                            } else {
                                pmd.TranslationFactor = TranslationDownLight;
                                pmd.RotationFactor = new Vector3(0.25f, 0f, -0.5f);
                            }
                        });
                    }),
                    StructuredData.Create<ButtonDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 14;
                        d.Label = "X";
                        d.AssignedCharacterLayer = Layer.RightThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                        d.CharacterAnimation.AnimationData = StructuredData.Create<HoverableStatefulTransitionAnimationData>(ad => {
                            ad.HoverAnimationData = StructuredData.Create<BaseRevertibleTransitionAnimationData>(had => {
                                had.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                            ad.DownAnimationData = StructuredData.Create<StatefulTransitionAnimationData>(sad => {
                                sad.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0f;
                                    t.Ease = Ease.OutCubic;
                                });
                                sad.UpTransition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                        });
                        d.PropAnimation = StructuredData.Create<TransitionablePropAnimationDefinition>(p => {
                            p.AnimatorLayerName = "X";
                            p.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0.2f;
                            });
                            p.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0f;
                            });
                        });
                        d.PropMotion = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownLight;
                            pmd.RotationFactor = RotationForwardLight;
                            if (profile == SignalProfileType.Xbox360Controller) {
                                pmd.TranslationFactor = TranslationDownStrong;
                                pmd.RotationFactor = RotationForwardStrong;
                            } else {
                                pmd.TranslationFactor = TranslationDownLight;
                                pmd.RotationFactor = RotationForwardLight;
                            }
                        });
                    }),
                    StructuredData.Create<ButtonDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 15;
                        d.Label = "Y";
                        d.AssignedCharacterLayer = Layer.RightThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                        d.CharacterAnimation.AnimationData = StructuredData.Create<HoverableStatefulTransitionAnimationData>(ad => {
                            ad.HoverAnimationData = StructuredData.Create<BaseRevertibleTransitionAnimationData>(had => {
                                had.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                            ad.DownAnimationData = StructuredData.Create<StatefulTransitionAnimationData>(sad => {
                                sad.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0f;
                                    t.Ease = Ease.OutCubic;
                                });
                                sad.UpTransition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                        });
                        d.PropAnimation = StructuredData.Create<TransitionablePropAnimationDefinition>(p => {
                            p.AnimatorLayerName = "Y";
                            p.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0.2f;
                            });
                            p.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0f;
                            });
                        });
                        d.PropMotion = StructuredData.Create<PropMotionDefinition>(pmd => {
                            if (profile == SignalProfileType.Xbox360Controller) {
                                pmd.TranslationFactor = TranslationDownLight;
                                pmd.RotationFactor = RotationForwardLight;
                            } else {
                                pmd.TranslationFactor = TranslationDownStrong;
                                pmd.RotationFactor = RotationForwardStrong;
                            }
                        });
                    }),
                    StructuredData.Create<ButtonDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 8;
                        if (profile == SignalProfileType.SwitchProController) {
                            d.Label = "L";
                        } else {
                            d.Label = "LB";
                        }
                        d.AssignedCharacterLayer = Layer.LeftIndex;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                        d.CharacterAnimation.AnimationData = StructuredData.Create<HoverableStatefulTransitionAnimationData>(ad => {
                            ad.DownAnimationData = StructuredData.Create<StatefulTransitionAnimationData>(sad => {
                                sad.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0f;
                                    t.Ease = Ease.OutCubic;
                                });
                                sad.UpTransition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                        });
                        d.PropAnimation = StructuredData.Create<TransitionablePropAnimationDefinition>(p => {
                            if (profile == SignalProfileType.SwitchProController) {
                                p.AnimatorLayerName = "L";
                            } else {
                                p.AnimatorLayerName = "LB";
                            }
                            p.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0.2f;
                            });
                            p.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0f;
                            });
                        });
                        d.PropMotion = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = new Vector3(0.25f, 0f, -0.25f);
                            pmd.RotationFactor = Vector3.zero;
                        });
                    }),
                    StructuredData.Create<ButtonDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 9;
                        if (profile == SignalProfileType.SwitchProController) {
                            d.Label = "R";
                        } else {
                            d.Label = "RB";
                        }
                        d.AssignedCharacterLayer = Layer.RightIndex;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                        d.CharacterAnimation.AnimationData = StructuredData.Create<HoverableStatefulTransitionAnimationData>(ad => {
                            ad.DownAnimationData = StructuredData.Create<StatefulTransitionAnimationData>(sad => {
                                sad.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0f;
                                    t.Ease = Ease.OutCubic;
                                });
                                sad.UpTransition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                        });
                        d.PropAnimation = StructuredData.Create<TransitionablePropAnimationDefinition>(p => {
                            if (profile == SignalProfileType.SwitchProController) {
                                p.AnimatorLayerName = "R";
                            } else {
                                p.AnimatorLayerName = "RB";
                            }
                            p.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0.2f;
                            });
                            p.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                t.Time = 0.05f;
                                t.Ease = Ease.Linear;
                                t.DelayTime = 0f;
                            });
                        });
                        d.PropMotion = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = new Vector3(-0.25f, 0f, -0.25f);
                            pmd.RotationFactor = Vector3.zero;
                        });
                    }),
                    StructuredData.Create<ButtonDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 4;
                        if (profile == SignalProfileType.SwitchProController) {
                            d.Label = "Plus";
                        } else {
                            d.Label = "Start";
                        }
                        d.AssignedCharacterLayer = Layer.LeftThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                    }),
                    StructuredData.Create<ButtonDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 5;
                        if (profile == SignalProfileType.SwitchProController) {
                            d.Label = "Minus";
                        } else {
                            d.Label = "Back";
                        }
                        d.AssignedCharacterLayer = Layer.RightThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                    }),
                    StructuredData.Create<ButtonDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 6;
                        d.Label = "LSB";
                        d.AssignedCharacterLayer = Layer.LeftThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                        d.CharacterAnimation.AnimationData = StructuredData.Create<HoverableStatefulTransitionAnimationData>(ad => {
                            ad.HoverAnimationData = StructuredData.Create<BaseRevertibleTransitionAnimationData>(had => {
                                had.SetDataInput(nameof(had.IsReturnToBaseWanted), true, true);
                                had.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                        });
                        d.PropMotion = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownStrong;
                            pmd.RotationFactor = Vector3.zero;
                        });
                    }),
                    StructuredData.Create<ButtonDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 7;
                        d.Label = "RSB";
                        d.AssignedCharacterLayer = Layer.RightThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                        d.CharacterAnimation.AnimationData = StructuredData.Create<HoverableStatefulTransitionAnimationData>(ad => {
                            ad.HoverAnimationData = StructuredData.Create<BaseRevertibleTransitionAnimationData>(had => {
                                had.SetDataInput(nameof(had.IsReturnToBaseWanted), true, true);
                                had.Transition = StructuredData.Create<Transition>(t => {
                                    t.Time = 0.1f;
                                    t.Ease = Ease.OutCubic;
                                });
                            });
                        });
                        d.PropMotion = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownStrong;
                            pmd.RotationFactor = Vector3.zero;
                        });
                    }),
                },
                true
            );
            SetDataInput(
                nameof(SwitchDefinitions),
                new SwitchDefinition[] {
                    StructuredData.Create<SwitchDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 0;
                        d.Label = "D-Pad";
                        d.AssignedCharacterLayer = Layer.LeftThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                        d.CharacterAnimation.AnimationDataD1 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                            ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                            ad.Index = 1;
                            ad.HoverAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                            ad.DownAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                        });
                        d.CharacterAnimation.AnimationDataD2 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                            ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                            ad.Index = 2;
                            ad.HoverAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                            ad.DownAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                        });
                        d.CharacterAnimation.AnimationDataD3 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                            ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                            ad.Index = 3;
                            ad.HoverAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                            ad.DownAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                        });
                        d.CharacterAnimation.AnimationDataD4 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                            ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                            ad.Index = 4;
                            ad.HoverAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                            ad.DownAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                        });
                        d.CharacterAnimation.AnimationDataD5 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                            ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                            ad.Index = 5;
                            ad.HoverAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                            ad.DownAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                        });
                        d.CharacterAnimation.AnimationDataD6 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                            ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                            ad.Index = 6;
                            ad.HoverAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                            ad.DownAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                        });
                        d.CharacterAnimation.AnimationDataD7 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                            ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                            ad.Index = 7;
                            ad.HoverAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                            ad.DownAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                        });
                        d.CharacterAnimation.AnimationDataD8 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                            ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                            ad.Index = 8;
                            ad.HoverAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                            ad.DownAnimation = StructuredData.Create<AnimationData>(tad => {
                            });
                        });
                        d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                        d.CharacterAnimation.TransitionHover = StructuredData.Create<Transition>(t => {
                            t.Time = 0.1f;
                            t.Ease = Ease.OutCubic;
                        });
                        d.CharacterAnimation.TransitionDown = StructuredData.Create<Transition>(t => {
                            t.Time = 0.05f;
                            t.Ease = Ease.OutCubic;
                        });
                        d.CharacterAnimation.TransitionUp = StructuredData.Create<Transition>(t => {
                            t.Time = 0.05f;
                            t.Ease = Ease.OutCubic;
                        });
                        d.PropAnimation = StructuredData.Create<SwitchPropAnimationDefinition>(p => {
                            p.D1 = StructuredData.Create<TransitionablePropAnimationDefinition>(pd => {
                                pd.AnimatorLayerName = "D8";
                                pd.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                                pd.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                            });
                            p.D2 = StructuredData.Create<TransitionablePropAnimationDefinition>(pd => {
                                pd.AnimatorLayerName = "D9";
                                pd.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                                pd.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                            });
                            p.D3 = StructuredData.Create<TransitionablePropAnimationDefinition>(pd => {
                                pd.AnimatorLayerName = "D6";
                                pd.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                                pd.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                            });
                            p.D4 = StructuredData.Create<TransitionablePropAnimationDefinition>(pd => {
                                pd.AnimatorLayerName = "D3";
                                pd.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                                pd.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                            });
                            p.D5 = StructuredData.Create<TransitionablePropAnimationDefinition>(pd => {
                                pd.AnimatorLayerName = "D2";
                                pd.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                                pd.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                            });
                            p.D6 = StructuredData.Create<TransitionablePropAnimationDefinition>(pd => {
                                pd.AnimatorLayerName = "D1";
                                pd.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                                pd.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                            });
                            p.D7 = StructuredData.Create<TransitionablePropAnimationDefinition>(pd => {
                                pd.AnimatorLayerName = "D4";
                                pd.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                                pd.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                            });
                            p.D8 = StructuredData.Create<TransitionablePropAnimationDefinition>(pd => {
                                pd.AnimatorLayerName = "D7";
                                pd.TransitionDown = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                                pd.TransitionUp = StructuredData.Create<DelayableTransition>(t => {
                                    t.Time = 0.05f;
                                    t.Ease = Ease.OutCubic;
                                    t.DelayTime = 0f;
                                });
                            });
                        });
                        d.PropMotionSet.D1 = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownStrong;
                            pmd.RotationFactor = Vector3.zero;
                        });
                        d.PropMotionSet.D2 = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownStrong;
                            pmd.RotationFactor = Vector3.zero;
                        });
                        d.PropMotionSet.D3 = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownStrong;
                            pmd.RotationFactor = Vector3.zero;
                        });
                        d.PropMotionSet.D4 = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownLight;
                            pmd.RotationFactor = RotationBackwardLight;
                        });
                        d.PropMotionSet.D5 = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = Vector3.zero;
                            pmd.RotationFactor = RotationBackwardStrong;
                        });
                        d.PropMotionSet.D6 = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = Vector3.zero;
                            pmd.RotationFactor = new Vector3(-0.5f, 0f, 0.5f);
                        });
                        d.PropMotionSet.D7 = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = Vector3.zero;
                            pmd.RotationFactor = RotationLeftStrong;
                        });
                        d.PropMotionSet.D8 = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownLight;
                            pmd.RotationFactor = RotationLeftLight;
                        });
                        d.VirtualDefinitionSet.Enabled = true;
                        d.VirtualDefinitionSet.UpId = 0;
                        d.VirtualDefinitionSet.DownId = 1;
                        d.VirtualDefinitionSet.LeftId = 2;
                        d.VirtualDefinitionSet.RightId = 3;
                    }),
                },
                true
            );
            SetDataInput(
                nameof(AxisDefinitions),
                new AxisDefinition[] {
                    StructuredData.Create<AxisDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 0;
                        d.Label = "Stick1 X";
                        d.AssignedCharacterLayer = Layer.LeftThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                        d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                        });
                        d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                        });
                        d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                        d.PropAnimation.Max.AnimatorLayerName = "Stick1 +X";
                        d.PropAnimation.Min.AnimatorLayerName = "Stick1 -X";
                        d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                        d.AssignedGroup = AxisGroup.Group1;
                        d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownVeryStrong;
                            pmd.RotationFactor = RotationRightStrong;
                        });
                        d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownVeryStrong;
                            pmd.RotationFactor = RotationLeftStrong;
                        });
                    }),
                    StructuredData.Create<AxisDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 1;
                        d.Label = "Stick1 Y";
                        d.AssignedCharacterLayer = Layer.LeftThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                        d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                        });
                        d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                        });
                        d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                        d.PropAnimation.Max.AnimatorLayerName = "Stick1 +Y";
                        d.PropAnimation.Min.AnimatorLayerName = "Stick1 -Y";
                        d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                        d.AssignedGroup = AxisGroup.Group1;
                        d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownVeryStrong;
                            pmd.RotationFactor = RotationForwardStrong;
                        });
                        d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationUpVeryStrong;
                            pmd.RotationFactor = RotationBackwardStrong;
                        });
                    }),
                    StructuredData.Create<AxisDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 2;
                        d.Label = "Stick2 X";
                        d.AssignedCharacterLayer = Layer.RightThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                        d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                        });
                        d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                        });
                        d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                        d.PropAnimation.Max.AnimatorLayerName = "Stick2 +X";
                        d.PropAnimation.Min.AnimatorLayerName = "Stick2 -X";
                        d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                        d.AssignedGroup = AxisGroup.Group2;
                        d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownVeryStrong;
                            pmd.RotationFactor = RotationRightStrong;
                        });
                        d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownVeryStrong;
                            pmd.RotationFactor = RotationLeftStrong;
                        });
                    }),
                    StructuredData.Create<AxisDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 3;
                        d.Label = "Stick2 Y";
                        d.AssignedCharacterLayer = Layer.RightThumb;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                        d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                        });
                        d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                        });
                        d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                        d.PropAnimation.Max.AnimatorLayerName = "Stick2 +Y";
                        d.PropAnimation.Min.AnimatorLayerName = "Stick2 -Y";
                        d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                        d.AssignedGroup = AxisGroup.Group2;
                        d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationDownVeryStrong;
                            pmd.RotationFactor = RotationForwardStrong;
                        });
                        d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = TranslationUpVeryStrong;
                            pmd.RotationFactor = RotationBackwardStrong;
                        });
                    }),
                    StructuredData.Create<AxisDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 4;
                        if (profile == SignalProfileType.SwitchProController) {
                            d.Label = "ZL";
                        } else {
                            d.Label = "LT";
                        }
                        d.AssignedCharacterLayer = Layer.LeftMiddle;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                        d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                        });
                        if (profile == SignalProfileType.SwitchProController) {
                            d.PropAnimation.Max.AnimatorLayerName = "ZL";
                        } else {
                            d.PropAnimation.Max.AnimatorLayerName = "LT";
                        }
                        d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = new Vector3(-0.25f, -0.5f, -0.25f);
                            pmd.RotationFactor = RotationForwardVeryLight;
                        });
                    }),
                    StructuredData.Create<AxisDefinition>(d => {
                        d.IsValid = true;
                        d.Index = 5;
                        if (profile == SignalProfileType.SwitchProController) {
                            d.Label = "ZR";
                        } else {
                            d.Label = "RT";
                        }
                        d.AssignedCharacterLayer = Layer.RightMiddle;
                        d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                        d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                        });
                        if (profile == SignalProfileType.SwitchProController) {
                            d.PropAnimation.Max.AnimatorLayerName = "ZR";
                        } else {
                            d.PropAnimation.Max.AnimatorLayerName = "RT";
                        }
                        d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                            pmd.TranslationFactor = new Vector3(0.25f, -0.5f, -0.25f);
                            pmd.RotationFactor = RotationForwardVeryLight;
                        });
                    }),
                },
                true
            );

            RequestCharacterAnimationChange();
        }
    }
}
