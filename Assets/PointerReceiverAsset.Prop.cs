using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Resource;
using Warudo.Plugins.Core.Assets.Prop;

namespace FlameStream
{
    public partial class PointerReceiverAsset : ReceiverAsset {

        [DataInput]
        [Hidden]
        public Guid PenPropAssetId;

        public GameObjectState propState;
        GameObjectState lastPropState;
        GameObjectState setupModePropState;
        Tween tweenPropPosition;
        Tween tweenPropRotation;
        Tween tweenPropScale;
        Vector3 propPosition;
        Vector3 propRotation;
        Vector3 propScale;
        PropAsset prop;

        void OnCreateProp() {
            Watch(nameof(IsPropEnabled), delegate { OnPropEnabledChanged(); });

            Watch(nameof(IsPropEnabled), delegate { OnInputAffectingPropChange(); });
            Watch(nameof(Character), delegate { OnInputAffectingPropChange(); });
            Watch(nameof(PropSource), delegate { OnInputAffectingPropChange(); });
            Watch(nameof(IsRightHanded), delegate { OnInputAffectingPropChange(); });
        }

        void OnReadyProp() {
            PropMouseMode.OnCreateAnchor = (p, tr, a) => { HandlePropCreateAnchor(p, tr, a); };
            PropMouseMode.OnEnterVisualSetup = (p) => { HandlePropEnterVisualSetup(p); };
            PropMouseMode.OnExitVisualSetup = (p, ia) => { HandlePropExitVisualSetup(p, ia); };

            PropPenMode.OnCreateAnchor = (p, tr, a) => { HandlePropCreateAnchor(p, tr, a); };
            PropPenMode.OnEnterVisualSetup = (p) => { HandlePropEnterVisualSetup(p); };
            PropPenMode.OnExitVisualSetup = (p, ia) => { HandlePropExitVisualSetup(p, ia); };

            OnPropEnabledChanged();
            OnInputAffectingPropChange();
        }

        void OnUpdateProp() {
            if (!IsPropEnabled) return;
            if (prop == null) return;

            if (inSetupMode) {
                if (setupModePropState != null) {
                    var tr = setupModePropState.Transform.setupAnchor.Transform;
                    propPosition = tr.Position;
                    propRotation = tr.Rotation;
                    propScale = tr.Scale;
                }
            } else {
                propState = GetPropState(Source, Button1);

                if (lastPropState != propState) {
                    OnPropStateChange();
                }
            }

            prop.Transform.Position = propPosition;
            prop.Transform.Rotation = propRotation;
            prop.Transform.Scale = propScale;
            prop.Transform.Broadcast();

            lastPropState = propState;
        }

        void OnPropEnabledChanged() {
            GetDataInputPort(nameof(PropSource)).Properties.hidden = !(IsPropEnabled && isBasicSetupComplete);
            GetDataInputPort(nameof(PropMouseMode)).Properties.hidden = !(IsPropEnabled && isBasicSetupComplete);
            GetDataInputPort(nameof(PropPenMode)).Properties.hidden = !(IsPropEnabled && isBasicSetupComplete);

            BroadcastDataInputProperties(nameof(PropSource));
            BroadcastDataInputProperties(nameof(PropMouseMode));
            BroadcastDataInputProperties(nameof(PropPenMode));
        }

        void OnInputAffectingPropChange() {
            if (!isReady) return;
            if (!IsPropEnabled) return;
            if (PropSource == null) {
                var p = GetProp(true);
                CleanDestroy(p);
                PenPropAssetId = Guid.Empty;
                return;
            };
            if (Character == null) return;

            prop = GetProp();
            prop.SetDataInput(nameof(Source), PropSource, true);
            prop.Attachable.Parent = Character;
            prop.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.HumanBodyBone;
            prop.Attachable.AttachToBone = IsRightHanded
                ? HumanBodyBones.RightHand
                : HumanBodyBones.LeftHand;
            prop.Broadcast();
        }

        void OnPropStateChange(bool immediate = false) {
            if (propState == null) return;

            var pos = propState.Transform.Position;
            var rot = propState.Transform.Rotation;
            var sca = propState.Transform.Scale;
            var time = immediate ? 0 : propState.EnterTransition.Time;
            var ease = propState.EnterTransition.Ease;

            SetPropTransforms(pos, rot, sca, time, ease);
        }

        GameObjectState GetPropState(int source, bool button1) {

            if (!IsPropEnabled) return null;

            var st = PropPenMode.Enabled && source == 2
                    ? (BaseState)PropPenMode
                    : PropMouseMode;

            if (!st.Enabled) return null;

            var o = st.ActiveState.Enabled && button1
                ? (GameObjectState)st.ActiveState
                : st;

            return o;
        }

        void HandlePropEnterVisualSetup(GameObjectState p) {
            if (setupModePropState != null) return;
            EnterSetupModeMinimal();
            setupModePropState = p;
            tweenPropPosition?.Kill();
            tweenPropRotation?.Kill();
            tweenPropScale?.Kill();

            if (setupModePropState == PropMouseMode) {
                GetHandState(0, false)?.Transform.EnterVisualSetup(false);
                GetBodyState(0, false)?.Transform.EnterVisualSetup(false);
            } else if (setupModePropState == PropMouseMode.ActiveState) {
                GetHandState(0, true)?.Transform.EnterVisualSetup(false);
                GetBodyState(0, true)?.Transform.EnterVisualSetup(false);
            } else if (setupModePropState == PropPenMode) {
                GetHandState(2, false)?.Transform.EnterVisualSetup(false);
                GetBodyState(2, false)?.Transform.EnterVisualSetup(false);
            } else if (setupModePropState == PropPenMode.ActiveState) {
                GetHandState(2, true)?.Transform.EnterVisualSetup(false);
                GetBodyState(2, true)?.Transform.EnterVisualSetup(false);
            }
        }

        void HandlePropExitVisualSetup(GameObjectState p, bool isApply) {
            if (setupModePropState == null) return;
            ExitSetupModeMinimal();
            setupModePropState = null;
            tweenPropPosition?.Kill();
            tweenPropRotation?.Kill();
            tweenPropScale?.Kill();
            OnPropStateChange();
            NeutralHandPosition.ExitVisualSetup(isApply);
            setupModeHandState?.Transform.ExitVisualSetup(isApply);
            setupModeBodyState?.Transform.ExitVisualSetup(isApply);
        }

        void HandlePropCreateAnchor(GameObjectState p, VisualSetupTransform tr, VisualSetupAnchorAsset a) {
            if (Character == null) return;
            a.Attachable.Parent = Character;
            a.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.HumanBodyBone;
            a.Attachable.AttachToBone = IsRightHanded
                ? HumanBodyBones.RightHand
                : HumanBodyBones.LeftHand;

        }

        PropAsset GetProp(bool skipAutoCreate = false) {
            return GetAsset<PropAsset>(ref PenPropAssetId, skipAutoCreate, newAssetName: $"📦-{LAYER_NAME_PREFIX} Hand Prop");
        }

        async UniTask<AutoCompleteList> GetPropSources() {
            return Context.ResourceManager.ProvideResources("Prop").ToAutoCompleteList();
        }

        void SetPropTransforms(Vector3 pos, Vector3 rot, Vector3 sca, float time = 0, Ease ease = Ease.Linear) {

            tweenPropPosition?.Kill();
            if (time == 0) {
                propPosition = pos;
            } else {
                tweenPropPosition = DOTween.To(
                    () => propPosition,
                    delegate(Vector3 it) { propPosition = it; },
                    pos,
                    time
                ).SetEase(ease);
            }

            tweenPropRotation?.Kill();
            if (time == 0) {
                propRotation = rot;
            } else {
                tweenPropRotation = DOTween.To(
                    () => propRotation,
                    delegate(Vector3 it) { propRotation = it; },
                    rot,
                    time
                ).SetEase(ease);
            }

            tweenPropScale?.Kill();
            if (time == 0) {
                propScale = sca;
            } else {
                tweenPropScale = DOTween.To(
                    () => propScale,
                    delegate(Vector3 it) { propScale = it; },
                    sca,
                    time
                ).SetEase(ease);
            }
        }
    }
}
