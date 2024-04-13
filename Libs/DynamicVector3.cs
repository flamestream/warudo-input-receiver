using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;

namespace FlameStream {
    public class DynamicVector3 : StructuredData {

        [DataInput]
        [Label("ENABLE")]
        public bool Enabled;
        [DataInput]
        [Hidden]
        public DistanceInputMathExpression X;
        [DataInput]
        [Hidden]
        public DistanceInputMathExpression Y;
        [DataInput]
        [Hidden]
        public DistanceInputMathExpression Z;

        public Vector3 Evaluate(Vector3 v) {
            var result = Enabled
                ? new Vector3(
                    X.Evaluate(v),
                    Y.Evaluate(v),
                    Z.Evaluate(v)
                )
                : Vector3.zero;
            if (debugMode) {
                DebugMessage = $@"
dx: {v.x:F3}

dy: {v.y:F3}

dz: {v.z:F3}

result: {result}
";
                BroadcastDataInput(nameof(DebugMessage));
            }
            return result;
        }

        [Markdown]
        public string DebugMessage;

        [Trigger]
        [Hidden]
        [Label("TOGGLE_DEBUG_MODE")]
        public void ToggleDebugModeTrigger() {
            debugMode = !debugMode;
            if (!debugMode) {
                DebugMessage = null;
                BroadcastDataInput(nameof(DebugMessage));
            }
        }
        bool debugMode;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Enabled), delegate { OnEnabledChanged(); });
        }

        void OnEnabledChanged() {
            GetDataInputPort(nameof(X)).Properties.hidden = !Enabled;
            GetDataInputPort(nameof(Y)).Properties.hidden = !Enabled;
            GetDataInputPort(nameof(Z)).Properties.hidden = !Enabled;
            GetDataInputPort(nameof(DebugMessage)).Properties.hidden = !Enabled;
            GetTriggerPort(nameof(ToggleDebugModeTrigger)).Properties.hidden = !Enabled;

            BroadcastDataInputProperties(nameof(X));
            BroadcastDataInputProperties(nameof(Y));
            BroadcastDataInputProperties(nameof(Z));
            BroadcastDataInputProperties(nameof(DebugMessage));
            BroadcastTriggerProperties(nameof(ToggleDebugModeTrigger));
        }
    }

    public class HandRotationDynamicVector3 : DynamicVector3 {
        public HandRotationDynamicVector3() {
            Z = StructuredData.Create<DistanceInputMathExpression>((me) => {
                me.Expression = "min(2 * abs(dx), 1) * (atan2(abs(dy), abs(dx)) * (180 / pi) - 90) * -sign(dx)";
            });
            Z.OnExpressionChange();
        }
    }

    public class BodyRotationDynamicVector3 : DynamicVector3 {
        public BodyRotationDynamicVector3() {
            Z = StructuredData.Create<DistanceInputMathExpression>((me) => {
                me.Expression = "min(0.15 * abs(dx), 1) * atan2(-abs(dx), abs(dy)) * 4 / pi * (180 / pi) * sign(dx)";
            });
            Z.OnExpressionChange();
        }
    }
}
