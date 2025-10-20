using Warudo.Core.Attributes;

namespace FlameStream {
    public enum VKCode {
        // Adding a gap for future use (7)
        [Label("Backspace (8)")]
        BACK = 8,
        [Label("Tab (9)")]
        TAB = 9,
        // Adding a gap for future use (10, 11)
        [Label("Clear (12)")]
        CLEAR = 12,
        [Label("Enter (13)")]
        RETURN = 13,
        // Adding a gap for future use (14, 15)
        [Label("Shift (16) (Unused; use LSHIFT or RSHIFT)")]
        SHIFT = 16,
        [Label("Control (17) (Unused; use LCONTROL or RCONTROL)")]
        CONTROL = 17,
        [Label("Alt (18) (Unused; use LMENU or RMENU)")]
        MENU = 18,
        [Label("Pause (19)")]
        PAUSE = 19,
        [Label("Caps Lock (20)")]
        CAPITAL = 20,
        [Label("Kana (21)")]
        KANA = 21,
        [Label("Junja (23)")]
        JUNJA = 23,
        [Label("Final (24)")]
        FINAL = 24,
        [Label("Hanja (25)")]
        HANJA = 25,
        [Label("Escape (27)")]
        ESCAPE = 27,
        [Label("Convert (28)")]
        CONVERT = 28,
        [Label("Non-Convert (29)")]
        NONCONVERT = 29,
        [Label("Accept (30)")]
        ACCEPT = 30,
        [Label("Mode Change (31)")]
        MODECHANGE = 31,
        [Label("Space (32)")]
        SPACE = 32,
        [Label("Page Up (33)")]
        PRIOR = 33,
        [Label("Page Down (34)")]
        NEXT = 34,
        [Label("End (35)")]
        END = 35,
        [Label("Home (36)")]
        HOME = 36,
        [Label("Left Arrow (37)")]
        LEFT = 37,
        [Label("Up Arrow (38)")]
        UP = 38,
        [Label("Right Arrow (39)")]
        RIGHT = 39,
        [Label("Down Arrow (40)")]
        DOWN = 40,
        [Label("Select (41)")]
        SELECT = 41,
        [Label("Print (42)")]
        PRINT = 42,
        [Label("Execute (43)")]
        EXECUTE = 43,
        [Label("Print Screen (44)")]
        SNAPSHOT = 44,
        [Label("Insert (45)")]
        INSERT = 45,
        [Label("Delete (46)")]
        DELETE = 46,
        [Label("Help (47)")]
        HELP = 47,
        [Label("A Key (65)")]
        A = 65,
        [Label("B Key (66)")]
        B = 66,
        [Label("C Key (67)")]
        C = 67,
        [Label("D Key (68)")]
        D = 68,
        [Label("E Key (69)")]
        E = 69,
        [Label("F Key (70)")]
        F = 70,
        [Label("G Key (71)")]
        G = 71,
        [Label("H Key (72)")]
        H = 72,
        [Label("I Key (73)")]
        I = 73,
        [Label("J Key (74)")]
        J = 74,
        [Label("K Key (75)")]
        K = 75,
        [Label("L Key (76)")]
        L = 76,
        [Label("M Key (77)")]
        M = 77,
        [Label("N Key (78)")]
        N = 78,
        [Label("O Key (79)")]
        O = 79,
        [Label("P Key (80)")]
        P = 80,
        [Label("Q Key (81)")]
        Q = 81,
        [Label("R Key (82)")]
        R = 82,
        [Label("S Key (83)")]
        S = 83,
        [Label("T Key (84)")]
        T = 84,
        [Label("U Key (85)")]
        U = 85,
        [Label("V Key (86)")]
        V = 86,
        [Label("W Key (87)")]
        W = 87,
        [Label("X Key (88)")]
        X = 88,
        [Label("Y Key (89)")]
        Y = 89,
        [Label("Z Key (90)")]
        Z = 90,
        [Label("Left Windows (91)")]
        LWIN = 91,
        [Label("Right Windows (92)")]
        RWIN = 92,
        [Label("Applications (93)")]
        APPS = 93,
        [Label("Sleep (95)")]
        SLEEP = 95,
        [Label("Numpad 0 (96)")]
        NUMPAD0 = 96,
        [Label("Numpad 1 (97)")]
        NUMPAD1 = 97,
        [Label("Numpad 2 (98)")]
        NUMPAD2 = 98,
        [Label("Numpad 3 (99)")]
        NUMPAD3 = 99,
        [Label("Numpad 4 (100)")]
        NUMPAD4 = 100,
        [Label("Numpad 5 (101)")]
        NUMPAD5 = 101,
        [Label("Numpad 6 (102)")]
        NUMPAD6 = 102,
        [Label("Numpad 7 (103)")]
        NUMPAD7 = 103,
        [Label("Numpad 8 (104)")]
        NUMPAD8 = 104,
        [Label("Numpad 9 (105)")]
        NUMPAD9 = 105,
        [Label("Multiply (106)")]
        MULTIPLY = 106,
        [Label("Add (107)")]
        ADD = 107,
        [Label("Separator (108)")]
        SEPARATOR = 108,
        [Label("Subtract (109)")]
        SUBTRACT = 109,
        [Label("Decimal (110)")]
        DECIMAL = 110,
        [Label("Divide (111)")]
        DIVIDE = 111,
        [Label("F1 (112)")]
        F1 = 112,
        [Label("F2 (113)")]
        F2 = 113,
        [Label("F3 (114)")]
        F3 = 114,
        [Label("F4 (115)")]
        F4 = 115,
        [Label("F5 (116)")]
        F5 = 116,
        [Label("F6 (117)")]
        F6 = 117,
        [Label("F7 (118)")]
        F7 = 118,
        [Label("F8 (119)")]
        F8 = 119,
        [Label("F9 (120)")]
        F9 = 120,
        [Label("F10 (121)")]
        F10 = 121,
        [Label("F11 (122)")]
        F11 = 122,
        [Label("F12 (123)")]
        F12 = 123,
        [Label("F13 (124)")]
        F13 = 124,
        [Label("F14 (125)")]
        F14 = 125,
        [Label("F15 (126)")]
        F15 = 126,
        [Label("F16 (127)")]
        F16 = 127,
        [Label("F17 (128)")]
        F17 = 128,
        [Label("F18 (129)")]
        F18 = 129,
        [Label("F19 (130)")]
        F19 = 130,
        [Label("F20 (131)")]
        F20 = 131,
        [Label("F21 (132)")]
        F21 = 132,
        [Label("F22 (133)")]
        F22 = 133,
        [Label("F23 (134)")]
        F23 = 134,
        [Label("F24 (135)")]
        F24 = 135,
        [Label("Num Lock (144)")]
        NUMLOCK = 144,
        [Label("Scroll Lock (145)")]
        SCROLL = 145,
        [Label("OEM Specific (146)")]
        OEM_SPECIFIC_1 = 146,
        [Label("OEM Specific (147)")]
        OEM_SPECIFIC_2 = 147,
        [Label("OEM Specific (148)")]
        OEM_SPECIFIC_3 = 148,
        [Label("OEM Specific (149)")]
        OEM_SPECIFIC_4 = 149,
        [Label("OEM Specific (150)")]
        OEM_SPECIFIC_5 = 150,
        [Label("OEM Specific (151)")]
        OEM_SPECIFIC_6 = 151,
        [Label("OEM Specific (152)")]
        OEM_SPECIFIC_7 = 152,
        [Label("OEM Specific (153)")]
        OEM_SPECIFIC_8 = 153,
        [Label("OEM Specific (154)")]
        OEM_SPECIFIC_9 = 154,
        [Label("OEM Specific (155)")]
        OEM_SPECIFIC_10 = 155,
        [Label("OEM Specific (156)")]
        OEM_SPECIFIC_11 = 156,
        [Label("OEM Specific (157)")]
        OEM_SPECIFIC_12 = 157,
        [Label("OEM Specific (158)")]
        OEM_SPECIFIC_13 = 158,
        [Label("OEM Specific (159)")]
        OEM_SPECIFIC_14 = 159,
        [Label("Left Shift (160)")]
        LSHIFT = 160,
        [Label("Right Shift (161)")]
        RSHIFT = 161,
        [Label("Left Control (162)")]
        LCONTROL = 162,
        [Label("Right Control (163)")]
        RCONTROL = 163,
        [Label("Left Menu (164)")]
        LMENU = 164,
        [Label("Right Menu (165)")]
        RMENU = 165,
        [Label("Browser Back (166)")]
        BROWSER_BACK = 166,
        [Label("Browser Forward (167)")]
        BROWSER_FORWARD = 167,
        [Label("Browser Refresh (168)")]
        BROWSER_REFRESH = 168,
        [Label("Browser Stop (169)")]
        BROWSER_STOP = 169,
        [Label("Browser Search (170)")]
        BROWSER_SEARCH = 170,
        [Label("Browser Favorites (171)")]
        BROWSER_FAVORITES = 171,
        [Label("Browser Home (172)")]
        BROWSER_HOME = 172,
        [Label("Volume Mute (173)")]
        VOLUME_MUTE = 173,
        [Label("Volume Down (174)")]
        VOLUME_DOWN = 174,
        [Label("Volume Up (175)")]
        VOLUME_UP = 175,
        [Label("Media Next Track (176)")]
        MEDIA_NEXT_TRACK = 176,
        [Label("Media Previous Track (177)")]
        MEDIA_PREV_TRACK = 177,
        [Label("Media Stop (178)")]
        MEDIA_STOP = 178,
        [Label("Media Play/Pause (179)")]
        MEDIA_PLAY_PAUSE = 179,
        [Label("Launch Mail (180)")]
        LAUNCH_MAIL = 180,
        [Label("Launch Media Select (181)")]
        LAUNCH_MEDIA_SELECT = 181,
        [Label("Launch App 1 (182)")]
        LAUNCH_APP1 = 182,
        [Label("Launch App 2 (183)")]
        LAUNCH_APP2 = 183,
        [Label("OEM 1 (186)")]
        OEM_1 = 186,
        [Label("Plus (187)")]
        OEM_PLUS = 187,
        [Label("Comma (188)")]
        OEM_COMMA = 188,
        [Label("Minus (189)")]
        OEM_MINUS = 189,
        [Label("Period (190)")]
        OEM_PERIOD = 190,
        [Label("OEM 2 (191)")]
        OEM_2 = 191,
        [Label("OEM 3 (192)")]
        OEM_3 = 192,
        [Label("OEM 4 (219)")]
        OEM_4 = 219,
        [Label("OEM 5 (220)")]
        OEM_5 = 220,
        [Label("OEM 6 (221)")]
        OEM_6 = 221,
        [Label("OEM 7 (222)")]
        OEM_7 = 222,
        [Label("OEM 8 (223)")]
        OEM_8 = 223,
        [Label("OEM 102 (226)")]
        OEM_102 = 226,
        [Label("Process Key (229)")]
        PROCESSKEY = 229,
        [Label("Packet (231)")]
        PACKET = 231,
        [Label("Attn (246)")]
        ATTN = 246,
        [Label("CrSel (247)")]
        CRSEL = 247,
        [Label("ExSel (248)")]
        EXSEL = 248,
        [Label("Erase EOF (249)")]
        EREOF = 249,
        [Label("Play (250)")]
        PLAY = 250,
        [Label("Zoom (251)")]
        ZOOM = 251,
        [Label("PA1 (253)")]
        PA1 = 253,
        [Label("OEM Clear (254)")]
        OEM_CLEAR = 254
    }
}
