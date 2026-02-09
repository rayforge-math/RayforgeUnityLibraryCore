using Rayforge.Core.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TerrainUtils;

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
            public static readonly int BakerYParamsId = Shader.PropertyToID("_BakerYParams");
            public static readonly int TerrainHeightmapId = Shader.PropertyToID("_TerrainHeightmap");
            public static readonly int TerrainUvParamsId = Shader.PropertyToID("_TerrainUvParams");
            public static readonly int TerrainYParamsId = Shader.PropertyToID("_TerrainYParams");
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

        /// <summary>Reusable MPB for bake operations.</summary>
        private static readonly MaterialPropertyBlock k_PropertyBlock;

        private const string k_MeshBakingPassName = "MeshBaking";
        private const string k_TerrainBakingPassName = "TerrainBaking";

        private static readonly int k_MeshBakingPassId;
        private static readonly int k_TerrainBakingPassId;

        /// <summary>
        /// Static constructor: loads the bake shader and initializes graphics resources.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the shader <c>Rayfroge/HeightmapProjection</c> could not be found.
        /// </exception>
        static HeightmapBaker()
        {
            var shaderName = ResourcePaths.ShaderResourceFolder + k_BakeShaderName;
            var shader = UnityEngine.Resources.Load<Shader>(shaderName);
            if (shader == null)
                throw new InvalidOperationException($"Bake shader '{k_BakeShaderName}' could not be found. Ensure it is included in the project.");

            k_BakeMaterial = new Material(shader);
            k_Cmd = new CommandBuffer { name = "Heightmap_Projection_Baker" };
            k_PropertyBlock = new MaterialPropertyBlock();

            k_MeshBakingPassId = k_BakeMaterial.FindPass(k_MeshBakingPassName);
            k_TerrainBakingPassId = k_BakeMaterial.FindPass(k_TerrainBakingPassName);
        }

        #region Immediate Execution Methods

        /// <summary>
        /// Bakes a set of surfaces into a destination texture using absolute world coordinates.
        /// Execution is immediate on the GPU.
        /// </summary>
        /// <typeparam name="TTex">The type of the texture, must inherit from <see cref="Texture"/>.</typeparam>
        /// <param name="target">The destination texture (e.g., RenderTexture or CustomRenderTexture).</param>
        /// <param name="worldCenter">The absolute world-space center of the bake area.</param>
        /// <param name="extent">The horizontal size (half-extents) of the bake volume.</param>
        /// <param name="minY">The minimum world-Y height (bottom of the bake volume).</param>
        /// <param name="maxY">The maximum world-Y height (top of the bake volume).</param>
        /// <param name="meshFilters">The collection of mesh filters to be projected.</param>
        /// <param name="terrains">Optional collection of Unity Terrains to include in the heightmap.</param>
        /// <exception cref="ArgumentNullException">Thrown if target, meshFilters, or terrains is null.</exception>
        public static void Bake<TTex>(
            TTex target,
            Vector3 worldCenter,
            Vector2 extent,
            float minY,
            float maxY,
            IEnumerable<MeshFilter> meshFilters,
            IEnumerable<Terrain> terrains = null)
            where TTex : Texture
        {
            var param = new HeightmapBakeParams
            {
                WorldCenter = worldCenter,
                Extent = extent,
                MinY = minY,
                MaxY = maxY
            };

            Bake(target, param, meshFilters, terrains);
        }

        /// <summary>
        /// Bakes a set of surfaces into a destination texture using the specified parameter structure.
        /// Execution is immediate on the GPU.
        /// </summary>
        /// <typeparam name="TTex">The type of the texture, must inherit from <see cref="Texture"/>.</typeparam>
        /// <param name="target">The destination texture (e.g., RenderTexture or CustomRenderTexture).</param>
        /// <param name="param">The spatial parameters for the bake volume.</param>
        /// <param name="meshFilters">The collection of mesh filters to be projected.</param>
        /// <param name="terrains">Optional collection of Unity Terrains to include.</param>
        /// <exception cref="ArgumentNullException">Thrown if target, meshFilters, or terrains is null.</exception>
        public static void Bake<TTex>(TTex target, HeightmapBakeParams param, IEnumerable<MeshFilter> meshFilters, IEnumerable<Terrain> terrains = null)
            where TTex : Texture
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            k_Cmd.Clear();
            SetupBakeCommandBuffer(k_Cmd, target, param, meshFilters, terrains);
            Graphics.ExecuteCommandBuffer(k_Cmd);
        }

        /// <summary>
        /// Bakes surfaces into a specific slice of a Texture2DArray using absolute world coordinates.
        /// Execution is immediate on the GPU.
        /// </summary>
        /// <param name="target">The destination Texture2DArray.</param>
        /// <param name="sliceIndex">The zero-based index of the array slice to bake into.</param>
        /// <param name="worldCenter">The absolute world-space center of the bake area.</param>
        /// <param name="extent">The horizontal size (half-extents) of the bake volume.</param>
        /// <param name="minY">The minimum world-Y height.</param>
        /// <param name="maxY">The maximum world-Y height.</param>
        /// <param name="meshFilters">The collection of mesh filters to be projected.</param>
        /// <param name="terrains">Optional collection of Unity Terrains to include.</param>
        /// <exception cref="ArgumentNullException">Thrown if target, meshFilters, or terrains is null.</exception>
        public static void Bake(
            Texture2DArray target,
            int sliceIndex,
            Vector3 worldCenter,
            Vector2 extent,
            float minY,
            float maxY,
            IEnumerable<MeshFilter> meshFilters,
            IEnumerable<Terrain> terrains = null)
        {
            var param = new HeightmapBakeParams
            {
                WorldCenter = worldCenter,
                Extent = extent,
                MinY = minY,
                MaxY = maxY
            };

            Bake(target, sliceIndex, param, meshFilters, terrains);
        }

        /// <summary>
        /// Bakes surfaces into a specific slice of a Texture2DArray using the specified parameter structure.
        /// Execution is immediate on the GPU.
        /// </summary>
        /// <param name="target">The destination Texture2DArray.</param>
        /// <param name="sliceIndex">The zero-based index of the array slice to bake into.</param>
        /// <param name="param">The spatial parameters for the bake volume.</param>
        /// <param name="meshFilters">The collection of mesh filters to be projected.</param>
        /// <param name="terrains">Optional collection of Unity Terrains to include.</param>
        /// <exception cref="ArgumentNullException">Thrown if target, meshFilters, or terrains is null.</exception>
        public static void Bake(Texture2DArray target, int sliceIndex, HeightmapBakeParams param, IEnumerable<MeshFilter> meshFilters, IEnumerable<Terrain> terrains = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            k_Cmd.Clear();
            SetupBakeCommandBuffer(k_Cmd, target, sliceIndex, param, meshFilters, terrains);
            Graphics.ExecuteCommandBuffer(k_Cmd);
        }

        #endregion

        #region CommandBuffer Recording Methods

        /// <summary>
        /// Records bake commands using absolute world coordinates into a provided <see cref="CommandBuffer"/>.
        /// </summary>
        /// <param name="cmd">The command buffer to record into.</param>
        /// <param name="target">The destination Texture.</param>
        /// <param name="worldCenter">Absolute world-space center of the projection.</param>
        /// <param name="extent">The horizontal size of the bake volume.</param>
        /// <param name="minY">The bottom Y-level in world space.</param>
        /// <param name="maxY">The top Y-level in world space.</param>
        /// <param name="meshFilters">Objects to project.</param>
        /// <param name="terrains">Unity Terrains to include in the heightmap.</param>
        /// <exception cref="ArgumentNullException">Thrown if target, meshFilters, or terrains is null.</exception>
        public static void SetupBakeCommandBuffer<TTex>(
             CommandBuffer cmd,
             TTex target,
             Vector3 worldCenter,
             Vector2 extent,
             float minY,
             float maxY,
             IEnumerable<MeshFilter> meshFilters,
             IEnumerable<Terrain> terrains = null)
            where TTex : Texture
        {
            var param = new HeightmapBakeParams
            {
                WorldCenter = worldCenter,
                Extent = extent,
                MinY = minY,
                MaxY = maxY
            };

            SetupBakeCommandBuffer(cmd, target, param, meshFilters, terrains);
        }

        /// <summary>
        /// Records bake commands using absolute world coordinates into a provided <see cref="CommandBuffer"/>.
        /// </summary>
        /// <param name="cmd">The command buffer to record into.</param>
        /// <param name="target">The destination Texture2DArray.</param>
        /// <param name="sliceIndex">Slice index of the destination.</param>
        /// <param name="worldCenter">Absolute world-space center of the projection.</param>
        /// <param name="extent">The horizontal size of the bake volume.</param>
        /// <param name="minY">The bottom Y-level in world space.</param>
        /// <param name="maxY">The top Y-level in world space.</param>
        /// <param name="meshFilters">Objects to project.</param>
        /// <param name="terrains">Unity Terrains to include in the heightmap.</param>
        /// <exception cref="ArgumentNullException">Thrown if target, meshFilters, or terrains is null.</exception>
        public static void SetupBakeCommandBuffer(
            CommandBuffer cmd,
            Texture2DArray target,
            int sliceIndex,
            Vector3 worldCenter,
            Vector2 extent,
            float minY,
            float maxY,
            IEnumerable<MeshFilter> meshFilters,
            IEnumerable<Terrain> terrains = null)
        {
            var param = new HeightmapBakeParams
            {
                WorldCenter = worldCenter,
                Extent = extent,
                MinY = minY,
                MaxY = maxY
            };

            SetupBakeCommandBuffer(cmd, target, sliceIndex, param, meshFilters, terrains);
        }

        /// <summary>
        /// Records bake commands for a standard texture target (RenderTexture, CustomRenderTexture, etc.).
        /// </summary>
        /// <param name="cmd">CommandBuffer to record the draw calls into.</param>
        /// <param name="target">The destination texture (must be a valid render target).</param>
        /// <param name="param">The spatial parameters defining the bake area and height range.</param>
        /// <param name="meshFilters">Static meshes to be projected into the heightmap.</param>
        /// <param name="terrains">Optional Unity Terrains to be included.</param>
        /// <exception cref="ArgumentNullException">Thrown if target, meshFilters, or terrains is null.</exception>
        public static void SetupBakeCommandBuffer<TTex>(
            CommandBuffer cmd,
            TTex target,
            HeightmapBakeParams param,
            IEnumerable<MeshFilter> meshFilters,
            IEnumerable<Terrain> terrains = null)
            where TTex : Texture
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            cmd.SetRenderTarget(target);

            SetupBakeCommandBuffer_internal(cmd, param, meshFilters, terrains);
        }

        /// <summary>
        /// Records bake commands specifically for a slice within a Texture2DArray.
        /// </summary>
        /// <param name="cmd">CommandBuffer to record the draw calls into.</param>
        /// <param name="target">The destination Texture2DArray.</param>
        /// <param name="sliceIndex">The depth index (slice) to bake into.</param>
        /// <param name="param">The spatial parameters defining the bake area and height range.</param>
        /// <param name="meshFilters">Static meshes to be projected into the heightmap.</param>
        /// <param name="terrains">Optional Unity Terrains to be included.</param>
        /// <exception cref="ArgumentNullException">Thrown if target, meshFilters, or terrains is null.</exception>
        public static void SetupBakeCommandBuffer(
            CommandBuffer cmd,
            Texture2DArray target,
            int sliceIndex,
            HeightmapBakeParams param,
            IEnumerable<MeshFilter> meshFilters,
            IEnumerable<Terrain> terrains = null)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            cmd.SetRenderTarget(target, 0, CubemapFace.Unknown, sliceIndex);

            SetupBakeCommandBuffer_internal(cmd, param, meshFilters, terrains);
        }

        /// <summary>
        /// Records the bake commands into a provided CommandBuffer using the specified parameter structure.
        /// Does not execute the buffer.
        /// </summary>
        /// <param name="cmd">CommandBuffer to record the draw calls into.</param>
        /// <param name="param">The bake volume parameters.</param>
        /// <param name="meshFilters">Objects to project.</param>
        /// <param name="terrains">Unity Terrains to include in the heightmap.</param>
        /// <exception cref="ArgumentNullException">Thrown if target, meshFilters, or terrains is null.</exception>
        private static void SetupBakeCommandBuffer_internal(
            CommandBuffer cmd,
            HeightmapBakeParams param,
            IEnumerable<MeshFilter> meshFilters,
            IEnumerable<Terrain> terrains = null)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));

            if(meshFilters == null && terrains == null) throw new ArgumentNullException($"MeshFilters {nameof(meshFilters)} and terrains {nameof(terrains)} are null");

            cmd.ClearRenderTarget(true, true, new Color(float.MinValue, 0, 0, 1));

            float absMaxY = param.WorldCenter.y + param.MaxY;
            float absMinY = param.WorldCenter.y + param.MinY;

            float near = 0.01f;
            float far = absMaxY - absMinY;

            Vector4 yParams = new Vector4(absMinY, absMaxY, .0f, 1.0f / far);

            k_PropertyBlock.Clear();
            k_PropertyBlock.SetVector(ShaderIds.BakerYParamsId, yParams);
            
            if (meshFilters != null)
            {
                Vector3 camPos = new Vector3(param.WorldCenter.x, absMaxY, param.WorldCenter.z);
                Quaternion rot = Quaternion.Euler(90f, 0f, 0f);
                Matrix4x4 viewMatrix = Matrix4x4.TRS(camPos, rot, Vector3.one).inverse;

                float xExtent = param.Extent.x;
                float yExtent = param.Extent.y;
                Matrix4x4 projMatrix = Matrix4x4.Ortho(-xExtent, xExtent, -yExtent, yExtent, near, far);

                cmd.SetViewProjectionMatrices(viewMatrix, projMatrix);

                foreach (var filter in meshFilters)
                {
                    if (filter == null || filter.sharedMesh == null) continue;

                    Mesh mesh = filter.sharedMesh;
                    Matrix4x4 matrix = filter.transform.localToWorldMatrix;

                    for (int i = 0; i < mesh.subMeshCount; ++i)
                    {
                        cmd.DrawMesh(mesh, matrix, k_BakeMaterial, i, k_MeshBakingPassId, k_PropertyBlock);
                    }
                }
            }
            
            if (terrains != null)
            {
                foreach (var terrain in terrains)
                {
                    if (terrain == null || terrain.terrainData == null) continue;

                    Vector3 size = terrain.terrainData.size;
                    Vector3 pos = terrain.transform.position;

                    float fullWidth = param.Extent.x * 2f;
                    float fullHeight = param.Extent.y * 2f;

                    Vector2 areaMin = new Vector2(
                        param.WorldCenter.x - param.Extent.x,
                        param.WorldCenter.z - param.Extent.y
                    );

                    Vector4 uvParams = new Vector4(
                        (pos.x - areaMin.x) / fullWidth,
                        (pos.z - areaMin.y) / fullHeight,
                        fullWidth / size.x,
                        fullHeight / size.z
                    );

                    Vector2 terrainYParams = new Vector2(pos.y, size.y);

                    k_PropertyBlock.Clear();
                    k_PropertyBlock.SetTexture(ShaderIds.TerrainHeightmapId, terrain.terrainData.heightmapTexture);
                    k_PropertyBlock.SetVector(ShaderIds.TerrainUvParamsId, uvParams);
                    k_PropertyBlock.SetVector(ShaderIds.TerrainYParamsId, terrainYParams);

                    cmd.DrawProcedural(Matrix4x4.identity, k_BakeMaterial, k_TerrainBakingPassId, MeshTopology.Triangles, 3, 1, k_PropertyBlock);
                }
            }
        }

        #endregion
    }
}