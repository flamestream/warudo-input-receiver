using System;
using System.Linq;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Utility;

namespace FlameStream {

    public enum HandCoordinationMode {
        Hybrid = 0,     // Mouse/Pen with dominant hand while off-hand stays on keyboard shortcuts
        NonHybrid = 1   // Dedicated single-device: mouse/pen uses desk rest, typing brings both hands to keyboard
    }

    public enum CoordinatorHandState {
        Keyboard = 0,
        Mouse = 1,
        Pen = 2,
        PointerRest = 3,
        Released = 4
    }

    [AssetType(
        Id = "FlameStream.Asset.MultiInputCoordinator",
        Title = "FS_ASSET_TITLE_MULTI_INPUT_COORDINATOR",
        Category = "FS_ASSET_CATEGORY_INPUT"
    )]
    public partial class MultiInputCoordinatorAsset : Asset {

        protected long uiVersion = 0;

        public override long GetVersion() {
            return base.GetVersion() + uiVersion;
        }

        public async void RequestUiRefresh() {
            await Cysharp.Threading.Tasks.UniTask.Yield();
            uiVersion++;
            Broadcast();
        }

        [Section("BASIC_SETUP")]

        [DataInput]
        [Label("ENABLE")]
        public bool IsEnabled = true;

        [DataInput]
        [Label("CHARACTER")]
        [Description("CHARACTER_DESC")]
        public CharacterAsset Character;

        [DataInput]
        [Label("KEYBOARD_RECEIVER")]
        [Description("KEYBOARD_RECEIVER_DESC")]
        public KeyboardReceiverAsset KeyboardReceiver;

        [DataInput]
        [Label("MOUSE_RECEIVER")]
        [Description("MOUSE_RECEIVER_DESC")]
        public PointerReceiverAsset MouseReceiver;

        [DataInput]
        [Label("PEN_RECEIVER")]
        [Description("PEN_RECEIVER_DESC")]
        public PointerReceiverAsset PenReceiver;


        [Section("COORDINATION_SETTINGS")]

        [DataInput]
        [Label("COORDINATION_MODE")]
        public HandCoordinationMode CoordinationMode = HandCoordinationMode.Hybrid;


        [DataInput]
        [Label("ACTIVE_TRANSITION")]
        public ActiveTransition Transition;

        [DataInput]
        [Label("ENABLE_INACTIVITY_RETURN")]
        public bool EnableInactivityReturn = true;

        [DataInput]
        [Label("MOUSE_RETURN_TIMEOUT")]
        public float MouseReturnTimeout = 2.0f;

        [DataInput]
        [Label("PEN_RETURN_TIMEOUT")]
        public float PenReturnTimeout = 3.0f;


        [Section("STATUS_DASHBOARD")]

        [Markdown]
        public string ConnectionOverview = "";

        [Markdown]
        public string LeftHandStatus = "";

        [Markdown]
        public string RightHandStatus = "";


        protected override void OnCreate() {
            base.OnCreate();
            OnCreateIK();
            OnCreateAnimation();

            Watch(nameof(IsEnabled), delegate { OnCoordinatorSettingChange(); });
            Watch(nameof(Character), delegate { OnCoordinatorSettingChange(); });
            Watch(nameof(KeyboardReceiver), delegate { OnReceiverLinksChanged(); });
            Watch(nameof(MouseReceiver), delegate { OnReceiverLinksChanged(); });
            Watch(nameof(PenReceiver), delegate { OnReceiverLinksChanged(); });
            Watch(nameof(CoordinationMode), delegate { RequestUiRefresh(); });
            Watch(nameof(EnableInactivityReturn), delegate { RequestUiRefresh(); });

            OnCoordinatorSettingChange();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            UnregisterAllReceivers();
            OnDestroyIK();
            OnDestroyAnimation();
        }

        public override void OnUpdate() {
            base.OnUpdate();
            EnsureReceiversRegistered();
            OnUpdateIK();
            OnUpdateAnimation();
            UpdateDashboardStatus();
        }

        void OnCoordinatorSettingChange() {
            if (!IsEnabled) {
                UnregisterAllReceivers();
                ReleaseCharacterIK();
            } else {
                RegisterAllReceivers();
                BindCharacterIK();
            }
            RequestUiRefresh();
        }

        void OnReceiverLinksChanged() {
            hasInitializedMouse = false;
            hasInitializedPen = false;
            EnsureReceiversRegistered();
            RequestUiRefresh();
        }

        void UpdateDashboardStatus() {
            var overview = GetConnectionOverviewMessage();
            if (ConnectionOverview != overview) {
                ConnectionOverview = overview;
                BroadcastDataInput(nameof(ConnectionOverview));
            }

            var left = GetLeftHandStatusMessage();
            if (LeftHandStatus != left) {
                LeftHandStatus = left;
                BroadcastDataInput(nameof(LeftHandStatus));
            }

            var right = GetRightHandStatusMessage();
            if (RightHandStatus != right) {
                RightHandStatus = right;
                BroadcastDataInput(nameof(RightHandStatus));
            }
        }

        string GetConnectionOverviewMessage() {
            if (!Active) {
                return "⚠️ " + "COORDINATOR_INACTIVE_NOTICE".Localized();
            }
            if (!IsEnabled) {
                return "⚠️ " + "COORDINATOR_DISABLED_NOTICE".Localized();
            }

            var kb = KeyboardReceiver != null ? $"⌨️ {KeyboardReceiver.Name}" : "KEYBOARD_NONE".Localized();
            var mouse = MouseReceiver != null ? $"🖱️ {MouseReceiver.Name} ({(MouseReceiver.IsRightHanded ? "RIGHT_HAND" : "LEFT_HAND").Localized()})" : "MOUSE_NONE".Localized();
            var pen = PenReceiver != null ? $"🖊️ {PenReceiver.Name} ({(PenReceiver.IsRightHanded ? "RIGHT_HAND" : "LEFT_HAND").Localized()})" : "PEN_NONE".Localized();

            return $"**{"CONNECTED_DEVICES".Localized()}:**\n- {kb}\n- {mouse}\n- {pen}";
        }

        string GetLeftHandStatusMessage() {
            return $"**{"LEFT_HAND".Localized()}:** {FormatHandState(currentLeftState)}";
        }

        string GetRightHandStatusMessage() {
            return $"**{"RIGHT_HAND".Localized()}:** {FormatHandState(currentRightState)}";
        }

        string FormatHandState(CoordinatorHandState state) {
            switch (state) {
                case CoordinatorHandState.Keyboard: return "STATE_KEYBOARD".Localized();
                case CoordinatorHandState.Mouse: return "STATE_MOUSE".Localized();
                case CoordinatorHandState.Pen: return "STATE_PEN".Localized();
                case CoordinatorHandState.PointerRest: return "STATE_POINTER_REST".Localized();
                case CoordinatorHandState.Released: return "STATE_RELEASED".Localized();
                default: return state.ToString();
            }
        }
    }
}
