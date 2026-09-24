using System;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Utility;

namespace FlameStream {
    public partial class PointerReceiverAsset {

        [DataInput]
        [Hidden]
        public Guid RestHandAnchorAssetId;

        [DataInput]
        [Hidden]
        public Guid RestHandPoleAnchorAssetId;

        AnchorAsset restHandAnchor;
        AnchorAsset restHandPoleAnchor;

        Tween tweenRestLimb;
        Tween tweenRestPole;
        float currentRestLimbWeight = 0f;
        float currentRestPoleWeight = 0f;
        bool lastRestHandActive = false;
        bool lastRestPoleActive = false;

        public float RestLimbWeight => currentRestLimbWeight;
        public float RestPoleWeight => currentRestPoleWeight;

        public AnchorAsset RestHandAnchor => restHandAnchor != null ? restHandAnchor : (restHandAnchor = GetRestHandAnchor());
        public AnchorAsset RestHandPoleAnchor => restHandPoleAnchor != null ? restHandPoleAnchor : (restHandPoleAnchor = GetRestHandPoleAnchor());

        public bool IsRestHandActive => IsEnabled && IsHandEnabled && RestHand != null && RestHand.Enabled;
        public bool IsRestHandPoleActive => (IsRestHandActive || IsInRestHandVisualSetup) && RestHand != null && RestHand.UsePole;
        public bool IsInRestHandVisualSetup => RestHand != null && RestHand.Enabled && ((RestHand.Target != null && RestHand.Target.InVisualSetupMode) || (RestHand.UsePole && RestHand.Pole != null && RestHand.Pole.InVisualSetupMode));
        public bool IsInRestHandPoleVisualSetup => RestHand != null && RestHand.Enabled && RestHand.UsePole && RestHand.Pole != null && RestHand.Pole.InVisualSetupMode;

        public AnchorAsset GetRestHandAnchor(bool skipAutoCreate = false) {
            bool shouldExist = RestHand != null && (RestHand.Enabled || (RestHand.Target != null && RestHand.Target.InVisualSetupMode));
            if (!shouldExist && !skipAutoCreate) {
                return null;
            }
            var anchor = GetAsset<AnchorAsset>(ref RestHandAnchorAssetId, out bool isNewlyCreated, skipAutoCreate, newAssetName: $"⚓-{LAYER_NAME_PREFIX} Rest Hand IK");
            if (anchor != null && anchor.Name.StartsWith($"🔒⚓-{LAYER_NAME_PREFIX}")) {
                anchor.Name = anchor.Name.Replace("🔒⚓-", "⚓-");
                Scene?.UpdateNewAssetName(anchor);
                anchor.Broadcast();
            }
            if (isNewlyCreated && anchor != null) {
                InitializeRestHandAnchor(anchor);
            }
            return anchor;
        }

        public AnchorAsset GetRestHandPoleAnchor(bool skipAutoCreate = false) {
            bool shouldExist = RestHand != null && RestHand.Enabled && (RestHand.UsePole || (RestHand.Pole != null && RestHand.Pole.InVisualSetupMode));
            if (!shouldExist && !skipAutoCreate) {
                return null;
            }
            var anchor = GetAsset<AnchorAsset>(ref RestHandPoleAnchorAssetId, out bool isNewlyCreated, skipAutoCreate, newAssetName: $"⚓-{LAYER_NAME_PREFIX} Rest Hand Pole IK");
            if (anchor != null && anchor.Name.StartsWith($"🔒⚓-{LAYER_NAME_PREFIX}")) {
                anchor.Name = anchor.Name.Replace("🔒⚓-", "⚓-");
                Scene?.UpdateNewAssetName(anchor);
                anchor.Broadcast();
            }
            if (isNewlyCreated && anchor != null) {
                InitializeRestHandPoleAnchor(anchor);
            }
            return anchor;
        }

        public void DestroyRestHandAnchor() {
            if (RestHandAnchorAssetId != Guid.Empty) {
                try { Scene?.RemoveAsset(RestHandAnchorAssetId); } catch {}
                RestHandAnchorAssetId = Guid.Empty;
                restHandAnchor = null;
            }
        }

        public void DestroyRestHandPoleAnchor() {
            if (RestHandPoleAnchorAssetId != Guid.Empty) {
                try { Scene?.RemoveAsset(RestHandPoleAnchorAssetId); } catch {}
                RestHandPoleAnchorAssetId = Guid.Empty;
                restHandPoleAnchor = null;
            }
        }

        RestHandParentType currentRestHandParentType = RestHandParentType.Character;

        public RestHandParentType GetAnchorParentType(AnchorAsset anchor) {
            if (anchor == null || anchor.Attachable == null || anchor.Attachable.Parent == null) {
                return RestHandParentType.World;
            }
            if (anchor.Attachable.Parent is CharacterAsset) {
                return RestHandParentType.Character;
            }
            return RestHandParentType.Screen;
        }

        public Transform GetRestHandParentTransform(RestHandParentType parentType) {
            switch (parentType) {
                case RestHandParentType.Character:
                    return Character?.MainTransform ?? Character?.GameObject?.transform;
                case RestHandParentType.Screen:
                    var screen = screenAsset ?? GetSurfaceScreen();
                    return screen?.GameObject?.transform;
                case RestHandParentType.World:
                default:
                    return null;
            }
        }

        public void TransformRestHandLocalToWorld(RestHandParentType parentType, Vector3 localPos, Vector3 localRot, out Vector3 worldPos, out Quaternion worldRot) {
            var p = GetRestHandParentTransform(parentType);
            if (p != null) {
                worldPos = p.position + p.rotation * localPos;
                worldRot = p.rotation * Quaternion.Euler(localRot);
            } else {
                worldPos = localPos;
                worldRot = Quaternion.Euler(localRot);
            }
        }

        public void TransformRestHandWorldToLocal(RestHandParentType parentType, Vector3 worldPos, Quaternion worldRot, out Vector3 localPos, out Vector3 localRot) {
            var p = GetRestHandParentTransform(parentType);
            if (p != null) {
                localPos = Quaternion.Inverse(p.rotation) * (worldPos - p.position);
                Quaternion rot = Quaternion.Inverse(p.rotation) * worldRot;
                localRot = Helper.NormalizeRotationAngles(rot.eulerAngles);
            } else {
                localPos = worldPos;
                localRot = Helper.NormalizeRotationAngles(worldRot.eulerAngles);
            }
        }

        public void AttachAnchorToParent(AnchorAsset anchor, RestHandParentType parentType) {
            if (anchor == null) return;
            switch (parentType) {
                case RestHandParentType.Character:
                    anchor.Attachable.Parent = Character;
                    anchor.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.TransformPath;
                    anchor.Attachable.AttachToTransform = null;
                    break;
                case RestHandParentType.Screen:
                    var screen = screenAsset ?? GetSurfaceScreen();
                    anchor.Attachable.Parent = screen;
                    anchor.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.TransformPath;
                    anchor.Attachable.AttachToTransform = null;
                    break;
                case RestHandParentType.World:
                    anchor.Attachable.Parent = null;
                    break;
            }
            if (anchor.GameObject != null && anchor.Attachable?.Transform != null) {
                anchor.GameObject.transform.SetParent(anchor.Attachable.Transform, false);
            }
        }

        void SetAnchorParentInitial(AnchorAsset anchor, RestHandParentType parentType) {
            AttachAnchorToParent(anchor, parentType);
        }

        void InitializeRestHandAnchor(AnchorAsset anchor) {
            var parentType = RestHand?.ParentType ?? RestHandParentType.Character;
            SetAnchorParentInitial(anchor, parentType);
            if (RestHand?.Target != null) {
                anchor.Transform.Position = RestHand.Target.Position;
                anchor.Transform.Rotation = KeyboardReceiverAsset.ApplyHandIKIntermediateRotation(IsRightHanded, RestHand.Target.Rotation);
            } else {
                Vector3 defaultCharPos = IsRightHanded ? new Vector3(-0.25f, 0.65f, 0.25f) : new Vector3(0.25f, 0.65f, 0.25f);
                TransformRestHandLocalToWorld(RestHandParentType.Character, defaultCharPos, Vector3.zero, out Vector3 worldPos, out Quaternion worldRot);
                TransformRestHandWorldToLocal(parentType, worldPos, worldRot, out Vector3 localPos, out Vector3 localRot);
                anchor.Transform.Position = localPos;
                anchor.Transform.Rotation = KeyboardReceiverAsset.ApplyHandIKIntermediateRotation(IsRightHanded, localRot);
            }
            anchor.Transform.Scale = Vector3.one;
            anchor.Transform.Broadcast();
        }

        void InitializeRestHandPoleAnchor(AnchorAsset anchor) {
            var parentType = RestHand?.ParentType ?? RestHandParentType.Character;
            SetAnchorParentInitial(anchor, parentType);
            if (RestHand?.Pole != null) {
                anchor.Transform.Position = RestHand.Pole.Position;
                anchor.Transform.Rotation = RestHand.Pole.Rotation;
            } else {
                Vector3 defaultCharPos = IsRightHanded ? new Vector3(-0.4f, 0.65f, -0.2f) : new Vector3(0.4f, 0.65f, -0.2f);
                TransformRestHandLocalToWorld(RestHandParentType.Character, defaultCharPos, Vector3.zero, out Vector3 worldPos, out Quaternion worldRot);
                TransformRestHandWorldToLocal(parentType, worldPos, worldRot, out Vector3 localPos, out Vector3 localRot);
                anchor.Transform.Position = localPos;
                anchor.Transform.Rotation = localRot;
            }
            anchor.Transform.Scale = Vector3.one;
            anchor.Transform.Broadcast();
        }

        public void OnRestHandParentTypeChange() {
            var newParentType = RestHand?.ParentType ?? RestHandParentType.Character;
            var targetAnchor = GetRestHandAnchor(true);
            var poleAnchor = GetRestHandPoleAnchor(true);

            var oldParentType = targetAnchor != null
                ? GetAnchorParentType(targetAnchor)
                : currentRestHandParentType;

            if (newParentType == oldParentType) {
                currentRestHandParentType = newParentType;
                return;
            }

            // 1. Convert Target
            if (RestHand?.Target != null) {
                Vector3 currentLocalPos = targetAnchor != null ? targetAnchor.Transform.Position : RestHand.Target.Position;
                Vector3 currentLocalRot = targetAnchor != null 
                    ? targetAnchor.Transform.Rotation 
                    : KeyboardReceiverAsset.ApplyHandIKIntermediateRotation(IsRightHanded, RestHand.Target.Rotation);

                TransformRestHandLocalToWorld(oldParentType, currentLocalPos, currentLocalRot, out Vector3 worldPos, out Quaternion worldRot);
                TransformRestHandWorldToLocal(newParentType, worldPos, worldRot, out Vector3 newLocalPos, out Vector3 newLocalRot);

                Vector3 targetStoredRot = KeyboardReceiverAsset.RemoveHandIKIntermediateRotation(IsRightHanded, newLocalRot);

                RestHand.Target.Position = newLocalPos;
                RestHand.Target.Rotation = targetStoredRot;
                RestHand.Target.Scale = Vector3.one;
                RestHand.Target.SetDataInput(nameof(RestHand.Target.Position), newLocalPos, true);
                RestHand.Target.SetDataInput(nameof(RestHand.Target.Rotation), targetStoredRot, true);
                RestHand.Target.Broadcast();

                if (targetAnchor != null) {
                    AttachAnchorToParent(targetAnchor, newParentType);
                    targetAnchor.Transform.Position = newLocalPos;
                    targetAnchor.Transform.Rotation = newLocalRot;
                    targetAnchor.Transform.Scale = Vector3.one;
                    targetAnchor.Transform.Broadcast();
                    if (targetAnchor.GameObject != null) {
                        targetAnchor.GameObject.transform.localPosition = newLocalPos;
                        targetAnchor.GameObject.transform.localRotation = Quaternion.Euler(newLocalRot);
                        targetAnchor.GameObject.transform.localScale = Vector3.one;
                    }
                }

                if (RestHand.Target.setupAnchor != null) {
                    AttachAnchorToParent(RestHand.Target.setupAnchor, newParentType);
                    RestHand.Target.setupAnchor.Transform.Position = newLocalPos;
                    RestHand.Target.setupAnchor.Transform.Rotation = targetStoredRot;
                    RestHand.Target.setupAnchor.Transform.Scale = Vector3.one;
                    RestHand.Target.setupAnchor.Transform.Broadcast();
                    if (RestHand.Target.setupAnchor.GameObject != null) {
                        RestHand.Target.setupAnchor.GameObject.transform.localPosition = newLocalPos;
                        RestHand.Target.setupAnchor.GameObject.transform.localRotation = Quaternion.Euler(targetStoredRot);
                        RestHand.Target.setupAnchor.GameObject.transform.localScale = Vector3.one;
                    }
                }
            }

            // 2. Convert Pole
            if (RestHand?.Pole != null) {
                Vector3 currentPolePos = poleAnchor != null ? poleAnchor.Transform.Position : RestHand.Pole.Position;
                Vector3 currentPoleRot = poleAnchor != null ? poleAnchor.Transform.Rotation : RestHand.Pole.Rotation;

                TransformRestHandLocalToWorld(oldParentType, currentPolePos, currentPoleRot, out Vector3 poleWorldPos, out Quaternion poleWorldRot);
                TransformRestHandWorldToLocal(newParentType, poleWorldPos, poleWorldRot, out Vector3 newPolePos, out Vector3 newPoleRot);

                RestHand.Pole.Position = newPolePos;
                RestHand.Pole.Rotation = newPoleRot;
                RestHand.Pole.Scale = Vector3.one;
                RestHand.Pole.SetDataInput(nameof(RestHand.Pole.Position), newPolePos, true);
                RestHand.Pole.SetDataInput(nameof(RestHand.Pole.Rotation), newPoleRot, true);
                RestHand.Pole.Broadcast();

                if (poleAnchor != null) {
                    AttachAnchorToParent(poleAnchor, newParentType);
                    poleAnchor.Transform.Position = newPolePos;
                    poleAnchor.Transform.Rotation = newPoleRot;
                    poleAnchor.Transform.Scale = Vector3.one;
                    poleAnchor.Transform.Broadcast();
                    if (poleAnchor.GameObject != null) {
                        poleAnchor.GameObject.transform.localPosition = newPolePos;
                        poleAnchor.GameObject.transform.localRotation = Quaternion.Euler(newPoleRot);
                        poleAnchor.GameObject.transform.localScale = Vector3.one;
                    }
                }

                if (RestHand.Pole.setupAnchor != null) {
                    AttachAnchorToParent(RestHand.Pole.setupAnchor, newParentType);
                    RestHand.Pole.setupAnchor.Transform.Position = newPolePos;
                    RestHand.Pole.setupAnchor.Transform.Rotation = newPoleRot;
                    RestHand.Pole.setupAnchor.Transform.Scale = Vector3.one;
                    RestHand.Pole.setupAnchor.Transform.Broadcast();
                    if (RestHand.Pole.setupAnchor.GameObject != null) {
                        RestHand.Pole.setupAnchor.GameObject.transform.localPosition = newPolePos;
                        RestHand.Pole.setupAnchor.GameObject.transform.localRotation = Quaternion.Euler(newPoleRot);
                        RestHand.Pole.setupAnchor.GameObject.transform.localScale = Vector3.one;
                    }
                }
            }

            currentRestHandParentType = newParentType;
            RequestUiRefresh();
        }

        public void TransitionRestHand(bool enable) {
            tweenRestLimb?.Kill();
            float targetWeight = enable ? 1.0f : 0.0f;
            float duration = ActiveTransition != null ? ActiveTransition.Time : 0.2f;
            var ease = ActiveTransition != null ? ActiveTransition.Ease : Ease.OutQuad;

            var restLimb = IsRightHanded ? Character?.LeftHandIK : Character?.RightHandIK;

            if (enable) {
                var anchor = GetRestHandAnchor();
                if (restLimb != null && activeCoordinator == null) {
                    restLimb.Enabled = true;
                    restLimb.IkTarget = anchor;
                }
            }

            if (duration <= 0f) {
                currentRestLimbWeight = targetWeight;
                if (restLimb != null && activeCoordinator == null) {
                    restLimb.PositionWeight = targetWeight;
                    restLimb.RotationWeight = targetWeight;
                    if (!enable) {
                        if (restLimb.IkTarget == restHandAnchor) {
                            restLimb.IkTarget = null;
                        }
                    }
                    restLimb.Broadcast();
                }
                if (!enable) {
                    DestroyRestHandAnchor();
                }
                return;
            }

            tweenRestLimb = DOTween.To(
                () => currentRestLimbWeight,
                (w) => {
                    currentRestLimbWeight = w;
                    if (restLimb != null && activeCoordinator == null) {
                        restLimb.PositionWeight = w;
                        restLimb.RotationWeight = w;
                        restLimb.Broadcast();
                    }
                },
                targetWeight,
                duration
            ).SetEase(ease).OnComplete(() => {
                if (!enable) {
                    if (restLimb != null && restLimb.IkTarget == restHandAnchor && activeCoordinator == null) {
                        restLimb.IkTarget = null;
                        restLimb.Broadcast();
                    }
                    DestroyRestHandAnchor();
                }
            });
        }

        public void TransitionRestPole(bool enable) {
            tweenRestPole?.Kill();
            float targetWeight = enable ? 1.0f : 0.0f;
            float duration = ActiveTransition != null ? ActiveTransition.Time : 0.2f;
            var ease = ActiveTransition != null ? ActiveTransition.Ease : Ease.OutQuad;

            var restLimb = IsRightHanded ? Character?.LeftHandIK : Character?.RightHandIK;

            if (enable) {
                var poleAnchor = GetRestHandPoleAnchor();
                if (restLimb != null && activeCoordinator == null) {
                    restLimb.BendGoalTarget = poleAnchor;
                }
            }

            if (duration <= 0f) {
                currentRestPoleWeight = targetWeight;
                if (restLimb != null && activeCoordinator == null) {
                    restLimb.BendGoalWeight = targetWeight;
                    if (!enable) {
                        restLimb.BendGoalTarget = null;
                    }
                    restLimb.Broadcast();
                }
                if (!enable) {
                    DestroyRestHandPoleAnchor();
                }
                return;
            }

            tweenRestPole = DOTween.To(
                () => currentRestPoleWeight,
                (w) => {
                    currentRestPoleWeight = w;
                    if (restLimb != null && activeCoordinator == null) {
                        restLimb.BendGoalWeight = w;
                        restLimb.Broadcast();
                    }
                },
                targetWeight,
                duration
            ).SetEase(ease).OnComplete(() => {
                if (!enable) {
                    if (restLimb != null && restLimb.BendGoalTarget == restHandPoleAnchor && activeCoordinator == null) {
                        restLimb.BendGoalTarget = null;
                        restLimb.Broadcast();
                    }
                    DestroyRestHandPoleAnchor();
                }
            });
        }

        void OnCreateRestHand() {
            if (RestHand == null) {
                RestHand = StructuredData.Create<PointerHandRestSetup>();
            }
            currentRestHandParentType = RestHand?.ParentType ?? RestHandParentType.Character;
            Watch(nameof(IsEnabled), delegate { OnRestHandSettingChange(); });
            Watch(nameof(IsHandEnabled), delegate { OnRestHandSettingChange(); });
            Watch(nameof(RestHand), delegate {
                OnRestHandSettingChange();
                SetupRestHandTransform();
                SetupRestHandPoleTransform();
            });
            Watch(nameof(Character), delegate {
                if (RestHand?.ParentType == RestHandParentType.Character) {
                    var targetAnchor = GetRestHandAnchor(true);
                    if (targetAnchor != null) AttachAnchorToParent(targetAnchor, RestHandParentType.Character);
                    var poleAnchor = GetRestHandPoleAnchor(true);
                    if (poleAnchor != null) AttachAnchorToParent(poleAnchor, RestHandParentType.Character);
                }
            });
        }

        void OnReadyRestHand() {
            currentRestHandParentType = RestHand?.ParentType ?? RestHandParentType.Character;
            RestHand?.SyncUI();
            SetupRestHandTransform();
            SetupRestHandPoleTransform();

            lastRestHandActive = IsRestHandActive;
            lastRestPoleActive = IsRestHandPoleActive;

            if (!IsRestHandPoleActive && RestHandPoleAnchorAssetId != Guid.Empty) {
                DestroyRestHandPoleAnchor();
            }
            if (!IsRestHandActive && RestHandAnchorAssetId != Guid.Empty) {
                DestroyRestHandAnchor();
            }

            if (IsRestHandActive && activeCoordinator == null) {
                var restLimb = IsRightHanded ? Character?.LeftHandIK : Character?.RightHandIK;
                if (restLimb != null) {
                    restLimb.Enabled = true;
                    restLimb.IkTarget = GetRestHandAnchor();
                    currentRestLimbWeight = 1.0f;
                    restLimb.PositionWeight = 1.0f;
                    restLimb.RotationWeight = 1.0f;
                    if (IsRestHandPoleActive) {
                        restLimb.BendGoalTarget = GetRestHandPoleAnchor();
                        currentRestPoleWeight = 1.0f;
                        restLimb.BendGoalWeight = 1.0f;
                    } else {
                        restLimb.BendGoalWeight = 0.0f;
                    }
                    restLimb.Broadcast();
                }
            }
        }

        void OnDestroyRestHand() {
            tweenRestLimb?.Kill();
            tweenRestPole?.Kill();
        }

        public void OnRestHandSettingChange() {
            RestHand?.SyncUI();

            bool currentActive = IsRestHandActive || IsInRestHandVisualSetup;
            bool currentPoleActive = IsRestHandPoleActive;

            if (currentActive != lastRestHandActive) {
                lastRestHandActive = currentActive;
                TransitionRestHand(currentActive);
            }

            if (currentPoleActive != lastRestPoleActive) {
                lastRestPoleActive = currentPoleActive;
                TransitionRestPole(currentPoleActive);
            }

            RequestUiRefresh();
        }

        public void SetupRestHandTransform() {
            if (RestHand?.Target == null) return;
            var target = RestHand.Target;
            target.LocalizationKey = "REST_HAND_TARGET";
            target.HideScale();
            target.CollapsedSelf = true;
            target.ShowOriginIndicator = false;

            target.OnEnterVisualSetup = (tr) => {
                EnsureRestHandAnchors();
                var liveAnchor = GetRestHandAnchor();
                if (liveAnchor != null) {
                    liveAnchor.Transform.Position = tr.Position;
                    liveAnchor.Transform.Rotation = KeyboardReceiverAsset.ApplyHandIKIntermediateRotation(IsRightHanded, tr.Rotation);
                    liveAnchor.Transform.Broadcast();
                }
                var restLimb = IsRightHanded ? Character?.LeftHandIK : Character?.RightHandIK;
                if (restLimb != null && activeCoordinator == null) {
                    restLimb.Enabled = true;
                    restLimb.IkTarget = liveAnchor;
                    currentRestLimbWeight = 1.0f;
                    restLimb.PositionWeight = 1.0f;
                    restLimb.RotationWeight = 1.0f;
                    restLimb.Broadcast();
                }
            };

            target.OnCreateAnchor = (tr, setupAnchor) => {
                var parentType = RestHand?.ParentType ?? RestHandParentType.Character;
                SetAnchorParentInitial(setupAnchor, parentType);

                setupAnchor.Transform.Position = tr.Position;
                setupAnchor.Transform.Rotation = tr.Rotation;
                setupAnchor.Transform.Scale = Vector3.one;
                var handSide = IsRightHanded ? "Left" : "Right";
                setupAnchor.Name = $"⌛⚓-{handSide} Hand Rest Target";
                setupAnchor.Instructions = "REST_HAND_TARGET_INSTRUCTIONS".Localized();
                setupAnchor.Transform.Broadcast();

                var liveAnchor = GetRestHandAnchor();
                if (liveAnchor != null) {
                    liveAnchor.Transform.Position = tr.Position;
                    liveAnchor.Transform.Rotation = KeyboardReceiverAsset.ApplyHandIKIntermediateRotation(IsRightHanded, tr.Rotation);
                    liveAnchor.Transform.Broadcast();
                }
            };

            target.OnResetAnchor = (tr, setupAnchor) => {
                var parentType = RestHand?.ParentType ?? RestHandParentType.Character;
                Vector3 defaultCharPos = IsRightHanded ? new Vector3(-0.25f, 0.65f, 0.25f) : new Vector3(0.25f, 0.65f, 0.25f);
                TransformRestHandLocalToWorld(RestHandParentType.Character, defaultCharPos, Vector3.zero, out Vector3 worldPos, out Quaternion worldRot);
                TransformRestHandWorldToLocal(parentType, worldPos, worldRot, out Vector3 localPos, out Vector3 localRot);

                setupAnchor.Transform.Position = localPos;
                setupAnchor.Transform.Rotation = localRot;
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Transform.Broadcast();
            };

            target.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                tr.Scale = Vector3.one;
                var liveAnchor = GetRestHandAnchor(true);
                if (liveAnchor != null) {
                    liveAnchor.Transform.Position = tr.Position;
                    liveAnchor.Transform.Rotation = KeyboardReceiverAsset.ApplyHandIKIntermediateRotation(IsRightHanded, tr.Rotation);
                    liveAnchor.Transform.Broadcast();
                }
            };

            target.OnExitVisualSetup = (isApplied) => {
                if (!isApplied) {
                    var liveAnchor = GetRestHandAnchor(true);
                    if (liveAnchor != null && RestHand?.Target != null) {
                        liveAnchor.Transform.Position = RestHand.Target.Position;
                        liveAnchor.Transform.Rotation = KeyboardReceiverAsset.ApplyHandIKIntermediateRotation(IsRightHanded, RestHand.Target.Rotation);
                        liveAnchor.Transform.Broadcast();
                    }
                }
                OnRestHandSettingChange();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
                RequestUiRefresh();
            };
        }

        public void SetupRestHandPoleTransform() {
            if (RestHand?.Pole == null) return;
            var pole = RestHand.Pole;
            pole.LocalizationKey = "REST_HAND_POLE";
            pole.HideScale();
            pole.CollapsedSelf = true;
            pole.ShowOriginIndicator = false;

            pole.OnEnterVisualSetup = (tr) => {
                EnsureRestHandAnchors();
                var livePole = GetRestHandPoleAnchor();
                if (livePole != null) {
                    livePole.Transform.Position = tr.Position;
                    livePole.Transform.Rotation = tr.Rotation;
                    livePole.Transform.Broadcast();
                }
                var restLimb = IsRightHanded ? Character?.LeftHandIK : Character?.RightHandIK;
                if (restLimb != null && activeCoordinator == null) {
                    restLimb.BendGoalTarget = livePole;
                    currentRestPoleWeight = 1.0f;
                    restLimb.BendGoalWeight = 1.0f;
                    restLimb.Broadcast();
                }
            };

            pole.OnCreateAnchor = (tr, setupAnchor) => {
                var parentType = RestHand?.ParentType ?? RestHandParentType.Character;
                SetAnchorParentInitial(setupAnchor, parentType);

                setupAnchor.Transform.Position = tr.Position;
                setupAnchor.Transform.Rotation = tr.Rotation;
                setupAnchor.Transform.Scale = Vector3.one;
                var handSide = IsRightHanded ? "Left" : "Right";
                setupAnchor.Name = $"⌛⚓-{handSide} Hand Rest Pole";
                setupAnchor.Instructions = "REST_HAND_POLE_INSTRUCTIONS".Localized();
                setupAnchor.Transform.Broadcast();

                var livePole = GetRestHandPoleAnchor();
                if (livePole != null) {
                    livePole.Transform.Position = tr.Position;
                    livePole.Transform.Rotation = tr.Rotation;
                    livePole.Transform.Broadcast();
                }
            };

            pole.OnResetAnchor = (tr, setupAnchor) => {
                var parentType = RestHand?.ParentType ?? RestHandParentType.Character;
                Vector3 defaultCharPos = IsRightHanded ? new Vector3(-0.4f, 0.65f, -0.2f) : new Vector3(0.4f, 0.65f, -0.2f);
                TransformRestHandLocalToWorld(RestHandParentType.Character, defaultCharPos, Vector3.zero, out Vector3 worldPos, out Quaternion worldRot);
                TransformRestHandWorldToLocal(parentType, worldPos, worldRot, out Vector3 localPos, out Vector3 localRot);

                setupAnchor.Transform.Position = localPos;
                setupAnchor.Transform.Rotation = localRot;
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Transform.Broadcast();
            };

            pole.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                tr.Scale = Vector3.one;
                var livePole = GetRestHandPoleAnchor(true);
                if (livePole != null) {
                    livePole.Transform.Position = tr.Position;
                    livePole.Transform.Rotation = tr.Rotation;
                    livePole.Transform.Broadcast();
                }
            };

            pole.OnExitVisualSetup = (isApplied) => {
                if (!isApplied) {
                    var livePole = GetRestHandPoleAnchor(true);
                    if (livePole != null && RestHand?.Pole != null) {
                        livePole.Transform.Position = RestHand.Pole.Position;
                        livePole.Transform.Rotation = RestHand.Pole.Rotation;
                        livePole.Transform.Broadcast();
                    }
                }
                OnRestHandSettingChange();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
                RequestUiRefresh();
            };
        }

        void EnsureRestHandAnchors() {
            var parentType = RestHand?.ParentType ?? RestHandParentType.Character;
            if (RestHand != null && (RestHand.Enabled || (RestHand.Target != null && RestHand.Target.InVisualSetupMode))) {
                var targetAnchor = GetRestHandAnchor();
                if (targetAnchor != null) AttachAnchorToParent(targetAnchor, parentType);
            }
            if (RestHand != null && (RestHand.UsePole || (RestHand.Pole != null && RestHand.Pole.InVisualSetupMode))) {
                var poleAnchor = GetRestHandPoleAnchor();
                if (poleAnchor != null) AttachAnchorToParent(poleAnchor, parentType);
            }
        }

        public void UpdateRestHandAnchorTransforms(bool force = false) {
            if (!force && !IsInRestHandVisualSetup) return;
            UpdateRestHandVisualSetupLiveTransforms();
        }

        public void UpdateRestHandVisualSetupLiveTransforms() {
            var anchor = GetRestHandAnchor(true);
            var pole = GetRestHandPoleAnchor(true);
            if (anchor == null && pole == null) return;

            if (anchor != null && RestHand?.Target != null && RestHand.Target.InVisualSetupMode && RestHand.Target.setupAnchor != null) {
                Vector3 targetPos = RestHand.Target.setupAnchor.Transform.Position;
                Vector3 targetRot = RestHand.Target.setupAnchor.Transform.Rotation;
                var ikRot = KeyboardReceiverAsset.ApplyHandIKIntermediateRotation(IsRightHanded, targetRot);

                if (anchor.Transform.Position != targetPos || anchor.Transform.Rotation != ikRot) {
                    anchor.Transform.Position = targetPos;
                    anchor.Transform.Rotation = ikRot;
                    anchor.Transform.Broadcast();
                }
            }

            if (pole != null && RestHand?.Pole != null && RestHand.Pole.InVisualSetupMode && RestHand.Pole.setupAnchor != null) {
                Vector3 polePos = RestHand.Pole.setupAnchor.Transform.Position;
                Vector3 poleRot = RestHand.Pole.setupAnchor.Transform.Rotation;
                if (pole.Transform.Position != polePos || pole.Transform.Rotation != poleRot) {
                    pole.Transform.Position = polePos;
                    pole.Transform.Rotation = poleRot;
                    pole.Transform.Broadcast();
                }
            }
        }

        void OnUpdateRestHand() {
            if (IsInRestHandVisualSetup) {
                UpdateRestHandVisualSetupLiveTransforms();
            }

            if (Character == null) return;
            var restLimb = IsRightHanded ? Character.LeftHandIK : Character.RightHandIK;
            if (restLimb == null) return;

            if (activeCoordinator != null) {
                return;
            }

            if (IsInRestHandVisualSetup) {
                restLimb.Enabled = true;
                restLimb.IkTarget = RestHandAnchor;
                restLimb.PositionWeight = 1.0f;
                restLimb.RotationWeight = 1.0f;
                if (IsRestHandPoleActive) {
                    restLimb.BendGoalTarget = RestHandPoleAnchor;
                    restLimb.BendGoalWeight = 1.0f;
                } else {
                    restLimb.BendGoalWeight = 0.0f;
                }
                restLimb.Broadcast();
            }
        }
    }
}
