namespace FlameStream {
    public partial class PointerReceiverAsset : ReceiverAsset {

        public int X;
        public int Y;
        public int Source;
        public bool Button1;
        public bool Button2;
        public int LastSource;
        public bool LastButton1;
        public bool LastButton2;

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

            LastSource = Source;
            LastButton1 = Button1;
            LastButton2 = Button2;

            int.TryParse(parts[1], out X);
            int.TryParse(parts[2], out Y);
            int.TryParse(parts[3], out Source);
            Button1 = parts[4] == "1";
            Button2 = parts[5] == "1";
        }

        public bool ActivatedButton1() {
            return Button1 && !LastButton1;
        }

        public bool ActivatedButton2() {
            return Button2 && !LastButton2;
        }

        public bool DeactivatedButton1() {
            return !Button1 && LastButton1;
        }

        public bool DeactivatedButton2() {
            return !Button2 && LastButton2;
        }
    }
}
