using UnityEngine;

namespace Rayforge.Core.Environment.Spatial.Helpers
{
    /// <summary>
    /// Professional spatial utility for grid-based world generation.
    /// Handles 1D, 2D, and 3D conversions with support for custom anchors (origins).
    /// All calculations use Floor-logic (Key 0 = 0 to Size) for mathematical stability.
    /// </summary>
    public static class SpatialUtils
    {
        #region 1D CORE LOGIC

        /// <summary>
        /// Converts a 1D position to a grid key using Floor-logic.
        /// Formula: floor((position - anchor) / size)
        /// </summary>
        /// <param name="position">World space coordinate.</param>
        /// <param name="gridSize">The physical size of one chunk.</param>
        /// <param name="anchor">The origin offset (e.g., -gridSize/2 to center the grid).</param>
        /// <returns>The integer key of the cell.</returns>
        public static int PositionToKey1D(float position, float gridSize, float anchor = 0)
        {
            return Mathf.FloorToInt((position - anchor) / gridSize);
        }

        /// <summary>
        /// Converts a 1D grid key back to a world position.
        /// </summary>
        /// <param name="key">The integer grid key.</param>
        /// <param name="gridSize">The physical size of one chunk.</param>
        /// <param name="anchor">The origin offset.</param>
        /// <param name="centered">If true, returns the center of the cell. If false, returns the minimum corner.</param>
        /// <returns>The world space coordinate.</returns>
        public static float KeyToPosition1D(int key, float gridSize, float anchor = 0, bool centered = false)
        {
            float pos = key * gridSize + anchor;
            if (centered) pos += gridSize * 0.5f;
            return pos;
        }

        #endregion

        #region 2D CONVERSIONS

        /// <summary> Maps a 3D world position to a 2D grid key (X and Z axes). </summary>
        public static Vector2Int PositionToKey2D(Vector3 position, float gridSize, Vector3 anchor = default)
        {
            return new Vector2Int(
                PositionToKey1D(position.x, gridSize, anchor.x),
                PositionToKey1D(position.z, gridSize, anchor.z)
            );
        }

        /// <summary> Maps a 2D world position (XY) to a 2D grid key. </summary>
        public static Vector2Int PositionToKey2D(Vector2 position, float gridSize, Vector2 anchor = default)
        {
            return new Vector2Int(
                PositionToKey1D(position.x, gridSize, anchor.x),
                PositionToKey1D(position.y, gridSize, anchor.y)
            );
        }

        /// <summary> Converts a 2D key back to 3D world space, setting Y to the anchor's height. </summary>
        public static Vector3 KeyToPosition3DFrom2D(Vector2Int key, float gridSize, Vector3 anchor = default, bool centered = false)
        {
            return new Vector3(
                KeyToPosition1D(key.x, gridSize, anchor.x, centered),
                anchor.y,
                KeyToPosition1D(key.y, gridSize, anchor.z, centered)
            );
        }

        #endregion

        #region 3D CONVERSIONS

        /// <summary> Maps a 3D world position to a 3D grid key. </summary>
        public static Vector3Int PositionToKey3D(Vector3 position, float gridSize, Vector3 anchor = default)
        {
            return new Vector3Int(
                PositionToKey1D(position.x, gridSize, anchor.x),
                PositionToKey1D(position.y, gridSize, anchor.y),
                PositionToKey1D(position.z, gridSize, anchor.z)
            );
        }

        /// <summary> Converts a 3D grid key back to world space. </summary>
        public static Vector3 KeyToPosition3D(Vector3Int key, float gridSize, Vector3 anchor = default, bool centered = false)
        {
            return new Vector3(
                KeyToPosition1D(key.x, gridSize, anchor.x, centered),
                KeyToPosition1D(key.y, gridSize, anchor.y, centered),
                KeyToPosition1D(key.z, gridSize, anchor.z, centered)
            );
        }

        #endregion

        #region ADDITIONAL HELPERS

        /// <summary>
        /// Returns the local 0.0 to 1.0 interpolation value of a point within its cell.
        /// Useful for UV mapping or noise sampling.
        /// </summary>
        public static Vector3 GetCellLocalAlpha(Vector3 worldPos, float gridSize, Vector3 anchor = default)
        {
            Vector3 relativePos = worldPos - anchor;
            return new Vector3(
                (relativePos.x / gridSize) - Mathf.Floor(relativePos.x / gridSize),
                (relativePos.y / gridSize) - Mathf.Floor(relativePos.y / gridSize),
                (relativePos.z / gridSize) - Mathf.Floor(relativePos.z / gridSize)
            );
        }

        /// <summary>
        /// Quickly calculates the distance between two grid keys in "cell-steps".
        /// </summary>
        public static int GetGridDistance(Vector3Int a, Vector3Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y), Mathf.Abs(a.z - b.z));
        }

        #endregion
    }
}