
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Warudo.Core.Attributes;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;

namespace FlameStream {
    public abstract class ReceiverAsset : Asset {

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

        public bool IsReceiving {
            get {
                return lastState != null;
            }
        }

        protected override void OnCreate() {
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

        abstract protected void Log(string msg);
    }
}
