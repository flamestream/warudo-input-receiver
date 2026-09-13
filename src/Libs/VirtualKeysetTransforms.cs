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
            if (Left != null) Left.CollapsedSelf = true;
            if (Right != null) Right.CollapsedSelf = true;
            if (Root != null) Root.CollapsedSelf = true;
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

        public Vector3 TransformLocalToRig(Vector3 deltaLocal) {
            Vector3 scaled = Vector3.Scale(deltaLocal, OriginScale);
            return ClusterOriginRig + ClusterRotRig * scaled;
        }

        public Quaternion TransformRotationToRig(Quaternion rotLocal) {
            return ClusterRotRig * rotLocal;
        }

        public void InverseTransformFromRig(Vector3 rigPos, Quaternion rigRot, out Vector3 deltaLocal, out Quaternion rotLocal) {
            Quaternion invRot = Quaternion.Inverse(ClusterRotRig);
            Vector3 unrotated = invRot * (rigPos - ClusterOriginRig);
            deltaLocal = new Vector3(
                Mathf.Abs(OriginScale.x) > 0.00001f ? unrotated.x / OriginScale.x : unrotated.x,
                Mathf.Abs(OriginScale.y) > 0.00001f ? unrotated.y / OriginScale.y : unrotated.y,
                Mathf.Abs(OriginScale.z) > 0.00001f ? unrotated.z / OriginScale.z : unrotated.z
            );
            rotLocal = invRot * rigRot;
        }
    }
}
