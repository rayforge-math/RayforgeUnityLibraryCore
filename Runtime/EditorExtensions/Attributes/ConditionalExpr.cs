using UnityEngine;

namespace Rayforge.Core.EditorExtensions.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class ConditionalExpr : PropertyAttribute
    {
        public readonly string Expression;
        public readonly DrawMode DrawMode;

        public ConditionalExpr(string expression, DrawMode drawMode = DrawMode.Disabled)
        {
            this.Expression = expression;
            this.DrawMode = drawMode;
        }
    }
}
