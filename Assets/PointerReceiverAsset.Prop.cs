using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Resource;
using Warudo.Plugins.Core.Assets.Prop;
using Warudo.Plugins.Core.Assets.Utility;

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
        Tween tweenPropProgress;
        float propTweenProgress = 1f;
        Vector3 propPositionOffset;
        Vector3 propRotationOffset;
        Vector3 propScale;
        PropAsset prop;
        AnchorAsset propSetupRootAnchor;

        void OnCreateProp() {
            Watch(nameof(IsPropEnabled), delegate { OnPropEnabledChanged(); });

            Watch(nameof(IsPropEnabled), delegate { OnInputAffectingPropChange(); });
            Watch(nameof(Character), delegate { OnInputAffectingPropChange(); });
            Watch(nameof(PropSource), delegate { OnInputAffectingPropChange(); });

            Watch(nameof(IsRightHanded), delegate { OnInputAffectingPropChange(); });

            Watch(nameof(PropMouseMode), delegate { OnPropStateSettingChange(); });
            Watch(nameof(PropPenMode), delegate { OnPropStateSettingChange(); });
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
                propState = setupModePropState;
                CalculateSetupPropOffsets();
            } else {
                propState = GetPropState(Source, Button1);
            }
            if (lastPropState != propState) {
                OnPropStateChange();
            }

            prop.Transform.Position = propPositionOffset;
            prop.Transform.Rotation = propRotationOffset;
            prop.Transform.Scale = propScale;
            prop.Transform.Broadcast();

            lastPropState = propState;
        }

        void OnPropEnabledChanged() {
            GetDataInputPort(nameof(PropSource)).Properties.hidden = !(IsPropEnabled && isBasicSetupComplete);
            GetDataInputPort(nameof(PropMouseMode)).Properties.hidden = !(IsPropEnabled && isBasicSetupComplete);
            GetDataInputPort(nameof(PropPenMode)).Properties.hidden = !(IsPropEnabled && isBasicSetupComplete);

            prop?.SetDataInput(nameof(prop.Enabled), IsPropEnabled && IsHandEnabled, true);

            BroadcastDataInputProperties(nameof(PropSource));
            BroadcastDataInputProperties(nameof(PropMouseMode));
            BroadcastDataInputProperties(nameof(PropPenMode));
        }

        void OnPropStateSettingChange() {
            OnPropStateChange();
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
            var st = setupModePropState ?? propState;

            var tr = GetGlobalCalculatorTransform();
            var r = Vector3.zero; // Used to allow 360+ degree rotation
            var s = Vector3.one;

            if (st != null) {
                if (st != PropMouseMode) {
                    tr.Translate(PropMouseMode.Transform.Position);
                    tr.Rotate(PropMouseMode.Transform.Rotation);
                    r += PropMouseMode.Transform.Rotation;
                    s = Vector3.Scale(s, PropMouseMode.Transform.Scale);
                }

                if (st == PropPenMode.ActiveState) {
                    tr.Translate(PropPenMode.Transform.Position);
                    tr.Rotate(PropPenMode.Transform.Rotation);
                    r += PropPenMode.Transform.Rotation;
                    s = Vector3.Scale(s, PropPenMode.Transform.Scale);
                }

                tr.Translate(st.Transform.Position);
                tr.Rotate(st.Transform.Rotation);
                r += st.Transform.Rotation;
                s = Vector3.Scale(s, st.Transform.Scale);
            }

            var time = inSetupMode ? 0 : st?.EnterTransition.Time ?? PropMouseMode.EnterTransition.Time;
            var ease = st?.EnterTransition.Ease ?? PropMouseMode.EnterTransition.Ease;

            SetPropOffsetTransforms(tr.position, r, s, time, ease);
        }

        void CalculateSetupPropOffsets() {
            if (setupModePropState == null) return;
            var st = setupModePropState;

            var tr = GetGlobalCalculatorTransform();
            var r = Vector3.zero; // Used to allow 360+ degree rotation
            var s = Vector3.one;

            if (st != PropMouseMode) {
                tr.Translate(PropMouseMode.Transform.Position);
                tr.Rotate(PropMouseMode.Transform.Rotation);
                r += PropMouseMode.Transform.Rotation;
                s = Vector3.Scale(s, PropMouseMode.Transform.Scale);
            }

            // TODO: generalize
            if (st == PropPenMode.ActiveState) {
                tr.Translate(PropPenMode.Transform.Position);
                tr.Rotate(PropPenMode.Transform.Rotation);
                r += PropPenMode.Transform.Rotation;
                s = Vector3.Scale(s, PropPenMode.Transform.Scale);
            }

            var trr = propSetupRootAnchor.Transform;
            trr.Position = tr.position;
            trr.Rotation = tr.eulerAngles;
            trr.Broadcast();

            var trs = st.Transform.setupAnchor.Transform;
            tr.Translate(trs.Position);
            tr.Rotate(trs.Rotation);
            r += trs.Rotation;
            s = Vector3.Scale(s, trs.Scale);

            propPositionOffset = tr.position;
            propRotationOffset = r;
            propScale = s;
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

            var source = 0;
            var button1 = false;
            if (setupModePropState == PropMouseMode.ActiveState) {
                button1 = true;
            } else if (setupModePropState == PropPenMode) {
                source = 2;
            } else if (setupModePropState == PropPenMode.ActiveState) {
                source = 2;
                button1 = true;
            }
            TriggerOtherEnterSetupModes(source, button1);
        }

        void HandlePropExitVisualSetup(GameObjectState p, bool isApply) {
            if (setupModePropState == null) return;

            CleanDestroy(propSetupRootAnchor);
            ExitSetupModeMinimal();
            setupModePropState = null;
            tweenPropPosition?.Kill();
            tweenPropRotation?.Kill();
            tweenPropScale?.Kill();

            if (isApply) {
                NeutralHandPosition.setupAnchor?.ApplyTrigger();
                setupModeHandState?.Transform.setupAnchor?.ApplyTrigger();
                setupModeBodyState?.Transform.setupAnchor?.ApplyTrigger();
            } else {
                NeutralHandPosition.setupAnchor?.CancelTrigger();
                setupModeHandState?.Transform.setupAnchor?.CancelTrigger();
                setupModeBodyState?.Transform.setupAnchor?.CancelTrigger();
            }
            lastPropState = null;
        }

        void HandlePropCreateAnchor(GameObjectState p, VisualSetupTransform tr, VisualSetupAnchorAsset a) {
            CleanDestroy(propSetupRootAnchor);
            propSetupRootAnchor = Scene.AddAsset<AnchorAsset>();
            propSetupRootAnchor.Attachable.Parent = handAnchor;
            var title = "SETUP_ANCHOR_NAME_PROP_ROOT".Localized();
            propSetupRootAnchor.Name = $"🔒⌛⚓-{title}";
            Scene.UpdateNewAssetName(propSetupRootAnchor);

            a.Attachable.Parent = propSetupRootAnchor;
            a.GetDataInputPort(nameof(a.Animation)).Properties.hidden = true;
            BroadcastDataInputProperties(nameof(a.Animation));

            // TODO: Work aound Warudo AnchorAsset hides scale on focus
            a.Transform.GetDataInputPort(nameof(a.Transform.Scale)).Properties.hidden = false;
            a.Transform.BroadcastDataInputProperties(nameof(a.Transform.Scale));
        }

        PropAsset GetProp(bool skipAutoCreate = false) {
            return GetAsset<PropAsset>(ref PenPropAssetId, skipAutoCreate, newAssetName: $"📦-{LAYER_NAME_PREFIX} Hand Prop");
        }

        async UniTask<AutoCompleteList> GetPropSources() {
            return Context.ResourceManager.ProvideResources("Prop").ToAutoCompleteList();
        }

        void SetPropOffsetTransforms(Vector3 pos, Vector3 rot, Vector3 sca, float time = 0, Ease ease = Ease.Linear) {
            // Tween doesn't seen to have a progress getter, so we're creating our own
            time *= propTweenProgress;
            propTweenProgress = 1 - propTweenProgress;
            tweenPropProgress?.Kill();
            if (time == 0) {
                propTweenProgress = 1f;
            } else {
                tweenPropProgress = DOTween.To(
                    () => propTweenProgress,
                    delegate(float it) { propTweenProgress = it; },
                    1f,
                    time
                ).SetEase(Ease.Linear);
            }

            tweenPropPosition?.Kill();
            if (time == 0) {
                propPositionOffset = pos;
            } else {
                tweenPropPosition = DOTween.To(
                    () => propPositionOffset,
                    delegate(Vector3 it) { propPositionOffset = it; },
                    pos,
                    time
                ).SetEase(ease);
            }

            tweenPropRotation?.Kill();
            if (time == 0) {
                propRotationOffset = rot;
            } else {
                tweenPropRotation = DOTween.To(
                    () => propRotationOffset,
                    delegate(Vector3 it) { propRotationOffset = it; },
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
