namespace FlameStream
{
    public interface IKeyListeningTarget
    {
        bool IsListeningForKey { get; }
        void SetListeningForKey(bool listening);
        void OnKeyCaptured(VKCode key);
    }
}
