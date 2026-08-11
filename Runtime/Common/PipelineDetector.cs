using UnityEngine.Rendering;

namespace Rayforge.Core.Common
{
    /// <summary>
    /// Detects which Scriptable Render Pipeline (SRP) is currently active at runtime.
    /// Supports HDRP and URP.
    /// </summary>
    public static class PipelineDetector
    {
        private static bool s_checked = false;
        private static bool s_isHDRP = false;
        private static bool s_isURP = false;
        private static bool s_isBuiltin = false;

        /// <summary>
        /// Returns true if the High Definition Render Pipeline (HDRP) is active.
        /// </summary>
        /// <returns><c>true</c> if HDRP is active; otherwise, <c>false</c>.</returns>
        public static bool IsHDRP
        {
            get
            {
                EnsureChecked();
                return s_isHDRP;
            }
        }

        /// <summary>
        /// Returns true if the Universal Render Pipeline (URP) is active.
        /// </summary>
        /// <returns><c>true</c> if URP is active; otherwise, <c>false</c>.</returns>
        public static bool IsURP
        {
            get
            {
                EnsureChecked();
                return s_isURP;
            }
        }

        /// <summary>
        /// Returns true if the legacy Built-in Render Pipeline is active.
        /// </summary>
        public static bool IsBuiltin 
        { 
            get 
            { 
                EnsureChecked(); 
                return s_isBuiltin; 
            } 
        }

        /// <summary>
        /// Forces re-detection of the currently active Scriptable Render Pipeline.
        /// </summary>
        /// <param name="force">
        /// If <c>true</c>, re-checks the active pipeline even if detection was already performed.
        /// </param>
        public static void Detect(bool force = false)
        {
            if (!s_checked || force)
            {
                var rp = GraphicsSettings.currentRenderPipeline;

                s_isHDRP = false;
                s_isURP = false;
                s_isBuiltin = false;

                if (rp != null)
                {
                    string name = rp.GetType().Name;
                    if (name.Contains("HDRenderPipeline"))
                        s_isHDRP = true;
                    else if (name.Contains("UniversalRenderPipeline"))
                        s_isURP = true;
                }
                else
                {
                    s_isBuiltin = true;
                }

                s_checked = true;
            }
        }

        /// <summary>
        /// Resets the detection state. The next access to a property will trigger a re-detection.
        /// </summary>
        public static void Reset()
        {
            s_checked = false;
        }

        /// <summary>
        /// Ensures that the pipeline detection has been performed before accessing <see cref="IsHDRP"/> or <see cref="IsURP"/>.
        /// </summary>
        private static void EnsureChecked()
        {
            if (!s_checked)
                Detect();
        }
    }
}