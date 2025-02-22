using System;
using System.Linq;
using Warudo.Core;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Graphs;
using Warudo.Plugins.Core.Nodes.Event;
using static Warudo.Plugins.Core.Assets.Character.CharacterAsset;

namespace FlameStream
{
    public partial class GamepadReceiverAsset : ReceiverAsset {

        const string LAYER_NAME_PREFIX = "🔥🎮";
        const string LAYER_NAME_IDLE = "🔥🎮 Idle";

        [DataInput]
        [Hidden]
        Guid AnimationGraphId;

        Graph AnimationGraph {
            get {
                return Context.OpenedScene.GetGraph(AnimationGraphId);
            }
        }

        void GenerateButtonAnimationDataTemplate() {
            var buttonType = typeof(SwitchProButton);
            switch (TargetGamepadType) {
                case GamepadType.PS5Controller:
                    buttonType = typeof(PS5Button);
                    break;
                case GamepadType.Xbox360Controller:
                    buttonType = typeof(Xbox360Button);
                    break;
            }

            ButtonAnimationData = Enum.GetValues(buttonType)
                .Cast<SwitchProButton>()
                .Where(e => e != 0)
                .Select(e => {
                    var old = Array.Find(ButtonAnimationData, d => d.ButtonId == (int)e);

                    var o = StructuredData.Create<GamepadButtonAnimationData>();
                    o.TargetControllerType = TargetGamepadType;
                    o.ButtonId = (int)e;
                    o.PropLayerName = old?.PropLayerName ?? e.ToString();
                    o.FingerHoverAnimation = old?.FingerHoverAnimation ?? null;
                    o.FingerPressAnimation = old?.FingerPressAnimation ?? null;
                    o.MaskRight = old?.MaskRight ?? false;
                    return o;
                })
                .ToArray();

            DPadAnimationData = Enum.GetValues(typeof(DPadDirection))
                .Cast<DPadDirection>()
                .Where(e => e != DPadDirection.None && e != DPadDirection.Neutral)
                .Select(e => {
                    var old = Array.Find(DPadAnimationData, d => (DPadDirection)d.ButtonId == e);

                    var o = StructuredData.Create<GamepadDPadAnimationData>();
                    o.ButtonId = (int)e;
                    o.PropLayerName = old?.PropLayerName ?? DPadLabelName((byte)e);
                    o.FingerHoverAnimation = old?.FingerHoverAnimation ?? null;
                    o.FingerPressAnimation = old?.FingerPressAnimation ?? null;
                    return o;
                })
                .ToArray();

            var oldLeftStickAnimationData = LeftStickAnimationData;
            LeftStickAnimationData = StructuredData.Create<GamepadStickAnimationData>((d) => {
                d.Number = 1;
                d.PositiveXAnimation = oldLeftStickAnimationData?.PositiveXAnimation;
                d.NegativeXAnimation = oldLeftStickAnimationData?.NegativeXAnimation;
                d.PositiveYAnimation = oldLeftStickAnimationData?.PositiveYAnimation;
                d.NegativeYAnimation = oldLeftStickAnimationData?.NegativeYAnimation;
                d.PropPositiveXLayerName = oldLeftStickAnimationData?.PropPositiveXLayerName ?? "Stick1 +X";
                d.PropPositiveYLayerName = oldLeftStickAnimationData?.PropPositiveYLayerName ?? "Stick1 +Y";
                d.PropNegativeXLayerName = oldLeftStickAnimationData?.PropNegativeXLayerName ?? "Stick1 -X";
                d.PropNegativeYLayerName = oldLeftStickAnimationData?.PropNegativeYLayerName ?? "Stick1 -Y";
            });

            var oldRightStickAnimationData = RightStickAnimationData;
            RightStickAnimationData = StructuredData.Create<GamepadStickAnimationData>((d) => {
                d.Number = 2;
                d.PositiveXAnimation = oldRightStickAnimationData?.PositiveXAnimation;
                d.PositiveYAnimation = oldRightStickAnimationData?.PositiveYAnimation;
                d.NegativeXAnimation = oldRightStickAnimationData?.NegativeXAnimation;
                d.NegativeYAnimation = oldRightStickAnimationData?.NegativeYAnimation;
                d.PropPositiveXLayerName = oldRightStickAnimationData?.PropPositiveXLayerName ?? "Stick2 +X";
                d.PropPositiveYLayerName = oldRightStickAnimationData?.PropPositiveYLayerName ?? "Stick2 +Y";
                d.PropNegativeXLayerName = oldRightStickAnimationData?.PropNegativeXLayerName ?? "Stick2 -X";
                d.PropNegativeYLayerName = oldRightStickAnimationData?.PropNegativeYLayerName ?? "Stick2 -Y";
            });

            BroadcastDataInput(nameof(ButtonAnimationData));
            BroadcastDataInput(nameof(DPadAnimationData));
            BroadcastDataInput(nameof(LeftStickAnimationData));
            BroadcastDataInput(nameof(RightStickAnimationData));
            Context.Service.PromptMessage("SUCCESS", $@"Button animation definition for [{TargetGamepadType}] has been succesfully generated.

Please note that they do not have to be all filled. You may remove unused fields to optimize your setup.
");
        }

        /// <summary>
        /// Ideally this should not be needed and instead we'd use the transient stack.
        /// But we don't have control over it; it may be cleared by somethign else.
        /// So until we have a reserved stack that can be self-managed, this will have to do.
        /// </summary>
        async void GenerateAnimationBlueprint() {

            Graph graph = new Graph
            {
                Name = "🔥🎮 Legacy Animators",
                Enabled = true
            };

            var receiverNode = CreateReceiverNode(graph);
            receiverNode.Receiver = this;

            OnUpdateNode onUpdateLeftFingerButtonNode = graph.AddNode<OnUpdateNode>();
            OnUpdateNode onUpdateRightFingerButtonNode = graph.AddNode<OnUpdateNode>();
            OnUpdateNode onUpdateOtherFingerButtonNode = graph.AddNode<OnUpdateNode>();
            OnUpdateNode onUpdateLeftPropButtonNode = graph.AddNode<OnUpdateNode>();
            OnUpdateNode onUpdateRightPropButtonNode = graph.AddNode<OnUpdateNode>();
            OnUpdateNode onUpdateOtherPropButtonNode = graph.AddNode<OnUpdateNode>();

            GamepadButtonHandAnimatorNode prevLeftFingerAnimatorNode = null;
            GamepadButtonHandAnimatorNode prevRightFingerAnimatorNode = null;
            GamepadButtonHandAnimatorNode prevOtherFingerAnimatorNode = null;
            GamepadButtonPropAnimatorNode prevLeftPropButtonNode = null;
            GamepadButtonPropAnimatorNode prevRightPropButtonNode = null;
            GamepadButtonPropAnimatorNode prevOtherPropButtonNode = null;

            foreach(var d in ButtonAnimationData) {
                var buttonName = d.ButtonName;
                var fingerAnimatorNode = graph.AddNode<GamepadButtonHandAnimatorNode>();
                fingerAnimatorNode.Character = Character;
                fingerAnimatorNode.InputId = buttonName;
                fingerAnimatorNode.HoverLayerId = d.HoverLayerId;
                fingerAnimatorNode.PressLayerId = d.PressLayerId;
                fingerAnimatorNode.HoverInputId = fingerAnimatorNode.InputId;

                var propButtonAnimatorNode = graph.AddNode<GamepadButtonPropAnimatorNode>();
                propButtonAnimatorNode.Controller = Gamepad;
                propButtonAnimatorNode.PressLayerId = buttonName;

                AddDataConnection(graph, receiverNode, buttonName, fingerAnimatorNode, "IsPressed");
                AddDataConnection(graph, receiverNode, buttonName, propButtonAnimatorNode, "IsPressed");

                var leftFaceButton = LeftFaceButtonIdsSwitch.FirstOrDefault(e => { return (int)e == d.ButtonId; });
                var rightFaceButton = RightFaceButtonIdsSwitch.FirstOrDefault(e => { return (int)e == d.ButtonId; });

                if (leftFaceButton != 0) {
                    AddDataConnection(
                        graph,
                        (Node)prevLeftFingerAnimatorNode ?? receiverNode,
                        prevLeftFingerAnimatorNode == null ? "LeftFaceHoverInputId" : "_HoverInputId",
                        fingerAnimatorNode,
                        "HoverInputId"
                    );

                    AddFlowConnection(graph, (Node)prevLeftFingerAnimatorNode ?? onUpdateLeftFingerButtonNode, "Exit", fingerAnimatorNode, "Enter");
                    AddFlowConnection(graph, (Node)prevLeftPropButtonNode ?? onUpdateLeftPropButtonNode, "Exit", propButtonAnimatorNode, "Enter");
                    prevLeftFingerAnimatorNode = fingerAnimatorNode;
                    prevLeftPropButtonNode = propButtonAnimatorNode;

                } else if (rightFaceButton != 0) {

                    AddDataConnection(
                        graph,
                        (Node)prevRightFingerAnimatorNode ?? receiverNode,
                        prevRightFingerAnimatorNode == null ? "RightFaceHoverInputId" : "_HoverInputId",
                        fingerAnimatorNode,
                        "HoverInputId"
                    );

                    AddFlowConnection(graph, (Node)prevRightFingerAnimatorNode ?? onUpdateRightFingerButtonNode, "Exit", fingerAnimatorNode, "Enter");
                    AddFlowConnection(graph, (Node)prevRightPropButtonNode ?? onUpdateRightPropButtonNode, "Exit", propButtonAnimatorNode, "Enter");
                    prevRightFingerAnimatorNode = fingerAnimatorNode;
                    prevRightPropButtonNode = propButtonAnimatorNode;

                } else {

                    AddFlowConnection(graph, (Node)prevOtherFingerAnimatorNode ?? onUpdateOtherFingerButtonNode, "Exit", fingerAnimatorNode, "Enter");
                    AddFlowConnection(graph, (Node)prevOtherPropButtonNode ?? onUpdateOtherPropButtonNode, "Exit", propButtonAnimatorNode, "Enter");
                    prevOtherFingerAnimatorNode = fingerAnimatorNode;
                    prevOtherPropButtonNode = propButtonAnimatorNode;
                }
            }

            OnUpdateNode onUpdateControlPadNode = graph.AddNode<OnUpdateNode>();

            var fingerControlPadAnimatorNode = graph.AddNode<GamepadDPadHandAnimatorNode>();
            fingerControlPadAnimatorNode.Character = Character;
            fingerControlPadAnimatorNode.D1HoverLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 1)?.HoverLayerId;
            fingerControlPadAnimatorNode.D1PressLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 1)?.PressLayerId;
            fingerControlPadAnimatorNode.D2HoverLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 2)?.HoverLayerId;
            fingerControlPadAnimatorNode.D2PressLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 2)?.PressLayerId;
            fingerControlPadAnimatorNode.D3HoverLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 3)?.HoverLayerId;
            fingerControlPadAnimatorNode.D3PressLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 3)?.PressLayerId;
            fingerControlPadAnimatorNode.D4HoverLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 4)?.HoverLayerId;
            fingerControlPadAnimatorNode.D4PressLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 4)?.PressLayerId;
            fingerControlPadAnimatorNode.D6HoverLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 6)?.HoverLayerId;
            fingerControlPadAnimatorNode.D6PressLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 6)?.PressLayerId;
            fingerControlPadAnimatorNode.D7HoverLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 7)?.HoverLayerId;
            fingerControlPadAnimatorNode.D7PressLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 7)?.PressLayerId;
            fingerControlPadAnimatorNode.D8HoverLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 8)?.HoverLayerId;
            fingerControlPadAnimatorNode.D8PressLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 8)?.PressLayerId;
            fingerControlPadAnimatorNode.D9HoverLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 9)?.HoverLayerId;
            fingerControlPadAnimatorNode.D9PressLayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 9)?.PressLayerId;
            AddDataConnection(graph, receiverNode, "LeftFaceHoverInputId", fingerControlPadAnimatorNode, "HoverInputId");
            AddDataConnection(graph, receiverNode, "DPad", fingerControlPadAnimatorNode, "DPad");
            AddFlowConnection(graph, onUpdateControlPadNode, "Exit", fingerControlPadAnimatorNode, "Enter");

            var propControlPadAnimatorNode = graph.AddNode<GamepadDPadPropAnimatorNode>();
            propControlPadAnimatorNode.Controller = Gamepad;
            propControlPadAnimatorNode.D1LayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 1)?.PropLayerName;
            propControlPadAnimatorNode.D2LayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 2)?.PropLayerName;
            propControlPadAnimatorNode.D3LayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 3)?.PropLayerName;
            propControlPadAnimatorNode.D4LayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 4)?.PropLayerName;
            propControlPadAnimatorNode.D6LayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 6)?.PropLayerName;
            propControlPadAnimatorNode.D7LayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 7)?.PropLayerName;
            propControlPadAnimatorNode.D8LayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 8)?.PropLayerName;
            propControlPadAnimatorNode.D9LayerId = DPadAnimationData.FirstOrDefault(d => d.ButtonId == 9)?.PropLayerName;
            AddDataConnection(graph, receiverNode, "DPad", propControlPadAnimatorNode, "DPad");
            AddFlowConnection(graph, fingerControlPadAnimatorNode, "Exit", propControlPadAnimatorNode, "Enter");

            OnUpdateNode onUpdateStickNode = graph.AddNode<OnUpdateNode>();

            var fingerStick1AxisXAnimatorNode = graph.AddNode<GamepadStickHandAnimatorNode>();
            fingerStick1AxisXAnimatorNode.Character = Character;
            fingerStick1AxisXAnimatorNode.NegativeLayerId = LeftStickAnimationData.NegativeXLayerId;
            fingerStick1AxisXAnimatorNode.PositiveLayerId = LeftStickAnimationData.PositiveXLayerId;
            AddDataConnection(graph, receiverNode, "LeftStickX", fingerStick1AxisXAnimatorNode, "AxisValue");
            AddFlowConnection(graph, onUpdateStickNode, "Exit", fingerStick1AxisXAnimatorNode, "Enter");

            var fingerStick1AxisYAnimatorNode = graph.AddNode<GamepadStickHandAnimatorNode>();
            fingerStick1AxisYAnimatorNode.Character = Character;
            fingerStick1AxisYAnimatorNode.NegativeLayerId = LeftStickAnimationData.NegativeYLayerId;
            fingerStick1AxisYAnimatorNode.PositiveLayerId = LeftStickAnimationData.PositiveYLayerId;
            AddDataConnection(graph, receiverNode, "LeftStickY", fingerStick1AxisYAnimatorNode, "AxisValue");
            AddFlowConnection(graph, fingerStick1AxisXAnimatorNode, "Exit", fingerStick1AxisYAnimatorNode, "Enter");

            var fingerStick2AxisXAnimatorNode = graph.AddNode<GamepadStickHandAnimatorNode>();
            fingerStick2AxisXAnimatorNode.Character = Character;
            fingerStick2AxisXAnimatorNode.NegativeLayerId = RightStickAnimationData.NegativeXLayerId;
            fingerStick2AxisXAnimatorNode.PositiveLayerId = RightStickAnimationData.PositiveXLayerId;
            AddDataConnection(graph, receiverNode, "RightStickX", fingerStick2AxisXAnimatorNode, "AxisValue");
            AddFlowConnection(graph, fingerStick1AxisYAnimatorNode, "Exit", fingerStick2AxisXAnimatorNode, "Enter");

            var fingerStick2AxisYAnimatorNode = graph.AddNode<GamepadStickHandAnimatorNode>();
            fingerStick2AxisYAnimatorNode.Character = Character;
            fingerStick2AxisYAnimatorNode.NegativeLayerId = RightStickAnimationData.NegativeYLayerId;
            fingerStick2AxisYAnimatorNode.PositiveLayerId = RightStickAnimationData.PositiveYLayerId;
            AddDataConnection(graph, receiverNode, "RightStickY", fingerStick2AxisYAnimatorNode, "AxisValue");
            AddFlowConnection(graph, fingerStick2AxisXAnimatorNode, "Exit", fingerStick2AxisYAnimatorNode, "Enter");

            var propStick1AxisXAnimatorNode = graph.AddNode<GamepadStickPropAnimatorNode>();
            propStick1AxisXAnimatorNode.Controller = Gamepad;
            propStick1AxisXAnimatorNode.NegativeLayerId = LeftStickAnimationData.PropNegativeXLayerName;
            propStick1AxisXAnimatorNode.PositiveLayerId = LeftStickAnimationData.PropPositiveXLayerName;
            AddDataConnection(graph, receiverNode, "LeftStickX", propStick1AxisXAnimatorNode, "AxisValue");
            AddFlowConnection(graph, fingerStick2AxisYAnimatorNode, "Exit", propStick1AxisXAnimatorNode, "Enter");

            var propStick1AxisYAnimatorNode = graph.AddNode<GamepadStickPropAnimatorNode>();
            propStick1AxisYAnimatorNode.Controller = Gamepad;
            propStick1AxisYAnimatorNode.NegativeLayerId = LeftStickAnimationData.PropNegativeYLayerName;
            propStick1AxisYAnimatorNode.PositiveLayerId = LeftStickAnimationData.PropPositiveYLayerName;
            AddDataConnection(graph, receiverNode, "LeftStickY", propStick1AxisYAnimatorNode, "AxisValue");
            AddFlowConnection(graph, propStick1AxisXAnimatorNode, "Exit", propStick1AxisYAnimatorNode, "Enter");

            var propStick2AxisXAnimatorNode = graph.AddNode<GamepadStickPropAnimatorNode>();
            propStick2AxisXAnimatorNode.Controller = Gamepad;
            propStick2AxisXAnimatorNode.NegativeLayerId = RightStickAnimationData.PropNegativeXLayerName;
            propStick2AxisXAnimatorNode.PositiveLayerId = RightStickAnimationData.PropPositiveXLayerName;
            AddDataConnection(graph, receiverNode, "RightStickX", propStick2AxisXAnimatorNode, "AxisValue");
            AddFlowConnection(graph, propStick1AxisYAnimatorNode, "Exit", propStick2AxisXAnimatorNode, "Enter");

            var propStick2AxisYAnimatorNode = graph.AddNode<GamepadStickPropAnimatorNode>();
            propStick2AxisYAnimatorNode.Controller = Gamepad;
            propStick2AxisYAnimatorNode.NegativeLayerId = RightStickAnimationData.PropNegativeYLayerName;
            propStick2AxisYAnimatorNode.PositiveLayerId = RightStickAnimationData.PropPositiveYLayerName;
            AddDataConnection(graph, receiverNode, "RightStickY", propStick2AxisYAnimatorNode, "AxisValue");
            AddFlowConnection(graph, propStick2AxisXAnimatorNode, "Exit", propStick2AxisYAnimatorNode, "Enter");

            Graph oldgraph = AnimationGraph;
            if (oldgraph != null) {
                bool isDeleteWanted = await Context.Service.PromptConfirmation("WARNING", "Previously generated blueprint has been detected. Would you like to replace it?");
                if (isDeleteWanted) {
                    Context.OpenedScene.RemoveGraph(oldgraph.Id);
                }
            }

            AnimationGraphId = graph.Id;
            base.Scene.AddGraph(graph);
            Context.Service.PromptMessage("SUCCESS", $"Blueprint {graph.Name} has been succesfully generated.");
            Context.Service.NavigateToGraph(AnimationGraphId, receiverNode.Id);
            Context.Service.BroadcastOpenedScene();
        }

        GetGamepadReceiverDataNode CreateReceiverNode(Graph graph) {
            switch(TargetGamepadType) {
                case GamepadType.SwitchProController:
                    return graph.AddNode<GetGamepadReceiverDataSwitchNode>();
                case GamepadType.PS5Controller:
                    return graph.AddNode<GetGamepadReceiverDataPs5Node>();
                case GamepadType.Xbox360Controller:
                    return graph.AddNode<GetGamepadReceiverDataXbox360Node>();
            }
            return null;
        }

        void AddFlowConnection(Graph graph, Node n1, string s1, Node n2, string s2) {
            UnityEngine.Debug.Log($"[FlameStream.Asset.GamepadReceiver.Animation] FLOW {n1.Name}#{s1} => {n2.Name}#{s2}");
            graph.AddFlowConnection(n1, s1, n2, s2);
        }

        void AddDataConnection(Graph graph, Node n1, string s1, Node n2, string s2) {
            UnityEngine.Debug.Log($"[FlameStream.Asset.GamepadReceiver.Animation] DATA {n1.Name}#{s1} => {n2.Name}#{s2}");
            graph.AddDataConnection(n1, s1, n2, s2);
        }

        void SyncCharacterOverlayingAnimations() {

            var userLayers = Character.OverlappingAnimations.Where(d => !(d.CustomLayerID?.StartsWith(LAYER_NAME_PREFIX) ?? false)).ToList();

            // This one must be before all additive animations
            if (IdleFingerAnimation != null) {
                var oldLayer = Character.OverlappingAnimations.FirstOrDefault(d => d.CustomLayerID == LAYER_NAME_IDLE);
                OverlappingAnimationData layer;
                if (oldLayer != null) {
                    layer = oldLayer;
                } else {
                    layer = CreateIdleFingerAnimationData();
                }

                userLayers.Add(layer);
            }

            foreach (var d in ButtonAnimationData) {
                if (d.FingerHoverAnimation != null) {
                    var hoverLayer = StructuredData.Create<OverlappingAnimationData>();
                    hoverLayer.Animation = d.FingerHoverAnimation;
                    hoverLayer.Weight = 1f;
                    hoverLayer.Speed = 1f;
                    hoverLayer.Masked = true;
                    hoverLayer.MaskedBodyParts = d.MaskRight
                        ? new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers }
                        : new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                    hoverLayer.Additive = true;
                    hoverLayer.Looping = false;
                    hoverLayer.CustomLayerID = d.HoverLayerId;

                    userLayers.Add(hoverLayer);
                }

                if (d.FingerPressAnimation != null) {
                    var pressLayer = StructuredData.Create<OverlappingAnimationData>();
                    pressLayer.Animation = d.FingerPressAnimation;
                    pressLayer.Weight = 1f;
                    pressLayer.Speed = 1f;
                    pressLayer.Masked = true;
                    pressLayer.MaskedBodyParts = d.MaskRight
                        ? new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.RightFingers }
                        : new AnimationMaskedBodyPart[] { AnimationMaskedBodyPart.LeftFingers };
                    pressLayer.Additive = true;
                    pressLayer.Looping = false;
                    pressLayer.CustomLayerID = d.PressLayerId;

                    userLayers.Add(pressLayer);
                }
            }

            foreach (var d in DPadAnimationData) {
                if (d.FingerHoverAnimation != null) {
                    var hoverLayer = StructuredData.Create<OverlappingAnimationData>();
                    hoverLayer.Animation = d.FingerHoverAnimation;
                    hoverLayer.Weight = 1f;
                    hoverLayer.Speed = 1f;
                    hoverLayer.Masked = true;
                    hoverLayer.MaskedBodyParts = new AnimationMaskedBodyPart[] {
                        AnimationMaskedBodyPart.RightFingers,
                        AnimationMaskedBodyPart.LeftFingers
                    };
                    hoverLayer.Additive = true;
                    hoverLayer.Looping = false;
                    hoverLayer.CustomLayerID = d.HoverLayerId;

                    userLayers.Add(hoverLayer);
                }

                if (d.FingerPressAnimation != null) {
                    var pressLayer = StructuredData.Create<OverlappingAnimationData>();
                    pressLayer.Animation = d.FingerPressAnimation;
                    pressLayer.Weight = 1f;
                    pressLayer.Speed = 1f;
                    pressLayer.Masked = true;
                    pressLayer.MaskedBodyParts = new AnimationMaskedBodyPart[] {
                        AnimationMaskedBodyPart.RightFingers,
                        AnimationMaskedBodyPart.LeftFingers
                    };
                    pressLayer.Additive = true;
                    pressLayer.Looping = false;
                    pressLayer.CustomLayerID = d.PressLayerId;

                    userLayers.Add(pressLayer);
                }
            }

            if (LeftStickAnimationData?.Number != 0) {
                var layerPosX = StructuredData.Create<OverlappingAnimationData>();
                layerPosX.Animation = LeftStickAnimationData.PositiveXAnimation;
                layerPosX.Weight = 1f;
                layerPosX.Speed = 1f;
                layerPosX.Masked = true;
                layerPosX.MaskedBodyParts = new AnimationMaskedBodyPart[] {
                    AnimationMaskedBodyPart.LeftFingers
                };
                layerPosX.Additive = true;
                layerPosX.Looping = false;
                layerPosX.CustomLayerID = LeftStickAnimationData.PositiveXLayerId;
                userLayers.Add(layerPosX);

                var layerNegX = StructuredData.Create<OverlappingAnimationData>();
                layerNegX.Animation = LeftStickAnimationData.NegativeXAnimation;
                layerNegX.Weight = 1f;
                layerNegX.Speed = 1f;
                layerNegX.Masked = true;
                layerNegX.MaskedBodyParts = new AnimationMaskedBodyPart[] {
                    AnimationMaskedBodyPart.LeftFingers
                };
                layerNegX.Additive = true;
                layerNegX.Looping = false;
                layerNegX.CustomLayerID = LeftStickAnimationData.NegativeXLayerId;
                userLayers.Add(layerNegX);

                var layerPosY = StructuredData.Create<OverlappingAnimationData>();
                layerPosY.Animation = LeftStickAnimationData.PositiveYAnimation;
                layerPosY.Weight = 1f;
                layerPosY.Speed = 1f;
                layerPosY.Masked = true;
                layerPosY.MaskedBodyParts = new AnimationMaskedBodyPart[] {
                    AnimationMaskedBodyPart.LeftFingers
                };
                layerPosY.Additive = true;
                layerPosY.Looping = false;
                layerPosY.CustomLayerID = LeftStickAnimationData.PositiveYLayerId;
                userLayers.Add(layerPosY);

                var layerNegY = StructuredData.Create<OverlappingAnimationData>();
                layerNegY.Animation = LeftStickAnimationData.NegativeYAnimation;
                layerNegY.Weight = 1f;
                layerNegY.Speed = 1f;
                layerNegY.Masked = true;
                layerNegY.MaskedBodyParts = new AnimationMaskedBodyPart[] {
                    AnimationMaskedBodyPart.LeftFingers
                };
                layerNegY.Additive = true;
                layerNegY.Looping = false;
                layerNegY.CustomLayerID = LeftStickAnimationData.NegativeYLayerId;
                userLayers.Add(layerNegY);
            }

            if (RightStickAnimationData?.Number != 0) {
                var layerPosX = StructuredData.Create<OverlappingAnimationData>();
                layerPosX.Animation = RightStickAnimationData.PositiveXAnimation;
                layerPosX.Weight = 1f;
                layerPosX.Speed = 1f;
                layerPosX.Masked = true;
                layerPosX.MaskedBodyParts = new AnimationMaskedBodyPart[] {
                    AnimationMaskedBodyPart.RightFingers
                };
                layerPosX.Additive = true;
                layerPosX.Looping = false;
                layerPosX.CustomLayerID = RightStickAnimationData.PositiveXLayerId;
                userLayers.Add(layerPosX);

                var layerNegX = StructuredData.Create<OverlappingAnimationData>();
                layerNegX.Animation = RightStickAnimationData.NegativeXAnimation;
                layerNegX.Weight = 1f;
                layerNegX.Speed = 1f;
                layerNegX.Masked = true;
                layerNegX.MaskedBodyParts = new AnimationMaskedBodyPart[] {
                    AnimationMaskedBodyPart.RightFingers
                };
                layerNegX.Additive = true;
                layerNegX.Looping = false;
                layerNegX.CustomLayerID = RightStickAnimationData.NegativeXLayerId;
                userLayers.Add(layerNegX);

                var layerPosY = StructuredData.Create<OverlappingAnimationData>();
                layerPosY.Animation = RightStickAnimationData.PositiveYAnimation;
                layerPosY.Weight = 1f;
                layerPosY.Speed = 1f;
                layerPosY.Masked = true;
                layerPosY.MaskedBodyParts = new AnimationMaskedBodyPart[] {
                    AnimationMaskedBodyPart.RightFingers
                };
                layerPosY.Additive = true;
                layerPosY.Looping = false;
                layerPosY.CustomLayerID = RightStickAnimationData.PositiveYLayerId;
                userLayers.Add(layerPosY);

                var layerNegY = StructuredData.Create<OverlappingAnimationData>();
                layerNegY.Animation = RightStickAnimationData.NegativeYAnimation;
                layerNegY.Weight = 1f;
                layerNegY.Speed = 1f;
                layerNegY.Masked = true;
                layerNegY.MaskedBodyParts = new AnimationMaskedBodyPart[] {
                    AnimationMaskedBodyPart.RightFingers
                };
                layerNegY.Additive = true;
                layerNegY.Looping = false;
                layerNegY.CustomLayerID = RightStickAnimationData.NegativeYLayerId;
                userLayers.Add(layerNegY);
            }

            UnityEngine.Debug.Log("[GamepadReceiverAsset.Animation] Final layers");
            foreach (var l in userLayers) {
                UnityEngine.Debug.Log($"{l.Id} {l.CustomLayerID}");
            }

            // Plug-in Mode
            Character.DataInputPortCollection.SetValueAtPath($"{nameof(Character.OverlappingAnimations)}", userLayers.ToArray(), true);
            // Playground Mode
            // Character.OverlappingAnimations = userLayers.ToArray();
            // Character.BroadcastDataInput(nameof(Character.OverlappingAnimations));

            Context.Service.PromptMessage("SUCCESS", "Character Overlaying Animations has been synced.");
        }

        public Animancer.AnimancerComponent CharacterAnimancer {
            get {
                return Character?.Animancer;
            }
        }

        public Animancer.AnimancerComponent CharacterAnimancerClone {
            get {
                return Character?.CloneAnimancer;
            }
        }

        public void ApplyAnimancerPropertyWeight(
            int idx,
            float weight
        ) {
            ApplyAnimancerPropertyWeightLow(CharacterAnimancer, idx, weight);
            ApplyAnimancerPropertyWeightLow(CharacterAnimancerClone, idx, weight);
        }

        void ApplyAnimancerPropertyWeightLow(
            Animancer.AnimancerComponent animancer,
            int idx,
            float weight
        ) {
            animancer.Layers[idx].SetWeight(weight);
        }

        OverlappingAnimationData CreateIdleFingerAnimationData() {
            var layer = StructuredData.Create<OverlappingAnimationData>();
            layer.Animation = IdleFingerAnimation;
            layer.Weight = 1f;
            layer.Speed = 1f;
            layer.Masked = true;
            layer.MaskedBodyParts = new AnimationMaskedBodyPart[] {
                AnimationMaskedBodyPart.RightFingers,
                AnimationMaskedBodyPart.LeftFingers,
            };
            layer.Additive = false;
            layer.Looping = false;
            layer.CustomLayerID = LAYER_NAME_IDLE;

            return layer;
        }

        public class GamepadButtonAnimationData : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [PreviewGallery]
            [AutoCompleteResource("CharacterAnimation", null)]
            public string FingerHoverAnimation;

            [DataInput]
            [PreviewGallery]
            [AutoCompleteResource("CharacterAnimation", null)]
            public string FingerPressAnimation;

            [DataInput]
            [Label("Right side mask")]
            public bool MaskRight;

            [DataInput]
            public string PropLayerName;

            [Hidden]
            [DataInput]
            public GamepadType TargetControllerType;

            [Hidden]
            [DataInput]
            public int ButtonId;

            public string GetHeader() {
                string name;
                switch(TargetControllerType) {
                    case GamepadType.SwitchProController:
                    default:
                        name = $"{ButtonName} ({TargetControllerType})";
                        break;
                }
                if (ButtonId == 0) return "Invalid. Please Generate this list instead.";
                return name;
            }

            public string ButtonName {
                get {
                    switch(TargetControllerType) {
                        case GamepadType.SwitchProController:
                            return Enum.GetName(typeof(SwitchProButton), ButtonId);
                        case GamepadType.PS5Controller:
                            return Enum.GetName(typeof(PS5Button), ButtonId);
                        case GamepadType.Xbox360Controller:
                            return Enum.GetName(typeof(Xbox360Button), ButtonId);
                    }
                    return $"Button ID {ButtonId}";
                }
            }

            public string HoverLayerId {
                get {
                    return $"{LAYER_NAME_PREFIX} {ButtonName} Hover";
                }
            }

            public string PressLayerId {
                get {
                    return $"{LAYER_NAME_PREFIX} {ButtonName} Press";
                }
            }
        }

        public class GamepadDPadAnimationData : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [PreviewGallery]
            [AutoCompleteResource("CharacterAnimation", null)]
            public string FingerHoverAnimation;

            [DataInput]
            [PreviewGallery]
            [AutoCompleteResource("CharacterAnimation", null)]
            public string FingerPressAnimation;

            [DataInput]
            public string PropLayerName;

            [Hidden]
            [DataInput]
            public int ButtonId;

            public string GetHeader() {
                if (ButtonId == 0) return "Invalid. Please Generate this list instead.";
                return $"{ButtonName}";
            }

            public string ButtonName {
                get {
                    switch(ButtonId) {
                        case 1: return "↙️";
                        case 2: return "⬇️";
                        case 3: return "↘️";
                        case 4: return "⬅️";
                        case 6: return "➡️";
                        case 7: return "↖️";
                        case 8: return "⬆️";
                        case 9: return "↗️";
                    }

                    return Enum.GetName(typeof(DPadDirection), ButtonId);
                }
            }

            public string HoverLayerId {
                get {
                    return $"{LAYER_NAME_PREFIX} {ButtonName} Hover";
                }
            }

            public string PressLayerId {
                get {
                    return $"{LAYER_NAME_PREFIX} {ButtonName} Press";
                }
            }
        }

        public class GamepadStickAnimationData : StructuredData, ICollapsibleStructuredData {

            [DataInput]
            [PreviewGallery]
            [AutoCompleteResource("CharacterAnimation", null)]
            public string PositiveXAnimation;
            [DataInput]
            [PreviewGallery]
            [AutoCompleteResource("CharacterAnimation", null)]
            public string PositiveYAnimation;

            [DataInput]
            [PreviewGallery]
            [AutoCompleteResource("CharacterAnimation", null)]
            public string NegativeXAnimation;
            [DataInput]
            [PreviewGallery]
            [AutoCompleteResource("CharacterAnimation", null)]
            public string NegativeYAnimation;

            [DataInput]
            public string PropPositiveXLayerName;
            [DataInput]
            public string PropPositiveYLayerName;
            [DataInput]
            public string PropNegativeXLayerName;
            [DataInput]
            public string PropNegativeYLayerName;

            [Hidden]
            [DataInput]
            public int Number;

            public string ButtonName {
                get {
                    return $"Stick{Number}";
                }
            }

            public string GetHeader() {
                if (Number == 0) return "Invalid. Regenerate button to fix.";
                return $"{ButtonName}";
            }

            public string NegativeXLayerId {
                get {
                    return $"{LAYER_NAME_PREFIX} {ButtonName} -X";
                }
            }
            public string PositiveXLayerId {
                get {
                    return $"{LAYER_NAME_PREFIX} {ButtonName} +X";
                }
            }
            public string NegativeYLayerId {
                get {
                    return $"{LAYER_NAME_PREFIX} {ButtonName} -Y";
                }
            }
            public string PositiveYLayerId {
                get {
                    return $"{LAYER_NAME_PREFIX} {ButtonName} +Y";
                }
            }
        }
    }
}
