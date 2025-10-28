
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Warudo.Core.Attributes;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;

namespace FlameStream {
    public abstract class ReceiverAsset : Asset {

        protected abstract ushort PROTOCOL_VERSION { get; }
        protected abstract string PROTOCOL_ID { get; }
        protected abstract int DEFAULT_PORT { get; }
        protected abstract string CHARACTER_ANIM_LAYER_ID_PREFIX { get; }

        UdpClient udpClient;
        Task listenTask;

        protected string lastState;

        [Markdown(0)]
        public string VersionUpdate = "FS_INPUT_RECEIVER_VERSION_UPDATE".Localized();

        [DataInput(0)]
        [Label("ENABLE")]
        public bool IsEnabled;

        [Markdown(0)]
        public string Message = "RECEIVER_NOT_STARTED".Localized();

        [DataInput(0)]
        [Label("PORT")]
        public int Port;

        [Section("ADVANCED")]
        [DataInput(1000)]
        [Label("RECEIVER_ASSET_DEBUG_TOAST_WANTED")]
        public bool IsDebugToastWanted;

        public bool IsReceiving {
            get {
                return lastState != null;
            }
        }

        protected override void OnCreate() {
            if (Port == 0) Port = DEFAULT_PORT;
            base.OnCreate();

            Watch(nameof(IsEnabled), delegate { OnIsEnabledChange(); });
            Watch(nameof(Port), delegate { OnPortChange(); });

            if (Active) StartReceiver();
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            StopReceiver();
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
        protected void Listen(UdpClient udpClient) {
            // Create an IPEndPoint object to store the sender's information
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            try {
                // Loop until the UdpClient object is closed
                while (udpClient != null && udpClient.Client != null)
                {
                    try {
                        // Receive data from the port and print it to the console
                        byte[] data = udpClient.Receive(ref remoteEP);
                        lastState = Encoding.ASCII.GetString(data);

                    } catch (SocketException ex) {
                        if (Active) {
                            SetMessage(ex.Message);
                        } else {
                            SetMessage("MSG_STOPPED_LISTENING");
                        }
                    }
                }
            } catch (ObjectDisposedException) {
                // This will happen, since the client will be closed as we wait for thread to die.
            }
        }

        protected bool StartReceiver() {
            try {
                Log($"Starting receiver on port {Port}");
                udpClient = new UdpClient(Port);
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

            if (udpClient != null) {
                Log("Stopping receiver");
                udpClient.Close();
                udpClient = null;
                listenTask.Wait();
                listenTask = null;
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

        protected void SetDataInputPropertyAndBroadcast(
            string portName,
            bool? hidden = null,
            bool? alwaysHidden = null,
            bool? disabled = null,
            bool? alwaysDisabled = null,
            string description = null
        ) {
            if (hidden != null)
                GetDataInputPort(portName).Properties.hidden = hidden.Value;
            if (alwaysHidden != null)
                GetDataInputPort(portName).Properties.alwaysHidden = alwaysHidden.Value;
            if (disabled != null)
                GetDataInputPort(portName).Properties.disabled = disabled.Value;
            if (alwaysDisabled != null)
                GetDataInputPort(portName).Properties.alwaysDisabled = alwaysDisabled.Value;
            if (description != null)
                GetDataInputPort(portName).Properties.description = description;

            BroadcastDataInputProperties(portName);
        }

        protected void ShowToast(
            string message,
            Warudo.Core.Server.ToastSeverity severity = Warudo.Core.Server.ToastSeverity.Info
        ) {
            if (!IsDebugToastWanted) return;
            Warudo.Core.Context.Service.Toast(severity, Name, message);
        }

        abstract protected void Log(string msg);

    }
}
