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
        Id = "FlameStream.Asset.DirectInputReceiver",
        Title = "FS_ASSET_TITLE_DIRECTINPUT",
        Category = "FS_ASSET_CATEGORY_INPUT"
    )]
    public class DirectInputReceiverAsset : InputReceiverAsset {

        protected override ushort PROTOCOL_VERSION {
            get {
                return 3;
            }
        }
        protected override int DEFAULT_PORT {
            get {
                return 40611;
            }
        }

        protected override SignalProfileType[] SupportedProfileTypes {
            get {
                return new SignalProfileType[] {
                    SignalProfileType.SwitchProController,
                    SignalProfileType.Xbox360Controller,
                    SignalProfileType.PS5Controller,
                    SignalProfileType.EightBitDoPro2Controller,
                    SignalProfileType.EightBitDoModKitGameCubeController,
                    SignalProfileType.RetroFightersBattlerGcProController,
                    SignalProfileType.PowerAGameCubeStyleForSwitchController,
                };
            }
        }

        protected override void GenerateButtonDefinitions(SignalProfileType profile) {
            // Create a list of button definitions based on profile
            switch (profile) {
                /**
                 * Switch Pro Controller
                 */
                case SignalProfileType.SwitchProController:
                    SetDataInput(
                        nameof(ButtonDefinitions),
                        new ButtonDefinition[] {
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 0;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = Vector3.zero;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 1;
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
                                    pmd.TranslationFactor = TranslationDownLight;
                                    pmd.RotationFactor = new Vector3(0.25f, 0f, -0.5f);
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 2;
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
                                    pmd.TranslationFactor = TranslationDownLight;
                                    pmd.RotationFactor = RotationForwardLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 4;
                                d.Label = "L";
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
                                    p.AnimatorLayerName = "L";
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
                                d.Index = 5;
                                d.Label = "R";
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
                                    p.AnimatorLayerName = "R";
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
                                d.Index = 6;
                                d.Label = "ZL";
                                d.AssignedCharacterLayer = Layer.LeftMiddle;
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
                                    p.AnimatorLayerName = "ZL";
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
                                    pmd.TranslationFactor = new Vector3(-0.25f, -0.5f, -0.25f);
                                    pmd.RotationFactor = RotationForwardVeryLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 7;
                                d.Label = "ZR";
                                d.AssignedCharacterLayer = Layer.RightMiddle;
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
                                    p.AnimatorLayerName = "ZR";
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
                                    pmd.TranslationFactor = new Vector3(0.25f, -0.5f, -0.25f);
                                    pmd.RotationFactor = RotationForwardVeryLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 8;
                                d.Label = "Minus";
                                d.AssignedCharacterLayer = Layer.LeftThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 9;
                                d.Label = "Plus";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 10;
                                d.Label = "LS";
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
                                d.Index = 11;
                                d.Label = "RS";
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
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 12;
                                d.Label = "Home";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 13;
                                d.Label = "Capture";
                                d.AssignedCharacterLayer = Layer.LeftThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
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
                                d.PropAnimation.Max.AnimatorLayerName = "Stick1 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick1 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group1;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<AxisDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
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
                                d.Index = 4;
                                d.Label = "Stick2 Y";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                                d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                                d.PropAnimation.Max.AnimatorLayerName = "Stick2 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick2 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group2;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                        },
                        true
                    );
                    break;

                /**
                 * Xbox 360 Controller
                 */
                case SignalProfileType.Xbox360Controller:
                    SetDataInput(
                        nameof(ButtonDefinitions),
                        new ButtonDefinition[] {
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 0;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = Vector3.zero;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 1;
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
                                    pmd.TranslationFactor = TranslationDownLight;
                                    pmd.RotationFactor = new Vector3(0.25f, 0f, -0.5f);
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 2;
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
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 4;
                                d.Label = "LB";
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
                                    p.AnimatorLayerName = "L";
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
                                d.Index = 5;
                                d.Label = "RB";
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
                                    p.AnimatorLayerName = "R";
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
                                d.Index = 8;
                                d.Label = "Back";
                                d.AssignedCharacterLayer = Layer.LeftThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 9;
                                d.Label = "Start";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 10;
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
                                d.Index = 11;
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
                                d.CharacterAnimation.AnimationDataD3 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                                    ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                                    ad.Index = 3;
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
                                d.CharacterAnimation.AnimationDataD7 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                                    ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                                    ad.Index = 7;
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
                                });
                                d.PropMotionSet.D1 = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = Vector3.zero;
                                });
                                d.PropMotionSet.D3 = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = Vector3.zero;
                                });
                                d.PropMotionSet.D5 = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = Vector3.zero;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.D7 = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = Vector3.zero;
                                    pmd.RotationFactor = RotationLeftStrong;
                                });
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
                                d.PropAnimation.Max.AnimatorLayerName = "Stick1 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick1 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group1;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<AxisDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
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
                                d.Index = 4;
                                d.Label = "Stick2 Y";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                                d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                                d.PropAnimation.Max.AnimatorLayerName = "Stick2 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick2 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group2;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                        },
                        true
                    );
                    break;

                /**
                 * PS5 Controller
                 */
                case SignalProfileType.PS5Controller:
                    SetDataInput(
                        nameof(ButtonDefinitions),
                        new ButtonDefinition[] {
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 0;
                                d.Label = "Square";
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
                                    p.AnimatorLayerName = "Square";
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = Vector3.zero;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 1;
                                d.Label = "Cross";
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
                                    p.AnimatorLayerName = "Cross";
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
                                    pmd.RotationFactor = new Vector3(0.25f, 0f, -0.5f);
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 2;
                                d.Label = "Circle";
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
                                    p.AnimatorLayerName = "Circle";
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
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
                                d.Label = "Triangle";
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
                                    p.AnimatorLayerName = "Triangle";
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 4;
                                d.Label = "L1";
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
                                    p.AnimatorLayerName = "L";
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
                                d.Index = 5;
                                d.Label = "R1";
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
                                    p.AnimatorLayerName = "R";
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
                                d.Index = 6;
                                d.Label = "L2";
                                d.AssignedCharacterLayer = Layer.LeftMiddle;
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
                                    p.AnimatorLayerName = "ZL";
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
                                    pmd.TranslationFactor = new Vector3(-0.25f, -0.5f, -0.25f);
                                    pmd.RotationFactor = RotationForwardVeryLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 7;
                                d.Label = "R2";
                                d.AssignedCharacterLayer = Layer.RightMiddle;
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
                                    p.AnimatorLayerName = "ZR";
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
                                    pmd.TranslationFactor = new Vector3(0.25f, -0.5f, -0.25f);
                                    pmd.RotationFactor = RotationForwardVeryLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 8;
                                d.Label = "Select";
                                d.AssignedCharacterLayer = Layer.LeftThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 9;
                                d.Label = "Start";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 10;
                                d.Label = "L3";
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
                                d.Index = 11;
                                d.Label = "R3";
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
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 12;
                                d.Label = "Home";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 13;
                                d.Label = "Touchpad";
                                d.AssignedCharacterLayer = Layer.LeftThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
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
                                d.CharacterAnimation.AnimationDataD3 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                                    ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                                    ad.Index = 3;
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
                                d.CharacterAnimation.AnimationDataD7 = StructuredData.Create<SwitchStateAnimationData>(ad => {
                                    ad.Parent = d.CharacterAnimation; // Particularly important because layer name will be generated soon
                                    ad.Index = 7;
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
                                });
                                d.PropMotionSet.D1 = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = Vector3.zero;
                                });
                                d.PropMotionSet.D3 = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = Vector3.zero;
                                });
                                d.PropMotionSet.D5 = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = Vector3.zero;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.D7 = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = Vector3.zero;
                                    pmd.RotationFactor = RotationLeftStrong;
                                });
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
                                d.PropAnimation.Max.AnimatorLayerName = "Stick1 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick1 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group1;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
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
                                d.Index = 5;
                                d.Label = "Stick2 Y";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                                d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                                d.PropAnimation.Max.AnimatorLayerName = "Stick2 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick2 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group2;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<AxisDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
                                d.Label = "L2";
                                d.AssignedCharacterLayer = Layer.LeftMiddle;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                                d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.PropAnimation.Max.AnimatorLayerName = "ZL";
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = new Vector3(-0.25f, -0.5f, -0.25f);
                                    pmd.RotationFactor = RotationForwardVeryLight;
                                });
                            }),
                            StructuredData.Create<AxisDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 4;
                                d.Label = "R2";
                                d.AssignedCharacterLayer = Layer.RightMiddle;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                                d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.PropAnimation.Max.AnimatorLayerName = "ZR";
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = new Vector3(0.25f, -0.5f, -0.25f);
                                    pmd.RotationFactor = RotationForwardVeryLight;
                                });
                            }),
                        },
                        true
                    );
                    break;

                /**
                 * 8BitDo Pro 2 Bluetooth Gamepad
                 * https://www.8bitdo.com/pro2/
                 */
                case SignalProfileType.EightBitDoPro2Controller:
                    SetDataInput(
                        nameof(ButtonDefinitions),
                        new ButtonDefinition[] {
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 1;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = Vector3.zero;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 0;
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
                                    pmd.TranslationFactor = TranslationDownLight;
                                    pmd.RotationFactor = new Vector3(0.25f, 0f, -0.5f);
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
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
                                    pmd.TranslationFactor = TranslationDownLight;
                                    pmd.RotationFactor = RotationForwardLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 2;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 4;
                                d.Label = "L1";
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
                                    p.AnimatorLayerName = "L1";
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
                                d.Index = 5;
                                d.Label = "R1";
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
                                    p.AnimatorLayerName = "R1";
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
                                d.Index = 6;
                                d.Label = "Select";
                                d.AssignedCharacterLayer = Layer.LeftThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 7;
                                d.Label = "Start";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 8;
                                d.Label = "L3";
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
                                d.Index = 9;
                                d.Label = "R3";
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
                                d.PropAnimation.Max.AnimatorLayerName = "Stick1 -X";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick1 +X";
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
                                d.PropAnimation.Max.AnimatorLayerName = "Stick1 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick1 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group1;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<AxisDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
                                d.Label = "Stick2 X";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                                d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                                d.PropAnimation.Max.AnimatorLayerName = "Stick2 -X";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick2 +X";
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
                                d.Index = 4;
                                d.Label = "Stick2 Y";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                                d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                                d.PropAnimation.Max.AnimatorLayerName = "Stick2 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick2 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group2;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                        },
                        true
                    );
                    break;

                /**
                 * 8BitDo Mod Kit for Original NGC Controller
                 * https://shop.8bitdo.com/products/mod-kit-for-original-ngc
                 */
                case SignalProfileType.EightBitDoModKitGameCubeController:
                    SetDataInput(
                        nameof(ButtonDefinitions),
                        new ButtonDefinition[] {
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 1;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = Vector3.zero;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 0;
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
                                    pmd.TranslationFactor = TranslationDownLight;
                                    pmd.RotationFactor = new Vector3(0.25f, 0f, -0.5f);
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 4;
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
                                    pmd.TranslationFactor = TranslationDownLight;
                                    pmd.RotationFactor = RotationForwardLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 10;
                                d.Label = "Z";
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
                                    p.AnimatorLayerName = "Z";
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
                                d.Index = 6;
                                d.Label = "L";
                                d.AssignedCharacterLayer = Layer.LeftMiddle;
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
                                    p.AnimatorLayerName = "L";
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
                                    pmd.TranslationFactor = new Vector3(-0.25f, -0.5f, -0.25f);
                                    pmd.RotationFactor = RotationForwardVeryLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 7;
                                d.Label = "R";
                                d.AssignedCharacterLayer = Layer.RightMiddle;
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
                                    p.AnimatorLayerName = "R";
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
                                    pmd.TranslationFactor = new Vector3(0.25f, -0.5f, -0.25f);
                                    pmd.RotationFactor = RotationForwardVeryLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 11;
                                d.Label = "Start";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
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
                                d.PropAnimation.Max.AnimatorLayerName = "Stick1 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick1 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group1;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<AxisDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
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
                                d.Index = 4;
                                d.Label = "Stick2 Y";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                                d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                                d.PropAnimation.Max.AnimatorLayerName = "Stick2 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick2 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group2;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                        },
                        true
                    );
                    break;

                /**
                 * Retro Fighters BattlerGC Pro
                 * https://retrofighters.com/our-collection/battlergc-pro/
                 */
                case SignalProfileType.RetroFightersBattlerGcProController:
                    SetDataInput(
                        nameof(ButtonDefinitions),
                        new ButtonDefinition[] {
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 0;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = Vector3.zero;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 1;
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
                                    pmd.TranslationFactor = TranslationDownLight;
                                    pmd.RotationFactor = new Vector3(0.25f, 0f, -0.5f);
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 2;
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
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 4;
                                d.Label = "L";
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
                                    p.AnimatorLayerName = "L";
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
                                d.Index = 5;
                                d.Label = "R";
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
                                    p.AnimatorLayerName = "R";
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
                                d.Index = 6;
                                d.Label = "Minus";
                                d.AssignedCharacterLayer = Layer.LeftThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 7;
                                d.Label = "Plus";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 8;
                                d.Label = "LS";
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
                                d.Index = 9;
                                d.Label = "RS";
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
                                d.PropAnimation.Max.AnimatorLayerName = "Stick1 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick1 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group1;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<AxisDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
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
                                d.Index = 4;
                                d.Label = "Stick2 Y";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                                d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                                d.PropAnimation.Max.AnimatorLayerName = "Stick2 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick2 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group2;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                        },
                        true
                    );
                    break;

                /**
                 * PowerA GameCube Style Wired Controller for Nintendo Switch
                 * https://www.powera.com/p/nintendo/nintendo-switch/controllers/wired/wired-controller-for-nintendo-switch-gamecube/
                 */
                case SignalProfileType.PowerAGameCubeStyleForSwitchController:
                    SetDataInput(
                        nameof(ButtonDefinitions),
                        new ButtonDefinition[] {
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 1;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = Vector3.zero;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 2;
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
                                    pmd.TranslationFactor = TranslationDownLight;
                                    pmd.RotationFactor = new Vector3(0.25f, 0f, -0.5f);
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 0;
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
                                    pmd.TranslationFactor = TranslationDownLight;
                                    pmd.RotationFactor = RotationForwardLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
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
                                    pmd.TranslationFactor = TranslationDownStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 4;
                                d.Label = "L";
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
                                    p.AnimatorLayerName = "L";
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
                                d.Index = 5;
                                d.Label = "R";
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
                                    p.AnimatorLayerName = "R";
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
                                d.Index = 6;
                                d.Label = "ZL";
                                d.AssignedCharacterLayer = Layer.LeftMiddle;
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
                                    p.AnimatorLayerName = "ZL";
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
                                    pmd.TranslationFactor = new Vector3(-0.25f, -0.5f, -0.25f);
                                    pmd.RotationFactor = RotationForwardVeryLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 7;
                                d.Label = "ZR";
                                d.AssignedCharacterLayer = Layer.RightMiddle;
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
                                    p.AnimatorLayerName = "ZR";
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
                                    pmd.TranslationFactor = new Vector3(0.25f, -0.5f, -0.25f);
                                    pmd.RotationFactor = RotationForwardVeryLight;
                                });
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 8;
                                d.Label = "Minus";
                                d.AssignedCharacterLayer = Layer.LeftThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 9;
                                d.Label = "Plus";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 10;
                                d.Label = "LS";
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
                                d.Index = 11;
                                d.Label = "RS";
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
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 12;
                                d.Label = "Home";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                            }),
                            StructuredData.Create<ButtonDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 13;
                                d.Label = "Capture";
                                d.AssignedCharacterLayer = Layer.LeftThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
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
                                d.PropAnimation.Max.AnimatorLayerName = "Stick1 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick1 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group1;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                            StructuredData.Create<AxisDefinition>(d => {
                                d.IsValid = true;
                                d.Index = 3;
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
                                d.Index = 4;
                                d.Label = "Stick2 Y";
                                d.AssignedCharacterLayer = Layer.RightThumb;
                                d.CharacterAnimation.MaskedBodyParts = new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers };
                                d.CharacterAnimation.Max = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Min = StructuredData.Create<AnimationData>(ad => {
                                });
                                d.CharacterAnimation.Base.SetDataInput(nameof(d.CharacterAnimation.Base.IsReturnToBaseWanted), true, true);
                                d.PropAnimation.Max.AnimatorLayerName = "Stick2 -Y";
                                d.PropAnimation.Min.AnimatorLayerName = "Stick2 +Y";
                                d.SetDataInput(nameof(d.NeutralState), AxisNeutralState.Midpoint, true);
                                d.AssignedGroup = AxisGroup.Group2;
                                d.PropMotionSet.Max = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationUpVeryStrong;
                                    pmd.RotationFactor = RotationBackwardStrong;
                                });
                                d.PropMotionSet.Min = StructuredData.Create<PropMotionDefinition>(pmd => {
                                    pmd.TranslationFactor = TranslationDownVeryStrong;
                                    pmd.RotationFactor = RotationForwardStrong;
                                });
                            }),
                        },
                        true
                    );
                    break;


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
