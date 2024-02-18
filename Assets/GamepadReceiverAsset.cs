
using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Graphs;
using Warudo.Core.Localization;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets;
using Warudo.Plugins.Core.Assets.Character;
using Warudo.Plugins.Core.Assets.Prop;
using Warudo.Plugins.Core.Assets.Utility;
using Warudo.Plugins.Core.Nodes;
using Warudo.Plugins.Core.Nodes.Event;

namespace FlameStream {
    [AssetType(
        Id = "FlameStream.Asset.GamepadReceiver",
        Title = "ASSET_TITLE_GAMEPAD"
    )]
    public class GamepadReceiverAsset : ReceiverAsset {

        public const float DEAD_ZONE_RADIUS = 0.1f;
        const ushort PROTOCOL_VERSION = 2;
        const int DEFAULT_PORT = 40611;

        public uint ButtonFlags;
        public ushort LX;
        public ushort LY;
        public ushort LZ;
        public ushort LrX;
        public ushort LrY;
        public ushort LrZ;
        public byte Pad;

        public uint LastButtonFlags;
        public uint ActivatedButtonFlags;

        public ushort LastLX;
        public ushort LastLY;
        public ushort LastLZ;
        public ushort LastLrX;
        public ushort LastLrY;
        public ushort LastLrZ;
        public byte LastPad;

        protected override void OnCreate() {

            if (Port == 0) Port = DEFAULT_PORT;

            base.OnCreate();
        }

        public override void OnUpdate() {
            base.OnUpdate();

            PerformButtonScanLoop();
            PerformPropMovement();
        }

        void PerformButtonScanLoop() {
            if (lastState == null) return;

            var parts = lastState.Split(';');
            byte.TryParse(parts[0], out byte protocolVersion);
            if (protocolVersion != PROTOCOL_VERSION) {
                StopReceiver();
                SetMessage($"Invalid gamepad protocol '{protocolVersion}'. Expected '{PROTOCOL_VERSION}'\n\nPlease download compatible version of emitter at https://github.com/flamestream/input-device-emitter/releases");
                return;
            }

            ActivatedButtonFlags = (ButtonFlags ^ LastButtonFlags) & ButtonFlags;
            LastButtonFlags = ButtonFlags;

            LastLX = LX;
            LastLY = LY;
            LastLZ = LZ;
            LastLrX = LrX;
            LastLrY = LrY;
            LastLrZ = LrZ;
            LastPad = Pad;

            uint.TryParse(parts[1], out ButtonFlags);
            ushort.TryParse(parts[2], out LX);
            ushort.TryParse(parts[3], out LY);
            ushort.TryParse(parts[4], out LZ);
            ushort.TryParse(parts[5], out LrX);
            ushort.TryParse(parts[6], out LrY);
            ushort.TryParse(parts[7], out LrZ);
            byte.TryParse(parts[8], out Pad);
        }

        public bool ButtonFlag(int idx) {
            return (ButtonFlags >> idx & 1) == 1;
        }

        public bool ButtonFlag(SwitchProButton val) {
            return ButtonFlag((int) val - 1);
        }

        public bool LastButtonFlag(int idx) {
            return (LastButtonFlags >> idx & 1) == 1;
        }

        public bool ActivatedButtonFlag(int idx) {
            return (ActivatedButtonFlags >> idx & 1) == 1;
        }
        public bool ActivatedButtonFlag(SwitchProButton val) {
            return ActivatedButtonFlag((int) val - 1);
        }

        public bool JustDownButtonFlag(int idx) {
            return ButtonFlag(idx) && !LastButtonFlag(idx);
        }

        public bool JustUpButtonFlag(int idx) {
            return !ButtonFlag(idx) && LastButtonFlag(idx);
        }

        protected override void Log(string msg) {
            UnityEngine.Debug.Log($"[FlameStream.Asset.GamepadReceiver] {msg}");
        }

        [Section("General Configuration")]

        [DataInput]
        [Label("Game Controller Type")]
        public ControllerType TargetControllerType;

        [DataInput]
        [Label("Default Controller Hand")]
        public GamepadAnchorSide DefaultControllerAnchorSide;

        public enum GamepadAnchorSide {
            LeftHand,
            RightHand
        }

        [Section("Basic Prop Setup")]

        [Markdown]
        [HiddenIf(nameof(IsSetupComplete))]
        public String BasicPropSetupInstructions = @"### Instructions

Make target controller hold controller in wanted neutral position, then set up anchors.";

        [DataInput]
        [DisabledIf(nameof(IsSetupComplete))]
        public CharacterAsset Character;

        [DataInput]
        [DisabledIf(nameof(IsSetupComplete))]
        [Label("Game Controller")]
        public PropAsset Gamepad;

        [DataInput]
        [Hidden]
        Guid GamepadAnchorAssetId;
        [DataInput]
        [Hidden]
        Guid GamepadLeftHandAnchorAssetId;
        [DataInput]
        [Hidden]
        Guid GamepadRightHandAnchorAssetId;

        public AnchorAsset GamepadAnchor {
            get {
                if (GamepadAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == GamepadAnchorAssetId);
            }
        }

        public AnchorAsset GamepadLeftHandAnchor {
            get {
                if (GamepadLeftHandAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == GamepadLeftHandAnchorAssetId);
            }
        }

        public AnchorAsset GamepadRightHandAnchor {
            get {
                if (GamepadRightHandAnchorAssetId == Guid.Empty) return null;
                return Scene.GetAssets<AnchorAsset>().FirstOrDefault(p => p.Id == GamepadRightHandAnchorAssetId);
            }
        }

        [DataInput]
        [Hidden]
        public Vector3 GamepadPosition;
        [DataInput]
        [Hidden]
        public Vector3 GamepadRotation;
        [DataInput]
        [Hidden]
        public Vector3 GamepadAnchorPosition;
        [DataInput]
        [Hidden]
        public Vector3 GamepadAnchorRotation;
        [DataInput]
        [Hidden]
        public Vector3 GamepadLeftHandPosition;
        [DataInput]
        [Hidden]
        public Vector3 GamepadLeftHandRotation;
        [DataInput]
        [Hidden]
        public Vector3 GamepadRightHandPosition;
        [DataInput]
        [Hidden]
        public Vector3 GamepadRightHandRotation;
        [DataInput]
        [Hidden]
        public Vector3 LeftHandAnchorPosition;
        [DataInput]
        [Hidden]
        public Vector3 LeftHandAnchorRotation;
        [DataInput]
        [Hidden]
        public Vector3 RightHandAnchorPosition;
        [DataInput]
        [Hidden]
        public Vector3 RightHandAnchorRotation;


        bool IsSetupIncomplete() {
            return GamepadAnchor == null;
        }
        bool IsSetupComplete() {
            return !IsSetupIncomplete();
        }

        bool IsMissingInput() {
            return Gamepad == null || Character == null;
        }

        [Trigger]
        [DisabledIf(nameof(IsMissingInput))]
        [HiddenIf(nameof(IsSetupComplete))]
        public void SetupGamepadAnchors() {

            AnchorAsset gamepadAnchor = Scene.AddAsset<AnchorAsset>();
            gamepadAnchor.Name = "⚓-🔥🎮";
            Scene.UpdateNewAssetName(gamepadAnchor);
            GamepadAnchorAssetId = gamepadAnchor.Id;

            var anchorTransform = gamepadAnchor.Transform;
            var gamepadTransform = Gamepad.Transform;
            anchorTransform.Position = gamepadTransform.Position;

            SetParent(Gamepad, gamepadAnchor);
            SetParent(gamepadAnchor, Character);

            GamepadPosition = Gamepad.Transform.Position;
            GamepadRotation = Gamepad.Transform.Rotation;

            GamepadAnchorPosition = gamepadAnchor.Transform.Position;
            GamepadAnchorRotation = gamepadAnchor.Transform.Rotation;

            var leftAnchor = SetupIKTargetHandAnchor(
                "⚓-🔥🎮🫲",
                HumanBodyBones.LeftHand,
                Character.LeftHandIK,
                gamepadAnchor,
                ref GamepadLeftHandAnchorAssetId,
                ref LeftHandAnchorPosition,
                ref LeftHandAnchorRotation
            );
            var rightAnchor = SetupIKTargetHandAnchor(
                "⚓-🔥🎮🫱",
                HumanBodyBones.RightHand,
                Character.RightHandIK,
                gamepadAnchor,
                ref GamepadRightHandAnchorAssetId,
                ref RightHandAnchorPosition,
                ref RightHandAnchorRotation
            );

            Gamepad.Transform.Position = GamepadPosition;
            Gamepad.Transform.Rotation = GamepadRotation;
            SetParent(Gamepad, leftAnchor);
            GamepadLeftHandPosition = Gamepad.Transform.Position;
            GamepadLeftHandRotation = Gamepad.Transform.Rotation;

            Gamepad.Transform.Position = GamepadPosition;
            Gamepad.Transform.Rotation = GamepadRotation;
            SetParent(Gamepad, rightAnchor);
            GamepadRightHandPosition = Gamepad.Transform.Position;
            GamepadRightHandRotation = Gamepad.Transform.Rotation;

            AttachGamepad(DefaultControllerAnchorSide);

            gamepadAnchor.Broadcast();
            leftAnchor.Broadcast();
            rightAnchor.Broadcast();
            Gamepad.Broadcast();
        }

        public void AttachGamepad(GamepadAnchorSide side = GamepadAnchorSide.LeftHand) {

            var position = GamepadLeftHandPosition;
            var rotation = GamepadLeftHandRotation;
            HumanBodyBones bone = HumanBodyBones.LeftHand;
            if (side == GamepadAnchorSide.RightHand) {
                position = GamepadRightHandPosition;
                rotation = GamepadRightHandRotation;
                bone = HumanBodyBones.RightHand;
            }

            var transform = Gamepad.Transform;
            transform.Position = position;
            transform.Rotation = rotation;

            Gamepad.Attachable.Parent = Character;
            Gamepad.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.HumanBodyBone;
            Gamepad.Attachable.AttachToBone = bone;
        }

        [Trigger]
        [Label("🔁 Reset All Anchors")]
        [HiddenIf(nameof(IsSetupIncomplete))]
        public void ResetAllAnchors() {
            Gamepad.Transform.Position = GamepadRightHandPosition;
            Gamepad.Transform.Rotation = GamepadRightHandRotation;
            AttachGamepad(DefaultControllerAnchorSide);

            GamepadAnchor.Transform.Position = GamepadAnchorPosition;
            GamepadAnchor.Transform.Rotation = GamepadAnchorRotation;
            GamepadLeftHandAnchor.Transform.Position = LeftHandAnchorPosition;
            GamepadLeftHandAnchor.Transform.Rotation = LeftHandAnchorRotation;
            GamepadRightHandAnchor.Transform.Position = RightHandAnchorPosition;
            GamepadRightHandAnchor.Transform.Rotation = RightHandAnchorRotation;

            Gamepad.Broadcast();
            GamepadAnchor.Broadcast();
            GamepadLeftHandAnchor.Broadcast();
            GamepadRightHandAnchor.Broadcast();
        }

        [Trigger]
        [HiddenIf(nameof(IsSetupIncomplete))]
        [Label("💣 Clear All Anchors")]
        public void ClearAllAnchors() {
            UnsetParent(Gamepad);
            CleanDestroy(GamepadAnchor);
            CleanDestroy(GamepadLeftHandAnchor);
            CleanDestroy(GamepadRightHandAnchor);
        }

        AnchorAsset SetupIKTargetHandAnchor(
            string name,
            HumanBodyBones targetBone,
            CharacterAsset.LimbIKData limb,
            AnchorAsset parent,
            ref Guid anchorAssetId,
            ref Vector3 handPosition,
            ref Vector3 handRotation
        ) {
			AnchorAsset anchor = Scene.AddAsset<AnchorAsset>();
			anchor.Name = name;
			Scene.UpdateNewAssetName(anchor);
			anchor.Transform.CopyFromWorldTransform(Character.Animator.GetBoneTransform(targetBone));
			anchor.Transform.ApplyAsWorldTransform(anchor.GameObject.transform);

            anchorAssetId = anchor.Id;

            limb.Enabled = true;
            limb.IkTarget = anchor;
            limb.PositionWeight = 1.0f;
            limb.RotationWeight = 1.0f;

            SetParent(anchor, parent);

            handPosition = anchor.Transform.Position;
            handRotation = anchor.Transform.Rotation;

			anchor.Broadcast();
            limb.Broadcast();

            return anchor;
		}

        void SetParent(AnchorAsset child, AnchorAsset parent) {
            _SetParent(child, parent);
            child.Attachable.Parent = parent;
        }

        void SetParent(PropAsset child, AnchorAsset parent) {
            _SetParent(child, parent);
            child.Attachable.Parent = parent;
        }

        void SetParent(AnchorAsset child, CharacterAsset parent) {
            _SetParent(child, parent);
            child.Attachable.Parent = parent;
            child.Attachable.AttachType = Warudo.Plugins.Core.Assets.Mixins.AttachType.TransformPath;
            child.Attachable.AttachToTransform = null;
        }

        /// <summary>
        /// Parent assets and preserve world transforms.
        /// Kinda hack-ish, since modifying GOA transforms directly seems to be undone immediately
        /// NOTE: This doesn't create the parent on GOA. Use #SetParent instead when possible.
        /// </summary>
        /// <param name="child">Child GOA</param>
        /// <param name="parent">Parent GOA</param>
        void _SetParent(GameObjectAsset child, GameObjectAsset parent) {
            var ch = new GameObject();
            ch.transform.SetParent(child.GameObject.transform.parent);
            ch.transform.localPosition = child.Transform.Position;
            ch.transform.localRotation = Quaternion.Euler(child.Transform.Rotation);
            ch.transform.localScale = child.Transform.Scale;

            var pa = new GameObject();
            pa.transform.SetParent(parent.GameObject.transform.parent);
            pa.transform.localPosition = parent.Transform.Position;
            pa.transform.localRotation = Quaternion.Euler(parent.Transform.Rotation);
            pa.transform.localScale = parent.Transform.Scale;

            // Take advantage of unity recursion multiplication
            ch.transform.SetParent(pa.transform, true);

            child.Transform.Position = ch.transform.localPosition;
            child.Transform.Rotation = ch.transform.localRotation.eulerAngles;
            child.Transform.Scale = ch.transform.localScale;

            UnityEngine.Object.Destroy(ch);
            UnityEngine.Object.Destroy(pa);
        }

        void UnsetParent(PropAsset child) {
            _UnsetParent(child);
            child.Attachable.Parent = null;
        }

        void _UnsetParent(GameObjectAsset child) {

            var transform = child.Transform;
            var unityTransform = child.GameObject.transform;
            transform.Position = unityTransform.position;
            transform.Rotation = unityTransform.rotation.eulerAngles;
            transform.Scale = unityTransform.lossyScale;
        }

        void CleanDestroy(GameObjectAsset g) {
            if (g == null) return;
            try {
                Scene.RemoveAsset(g.Id);
            } catch {}
        }

        [Section("Shaking Motion")]

        [Markdown]
        [HiddenIf(nameof(CanConfigureShaking))]
        public string ShakingMotionInstructions = "Allows game controller to shake in response to input.";

        [DataInput]
        [Label("Enable")]
        [DisabledIf(nameof(IsSetupIncomplete))]
        public bool IsShakingEnabled;

        public bool CanConfigureShaking() {
            return IsShakingEnabled;
        }

        public bool CannotConfigureShaking() {
            return !IsShakingEnabled;
        }

        public enum ControllerType {
            SwitchProController,
            PS5Controller,
        }

        [DataInput]
        [HiddenIf(nameof(CannotConfigureShaking))]
        [FloatSlider(0.1f, 10f, 0.01f)]
        float TiltInfluenceFactor = 1.0f;
        [DataInput]
        [HiddenIf(nameof(CannotConfigureShaking))]
        [FloatSlider(0.1f, 10f, 0.01f)]
        float DisplacementInfluenceFactor = 1.0f;

        Tween rotationTween;
        Tween positionTween;

        bool LastAnyFaceButton;

        void PerformPropMovement() {
            if (!IsShakingEnabled) {
                return;
            }

            var anchor = GamepadAnchor;
            if (anchor == null) {
                return;
            }

            var displacement = Vector3.zero;
            if (IsAnyFaceButtonActivated) {
                displacement += Vector3.down;
            }
            if (IsAnyFaceButtonDown) {
                displacement += Vector3.down;
            }
            if (IsAnyShoulderButtonActivated) {
                displacement += Vector3.back;
            }
            if (IsAnyShoulderButtonDown) {
                displacement += Vector3.back;
            }

            var influenceX = LeftStickX + RightStickX;
            var influenceY = LeftStickY + RightStickY;

            switch(Pad) {
                case 1:
                    influenceX += -1f;
                    influenceY += 1f;
                    break;
                case 2:
                    influenceY += -2f;
                    break;
                case 3:
                    influenceX += 1f;
                    influenceY += 1f;
                    break;
                case 4:
                    influenceX += -2f;
                    break;
                case 6:
                    influenceX += 2f;
                    break;
                case 7:
                    influenceX += -1f;
                    influenceY += -1f;
                    break;
                case 8:
                    influenceY += -2f;
                    break;
                case 9:
                    influenceX += 1f;
                    influenceY += -1f;
                    break;
            }

            var tilt = new Vector3(-influenceY, 0, -influenceX);


            positionTween?.Kill();
            positionTween = DOTween.To(
                () => anchor.Transform.Position,
                delegate(Vector3 it) { anchor.Transform.Position = it; },
                GamepadAnchorPosition + displacement * 0.001f * DisplacementInfluenceFactor,
                0.1f
            ).SetEase(Ease.OutBack);

            rotationTween?.Kill();
            rotationTween = DOTween.To(
                () => anchor.Transform.Rotation,
                delegate(Vector3 it) { anchor.Transform.Rotation = it; },
                GamepadAnchorRotation + tilt * TiltInfluenceFactor,
                0.1f
            ).SetEase(Ease.Linear);
        }

        float LeftStickX {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return (LX / (float)ushort.MaxValue - 0.5f) * 2f;
                }
                return 0.5f;
            }
        }

        float LeftStickY {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return (LY / (float)ushort.MaxValue - 0.5f) * 2f;
                }
                return 0.5f;
            }
        }

        float RightStickX {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return (LrX / (float)ushort.MaxValue - 0.5f) * 2f;
                }
                return 0.5f;
            }
        }

        float RightStickY {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return (LrY / (float)ushort.MaxValue - 0.5f) * 2f;
                }
                return 0.5f;
            }
        }

        bool IsAnyFaceButtonActivated {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return Array.Find(FaceButtonIdsSwitch, v => ActivatedButtonFlag(v)) != 0;
                }
                return false;
            }
        }

        bool IsAnyFaceButtonDown {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return Array.Find(FaceButtonIdsSwitch, v => ButtonFlag(v)) != 0;
                }
                return false;
            }
        }

        bool IsAnyShoulderButtonActivated {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return Array.Find(ShoulderButtonIdsSwitch, v => ActivatedButtonFlag(v)) != 0;
                }
                return false;
            }
        }

        bool IsAnyShoulderButtonDown {
            get {
                switch(TargetControllerType) {
                    case ControllerType.SwitchProController:
                        return Array.Find(ShoulderButtonIdsSwitch, v => ButtonFlag(v)) != 0;
                }
                return false;
            }
        }

        // NOTE: Buttons indices are offset by one to allow dummy default at 0./
        public enum SwitchProButton: int
        {
            None = 0,
            B = 1,
            A = 2,
            Y = 3,
            X = 4,
            L = 5,
            R = 6,
            ZL = 7,
            ZR = 8,
            Plus = 9,
            Minus = 10,
            LeftStick = 11,
            RightStick = 12,
            Home = 13,
            Capture = 14,
        };

        public static readonly SwitchProButton[] LeftFaceButtonIdsSwitch = {
            SwitchProButton.LeftStick,
            SwitchProButton.Plus,
            SwitchProButton.Capture,
        };

        public static readonly SwitchProButton[] RightFaceButtonIdsSwitch = {
            SwitchProButton.A,
            SwitchProButton.B,
            SwitchProButton.X,
            SwitchProButton.Y,
            SwitchProButton.RightStick,
            SwitchProButton.Minus,
            SwitchProButton.Home,
        };

        public static readonly SwitchProButton[] FaceButtonIdsSwitch = LeftFaceButtonIdsSwitch.Union(RightFaceButtonIdsSwitch).ToArray();

        public static readonly SwitchProButton[] LeftShoulderButtonIdsSwitch = {
            SwitchProButton.L,
            SwitchProButton.ZL,
        };

        public static readonly SwitchProButton[] RightShoulderButtonIdsSwitch = {
            SwitchProButton.R,
            SwitchProButton.ZR,
        };

        public static readonly SwitchProButton[] ShoulderButtonIdsSwitch = LeftShoulderButtonIdsSwitch.Union(RightShoulderButtonIdsSwitch).ToArray();

        [Section("Hand Tracking")]

        [Markdown]
        [HiddenIf(nameof(IsHandTrackingGraphPresent))]
        public string HandTrackingInstructions = @"### Information
Allows temporarily holding the game controller with the untracked hand while the other hand is being tracked.
### Instructions
Go to your **Pose Tracking** blueprint and insert the **🔥🎮 Hand Tracker** node before the **Override Character Bone Rotations** node's **Bone Rotation Weights** input.";

        string HandTrackingGraphId;

        bool IsHandTrackingGraphPresent() {
            return HandTrackingGraphId != null;
        }
        bool IsHandTrackingGraphBlank() {
            return !IsHandTrackingGraphPresent();
        }

        [Trigger]
        [HiddenIf(nameof(IsHandTrackingGraphPresent))]
        void GenerateBlueprint() {

            Graph graph = new Graph
            {
                Name = "🔥🎮 Hand Tracker",
                Enabled = true
            };

            CommentNode commentNode = graph.AddNode<CommentNode>();
            commentNode.SetDataInput("Text", $@"### Instructions

1. Place node corresponding to your hand tracker and feed it with the hand tracking flags.
2. Go to your **Pose Tracking** blueprint and insert the **🔥🎮 Hand Tracker** node before the **Override Character Bone Rotations** node's **Bone Rotation Weights** input. Feed it the hand tracking flags.
");

            OnUpdateNode onUpdateNode = graph.AddNode<OnUpdateNode>();

            GamepadHandSwitcherNode gamepadHandTrackerNode = graph.AddNode<GamepadHandSwitcherNode>();
            gamepadHandTrackerNode.Receiver = this;

            graph.AddFlowConnection(onUpdateNode, "Exit", gamepadHandTrackerNode, "Enter");

            base.Scene.AddGraph(graph);
            HandTrackingGraphId = graph.Id.ToString();
            Context.Service.PromptMessage("SUCCESS", $"Blueprint {graph.Name} has been succesfully generated.");
            Context.Service.BroadcastOpenedScene();
            Context.Service.NavigateToGraph(graph.Id, commentNode.Id);
        }

        [Trigger]
        [HiddenIf(nameof(IsHandTrackingGraphBlank))]
        async void DeleteGeneratedBlueprint() {

            Graph graph = Context.OpenedScene.GetGraph(Guid.Parse(HandTrackingGraphId));
            if (graph == null)
            {
                HandTrackingGraphId = null;
            }
            else if (await Context.Service.PromptConfirmation("WARNING", "BLUEPRINT_WILL_BE_REMOVED".Localized(graph.Name)))
            {
                Context.OpenedScene.RemoveGraph(graph.Id);
                Context.Service.BroadcastOpenedScene();
                HandTrackingGraphId = null;
            }
        }

        [DataInput]
        public Vector3 HoldLeftHandTilt = new Vector3(30f, 10f, 0f);
        [DataInput]
        public Vector3 HoldLeftHandDisplacement = new Vector3(0f, -0.05f, -0.02f);

        [DataInput]
        public Vector3 HoldRightHandTilt = new Vector3(30f, 10f, 0f);
        [DataInput]
        public Vector3 HoldRightHandDisplacement = new Vector3(0f, -0.05f, -0.02f);

        [DataInput]
        [FloatSlider(0f, 2f, 0.01f)]
        public float TiltTransitionTime = 1.0f;
        [DataInput]
        public Ease TiltEasing = Ease.OutCubic;
        [DataInput]
        [FloatSlider(0f, 2f, 0.01f)]
        public float DisplacementTransitionTime = 1.5f;
        [DataInput]
        public Ease DisplacementEasing = Ease.OutBack;
        [DataInput]
        [FloatSlider(0f, 2f, 0.01f)]
        public float ReturnTime = 0.2f;
        [DataInput]
        public Ease ReturnEasing = Ease.OutBack;

        Tween leftPositionTween;
        Tween leftRotationTween;

        Tween rightPositionTween;
        Tween rightRotationTween;

        // -1: Left
        //  0: None
        //  1: Right
        short firstTracked;
        bool LastIsLeftHandTracked;
        bool LastIsRightHandTracked;

        public short ProcessHandTracking(bool IsLeftHandTracked, bool IsRightHandTracked) {

            if (firstTracked == 0) {
                if (IsLeftHandTracked) {
                    firstTracked = -1;
                } else if (IsRightHandTracked) {
                    firstTracked = 1;
                }
            } else {
                if (!IsLeftHandTracked && !IsRightHandTracked) {
                    firstTracked = 0;
                }
            }

            if (IsLeftHandTracked && !LastIsLeftHandTracked && !IsRightHandTracked) {
                // Ensure gamepad is on right hand
                TransitionRightHandAnchorToHoldState();
            } else if (IsRightHandTracked && !LastIsRightHandTracked && !IsLeftHandTracked) {
                // Ensure gamepad is on left hand
                TransitionLeftHandAnchorToHoldState();
            } else if (!IsLeftHandTracked && !IsRightHandTracked && (LastIsLeftHandTracked || LastIsRightHandTracked)) {
                TransitionHandAnchorsToResetState();
            }

            LastIsLeftHandTracked = IsLeftHandTracked;
            LastIsRightHandTracked = IsRightHandTracked;

            return firstTracked;
        }

        public void TransitionRightHandAnchorToHoldState() {
            var leftAnchor = GamepadLeftHandAnchor;
            var rightAnchor = GamepadRightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            AttachGamepad(GamepadAnchorSide.RightHand);

            rightPositionTween?.Kill();
            rightPositionTween = DOTween.To(
                () => rightAnchor.Transform.Position,
                delegate(Vector3 it) { rightAnchor.Transform.Position = it; },
                RightHandAnchorPosition + HoldRightHandDisplacement,
                DisplacementTransitionTime
            ).SetEase(DisplacementEasing);
            rightRotationTween?.Kill();
            rightRotationTween = DOTween.To(
                () => rightAnchor.Transform.Rotation,
                delegate(Vector3 it) { rightAnchor.Transform.Rotation = it; },
                RightHandAnchorRotation + HoldLeftHandTilt,
                TiltTransitionTime
            ).SetEase(TiltEasing);
        }

        public void TransitionLeftHandAnchorToHoldState() {
            var leftAnchor = GamepadLeftHandAnchor;
            var rightAnchor = GamepadRightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            AttachGamepad(GamepadAnchorSide.LeftHand);

            leftPositionTween?.Kill();
            leftPositionTween = DOTween.To(
                () => leftAnchor.Transform.Position,
                delegate(Vector3 it) { leftAnchor.Transform.Position = it; },
                LeftHandAnchorPosition + HoldLeftHandDisplacement,
                DisplacementTransitionTime
            ).SetEase(DisplacementEasing);

            leftRotationTween?.Kill();
            leftRotationTween = DOTween.To(
                () => leftAnchor.Transform.Rotation,
                delegate(Vector3 it) { leftAnchor.Transform.Rotation = it; },
                LeftHandAnchorRotation + HoldLeftHandTilt,
                TiltTransitionTime
            ).SetEase(TiltEasing);
        }

        public void TransitionHandAnchorsToResetState() {
            var leftAnchor = GamepadLeftHandAnchor;
            var rightAnchor = GamepadRightHandAnchor;
            if (leftAnchor == null || rightAnchor == null) {
                return;
            }

            rightPositionTween?.Kill();
            rightPositionTween = DOTween.To(
                () => rightAnchor.Transform.Position,
                delegate(Vector3 it) { rightAnchor.Transform.Position = it; },
                RightHandAnchorPosition,
                ReturnTime
            ).SetEase(ReturnEasing);

            rightRotationTween?.Kill();
            rightRotationTween = DOTween.To(
                () => rightAnchor.Transform.Rotation,
                delegate(Vector3 it) { rightAnchor.Transform.Rotation = it; },
                RightHandAnchorRotation,
                ReturnTime
            ).SetEase(ReturnEasing);

            leftPositionTween?.Kill();
            leftPositionTween = DOTween.To(
                () => leftAnchor.Transform.Position,
                delegate(Vector3 it) { leftAnchor.Transform.Position = it; },
                LeftHandAnchorPosition,
                ReturnTime
            ).SetEase(ReturnEasing);

            leftRotationTween?.Kill();
            leftRotationTween = DOTween.To(
                () => leftAnchor.Transform.Rotation,
                delegate(Vector3 it) { leftAnchor.Transform.Rotation = it; },
                LeftHandAnchorRotation,
                ReturnTime
            ).SetEase(ReturnEasing);
        }

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
