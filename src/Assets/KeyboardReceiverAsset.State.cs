using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Warudo.Core;

namespace FlameStream {
    public partial class KeyboardReceiverAsset : ReceiverAsset {

        const int MIN_VK_CODE = 0;
        const int MAX_VK_CODE = 255;

        public BitArray KeyDownRegistry = new BitArray(256);
        public BitArray LastKeyDownRegistry = new BitArray(256);
        public BitArray DownDiffRegistry = new BitArray(256);
        public uint[] KeyPressTimestamp = new uint[256];
        uint currentFrameCounter = 1;

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
                    
                    bool wasDown = KeyDownRegistry[currentVkCode];
                    LastKeyDownRegistry[currentVkCode] = DownDiffRegistry[currentVkCode] = wasDown;
                    KeyDownRegistry[currentVkCode] = currentValue;

                    if (currentValue) {
                        AnyDown = true;
                        if (!wasDown) {
                            KeyPressTimestamp[currentVkCode] = ++currentFrameCounter;
                        }

                        if (!wasDown && ActiveKeyListeningTarget != null) {
                            var vk = (VKCode)currentVkCode;
                            var target = ActiveKeyListeningTarget;
                            ActiveKeyListeningTarget = null;
                            target.OnKeyCaptured(vk);
                            target.SetListeningForKey(false);
                            Context.Service?.Toast(
                                Warudo.Core.Server.ToastSeverity.Success,
                                Name,
                                $"Key assigned: [{vk}]"
                            );
                        }
                    } else {
                        KeyPressTimestamp[currentVkCode] = 0;
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

        /// <summary>
        /// Retrieves the most recently pressed key binding for the given hand.
        /// Up to 1 active key per hand (2 fingers max simultaneously across both hands).
        /// </summary>
        public KeyboardKeyBinding GetActiveKeyForHand(bool isLeftHand, KeyboardKeyBinding[] bindings) {
            if (bindings == null || bindings.Length == 0) return null;

            KeyboardKeyBinding newestBinding = null;
            uint highestTimestamp = 0;

            for (int i = 0; i < bindings.Length; i++) {
                var binding = bindings[i];
                if (binding == null) continue;

                if (binding.Finger.IsLeftHand() != isLeftHand) continue;

                int vk = (int)binding.Key;
                if (Down(vk)) {
                    uint ts = KeyPressTimestamp[vk];
                    if (ts >= highestTimestamp) {
                        highestTimestamp = ts;
                        newestBinding = binding;
                    }
                }
            }

            return newestBinding;
        }
    }
}
