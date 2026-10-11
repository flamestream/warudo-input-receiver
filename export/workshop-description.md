Input Receiver and Tools for Mouse, Pen, Keyboard, Gamepad, and Remote PC

===

Enhance Warudo with real-time input tracking and puppeteering tools tailored for VTuber artists, typists, and gamers!

⚠️ IMPORTANT REQUIREMENT
* Requires Warudo 0.15.1 or newer. (Legacy Warudo 0.14.x is no longer supported).

===

### 🌟 Key Features

* ⌨️ Keyboard Receiver & 3D Floating Keys
  - Real-time 256-key input tracking via companion emitter (tracks raw physical keys).
  - Procedural 3D Virtual Floating Keys with rounded borders and glow-on-press effects.
  - 8 Built-in layout presets: Full-Size 104, Compact 96%, TKL 80%, 75%, Keychron Q11 Split, 65%, 60%, and Ergonomic Southpaw.
  - Per-finger animation framework with automated key binding generator and natural hand reach scaling.
  - Demo finger animations available on Steam Workshop:
    https://steamcommunity.com/sharedfiles/filedetails/?id=3817573521

* 🎛️ Multi-Input Coordinator
  - Intelligently arbitrates hand IK target ownership between mouse/pen and keyboard.
  - Inactivity return-to-rest timers to keep hands natural when you stop typing.
  - Live inspector dashboard showing active input layers.

* 🎯 Pointer Receiver (Mouse & Pen Tracking)
  - Full visual setup for mouse and drawing tablet tracking (https://youtu.be/yxjbKF1MpVU).
  - Planar dynamic body and elbow rotation with speed limiting and jitter settling.
  - Customizable screen appearance for drawing screens.

* 🎮 Gamepad & Controller Tracking
  - Modern Xbox controllers via GameInput.
  - DirectInput and XInput support for flight sticks, steering wheels, pedals, and generic gamepads.
  - Device animation framework for props and fingers (https://youtu.be/jPY5_QvYEpI).

* 🧩 Extensive Blueprint Nodes
  - Low-level nodes to read live keyboard states, key down/up events, mouse/pen coordinates, and analog axes directly in Warudo node graphs.

===

### ⚙️ Requirements

1. Warudo: Version 0.15.1 or newer
2. Companion Background App (FS Input Device Emitter):
   Sends input signals over UDP to Warudo:
   https://github.com/flamestream/input-device-emitter/releases/latest

===

### 🔗 Links & Community

* 📖 Documentation & Setup Guides:
  https://github.com/flamestream/warudo-input-receiver/wiki

* 💾 Source Code & Release Notes:
  https://github.com/flamestream/warudo-input-receiver

* 💬 Official Warudo Discord Support Channel:
  https://discord.com/channels/1053915524280614983/1202095392041467964

