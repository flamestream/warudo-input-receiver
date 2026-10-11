using System.Linq;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;
using Warudo.Core.Serializations;

namespace FlameStream
{
    [AssetType(
        Id = "FlameStream.Asset.KeyboardReceiver",
        Title = "FS_ASSET_TITLE_KEYBOARD",
        Category = "FS_ASSET_CATEGORY_INPUT"
    )]
    public partial class KeyboardReceiverAsset : ReceiverAsset {

        protected override string CHARACTER_ANIM_LAYER_ID_PREFIX {
            get {
                return "🔥⌨️";
            }
        }

        protected override ushort PROTOCOL_VERSION {
            get {
                return 1;
            }
        }

        protected override string PROTOCOL_ID {
            get {
                return "K";
            }
        }

        protected override int DEFAULT_PORT {
            get {
                return 40612;
            }
        }

        // As long as the plugin is in v5.5 development, this asset stays at version 1 (no migration code needed)
        protected override int CURRENT_ASSET_VERSION => 1;

        protected override void Log(string msg) {
            UnityEngine.Debug.Log($"[FlameStream.Asset.KeyboardReceiver] {msg}");
        }


        private MultiInputCoordinatorAsset activeCoordinator;
        public MultiInputCoordinatorAsset ActiveCoordinator => activeCoordinator;
        public bool IsHandPuppeteered => activeCoordinator != null && activeCoordinator.IsEnabled && activeCoordinator.EnableHands;

        public void RegisterCoordinator(MultiInputCoordinatorAsset coordinator) {
            if (activeCoordinator == coordinator) return;
            activeCoordinator = coordinator;
            OnHandDelegationChanged();
            OnCoordinatorStateChanged();
            RequestUiRefresh();
        }

        public void UnregisterCoordinator(MultiInputCoordinatorAsset coordinator, bool isTeardown = false) {
            if (activeCoordinator != coordinator) return;
            activeCoordinator = null;
            OnHandDelegationChanged();
            if (!isTeardown && !isDestroyed) {
                OnCoordinatorStateChanged();
                RequestUiRefresh();
            }
        }

        public void OnCoordinatorStateChanged() {
            if (isDestroyed) return;
            bool isPuppeteered = activeCoordinator != null && activeCoordinator.IsEnabled;
            SetDataInputPropertyAndBroadcast(
                nameof(IsEnabled),
                disabled: isPuppeteered,
                description: isPuppeteered
                    ? string.Format("IK_DELEGATED_TO".Localized(), "ENABLE".Localized(), activeCoordinator.Name)
                    : "KEYBOARD_ENABLE_DESC".Localized()
            );

            UpdateFloatingKeysState();
            OnHandDelegationChanged();
            RefreshSetupPorts();
            RequestUiRefresh();
        }

        public string GetDelegationStatusMessage() {
            if (activeCoordinator != null && activeCoordinator.IsEnabled) {
                return string.Format("IK_DELEGATED_TO".Localized(), "HAND".Localized(), activeCoordinator.Name);
            }
            if (Character != null && IsEnabled && IsHandEnabled) {
                var leftAnchor = LeftHandAnchor;
                var rightAnchor = RightHandAnchor;
                var leftIk = Character.LeftHandIK;
                var rightIk = Character.RightHandIK;

                if (leftIk != null && leftIk.Enabled && leftIk.IkTarget != null && (leftAnchor == null || leftIk.IkTarget != leftAnchor)) {
                    return string.Format("IK_NOT_CONTROLLING_HAND".Localized(), "LEFT_HAND".Localized(), leftIk.IkTarget.Name);
                }
                if (rightIk != null && rightIk.Enabled && rightIk.IkTarget != null && (rightAnchor == null || rightIk.IkTarget != rightAnchor)) {
                    return string.Format("IK_NOT_CONTROLLING_HAND".Localized(), "RIGHT_HAND".Localized(), rightIk.IkTarget.Name);
                }
                if ((leftIk != null && !leftIk.Enabled && leftAnchor != null) || (rightIk != null && !rightIk.Enabled && rightAnchor != null)) {
                    return string.Format("IK_NOT_ACTIVE".Localized(), "HAND".Localized());
                }

                var allReceivers = Scene?.GetAssets<PointerReceiverAsset>();
                if (allReceivers != null) {
                    bool conflict = allReceivers.Any(p => p.Character == Character && p.IsEnabled && p.IsHandEnabled && p.ActiveCoordinator == null);
                    if (conflict) {
                        return string.Format("IK_CONFLICT_DETECTED".Localized(), Character.Name);
                    }
                }
                var allKeyboards = Scene?.GetAssets<KeyboardReceiverAsset>();
                if (allKeyboards != null) {
                    bool conflict = allKeyboards.Any(k => k != this && k.Character == Character && k.IsEnabled && k.IsHandEnabled && k.ActiveCoordinator == null);
                    if (conflict) {
                        return string.Format("IK_CONFLICT_DETECTED".Localized(), Character.Name);
                    }
                }
                var allGamepads = Scene?.GetAssets<GamepadReceiverAsset>();
                if (allGamepads != null) {
                    bool conflict = allGamepads.Any(g => g.Character == Character && g.IsEnabled && g.IsHandEnabled);
                    if (conflict) {
                        return string.Format("IK_CONFLICT_DETECTED".Localized(), Character.Name);
                    }
                }
            }
            return "";
        }

        private float lastUiStatusUpdateTime;

        public void UpdateUiStatus(bool force = false) {
            if (!force && Time.time - lastUiStatusUpdateTime < 0.15f) return;
            lastUiStatusUpdateTime = Time.time;

            var newDelegation = GetDelegationStatusMessage();
            if (DelegationStatus != newDelegation) {
                DelegationStatus = newDelegation;
                BroadcastDataInput(nameof(DelegationStatus));
            }
        }

        protected override void OnCreate() {
            base.OnCreate();
            OnCreateBasicSetup();
            OnCreateFingerMovement();
            OnCreateVirtualKeys();
            UpdateInputEnableDescriptions();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            CancelKeyListening();
            OnDestroyBasicSetup();
            OnDestroyFingerMovement();
            OnDestroyVirtualKeys();
        }

        public override void OnUpdate() {
            base.OnUpdate();
            if (!IsSelected && ActiveKeyListeningTarget != null) {
                CancelKeyListening();
            }
            OnUpdateState();
            OnUpdateBasicSetup();
            OnUpdateFingerMovement();
            OnUpdateVirtualKeys();
        }
    }
}
