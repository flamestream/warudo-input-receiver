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

        [DataInput(25)]
        [Label("AUTO_SETUP")]
        public KeyboardAutoSetup AutoSetup;

        [DataInput(30)]
        [Label("KEYBOARD_RIG_ORIGIN")]
        [Description("KEYBOARD_RIG_ORIGIN_DESC")]
        public CollapsibleVisualSetupTransform RootTransform;

        public Vector3 GetLeftHandMovementScale() {
            return (LeftHandRestTransform != null && LeftHandRestTransform.InVisualSetupMode && LeftHandRestTransform.setupAnchor != null)
                ? LeftHandRestTransform.setupAnchor.Transform.Scale
                : (LeftHandRestTransform?.Scale ?? Vector3.one);
        }

        public Vector3 GetRightHandMovementScale() {
            return (RightHandRestTransform != null && RightHandRestTransform.InVisualSetupMode && RightHandRestTransform.setupAnchor != null)
                ? RightHandRestTransform.setupAnchor.Transform.Scale
                : (RightHandRestTransform?.Scale ?? Vector3.one);
        }

        [DataInput(40)]
        [Label("HAND_REST_POSES")]
        public KeyboardHandRestSetup HandRestSetup;

        public bool ReturnToRest {
            get => HandRestSetup?.ReturnToRest ?? true;
            set {
                if (HandRestSetup != null) HandRestSetup.ReturnToRest = value;
            }
        }

        public float RestTimeout {
            get => HandRestSetup?.RestTimeout ?? 3.0f;
            set {
                if (HandRestSetup != null) HandRestSetup.RestTimeout = value;
            }
        }

        public CollapsibleVisualSetupTransform LeftHandRestTransform => HandRestSetup?.Left;
        public CollapsibleVisualSetupTransform RightHandRestTransform => HandRestSetup?.Right;

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

        public static readonly Vector3 HAND_IK_INTERMEDIATE_ROTATION_LEFT = new Vector3(0f, 90f, 0f);
        public static readonly Vector3 HAND_IK_INTERMEDIATE_ROTATION_RIGHT = new Vector3(0f, -90f, 0f);

        public static Vector3 ApplyHandIKIntermediateRotation(bool isLeft, Vector3 userRotation) {
            var intermediateQ = isLeft 
                ? Quaternion.Euler(HAND_IK_INTERMEDIATE_ROTATION_LEFT) 
                : Quaternion.Euler(HAND_IK_INTERMEDIATE_ROTATION_RIGHT);
            var userQ = Quaternion.Euler(userRotation);
            return Helper.NormalizeRotationAngles((userQ * intermediateQ).eulerAngles);
        }

        public static Vector3 RemoveHandIKIntermediateRotation(bool isLeft, Vector3 ikRotation) {
            var intermediateQ = isLeft 
                ? Quaternion.Euler(HAND_IK_INTERMEDIATE_ROTATION_LEFT) 
                : Quaternion.Euler(HAND_IK_INTERMEDIATE_ROTATION_RIGHT);
            var ikQ = Quaternion.Euler(ikRotation);
            return Helper.NormalizeRotationAngles((ikQ * Quaternion.Inverse(intermediateQ)).eulerAngles);
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
            get => LeftHandRestTransform?.Rotation ?? Vector3.zero;
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
            get => RightHandRestTransform?.Rotation ?? Vector3.zero;
            set {
                if (RightHandRestTransform != null) {
                    RightHandRestTransform.Rotation = value;
                    RightHandRestTransform.BroadcastDataInput(nameof(RightHandRestTransform.Rotation));
                }
            }
        }

        [Section("ADVANCED")]
        [Trigger(1010)]
        [Label("RESET_ANCHORS")]
        [Description("RESET_ANCHORS_DESC")]
        public void TriggerResetAnchors() {
            ClearAnchors();
            Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Info, Name, "Anchors reset and character IK detached. Fresh anchors will regenerate when enabled.");
        }

        [Trigger(1015)]
        [Label("DEACTIVATE_ALL_DEBUG_KEYS")]
        [Hidden]
        public void TriggerDeactivateAllDebugKeys() {
            DeactivateAllDebugKeys();
        }

        private bool lastAdvancedDebugState = false;

        public void UpdateAdvancedSectionDebugState() {
            bool isDebug = IsDebugKeyModeActive;
            bool stateChanged = isDebug != lastAdvancedDebugState;
            lastAdvancedDebugState = isDebug;

            string sectionTitle = isDebug ? "ADVANCED_DEBUG_MODE".Localized() : "ADVANCED".Localized();

            var resetPort = GetTriggerPort(nameof(TriggerResetAnchors));
            if (resetPort != null && resetPort.Properties != null) {
                resetPort.Properties.sectionTitle = sectionTitle;
                BroadcastTriggerProperties(nameof(TriggerResetAnchors));
            }

            var deactPort = GetTriggerPort(nameof(TriggerDeactivateAllDebugKeys));
            if (deactPort != null && deactPort.Properties != null) {
                deactPort.Properties.hidden = !isDebug;
                deactPort.Properties.sectionTitle = "";
                if (isDebug) {
                    var keyList = string.Join(", ", DebugForcedKeys.Select(k => k.ToString()));
                    deactPort.Properties.description = string.Format("DEACTIVATE_ALL_DEBUG_KEYS_DESC".Localized(), keyList);
                } else {
                    deactPort.Properties.description = "";
                }
                BroadcastTriggerProperties(nameof(TriggerDeactivateAllDebugKeys));
            }

            if (stateChanged) {
                Broadcast();
            }
        }

        public void UpdateAllDebugButtonStates() {
            if (KeyBindingsCollection?.KeyBindings != null) {
                foreach (var b in KeyBindingsCollection.KeyBindings) {
                    b?.UpdateDebugButtonState();
                }
            }
            if (VirtualKeysCollection?.Keys != null) {
                foreach (var k in VirtualKeysCollection.Keys) {
                    k?.UpdateDebugButtonState();
                }
            }
            UpdateAdvancedSectionDebugState();
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

        public string LeftPinkyAnimation => FingerAnimations?.LeftPinky;
        public string LeftRingAnimation => FingerAnimations?.LeftRing;
        public string LeftMiddleAnimation => FingerAnimations?.LeftMiddle;
        public string LeftIndexAnimation => FingerAnimations?.LeftIndex;
        public string LeftThumbAnimation => FingerAnimations?.LeftThumb;
        public string RightThumbAnimation => FingerAnimations?.RightThumb;
        public string RightIndexAnimation => FingerAnimations?.RightIndex;
        public string RightMiddleAnimation => FingerAnimations?.RightMiddle;
        public string RightRingAnimation => FingerAnimations?.RightRing;
        public string RightPinkyAnimation => FingerAnimations?.RightPinky;

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
            return FingerAnimations?.GetAnimation(finger);
        }

        public string GetFingerLayerId(KeyboardFinger finger) {
            return $"{CHARACTER_ANIM_LAYER_ID_PREFIX} {finger.GetLayerSuffix()}";
        }

        void SetupVisualTransforms() {
            SetupAutoSetup();
            SetupRootTransform();
            SetupHandRestTransforms();
        }

        public void SetupAutoSetup() {
            if (AutoSetup == null) {
                AutoSetup = StructuredData.Create<KeyboardAutoSetup>();
            }
            AutoSetup.Parent = this;
            AutoSetup.CollapsedSelf = true;
            AutoSetup.UpdateButtonState();
        }

        public void SetupHandRestTransforms() {
            if (HandRestSetup == null) {
                HandRestSetup = StructuredData.Create<KeyboardHandRestSetup>();
                HandRestSetup.Parent = this;
            }
            HandRestSetup.CollapsedSelf = true;
            SetupLeftHandRestTransform();
            SetupRightHandRestTransform();
        }

        void SetupRootTransform() {
            if (RootTransform == null) {
                RootTransform = StructuredData.Create<CollapsibleVisualSetupTransform>();
                RootTransform.Position = new Vector3(0f, 0.6218f, 0.1484f);
                RootTransform.Rotation = Vector3.zero;
            }
            RootTransform.CollapsedSelf = true;
            RootTransform.LocalizationKey = "KEYBOARD_RIG_ORIGIN";
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
                setupAnchor.Name = "⌛⚓-Keyboard Rig Origin";
                setupAnchor.Instructions = "Position and rotate the **Keyboard Rig Origin** reference anchor, relative to the **Character** (Avatar Root).\nThe red sphere indicates the origin location (character root).\nClick **[Apply Transform]** when finished.";
                setupAnchor.Transform.Broadcast();
            };
            RootTransform.OnResetAnchor = (tr, setupAnchor) => {
                setupAnchor.Transform.Position = Vector3.zero;
                setupAnchor.Transform.Rotation = Vector3.zero;
                setupAnchor.Transform.Scale = Vector3.one;
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

        public void SetupLeftHandRestTransform() {
            var rest = HandRestSetup;
            if (rest == null) return;
            rest.CollapsedSelf = true;
            if (rest.Left == null) {
                rest.Left = StructuredData.Create<CollapsibleVisualSetupTransform>();
                rest.Left.Position = new Vector3(-0.07f, 0.02f, 0f);
                rest.Left.Rotation = Vector3.zero;
                rest.Left.Scale = Vector3.one;
                rest.Left.SetDataInput(nameof(rest.Left.Scale), Vector3.one, true);
            }
            rest.Left.CollapsedSelf = true;
            LeftHandRestTransform.LocalizationKey = "LEFT_HAND_REST";
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
                setupAnchor.Transform.Scale = tr.Scale;
                setupAnchor.Name = "⌛⚓-Left Hand Rest Pose";
                setupAnchor.Instructions = "Position, rotate, and scale the **Left Hand Rest Pose** (resting over ASDF), relative to the **Keyboard Rig Origin**.\nThe red sphere indicates the origin location (Keyboard Rig Origin).\nClick **[Apply Transform]** when finished.";
                setupAnchor.Transform.Broadcast();
            };
            LeftHandRestTransform.OnResetAnchor = (tr, setupAnchor) => {
                setupAnchor.Transform.Position = Vector3.zero;
                setupAnchor.Transform.Rotation = Vector3.zero;
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Transform.Broadcast();
            };
            LeftHandRestTransform.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                tr.Scale = setupAnchor.Transform.Scale;
                UpdateHandRestTransforms();
            };
            LeftHandRestTransform.OnExitVisualSetup = (isApplied) => {
                UpdateHandRestTransforms();
                ResetActiveKeyValues();
                Context.Service.NavigateToAsset(Id);
                Context.Service.BroadcastOpenedScene();
            };
        }

        public void SetupRightHandRestTransform() {
            var rest = HandRestSetup;
            if (rest == null) return;
            rest.CollapsedSelf = true;
            if (rest.Right == null) {
                rest.Right = StructuredData.Create<CollapsibleVisualSetupTransform>();
                rest.Right.Position = new Vector3(0.07f, 0.02f, 0f);
                rest.Right.Rotation = Vector3.zero;
                rest.Right.Scale = Vector3.one;
                rest.Right.SetDataInput(nameof(rest.Right.Scale), Vector3.one, true);
            }
            rest.Right.CollapsedSelf = true;
            RightHandRestTransform.LocalizationKey = "RIGHT_HAND_REST";
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
                setupAnchor.Transform.Scale = tr.Scale;
                setupAnchor.Name = "⌛⚓-Right Hand Rest Pose";
                setupAnchor.Instructions = "Position, rotate, and scale the **Right Hand Rest Pose** (resting over JKL;), relative to the **Keyboard Rig Origin**.\nThe red sphere indicates the origin location (Keyboard Rig Origin).\nClick **[Apply Transform]** when finished.";
                setupAnchor.Transform.Broadcast();
            };
            RightHandRestTransform.OnResetAnchor = (tr, setupAnchor) => {
                setupAnchor.Transform.Position = Vector3.zero;
                setupAnchor.Transform.Rotation = Vector3.zero;
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Transform.Broadcast();
            };
            RightHandRestTransform.OnApplyAnchor = (tr, setupAnchor) => {
                tr.Position = setupAnchor.Transform.Position;
                tr.Rotation = setupAnchor.Transform.Rotation;
                tr.Scale = setupAnchor.Transform.Scale;
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
            if (AutoSetup == null) {
                AutoSetup = StructuredData.Create<KeyboardAutoSetup>();
            }
            AutoSetup.Parent = this;
            AutoSetup.CollapsedSelf = true;

            if (HandRestSetup == null) {
                HandRestSetup = StructuredData.Create<KeyboardHandRestSetup>();
                HandRestSetup.CollapsedSelf = true;
            }
            HandRestSetup.Parent = this;

            SetupVisualTransforms();

            Watch(nameof(AutoSetup), delegate {
                if (AutoSetup != null) {
                    AutoSetup.Parent = this;
                }
                AutoSetup?.UpdateButtonState();
            });
            Watch(nameof(RootTransform), delegate { SetupRootTransform(); });
            Watch(nameof(HandRestSetup), delegate {
                if (HandRestSetup != null) {
                    HandRestSetup.Parent = this;
                }
                SetupHandRestTransforms();
            });

            Watch(nameof(IsEnabled), delegate { 
                UpdateHandTrackingState(); 
                AutoSetup?.UpdateButtonState();
            });
            Watch(nameof(IsHandEnabled), delegate { 
                UpdateHandTrackingState(); 
                AutoSetup?.UpdateButtonState();
            });
            Watch(nameof(Character), delegate { 
                UpdateHandTrackingState(); 
                AutoSetup?.UpdateButtonState();
            });

            UpdateAdvancedSectionDebugState();
        }

        public KeyboardKeyBinding ActiveKeyBindingInVisualSetup;

        void OnDestroyBasicSetup() {
            ActiveKeyBindingInVisualSetup = null;
            DeactivateAllDebugKeys();

            try {
                if (Character != null && Character.Active && Character.OverlappingAnimations != null) {
                    var userLayers = Character.OverlappingAnimations.Where(d => !(d.CustomLayerID?.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX) ?? false)).ToArray();
                    Helper.SetCharacterOverlappingAnimations(Character, userLayers);
                }
            } catch {}
        }

        void OnUpdateBasicSetup() {
            if (AutoSetup == null) {
                SetupAutoSetup();
            }

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
                    var ikRot = ApplyHandIKIntermediateRotation(true, st.Rotation);
                    if (LeftHandAnchor.Transform.Position != st.Position || LeftHandAnchor.Transform.Rotation != ikRot) {
                        LeftHandAnchor.Transform.Position = st.Position;
                        LeftHandAnchor.Transform.Rotation = ikRot;
                        LeftHandAnchor.Transform.Broadcast();
                    }
                }

                // If in visual setup mode for RightHandRestTransform, drive RightHandAnchor in real time!
                if (RightHandRestTransform != null && RightHandRestTransform.InVisualSetupMode && RightHandRestTransform.setupAnchor != null && RightHandAnchor != null) {
                    var st = RightHandRestTransform.setupAnchor.Transform;
                    var ikRot = ApplyHandIKIntermediateRotation(false, st.Rotation);
                    if (RightHandAnchor.Transform.Position != st.Position || RightHandAnchor.Transform.Rotation != ikRot) {
                        RightHandAnchor.Transform.Position = st.Position;
                        RightHandAnchor.Transform.Rotation = ikRot;
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
                ClearFingerAnimations();
            }
        }

        public void ClearFingerAnimations() {
            if (Character == null || Character.OverlappingAnimations == null) return;
            var userLayers = Character.OverlappingAnimations.Where(d => !(d.CustomLayerID?.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX) ?? false)).ToArray();
            Helper.SetCharacterOverlappingAnimations(Character, userLayers);
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
                var ikRot = ApplyHandIKIntermediateRotation(true, LeftHandRestRotation);
                left.Transform.Position = LeftHandRestPosition;
                left.Transform.Rotation = ikRot;
                left.Transform.Broadcast();
                if (left.GameObject != null) {
                    left.GameObject.transform.localPosition = LeftHandRestPosition;
                    left.GameObject.transform.localRotation = Quaternion.Euler(ikRot);
                }
            }
            var right = RightHandAnchor;
            if (right != null) {
                var ikRot = ApplyHandIKIntermediateRotation(false, RightHandRestRotation);
                right.Transform.Position = RightHandRestPosition;
                right.Transform.Rotation = ikRot;
                right.Transform.Broadcast();
                if (right.GameObject != null) {
                    right.GameObject.transform.localPosition = RightHandRestPosition;
                    right.GameObject.transform.localRotation = Quaternion.Euler(ikRot);
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
            var leftIkRot = ApplyHandIKIntermediateRotation(true, LeftHandRestRotation);
            leftHandAnchor.Transform.Position = LeftHandRestPosition;
            leftHandAnchor.Transform.Rotation = leftIkRot;
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
            var rightIkRot = ApplyHandIKIntermediateRotation(false, RightHandRestRotation);
            rightHandAnchor.Transform.Position = RightHandRestPosition;
            rightHandAnchor.Transform.Rotation = rightIkRot;
            rightHandAnchor.Transform.Broadcast();
        }

        Vector3 GetIndexTipWorldPosition(Animator animator, bool isLeft) {
            var distalBone = isLeft ? HumanBodyBones.LeftIndexDistal : HumanBodyBones.RightIndexDistal;
            var distalTransform = animator.GetBoneTransform(distalBone);
            if (distalTransform != null) {
                if (distalTransform.childCount > 0) {
                    return distalTransform.GetChild(0).position;
                }
                var interBone = isLeft ? HumanBodyBones.LeftIndexIntermediate : HumanBodyBones.RightIndexIntermediate;
                var interTransform = animator.GetBoneTransform(interBone);
                if (interTransform != null) {
                    var delta = distalTransform.position - interTransform.position;
                    if (delta.sqrMagnitude > 0.00001f) {
                        return distalTransform.position + delta.normalized * (delta.magnitude * 0.75f);
                    }
                }
                return distalTransform.position;
            }
            var handBone = isLeft ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
            var handTransform = animator.GetBoneTransform(handBone);
            return handTransform != null ? handTransform.position : Vector3.zero;
        }

        public void AutoSetupFromCharacterPose() {
            if (Character == null || !Character.Active || Character.GameObject == null) {
                Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Warning, "AUTO_SETUP".Localized(), "AUTO_SETUP_BLOCK_NO_CHARACTER".Localized());
                return;
            }

            if (IsHandEnabled) {
                Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Warning, "AUTO_SETUP".Localized(), "AUTO_SETUP_BLOCK_HANDS_ENABLED".Localized());
                return;
            }

            var animator = Character.Animator;
            if (animator == null) {
                Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Error, "AUTO_SETUP".Localized(), "AUTO_SETUP_NO_ANIMATOR".Localized());
                return;
            }

            var leftHandBone = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            var rightHandBone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (leftHandBone == null || rightHandBone == null) {
                Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Error, "AUTO_SETUP".Localized(), "AUTO_SETUP_NO_HAND_BONES".Localized());
                return;
            }

            // Cancel any active visual setup anchors to avoid dirty states
            if (RootTransform != null && RootTransform.InVisualSetupMode) {
                RootTransform.CancelVisualSetupTrigger();
            }
            if (LeftHandRestTransform != null && LeftHandRestTransform.InVisualSetupMode) {
                LeftHandRestTransform.CancelVisualSetupTrigger();
            }
            if (RightHandRestTransform != null && RightHandRestTransform.InVisualSetupMode) {
                RightHandRestTransform.CancelVisualSetupTrigger();
            }

            var charTransform = Character.GameObject.transform;
            var leftHandLocal = charTransform.InverseTransformPoint(leftHandBone.position);
            var rightHandLocal = charTransform.InverseTransformPoint(rightHandBone.position);

            var leftTipWorld = GetIndexTipWorldPosition(animator, true);
            var rightTipWorld = GetIndexTipWorldPosition(animator, false);

            var leftTipLocal = charTransform.InverseTransformPoint(leftTipWorld);
            var rightTipLocal = charTransform.InverseTransformPoint(rightTipWorld);

            // KEYBOARD_RIG_ORIGIN: in between X and Y of hands, Z from index fingertips
            var rigX = (leftHandLocal.x + rightHandLocal.x) * 0.5f;
            var rigY = (leftHandLocal.y + rightHandLocal.y) * 0.5f;
            var rigZ = (leftTipLocal.z + rightTipLocal.z) * 0.5f;
            var newRigPos = new Vector3(rigX, rigY, rigZ);

            // Hand Rest Poses relative to Keyboard Rig Origin (facing straight forward, rotation zero)
            var newLeftPos = leftHandLocal - newRigPos;
            var newRightPos = rightHandLocal - newRigPos;

            var invCharRot = Quaternion.Inverse(charTransform.rotation);
            var leftHandLocalRot = invCharRot * leftHandBone.rotation;
            var rightHandLocalRot = invCharRot * rightHandBone.rotation;

            var userLeftRot = RemoveHandIKIntermediateRotation(true, leftHandLocalRot.eulerAngles);
            var userRightRot = RemoveHandIKIntermediateRotation(false, rightHandLocalRot.eulerAngles);

            // Apply values using SetDataInput and broadcast
            if (RootTransform != null) {
                RootTransform.Position = newRigPos;
                RootTransform.Rotation = Vector3.zero;
                RootTransform.SetDataInput(nameof(RootTransform.Position), newRigPos, true);
                RootTransform.SetDataInput(nameof(RootTransform.Rotation), Vector3.zero, true);
                RootTransform.BroadcastDataInput(nameof(RootTransform.Position));
                RootTransform.BroadcastDataInput(nameof(RootTransform.Rotation));
            }

            if (LeftHandRestTransform != null) {
                LeftHandRestTransform.Position = newLeftPos;
                LeftHandRestTransform.Rotation = userLeftRot;
                LeftHandRestTransform.SetDataInput(nameof(LeftHandRestTransform.Position), newLeftPos, true);
                LeftHandRestTransform.SetDataInput(nameof(LeftHandRestTransform.Rotation), userLeftRot, true);
                LeftHandRestTransform.BroadcastDataInput(nameof(LeftHandRestTransform.Position));
                LeftHandRestTransform.BroadcastDataInput(nameof(LeftHandRestTransform.Rotation));
            }

            if (RightHandRestTransform != null) {
                RightHandRestTransform.Position = newRightPos;
                RightHandRestTransform.Rotation = userRightRot;
                RightHandRestTransform.SetDataInput(nameof(RightHandRestTransform.Position), newRightPos, true);
                RightHandRestTransform.SetDataInput(nameof(RightHandRestTransform.Rotation), userRightRot, true);
                RightHandRestTransform.BroadcastDataInput(nameof(RightHandRestTransform.Position));
                RightHandRestTransform.BroadcastDataInput(nameof(RightHandRestTransform.Rotation));
            }

            EnsureAnchors();
            UpdateRootTransform();
            UpdateHandRestTransforms();
            UpdateGroupTransforms();

            AutoSetup?.UpdateButtonState();

            Context.Service?.BroadcastOpenedScene();
            Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Success, "AUTO_SETUP".Localized(), "AUTO_SETUP_SUCCESS".Localized());
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

        public void SynchronizeFingerAnimation(KeyboardFinger finger) {
            SynchronizeAllFingerAnimations();
        }

        public void SynchronizeAllFingerAnimations() {
            if (Character == null) return;

            var userLayers = Character.OverlappingAnimations?.Where(d => !(d.CustomLayerID?.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX) ?? false)).ToList() ?? new List<OverlappingAnimationData>();
            var pluginLayers = Character.OverlappingAnimations?.Where(d => d.CustomLayerID?.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX) ?? false).ToList() ?? new List<OverlappingAnimationData>();

            // Always maintain strict canonical order of all fingers (LeftPinky -> RightPinky)
            var newPluginLayers = new List<OverlappingAnimationData>();
            foreach (KeyboardFinger f in Enum.GetValues(typeof(KeyboardFinger))) {
                var layerId = GetFingerLayerId(f);
                var animClip = GetFingerAnimation(f);
                if (string.IsNullOrEmpty(animClip)) continue;

                var existingLayer = pluginLayers.FirstOrDefault(d => d.CustomLayerID == layerId);
                bool isLeft = (int)f <= (int)KeyboardFinger.LeftThumb;
                var maskedParts = isLeft
                    ? new[] { AnimationMaskedBodyPart.LeftFingers }
                    : new[] { AnimationMaskedBodyPart.RightFingers };

                if (existingLayer == null) {
                    existingLayer = StructuredData.Create<OverlappingAnimationData>();
                    existingLayer.CustomLayerID = layerId;
                    existingLayer.Enabled = true;
                    existingLayer.Animation = animClip;
                    existingLayer.Weight = 0f;
                    existingLayer.Speed = 1f;
                    existingLayer.Additive = true;
                    existingLayer.Looping = true;
                    existingLayer.Masked = true;
                    existingLayer.MaskedBodyParts = maskedParts;
                } else {
                    existingLayer.Enabled = true;
                    existingLayer.Animation = animClip;
                    existingLayer.Additive = true;
                    existingLayer.Looping = true;
                    existingLayer.Speed = 1f;
                    existingLayer.Masked = true;
                    existingLayer.MaskedBodyParts = maskedParts;
                }
                newPluginLayers.Add(existingLayer);
            }

            userLayers.AddRange(newPluginLayers);
            Helper.SetCharacterOverlappingAnimations(Character, userLayers.ToArray());
            ResetFingerWeights();
        }
    }
}
