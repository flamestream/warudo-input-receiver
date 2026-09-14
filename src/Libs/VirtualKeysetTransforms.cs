using System;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Localization;

namespace FlameStream
{
    public class VirtualKeysetTransforms : StructuredData<KeyboardReceiverAsset>, ICollapsibleStructuredData
    {
        [DataInput(10)]
        [Label("CENTER_KEY_ORIGIN")]
        [Description("CENTER_KEY_ORIGIN_DESC")]
        public CollapsibleVisualSetupTransform Root;

        [DataInput(20)]
        [Label("LEFT_KEY_ORIGIN")]
        [Description("LEFT_KEY_ORIGIN_DESC")]
        public CollapsibleVisualSetupTransform Left;

        [DataInput(30)]
        [Label("RIGHT_KEY_ORIGIN")]
        [Description("RIGHT_KEY_ORIGIN_DESC")]
        public CollapsibleVisualSetupTransform Right;

        public string GetHeader() => "KEY_ORIGINS".Localized();

        protected override void OnCreate() {
            base.OnCreate();
            CollapsedSelf = true;
            if (Root == null) {
                Root = StructuredData.Create<CollapsibleVisualSetupTransform>();
                Root.Position = Vector3.zero;
                Root.Rotation = Vector3.zero;
                Root.Scale = Vector3.one;
            }
            Root.CollapsedSelf = true;

            if (Left == null) {
                Left = StructuredData.Create<CollapsibleVisualSetupTransform>();
                Left.Position = Vector3.zero;
                Left.Rotation = Vector3.zero;
                Left.Scale = Vector3.one;
            }
            Left.CollapsedSelf = true;

            if (Right == null) {
                Right = StructuredData.Create<CollapsibleVisualSetupTransform>();
                Right.Position = Vector3.zero;
                Right.Rotation = Vector3.zero;
                Right.Scale = Vector3.one;
            }
            Right.CollapsedSelf = true;

            Watch(nameof(Root), delegate {
                Parent?.SetupRootKeysetTransform();
            });
            Watch(nameof(Left), delegate {
                Parent?.SetupLeftKeysetTransform();
            });
            Watch(nameof(Right), delegate {
                Parent?.SetupRightKeysetTransform();
            });
        }
    }

    public struct KeyClusterTransform
    {
        public Vector3 BasePivotRig;
        public Quaternion BaseRotRig;
        public Vector3 OriginPos;
        public Quaternion OriginRot;
        public Vector3 OriginScale;

        public Vector3 ClusterOriginRig => BasePivotRig + BaseRotRig * OriginPos;
        public Quaternion ClusterRotRig => BaseRotRig * OriginRot;

        public Vector3 TransformLocalToRig(Vector3 deltaLocal, Vector3 keyLocalPos) {
            Vector3 adjustedLocal = deltaLocal + Vector3.Scale(OriginScale - Vector3.one, keyLocalPos);
            return ClusterOriginRig + ClusterRotRig * adjustedLocal;
        }

        public Vector3 TransformLocalToRig(Vector3 deltaLocal) {
            return TransformLocalToRig(deltaLocal, deltaLocal);
        }

        public Quaternion TransformRotationToRig(Quaternion rotLocal) {
            return ClusterRotRig * rotLocal;
        }

        public void InverseTransformFromRig(Vector3 rigPos, Quaternion rigRot, Vector3 keyLocalPos, out Vector3 deltaLocal, out Quaternion rotLocal) {
            Quaternion invRot = Quaternion.Inverse(ClusterRotRig);
            Vector3 adjustedLocal = invRot * (rigPos - ClusterOriginRig);
            deltaLocal = adjustedLocal - Vector3.Scale(OriginScale - Vector3.one, keyLocalPos);
            rotLocal = invRot * rigRot;
        }

        public void InverseTransformFromRig(Vector3 rigPos, Quaternion rigRot, out Vector3 deltaLocal, out Quaternion rotLocal) {
            InverseTransformFromRig(rigPos, rigRot, Vector3.zero, out deltaLocal, out rotLocal);
        }
    }
}
