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
        Hybrid = 0,     // Per-hand independent tracking: active input moves only its hand; idle timer returns both to ReturnTarget
        NonHybrid = 1   // Dedicated single-device: mouse/pen uses desk rest, typing brings both hands to keyboard
    }

    public enum CoordinatorHandState {
        Released = 0,
        Keyboard = 1,
        Mouse = 2,
        Pen = 3
    }

    public enum InactivityReturnTarget {
        Keyboard = 0,
        Mouse = 1,
        Pen = 2
    }

    [AssetType(
        Id = "FlameStream.Asset.MultiInputCoordinator",
        Title = "FS_ASSET_TITLE_MULTI_INPUT_COORDINATOR",
        Category = "FS_ASSET_CATEGORY_INPUT"
    )]
    public partial class MultiInputCoordinatorAsset : ReceiverAsset {

        protected override ushort PROTOCOL_VERSION => 1;
        protected override string PROTOCOL_ID => "FSMC";
        protected override int DEFAULT_PORT => 40610;
        protected override string CHARACTER_ANIM_LAYER_ID_PREFIX => "🔥🎮";

        protected override void Log(string msg) {
            Debug.Log($"[FlameStream.Asset.MultiInputCoordinator] {msg}");
        }

        private int lockedPort = 0;
        private int onReadyCountdown = 3;
        private bool isReady = false;

        [Section("BASIC_SETUP")]

        [DataInput]
        [Label("ENABLE_REGISTERED_RECEIVERS")]
        [Description("ENABLE_REGISTERED_RECEIVERS_DESC")]
        public bool EnableRegisteredReceivers = true;

        [DataInput]
        [Label("ENABLE_REGISTERED_RECEIVERS_BASIC_SETUP")]
        [Description("ENABLE_REGISTERED_RECEIVERS_BASIC_SETUP_DESC")]
        public bool EnableHands = true;

        [DataInput]
        [Label("CHARACTER")]
        [Description("COORDINATOR_CHARACTER_AUTO_DESC")]
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
        public ActiveTransition ActiveTransition;

        [DataInput]
        [Label("INACTIVE_TRANSITION")]
        public InactiveTransition InactiveTransition;

        [DataInput]
        [Label("SWITCH_TRANSITION")]
        public Transition SwitchTransition;

        [DataInput]
        [Label("ENABLE_INACTIVITY_RETURN")]
        public bool EnableInactivityReturn = true;

        [DataInput]
        [Label("RETURN_TARGET")]
        [HiddenIf(nameof(IsReturnDisabled))]
        public InactivityReturnTarget ReturnTarget = InactivityReturnTarget.Keyboard;

        [DataInput]
        [Label("KEYBOARD_IDLE_TIMEOUT")]
        [FloatSlider(0.1f, 10f, 0.1f)]
        [HiddenIf(nameof(IsReturnDisabled))]
        public float KeyboardIdleTimeout = 2.0f;

        [DataInput]
        [Label("MOUSE_IDLE_TIMEOUT")]
        [FloatSlider(0.1f, 10f, 0.1f)]
        [HiddenIf(nameof(IsReturnDisabled))]
        public float MouseIdleTimeout = 2.0f;

        [DataInput]
        [Label("PEN_IDLE_TIMEOUT")]
        [FloatSlider(0.1f, 10f, 0.1f)]
        [HiddenIf(nameof(IsReturnDisabled))]
        public float PenIdleTimeout = 3.0f;

        [DataInput]
        [Hidden]
        public float MouseReturnTimeout = 2.0f;

        [DataInput]
        [Hidden]
        public float PenReturnTimeout = 3.0f;

        public bool IsReturnDisabled() => !EnableInactivityReturn;


        [Section("STATUS_DASHBOARD")]

        [Markdown]
        public string ConnectionOverview = "";

        [Markdown]
        public string LeftHandStatus = "";

        [Markdown]
        public string RightHandStatus = "";

        [Markdown]
        public string AnimationLayersOverview = "";


        protected override void OnCreate() {
            base.OnCreate();
            OnCreateIK();
            OnCreateAnimation();

            if (ActiveTransition == null) {
                ActiveTransition = StructuredData.Create<ActiveTransition>();
                SetDataInput(nameof(ActiveTransition), ActiveTransition, true);
            }
            if (ActiveTransition != null) {
                var delayPort = ActiveTransition.GetDataInputPort(nameof(ActiveTransition.Delay));
                if (delayPort?.Properties != null) {
                    delayPort.Properties.hidden = false;
                }
            }
            Watch(nameof(ActiveTransition), delegate {
                if (ActiveTransition != null) {
                    var delayPort = ActiveTransition.GetDataInputPort(nameof(ActiveTransition.Delay));
                    if (delayPort?.Properties != null) {
                        delayPort.Properties.hidden = false;
                    }
                }
            });

            if (InactiveTransition == null) {
                InactiveTransition = StructuredData.Create<InactiveTransition>();
                SetDataInput(nameof(InactiveTransition), InactiveTransition, true);
            }

            if (SwitchTransition == null) {
                SwitchTransition = StructuredData.Create<Transition>();
                SwitchTransition.Time = 0.2f;
                SwitchTransition.Ease = DG.Tweening.Ease.OutQuad;
                SetDataInput(nameof(SwitchTransition), SwitchTransition, true);
            }

            Watch(nameof(IsEnabled), delegate { OnCoordinatorSettingChange(); });
            Watch(nameof(EnableRegisteredReceivers), delegate { PuppetRegisteredReceivers(); });
            Watch(nameof(EnableHands), delegate { OnEnableHandsChange(); });
            Watch(nameof(Character), delegate { OnCoordinatorSettingChange(); });
            Watch(nameof(KeyboardReceiver), delegate { OnReceiverLinksChanged(); });
            Watch(nameof(MouseReceiver), delegate { OnReceiverLinksChanged(); });
            Watch(nameof(PenReceiver), delegate { OnReceiverLinksChanged(); });
            Watch(nameof(CoordinationMode), delegate { RequestUiRefresh(); });
            Watch(nameof(EnableInactivityReturn), delegate { RequestUiRefresh(); });
            Watch(nameof(ReturnTarget), delegate { RequestUiRefresh(); });

            if (MouseIdleTimeout == 2.0f && MouseReturnTimeout != 2.0f) MouseIdleTimeout = MouseReturnTimeout;
            if (PenIdleTimeout == 3.0f && PenReturnTimeout != 3.0f) PenIdleTimeout = PenReturnTimeout;

            if (Active && IsEnabled) {
                lockedPort = Port;
            }
            // Defer ValidateConfiguration() and OnCoordinatorSettingChange() to OnUpdate after the 3-frame settle period
        }

        protected override void OnDestroy() {
            isDestroyed = true;
            UnregisterAllReceivers(isTeardown: true);
            OnDestroyIK();
            OnDestroyAnimation();
            base.OnDestroy();
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (onReadyCountdown > 0) {
                --onReadyCountdown;
                if (onReadyCountdown == 0) {
                    isReady = true;
                    if (IsEnabled) {
                        EnsureReceiversRegistered();
                        ValidateConfiguration();
                        PuppetRegisteredReceivers();
                        if (EnableHands && Character != null && !HasCharacterMismatch()) {
                            BindCharacterIK();
                            KeyboardReceiver?.SetBaseHandPoseWeight(1f);
                        }
                    } else {
                        ValidateConfiguration();
                    }
                }
                return;
            }

            if (Time.time - lastConfigValidationTime >= 0.25f) {
                lastConfigValidationTime = Time.time;
                ValidateConfiguration();
            }

            if (IsEnabled && lastState != null) {
                MouseReceiver?.InjectRawState(lastState);
                PenReceiver?.InjectRawState(lastState);
            }

            EnsureReceiversRegistered();
            OnUpdateIK();
            OnUpdateAnimation();
            UpdateDashboardStatus();
        }

        void OnCoordinatorSettingChange() {
            if (!isReady) return;
            if (!IsEnabled) {
                lockedPort = 0;
                UnregisterAllReceivers();
                if (isIkBound) ReleaseCharacterIK();
            } else {
                lockedPort = Port;
                RegisterAllReceivers();
                PuppetRegisteredReceivers();
                if (EnableHands && Character != null && !HasCharacterMismatch()) {
                    BindCharacterIK();
                    KeyboardReceiver?.SetBaseHandPoseWeight(1f);
                } else {
                    ReleaseCharacterIK();
                    KeyboardReceiver?.SetBaseHandPoseWeight(0f);
                }
            }
            ValidateConfiguration();
            RequestUiRefresh();
        }

        void OnEnableHandsChange() {
            if (!isReady || !IsEnabled) return;
            PuppetRegisteredReceivers();
            if (EnableHands && Character != null && !HasCharacterMismatch()) {
                BindCharacterIK();
                KeyboardReceiver?.SetBaseHandPoseWeight(1f);
            } else {
                ReleaseCharacterIK();
                KeyboardReceiver?.SetBaseHandPoseWeight(0f);
            }
            ValidateConfiguration();
            RequestUiRefresh();
        }

        public void PuppetRegisteredReceivers() {
            if (!IsEnabled) return;

            var receivers = new ReceiverAsset[] { KeyboardReceiver, MouseReceiver, PenReceiver };
            string desc = string.Format("IK_DELEGATED_TO".Localized(), "ENABLE".Localized(), Name);

            foreach (var r in receivers) {
                if (r == null) continue;

                if (EnableRegisteredReceivers) {
                    if (!r.IsEnabled) {
                        r.SetDataInput(nameof(r.IsEnabled), true, true);
                    }
                    r.SetDataInputPropertyAndBroadcast(nameof(r.IsEnabled), disabled: true, description: desc);
                } else {
                    if (r.IsEnabled) {
                        r.SetDataInput(nameof(r.IsEnabled), false, true);
                    }
                    r.SetDataInputPropertyAndBroadcast(nameof(r.IsEnabled), disabled: true, description: desc);
                }

                if (r is PointerReceiverAsset pr) {
                    if (EnableHands) {
                        if (!pr.IsHandEnabled) {
                            pr.SetDataInput(nameof(pr.IsHandEnabled), true, true);
                        }
                        pr.SetDataInputPropertyAndBroadcast(nameof(pr.IsHandEnabled), disabled: true, description: desc);
                    } else {
                        if (pr.IsHandEnabled) {
                            pr.SetDataInput(nameof(pr.IsHandEnabled), false, true);
                        }
                        pr.SetDataInputPropertyAndBroadcast(nameof(pr.IsHandEnabled), disabled: true, description: desc);
                    }
                    pr.OnCoordinatorStateChanged();
                } else if (r is KeyboardReceiverAsset kr) {
                    if (EnableHands) {
                        if (!kr.IsHandEnabled) {
                            kr.SetDataInput(nameof(kr.IsHandEnabled), true, true);
                        }
                        kr.SetDataInputPropertyAndBroadcast(nameof(kr.IsHandEnabled), disabled: true, description: desc);
                    } else {
                        if (kr.IsHandEnabled) {
                            kr.SetDataInput(nameof(kr.IsHandEnabled), false, true);
                        }
                        kr.SetDataInputPropertyAndBroadcast(nameof(kr.IsHandEnabled), disabled: true, description: desc);
                    }
                    kr.OnCoordinatorStateChanged();
                }
            }
        }

        public void NotifyRegisteredReceiversStateChanged() {
            PuppetRegisteredReceivers();
        }

        void OnReceiverLinksChanged() {
            if (!isReady) return;
            if (Scene != null) {
                var krs = Scene.GetAssets<KeyboardReceiverAsset>();
                if (krs != null) {
                    foreach (var kr in krs) {
                        if (kr.ActiveCoordinator == this && kr != KeyboardReceiver) {
                            kr.UnregisterCoordinator(this);
                        }
                    }
                }
                var prs = Scene.GetAssets<PointerReceiverAsset>();
                if (prs != null) {
                    foreach (var pr in prs) {
                        if (pr.ActiveCoordinator == this && pr != MouseReceiver && pr != PenReceiver) {
                            pr.UnregisterCoordinator(this);
                        }
                    }
                }
            }

            hasInitializedMouse = false;
            hasInitializedPen = false;
            if (IsEnabled) {
                EnsureReceiversRegistered();
                ValidateConfiguration();
                PuppetRegisteredReceivers();
            } else {
                ValidateConfiguration();
            }
            RequestUiRefresh();
        }

        private float lastConfigValidationTime;
        private bool isPortAndCharLocked;
        private string lastEnableDescription;
        private bool? lastEnableDisabled;
        private string lastEnableHandsDescription;
        private bool? lastEnableHandsDisabled;

        void SetEnablePropertyIfChanged(bool disabled, string description) {
            if (lastEnableDisabled != disabled || lastEnableDescription != description) {
                lastEnableDisabled = disabled;
                lastEnableDescription = description;
                SetDataInputPropertyAndBroadcast(nameof(IsEnabled), disabled: disabled, description: description);
            }
        }

        void SetEnableHandsPropertyIfChanged(bool disabled, string description) {
            if (lastEnableHandsDisabled != disabled || lastEnableHandsDescription != description) {
                lastEnableHandsDisabled = disabled;
                lastEnableHandsDescription = description;
                SetDataInputPropertyAndBroadcast(nameof(EnableHands), disabled: disabled, description: description);
            }
        }

        void ValidateConfiguration() {
            // Port and Character inspector lock (static, only broadcast once)
            if (!isPortAndCharLocked) {
                SetDataInputPropertyAndBroadcast(
                    nameof(Port),
                    disabled: true,
                    description: "COORDINATOR_PORT_DESC".Localized()
                );
                SetDataInputPropertyAndBroadcast(
                    nameof(Character),
                    disabled: true,
                    description: "COORDINATOR_CHARACTER_AUTO_DESC".Localized()
                );
                isPortAndCharLocked = true;
            }

            // Auto-derive Character when coordinator is disabled
            if (!IsEnabled) {
                var chars = new[] { KeyboardReceiver?.Character, MouseReceiver?.Character, PenReceiver?.Character }
                    .Where(c => c != null)
                    .Distinct()
                    .ToList();

                CharacterAsset sharedChar = null;
                bool hasMismatch = false;

                if (chars.Count == 1) {
                    sharedChar = chars[0];
                } else if (chars.Count > 1) {
                    hasMismatch = true;
                }

                if (hasMismatch) {
                    if (Character != null) {
                        Character = null;
                        SetDataInput(nameof(Character), null, true);
                        BroadcastDataInput(nameof(Character));
                    }
                } else if (Character != sharedChar) {
                    Character = sharedChar;
                    SetDataInput(nameof(Character), sharedChar, true);
                    BroadcastDataInput(nameof(Character));
                }
            }

            // 1. Port Validation & Locking
            if (!IsEnabled) {
                if (MouseReceiver != null && PenReceiver != null) {
                    if (MouseReceiver.Port != PenReceiver.Port) {
                        SetEnablePropertyIfChanged(
                            disabled: true,
                            description: $"Pointer receivers have conflicting UDP ports (Mouse: {MouseReceiver.Port}, Pen: {PenReceiver.Port}). Both must be set to the same port."
                        );
                    } else {
                        if (Port != MouseReceiver.Port) {
                            Port = MouseReceiver.Port;
                            BroadcastDataInput(nameof(Port));
                        }
                        SetEnablePropertyIfChanged(
                            disabled: false,
                            description: "COORDINATOR_ENABLE_DESC".Localized()
                        );
                    }
                } else if (MouseReceiver != null) {
                    if (Port != MouseReceiver.Port) {
                        Port = MouseReceiver.Port;
                        BroadcastDataInput(nameof(Port));
                    }
                    SetEnablePropertyIfChanged(
                        disabled: false,
                        description: "COORDINATOR_ENABLE_DESC".Localized()
                    );
                } else if (PenReceiver != null) {
                    if (Port != PenReceiver.Port) {
                        Port = PenReceiver.Port;
                        BroadcastDataInput(nameof(Port));
                    }
                    SetEnablePropertyIfChanged(
                        disabled: false,
                        description: "COORDINATOR_ENABLE_DESC".Localized()
                    );
                } else {
                    SetEnablePropertyIfChanged(
                        disabled: false,
                        description: "COORDINATOR_ENABLE_DESC".Localized()
                    );
                }
            }

            // 2. EnableHands Preconditions
            // A. Same Character Check
            if (Character == null) {
                SetEnableHandsPropertyIfChanged(
                    disabled: true,
                    description: "COORDINATOR_CHAR_REQUIRED_DESC".Localized()
                );
                return;
            }

            bool kbMismatch = KeyboardReceiver != null && KeyboardReceiver.Character != Character;
            bool mouseMismatch = MouseReceiver != null && MouseReceiver.Character != Character;
            bool penMismatch = PenReceiver != null && PenReceiver.Character != Character;
            if (kbMismatch || mouseMismatch || penMismatch) {
                string mismatchedName = kbMismatch ? KeyboardReceiver.Name : (mouseMismatch ? MouseReceiver.Name : PenReceiver.Name);
                SetEnableHandsPropertyIfChanged(
                    disabled: true,
                    description: $"All registered receivers must be bound to {Character.Name}. '{mismatchedName}' is currently bound to another character or no character."
                );
                return;
            }

            // All checks passed
            SetEnableHandsPropertyIfChanged(
                disabled: false,
                description: "ENABLE_HANDS_DESC".Localized()
            );
        }

        public bool HasCharacterMismatch() {
            if (Character == null) return false;
            bool kbMismatch = KeyboardReceiver != null && KeyboardReceiver.Character != Character;
            bool mouseMismatch = MouseReceiver != null && MouseReceiver.Character != Character;
            bool penMismatch = PenReceiver != null && PenReceiver.Character != Character;
            return kbMismatch || mouseMismatch || penMismatch;
        }

        public bool IsPointerDeviceActive(PointerReceiverAsset ptr) {
            if (ptr == null || !IsEnabled || !EnableHands) return false;

            bool isMouse = (ptr == MouseReceiver);
            bool isPen = (ptr == PenReceiver);
            if (!isMouse && !isPen) return false;

            bool isRight = ptr.IsRightHanded;
            var state = isRight ? targetRightState : targetLeftState;

            if (isMouse && state == CoordinatorHandState.Mouse) return true;
            if (isPen && state == CoordinatorHandState.Pen) return true;
            return false;
        }

        public bool IsPointerInPenState(PointerReceiverAsset ptr) {
            if (!IsPointerDeviceActive(ptr)) return false;
            bool isRight = ptr.IsRightHanded;
            var state = isRight ? targetRightState : targetLeftState;
            return state == CoordinatorHandState.Pen;
        }

        public bool IsPointerInMouseState(PointerReceiverAsset ptr) {
            if (!IsPointerDeviceActive(ptr)) return false;
            bool isRight = ptr.IsRightHanded;
            var state = isRight ? targetRightState : targetLeftState;
            return state == CoordinatorHandState.Mouse;
        }

        private float lastDashboardUpdateTime;

        void UpdateDashboardStatus() {
            if (Time.time - lastDashboardUpdateTime < 0.1f) return;
            lastDashboardUpdateTime = Time.time;

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

            var animLayers = GetAnimationLayersOverviewMessage();
            if (AnimationLayersOverview != animLayers) {
                AnimationLayersOverview = animLayers;
                BroadcastDataInput(nameof(AnimationLayersOverview));
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
                case CoordinatorHandState.Released: return "STATE_RELEASED".Localized();
                case CoordinatorHandState.Keyboard: return "STATE_KEYBOARD".Localized();
                case CoordinatorHandState.Mouse: return "STATE_MOUSE".Localized();
                case CoordinatorHandState.Pen: return "STATE_PEN".Localized();
                default: return state.ToString();
            }
        }
    }
}
