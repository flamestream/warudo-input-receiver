# FS Controller Toolbox

This toolbox provides Unity Editor utilities and sample assets for creating interactive controller props with finger animations for Warudo.

## Usage

To use this toolbox in your Warudo SDK project:

1. Copy the entire `FS Controller Toolbox` folder to your project's `Assets/` directory
2. Access the tools via the Unity menu: **FS Toolbox**
3. Reference the sample animations as templates for creating your own finger animation sets

For complete setup and workflow instructions, refer to the [Creating Your Own Controller Animations](../../wiki/Creating-Your-Own-Controller-Animations.md) guide.

## Contents

- **umotion-normalized-project.asset**: Pre-configured UMotion Pro project file optimized for humanoid finger animation workflows
- **Animation Clips/**: Sample finger animations for Nintendo Switch Pro Controller interactions

The included animation clips in `Animation Clips/` demonstrate a complete set of finger poses for controller interaction:

- **Base Poses**: Neutral holding positions (`SWPro Base (Hold Controller).anim`, `SWPro D Base.anim`)
- **Button Interactions**: Hover and press states for face buttons (A, B, X, Y) and D-pad directions (D1-D8)
- **Analog Inputs**: Stick movements in all axes (`SWPro Stick1/2 X+/-, Y+/-`)
- **Shoulder Controls**: Shoulder button and trigger press animations

> [!IMPORTANT]
> The provided animations are tailored for default-size female VRoid avatars. When creating animations for different character models, you may need to adjust finger positions and ranges to account for variations in hand size, proportions, and rig structure.

## File Structure

```
FS Controller Toolbox/
├── README.md                              # This file
├── umotion-normalized-project.asset       # UMotion project configuration
├── Animation Clips/                       # Sample finger animations
├── Editor/                                # Unity Editor scripts
│   ├── AnimationModBuilder.cs
│   └── ControllerPropSetup.cs
├── EditorSupport/                         # Templates and supporting files
│   └── AnimSettingsReader.template.txt
├── uMod Assets/                           # Generated mod asset folders
└── uMod Temp Settings/                    # Temporary build configuration
```
