using UnityEngine;

namespace Rayforge.Core.Maths.Vector
{
    public static class VectorMath
    {
        public static Vector2 DegreeToVector(float degree)
        {
            float rad = degree * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public static float VectorToDegree(Vector2 direction)
        {
            return Mathf.Repeat(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, 360f);
        }
    }
}
