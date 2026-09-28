using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Utility;
using static Warudo.Plugins.Core.Assets.Character.CharacterAsset;
using Cysharp.Threading.Tasks;

namespace FlameStream
{
    public partial class KeyboardReceiverAsset : ReceiverAsset {

        [Section("BASIC_SETUP")]
        [Markdown(5)]
        public string DelegationStatus = "";

        [DataInput(10)]
        [Label("ENABLE")]
        [Description("KEYBOARD_ENABLE_DESC")]
        [DisabledIf(nameof(IsHandTrackingDisabled))]
        public bool IsHandEnabled;

        public bool IsHandTrackingDisabled() => (activeCoordinator != null && activeCoordinator.IsEnabled) || IsCharacterMissing() || IsBasicSetupNotDone();

        [DataInput(12)]
        [Label("ACTIVE_TRANSITION")]
        public ActiveTransition ActiveTransition;

        public bool IsCharacterMissing() => Character == null;

        [DataInput(20)]
        [Label("CHARACTER")]
        public CharacterAsset Character;

        [DataInput(25)]
        [Label("AUTO_SETUP")]
        [HiddenIf(nameof(IsBasicSetupDone))]
        public KeyboardAutoSetup AutoSetup;

        [Trigger(26)]
        [Label("RESET_BASIC_SETUP")]
        [Description("RESET_BASIC_SETUP_DESC")]
        [HiddenIf(nameof(IsBasicSetupNotDone))]
        public void TriggerResetBasicSetup() {
            ResetBasicSetup();
        }

        [DataInput(30)]
        [Label("SAVED_DATA")]
        public KeyboardBasicSetupSavedData SavedData;

        public KeyboardBasicSetupSavedData Advanced => SavedData;

        public string BaseHandAnimation {
            get => SavedData?.BaseHandAnimation;
            set {
                if (SavedData != null) {
                    SavedData.BaseHandAnimation = value;
                    SavedData.SetDataInput(nameof(SavedData.BaseHandAnimation), value, true);
                    SavedData.BroadcastDataInput(nameof(SavedData.BaseHandAnimation));
                }
            }
        }

        public bool IsBasicSetupComplete {
            get => SavedData?.IsBasicSetupDone ?? false;
            set {
                if (SavedData != null) {
                    SavedData.IsBasicSetupDone = value;
                    SavedData.SetDataInput(nameof(SavedData.IsBasicSetupDone), value, true);
                    SavedData.BroadcastDataInput(nameof(SavedData.IsBasicSetupDone));
                }
            }
        }

        public bool IsBasicSetupDone() => IsBasicSetupComplete && Character != null && Character.Active;
        public bool IsBasicSetupNotDone() => !IsBasicSetupDone();

        public CollapsibleVisualSetupTransform RootTransform {
            get => SavedData?.RootTransform;
            set {
                if (SavedData != null) {
                    SavedData.RootTransform = value;
                    SavedData.SetDataInput(nameof(SavedData.RootTransform), value, true);
                    SavedData.BroadcastDataInput(nameof(SavedData.RootTransform));
                }
            }
        }

        public CollapsibleVisualSetupTransform LeftHandRestTransform {
            get => SavedData?.LeftHandRestTransform;
            set {
                if (SavedData != null) {
                    SavedData.LeftHandRestTransform = value;
                    SavedData.SetDataInput(nameof(SavedData.LeftHandRestTransform), value, true);
                    SavedData.BroadcastDataInput(nameof(SavedData.LeftHandRestTransform));
                }
            }
        }

        public CollapsibleVisualSetupTransform RightHandRestTransform {
            get => SavedData?.RightHandRestTransform;
            set {
                if (SavedData != null) {
                    SavedData.RightHandRestTransform = value;
                    SavedData.SetDataInput(nameof(SavedData.RightHandRestTransform), value, true);
                    SavedData.BroadcastDataInput(nameof(SavedData.RightHandRestTransform));
                }
            }
        }

        public Vector3 GetLeftHandMovementScale() => Vector3.one;

        public Vector3 GetRightHandMovementScale() => Vector3.one;

        public bool IsInAnyVisualSetup {
            get {
                if (RootTransform != null && RootTransform.InVisualSetupMode) return true;
                if (LeftHandRestTransform != null && LeftHandRestTransform.InVisualSetupMode) return true;
                if (RightHandRestTransform != null && RightHandRestTransform.InVisualSetupMode) return true;
                if (RootKeysetTransform != null && RootKeysetTransform.InVisualSetupMode) return true;
                if (LeftKeysetTransform != null && LeftKeysetTransform.InVisualSetupMode) return true;
                if (RightKeysetTransform != null && RightKeysetTransform.InVisualSetupMode) return true;
                if (ActiveVirtualKeyInVisualSetup != null) return true;
                if (ActiveKeyBindingInVisualSetup != null) return true;
                if (activeVisualSetupFinger.HasValue) return true;
                if (FingerMovementDefinitions?.FingertipOffsets != null && FingerMovementDefinitions.FingertipOffsets.IsInAnyVisualSetup()) return true;
                return false;
            }
        }


        public bool ReturnToRest {
            get => FingerTransitions?.ReturnToRest ?? true;
            set {
                if (FingerTransitions != null) FingerTransitions.ReturnToRest = value;
            }
        }

        public float RestTimeout {
            get => FingerTransitions?.RestTimeout ?? 3.0f;
            set {
                if (FingerTransitions != null) FingerTransitions.RestTimeout = value;
            }
        }

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
            var root = RootAnchor;
            if (root != null) {
                root.Transform.Position = KeyboardRootPosition;
                root.Transform.Rotation = KeyboardRootRotation;
                root.Transform.Scale = RootTransform?.Scale ?? Vector3.one;
                root.Transform.Broadcast();
                if (root.GameObject != null) {
                    root.GameObject.transform.localPosition = KeyboardRootPosition;
                    root.GameObject.transform.localRotation = Quaternion.Euler(KeyboardRootRotation);
                    root.GameObject.transform.localScale = RootTransform?.Scale ?? Vector3.one;
                }
                Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Info, Name, "RESET_ANCHORS_SUCCESS".Localized());
            } else {
                Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Warning, Name, "RESET_ANCHORS_NO_ROOT".Localized());
            }
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
                RequestUiRefresh();
            }
        }

        public void UpdateAllDebugButtonStates() {
            if (FingerMovementDefinitions?.MovementDefinitions != null) {
                foreach (var b in FingerMovementDefinitions.MovementDefinitions) {
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
            rootAnchor.Broadcast();
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
            SetupLeftHandRestTransform();
            SetupRightHandRestTransform();
        }

        public void SetupRootTransform() {
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
            if (LeftHandRestTransform == null) {
                LeftHandRestTransform = StructuredData.Create<CollapsibleVisualSetupTransform>();
                LeftHandRestTransform.Position = new Vector3(-0.07f, 0.02f, 0f);
                LeftHandRestTransform.Rotation = Vector3.zero;
                LeftHandRestTransform.Scale = Vector3.one;
                LeftHandRestTransform.SetDataInput(nameof(LeftHandRestTransform.Scale), Vector3.one, true);
            }
            LeftHandRestTransform.HideScale();
            LeftHandRestTransform.CollapsedSelf = true;
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
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Name = "⌛⚓-Left Hand Rest Pose";
                setupAnchor.Instructions = "Position and rotate the **Left Hand Rest Pose** (resting over ASDF), relative to the **Keyboard Rig Origin**.\nThe red sphere indicates the origin location (Keyboard Rig Origin).\nClick **[Apply Transform]** when finished.";
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
                tr.Scale = Vector3.one;
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
            if (RightHandRestTransform == null) {
                RightHandRestTransform = StructuredData.Create<CollapsibleVisualSetupTransform>();
                RightHandRestTransform.Position = new Vector3(0.07f, 0.02f, 0f);
                RightHandRestTransform.Rotation = Vector3.zero;
                RightHandRestTransform.Scale = Vector3.one;
                RightHandRestTransform.SetDataInput(nameof(RightHandRestTransform.Scale), Vector3.one, true);
            }
            RightHandRestTransform.HideScale();
            RightHandRestTransform.CollapsedSelf = true;
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
                setupAnchor.Transform.Scale = Vector3.one;
                setupAnchor.Name = "⌛⚓-Right Hand Rest Pose";
                setupAnchor.Instructions = "Position and rotate the **Right Hand Rest Pose** (resting over JKL;), relative to the **Keyboard Rig Origin**.\nThe red sphere indicates the origin location (Keyboard Rig Origin).\nClick **[Apply Transform]** when finished.";
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
                tr.Scale = Vector3.one;
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

            if (SavedData == null) {
                SavedData = StructuredData.Create<KeyboardBasicSetupSavedData>();
            }
            SavedData.Parent = this;
            SavedData.CollapsedSelf = true;

            SetupVisualTransforms();

            if (ActiveTransition == null) {
                ActiveTransition = StructuredData.Create<ActiveTransition>();
            }
            Watch(nameof(ActiveTransition), delegate { });
            Watch(nameof(SavedData), delegate {
                if (SavedData != null) {
                    SavedData.Parent = this;
                }
                SetupVisualTransforms();
                SynchronizeAllFingerAnimations();
                RefreshSetupPorts();
            });

            Watch(nameof(AutoSetup), delegate {
                if (AutoSetup != null) {
                    AutoSetup.Parent = this;
                }
                AutoSetup?.UpdateButtonState();
            });

            Watch(nameof(IsEnabled), delegate { 
                UpdateHandTrackingState(); 
                AutoSetup?.UpdateButtonState();
                UpdateInputEnableDescriptions();
            });
            Watch(nameof(IsHandEnabled), delegate { 
                UpdateHandTrackingState(); 
                AutoSetup?.UpdateButtonState();
            });
            Watch(nameof(Character), delegate { 
                if (Character == null) {
                    if (IsHandEnabled) {
                        SetDataInput(nameof(IsHandEnabled), false, true);
                        BroadcastDataInput(nameof(IsHandEnabled));
                    }
                }
                UpdateHandTrackingState(); 
                UpdateFloatingKeysState();
                AutoSetup?.UpdateButtonState();
                UpdateInputEnableDescriptions();
                RefreshSetupPorts();
            });

            UpdateInputEnableDescriptions();
            UpdateAdvancedSectionDebugState();
            RefreshSetupPorts();
        }

        public void RefreshSetupPorts() {
            try {
                var isEnabledPort = GetDataInputPort(nameof(IsEnabled));
                if (isEnabledPort?.Properties != null) {
                    bool isPuppeteered = activeCoordinator != null && activeCoordinator.IsEnabled;
                    isEnabledPort.Properties.disabled = isPuppeteered;
                    isEnabledPort.Properties.description = isPuppeteered
                        ? string.Format("IK_DELEGATED_TO".Localized(), "ENABLE".Localized(), activeCoordinator.Name)
                        : "KEYBOARD_ENABLE_DESC".Localized();
                    BroadcastDataInputProperties(nameof(IsEnabled));
                }
                var handPort = GetDataInputPort(nameof(IsHandEnabled));
                if (handPort?.Properties != null) {
                    bool isPuppeteered = activeCoordinator != null && activeCoordinator.IsEnabled;
                    handPort.Properties.disabled = isPuppeteered || IsHandTrackingDisabled();
                    handPort.Properties.description = isPuppeteered
                        ? string.Format("IK_DELEGATED_TO".Localized(), "ENABLE".Localized(), activeCoordinator.Name)
                        : (IsBasicSetupNotDone() ? "BASIC_SETUP_REQUIRED_DESC".Localized() : "KEYBOARD_ENABLE_DESC".Localized());
                    BroadcastDataInputProperties(nameof(IsHandEnabled));
                }
                var autoSetupPort = GetDataInputPort(nameof(AutoSetup));
                if (autoSetupPort?.Properties != null) {
                    autoSetupPort.Properties.hidden = IsBasicSetupDone();
                    BroadcastDataInputProperties(nameof(AutoSetup));
                }
                var resetTrigger = GetTriggerPort(nameof(TriggerResetBasicSetup));
                if (resetTrigger?.Properties != null) {
                    resetTrigger.Properties.hidden = IsBasicSetupNotDone();
                    BroadcastTriggerProperties(nameof(TriggerResetBasicSetup));
                }
                AutoSetup?.UpdateButtonState();
                BindingGenerator?.UpdateStatus();
                RequestUiRefresh();
            } catch { }
        }

        public void UpdateInputEnableDescriptions() {
            try {
                var handPort = GetDataInputPort(nameof(IsHandEnabled));
                if (handPort?.Properties != null) {
                    bool isPuppeteered = activeCoordinator != null && activeCoordinator.IsEnabled;
                    if (isPuppeteered) {
                        handPort.Properties.disabled = true;
                        handPort.Properties.description = string.Format("IK_DELEGATED_TO".Localized(), "ENABLE".Localized(), activeCoordinator.Name);
                    } else {
                        handPort.Properties.description = IsBasicSetupNotDone()
                            ? "BASIC_SETUP_REQUIRED_DESC".Localized()
                            : "KEYBOARD_ENABLE_DESC".Localized();
                    }
                    BroadcastDataInputProperties(nameof(IsHandEnabled));
                }

                var keysPort = GetDataInputPort(nameof(EnableFloatingKeys));
                if (keysPort?.Properties != null) {
                    if (Character == null) {
                        keysPort.Properties.description = "ENABLE_FLOATING_KEYS_DESC".Localized();
                    } else if (AreKeyDefinitionsEmpty()) {
                        keysPort.Properties.description = "ENABLE_FLOATING_KEYS_NO_PRESET_DESC".Localized();
                    } else if (!IsEnabled) {
                        keysPort.Properties.description = "ENABLE_FLOATING_KEYS_DISABLED_ASSET_DESC".Localized();
                    } else {
                        keysPort.Properties.description = "";
                    }
                    BroadcastDataInputProperties(nameof(EnableFloatingKeys));
                }
                RequestUiRefresh();
            } catch { }
        }

        public KeyboardKeyBinding ActiveKeyBindingInVisualSetup;

        void OnDestroyBasicSetup() {
            ActiveKeyBindingInVisualSetup = null;
            DeactivateAllDebugKeys();
            handIkTween?.Kill();
            handIkTween = null;

            try {
                if (Character != null && Character.Active && Character.OverlappingAnimations != null) {
                    var userLayers = Character.OverlappingAnimations.Where(d => !(d.CustomLayerID?.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX) ?? false)).ToArray();
                    Helper.SetCharacterOverlappingAnimations(Character, userLayers).Forget();
                }
            } catch {}
        }

        void OnUpdateBasicSetup() {
            UpdateUiStatus();
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

        DG.Tweening.Tween handIkTween;
        float currentHandIkWeight = 0f;

        public void OnHandDelegationChanged() {
            if (activeCoordinator != null) {
                handIkTween?.Kill();
            } else {
                UpdateHandTrackingState();
            }
        }

        void TransitionIkLimbs(bool isEnabledWanted, DG.Tweening.TweenCallback onComplete = null) {
            if (Character == null) return;
            if (activeCoordinator != null) {
                return;
            }

            var transition = ActiveTransition ?? new ActiveTransition();
            var targetWeight = isEnabledWanted ? 1f : 0f;

            var leftIk = Character.LeftHandIK;
            var rightIk = Character.RightHandIK;
            var leftAnchor = LeftHandAnchor;
            var rightAnchor = RightHandAnchor;

            if (isEnabledWanted) {
                if (leftIk != null && leftAnchor != null) {
                    leftIk.Enabled = true;
                    leftIk.IkTarget = leftAnchor;
                }
                if (rightIk != null && rightAnchor != null) {
                    rightIk.Enabled = true;
                    rightIk.IkTarget = rightAnchor;
                }
            }

            handIkTween?.Kill();

            if (transition.Time <= 0f) {
                currentHandIkWeight = targetWeight;
                ApplyHandIkWeight(targetWeight);
                if (!isEnabledWanted) {
                    if (leftIk != null && leftAnchor != null && leftIk.IkTarget == leftAnchor) { leftIk.Enabled = false; leftIk.IkTarget = null; leftIk.Broadcast(); }
                    if (rightIk != null && rightAnchor != null && rightIk.IkTarget == rightAnchor) { rightIk.Enabled = false; rightIk.IkTarget = null; rightIk.Broadcast(); }
                    ClearAnchors();
                    ResetFingerWeights();
                    ClearFingerAnimations();
                }
                onComplete?.Invoke();
                return;
            }

            handIkTween = DG.Tweening.DOTween.To(
                () => currentHandIkWeight,
                v => {
                    currentHandIkWeight = v;
                    ApplyHandIkWeight(v);
                },
                targetWeight,
                transition.Time
            )
            .SetEase(transition.Ease)
            .OnComplete(() => {
                if (!isEnabledWanted) {
                    if (leftIk != null && leftAnchor != null && leftIk.IkTarget == leftAnchor) { leftIk.Enabled = false; leftIk.IkTarget = null; leftIk.Broadcast(); }
                    if (rightIk != null && rightAnchor != null && rightIk.IkTarget == rightAnchor) { rightIk.Enabled = false; rightIk.IkTarget = null; rightIk.Broadcast(); }
                    ClearAnchors();
                    ResetFingerWeights();
                    ClearFingerAnimations();
                }
                onComplete?.Invoke();
            });
        }

        public void SetBaseHandPoseWeight(float weight) {
            if (Character == null || Character.OverlappingAnimations == null) return;
            var baseLayerId = $"{CHARACTER_ANIM_LAYER_ID_PREFIX} Base Hand Pose";
            var baseLayerData = Array.Find(Character.OverlappingAnimations, a => a.CustomLayerID == baseLayerId);
            if (baseLayerData != null) {
                if (Mathf.Abs(baseLayerData.Weight - weight) > 0.001f) {
                    baseLayerData.SetDataInput(nameof(baseLayerData.Weight), weight, false);
                }
                int animancerIdx = GetAnimancerLayerIndex(baseLayerData);
                if (animancerIdx >= 0) {
                    SetAnimancerLayerWeight(animancerIdx, weight);
                }
            }
        }

        void ApplyHandIkWeight(float weight) {
            SetBaseHandPoseWeight(weight);
            if (activeCoordinator != null) return;
            if (Character == null) return;

            var leftAnchor = LeftHandAnchor;
            var rightAnchor = RightHandAnchor;

            var leftIk = Character.LeftHandIK;
            if (leftIk != null && leftIk.Enabled && leftAnchor != null && leftIk.IkTarget == leftAnchor) {
                leftIk.PositionWeight = weight;
                leftIk.RotationWeight = weight;
                leftIk.BendGoalWeight = weight;
                leftIk.Broadcast();
            }

            var rightIk = Character.RightHandIK;
            if (rightIk != null && rightIk.Enabled && rightAnchor != null && rightIk.IkTarget == rightAnchor) {
                rightIk.PositionWeight = weight;
                rightIk.RotationWeight = weight;
                rightIk.BendGoalWeight = weight;
                rightIk.Broadcast();
            }
        }

        void UpdateHandTrackingState() {
            if (Character != null && IsEnabled && IsHandEnabled && IsBasicSetupDone()) {
                EnsureAnchors();
                SynchronizeAllFingerAnimations();
                TransitionIkLimbs(true);
            } else {
                TransitionIkLimbs(false);
            }
        }

        public void ClearFingerAnimations() {
            if (Character == null || Character.OverlappingAnimations == null) return;
            var userLayers = Character.OverlappingAnimations.Where(d => !(d.CustomLayerID?.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX) ?? false)).ToArray();
            Helper.SetCharacterOverlappingAnimations(Character, userLayers).Forget();
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
                leftHandAnchor = Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INTERNAL".Localized());
                leftHandAnchor.Name = "🔒⚓-🔥⌨️🫲 Left Hand";
                Scene.UpdateNewAssetName(leftHandAnchor);
                LeftHandAnchorAssetId = leftHandAnchor.Id;
                BroadcastDataInput(nameof(LeftHandAnchorAssetId));
            } else if (leftHandAnchor.Name == "⚓-🔥⌨️🫲 Left Hand") {
                leftHandAnchor.Name = "🔒⚓-🔥⌨️🫲 Left Hand";
                Scene.UpdateNewAssetName(leftHandAnchor);
            }
            leftHandAnchor.Attachable.Parent = rootAnchor;
            if (rootAnchor.GameObject != null) {
                leftHandAnchor.GameObject.transform.SetParent(rootAnchor.GameObject.transform, false);
            }
            var leftIkRot = ApplyHandIKIntermediateRotation(true, LeftHandRestRotation);
            leftHandAnchor.Transform.Position = LeftHandRestPosition;
            leftHandAnchor.Transform.Rotation = leftIkRot;
            leftHandAnchor.Transform.Broadcast();
            leftHandAnchor.Broadcast();

            // 3. Right Hand IK Anchor
            AnchorAsset rightHandAnchor = RightHandAnchor;
            if (rightHandAnchor == null) {
                rightHandAnchor = Scene.AddAssetToGroup<AnchorAsset>("FS_ASSET_CATEGORY_INTERNAL".Localized());
                rightHandAnchor.Name = "🔒⚓-🔥⌨️🫱 Right Hand";
                Scene.UpdateNewAssetName(rightHandAnchor);
                RightHandAnchorAssetId = rightHandAnchor.Id;
                BroadcastDataInput(nameof(RightHandAnchorAssetId));
            } else if (rightHandAnchor.Name == "⚓-🔥⌨️🫱 Right Hand") {
                rightHandAnchor.Name = "🔒⚓-🔥⌨️🫱 Right Hand";
                Scene.UpdateNewAssetName(rightHandAnchor);
            }
            rightHandAnchor.Attachable.Parent = rootAnchor;
            if (rootAnchor.GameObject != null) {
                rightHandAnchor.GameObject.transform.SetParent(rootAnchor.GameObject.transform, false);
            }
            var rightIkRot = ApplyHandIKIntermediateRotation(false, RightHandRestRotation);
            rightHandAnchor.Transform.Position = RightHandRestPosition;
            rightHandAnchor.Transform.Rotation = rightIkRot;
            rightHandAnchor.Transform.Broadcast();
            rightHandAnchor.Broadcast();

            rootAnchor.Broadcast();
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

            string capturedAnim = !string.IsNullOrEmpty(Character.DefaultIdleAnimation) 
                ? Character.DefaultIdleAnimation 
                : Character.OverlappingAnimations?.FirstOrDefault(a => !string.IsNullOrEmpty(a.Animation) && !(a.CustomLayerID?.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX) ?? false))?.Animation;

            if (!string.IsNullOrEmpty(capturedAnim)) {
                BaseHandAnimation = capturedAnim;
                if (SavedData != null) {
                    SavedData.SetDataInput(nameof(SavedData.BaseHandAnimation), capturedAnim, true);
                    SavedData.BroadcastDataInput(nameof(SavedData.BaseHandAnimation));
                }
            }

            IsBasicSetupComplete = true;

            EnsureAnchors();
            UpdateRootTransform();
            UpdateHandRestTransforms();
            UpdateGroupTransforms();
            SynchronizeAllFingerAnimations();

            if (AutoSetup != null) {
                AutoSetup.CollapsedSelf = true;
            }
            AutoSetup?.UpdateButtonState();
            RefreshSetupPorts();

            Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Success, "AUTO_SETUP".Localized(), "AUTO_SETUP_SUCCESS".Localized());
        }

        public void ResetBasicSetup() {
            IsBasicSetupComplete = false;

            if (IsHandEnabled) {
                IsHandEnabled = false;
                SetDataInput(nameof(IsHandEnabled), false, true);
                BroadcastDataInput(nameof(IsHandEnabled));
            }

            if (AutoSetup != null) {
                AutoSetup.CollapsedSelf = true;
            }
            UpdateHandTrackingState();
            RefreshSetupPorts();

            Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Info, "RESET_BASIC_SETUP".Localized(), "RESET_BASIC_SETUP_DESC".Localized());
        }

        public void ClearAnchors() {
            var root = RootAnchor;
            var left = LeftHandAnchor;
            var right = RightHandAnchor;

            if (Character != null) {
                EnableLimb(false, Character.LeftHandIK, left);
                EnableLimb(false, Character.RightHandIK, right);
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
            if (activeCoordinator != null) return;
            if (limb == null) return;
            if (isEnabled && anchor != null) {
                limb.Enabled = true;
                limb.IkTarget = anchor;
                limb.PositionWeight = 1.0f;
                limb.RotationWeight = 1.0f;
                limb.Broadcast();
            } else if (!isEnabled) {
                if (anchor != null && limb.IkTarget == anchor) {
                    limb.Enabled = false;
                    limb.IkTarget = null;
                    limb.PositionWeight = 0f;
                    limb.RotationWeight = 0f;
                    limb.Broadcast();
                }
            }
        }

        public void SynchronizeFingerAnimation(KeyboardFinger finger) {
            SynchronizeAllFingerAnimations();
        }

        public void SynchronizeAllFingerAnimations() {
            if (Character == null) return;

            var userLayers = Character.OverlappingAnimations?.Where(d => !(d.CustomLayerID?.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX) ?? false)).ToList() ?? new List<OverlappingAnimationData>();
            var pluginLayers = Character.OverlappingAnimations?.Where(d => d.CustomLayerID?.StartsWith(CHARACTER_ANIM_LAYER_ID_PREFIX) ?? false).ToList() ?? new List<OverlappingAnimationData>();

            var newPluginLayers = new List<OverlappingAnimationData>();

            // 1. Base Hand Pose Layer (Non-additive, masked to Left & Right fingers)
            var baseAnimClip = !string.IsNullOrEmpty(BaseHandAnimation) ? BaseHandAnimation : Character.DefaultIdleAnimation;
            if (!string.IsNullOrEmpty(baseAnimClip)) {
                var baseLayerId = $"{CHARACTER_ANIM_LAYER_ID_PREFIX} Base Hand Pose";
                var baseLayer = pluginLayers.FirstOrDefault(d => d.CustomLayerID == baseLayerId);
                var baseMask = new[] { AnimationMaskedBodyPart.LeftFingers, AnimationMaskedBodyPart.RightFingers };
                float targetBaseWeight = (IsEnabled && IsHandEnabled) ? (handIkTween != null && handIkTween.IsActive() ? currentHandIkWeight : 1f) : 0f;
                if (activeCoordinator != null && activeCoordinator.IsEnabled && activeCoordinator.EnableHands) {
                    targetBaseWeight = 1f;
                }

                if (baseLayer == null) {
                    baseLayer = StructuredData.Create<OverlappingAnimationData>();
                    baseLayer.CustomLayerID = baseLayerId;
                    baseLayer.Enabled = true;
                    baseLayer.Animation = baseAnimClip;
                    baseLayer.SetDataInput(nameof(baseLayer.Weight), targetBaseWeight, false);
                    baseLayer.Speed = 1f;
                    baseLayer.Additive = false; // Non-additive: sets stable baseline
                    baseLayer.Looping = false;
                    baseLayer.Masked = true;
                    baseLayer.MaskedBodyParts = baseMask;
                } else {
                    baseLayer.Enabled = true;
                    baseLayer.Animation = baseAnimClip;
                    baseLayer.SetDataInput(nameof(baseLayer.Weight), targetBaseWeight, false);
                    baseLayer.Additive = false;
                    baseLayer.Looping = false;
                    baseLayer.Speed = 1f;
                    baseLayer.Masked = true;
                    baseLayer.MaskedBodyParts = baseMask;
                }
                newPluginLayers.Add(baseLayer);
            }

            // 2. 10 Finger Press Overlay Animations (Additive = true)
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
                    existingLayer.Looping = false;
                    existingLayer.Masked = true;
                    existingLayer.MaskedBodyParts = maskedParts;
                } else {
                    existingLayer.Enabled = true;
                    existingLayer.Animation = animClip;
                    existingLayer.Additive = true;
                    existingLayer.Looping = false;
                    existingLayer.SetDataInput(nameof(existingLayer.Looping), false, true);
                    existingLayer.Speed = 1f;
                    existingLayer.Masked = true;
                    existingLayer.MaskedBodyParts = maskedParts;
                }
                newPluginLayers.Add(existingLayer);
            }

            userLayers.AddRange(newPluginLayers);
            Helper.SetCharacterOverlappingAnimations(Character, userLayers.ToArray()).Forget();
            ResetFingerWeights();
        }
    }
}
