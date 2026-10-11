using System;
using Cysharp.Threading.Tasks;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;
using Warudo.Core.Serializations;

namespace FlameStream
{
    public class KeyboardKeyInput : StructuredData<KeyboardReceiverAsset>, IKeyListeningTarget
    {
        private long uiVersion = 0;
        public override long GetVersion() => base.GetVersion() + uiVersion;

        public override SerializedStructuredData Serialize() {
            var serialized = base.Serialize();
            serialized.version = GetVersion();
            return serialized;
        }

        public async void RequestUiRefresh() {
            await UniTask.Yield();
            uiVersion++;
            Broadcast();
        }

        [DataInput(10)]
        [Label("KEY")]
        public VKCode Key = default(VKCode);

        [Trigger(15)]
        [Label("INPUT_KEY")]
        [Description("INPUT_KEY_DESC")]
        public void TriggerInputKey() {
            Parent?.ToggleKeyListening(this);
        }

        public bool IsListeningForKey { get; private set; }

        public Action<VKCode> OnKeyChanged;
        public Action<bool> OnListeningChanged;

        public void SetListeningForKey(bool listening) {
            IsListeningForKey = listening;
            var port = GetTriggerPort(nameof(TriggerInputKey));
            if (port != null && port.Properties != null) {
                port.Properties.label = listening ? "INPUTTING_KEY".Localized() : "INPUT_KEY".Localized();
                BroadcastTriggerProperties(nameof(TriggerInputKey));
                RequestUiRefresh();
            }
            OnListeningChanged?.Invoke(listening);
        }

        public void OnKeyCaptured(VKCode newKey) {
            Key = newKey;
            SetDataInput(nameof(Key), newKey, true);
            BroadcastDataInput(nameof(Key));
            OnKeyChanged?.Invoke(newKey);
            RequestUiRefresh();
        }

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Key), delegate {
                OnKeyChanged?.Invoke(Key);
            });
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            if (Parent != null && Parent.ActiveKeyListeningTarget == (IKeyListeningTarget)this) {
                Parent.CancelKeyListening();
            }
        }
    }
}
