
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Warudo.Core.Attributes;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;
using Warudo.Core.Serializations;

namespace FlameStream {
    public abstract class ReceiverAsset : Asset {

        protected virtual int CURRENT_ASSET_VERSION => 1;

        [Hidden]
        [DataInput]
        public int AssetVersion = 1;

        protected long uiVersion = 0;

        public override long GetVersion() {
            return base.GetVersion() + uiVersion;
        }

        public async void RequestUiRefresh() {
            await UniTask.Yield();
            uiVersion++;
            Broadcast();
        }

        protected abstract ushort PROTOCOL_VERSION { get; }
        protected abstract string PROTOCOL_ID { get; }
        protected abstract int DEFAULT_PORT { get; }
        protected abstract string CHARACTER_ANIM_LAYER_ID_PREFIX { get; }

        UdpClient udpClient;
        Task listenTask;

        protected string lastState;
        public string LastRawState => lastState;
        public bool IsSocketListening => udpClient != null;

        [Markdown(0)]
        public string VersionUpdate = "";

        [DataInput(0)]
        [Label("ENABLE")]
        public bool IsEnabled;

        [Markdown(0)]
        public string Message = "";

        [DataInput(0)]
        [Label("PORT")]
        public int Port;

        public bool IsReceiving {
            get {
                return lastState != null;
            }
        }

        protected bool isDestroyed = false;

        protected override void OnCreate() {
            if (Port == 0) Port = DEFAULT_PORT;
            base.OnCreate();

            VersionUpdate = "FS_INPUT_RECEIVER_VERSION_UPDATE".Localized();
            Message = "RECEIVER_NOT_STARTED".Localized();

            Watch(nameof(IsEnabled), delegate { OnIsEnabledChange(); });
            Watch(nameof(Port), delegate { OnPortChange(); });

            if (Active) StartReceiver();
        }

        protected override void OnDestroy() {
            isDestroyed = true;
            StopReceiver();
            base.OnDestroy();
        }

        public override void OnUpdate() {
            BroadcastDataInput(nameof(Message));
            if (lastState == null) return;

            SetMessage("MSG_RECEIVING", skipLog: false);
        }

        /// <summary>
        /// Background task loop for listener
        /// </summary>
        /// <param name="udpClient">Client waiting for messages</param>
        protected void Listen(UdpClient client) {
            // Create an IPEndPoint object to store the sender's information
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            try {
                // Loop until the UdpClient object is closed or receiver is deactivated
                while (Active && client != null && client.Client != null)
                {
                    try {
                        // Receive data from the port and print it to the console
                        byte[] data = client.Receive(ref remoteEP);
                        if (data != null && data.Length > 0) {
                            lastState = Encoding.ASCII.GetString(data);
                        }

                    } catch (SocketException ex) {
                        if (Active) {
                            SetMessage(ex.Message);
                        } else {
                            break;
                        }
                    } catch (ObjectDisposedException) {
                        break;
                    } catch (NullReferenceException) {
                        // Mono's UdpClient.Receive throws NullReferenceException when Client is disposed/closed while blocking
                        break;
                    } catch (Exception) {
                        if (!Active) break;
                    }
                }
            } catch (Exception) {
                // Safely absorb any shutdown exceptions on the background listener thread
            }
        }

        protected virtual bool ShouldStartReceiver() {
            return true;
        }

        protected bool StartReceiver() {
            try {
                if (!ShouldStartReceiver()) {
                    SetActive(true);
                    SetMessage("MSG_WAITING");
                    return true;
                }

                Log($"Starting receiver on port {Port}");

                // Enable SO_REUSEADDR so port can be rebound immediately across scene changes
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.Bind(new IPEndPoint(IPAddress.Any, Port));

                udpClient = new UdpClient { Client = socket };
                listenTask = Task.Run(() => Listen(udpClient));

                SetActive(true);
                SetMessage("MSG_WAITING");
                return true;

            } catch (Exception ex) {
                SetMessage("MSG_START_FAILED");
                Log(ex.Message);
                StopReceiver();
            }

            return false;
        }

        protected void StopReceiver() {
            SetActive(false);

            var client = udpClient;
            udpClient = null;
            var task = listenTask;
            listenTask = null;

            if (client != null) {
                Log("Stopping receiver");
                try {
                    client.Close();
                } catch (Exception ex) {
                    Log($"Error closing UDP client: {ex.Message}");
                }
            }

            if (task != null) {
                try {
                    // Allow the background thread up to 500ms to exit cleanly
                    task.Wait(500);
                } catch (Exception) {
                    // Absorb AggregateException / task cancellation during teardown
                }
            }

            lastState = null;
        }

        protected void OnPortChange() {
            if (!Active) return;
            StopReceiver();
            StartReceiver();
        }

        protected void OnIsEnabledChange() {
            if (IsEnabled) {
                StopReceiver();
                StartReceiver();
            } else {
                StopReceiver();
            }
        }

        protected void SetMessage(string msg, bool skipLog = false) {
            if (skipLog) Log(msg);
            Message = msg.Localized();
        }

        /// <summary>
        /// Validates protocol version string against expected version and name.
        /// Supports both old numeric format (e.g., "2") and new number+letter format (e.g., "2G").
        /// If a letter is present, it will not fall back to numeric validation.
        /// </summary>
        /// <param name="inputString">The protocol version string from the incoming data</param>
        /// <param name="expectedVersion">The expected protocol version number</param>
        /// <param name="expectedName">The expected protocol ID letter</param>
        /// <returns>Error message if validation fails, null if valid</returns>
        protected string CheckValidProtocolVersion(string inputString, ushort expectedVersion, string expectedName) {
            // Early validation
            if (string.IsNullOrEmpty(inputString)) {
                return $"Invalid protocol format '{inputString}'. Expected '{expectedVersion}{expectedName}' or '{expectedVersion}' (legacy)";
            }

            var expectedNewFormat = $"{expectedVersion}{expectedName}";
            var expectedLegacyFormat = expectedVersion.ToString();
            bool hasLetter = inputString.Any(char.IsLetter);

            // Handle legacy format (numeric only) for backward compatibility
            if (!hasLetter) {
                if (!ushort.TryParse(inputString, out ushort numericProtocolVersion)) {
                    return $"Invalid protocol format '{inputString}'. Expected '{expectedLegacyFormat}' (legacy)";
                }

                if (numericProtocolVersion != expectedVersion) {
                    return $"Invalid protocol version '{inputString}'. Expected '{expectedLegacyFormat}' (legacy) but got version {numericProtocolVersion}. Please download the latest version of the emitter at https://github.com/flamestream/input-device-emitter/releases/latest";
                }

                return null; // Valid legacy format
            }

            // Handle new format (number + letter)
            if (inputString.Length < 2) {
                return $"Invalid protocol format '{inputString}'. Expected '{expectedNewFormat}' or '{expectedLegacyFormat}' (legacy)";
            }

            var protocolNumberPart = inputString.Substring(0, inputString.Length - 1);
            var protocolNamePart = inputString.Substring(inputString.Length - 1);

            if (!ushort.TryParse(protocolNumberPart, out ushort protocolVersionNumber)) {
                return $"Invalid protocol format '{inputString}'. Expected '{expectedNewFormat}' or '{expectedLegacyFormat}' (legacy)";
            }

            if (protocolVersionNumber != expectedVersion) {
                return $"Invalid protocol version '{inputString}'. Expected '{expectedNewFormat}' but got version {protocolVersionNumber}. Please download the latest version of the emitter at https://github.com/flamestream/input-device-emitter/releases/latest";
            }

            if (protocolNamePart != expectedName) {
                return $"Invalid protocol '{inputString}'. Expected '{expectedNewFormat}' but got name '{protocolNamePart}'. Please ensure that the emitter and receiver have matching ports";
            }

            return null; // Valid new format
        }

        public void SetDataInputPropertyAndBroadcast(
            string portName,
            bool? hidden = null,
            bool? alwaysHidden = null,
            bool? disabled = null,
            bool? alwaysDisabled = null,
            string description = null
        ) {
            var port = GetDataInputPort(portName);
            if (port?.Properties == null) return;

            if (hidden != null)
                port.Properties.hidden = hidden.Value;
            if (alwaysHidden != null)
                port.Properties.alwaysHidden = alwaysHidden.Value;
            if (disabled != null)
                port.Properties.disabled = disabled.Value;
            if (alwaysDisabled != null)
                port.Properties.alwaysDisabled = alwaysDisabled.Value;
            if (description != null)
                port.Properties.description = description;

            BroadcastDataInputProperties(portName);
        }


        abstract protected void Log(string msg);

        public override void Deserialize(SerializedAsset serialized) {
            int savedVersion = 0;
            if (serialized?.dataInputs != null && serialized.dataInputs.TryGetValue(nameof(AssetVersion), out var versionPort)) {
                if (!string.IsNullOrEmpty(versionPort?.value) && int.TryParse(versionPort.value, out int parsed)) {
                    savedVersion = parsed;
                }
            }

            if (savedVersion < CURRENT_ASSET_VERSION) {
                try {
                    OnPreMigrateAsset(savedVersion, CURRENT_ASSET_VERSION, serialized);
                } catch (Exception ex) {
                    Log($"Pre-migration exception from v{savedVersion} to v{CURRENT_ASSET_VERSION}: {ex}");
                }
            }

            base.Deserialize(serialized);

            if (savedVersion < CURRENT_ASSET_VERSION) {
                try {
                    OnPostMigrateAsset(savedVersion, CURRENT_ASSET_VERSION);
                } catch (Exception ex) {
                    Log($"Post-migration exception from v{savedVersion} to v{CURRENT_ASSET_VERSION}: {ex}");
                }
                AssetVersion = CURRENT_ASSET_VERSION;
                BroadcastDataInput(nameof(AssetVersion));
                Log($"Auto-migrated asset '{Name}' from v{savedVersion} to v{CURRENT_ASSET_VERSION}.");
            }
        }

        protected virtual void OnPreMigrateAsset(int fromVersion, int toVersion, SerializedAsset rawSerialized) { }

        protected virtual void OnPostMigrateAsset(int fromVersion, int toVersion) { }

        protected bool MigrateRenamePort(SerializedAsset rawSerialized, string oldPortName, string newPortName) {
            if (rawSerialized?.dataInputs == null) return false;
            if (rawSerialized.dataInputs.TryGetValue(oldPortName, out var port)) {
                rawSerialized.dataInputs.Remove(oldPortName);
                port.key = newPortName;
                rawSerialized.dataInputs[newPortName] = port;
                return true;
            }
            return false;
        }

        protected bool MigrateRemovePort(SerializedAsset rawSerialized, string portName) {
            if (rawSerialized?.dataInputs == null) return false;
            return rawSerialized.dataInputs.Remove(portName);
        }

    }
}
