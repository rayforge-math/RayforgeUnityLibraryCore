using UnityEngine;

namespace Rayforge.Core.Rendering.Helpers
{
    /// <summary>
    /// Provides globally accessible fallback textures for use in both runtime and editor contexts.
    /// In editor windows, these textures remain valid across play mode changes, unlike Unity's built-in textures.
    /// In runtime, they serve as reliable single-pixel textures for shader inputs or placeholder visuals.
    /// </summary>
    public static class FallbackTextures
    {
        /// <summary>
        /// Cached 1x1 black texture instance.
        /// </summary>
        private static Texture2D _black;

        /// <summary>
        /// Cached 1x1 white texture instance.
        /// </summary>
        private static Texture2D _white;

        /// <summary>
        /// Gets a 1x1 black texture that persists across play mode changes.
        /// The texture is lazily initialized on first access.
        /// </summary>
        /// <value>A 1x1 black RGBA32 texture.</value>
        public static Texture2D Black
        {
            get
            {
                if (_black == null)
                {
                    _black = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    _black.SetPixel(0, 0, Color.black);
                    _black.Apply();
                }
                return _black;
            }
        }

        /// <summary>
        /// Gets a 1x1 white texture that persists across play mode changes.
        /// The texture is lazily initialized on first access.
        /// </summary>
        /// <value>A 1x1 white RGBA32 texture.</value>
        public static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    _white.SetPixel(0, 0, Color.white);
                    _white.Apply();
                }
                return _white;
            }
        }
    }
}
