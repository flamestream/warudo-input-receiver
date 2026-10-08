using Warudo.Core.Attributes;

namespace FlameStream
{
    public enum VirtualKeyAnchor
    {
        [Label("VIRTUAL_KEY_ANCHOR_LEFT_HAND")]
        LeftHand,
        [Label("VIRTUAL_KEY_ANCHOR_RIGHT_HAND")]
        RightHand,
        [Label("VIRTUAL_KEY_ANCHOR_CENTER")]
        KeyboardRoot
    }

    public enum VirtualKeyLayoutPreset
    {
        [Label("VIRTUAL_KEY_PRESET_FULL_SIZE_100")]
        FullSize100 = 0,
        [Label("VIRTUAL_KEY_PRESET_COMPACT_FULL_SIZE_96")]
        CompactFullSize96,
        [Label("VIRTUAL_KEY_PRESET_TENKEYLESS_80")]
        Tenkeyless80,
        [Label("VIRTUAL_KEY_PRESET_COMPACT_75")]
        Compact75,
        [Label("VIRTUAL_KEY_PRESET_COMPACT_65")]
        Compact65,
        [Label("VIRTUAL_KEY_PRESET_COMPACT_60")]
        Compact60,
        [Label("VIRTUAL_KEY_PRESET_KEYCHRON_Q11")]
        KeychronQ11
    }
}
