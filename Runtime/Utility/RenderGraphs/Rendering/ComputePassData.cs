using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Rayforge.Core.Rendering.Passes;

namespace Rayforge.Core.Utility.RenderGraphs.Rendering
{
    /// <summary>
    /// Base class for compute RenderGraph pass input/output configuration and dispatch metadata.
    /// Contains the destination texture(s) and the core compute pass metadata.
    /// </summary>
    /// <remarks>
    /// This class is declared as <c>partial</c> to allow projects to extend compute pass data
    /// with additional fields that should be present on all compute passes of this type
    /// (e.g. debug flags, frame indices, shared constants, or platform-specific parameters),
    /// without requiring inheritance or modification of the core framework.
    /// 
    /// Extensions should remain data-only and must not introduce execution logic.
    /// 
    /// Any per-dispatch logic should instead be implemented via <see cref="RenderFuncUpdate"/>,
    /// which receives the fully populated pass data instance and allows binding of resources
    /// and constants without capturing external state.
    /// 
    /// This design follows Unity's RenderGraph principles:
    /// pass data is immutable during execution, value-type based, and free of per-frame
    /// heap allocations, ensuring predictable performance and zero-GC behavior.
    /// </remarks>
    /// <typeparam name="TDerived">
    /// Concrete pass data type (CRTP) enabling type-safe callbacks and extensions without allocations.
    /// </typeparam>
    public abstract partial class ComputePassData<TDerived> : PassDataBase<TDerived, ComputePassMeta>
        where TDerived : ComputePassData<TDerived>
    {
        /// <summary>
        /// Optional callback invoked before dispatch to bind resources, set constants, etc.
        /// <para>
        /// <b>Performance note:</b> To avoid heap allocations per frame and to comply with RenderGraph's
        /// GC-free design, this callback should be assigned using a <c>static</c> lambda whenever possible.
        /// </para>
        /// <para>
        /// Using non-static lambdas or capturing local variables will create a closure object on the heap,
        /// which can result in per-frame allocations and temporary GC pressure.
        /// </para>
        /// <para>
        /// This design follows Unity's RenderGraph pattern, where all internal pass data and dispatch
        /// logic is value-type-based and heap-free, ensuring predictable frame timings and zero GC overhead.
        /// </para>
        /// </summary>
        public Action<ComputeCommandBuffer, TDerived> RenderFuncUpdate { get; set; }

        /// <summary>
        /// Fetches all data from another raster pass instance.
        /// <para>
        /// Base inputs, destination, and metadata are fetched via <see cref="PassDataBase{TDerived, TMeta}.FetchFrom"/>.
        /// Child-specific fields such as <see cref="RenderFuncUpdate"/> are then transferred.
        /// </para>
        /// <para>
        /// Inputs and destination in <paramref name="other"/> are consumed and reset.
        /// </para>
        /// </summary>
        /// <param name="other">The source raster pass data instance to fetch from.</param>
        public void FetchFrom(ComputePassData<TDerived> other)
        {
            base.FetchFrom(other);

            RenderFuncUpdate = other.RenderFuncUpdate;
        }
    }
}