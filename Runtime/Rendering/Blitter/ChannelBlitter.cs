using Rayforge.Core.Common;
using Rayforge.Core.ManagedResources.NativeMemory;
using Rayforge.Core.ManagedResources.NativeMemory.Helpers;
using Rayforge.Core.ShaderExtensions.Blitter;
using System;
using UnityEngine;
using static UnityEngine.Resources;

namespace Rayforge.Core.Rendering.Blitter
{
    /// <summary>
    /// Utility class for performing channel-wise blits from a source texture
    /// to a destination render target, supporting per-channel remapping and
    /// scale/bias based sub-rect blits via compute or rasterization pipelines.
    /// </summary>
    public static class ChannelBlitter
    {
        /// <summary>
        /// Determines which pipeline to use for the blit operation.
        /// </summary>
        public enum BlitType
        {
            /// <summary>Use a compute shader for the blit.</summary>
            Compute,
            /// <summary>Use the rasterization pipeline (full-screen triangle / DrawProcedural).</summary>
            Raster
        }

        public static class ChannelShaderIds
        {
            private const string ChannelMapping = "_ChannelMapping";
            private const string ChannelSource = "_ChannelSource";
            private const string ChannelOps = "_ChannelOps";
            private const string ChannelMults = "_ChannelMults";
            private const string ChannelBlitterParams = "_ChannelBlitterParams";

            public static readonly int ChannelMappingId = Shader.PropertyToID(ChannelMapping);
            public static readonly int ChannelSourceId = Shader.PropertyToID(ChannelSource);
            public static readonly int ChannelOpsId = Shader.PropertyToID(ChannelOps);
            public static readonly int ChannelMultsId = Shader.PropertyToID(ChannelMults);
            public static readonly int ChannelBlitterParamsId = Shader.PropertyToID(ChannelBlitterParams);

            public static readonly int BlitTextureId = BlitParameters.BlitTextureId;
            public static readonly int BlitDestinationId = BlitParameters.BlitDestinationId;
            public static readonly int BlitScaleBiasId = BlitParameters.BlitScaleBiasId;
        }

        public static class ComputeBlitShaderIds
        {
            private const string ComputeBlitParams = "_ComputeBlitParams";
            public static readonly int ComputeBlitParamsId = Shader.PropertyToID(ComputeBlitParams);

            public const string BlitTexture0 = "_BlitTexture0";
            public const string BlitTexture1 = "_BlitTexture1";
            public const string BlitTexture2 = "_BlitTexture2";
            public const string BlitTexture3 = "_BlitTexture3";

            public static readonly int BlitTexture0Id = Shader.PropertyToID(BlitTexture0);
            public static readonly int BlitTexture1Id = Shader.PropertyToID(BlitTexture1);
            public static readonly int BlitTexture2Id = Shader.PropertyToID(BlitTexture2);
            public static readonly int BlitTexture3Id = Shader.PropertyToID(BlitTexture3);

            public const string BlitTexture0_TexelSize = "_BlitTexture0_TexelSize";
            public const string BlitTexture1_TexelSize = "_BlitTexture1_TexelSize";
            public const string BlitTexture2_TexelSize = "_BlitTexture2_TexelSize";
            public const string BlitTexture3_TexelSize = "_BlitTexture3_TexelSize";

            public static readonly int BlitTexture0_TexelSizeId = Shader.PropertyToID(BlitTexture0_TexelSize);
            public static readonly int BlitTexture1_TexelSizeId = Shader.PropertyToID(BlitTexture1_TexelSize);
            public static readonly int BlitTexture2_TexelSizeId = Shader.PropertyToID(BlitTexture2_TexelSize);
            public static readonly int BlitTexture3_TexelSizeId = Shader.PropertyToID(BlitTexture3_TexelSize);
        }


        /// <summary>
        /// Name of the compute shader inside the Resources folder.
        /// Used for channel-wise blitting via compute shader.
        /// Loaded through <c>Shader.Find()</c> or <c>Resources.Load()</c>.
        /// </summary>
        private const string k_ComputeBlitShaderName = "ComputeChannelBlitter";
        /// <summary>
        /// Name of the raster (non-compute) shader inside the Resources folder.
        /// Used for standard GPU rasterization-based channel blitting.
        /// Loaded through <c>Shader.Find()</c> or <c>Resources.Load()</c>.
        /// </summary>
        private const string k_RasterBlitShaderName = "RasterChannelBlitter";

        /// <summary>Compute shader used for compute pipeline blits.</summary>
        private static readonly ComputeShader k_ComputeBlitShader;

        /// <summary>Material used for rasterization pipeline blits (fullscreen triangle).</summary>
        private static readonly Material k_RasterBlitMaterial;

        /// <summary>Reusable MaterialPropertyBlock for raster blits.</summary>
        private static readonly MaterialPropertyBlock k_PropertyBlock;

        /// <summary>
        /// Static constructor: loads shaders and initializes the raster blit material and property block.
        /// Throws exceptions if shaders cannot be found or loaded.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the compute shader <c>ComputeChannelBlitter</c> could not be loaded
        /// or if the raster shader <c>RasterChannelBlitter</c> could not be found.
        /// </exception>
        static ChannelBlitter()
        {
            k_ComputeBlitShader = Load<ComputeShader>(k_ComputeBlitShaderName);
            if (k_ComputeBlitShader == null)
                throw new InvalidOperationException($"Compute shader '{k_ComputeBlitShaderName}' could not be loaded.");

            var shader = Shader.Find(ResourcePaths.ShaderNamespace + k_RasterBlitShaderName);
            if (shader == null)
                throw new InvalidOperationException($"Raster shader '{k_RasterBlitShaderName}' could not be found.");

            k_RasterBlitMaterial = new Material(shader);
            k_PropertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// Blits a source texture to a destination render target using the specified pipeline type,
        /// applying channel remapping and scale/bias based coordinate transformation.
        /// </summary>
        /// <param name="source">Source texture to read from.</param>
        /// <param name="dest">Destination render texture.</param>
        /// <param name="type">Pipeline type (Compute or Raster) to use.</param>
        /// <param name="param">Channel mapping and rectangle parameters.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="dest"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="type"/> is unknown or if <paramref name="param"/> has non-positive size.
        /// </exception>
        public static void Blit(Texture source, RenderTexture dest, BlitType type, ChannelBlitParams param)
        {
            switch (type)
            {
                case BlitType.Raster:
                    RasterBlit(source, dest, param);
                    break;
                case BlitType.Compute:
                    ComputeBlit(source, dest, param);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), "Unknown blit type.");
            }
        }

        /// <summary>
        /// Rasterization blit using a ChannelBlitParams cbuffer.
        /// The offset specifies the struct offset within the constant buffer.
        /// </summary>
        /// <param name="source">Source texture.</param>
        /// <param name="dest">Destination render target.</param>
        /// <param name="param">Cbuffer expected to contain <see cref="ChannelBlitterParams"/>.</param>
        /// <param name="offset"><see cref="ChannelBlitterParams"/> struct offset within cbuffer.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/>, <paramref name="dest"/>, or <paramref name="param"/> is null.</exception>
        public static void RasterBlit(Texture source, RenderTexture dest, ManagedComputeBuffer param, int offset = 0)
        {
            if (param == null) 
                throw new ArgumentNullException(nameof(param));

            k_PropertyBlock.SetCBuffer(ChannelShaderIds.ChannelBlitterParamsId, param, offset);
            RasterBlit(source, dest, k_PropertyBlock);
        }

        /// <summary>
        /// Rasterization blit using ChannelBlitParams.
        /// Applies scale/bias for source UV transformation and per-channel remapping,
        /// converts parameters into a MaterialPropertyBlock, and invokes the raster blit.
        /// </summary>
        /// <param name="source">Source texture.</param>
        /// <param name="dest">Destination render target.</param>
        /// <param name="param">Channel mapping and offset/size rectangle.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if param.size is non-positive.</exception>
        public static void RasterBlit(Texture source, RenderTexture dest, ChannelBlitParams param)
        {
            k_PropertyBlock.SetVector(ChannelShaderIds.BlitScaleBiasId, new Vector4(param.scale.x, param.scale.y, param.bias.x, param.bias.y));
            k_PropertyBlock.SetVector(ChannelShaderIds.ChannelMappingId, new Vector4((int)param.R, (int)param.G, (int)param.B, (int)param.A));
            k_PropertyBlock.SetVector(ChannelShaderIds.ChannelOpsId, new Vector4((int)param.ROps, (int)param.GOps, (int)param.BOps, (int)param.AOps));
            k_PropertyBlock.SetVector(ChannelShaderIds.ChannelMultsId, new Vector4(param.RMultiplier, param.GMultiplier, param.BMultiplier, param.AMultiplier));

            RasterBlit(source, dest, k_PropertyBlock);
        }

        /// <summary>
        /// Performs a rasterization blit using a pre-filled MaterialPropertyBlock.
        /// </summary>
        /// <param name="source">Source texture.</param>
        /// <param name="dest">Destination render target.</param>
        /// <param name="mpb">MaterialPropertyBlock containing channel and blit parameters.</param>
        /// <exception cref="ArgumentNullException">Thrown if source, dest or mpb is null.</exception>
        public static void RasterBlit(Texture source, RenderTexture dest, MaterialPropertyBlock mpb)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));
            if (mpb == null)
                throw new ArgumentNullException(nameof(mpb));

            mpb.SetTexture(ChannelShaderIds.BlitTextureId, source);
            Graphics.SetRenderTarget(dest);
            Graphics.DrawProcedural(k_RasterBlitMaterial, new Bounds(Vector2.zero, Vector2.one), MeshTopology.Triangles, 3, 1, null, mpb);
            Graphics.SetRenderTarget(null);
        }

        /// <summary>
        /// Single-source compute blit using <see cref="ChannelBlitParams"/>.
        /// Internally calls the multi-source ComputeBlit with all channels pointing to Texture0.
        /// </summary>
        /// <param name="source">Source texture containing raw pixel data.</param>
        /// <param name="dest">Destination render texture.</param>
        /// <param name="param">Channel mapping and per-channel scale/bias.</param>
        public static void ComputeBlit(Texture source, RenderTexture dest, ChannelBlitParams param)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));

            param.RSource = SourceTexture.Texture0;
            param.GSource = SourceTexture.Texture0;
            param.BSource = SourceTexture.Texture0;
            param.ASource = SourceTexture.Texture0;

            var computeParam = new ComputeBlitParams
            {
                BlitTexture0_TexelSize = new Vector4(1f / source.width, 1f / source.height, source.width, source.height),
                BlitTexture1_TexelSize = Vector4.zero,
                BlitTexture2_TexelSize = Vector4.zero,
                BlitTexture3_TexelSize = Vector4.zero,
                BlitDest_Res = new Vector2Int(dest.width, dest.height)
            };

            ComputeBlit(source, null, null, null, dest, param, computeParam);
        }


        /// <summary>
        /// Performs a compute-based blit using up to 4 source textures.
        /// Automatically binds the textures to the shader and sets <see cref="ChannelBlitParams"/> source slots.
        /// Supports per-channel remapping and scale/bias for sub-region blitting.
        /// </summary>
        /// <param name="tex0">Source texture 0 (used if any channel references Texture0).</param>
        /// <param name="tex1">Source texture 1 (used if any channel references Texture1).</param>
        /// <param name="tex2">Source texture 2 (used if any channel references Texture2).</param>
        /// <param name="tex3">Source texture 3 (used if any channel references Texture3).</param>
        /// <param name="dest">Destination render texture.</param>
        /// <param name="channelParam">Per-channel mapping and source selection.</param>
        /// <param name="computeParam">Compute shader parameters including texel sizes.</param>
        public static void ComputeBlit(
            Texture tex0,
            Texture tex1,
            Texture tex2,
            Texture tex3,
            RenderTexture dest,
            ChannelBlitParams channelParam,
            ComputeBlitParams computeParam)
        {
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));

            Func<Texture, ChannelBlitParams, SourceTexture, bool> ValidTexture = static (tex, param, src) =>
                tex != null &&
                (param.RSource == src || param.GSource == src || param.BSource == src || param.ASource == src);


            // Bind only textures that exist and are actually referenced
            if (ValidTexture.Invoke(tex0, channelParam, SourceTexture.Texture0))
                k_ComputeBlitShader.SetTexture(0, ComputeBlitShaderIds.BlitTexture0, tex0);

            if (ValidTexture.Invoke(tex1, channelParam, SourceTexture.Texture1))
                k_ComputeBlitShader.SetTexture(0, ComputeBlitShaderIds.BlitTexture1, tex1);

            if (ValidTexture.Invoke(tex2, channelParam, SourceTexture.Texture2))
                k_ComputeBlitShader.SetTexture(0, ComputeBlitShaderIds.BlitTexture2, tex2);

            if (ValidTexture.Invoke(tex3, channelParam, SourceTexture.Texture3))
                k_ComputeBlitShader.SetTexture(0, ComputeBlitShaderIds.BlitTexture3, tex3);

            k_ComputeBlitShader.SetVector(ComputeBlitShaderIds.BlitTexture0_TexelSizeId, computeParam.BlitTexture0_TexelSize);
            k_ComputeBlitShader.SetVector(ComputeBlitShaderIds.BlitTexture1_TexelSizeId, computeParam.BlitTexture1_TexelSize);
            k_ComputeBlitShader.SetVector(ComputeBlitShaderIds.BlitTexture2_TexelSizeId, computeParam.BlitTexture2_TexelSize);
            k_ComputeBlitShader.SetVector(ComputeBlitShaderIds.BlitTexture3_TexelSizeId, computeParam.BlitTexture3_TexelSize);

            k_ComputeBlitShader.SetVector(ChannelShaderIds.BlitScaleBiasId, new Vector4(channelParam.scale.x, channelParam.scale.y, channelParam.bias.x, channelParam.bias.y));
            k_ComputeBlitShader.SetVector(ChannelShaderIds.ChannelMappingId, new Vector4((int)channelParam.R, (int)channelParam.G, (int)channelParam.B, (int)channelParam.A));
            k_ComputeBlitShader.SetVector(ChannelShaderIds.ChannelSourceId, new Vector4((int)channelParam.RSource, (int)channelParam.GSource, (int)channelParam.BSource, (int)channelParam.ASource));
            k_ComputeBlitShader.SetVector(ChannelShaderIds.ChannelOpsId, new Vector4((int)channelParam.ROps, (int)channelParam.GOps, (int)channelParam.BOps, (int)channelParam.AOps));
            k_ComputeBlitShader.SetVector(ChannelShaderIds.ChannelMultsId, new Vector4(channelParam.RMultiplier, channelParam.GMultiplier, channelParam.BMultiplier, channelParam.AMultiplier));

            k_ComputeBlitShader.SetTexture(0, ChannelShaderIds.BlitDestinationId, dest);

            int threadGroupX = Mathf.CeilToInt(dest.width / 8.0f);
            int threadGroupY = Mathf.CeilToInt(dest.height / 8.0f);
            k_ComputeBlitShader.Dispatch(0, threadGroupX, threadGroupY, 1);
        }

    }
}
