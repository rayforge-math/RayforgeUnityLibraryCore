using System;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Rayforge.Core.Utility.VolumeComponents.Editor
{
    public static class ExpressionEvaluator
    {
        private static readonly Regex TokenRegex = new Regex(@"([a-zA-Z_][a-zA-Z0-9_]*)|(==|!=|>=|<=|&&|\|\||[<>!=()+-/*])", RegexOptions.Compiled);

        public static bool Evaluate(string expression, Func<string, object> getValue)
        {
            if (string.IsNullOrWhiteSpace(expression)) return true;

            try
            {
                string processed = TokenRegex.Replace(expression, match =>
                {
                    string token = match.Value;

                    if (Regex.IsMatch(token, @"^[a-zA-Z_]"))
                    {
                        if (token == "true" || token == "false" || token == "AND" || token == "OR" || token == "NOT")
                            return token;

                        object val = getValue(token);
                        return FormatValueForExpression(val);
                    }

                    return token switch
                    {
                        "&&" => " AND ",
                        "||" => " OR ",
                        "==" => " = ",
                        "!=" => " <> ",
                        "!" => " NOT ",
                        _ => token
                    };
                });

                using (var table = new DataTable())
                {
                    var result = table.Compute(processed, "");
                    return result is bool b && b;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConditionalExpr] Syntax Error: {expression} -> {e.Message}");
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
            if (val.GetType().IsEnum) return ((int)val).ToString();

            return val.ToString();
        }
    }
}