## Warudo Plugin: Input Device Receiver and Tools for Pen, Keyboard, and Game Controller

Enhance Warudo with abilities around device inputs, tailored for artists, typists, and gamers.
Available from the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3279857531).

### Features

* **Keyboard Tracking & Floating Keys**: Real-time 256-key input tracking, 3D floating keys with rounded borders and glow-on-press effects, 8 layout presets, per-finger reaching animations, and auto-binding generator.
* **Multi-Input Coordination**: Intelligent hand IK arbitration between mouse/pen, keyboard, and controller with inactivity return-to-rest timers.
* **Pen & Mouse Tracking**: Pen detection support (Wacom Intuos Pro), planar body/elbow dynamic rotation, and full visual setup for mouse/pen tracking.
* **Modern Game Controller Support**: GameInput (modern Xbox controllers), DirectInput, and XInput for wheels, pedals, flight sticks, and gamepads.
* **Fingers & Props**: Finger animations and prop button/stick animations tied to controller/keyboard input.
* **Blueprint Nodes**: Extensive low-level nodes to read mouse, pen, keyboard, and game controller input directly in Warudo node graphs.

### Requirements

1. **Warudo Version**: **Warudo 0.15.1 or newer** is required.
2. **Input Emitter**: Requires the companion background process that sends input signals over UDP to Warudo:
   https://github.com/flamestream/input-device-emitter/releases/latest

