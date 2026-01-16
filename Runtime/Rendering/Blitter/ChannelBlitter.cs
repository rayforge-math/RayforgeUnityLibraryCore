using Rayforge.Core.Common;
using Rayforge.Core.Diagnostics;
using Rayforge.Core.ManagedResources.NativeMemory;
using Rayforge.Core.ManagedResources.NativeMemory.Helpers;
using Rayforge.Core.ShaderExtensions.Blitter;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
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
            private const string SrcChannels = "_SrcChannels";
            private const string SrcTextures = "_SrcTextures";
            private const string ChannelOps = "_ChannelOps";
            private const string ChannelMults = "_ChannelMults";
            private const string ChannelBlitParams = "_ChannelBlitParams";

            public static readonly int SrcChannelsId = Shader.PropertyToID(SrcChannels);
            public static readonly int SrcTexturesId = Shader.PropertyToID(SrcTextures);
            public static readonly int ChannelOpsId = Shader.PropertyToID(ChannelOps);
            public static readonly int ChannelMultsId = Shader.PropertyToID(ChannelMults);
            public static readonly int ChannelBlitParamsId = Shader.PropertyToID(ChannelBlitParams);

            public static readonly int BlitTextureId = BlitParameters.BlitTextureId;
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

            public const string BlitDestRes = "_BlitDest_Res";
            public static readonly int BlitDestResId = Shader.PropertyToID(BlitDestRes);

            public const string BlitStretchToFit = "_BlitStretchToFit";
            public static readonly int BlitStretchToFitId = Shader.PropertyToID(BlitStretchToFit);

            public static readonly int BlitDestinationId = BlitParameters.BlitDestinationId;
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

        /// <summary>Reusable CommandBuffer for blits.</summary>
        private static readonly CommandBuffer k_Cmd;

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
            string computePath = ResourcePaths.ShaderResourceFolder + k_ComputeBlitShaderName;
            k_ComputeBlitShader = Load<ComputeShader>(computePath);
            if (k_ComputeBlitShader == null)
                throw new InvalidOperationException($"Compute shader '{computePath}' could not be loaded.");

            string shaderNamespacePath = ResourcePaths.ShaderNamespace + k_RasterBlitShaderName;
            var shader = Shader.Find(shaderNamespacePath);
            if (shader == null)
                throw new InvalidOperationException($"Raster shader '{shaderNamespacePath}' could not be found.");

            k_RasterBlitMaterial = new Material(shader);
            k_PropertyBlock = new MaterialPropertyBlock();
            k_Cmd = new CommandBuffer();
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
        /// Records a rasterization blit into a command buffer using caller-owned GPU resources.
        /// All parameters must already be written into the provided constant buffer.
        /// The command buffer is not executed by this method.
        /// </summary>
        /// <param name="cmd">CommandBuffer to record the draw into.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="dest">Destination render target.</param>
        /// <param name="mpb">
        /// Caller-owned <see cref="MaterialPropertyBlock"/> used for texture bindings.
        /// Must not be shared with other in-flight command buffers.
        /// </param>
        /// <param name="paramBuffer">
        /// Constant buffer containing one or more <see cref="ChannelBlitParams"/> structs.
        /// </param>
        /// <param name="paramOffset">
        /// Byte offset of the <see cref="ChannelBlitParams"/> struct within <paramref name="paramBuffer"/>.
        /// </param>
        public static void RasterBlit(CommandBuffer cmd, Texture source, RenderTexture dest, MaterialPropertyBlock mpb, ComputeBuffer paramBuffer, int paramOffset = 0)
        {
            if (paramBuffer == null)
                throw new ArgumentNullException(nameof(paramBuffer));

            mpb.Clear();
            mpb.SetConstantBuffer(ChannelShaderIds.ChannelBlitParamsId, paramBuffer, paramOffset, Marshal.SizeOf<ChannelBlitParams>());

            RasterBlit(cmd, source, dest, mpb);
        }

        /// <summary>
        /// Records a rasterization blit using <see cref="ChannelBlitParams"/> into a provided
        /// <see cref="CommandBuffer"/> using a caller-owned <see cref="MaterialPropertyBlock"/>.
        /// The command buffer is not executed by this method.
        /// </summary>
        /// <param name="cmd">CommandBuffer to record the blit into.</param>
        /// <param name="source">Source texture.</param>
        /// <param name="dest">Destination render target.</param>
        /// <param name="param">Channel mapping and scale/bias rectangle.</param>
        /// <param name="mpb">
        /// Caller-owned <see cref="MaterialPropertyBlock"/> used to store all shader parameters.
        /// Must not be shared with other in-flight command buffers.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="cmd"/>, <paramref name="source"/>, <paramref name="dest"/> or <paramref name="mpb"/> is null.
        /// </exception>
        public static void RasterBlit(CommandBuffer cmd, Texture source, RenderTexture dest, ChannelBlitParams param, MaterialPropertyBlock mpb)
        {
            if (mpb == null)
                throw new ArgumentNullException(nameof(mpb));

            mpb.Clear();
            PrepareRasterBlitPropertyBlock(param, mpb);

            RasterBlit(cmd, source, dest, mpb);
        }

        /// <summary>
        /// Performs a rasterization blit using <see cref="ChannelBlitParams"/>.
        /// Internally fills a <see cref="MaterialPropertyBlock"/> with the provided parameters,
        /// enqueues the draw in the shared internal <see cref="CommandBuffer"/>,
        /// executes it, and optionally invokes a callback when the target is ready via <see cref="AsyncGPUReadback"/>.
        /// </summary>
        /// <param name="source">Source texture.</param>
        /// <param name="dest">Destination render target.</param>
        /// <param name="param">Channel mapping and scale/bias rectangle.</param>
        /// <param name="onComplete">Optional callback invoked when the GPU operation is complete, with <paramref name="dest"/> ready to use.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> or <paramref name="dest"/> is null.</exception>
        public static void RasterBlit(Texture source, RenderTexture dest, ChannelBlitParams param, Action<AsyncGPUReadbackRequest> onComplete = null)
        {
            var mpb = k_PropertyBlock;
            mpb.Clear();
            PrepareRasterBlitPropertyBlock(param, mpb);

            RasterBlit(source, dest, mpb, onComplete);
        }

        /// <summary>
        /// Fills a <see cref="MaterialPropertyBlock"/> with channel/blit parameters from <see cref="ChannelBlitParams"/>.
        /// Does not perform any rendering.
        /// </summary>
        /// <param name="param">Channel mapping and scale/bias rectangle.</param>
        /// <param name="mpb">MaterialPropertyBlock to fill. If null, an internal one is used.</param>
        public static void PrepareRasterBlitPropertyBlock(ChannelBlitParams param, MaterialPropertyBlock mpb)
        {
            mpb.SetVector(ChannelShaderIds.BlitScaleBiasId, new Vector4(param.scale.x, param.scale.y, param.bias.x, param.bias.y));
            mpb.SetVector(ChannelShaderIds.SrcChannelsId, new Vector4((int)param.R.SrcChannel, (int)param.G.SrcChannel, (int)param.B.SrcChannel, (int)param.A.SrcChannel));
            mpb.SetVector(ChannelShaderIds.ChannelOpsId, new Vector4((int)param.R.Ops, (int)param.G.Ops, (int)param.B.Ops, (int)param.A.Ops));
            mpb.SetVector(ChannelShaderIds.ChannelMultsId, new Vector4(param.R.Multiplier, param.G.Multiplier, param.B.Multiplier, param.A.Multiplier));
        }

        /// <summary>
        /// Performs a rasterization blit using a pre-filled <see cref="MaterialPropertyBlock"/>,
        /// enqueues the draw in a shared internal <see cref="CommandBuffer"/>, executes it, 
        /// and invokes a callback when the target is ready via <see cref="AsyncGPUReadback"/>.
        /// </summary>
        /// <param name="source">Source texture (used by the provided <paramref name="mpb"/>).</param>
        /// <param name="dest">Destination render target.</param>
        /// <param name="mpb">Pre-filled <see cref="MaterialPropertyBlock"/> containing all blit parameters.</param>
        /// <param name="onComplete">Callback invoked when the GPU operation is complete, with <paramref name="dest"/> ready to use.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/>, <paramref name="dest"/>, or <paramref name="mpb"/> is null.</exception>
        public static void RasterBlit(Texture source, RenderTexture dest, MaterialPropertyBlock mpb, Action<AsyncGPUReadbackRequest> onComplete)
        {
            k_Cmd.Clear();
            RasterBlit(k_Cmd, source, dest, mpb);
            Graphics.ExecuteCommandBuffer(k_Cmd);

            if (onComplete != null)
            {
                AsyncGPUReadback.Request(dest, 0, TextureFormat.RGBA32, onComplete);
            }
        }

        /// <summary>
        /// Records a rasterization blit into a provided <see cref="CommandBuffer"/> using an
        /// already prepared <see cref="MaterialPropertyBlock"/>.
        /// The source texture is bound at dispatch time.
        /// The command buffer is not executed by this method.
        /// </summary>
        /// <param name="cmd">CommandBuffer to record the blit into.</param>
        /// <param name="source">Source texture to bind for this draw.</param>
        /// <param name="dest">Destination render target.</param>
        /// <param name="mpb">
        /// Caller-owned <see cref="MaterialPropertyBlock"/> containing all channel and blit parameters.
        /// Must not be modified while the command buffer is in flight.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="cmd"/>, <paramref name="source"/>, <paramref name="dest"/> or <paramref name="mpb"/> is null.
        /// </exception>
        public static void RasterBlit(CommandBuffer cmd, Texture source, RenderTexture dest, MaterialPropertyBlock mpb)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (dest == null)
                throw new ArgumentNullException(nameof(dest));
            if (mpb == null)
                throw new ArgumentNullException(nameof(mpb));

            mpb.SetTexture(ChannelShaderIds.BlitTextureId, source);
            cmd.SetRenderTarget(dest);
            cmd.DrawProcedural(Matrix4x4.identity, k_RasterBlitMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);
        }

        /// <summary>
        /// Performs a compute-based blit from a single source texture to a destination render texture.
        /// All channels (R,G,B,A) are taken from <paramref name="source"/>.
        /// Uses AsyncGPUReadback if <paramref name="onComplete"/> is provided, otherwise just dispatches.
        /// </summary>
        /// <param name="source">Source texture containing the pixel data to blit.</param>
        /// <param name="dest">Destination render texture where the result will be written.</param>
        /// <param name="param">Channel mapping and scale/bias parameters.</param>
        /// <param name="stretchToFit">
        /// If true, the source texture will be stretched to match the destination resolution.
        /// If false, the scale/bias values from <paramref name="param"/> are applied instead.
        /// </param>
        /// <param name="onComplete">
        /// Optional callback invoked once the GPU blit is complete and <paramref name="dest"/> is ready to use.
        /// If null, no callback is performed.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="source"/> or <paramref name="dest"/> is null.
        /// </exception>
        public static void ComputeBlit(
            Texture source,
            RenderTexture dest,
            ChannelBlitParams param,
            bool stretchToFit = true,
            Action<AsyncGPUReadbackRequest> onComplete = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (dest == null) throw new ArgumentNullException(nameof(dest));

            param.R.SrcTexture = SourceTexture.Texture0;
            param.G.SrcTexture = SourceTexture.Texture0;
            param.B.SrcTexture = SourceTexture.Texture0;
            param.A.SrcTexture = SourceTexture.Texture0;

            ComputeBlit(source, null, null, null, dest, param, stretchToFit, onComplete);
        }

        /// <summary>
        /// Performs a compute-based blit from a single source texture to a destination render texture,
        /// recording the commands into the provided <see cref="CommandBuffer"/>.
        /// All channels (R,G,B,A) are taken from <paramref name="source"/>.
        /// </summary>
        /// <param name="cmd">CommandBuffer to record the dispatch commands into. Must not be null.</param>
        /// <param name="source">Source texture containing the pixel data to blit.</param>
        /// <param name="dest">Destination render texture where the result will be written.</param>
        /// <param name="param">Channel mapping and scale/bias parameters.</param>
        /// <param name="stretchToFit">
        /// If true, the source texture will be stretched to match the destination resolution.
        /// If false, the scale/bias values from <paramref name="param"/> are applied instead.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="cmd"/>, <paramref name="source"/>, or <paramref name="dest"/> is null.
        /// </exception>
        public static void ComputeBlit(
            CommandBuffer cmd,
            Texture source,
            RenderTexture dest,
            ChannelBlitParams param,
            bool stretchToFit = true)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd), "CommandBuffer cannot be null.");
            if (source == null) throw new ArgumentNullException(nameof(source), "Source texture cannot be null.");
            if (dest == null) throw new ArgumentNullException(nameof(dest), "Destination texture cannot be null.");

            param.R.SrcTexture = SourceTexture.Texture0;
            param.G.SrcTexture = SourceTexture.Texture0;
            param.B.SrcTexture = SourceTexture.Texture0;
            param.A.SrcTexture = SourceTexture.Texture0;

            ComputeBlit(cmd, source, null, null, null, dest, param, stretchToFit);
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
        /// <param name="channelParam">Per-channel mapping and source selection.</param>
        private static void PrepareComputeBlitData(
            ref Texture tex0,
            ref Texture tex1,
            ref Texture tex2,
            ref Texture tex3,
            ChannelBlitParams channelParam)
        {
            static bool ValidateChannel(ChannelData ch, Texture tex0, Texture tex1, Texture tex2, Texture tex3)
            {
                if (ch.SrcChannel == Channel.None || ch.SrcTexture == SourceTexture.None)
                    return false;

                Texture tex = ch.SrcTexture switch
                {
                    SourceTexture.Texture0 => tex0,
                    SourceTexture.Texture1 => tex1,
                    SourceTexture.Texture2 => tex2,
                    SourceTexture.Texture3 => tex3,
                    _ => null
                };

                if (tex == null)
                    throw new InvalidOperationException($"Channel references Source {ch.SrcChannel} on missing texture {ch.SrcTexture}");

                return true;
            }

            bool ch0Valid = ValidateChannel(channelParam.R, tex0, tex1, tex2, tex3);
            bool ch1Valid = ValidateChannel(channelParam.G, tex0, tex1, tex2, tex3);
            bool ch2Valid = ValidateChannel(channelParam.B, tex0, tex1, tex2, tex3);
            bool ch3Valid = ValidateChannel(channelParam.A, tex0, tex1, tex2, tex3);

            if (!(ch0Valid || ch1Valid || ch2Valid || ch3Valid))
                throw new InvalidOperationException("ComputeBlit aborted: no valid source texture is referenced by any channel mapping.");

            tex0 = tex0 ?? Texture2D.blackTexture;
            tex1 = tex1 ?? Texture2D.blackTexture;
            tex2 = tex2 ?? Texture2D.blackTexture;
            tex3 = tex3 ?? Texture2D.blackTexture;
        }

        /// <summary>
        /// Performs a compute-based blit using up to 4 source textures, using a shared internal <see cref="CommandBuffer"/>.
        /// Automatically binds the textures to the compute shader and sets <see cref="ChannelBlitParams"/> source slots.
        /// Supports per-channel remapping and scale/bias for sub-region blitting.
        /// </summary>
        /// <param name="tex0">Source texture 0 (used if any channel references Texture0).</param>
        /// <param name="tex1">Source texture 1 (used if any channel references Texture1).</param>
        /// <param name="tex2">Source texture 2 (used if any channel references Texture2).</param>
        /// <param name="tex3">Source texture 3 (used if any channel references Texture3).</param>
        /// <param name="dest">Destination render texture where the result is written.</param>
        /// <param name="channelParam">Per-channel mapping and source selection.</param>
        /// <param name="stretchToFit">
        /// If true, each source texture will be automatically stretched to fit the destination resolution.
        /// If false, the scale/bias values from <paramref name="channelParam"/> are applied directly.
        /// </param>
        /// <param name="onComplete">Optional callback invoked when the GPU dispatch is finished, with <paramref name="dest"/> ready to read.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="dest"/> is null.</exception>
        public static void ComputeBlit(
            Texture tex0,
            Texture tex1,
            Texture tex2,
            Texture tex3,
            RenderTexture dest,
            ChannelBlitParams channelParam,
            bool stretchToFit = true,
            Action<AsyncGPUReadbackRequest> onComplete = null)
        {
            k_Cmd.Clear();
            ComputeBlit(k_Cmd, tex0, tex1, tex2, tex3, dest, channelParam, stretchToFit);
            Graphics.ExecuteCommandBuffer(k_Cmd);

            if (onComplete != null)
            {
                AsyncGPUReadback.Request(dest, 0, TextureFormat.RGBA32, onComplete);
            }
        }

        /// <summary>
        /// Fills a CommandBuffer with a compute-based blit using up to 4 source textures.
        /// Does not block; user is responsible for executing the CommandBuffer.
        /// </summary>
        /// <param name="cmd">CommandBuffer to fill with the dispatch.</param>
        /// <param name="tex0">Source texture 0 (used if any channel references Texture0).</param>
        /// <param name="tex1">Source texture 1 (used if any channel references Texture1).</param>
        /// <param name="tex2">Source texture 2 (used if any channel references Texture2).</param>
        /// <param name="tex3">Source texture 3 (used if any channel references Texture3).</param>
        /// <param name="dest">Destination render texture.</param>
        /// <param name="channelParam">Per-channel mapping and source selection.</param>
        /// <param name="stretchToFit">
        /// If true, source textures are automatically scaled to fit the destination resolution.
        /// If false, scale/bias from <paramref name="channelParam"/> are applied instead.
        /// </param>
        public static void ComputeBlit(
            CommandBuffer cmd,
            Texture tex0,
            Texture tex1,
            Texture tex2,
            Texture tex3,
            RenderTexture dest,
            ChannelBlitParams channelParam,
            bool stretchToFit = true)
        {
            if (cmd == null)
                throw new ArgumentNullException(nameof(cmd), "ComputeBlitCommandBuffer: CommandBuffer cannot be null. Provide a valid CommandBuffer to record GPU commands.");

            if (dest == null)
                throw new ArgumentNullException(nameof(dest), "ComputeBlitCommandBuffer: Destination RenderTexture cannot be null. Provide a valid RenderTexture as the target for the blit.");

            PrepareComputeBlitData(ref tex0, ref tex1, ref tex2, ref tex3, channelParam);

            static Vector4 TexelSize(Texture tex) => new Vector4(1f / tex.width, 1f / tex.height, tex.width, tex.height);

            cmd.SetComputeTextureParam(k_ComputeBlitShader, 0, ComputeBlitShaderIds.BlitTexture0, tex0);
            cmd.SetComputeVectorParam(k_ComputeBlitShader, ComputeBlitShaderIds.BlitTexture0_TexelSizeId, TexelSize(tex0));

            cmd.SetComputeTextureParam(k_ComputeBlitShader, 0, ComputeBlitShaderIds.BlitTexture1, tex1);
            cmd.SetComputeVectorParam(k_ComputeBlitShader, ComputeBlitShaderIds.BlitTexture1_TexelSizeId, TexelSize(tex1));

            cmd.SetComputeTextureParam(k_ComputeBlitShader, 0, ComputeBlitShaderIds.BlitTexture2, tex2);
            cmd.SetComputeVectorParam(k_ComputeBlitShader, ComputeBlitShaderIds.BlitTexture2_TexelSizeId, TexelSize(tex2));

            cmd.SetComputeTextureParam(k_ComputeBlitShader, 0, ComputeBlitShaderIds.BlitTexture3, tex3);
            cmd.SetComputeVectorParam(k_ComputeBlitShader, ComputeBlitShaderIds.BlitTexture3_TexelSizeId, TexelSize(tex3));

            cmd.SetComputeVectorParam(k_ComputeBlitShader, ChannelShaderIds.BlitScaleBiasId, new Vector4(channelParam.scale.x, channelParam.scale.y, channelParam.bias.x, channelParam.bias.y));
            cmd.SetComputeVectorParam(k_ComputeBlitShader, ChannelShaderIds.SrcChannelsId, new Vector4((int)channelParam.R.SrcChannel, (int)channelParam.G.SrcChannel, (int)channelParam.B.SrcChannel, (int)channelParam.A.SrcChannel));
            cmd.SetComputeVectorParam(k_ComputeBlitShader, ChannelShaderIds.SrcTexturesId, new Vector4((int)channelParam.R.SrcTexture, (int)channelParam.G.SrcTexture, (int)channelParam.B.SrcTexture, (int)channelParam.A.SrcTexture));
            cmd.SetComputeVectorParam(k_ComputeBlitShader, ChannelShaderIds.ChannelOpsId, new Vector4((int)channelParam.R.Ops, (int)channelParam.G.Ops, (int)channelParam.B.Ops, (int)channelParam.A.Ops));
            cmd.SetComputeVectorParam(k_ComputeBlitShader, ChannelShaderIds.ChannelMultsId, new Vector4(channelParam.R.Multiplier, channelParam.G.Multiplier, channelParam.B.Multiplier, channelParam.A.Multiplier));

            cmd.SetComputeIntParam(k_ComputeBlitShader, ComputeBlitShaderIds.BlitStretchToFitId, stretchToFit ? 1 : 0);
            cmd.SetComputeTextureParam(k_ComputeBlitShader, 0, ComputeBlitShaderIds.BlitDestinationId, dest);
            cmd.SetComputeVectorParam(k_ComputeBlitShader, ComputeBlitShaderIds.BlitDestResId, new Vector2(dest.width, dest.height));

            int threadGroupX = Mathf.CeilToInt(dest.width / 8f);
            int threadGroupY = Mathf.CeilToInt(dest.height / 8f);

            cmd.DispatchCompute(k_ComputeBlitShader, 0, threadGroupX, threadGroupY, 1);
        }

        /// <summary>
        /// Placeholder for a compute-based blit implementation.
        /// </summary>
        /// <remarks>
        /// Technical debt:
        /// Compute-side constant buffer support is not implemented yet.
        /// A future revision may introduce a parameter buffer to match the raster path.
        /// </remarks>
        private static void ComputeBlit() { }
    }
}
