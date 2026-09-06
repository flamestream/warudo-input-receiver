using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Warudo.Core;
using Warudo.Core.Localization;

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

        public readonly HashSet<VKCode> DebugForcedKeys = new HashSet<VKCode>();
        public readonly Dictionary<KeyboardFinger, VKCode> DebugFingerToKey = new Dictionary<KeyboardFinger, VKCode>();

        public bool IsDebugKeyModeActive => DebugForcedKeys.Count > 0;

        public bool IsKeyDebugForced(VKCode key) => DebugForcedKeys.Contains(key);

        void OnUpdateState() {
            if (IsDebugKeyModeActive) {
                // Ignore all incoming network input signals.
                // KeyDownRegistry is driven exclusively by DebugForcedKeys.
                AnyDown = DebugForcedKeys.Count > 0;

                for (int currentVkCode = 0; currentVkCode < 256; currentVkCode++) {
                    bool wasDown = KeyDownRegistry[currentVkCode];
                    bool currentValue = DebugForcedKeys.Contains((VKCode)currentVkCode);

                    LastKeyDownRegistry[currentVkCode] = DownDiffRegistry[currentVkCode] = wasDown;
                    KeyDownRegistry[currentVkCode] = currentValue;

                    if (currentValue) {
                        if (!wasDown) {
                            KeyPressTimestamp[currentVkCode] = ++currentFrameCounter;
                        }
                    } else {
                        KeyPressTimestamp[currentVkCode] = 0;
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
                return;
            }

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

        public void ToggleDebugKey(VKCode key, KeyboardFinger? finger = null) {
            if (DebugForcedKeys.Contains(key)) {
                // Deactivate this key
                DebugForcedKeys.Remove(key);
                if (finger.HasValue && DebugFingerToKey.TryGetValue(finger.Value, out var k) && k == key) {
                    DebugFingerToKey.Remove(finger.Value);
                } else {
                    var fingerEntry = DebugFingerToKey.FirstOrDefault(kvp => kvp.Value == key);
                    if (fingerEntry.Value == key) {
                        DebugFingerToKey.Remove(fingerEntry.Key);
                    }
                }
            } else {
                // If this finger already has an active key, release the previous key on this finger
                if (finger.HasValue && DebugFingerToKey.TryGetValue(finger.Value, out var existingKey)) {
                    DebugForcedKeys.Remove(existingKey);
                    DebugFingerToKey.Remove(finger.Value);
                }

                // Check 10-key maximum
                if (DebugForcedKeys.Count >= 10) {
                    Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Warning, Name, "DEBUG_MAX_KEYS_REACHED".Localized());
                    return;
                }

                DebugForcedKeys.Add(key);
                if (finger.HasValue) {
                    DebugFingerToKey[finger.Value] = key;
                }
            }

            UpdateAllDebugButtonStates();
            Context.Service?.BroadcastOpenedScene();
        }

        public void ToggleDebugVirtualKey(VirtualKeyDefinition keyDef) {
            if (keyDef == null) return;

            KeyboardFinger? finger = null;
            if (KeyBindings != null) {
                var binding = KeyBindings.FirstOrDefault(b => b != null && b.Key == keyDef.Key);
                if (binding != null) {
                    finger = binding.Finger;
                }
            }

            ToggleDebugKey(keyDef.Key, finger);
        }

        public void DeactivateAllDebugKeys() {
            if (DebugForcedKeys.Count == 0) return;

            DebugForcedKeys.Clear();
            DebugFingerToKey.Clear();

            KeyDownRegistry.SetAll(false);
            LastKeyDownRegistry.SetAll(false);
            DownDiffRegistry.SetAll(false);
            Array.Clear(KeyPressTimestamp, 0, KeyPressTimestamp.Length);
            AnyDown = false;

            UpdateAllDebugButtonStates();
            Context.Service?.BroadcastOpenedScene();
            Context.Service?.Toast(Warudo.Core.Server.ToastSeverity.Info, Name, "DEBUG_RELEASED_ALL_KEYS".Localized());
        }
    }
}
