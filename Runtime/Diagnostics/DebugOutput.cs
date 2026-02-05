using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Diagnostics
{
    public static class DebugOutput
    {
        private const string DEFAULT_COLOR = "#4FC3F7";

        [Conditional("UNITY_EDITOR")]
        public static void Log(
            string message,
            bool isEnabled,
            string htmlColor = DEFAULT_COLOR,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!isEnabled) return;

            string className = Path.GetFileNameWithoutExtension(filePath);
            UnityEngine.Debug.Log($"<color={htmlColor}><b>[{className}:{lineNumber}]</b></color> {message}");
        }
    }
}
