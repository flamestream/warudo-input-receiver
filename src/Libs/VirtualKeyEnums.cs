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
        [Label("VIRTUAL_KEY_PRESET_STANDARD_QWERTY_FULL")]
        StandardQWERTYFull = 9,
        [Label("VIRTUAL_KEY_PRESET_SPLIT_QWERTY")]
        SplitQWERTY = 0,
        [Label("VIRTUAL_KEY_PRESET_GAMING_WASD")]
        GamingWASD,
        [Label("VIRTUAL_KEY_PRESET_GAMING_WASD_EXTENDED")]
        GamingWASDExtended,
        [Label("VIRTUAL_KEY_PRESET_RHYTHM_4KEY")]
        Rhythm4Key,
        [Label("VIRTUAL_KEY_PRESET_RHYTHM_7KEY")]
        Rhythm7Key,
        [Label("VIRTUAL_KEY_PRESET_LEVERLESS_ARCADE")]
        LeverlessArcade,
        [Label("VIRTUAL_KEY_PRESET_NUMPAD")]
        Numpad,
        [Label("VIRTUAL_KEY_PRESET_STANDARD_60PERCENT")]
        Standard60Percent,
        [Label("VIRTUAL_KEY_PRESET_KEYCHRON_Q11")]
        KeychronQ11
    }
}
