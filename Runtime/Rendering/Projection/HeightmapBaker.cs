using Rayforge.Core.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Rayforge.Core.Rendering.Projection
{
    /// <summary>
    /// Utility class for baking scene geometry (Meshes/Terrains) into a heightmap
    /// using an orthographic projection relative to a world-space anchor.
    /// Uses OpMax blending to capture the highest surface points.
    /// </summary>
    public static class HeightmapBaker
    {
        public static class ShaderIds
        {
            public static readonly int UnityObjectToWorldId = Shader.PropertyToID("unity_ObjectToWorld");
            public static readonly int UnityMatrixVPId = Shader.PropertyToID("unity_MatrixVP");
        }

        /// <summary>
        /// Name of the height projection shader.
        /// Loaded through <c>Shader.Find()</c>.
        /// </summary>
        private const string k_BakeShaderName = "HeightmapProjection";

        /// <summary>Material used for the height projection pass.</summary>
        private static readonly Material k_BakeMaterial;

        /// <summary>Reusable CommandBuffer for bake operations.</summary>
        private static readonly CommandBuffer k_Cmd;

        /// <summary>
        /// Static constructor: loads the bake shader and initializes graphics resources.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the shader <c>Hidden/HeightmapBake</c> could not be found.
        /// </exception>
        static HeightmapBaker()
        {
            var shaderName = ResourcePaths.ShaderResourceFolder + k_BakeShaderName;
            var shader = Shader.Find(shaderName);
            if (shader == null)
                throw new InvalidOperationException($"Bake shader '{k_BakeShaderName}' could not be found. Ensure it is included in the project.");

            k_BakeMaterial = new Material(shader);
            k_Cmd = new CommandBuffer { name = "Heightmap_Projection_Baker" };
        }

        #region Immediate Execution Methods

        /// <summary>
        /// Convenience method that calculates absolute world center from an origin and local offset before baking.
        /// Execution is immediate on the GPU.
        /// </summary>
        /// <param name="target">The RFloat/R32 destination texture.</param>
        /// <param name="localCenter">Position relative to the anchor origin.</param>
        /// <param name="origin">World space origin of the grid (Anchor).</param>
        /// <param name="extent">Extent of the bake area.</param>
        /// <param name="minY">Minimum world-Y height (floor of the bake volume).</param>
        /// <param name="maxY">Maximum world-Y height (ceiling of the bake volume).</param>
        /// <param name="renderers">Objects to project into the heightmap.</param>
        public static void Bake(
            RenderTexture target,
            Vector3 localCenter,
            Vector3 origin,
            float extent,
            float minY,
            float maxY,
            IEnumerable<Renderer> renderers)
        {
            var param = new HeightmapBakeParams
            {
                WorldCenter = origin + localCenter,
                Extent = extent,
                MinY = minY,
                MaxY = maxY
            };

            Bake(target, param, renderers);
        }

        /// <summary>
        /// Bakes a set of renderers into a destination RenderTexture using absolute world coordinates.
        /// Execution is immediate on the GPU.
        /// </summary>
        /// <param name="target">The RFloat/R32 destination texture.</param>
        /// <param name="worldCenter">The absolute world-space center of the bake area.</param>
        /// <param name="extent">Extent of the bake area.</param>
        /// <param name="minY">Minimum world-Y height.</param>
        /// <param name="maxY">Maximum world-Y height.</param>
        /// <param name="renderers">Objects to project.</param>
        public static void Bake(
            RenderTexture target,
            Vector3 worldCenter,
            float extent,
            float minY,
            float maxY,
            IEnumerable<Renderer> renderers)
        {
            var param = new HeightmapBakeParams
            {
                WorldCenter = worldCenter,
                Extent = extent,
                MinY = minY,
                MaxY = maxY
            };

            Bake(target, param, renderers);
        }

        /// <summary>
        /// Bakes a set of renderers into a destination RenderTexture using the specified parameters.
        /// Execution is immediate on the GPU.
        /// </summary>
        /// <param name="target">The RFloat/R32 destination texture.</param>
        /// <param name="param">Spatial parameters for the bake area.</param>
        /// <param name="renderers">Objects to project.</param>
        /// <exception cref="ArgumentNullException">Thrown if target is null.</exception>
        public static void Bake(RenderTexture target, HeightmapBakeParams param, IEnumerable<Renderer> renderers)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            k_Cmd.Clear();
            SetupBakeCommandBuffer(k_Cmd, target, param, renderers);
            Graphics.ExecuteCommandBuffer(k_Cmd);
        }

        #endregion

        #region CommandBuffer Recording Methods

        /// <summary>
        /// Records bake commands using relative coordinates (Origin + LocalCenter) into a provided <see cref="CommandBuffer"/>.
        /// Use this for integration into existing rendering pipelines or frame-graph systems.
        /// </summary>
        /// <param name="cmd">The command buffer to record into.</param>
        /// <param name="target">The destination RenderTexture.</param>
        /// <param name="localCenter">Position of the bake area relative to the <paramref name="origin"/>.</param>
        /// <param name="origin">The world-space reference point (e.g., Grid Anchor).</param>
        /// <param name="extent">The horizontal size of the bake volume.</param>
        /// <param name="minY">The bottom Y-level in world space.</param>
        /// <param name="maxY">The top Y-level in world space.</param>
        /// <param name="renderers">Objects to project.</param>
        public static void SetupBakeCommandBuffer(
            CommandBuffer cmd,
            RenderTexture target,
            Vector3 localCenter,
            Vector3 origin,
            float extent,
            float minY,
            float maxY,
            IEnumerable<Renderer> renderers)
        {
            var param = new HeightmapBakeParams
            {
                WorldCenter = origin + localCenter,
                Extent = extent,
                MinY = minY,
                MaxY = maxY
            };

            SetupBakeCommandBuffer(cmd, target, param, renderers);
        }

        /// <summary>
        /// Records bake commands using absolute world coordinates into a provided <see cref="CommandBuffer"/>.
        /// </summary>
        /// <param name="cmd">The command buffer to record into.</param>
        /// <param name="target">The destination RenderTexture.</param>
        /// <param name="worldCenter">Absolute world-space center of the projection.</param>
        /// <param name="extent">The horizontal size of the bake volume.</param>
        /// <param name="minY">The bottom Y-level in world space.</param>
        /// <param name="maxY">The top Y-level in world space.</param>
        /// <param name="renderers">Objects to project.</param>
        public static void SetupBakeCommandBuffer(
            CommandBuffer cmd,
            RenderTexture target,
            Vector3 worldCenter,
            float extent,
            float minY,
            float maxY,
            IEnumerable<Renderer> renderers)
        {
            var param = new HeightmapBakeParams
            {
                WorldCenter = worldCenter,
                Extent = extent,
                MinY = minY,
                MaxY = maxY
            };

            SetupBakeCommandBuffer(cmd, target, param, renderers);
        }

        /// <summary>
        /// Records the bake commands into a provided CommandBuffer using the specified parameter structure.
        /// Does not execute the buffer.
        /// </summary>
        /// <param name="cmd">CommandBuffer to record the draw calls into.</param>
        /// <param name="target">The destination RenderTexture.</param>
        /// <param name="param">The bake volume parameters.</param>
        /// <param name="renderers">Objects to project.</param>
        /// <exception cref="ArgumentNullException">Thrown if cmd or target is null.</exception>
        public static void SetupBakeCommandBuffer(
            CommandBuffer cmd,
            RenderTexture target,
            HeightmapBakeParams param,
            IEnumerable<Renderer> renderers)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            if (target == null) throw new ArgumentNullException(nameof(target));

            cmd.SetRenderTarget(target);

            cmd.ClearRenderTarget(true, true, new Color(float.MinValue, 0, 0, 1));

            Vector3 camPos = new Vector3(param.WorldCenter.x, param.MaxY, param.WorldCenter.z);
            Vector3 lookTarget = new Vector3(param.WorldCenter.x, param.MinY, param.WorldCenter.z);

            Matrix4x4 viewMatrix = Matrix4x4.LookAt(camPos, lookTarget, Vector3.forward);

            float depth = param.MaxY - param.MinY;
            Matrix4x4 projMatrix = Matrix4x4.Ortho(-param.Extent, param.Extent, -param.Extent, param.Extent, 0, depth);

            cmd.SetViewProjectionMatrices(viewMatrix, projMatrix);

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                cmd.DrawRenderer(renderer, k_BakeMaterial);
            }
        }

        #endregion
    }
}