namespace FlameStream
{
    public partial class KeyboardReceiverAsset : ReceiverAsset {

        protected override void OnCreate() {
            base.OnCreate();
        }

        public override void OnUpdate() {
            base.OnUpdate();

            OnUpdateState();
        }
    }
}
