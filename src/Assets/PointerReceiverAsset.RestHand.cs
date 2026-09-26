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
        public bool IsRestHandPoleActive => (IsRestHandActive || IsInRestHandVisualSetup) && RestHand != null && (
            RestHand.UsePole || 
            (RestHand.ElbowPole != null && (RestHand.ElbowPole.UsePole || IsInRestHandPoleVisualSetup)) ||
            IsInRestHandPoleVisualSetup
        );
        public bool IsInRestHandVisualSetup => RestHand != null && RestHand.Enabled && (
            (RestHand.Target != null && RestHand.Target.InVisualSetupMode) || 
            (RestHand.Pole != null && RestHand.Pole.InVisualSetupMode) ||
            (RestHand.ElbowPole != null && RestHand.ElbowPole.Pole != null && RestHand.ElbowPole.Pole.InVisualSetupMode)
        );
        public bool IsInRestHandPoleVisualSetup => RestHand != null && RestHand.Enabled && (
            (RestHand.Pole != null && RestHand.Pole.InVisualSetupMode) ||
            (RestHand.ElbowPole != null && RestHand.ElbowPole.Pole != null && RestHand.ElbowPole.Pole.InVisualSetupMode)
        );

        public AnchorAsset GetRestHandAnchor(bool skipAutoCreate = false) {
            bool shouldExist = RestHand != null && (RestHand.Enabled || (RestHand.Target != null && RestHand.Target.InVisualSetupMode));
            if (!shouldExist && !skipAutoCreate) {
                return null;
            }
            var anchor = GetAsset<AnchorAsset>(ref RestHandAnchorAssetId, out bool isNewlyCreated, skipAutoCreate, newAssetName: $"⚓-{LAYER_NAME_PREFIX} Rest Hand IK", category: "FS_ASSET_CATEGORY_INPUT".Localized());
            if (anchor != null && anchor.Name.StartsWith($"🔒⚓-{LAYER_NAME_PREFIX}")) {
                anchor.Name = anchor.Name.Replace("🔒⚓-", "⚓-");
                Scene?.UpdateNewAssetName(anchor);
                anchor.Broadcast();
                try { Scene?.AddAssetToGroup(anchor, "FS_ASSET_CATEGORY_INPUT".Localized()); } catch {}
            }
            if (isNewlyCreated && anchor != null) {
                InitializeRestHandAnchor(anchor);
            }
            return anchor;
        }

        public AnchorAsset GetRestHandPoleAnchor(bool skipAutoCreate = false) {
            bool shouldExist = RestHand != null && RestHand.Enabled && (
                RestHand.UsePole || 
                (RestHand.Pole != null && RestHand.Pole.InVisualSetupMode) ||
                (RestHand.ElbowPole != null && (RestHand.ElbowPole.UsePole || (RestHand.ElbowPole.Pole != null && RestHand.ElbowPole.Pole.InVisualSetupMode)))
            );
            if (!shouldExist && !skipAutoCreate) {
                return null;
            }
            var anchor = GetAsset<AnchorAsset>(ref RestHandPoleAnchorAssetId, out bool isNewlyCreated, skipAutoCreate, newAssetName: $"⚓-{LAYER_NAME_PREFIX} Rest Hand Pole IK", category: "FS_ASSET_CATEGORY_INPUT".Localized());
            if (anchor != null && anchor.Name.StartsWith($"🔒⚓-{LAYER_NAME_PREFIX}")) {
                anchor.Name = anchor.Name.Replace("🔒⚓-", "⚓-");
                Scene?.UpdateNewAssetName(anchor);
                anchor.Broadcast();
                try { Scene?.AddAssetToGroup(anchor, "FS_ASSET_CATEGORY_INPUT".Localized()); } catch {}
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

        AnchorParentType currentRestHandParentType = AnchorParentType.World;

        public AnchorParentType GetAnchorParentType(AnchorAsset anchor) {
            if (anchor == null || anchor.Attachable == null || anchor.Attachable.Parent == null) {
                return AnchorParentType.World;
            }
            if (anchor.Attachable.Parent is CharacterAsset) {
                if (anchor.Attachable.AttachType == Warudo.Plugins.Core.Assets.Mixins.AttachType.HumanBodyBone) {
                    var bone = anchor.Attachable.AttachToBone;
                    if (bone == HumanBodyBones.RightShoulder || bone == HumanBodyBones.LeftShoulder ||
                        bone == HumanBodyBones.UpperChest || bone == HumanBodyBones.Chest || bone == HumanBodyBones.Spine ||
                        bone == HumanBodyBones.RightUpperArm || bone == HumanBodyBones.LeftUpperArm) {
                        return AnchorParentType.CharacterShoulder;
                    }
                }
                return AnchorParentType.Character;
            }
            return AnchorParentType.Screen;
        }

        public Transform GetAnchorParentTransform(AnchorParentType parentType, bool isRight) {
            switch (parentType) {
                case AnchorParentType.Character:
                    return Character?.MainTransform ?? Character?.GameObject?.transform;
                case AnchorParentType.CharacterShoulder:
                    var anim = Character?.Animator ?? Character?.CloneAnimator;
                    var shoulderBone = anim != null ? anim.GetBoneTransform(isRight ? HumanBodyBones.RightShoulder : HumanBodyBones.LeftShoulder) : null;
                    if (shoulderBone != null) return shoulderBone;
                    var torsoBone = anim != null ? (anim.GetBoneTransform(HumanBodyBones.UpperChest) ?? anim.GetBoneTransform(HumanBodyBones.Chest) ?? anim.GetBoneTransform(HumanBodyBones.Spine)) : null;
                    return torsoBone ?? (Character?.MainTransform ?? Character?.GameObject?.transform);
                case AnchorParentType.Screen:
                    var screen = screenAsset ?? GetSurfaceScreen();
                    return screen?.GameObject?.transform;
                case AnchorParentType.World:
                default:
                    return null;
            }
        }

        public Transform GetRestHandParentTransform(AnchorParentType parentType) {
            return GetAnchorParentTransform(parentType, !IsRightHanded);
        }

        public void TransformAnchorLocalToWorld(AnchorParentType parentType, Vector3 localPos, Vector3 localRot, out Vector3 worldPos, out Quaternion worldRot, bool isRight) {
            var p = GetAnchorParentTransform(parentType, isRight);
            if (p != null) {
                worldPos = p.position + p.rotation * localPos;
                worldRot = p.rotation * Quaternion.Euler(localRot);
            } else {
                worldPos = localPos;
                worldRot = Quaternion.Euler(localRot);
            }
        }

        public void TransformAnchorWorldToLocal(AnchorParentType parentType, Vector3 worldPos, Quaternion worldRot, out Vector3 localPos, out Vector3 localRot, bool isRight) {
            var p = GetAnchorParentTransform(parentType, isRight);
            if (p != null) {
                localPos = Quaternion.Inverse(p.rotation) * (worldPos - p.position);
                Quaternion rot = Quaternion.Inverse(p.rotation) * worldRot;
                localRot = Helper.NormalizeRotationAngles(rot.eulerAngles);
            } else {
                localPos = worldPos;
                localRot = Helper.NormalizeRotationAngles(worldRot.eulerAngles);
            }
        }

        public void TransformRestHandLocalToWorld(AnchorParentType parentType, Vector3 localPos, Vector3 localRot, out Vector3 worldPos, out Quaternion worldRot) {
            TransformAnchorLocalToWorld(parentType, localPos, localRot, out worldPos, out worldRot, !IsRightHanded);
        }

        public void TransformRestHandWorldToLocal(AnchorParentType parentType, Vector3 worldPos, Quaternion worldRot, out Vector3 localPos, out Vector3 localRot) {
            TransformAnchorWorldToLocal(parentType, worldPos, worldRot, out localPos, out localRot, !IsRightHanded);
        }

        public void AttachAnchorToParent(AnchorAsset anchor, AnchorParentType parentType, bool isRight) {
            if (anchor == null) return;
            switch (parentType) {
                case AnchorParentType.Character:
                    anchor.Attachable.Parent = Character;
                    anchor.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.TransformPath;
                    anchor.Attachable.AttachToTransform = null;
                    break;
                case AnchorParentType.CharacterShoulder:
                    anchor.Attachable.Parent = Character;
                    anchor.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.HumanBodyBone;
                    var animCheck = Character?.Animator ?? Character?.CloneAnimator;
                    var hasShoulder = animCheck != null && animCheck.GetBoneTransform(isRight ? HumanBodyBones.RightShoulder : HumanBodyBones.LeftShoulder) != null;
                    if (hasShoulder) {
                        anchor.Attachable.AttachToBone = isRight ? HumanBodyBones.RightShoulder : HumanBodyBones.LeftShoulder;
                    } else {
                        // Fallback to torso (UpperChest -> Chest -> Spine)
                        var hasUpperChest = animCheck != null && animCheck.GetBoneTransform(HumanBodyBones.UpperChest) != null;
                        var hasChest = animCheck != null && animCheck.GetBoneTransform(HumanBodyBones.Chest) != null;
                        anchor.Attachable.AttachToBone = hasUpperChest ? HumanBodyBones.UpperChest : (hasChest ? HumanBodyBones.Chest : HumanBodyBones.Spine);
                    }
                    anchor.Attachable.AttachToTransform = null;
                    break;
                case AnchorParentType.Screen:
                    var screen = screenAsset ?? GetSurfaceScreen();
                    anchor.Attachable.Parent = screen;
                    anchor.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.TransformPath;
                    anchor.Attachable.AttachToTransform = null;
                    break;
                case AnchorParentType.World:
                    anchor.Attachable.Parent = null;
                    break;
            }
            if (anchor.GameObject != null && anchor.Attachable?.Transform != null) {
                anchor.GameObject.transform.SetParent(anchor.Attachable.Transform, false);
            }
        }

        public void AttachAnchorToParent(AnchorAsset anchor, AnchorParentType parentType) {
            AttachAnchorToParent(anchor, parentType, !IsRightHanded);
        }

        void SetAnchorParentInitial(AnchorAsset anchor, AnchorParentType parentType, bool isRight) {
            AttachAnchorToParent(anchor, parentType, isRight);
        }

        void SetAnchorParentInitial(AnchorAsset anchor, AnchorParentType parentType) {
            AttachAnchorToParent(anchor, parentType, !IsRightHanded);
        }

        void InitializeRestHandAnchor(AnchorAsset anchor) {
            var parentType = RestHand?.ParentType ?? AnchorParentType.World;
            SetAnchorParentInitial(anchor, parentType, !IsRightHanded);
            if (RestHand?.Target != null && RestHand.Target.Position != Vector3.zero) {
                anchor.Transform.Position = RestHand.Target.Position;
                anchor.Transform.Rotation = KeyboardReceiverAsset.ApplyHandIKIntermediateRotation(IsRightHanded, RestHand.Target.Rotation);
            } else {
                Vector3 defaultCharPos = IsRightHanded ? new Vector3(-0.25f, 0.65f, 0.25f) : new Vector3(0.25f, 0.65f, 0.25f);
                TransformAnchorLocalToWorld(AnchorParentType.Character, defaultCharPos, Vector3.zero, out Vector3 worldPos, out Quaternion worldRot, !IsRightHanded);
                TransformAnchorWorldToLocal(parentType, worldPos, worldRot, out Vector3 localPos, out Vector3 localRot, !IsRightHanded);
                anchor.Transform.Position = localPos;
                anchor.Transform.Rotation = KeyboardReceiverAsset.ApplyHandIKIntermediateRotation(IsRightHanded, localRot);
                if (RestHand?.Target != null) {
                    RestHand.Target.Position = localPos;
                    RestHand.Target.Rotation = KeyboardReceiverAsset.RemoveHandIKIntermediateRotation(IsRightHanded, localRot);
                    RestHand.Target.SetDataInput(nameof(RestHand.Target.Position), localPos, true);
                    RestHand.Target.SetDataInput(nameof(RestHand.Target.Rotation), RestHand.Target.Rotation, true);
                }
            }
            anchor.Transform.Scale = Vector3.one;
            anchor.Transform.Broadcast();
        }

        void InitializeRestHandPoleAnchor(AnchorAsset anchor) {
            var parentType = RestHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
            SetAnchorParentInitial(anchor, parentType, !IsRightHanded);
            if (RestHand?.ElbowPole?.Pole != null && RestHand.ElbowPole.Pole.Position != Vector3.zero) {
                anchor.Transform.Position = RestHand.ElbowPole.Pole.Position;
                anchor.Transform.Rotation = RestHand.ElbowPole.Pole.Rotation;
            } else {
                Vector3 defaultWorld = GetDefaultElbowPoleWorldPosition(!IsRightHanded);
                TransformAnchorWorldToLocal(parentType, defaultWorld, Quaternion.identity, out Vector3 localPos, out Vector3 localRot, !IsRightHanded);
                anchor.Transform.Position = localPos;
                anchor.Transform.Rotation = localRot;
                if (RestHand?.ElbowPole?.Pole != null) {
                    RestHand.ElbowPole.Pole.Position = localPos;
                    RestHand.ElbowPole.Pole.Rotation = localRot;
                    RestHand.ElbowPole.Pole.SetDataInput(nameof(RestHand.ElbowPole.Pole.Position), localPos, true);
                    RestHand.ElbowPole.Pole.SetDataInput(nameof(RestHand.ElbowPole.Pole.Rotation), localRot, true);
                }
            }
            anchor.Transform.Scale = Vector3.one;
            anchor.Transform.Broadcast();
        }

        AnchorParentType currentRestHandPoleParentType = AnchorParentType.World;

        public void OnRestHandParentTypeChange() {
            var newParentType = RestHand?.ParentType ?? AnchorParentType.World;
            var targetAnchor = GetRestHandAnchor(true);

            var oldParentType = targetAnchor != null
                ? GetAnchorParentType(targetAnchor)
                : currentRestHandParentType;

            if (newParentType == oldParentType) {
                currentRestHandParentType = newParentType;
                return;
            }

            // Convert Target
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

            currentRestHandParentType = newParentType;
            RequestUiRefresh();
        }

        public void OnRestHandPoleParentTypeChange() {
            var newParentType = RestHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
            var poleAnchor = GetRestHandPoleAnchor(true);

            var oldParentType = poleAnchor != null
                ? GetAnchorParentType(poleAnchor)
                : currentRestHandPoleParentType;

            if (newParentType == oldParentType) {
                currentRestHandPoleParentType = newParentType;
                return;
            }

            // Convert Pole
            var pole = RestHand?.ElbowPole?.Pole ?? RestHand?.Pole;
            if (pole != null) {
                Vector3 currentPolePos = poleAnchor != null ? poleAnchor.Transform.Position : pole.Position;
                Vector3 currentPoleRot = poleAnchor != null ? poleAnchor.Transform.Rotation : pole.Rotation;

                TransformAnchorLocalToWorld(oldParentType, currentPolePos, currentPoleRot, out Vector3 poleWorldPos, out Quaternion poleWorldRot, !IsRightHanded);
                TransformAnchorWorldToLocal(newParentType, poleWorldPos, poleWorldRot, out Vector3 newPolePos, out Vector3 newPoleRot, !IsRightHanded);

                pole.Position = newPolePos;
                pole.Rotation = newPoleRot;
                pole.Scale = Vector3.one;
                pole.SetDataInput(nameof(pole.Position), newPolePos, true);
                pole.SetDataInput(nameof(pole.Rotation), newPoleRot, true);
                pole.Broadcast();

                if (poleAnchor != null) {
                    AttachAnchorToParent(poleAnchor, newParentType, !IsRightHanded);
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

                if (pole.setupAnchor != null) {
                    AttachAnchorToParent(pole.setupAnchor, newParentType, !IsRightHanded);
                    pole.setupAnchor.Transform.Position = newPolePos;
                    pole.setupAnchor.Transform.Rotation = newPoleRot;
                    pole.setupAnchor.Transform.Scale = Vector3.one;
                    pole.setupAnchor.Transform.Broadcast();
                    if (pole.setupAnchor.GameObject != null) {
                        pole.setupAnchor.GameObject.transform.localPosition = newPolePos;
                        pole.setupAnchor.GameObject.transform.localRotation = Quaternion.Euler(newPoleRot);
                        pole.setupAnchor.GameObject.transform.localScale = Vector3.one;
                    }
                }
            }

            currentRestHandPoleParentType = newParentType;
            RequestUiRefresh();
        }

        public void TransitionRestHand(bool enable) {
            tweenRestLimb?.Kill();
            var restLimb = IsRightHanded ? Character?.LeftHandIK : Character?.RightHandIK;
            if (restLimb == null && Character != null) {
                DestroyRestHandAnchor();
                return;
            }

            float duration = enable 
                ? (ActiveTransition != null ? ActiveTransition.Time : 0.5f)
                : (InactiveTransition != null ? InactiveTransition.Time : 0.5f);
            var ease = enable
                ? (ActiveTransition != null ? ActiveTransition.Ease : Ease.OutQuad)
                : (InactiveTransition != null ? InactiveTransition.Ease : Ease.Linear);
            var delay = (enable && ActiveTransition != null) ? ActiveTransition.Delay : 0f;

            if (enable) {
                var anchor = GetRestHandAnchor();
                if (restLimb != null && activeCoordinator == null) {
                    if (restLimb.IkTarget != anchor || !restLimb.Enabled) {
                        restLimb.PositionWeight = 0f;
                        restLimb.RotationWeight = 0f;
                        currentRestLimbWeight = 0f;
                    }
                    restLimb.Enabled = true;
                    restLimb.IkTarget = anchor;
                    restLimb.Broadcast();
                }

                float currentWeight = restLimb != null ? Mathf.Clamp01(restLimb.PositionWeight) : Mathf.Clamp01(currentRestLimbWeight);
                float remaining = 1.0f - currentWeight;
                float actualDuration = duration * remaining;

                if (actualDuration <= 0f) {
                    currentRestLimbWeight = 1.0f;
                    if (restLimb != null && activeCoordinator == null) {
                        restLimb.PositionWeight = 1.0f;
                        restLimb.RotationWeight = 1.0f;
                        restLimb.Broadcast();
                    }
                    return;
                }

                currentRestLimbWeight = currentWeight;
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
                    1.0f,
                    actualDuration
                ).SetEase(ease);

                if (delay > 0f) {
                    tweenRestLimb.SetDelay(delay);
                }
            } else {
                float currentWeight = restLimb != null ? Mathf.Clamp01(restLimb.PositionWeight) : Mathf.Clamp01(currentRestLimbWeight);
                float actualDuration = duration * currentWeight;

                if (currentWeight <= 0.001f || actualDuration <= 0f) {
                    currentRestLimbWeight = 0f;
                    if (restLimb != null && activeCoordinator == null) {
                        restLimb.PositionWeight = 0f;
                        restLimb.RotationWeight = 0f;
                        if (restLimb.IkTarget == restHandAnchor) {
                            restLimb.IkTarget = null;
                            restLimb.Enabled = false;
                        }
                        restLimb.Broadcast();
                    }
                    DestroyRestHandAnchor();
                    return;
                }

                currentRestLimbWeight = currentWeight;
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
                    0.0f,
                    actualDuration
                ).SetEase(ease).OnComplete(() => {
                    if (restLimb != null && restLimb.IkTarget == restHandAnchor && activeCoordinator == null) {
                        restLimb.IkTarget = null;
                        restLimb.Enabled = false;
                        restLimb.Broadcast();
                    }
                    DestroyRestHandAnchor();
                });
            }
        }

        public void TransitionRestPole(bool enable) {
            tweenRestPole?.Kill();
            var restLimb = IsRightHanded ? Character?.LeftHandIK : Character?.RightHandIK;
            float duration = enable
                ? (ActiveTransition != null ? ActiveTransition.Time : 0.5f)
                : (InactiveTransition != null ? InactiveTransition.Time : 0.5f);
            var ease = enable
                ? (ActiveTransition != null ? ActiveTransition.Ease : Ease.OutQuad)
                : (InactiveTransition != null ? InactiveTransition.Ease : Ease.Linear);
            var delay = (enable && ActiveTransition != null) ? ActiveTransition.Delay : 0f;

            if (enable) {
                var poleAnchor = GetRestHandPoleAnchor();
                if (restLimb != null && activeCoordinator == null) {
                    if (restLimb.BendGoalTarget != poleAnchor) {
                        restLimb.BendGoalWeight = 0f;
                        currentRestPoleWeight = 0f;
                    }
                    restLimb.BendGoalTarget = poleAnchor;
                    restLimb.Broadcast();
                }

                float currentWeight = restLimb != null ? Mathf.Clamp01(restLimb.BendGoalWeight) : Mathf.Clamp01(currentRestPoleWeight);
                float remaining = 1.0f - currentWeight;
                float actualDuration = duration * remaining;

                if (actualDuration <= 0f) {
                    currentRestPoleWeight = 1.0f;
                    if (restLimb != null && activeCoordinator == null) {
                        restLimb.BendGoalWeight = 1.0f;
                        restLimb.Broadcast();
                    }
                    return;
                }

                currentRestPoleWeight = currentWeight;
                tweenRestPole = DOTween.To(
                    () => currentRestPoleWeight,
                    (w) => {
                        currentRestPoleWeight = w;
                        if (restLimb != null && activeCoordinator == null) {
                            restLimb.BendGoalWeight = w;
                            restLimb.Broadcast();
                        }
                    },
                    1.0f,
                    actualDuration
                ).SetEase(ease);

                if (delay > 0f) {
                    tweenRestPole.SetDelay(delay);
                }
            } else {
                float currentWeight = restLimb != null ? Mathf.Clamp01(restLimb.BendGoalWeight) : Mathf.Clamp01(currentRestPoleWeight);
                float actualDuration = duration * currentWeight;

                if (currentWeight <= 0.001f || actualDuration <= 0f) {
                    currentRestPoleWeight = 0f;
                    if (restLimb != null && activeCoordinator == null) {
                        restLimb.BendGoalWeight = 0f;
                        if (restLimb.BendGoalTarget == restHandPoleAnchor) {
                            restLimb.BendGoalTarget = null;
                        }
                        restLimb.Broadcast();
                    }
                    DestroyRestHandPoleAnchor();
                    return;
                }

                currentRestPoleWeight = currentWeight;
                tweenRestPole = DOTween.To(
                    () => currentRestPoleWeight,
                    (w) => {
                        currentRestPoleWeight = w;
                        if (restLimb != null && activeCoordinator == null) {
                            restLimb.BendGoalWeight = w;
                            restLimb.Broadcast();
                        }
                    },
                    0.0f,
                    actualDuration
                ).SetEase(ease).OnComplete(() => {
                    if (restLimb != null && restLimb.BendGoalTarget == restHandPoleAnchor && activeCoordinator == null) {
                        restLimb.BendGoalTarget = null;
                        restLimb.Broadcast();
                    }
                    DestroyRestHandPoleAnchor();
                });
            }
        }

        void OnCreateRestHand() {
            if (RestHand == null) {
                RestHand = StructuredData.Create<PointerHandRestSetup>();
            }
            currentRestHandParentType = RestHand?.ParentType ?? AnchorParentType.World;
            currentRestHandPoleParentType = RestHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
            if (RestHand?.ElbowPole != null) {
                RestHand.ElbowPole.OnPoleSettingChange = () => OnRestHandSettingChange();
                RestHand.ElbowPole.OnParentTypeChange = () => OnRestHandPoleParentTypeChange();
                RestHand.ElbowPole.OnPoleTransformChange = () => SetupRestHandPoleTransform();
            }
            Watch(nameof(IsEnabled), delegate { OnRestHandSettingChange(); });
            Watch(nameof(IsHandEnabled), delegate { OnRestHandSettingChange(); });
            Watch(nameof(RestHand), delegate {
                if (RestHand?.ElbowPole != null) {
                    RestHand.ElbowPole.OnPoleSettingChange = () => OnRestHandSettingChange();
                    RestHand.ElbowPole.OnParentTypeChange = () => OnRestHandPoleParentTypeChange();
                    RestHand.ElbowPole.OnPoleTransformChange = () => SetupRestHandPoleTransform();
                }
                OnRestHandSettingChange();
                SetupRestHandTransform();
                SetupRestHandPoleTransform();
            });
            Watch(nameof(Character), delegate {
                if (RestHand?.ParentType == AnchorParentType.Character || RestHand?.ParentType == AnchorParentType.CharacterShoulder) {
                    var targetAnchor = GetRestHandAnchor(true);
                    if (targetAnchor != null) AttachAnchorToParent(targetAnchor, RestHand.ParentType, !IsRightHanded);
                }
                if (RestHand?.ElbowPole?.ParentType == AnchorParentType.Character || RestHand?.ElbowPole?.ParentType == AnchorParentType.CharacterShoulder) {
                    var poleAnchor = GetRestHandPoleAnchor(true);
                    if (poleAnchor != null) AttachAnchorToParent(poleAnchor, RestHand.ElbowPole.ParentType, !IsRightHanded);
                }
            });
        }

        void OnReadyRestHand() {
            currentRestHandParentType = RestHand?.ParentType ?? AnchorParentType.World;
            currentRestHandPoleParentType = RestHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
            if (RestHand?.ElbowPole != null) {
                RestHand.ElbowPole.OnPoleSettingChange = () => OnRestHandSettingChange();
                RestHand.ElbowPole.OnParentTypeChange = () => OnRestHandPoleParentTypeChange();
                RestHand.ElbowPole.OnPoleTransformChange = () => SetupRestHandPoleTransform();
            }
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
            target.LocalizationKey = "INITIAL_REST_POSITION";
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
                var parentType = RestHand?.ParentType ?? AnchorParentType.World;
                SetAnchorParentInitial(setupAnchor, parentType, !IsRightHanded);

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
                var parentType = RestHand?.ParentType ?? AnchorParentType.World;
                Vector3 defaultCharPos = IsRightHanded ? new Vector3(-0.25f, 0.65f, 0.25f) : new Vector3(0.25f, 0.65f, 0.25f);
                TransformRestHandLocalToWorld(AnchorParentType.Character, defaultCharPos, Vector3.zero, out Vector3 worldPos, out Quaternion worldRot);
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
            var pole = RestHand?.ElbowPole?.Pole ?? RestHand?.Pole;
            if (pole == null) return;
            pole.LocalizationKey = "INITIAL_POLE_TRANSFORM";
            pole.HideScale();
            pole.CollapsedSelf = true;
            pole.ShowOriginIndicator = false;

            pole.OnEnterVisualSetup = (tr) => {
                EnsureRestHandAnchors();
                NeutralHandPosition?.EnterVisualSetup(false);
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
                var parentType = RestHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
                SetAnchorParentInitial(setupAnchor, parentType, !IsRightHanded);

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
                var parentType = RestHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
                Vector3 defaultWorldPos = GetDefaultElbowPoleWorldPosition(!IsRightHanded);
                TransformAnchorWorldToLocal(parentType, defaultWorldPos, Quaternion.identity, out Vector3 localPos, out Vector3 localRot, !IsRightHanded);

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
                    var currentPole = RestHand?.ElbowPole?.Pole ?? RestHand?.Pole;
                    if (livePole != null && currentPole != null) {
                        livePole.Transform.Position = currentPole.Position;
                        livePole.Transform.Rotation = currentPole.Rotation;
                        livePole.Transform.Broadcast();
                    }
                }
                OnRestHandSettingChange();

                if (!isExitingVisualSetup) {
                    isExitingVisualSetup = true;
                    try {
                        TriggerVisualSetupAnchor(NeutralHandPosition, isApplied);
                    } finally {
                        isExitingVisualSetup = false;
                    }
                }

                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
                RequestUiRefresh();
            };
        }

        void EnsureRestHandAnchors() {
            var targetParentType = RestHand?.ParentType ?? AnchorParentType.World;
            if (RestHand != null && (RestHand.Enabled || (RestHand.Target != null && RestHand.Target.InVisualSetupMode))) {
                var targetAnchor = GetRestHandAnchor();
                if (targetAnchor != null) AttachAnchorToParent(targetAnchor, targetParentType, !IsRightHanded);
            }
            var poleParentType = RestHand?.ElbowPole?.ParentType ?? AnchorParentType.World;
            if (RestHand != null && (RestHand.UsePole || (RestHand.Pole != null && RestHand.Pole.InVisualSetupMode) || (RestHand.ElbowPole != null && (RestHand.ElbowPole.UsePole || (RestHand.ElbowPole.Pole != null && RestHand.ElbowPole.Pole.InVisualSetupMode))))) {
                var poleAnchor = GetRestHandPoleAnchor();
                if (poleAnchor != null) AttachAnchorToParent(poleAnchor, poleParentType, !IsRightHanded);
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

            var currentPoleSetup = RestHand?.ElbowPole?.Pole ?? RestHand?.Pole;
            if (pole != null && currentPoleSetup != null && currentPoleSetup.InVisualSetupMode && currentPoleSetup.setupAnchor != null) {
                Vector3 polePos = currentPoleSetup.setupAnchor.Transform.Position;
                Vector3 poleRot = currentPoleSetup.setupAnchor.Transform.Rotation;
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

        public void MirrorRestHandTransforms(bool wasRightHanded) {
            if (RestHand == null) return;

            bool wasRestRight = !wasRightHanded;
            bool newRestRight = !IsRightHanded;

            // 1. Mirror Target Transform
            if (RestHand.Target != null) {
                MirrorTransformAcrossCharacter(
                    RestHand.Target.Position, 
                    RestHand.Target.Rotation, 
                    RestHand.ParentType, 
                    wasRestRight, 
                    newRestRight, 
                    out Vector3 newPos, 
                    out Vector3 newRot
                );

                RestHand.Target.Position = newPos;
                RestHand.Target.Rotation = newRot;
                RestHand.Target.SetDataInput(nameof(RestHand.Target.Position), newPos, true);
                RestHand.Target.SetDataInput(nameof(RestHand.Target.Rotation), newRot, true);
                RestHand.Target.Broadcast();

                if (RestHand.Target.setupAnchor != null) {
                    SetAnchorParentInitial(RestHand.Target.setupAnchor, RestHand.ParentType, newRestRight);
                    RestHand.Target.setupAnchor.Transform.Position = newPos;
                    RestHand.Target.setupAnchor.Transform.Rotation = newRot;
                    var handSide = IsRightHanded ? "Left" : "Right";
                    RestHand.Target.setupAnchor.Name = $"⌛⚓-{handSide} Hand Rest Target";
                    RestHand.Target.setupAnchor.Transform.Broadcast();
                    Scene?.UpdateNewAssetName(RestHand.Target.setupAnchor);
                }
            }

            // 2. Mirror Elbow Pole Transform
            var poleSetup = RestHand.ElbowPole;
            var pole = poleSetup?.Pole ?? RestHand.Pole;
            if (pole != null) {
                var poleParentType = poleSetup?.ParentType ?? AnchorParentType.World;
                MirrorTransformAcrossCharacter(
                    pole.Position, 
                    pole.Rotation, 
                    poleParentType, 
                    wasRestRight, 
                    newRestRight, 
                    out Vector3 newPolePos, 
                    out Vector3 newPoleRot
                );

                pole.Position = newPolePos;
                pole.Rotation = newPoleRot;
                pole.SetDataInput(nameof(pole.Position), newPolePos, true);
                pole.SetDataInput(nameof(pole.Rotation), newPoleRot, true);
                pole.Broadcast();

                if (pole.setupAnchor != null) {
                    SetAnchorParentInitial(pole.setupAnchor, poleParentType, newRestRight);
                    pole.setupAnchor.Transform.Position = newPolePos;
                    pole.setupAnchor.Transform.Rotation = newPoleRot;
                    var handSide = IsRightHanded ? "Left" : "Right";
                    pole.setupAnchor.Name = $"⌛⚓-{handSide} Hand Rest Pole";
                    pole.setupAnchor.Transform.Broadcast();
                    Scene?.UpdateNewAssetName(pole.setupAnchor);
                }
            }

            // 3. Update live anchors
            var targetAnchor = GetRestHandAnchor(true);
            if (targetAnchor != null) {
                AttachAnchorToParent(targetAnchor, RestHand.ParentType, newRestRight);
                targetAnchor.Transform.Position = RestHand.Target != null ? RestHand.Target.Position : Vector3.zero;
                targetAnchor.Transform.Rotation = RestHand.Target != null 
                    ? KeyboardReceiverAsset.ApplyHandIKIntermediateRotation(IsRightHanded, RestHand.Target.Rotation)
                    : Vector3.zero;
                targetAnchor.Transform.Broadcast();
                if (targetAnchor.GameObject != null) {
                    targetAnchor.GameObject.transform.localPosition = targetAnchor.Transform.Position;
                    targetAnchor.GameObject.transform.localRotation = Quaternion.Euler(targetAnchor.Transform.Rotation);
                }
            }

            var poleAnchor = GetRestHandPoleAnchor(true);
            if (poleAnchor != null && RestHand.ElbowPole != null) {
                AttachAnchorToParent(poleAnchor, RestHand.ElbowPole.ParentType, newRestRight);
                poleAnchor.Transform.Position = pole != null ? pole.Position : Vector3.zero;
                poleAnchor.Transform.Rotation = pole != null ? pole.Rotation : Vector3.zero;
                poleAnchor.Transform.Broadcast();
                if (poleAnchor.GameObject != null) {
                    poleAnchor.GameObject.transform.localPosition = poleAnchor.Transform.Position;
                    poleAnchor.GameObject.transform.localRotation = Quaternion.Euler(poleAnchor.Transform.Rotation);
                }
            }
        }

        public void MirrorActiveHandPoleTransform(bool wasRightHanded) {
            var pole = ActiveHand?.ElbowPole?.Pole;
            if (pole == null) return;
            var parentType = ActiveHand?.ElbowPole?.ParentType ?? AnchorParentType.World;

            MirrorTransformAcrossCharacter(
                pole.Position,
                pole.Rotation,
                parentType,
                wasRightHanded,
                IsRightHanded,
                out Vector3 newPos,
                out Vector3 newRot
            );

            pole.Position = newPos;
            pole.Rotation = newRot;
            pole.SetDataInput(nameof(pole.Position), newPos, true);
            pole.SetDataInput(nameof(pole.Rotation), newRot, true);
            pole.Broadcast();

            if (pole.setupAnchor != null) {
                SetAnchorParentInitial(pole.setupAnchor, parentType, IsRightHanded);
                pole.setupAnchor.Transform.Position = newPos;
                pole.setupAnchor.Transform.Rotation = newRot;
                var handSide = IsRightHanded ? "Right" : "Left";
                pole.setupAnchor.Name = $"⌛⚓-{handSide} Hand Active Pole";
                pole.setupAnchor.Transform.Broadcast();
                Scene?.UpdateNewAssetName(pole.setupAnchor);
            }

            var livePole = GetActiveHandPoleAnchor(true);
            if (livePole != null) {
                AttachAnchorToParent(livePole, parentType, IsRightHanded);
                livePole.Transform.Position = newPos;
                livePole.Transform.Rotation = newRot;
                livePole.Transform.Broadcast();
                if (livePole.GameObject != null) {
                    livePole.GameObject.transform.localPosition = newPos;
                    livePole.GameObject.transform.localRotation = Quaternion.Euler(newRot);
                }
            }
        }

        void MirrorTransformAcrossCharacter(Vector3 localPos, Vector3 localRot, AnchorParentType parentType, bool wasRight, bool newRight, out Vector3 newLocalPos, out Vector3 newLocalRot) {
            if (Character != null) {
                TransformAnchorLocalToWorld(parentType, localPos, localRot, out Vector3 worldPos, out Quaternion worldRot, wasRight);

                var charTr = Character.MainTransform ?? (Character.GameObject != null ? Character.GameObject.transform : null);
                if (charTr != null) {
                    Vector3 charLocalPos = Quaternion.Inverse(charTr.rotation) * (worldPos - charTr.position);
                    Quaternion charLocalRot = Quaternion.Inverse(charTr.rotation) * worldRot;

                    Vector3 mirroredCharPos = new Vector3(-charLocalPos.x, charLocalPos.y, charLocalPos.z);
                    Quaternion mirroredCharRot = new Quaternion(charLocalRot.x, -charLocalRot.y, -charLocalRot.z, charLocalRot.w);

                    Vector3 mirroredWorldPos = charTr.position + charTr.rotation * mirroredCharPos;
                    Quaternion mirroredWorldRot = charTr.rotation * mirroredCharRot;

                    TransformAnchorWorldToLocal(parentType, mirroredWorldPos, mirroredWorldRot, out newLocalPos, out newLocalRot, newRight);
                    return;
                }
            }

            // Fallback if no character
            newLocalPos = new Vector3(-localPos.x, localPos.y, localPos.z);
            newLocalRot = Helper.NormalizeRotationAngles(new Vector3(localRot.x, -localRot.y, -localRot.z));
        }
    }
}
