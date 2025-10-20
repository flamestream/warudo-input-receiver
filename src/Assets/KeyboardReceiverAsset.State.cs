using System.Collections;
using Warudo.Core;

namespace FlameStream {
    public partial class KeyboardReceiverAsset : ReceiverAsset {

        const int MIN_VK_CODE = 0;
        const int MAX_VK_CODE = 255;

        public BitArray KeyDownRegistry = new BitArray(256);
        public BitArray LastKeyDownRegistry = new BitArray(256);
        public BitArray DownDiffRegistry = new BitArray(256);

        public bool AnyDown;

        public int LastVkCode;

        void OnUpdateState() {
            if (lastState == null) return;

            var parts = lastState.Split(';');
            var protocolVersionString = parts[0];

            // Validate protocol version using helper function
            var protocolError = CheckValidProtocolVersion(protocolVersionString, PROTOCOL_VERSION, PROTOCOL_ID);
            if (protocolError != null) {
                StopReceiver();
                SetMessage(protocolError);
                return;
            }

            AnyDown = false;

            for (int i = 1; i < parts.Length; i++) {
                var part = parts[i];
                for (int j = 0; j < 64; j++) {
                    var currentValue = part[j] == '1';
                    var currentVkCode = (i - 1) * 64 + (63 - j);
                    LastKeyDownRegistry[currentVkCode] = DownDiffRegistry[currentVkCode] = KeyDownRegistry[currentVkCode];
                    KeyDownRegistry[currentVkCode] = currentValue;
                    if (currentValue) {
                        AnyDown = true;

                        if (IsWaitingForButtonPress) {
                            Context.Service?.Toast(
                                Warudo.Core.Server.ToastSeverity.Info,
                                Name,
                                $"Button identified with code [{currentVkCode}]"
                            );
                            TriggerCancelIdentifyButton();
                        }
                    }
                }
            }

            // Compute last pressed key
            DownDiffRegistry.Xor(KeyDownRegistry).And(KeyDownRegistry);
            for (int i = 0; i < DownDiffRegistry.Length; ++i) {
                if (DownDiffRegistry[i]) {
                    LastVkCode = i;
                    break;
                }
            }
        }

        public bool Down(int vkCode) {
            if (vkCode < MIN_VK_CODE || vkCode > MAX_VK_CODE) {
                return false;
            }
            return KeyDownRegistry[vkCode];
        }

        public bool Up(int vkCode) {
            if (vkCode < MIN_VK_CODE || vkCode > MAX_VK_CODE) {
                return false;
            }
            return !KeyDownRegistry[vkCode];
        }

        public bool Activated(int vkCode) {
            if (vkCode < MIN_VK_CODE || vkCode > MAX_VK_CODE) {
                return false;
            }
            return KeyDownRegistry[vkCode] && !LastKeyDownRegistry[vkCode];
        }

        public bool Deactivated(int vkCode) {
            if (vkCode < MIN_VK_CODE || vkCode > MAX_VK_CODE) {
                return false;
            }
            return !KeyDownRegistry[vkCode] && LastKeyDownRegistry[vkCode];
        }
    }
}
