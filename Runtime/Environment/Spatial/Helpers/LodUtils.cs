using System;

namespace Rayforge.Core.Environment.Spatial.Helpers
{
    public static class LodUtils
    {
        /// <summary>
        /// Ermittelt das Ziel-LOD basierend auf der quadrierten Distanz.
        /// </summary>
        /// <param name="sqrDistance">Die quadrierte Distanz zum Viewer.</param>
        /// <param name="sqrThresholds">Ein Span der quadrierten Distanz-Schwellenwerte.</param>
        /// <returns>Der Index des LOD-Levels oder -1, wenn außerhalb der Reichweite.</returns>
        public static int CalculateTargetLOD(float sqrDistance, ReadOnlySpan<float> sqrThresholds)
        {
            for (int i = 0; i < sqrThresholds.Length; i++)
            {
                if (sqrDistance < sqrThresholds[i]) return i;
            }
            return -1;
        }
    }
}
