using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Rayforge.Core.Utility.VolumeComponents.Editor
{
    public static class ExpressionEvaluator
    {
        private static readonly Regex VariableRegex = new Regex(@"([a-zA-Z_][a-zA-Z0-9_]*)", RegexOptions.Compiled);

        public static bool Evaluate(string expression, Func<string, object> getValue)
        {
            if (string.IsNullOrWhiteSpace(expression)) return true;

            try
            {
                string processed = VariableRegex.Replace(expression, match =>
                {
                    string name = match.Value;

                    if (name == "true" || name == "false" || name == "AND" || name == "OR")
                        return name;

                    object val = getValue(name);
                    return FormatValueForExpression(val);
                });

                processed = processed.Replace("&&", " AND ")
                                     .Replace("||", " OR ")
                                     .Replace("==", " = ")
                                     .Replace("!=", " <> ");

                using (var table = new DataTable())
                {
                    var result = table.Compute(processed, "");
                    return result is bool b && b;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConditionalExpr] Evaluation Error: {e.Message} in Expression: {expression}");
                return true;
            }
        }

        private static string FormatValueForExpression(object val)
        {
            if (val == null) return "0";
            if (val is bool b) return b ? "true" : "false";
            if (val is string s) return $"'{s}'";
            if (val is float f) return f.ToString(CultureInfo.InvariantCulture);
            if (val is double d) return d.ToString(CultureInfo.InvariantCulture);

            return val.ToString();
        }
    }
}