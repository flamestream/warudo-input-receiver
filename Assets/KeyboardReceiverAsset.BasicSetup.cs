namespace FlameStream
{
    public partial class KeyboardReceiverAsset : ReceiverAsset {

        protected override void OnCreate() {
            if (Port == 0) Port = DEFAULT_PORT;
            base.OnCreate();
        }

        public override void OnUpdate() {
            base.OnUpdate();

            OnUpdateState();
        }
    }
}
