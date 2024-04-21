using System;
using System.Linq;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Scenes;
using Warudo.Plugins.Core.Assets;
using Warudo.Plugins.Core.Assets.Utility;
using uDesktopDuplication;
using DG.Tweening;
using Warudo.Core;

namespace FlameStream
{
    public partial class PointerReceiverAsset : ReceiverAsset {
        [DataInput]
        [Hidden]
        public Guid SurfaceAnchorAssetId;
        [DataInput]
        [Hidden]
        public Guid SurfaceScreenAssetId;
        [DataInput]
        [Hidden]
        public Guid CursorAnchorAssetId;

        public const string LAYER_NAME_PREFIX = "🔥👆";
        const float SCREEN_PIXEL_TO_WORLD_FACTOR = 0.00177f;
        static Vector3 IK_BASIS_LEFT_HANDED = new Vector3(0, -90f, -90f);
        static Vector3 IK_BASIS_RIGHT_HANDED = new Vector3(0, 90f, 90f);

        Monitor monitor;
        DrawingScreenAsset screenAsset;
        AnchorAsset cursorAnchorAsset;
        Vector2 cursorToScreenCenterOffset;
        Vector2 cursorToScreenScaleFactor;
        Tween cursorMoveTween;

        GameObject calculator;
        GameObject debugSphereCursorScreenPosition;
        GameObject debugSphereNeutralPosition;
        GameObject debugSphereCharacterPosition;
        GameObject debugSphereCharacterTargetPosition;
        GameObject debugSphereCharacterTargetBasePosition;
        Material debugSphereCursorScreenPositionMaterial;
        Material debugSphereNeutralPositionMaterial;
        Material debugSphereCharacterPositionMaterial;
        Material debugSphereCharacterTargetPositionMaterial;
        Material debugSphereCharacterTargetBasePositionMaterial;

        Vector3 cursorScreenPosition;

        bool isReady;
        int onReadyCountdown = 3;
        bool isDebugModeForced;
        bool inSetupMode;
        bool isBasicSetupComplete;

        protected override void OnCreate() {
            if (Port == 0) Port = DEFAULT_PORT;
            base.OnCreate();
            OnCreateBasicSetup();
            OnCreateMoveHand();
            OnCreateProp();
            OnCreateMoveBody();
        }

        protected override void OnDestroy() {
            base.OnDestroy();

            DestroyDebugSpheres(true);
            CleanDestroy(bodySetupRootAnchor);
            GameObject.Destroy(calculator);
        }

        public override void OnUpdate() {
            base.OnUpdate();

            if (Character == null) return;

            // OnReady: One-time execution before update events are sent
            // Delay if a few tick is needed because things are for settled
            if (onReadyCountdown > 0) {
                --onReadyCountdown;
                if (onReadyCountdown == 0) {
                    isReady = true;
                    OnReadyHand();
                    OnReadyBody();
                    OnReadyProp();
                };
            }

            OnUpdateState();
            OnUpdateBasicSetup();
            OnUpdateHand();
            OnUpdateProp();
            OnUpdateBody();
        }

        void OnCreateBasicSetup() {
            calculator = new GameObject();
            // Has to be before #OnBasicSettingChange
            Watch(nameof(DisplayName), delegate { OnSurfaceSettingChange(); });

            Watch(nameof(IsHandEnabled), delegate { OnBasicSettingChange(); });
            Watch(nameof(Character), delegate { OnBasicSettingChange(); });
            Watch(nameof(DisplayName), delegate { OnBasicSettingChange(); });
            OnBasicSettingChange();

            Watch(nameof(DebugSphereScaleFactor), delegate { ApplyDisplayPointerSphereScale(); });
        }

        void OnUpdateBasicSetup() {
            if (!IsHandEnabled) return;
            if (monitor == null) return;
            if (cursorAnchorAsset == null) return;

            var anchorTransform = cursorAnchorAsset.Transform;
            anchorTransform.Rotation = IsRightHanded
                ? IK_BASIS_RIGHT_HANDED
                : IK_BASIS_LEFT_HANDED;

            if (inSetupMode) {
                anchorTransform.Position = NeutralHandPosition.setupAnchor?.Transform.Position ?? NeutralHandPosition.Position;
            } else {
                // Follow pointer
                var targetPlanarPosition = new Vector3(
                    -(X - cursorToScreenCenterOffset.x) * screenAsset.Transform.Scale.x * SCREEN_PIXEL_TO_WORLD_FACTOR,
                    -(Y - cursorToScreenCenterOffset.y) * screenAsset.Transform.Scale.y * SCREEN_PIXEL_TO_WORLD_FACTOR,
                    0
                ) * PointerFactorCorrection;

                cursorMoveTween?.Kill();
                if (CursorSmoothness == 0) {
                    cursorScreenPosition = targetPlanarPosition;
                } else {
                    cursorMoveTween = DOTween.To(
                        () => cursorScreenPosition,
                        delegate(Vector3 it) { cursorScreenPosition = it; },
                        targetPlanarPosition,
                        CursorSmoothness
                    ).SetEase(Ease.Linear);
                }

                anchorTransform.Position = cursorScreenPosition;

                // TODO: Improve this by using hand offset delta
                Vector3 delta = IsBodyEnabled
                    ? bodyDistanceFactor
                    : NeutralHandPosition.Position - cursorAnchorAsset.Transform.Position;
                anchorTransform.Rotation += HandDynamicRotation.Evaluate(delta);
            }

            if (debugSphereNeutralPosition != null) {
                // Zero division protection
                if (screenAsset.Transform.Scale.x > 0 && screenAsset.Transform.Scale.x > 0 && screenAsset.Transform.Scale.x > 0) {
                    debugSphereNeutralPosition.gameObject.transform.localPosition = new Vector3(
                        NeutralHandPosition.Position.x / screenAsset.Transform.Scale.x,
                        NeutralHandPosition.Position.y / screenAsset.Transform.Scale.y,
                        NeutralHandPosition.Position.z / screenAsset.Transform.Scale.z
                    );
                }
            }

            anchorTransform.Broadcast();
        }

        protected override void Log(string msg) {
            Debug.Log($"[FlameStream.Asset.PointerReceiver] {msg}");
        }

        void OnBasicSettingChange() {
            GetDataInputPort(nameof(Character)).Properties.hidden = !IsHandEnabled;
            GetDataInputPort(nameof(IsRightHanded)).Properties.hidden = !IsHandEnabled;
            GetDataInputPort(nameof(DisplayName)).Properties.hidden = !IsHandEnabled;
            GetDataInputPort(nameof(CursorSmoothness)).Properties.hidden = !IsHandEnabled;
            BroadcastDataInputProperties(nameof(Character));
            BroadcastDataInputProperties(nameof(IsRightHanded));
            BroadcastDataInputProperties(nameof(DisplayName));
            BroadcastDataInputProperties(nameof(CursorSmoothness));

            isBasicSetupComplete = IsHandEnabled && Character != null && monitor != null;

            GetDataInputPort(nameof(NeutralHandPosition)).Properties.hidden = !isBasicSetupComplete;
            GetDataInputPort(nameof(HandMouseMode)).Properties.hidden = !isBasicSetupComplete;
            GetDataInputPort(nameof(HandPenMode)).Properties.hidden = !isBasicSetupComplete;
            GetDataInputPort(nameof(HandDynamicRotation)).Properties.hidden = !isBasicSetupComplete;
            BroadcastDataInputProperties(nameof(NeutralHandPosition));
            BroadcastDataInputProperties(nameof(HandMouseMode));
            BroadcastDataInputProperties(nameof(HandPenMode));
            BroadcastDataInputProperties(nameof(HandDynamicRotation));

            GetDataInputPort(nameof(IsPropEnabled)).Properties.hidden = !isBasicSetupComplete;
            BroadcastDataInputProperties(nameof(IsPropEnabled));
            OnPropEnabledChanged();

            GetDataInputPort(nameof(IsBodyEnabled)).Properties.hidden = !isBasicSetupComplete;
            BroadcastDataInputProperties(nameof(IsBodyEnabled));
            OnBodyEnabledChanged();

            GetDataInputPort(nameof(PointerFactorCorrection)).Properties.hidden = !isBasicSetupComplete;
            GetDataInputPort(nameof(DebugSphereScaleFactor)).Properties.hidden = !isBasicSetupComplete;
            BroadcastDataInputProperties(nameof(PointerFactorCorrection));
            BroadcastDataInputProperties(nameof(DebugSphereScaleFactor));
            OnDebugSettingChange();
        }

        void OnSurfaceSettingChange() {
            screenAsset = GetSurfaceScreen();
            screenAsset.DataInputPortCollection.SetValueAtPath("DisplayName", DisplayName, true);
            screenAsset.DataInputPortCollection.SetValueAtPath("Enabled", true, true);

            cursorAnchorAsset = GetCursorAnchor();
            cursorAnchorAsset.Attachable.Parent = screenAsset;

            monitor = Manager.monitors.FirstOrDefault((Monitor it) => it.name == DisplayName);
            if (monitor == null) return;

            cursorToScreenCenterOffset = new Vector2(
                (monitor.left + monitor.right) * 0.5f,
                (monitor.bottom + monitor.top) * 0.5f
            );
            cursorToScreenScaleFactor = new Vector2(
                monitor.widthMeter,
                monitor.heightMeter
            );
        }

        void OnDebugSettingChange() {
            GetTriggerPort(nameof(TriggerForceDebugSpheres)).Properties.hidden = isDebugModeForced || !isBasicSetupComplete;
            GetTriggerPort(nameof(TriggerDestroyDebugSpheres)).Properties.hidden = !isDebugModeForced || !isBasicSetupComplete;
            BroadcastTriggerProperties(nameof(TriggerForceDebugSpheres));
            BroadcastTriggerProperties(nameof(TriggerDestroyDebugSpheres));
        }

        public void EnterSetupModeMinimal() {
            if (inSetupMode) return;
            inSetupMode = true;
            ResetCharacterPosition();
            DisplayDebugSpheres();
        }

        public void TriggerOtherEnterSetupModes(int source, bool button1) {
            NeutralHandPosition.EnterVisualSetup(false);
            if (setupModeHandState == null) {
                GetHandState(source, button1)?.Transform.EnterVisualSetup(false);
            }
            if (setupModePropState == null) {
                GetPropState(source, button1)?.Transform.EnterVisualSetup(false);
            }
            if (setupModeBodyState == null) {
                GetBodyState(source, button1)?.Transform.EnterVisualSetup(false);
            }
        }

        public void ExitSetupModeMinimal() {
            if (!inSetupMode) return;
            inSetupMode = false;
            DestroyDebugSpheres();

            Context.Service.NavigateToAsset(Id);
            Context.Service.BroadcastOpenedScene();
        }

        void CleanDestroy(GameObjectAsset g) {
            if (g == null) return;
            try {
                Scene.RemoveAsset(g.Id);
            } catch {}
        }

        DrawingScreenAsset GetSurfaceScreen(bool skipAutoCreate = false) {
            var screen = GetAsset<DrawingScreenAsset>(ref SurfaceScreenAssetId, out bool isNewlyCreated, skipAutoCreate, newAssetName: $"📦-{LAYER_NAME_PREFIX} Screen");
            if (isNewlyCreated) {
                Vector3 position;
                Vector3 rotation;
                var transform = screen.Transform;
                if (Character == null) {
                    position = new Vector3(0, 0.6f, 1f);
                    rotation = new Vector3(-60f, 180f, 0);
                } else {
                    position = Character.CloneAnimator.GetBoneTransform(HumanBodyBones.Chest).position;
                    position += Character.GameObject.transform.forward * 0.25f;
                    rotation = Character.Transform.Rotation;
                    rotation.y += 180f;
                    rotation.x -= 60f;
                }
                transform.Position = position;
                transform.Rotation = rotation;
                transform.Scale = new Vector3(0.07f, 0.07f, 0.07f);
                transform.Broadcast();
            }
            return screen;
        }

        AnchorAsset GetCursorAnchor(bool skipAutoCreate = false) {
            return GetAsset<AnchorAsset>(ref CursorAnchorAssetId, skipAutoCreate, newAssetName: $"🔒⚓-{LAYER_NAME_PREFIX}🎯");
        }

        T GetAsset<T>(ref Guid assetId, out bool isNewlyCreated, bool skipAutoCreate = false, string newAssetName = null) where T : Asset  {
            isNewlyCreated = false;
            if (assetId != Guid.Empty) {
                var id = assetId;
                var fetchResult = Scene.GetAssets<T>().FirstOrDefault(p => p.Id == id);
                if (fetchResult != null) {
                    return fetchResult;
                }
            }

            if (skipAutoCreate) return null;

            var newAsset = Scene.AddAsset<T>();
            newAsset.Name = newAssetName ?? $"📦-{LAYER_NAME_PREFIX} {nameof(T)}";
            Scene.UpdateNewAssetName(newAsset);
            newAsset.Broadcast();
            assetId = newAsset.Id;

            isNewlyCreated = true;
            return newAsset;
        }

        T GetAsset<T>(ref Guid assetId, bool skipAutoCreate = false, string newAssetName = null) where T : Asset  {
            return GetAsset<T>(ref assetId, out bool _isNewlyCreated, skipAutoCreate, newAssetName);
        }

        GameObject GetDebugSphereCursorPosition(bool skipAutoCreate = false) {
            if (debugSphereCursorScreenPosition) return debugSphereCursorScreenPosition;
            if (skipAutoCreate) return null;

            var s = CreateDebugSphere(
                ref debugSphereCursorScreenPosition,
                ref debugSphereCursorScreenPositionMaterial,
                new Color(1f, 0f, 0f, 0.8f)
            );
            s.transform.position = Vector3.zero;
            s.transform.SetParent(cursorAnchorAsset.GameObject.transform, false);
            return s;
        }

        GameObject GetDebugSphereNeutralPosition(bool skipAutoCreate = false) {
            if (debugSphereNeutralPosition) return debugSphereNeutralPosition;
            if (skipAutoCreate) return null;

            var s = CreateDebugSphere(
                ref debugSphereNeutralPosition,
                ref debugSphereNeutralPositionMaterial,
                Color.yellow
            );
            s.transform.position = Vector3.zero;
            s.transform.SetParent(screenAsset.GameObject.transform, false);
            return s;
        }

        GameObject GetDebugSphereCharacterPosition(bool skipAutoCreate = false) {
            if (debugSphereCharacterPosition) return debugSphereCharacterPosition;
            if (skipAutoCreate) return null;

            var s = CreateDebugSphere(
                ref debugSphereCharacterPosition,
                ref debugSphereCharacterPositionMaterial,
                Color.green
            );
            s.transform.position = Vector3.zero;
            s.transform.SetParent(Character.GameObject.transform, false);
            return s;
        }

        GameObject GetDebugSphereCharacterTargetPosition(bool skipAutoCreate = false) {
            if (debugSphereCharacterTargetPosition) return debugSphereCharacterTargetPosition;
            if (skipAutoCreate) return null;

            var s = CreateDebugSphere(
                ref debugSphereCharacterTargetPosition,
                ref debugSphereCharacterTargetPositionMaterial,
                new Color(1f, 0f, 1f, 0.5f)
            );
            s.transform.position = Vector3.zero;
            s.transform.SetParent(cursorAnchorAsset.GameObject.transform, false);
            return s;
        }

        GameObject GetDebugSphereCharacterTargetBasePosition(bool skipAutoCreate = false) {
            if (debugSphereCharacterTargetBasePosition) return debugSphereCharacterTargetBasePosition;
            if (skipAutoCreate) return null;

            var s = CreateDebugSphere(
                ref debugSphereCharacterTargetBasePosition,
                ref debugSphereCharacterTargetBasePositionMaterial,
                Color.blue
            );
            s.transform.position = Vector3.zero;
            s.transform.SetParent(cursorAnchorAsset.GameObject.transform, false);
            return s;
        }

        GameObject CreateDebugSphere(ref GameObject go, ref Material m, Color c) {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            m = new Material(Shader.Find("Standard"));
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c);
            m.SetFloat("_EmissionIntensity", 1.0f);
            m.color = c;
            m.SetFloat("_Mode", 3); // Unlit
            go.GetComponent<Renderer>().material = m;

            return go;
        }

        void DisplayDebugSpheres(bool force = false) {
            if (force) isDebugModeForced = true;
            GetDebugSphereCursorPosition();
            GetDebugSphereNeutralPosition();
            GetDebugSphereCharacterPosition();
            GetDebugSphereCharacterTargetPosition();
            GetDebugSphereCharacterTargetBasePosition();
            ApplyDisplayPointerSphereScale();
        }

        void ApplyDisplayPointerSphereScale() {
            var screenScale = (screenAsset != null) ? screenAsset.Transform.Scale : Vector3.one;
            var baseScaleFactor = 0.2f * DebugSphereScaleFactor;
            var scale = screenScale * baseScaleFactor;
            var scale2 = Vector3.one * baseScaleFactor;
            if (debugSphereCursorScreenPosition != null) {
                debugSphereCursorScreenPosition.gameObject.transform.localScale = scale;
            }
            if (debugSphereNeutralPosition != null) {
                debugSphereNeutralPosition.gameObject.transform.localScale = scale2 * 0.99f;
            }
            if (debugSphereCharacterPosition != null) {
                debugSphereCharacterPosition.gameObject.transform.localScale = scale;
            }
            if (debugSphereCharacterTargetPosition != null) {
                debugSphereCharacterTargetPosition.gameObject.transform.localScale = scale * 0.99f;
            }
            if (debugSphereCharacterTargetBasePosition != null) {
                debugSphereCharacterTargetBasePosition.gameObject.transform.localScale = scale * 0.98f;
            }
        }

        void DestroyDebugSpheres(bool force = false) {
            if (isDebugModeForced && !force) return;

            isDebugModeForced = false;

            GameObject.Destroy(debugSphereCursorScreenPosition);
            Material.Destroy(debugSphereCursorScreenPositionMaterial);
            GameObject.Destroy(debugSphereNeutralPosition);
            Material.Destroy(debugSphereNeutralPositionMaterial);
            GameObject.Destroy(debugSphereCharacterPosition);
            Material.Destroy(debugSphereCharacterPositionMaterial);
            GameObject.Destroy(debugSphereCharacterTargetPosition);
            Material.Destroy(debugSphereCharacterTargetPositionMaterial);
            GameObject.Destroy(debugSphereCharacterTargetBasePosition);
            Material.Destroy(debugSphereCharacterTargetBasePositionMaterial);
        }

        Transform GetGlobalCalculatorTransform() {
            var tr = calculator.transform;
            tr.position = Vector3.zero;
            tr.rotation = Quaternion.identity;
            tr.localScale = Vector3.one;
            return tr;
        }
    }
}
