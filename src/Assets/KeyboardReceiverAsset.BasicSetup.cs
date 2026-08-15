using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Utility;
using static Warudo.Plugins.Core.Assets.Character.CharacterAsset;

namespace FlameStream
{
    public partial class KeyboardReceiverAsset : ReceiverAsset {

        [Section("BASIC_SETUP")]

        [DataInput(10)]
        [Label("ENABLE")]
        public bool IsHandEnabled;

        [DataInput(20)]
        [Label("CHARACTER")]
        [Description("CHARACTER_DESC")]
        public CharacterAsset Character;

        [DataInput(30)]
        [Label("ROOT_INITIAL_TRANSFORM")]
        public VisualSetupTransform RootTransform;

        [DataInput(40)]
        [Label("MOVEMENT_FACTOR")]
        public Vector3 MovementFactor = Vector3.one;

        [DataInput(45)]
        [Label("PLAY_ANIM_ON_DISABLED_KEYS")]
        public bool PlayAnimationOnDisabledKey = true;

        [DataInput(46)]
        [Label("RETURN_TO_REST")]
        public bool ReturnToRest = true;

        [DataInput(48)]
        [Label("REST_TIMEOUT")]
        [FloatSlider(0.1f, 30f)]
        public float RestTimeout = 3.0f;

        [DataInput(50)]
        [Label("LEFT_HAND_REST_TRANSFORM")]
        public VisualSetupTransform LeftHandRestTransform;

        [DataInput(60)]
        [Label("RIGHT_HAND_REST_TRANSFORM")]
        public VisualSetupTransform RightHandRestTransform;

        public Vector3 KeyboardRootPosition {
            get => RootTransform?.Position ?? new Vector3(0f, 0.6218f, 0.1484f);
            set {
                if (RootTransform != null) {
                    RootTransform.Position = value;
                    RootTransform.BroadcastDataInput(nameof(RootTransform.Position));
                }
            }
        }

        public Vector3 KeyboardRootRotation {
            get => RootTransform?.Rotation ?? Vector3.zero;
            set {
                if (RootTransform != null) {
                    RootTransform.Rotation = value;
                    RootTransform.BroadcastDataInput(nameof(RootTransform.Rotation));
                }
            }
        }

        public Vector3 LeftHandRestPosition {
            get => LeftHandRestTransform?.Position ?? new Vector3(-0.07f, 0.02f, 0f);
            set {
                if (LeftHandRestTransform != null) {
                    LeftHandRestTransform.Position = value;
                    LeftHandRestTransform.BroadcastDataInput(nameof(LeftHandRestTransform.Position));
                }
            }
        }

        public Vector3 LeftHandRestRotation {
            get => LeftHandRestTransform?.Rotation ?? new Vector3(0f, 90f, 0f);
            set {
                if (LeftHandRestTransform != null) {
                    LeftHandRestTransform.Rotation = value;
                    LeftHandRestTransform.BroadcastDataInput(nameof(LeftHandRestTransform.Rotation));
                }
            }
        }

        public Vector3 RightHandRestPosition {
            get => RightHandRestTransform?.Position ?? new Vector3(0.07f, 0.02f, 0f);
            set {
                if (RightHandRestTransform != null) {
                    RightHandRestTransform.Position = value;
                    RightHandRestTransform.BroadcastDataInput(nameof(RightHandRestTransform.Position));
                }
            }
        }

        public Vector3 RightHandRestRotation {
            get => RightHandRestTransform?.Rotation ?? new Vector3(0f, -90f, 0f);
            set {
                if (RightHandRestTransform != null) {
                    RightHandRestTransform.Rotation = value;
                    RightHandRestTransform.BroadcastDataInput(nameof(RightHandRestTransform.Rotation));
                }
            }
        }

        [Trigger(70)]
        [Label("APPLY_BASIC_SETUP")]
        public void TriggerApplyBasicSetup() {
            ApplyBasicSetup();
        }

        [Trigger(75)]
        [Label("CLEAR_ANCHORS")]
        public void TriggerClearAnchors() {
            ClearAnchors();
        }

        [DataInput]
        [Hidden]
        public Guid RootAnchorAssetId;

        [DataInput]
        [Hidden]
        public Guid LeftHandAnchorAssetId;

        [DataInput]
        [Hidden]
        public Guid RightHandAnchorAssetId;

        public AnchorAsset GetRootAnchor(bool skipAutoCreate = false) {
            if (RootAnchorAssetId != Guid.Empty && Scene != null) {
                var found = Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == RootAnchorAssetId);
                if (found != null) return found;
            }
            if (skipAutoCreate || Scene == null) return null;

            var rootAnchor = Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
            rootAnchor.Name = "⚓-🔥⌨️ Root";
            Scene.UpdateNewAssetName(rootAnchor);
            RootAnchorAssetId = rootAnchor.Id;
            BroadcastDataInput(nameof(RootAnchorAssetId));

            if (Character != null) {
                rootAnchor.Attachable.Parent = Character;
                rootAnchor.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.TransformPath;
                rootAnchor.Attachable.AttachToTransform = null;
                if (Character.GameObject != null) {
                    rootAnchor.GameObject.transform.SetParent(Character.GameObject.transform, false);
                }
            }
            rootAnchor.Transform.Position = KeyboardRootPosition;
            rootAnchor.Transform.Rotation = KeyboardRootRotation;
            rootAnchor.Transform.Broadcast();
            return rootAnchor;
        }

        public AnchorAsset RootAnchor => GetRootAnchor(true);

        public AnchorAsset LeftHandAnchor {
            get {
                if (LeftHandAnchorAssetId == Guid.Empty || Scene == null) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == LeftHandAnchorAssetId);
            }
        }

        public AnchorAsset RightHandAnchor {
            get {
                if (RightHandAnchorAssetId == Guid.Empty || Scene == null) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == RightHandAnchorAssetId);
            }
        }

        /// <summary>
        /// 10 FINGER ADDITIVE OVERLAY ANIMATIONS
        /// </summary>
        [Section("FINGER_OVERLAY_ANIMATIONS")]

        [DataInput(100)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("LEFT_PINKY_ANIMATION")]
        public string LeftPinkyAnimation;

        [DataInput(110)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("LEFT_RING_ANIMATION")]
        public string LeftRingAnimation;

        [DataInput(120)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("LEFT_MIDDLE_ANIMATION")]
        public string LeftMiddleAnimation;

        [DataInput(130)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("LEFT_INDEX_ANIMATION")]
        public string LeftIndexAnimation;

        [DataInput(140)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("LEFT_THUMB_ANIMATION")]
        public string LeftThumbAnimation;

        [DataInput(150)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("RIGHT_THUMB_ANIMATION")]
        public string RightThumbAnimation;

        [DataInput(160)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("RIGHT_INDEX_ANIMATION")]
        public string RightIndexAnimation;

        [DataInput(170)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("RIGHT_MIDDLE_ANIMATION")]
        public string RightMiddleAnimation;

        [DataInput(180)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("RIGHT_RING_ANIMATION")]
        public string RightRingAnimation;

        [DataInput(190)]
        [AutoCompleteResource("CharacterAnimation", null)]
        [PreviewGallery]
        [Label("RIGHT_PINKY_ANIMATION")]
        public string RightPinkyAnimation;

        public string GetFingerLayerSuffix(KeyboardFinger finger) {
            switch (finger) {
                case KeyboardFinger.LeftPinky: return "Left Pinky";
                case KeyboardFinger.LeftRing: return "Left Ring";
                case KeyboardFinger.LeftMiddle: return "Left Middle";
                case KeyboardFinger.LeftIndex: return "Left Index";
                case KeyboardFinger.LeftThumb: return "Left Thumb";
                case KeyboardFinger.RightThumb: return "Right Thumb";
                case KeyboardFinger.RightIndex: return "Right Index";
                case KeyboardFinger.RightMiddle: return "Right Middle";
                case KeyboardFinger.RightRing: return "Right Ring";
                case KeyboardFinger.RightPinky: return "Right Pinky";
                default: return null;
            }
        }

        public string GetFingerAnimation(KeyboardFinger finger) {
            switch (finger) {
                case KeyboardFinger.LeftPinky: return LeftPinkyAnimation;
                case KeyboardFinger.LeftRing: return LeftRingAnimation;
                case KeyboardFinger.LeftMiddle: return LeftMiddleAnimation;
                case KeyboardFinger.LeftIndex: return LeftIndexAnimation;
                case KeyboardFinger.LeftThumb: return LeftThumbAnimation;
                case KeyboardFinger.RightThumb: return RightThumbAnimation;
                case KeyboardFinger.RightIndex: return RightIndexAnimation;
                case KeyboardFinger.RightMiddle: return RightMiddleAnimation;
                case KeyboardFinger.RightRing: return RightRingAnimation;
                case KeyboardFinger.RightPinky: return RightPinkyAnimation;
                default: return null;
            }
        }

        public string GetFingerLayerId(KeyboardFinger finger) {
            return $"{CHARACTER_ANIM_LAYER_ID_PREFIX} {finger.GetLayerSuffix()}";
        }

        void SetupVisualTransforms() {
            SetupRootTransform();
            SetupLeftHandRestTransform();
            SetupRightHandRestTransform();
        }

        void SetupRootTransform() {
            if (RootTransform == null) {
                RootTransform = StructuredData.Create<VisualSetupTransform>();
                RootTransform.Position = new Vector3(0f, 0.6218f, 0.1484f);
                RootTransform.Rotation = Vector3.zero;
            }
            RootTransform.LocalizationKey = "KEYBOARD_ROOT";
            RootTransform.HideScale();
            RootTransform.OnEnterVisualSetup = (tr) => {
                EnsureAnchors();
            };
            RootTransform.OnCreateAnchor = (tr, setupAnchor) => {
                if (Character != null && Character.GameObject != null) {
                    setupAnchor.Attachable.Parent = Character;
                    setupAnchor.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.TransformPath;
                    setupAnchor.Attachable.AttachToTransform = null;
                    setupAnchor.GameObject.transform.SetParent(Character.GameObject.transform, false);
                }
                setupAnchor.Transform.Position = tr.Position;
                setupAnchor.Transform.Rotation = tr.Rotation;
                setupAnchor.Transform.Broadcast();
            };
            RootTransform.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                UpdateRootTransform();
            };
            RootTransform.OnExitVisualSetup = (isApplied) => {
                UpdateRootTransform();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };
        }

        void SetupLeftHandRestTransform() {
            if (LeftHandRestTransform == null) {
                LeftHandRestTransform = StructuredData.Create<VisualSetupTransform>();
                LeftHandRestTransform.Position = new Vector3(-0.07f, 0.02f, 0f);
                LeftHandRestTransform.Rotation = new Vector3(0f, 90f, 0f);
            }
            LeftHandRestTransform.LocalizationKey = "KEYBOARD_LEFT_HAND_REST";
            LeftHandRestTransform.HideScale();
            LeftHandRestTransform.OnEnterVisualSetup = (tr) => {
                EnsureAnchors();
            };
            LeftHandRestTransform.OnCreateAnchor = (tr, setupAnchor) => {
                var root = GetRootAnchor(false);
                if (root != null) {
                    setupAnchor.Attachable.Parent = root;
                    if (root.GameObject != null) {
                        setupAnchor.GameObject.transform.SetParent(root.GameObject.transform, false);
                    }
                }
                setupAnchor.Transform.Position = tr.Position;
                setupAnchor.Transform.Rotation = tr.Rotation;
                setupAnchor.Transform.Broadcast();
            };
            LeftHandRestTransform.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                UpdateHandRestTransforms();
            };
            LeftHandRestTransform.OnExitVisualSetup = (isApplied) => {
                UpdateHandRestTransforms();
                ResetActiveKeyValues();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };
        }

        void SetupRightHandRestTransform() {
            if (RightHandRestTransform == null) {
                RightHandRestTransform = StructuredData.Create<VisualSetupTransform>();
                RightHandRestTransform.Position = new Vector3(0.07f, 0.02f, 0f);
                RightHandRestTransform.Rotation = new Vector3(0f, -90f, 0f);
            }
            RightHandRestTransform.LocalizationKey = "KEYBOARD_RIGHT_HAND_REST";
            RightHandRestTransform.HideScale();
            RightHandRestTransform.OnEnterVisualSetup = (tr) => {
                EnsureAnchors();
            };
            RightHandRestTransform.OnCreateAnchor = (tr, setupAnchor) => {
                var root = GetRootAnchor(false);
                if (root != null) {
                    setupAnchor.Attachable.Parent = root;
                    if (root.GameObject != null) {
                        setupAnchor.GameObject.transform.SetParent(root.GameObject.transform, false);
                    }
                }
                setupAnchor.Transform.Position = tr.Position;
                setupAnchor.Transform.Rotation = tr.Rotation;
                setupAnchor.Transform.Broadcast();
            };
            RightHandRestTransform.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                UpdateHandRestTransforms();
            };
            RightHandRestTransform.OnExitVisualSetup = (isApplied) => {
                UpdateHandRestTransforms();
                ResetActiveKeyValues();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };
        }

        void OnCreateBasicSetup() {
            SetupVisualTransforms();

            Watch(nameof(RootTransform), delegate { SetupRootTransform(); });
            Watch(nameof(LeftHandRestTransform), delegate { SetupLeftHandRestTransform(); });
            Watch(nameof(RightHandRestTransform), delegate { SetupRightHandRestTransform(); });

            Watch(nameof(IsEnabled), delegate { UpdateHandTrackingState(); });
            Watch(nameof(IsHandEnabled), delegate { UpdateHandTrackingState(); });
            Watch(nameof(Character), delegate { UpdateHandTrackingState(); });

            Watch(nameof(LeftPinkyAnimation), delegate { SynchronizeFingerAnimation(KeyboardFinger.LeftPinky); });
            Watch(nameof(LeftRingAnimation), delegate { SynchronizeFingerAnimation(KeyboardFinger.LeftRing); });
            Watch(nameof(LeftMiddleAnimation), delegate { SynchronizeFingerAnimation(KeyboardFinger.LeftMiddle); });
            Watch(nameof(LeftIndexAnimation), delegate { SynchronizeFingerAnimation(KeyboardFinger.LeftIndex); });
            Watch(nameof(LeftThumbAnimation), delegate { SynchronizeFingerAnimation(KeyboardFinger.LeftThumb); });
            Watch(nameof(RightThumbAnimation), delegate { SynchronizeFingerAnimation(KeyboardFinger.RightThumb); });
            Watch(nameof(RightIndexAnimation), delegate { SynchronizeFingerAnimation(KeyboardFinger.RightIndex); });
            Watch(nameof(RightMiddleAnimation), delegate { SynchronizeFingerAnimation(KeyboardFinger.RightMiddle); });
            Watch(nameof(RightRingAnimation), delegate { SynchronizeFingerAnimation(KeyboardFinger.RightRing); });
            Watch(nameof(RightPinkyAnimation), delegate { SynchronizeFingerAnimation(KeyboardFinger.RightPinky); });
        }

        void OnDestroyBasicSetup() {
            ClearAnchors();
            RootTransform?.DestroyAnchor();
            LeftHandRestTransform?.DestroyAnchor();
            RightHandRestTransform?.DestroyAnchor();
        }

        void OnUpdateBasicSetup() {
            if (RootTransform != null && string.IsNullOrEmpty(RootTransform.LocalizationKey)) {
                SetupVisualTransforms();
            }

            var root = RootAnchor;
            if (root != null) {
                // If in visual setup mode for RootTransform, move RootAnchor to follow RootTransform.setupAnchor in real time!
                if (RootTransform != null && RootTransform.InVisualSetupMode && RootTransform.setupAnchor != null) {
                    var st = RootTransform.setupAnchor.Transform;
                    if (root.Transform.Position != st.Position || root.Transform.Rotation != st.Rotation) {
                        root.Transform.Position = st.Position;
                        root.Transform.Rotation = st.Rotation;
                        root.Transform.Broadcast();
                    }
                } else {
                    // If user moved root anchor in viewport via gizmo, reflect it in RootTransform
                    if (root.Transform.Position != KeyboardRootPosition) {
                        KeyboardRootPosition = root.Transform.Position;
                    }
                    if (root.Transform.Rotation != KeyboardRootRotation) {
                        KeyboardRootRotation = root.Transform.Rotation;
                    }
                }

                // If in visual setup mode for LeftHandRestTransform, drive LeftHandAnchor in real time!
                if (LeftHandRestTransform != null && LeftHandRestTransform.InVisualSetupMode && LeftHandRestTransform.setupAnchor != null && LeftHandAnchor != null) {
                    var st = LeftHandRestTransform.setupAnchor.Transform;
                    if (LeftHandAnchor.Transform.Position != st.Position || LeftHandAnchor.Transform.Rotation != st.Rotation) {
                        LeftHandAnchor.Transform.Position = st.Position;
                        LeftHandAnchor.Transform.Rotation = st.Rotation;
                        LeftHandAnchor.Transform.Broadcast();
                    }
                }

                // If in visual setup mode for RightHandRestTransform, drive RightHandAnchor in real time!
                if (RightHandRestTransform != null && RightHandRestTransform.InVisualSetupMode && RightHandRestTransform.setupAnchor != null && RightHandAnchor != null) {
                    var st = RightHandRestTransform.setupAnchor.Transform;
                    if (RightHandAnchor.Transform.Position != st.Position || RightHandAnchor.Transform.Rotation != st.Rotation) {
                        RightHandAnchor.Transform.Position = st.Position;
                        RightHandAnchor.Transform.Rotation = st.Rotation;
                        RightHandAnchor.Transform.Broadcast();
                    }
                }
            }
        }

        void UpdateHandTrackingState() {
            if (Character != null && IsEnabled && IsHandEnabled) {
                EnsureAnchors();
                SynchronizeAllFingerAnimations();
                var leftAnchor = LeftHandAnchor;
                var rightAnchor = RightHandAnchor;
                if (leftAnchor != null) {
                    EnableLimb(true, Character.LeftHandIK, leftAnchor);
                }
                if (rightAnchor != null) {
                    EnableLimb(true, Character.RightHandIK, rightAnchor);
                }
            } else {
                ClearAnchors();
                ResetFingerWeights();
            }
        }

        void UpdateRootTransform() {
            var root = RootAnchor;
            if (root == null) return;
            if (root.Transform.Position != KeyboardRootPosition || root.Transform.Rotation != KeyboardRootRotation) {
                root.Transform.Position = KeyboardRootPosition;
                root.Transform.Rotation = KeyboardRootRotation;
                root.Transform.Broadcast();
                if (root.GameObject != null) {
                    root.GameObject.transform.localPosition = KeyboardRootPosition;
                    root.GameObject.transform.localRotation = Quaternion.Euler(KeyboardRootRotation);
                }
            }
        }

        void UpdateHandRestTransforms() {
            var left = LeftHandAnchor;
            if (left != null) {
                left.Transform.Position = LeftHandRestPosition;
                left.Transform.Rotation = LeftHandRestRotation;
                left.Transform.Broadcast();
                if (left.GameObject != null) {
                    left.GameObject.transform.localPosition = LeftHandRestPosition;
                    left.GameObject.transform.localRotation = Quaternion.Euler(LeftHandRestRotation);
                }
            }
            var right = RightHandAnchor;
            if (right != null) {
                right.Transform.Position = RightHandRestPosition;
                right.Transform.Rotation = RightHandRestRotation;
                right.Transform.Broadcast();
                if (right.GameObject != null) {
                    right.GameObject.transform.localPosition = RightHandRestPosition;
                    right.GameObject.transform.localRotation = Quaternion.Euler(RightHandRestRotation);
                }
            }
            ResetActiveKeyValues();
        }

        public void EnsureAnchors() {
            if (Scene == null) return;

            // 1. Root Anchor
            AnchorAsset rootAnchor = GetRootAnchor(false);
            if (rootAnchor == null) return;

            // 2. Left Hand IK Anchor
            AnchorAsset leftHandAnchor = LeftHandAnchor;
            if (leftHandAnchor == null) {
                leftHandAnchor = Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
                leftHandAnchor.Name = "⚓-🔥⌨️🫲 Left Hand";
                Scene.UpdateNewAssetName(leftHandAnchor);
                LeftHandAnchorAssetId = leftHandAnchor.Id;
                BroadcastDataInput(nameof(LeftHandAnchorAssetId));
            }
            leftHandAnchor.Attachable.Parent = rootAnchor;
            if (rootAnchor.GameObject != null) {
                leftHandAnchor.GameObject.transform.SetParent(rootAnchor.GameObject.transform, false);
            }
            leftHandAnchor.Transform.Position = LeftHandRestPosition;
            leftHandAnchor.Transform.Rotation = LeftHandRestRotation;
            leftHandAnchor.Transform.Broadcast();

            // 3. Right Hand IK Anchor
            AnchorAsset rightHandAnchor = RightHandAnchor;
            if (rightHandAnchor == null) {
                rightHandAnchor = Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INPUT".Localized());
                rightHandAnchor.Name = "⚓-🔥⌨️🫱 Right Hand";
                Scene.UpdateNewAssetName(rightHandAnchor);
                RightHandAnchorAssetId = rightHandAnchor.Id;
                BroadcastDataInput(nameof(RightHandAnchorAssetId));
            }
            rightHandAnchor.Attachable.Parent = rootAnchor;
            if (rootAnchor.GameObject != null) {
                rightHandAnchor.GameObject.transform.SetParent(rootAnchor.GameObject.transform, false);
            }
            rightHandAnchor.Transform.Position = RightHandRestPosition;
            rightHandAnchor.Transform.Rotation = RightHandRestRotation;
            rightHandAnchor.Transform.Broadcast();
        }

        public void ApplyBasicSetup() {
            if (Character == null) {
                Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Warning, Name, "Please select a Character before applying Basic Setup.");
                return;
            }

            IsEnabled = true;
            IsHandEnabled = true;
            BroadcastDataInput(nameof(IsEnabled));
            BroadcastDataInput(nameof(IsHandEnabled));

            UpdateHandTrackingState();

            Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Success, Name, "Basic setup applied successfully. Root and hand anchors configured.");
        }

        public void ClearAnchors() {
            var root = RootAnchor;
            var left = LeftHandAnchor;
            var right = RightHandAnchor;

            if (Character != null) {
                EnableLimb(false, Character.LeftHandIK, null);
                EnableLimb(false, Character.RightHandIK, null);
            }

            if (left != null) {
                try { Scene.RemoveAsset(left.Id); } catch {}
            }
            if (right != null) {
                try { Scene.RemoveAsset(right.Id); } catch {}
            }
            if (root != null) {
                try { Scene.RemoveAsset(root.Id); } catch {}
            }

            RootAnchorAssetId = Guid.Empty;
            LeftHandAnchorAssetId = Guid.Empty;
            RightHandAnchorAssetId = Guid.Empty;

            BroadcastDataInput(nameof(RootAnchorAssetId));
            BroadcastDataInput(nameof(LeftHandAnchorAssetId));
            BroadcastDataInput(nameof(RightHandAnchorAssetId));
        }

        void EnableLimb(bool isEnabled, LimbIKData limb, AnchorAsset anchor) {
            if (limb == null) return;
            if (isEnabled && anchor != null) {
                limb.Enabled = true;
                limb.IkTarget = anchor;
                limb.PositionWeight = 1.0f;
                limb.RotationWeight = 1.0f;
            } else {
                limb.Enabled = false;
                limb.IkTarget = null;
                limb.PositionWeight = 0f;
                limb.RotationWeight = 0f;
            }
            limb.Broadcast();
        }

        public void SynchronizeAllFingerAnimations() {
            if (Character == null) return;

            var userLayers = Character.OverlappingAnimations?.Where(d => !d.CustomLayerID.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX)).ToList() ?? new List<OverlappingAnimationData>();
            var pluginLayers = Character.OverlappingAnimations?.Where(d => d.CustomLayerID.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX)).ToList() ?? new List<OverlappingAnimationData>();

            bool changed = false;

            foreach (KeyboardFinger finger in Enum.GetValues(typeof(KeyboardFinger))) {
                var layerId = GetFingerLayerId(finger);
                var animClip = GetFingerAnimation(finger);
                var existingLayer = pluginLayers.FirstOrDefault(d => d.CustomLayerID == layerId);

                if (string.IsNullOrEmpty(animClip)) {
                    if (existingLayer != null) {
                        pluginLayers.Remove(existingLayer);
                        changed = true;
                    }
                } else {
                    if (existingLayer == null) {
                        existingLayer = StructuredData.Create<OverlappingAnimationData>();
                        existingLayer.CustomLayerID = layerId;
                        existingLayer.Animation = animClip;
                        existingLayer.Weight = 0f;
                        existingLayer.Additive = true;
                        existingLayer.Speed = 1f;
                        existingLayer.Looping = false;
                        pluginLayers.Add(existingLayer);
                        changed = true;
                    } else {
                        if (existingLayer.Animation != animClip || !existingLayer.Additive) {
                            existingLayer.Animation = animClip;
                            existingLayer.Additive = true;
                            changed = true;
                        }
                    }
                }
            }

            if (changed) {
                userLayers.AddRange(pluginLayers);
                Character.SetDataInput(nameof(Character.OverlappingAnimations), userLayers.ToArray(), true);
            }
        }

        public void SynchronizeFingerAnimation(KeyboardFinger finger) {
            if (Character == null) return;

            var layerId = GetFingerLayerId(finger);
            var animClip = GetFingerAnimation(finger);

            var userLayers = Character.OverlappingAnimations?.Where(d => !d.CustomLayerID.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX)).ToList() ?? new List<OverlappingAnimationData>();
            var pluginLayers = Character.OverlappingAnimations?.Where(d => d.CustomLayerID.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX)).ToList() ?? new List<OverlappingAnimationData>();

            var existingLayer = pluginLayers.FirstOrDefault(d => d.CustomLayerID == layerId);

            if (string.IsNullOrEmpty(animClip)) {
                if (existingLayer != null) {
                    pluginLayers.Remove(existingLayer);
                    userLayers.AddRange(pluginLayers);
                    Character.SetDataInput(nameof(Character.OverlappingAnimations), userLayers.ToArray(), true);
                }
                return;
            }

            if (existingLayer == null) {
                existingLayer = StructuredData.Create<OverlappingAnimationData>();
                existingLayer.CustomLayerID = layerId;
                existingLayer.Animation = animClip;
                existingLayer.Weight = 0f;
                existingLayer.Additive = true;
                existingLayer.Speed = 1f;
                existingLayer.Looping = false;
                pluginLayers.Add(existingLayer);
            } else {
                existingLayer.Animation = animClip;
                existingLayer.Additive = true;
            }

            userLayers.AddRange(pluginLayers);
            Character.SetDataInput(nameof(Character.OverlappingAnimations), userLayers.ToArray(), true);
        }
    }
}
