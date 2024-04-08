using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using UnityEngine;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Plugins.Core.Nodes;

namespace FlameStream {
    public abstract class MathExpression : StructuredData {
        [Markdown]
        public string CompilationError;

        [DataInput]
        [Label("EXPRESSION")]
        public string Expression;

        Delegate compiledExpression;
        List<string> variableList;
        Dictionary<string, ParameterExpression> variableMap;
        protected Dictionary<string, float> variableValueMap;

        protected override void OnCreate() {
            base.OnCreate();
            Watch(nameof(Expression), delegate { OnExpressionChange(); });
        }

        void OnExpressionChange() {
            if (Expression == null || Expression == "") {
                compiledExpression = null;
                CompilationError = null;
            } else {
                try {
                    variableList = ExtractVariableNames(Expression).Distinct().ToList();
                    variableMap = variableList.ToDictionary((string name) => name, (string name) => System.Linq.Expressions.Expression.Variable(typeof(float), name));
                    variableValueMap = new Dictionary<string, float>();

                    foreach (KeyValuePair<string, ParameterExpression> item in variableMap) {
                        if (!IsValidVariable(item.Key)) {
                            throw new InvalidOperationException("Variable '" + item.Key + "' is invalid.");
                        }
                        variableValueMap.Add(item.Key, 0);
                    }

                    LambdaExpression lambdaExpression = System.Linq.Expressions.Expression.Lambda(
                        new ExpressionParser(Expression, variableMap).Parse(),
                        variableMap.Values
                    );
                    compiledExpression = lambdaExpression.Compile();
                    CompilationError = null;
                } catch (Exception ex) {
                    compiledExpression = null;
                    CompilationError = $"⚠️ {ex.Message}";
                }
            }
            BroadcastDataInput(nameof(CompilationError));
        }

        protected float Evaluate() {
            if (compiledExpression == null) return 0;

            object[] array = new object[variableMap.Count];
            int num = 0;
            foreach (KeyValuePair<string, ParameterExpression> item in variableMap) {
                variableValueMap.TryGetValue(item.Key, out float n);
                array[num] = n;
                ++num;
            }

            var result = (float)compiledExpression.DynamicInvoke(array);
            OnAfterEvaluate(result);
            return result;
        }

        static IEnumerable<string> ExtractVariableNames(string formula) {
            MatchCollection matchCollection = Regex.Matches(formula, "\\b[a-zA-Z_][a-zA-Z0-9_]*\\b");
            foreach (Match item in matchCollection) {
                string value = item.Value;
                if (!ExpressionParser.Functions.ContainsKey(value) && !ExpressionParser.Constants.ContainsKey(value)) {
                    yield return value;
                }
            }
        }
        protected abstract bool IsValidVariable(string v);
        protected abstract string DebugMessage(float result);
        protected virtual void OnAfterEvaluate(float result) {}
    }

    public class DistanceInputMathExpression : MathExpression {

        protected override string DebugMessage(float result) {
            return $@"
*dx*: {variableValueMap["dx"]}

*dy*: {variableValueMap["dy"]}

*dz*: {variableValueMap["dz"]}

*Result*: {result}
";
        }

        protected override bool IsValidVariable(string v) {
            return v == "dx" || v == "dy" || v == "dz";
        }

        public float Evaluate(Vector3 v) {
            if (variableValueMap == null) return 0;
            variableValueMap["dx"] = v.x;
            variableValueMap["dy"] = v.y;
            variableValueMap["dz"] = v.z;
            return Evaluate();
        }
    }
}
