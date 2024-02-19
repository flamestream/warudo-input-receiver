using Warudo.Core.Attributes;

namespace FlameStream {
    public partial class GamepadReceiverAsset : ReceiverAsset {
        

        [Section("Finger and Prop Animation")]
        [Markdown]
        public string AnimationInstructions = @"### Game Controller Prop Setup
* The prop must have an Animator component with multiple named Additive blending layers for each wanted buttons
* Map each button to the correct layer
### Finger Animation
* Finger animations files must be provided
* Two animations per button: hover and press
* Animations must have an additive reference pose";
    }
}