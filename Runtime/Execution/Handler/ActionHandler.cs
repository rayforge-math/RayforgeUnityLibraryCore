using Rayforge.Core.Execution.Abstractions;
using System;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Execution.Handler
{
    /// <summary>
    /// A lightweight wrapper that enables the use of lambda expressions (delegates) 
    /// with systems expecting an <see cref="IExecutionHandler{TData}"/>.
    /// <para>
    /// PERFORMANCE TIP: To avoid heap allocations during iteration, use one of the following strategies:
    /// <list type="bullet">
    /// <item>
    /// <description><b>Static Lambdas/Methods:</b> Use <c>static x => ...</c> or <c>static <Type> func(...) ...</c> 
    /// to prevent the compiler from creating a closure object to capture local state.</description>
    /// </item>
    /// <item>
    /// <description><b>Caching:</b> Pre-allocate this struct (and its delegate) in a field 
    /// once and reuse it. This eliminates delegate re-instantiation entirely.</description>
    /// </item>
    /// </list>
    /// </para>
    /// </summary>
    /// <typeparam name="TData">The type of data being processed during iteration.</typeparam>
    public readonly struct ActionHandler<TData> : IExecutionHandler<TData>
    {
        private readonly Action<TData> _action;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionHandler{TData}"/> struct.
        /// </summary>
        /// <param name="action">The delegate to execute for each element.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ActionHandler(Action<TData> action)
        {
            _action = action;
        }

        /// <summary>
        /// Executes the underlying delegate for the provided value.
        /// </summary>
        /// <param name="value">The element being processed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(TData value)
        {
            _action?.Invoke(value);
        }
    }

    /// <summary>
    /// A specialized wrapper that enables the use of stateful lambda expressions 
    /// with systems expecting an <see cref="IExecutionHandler{TData}"/>.
    /// <para>
    /// This version allows passing an external context (<typeparamref name="TState"/>) 
    /// into a static delegate, effectively preventing heap allocations caused by closures.
    /// </para>
    /// <para>
    /// PERFORMANCE TIP: Use a <b>static lambda</b> (e.g., <c>static (val, state) => ...</c>) 
    /// to ensure the compiler does not create a hidden capture class.
    /// </para>
    /// </summary>
    /// <typeparam name="TData">The type of data being processed during iteration.</typeparam>
    /// <typeparam name="TState">The type of the external state/context to pass through.</typeparam>
    public readonly struct StatefulActionHandler<TData, TState> : IExecutionHandler<TData>
    {
        private readonly TState _state;
        private readonly Action<TData, TState> _action;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionHandler{TData, TState}"/> struct.
        /// </summary>
        /// <param name="state">The external context or state to be passed to the delegate.</param>
        /// <param name="action">The delegate to execute, accepting both the element and the state.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatefulActionHandler(TState state, Action<TData, TState> action)
        {
            _state = state;
            _action = action;
        }

        /// <summary>
        /// Executes the underlying delegate using the provided value and the stored state.
        /// </summary>
        /// <param name="value">The element being processed.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(TData value)
        {
            _action?.Invoke(value, _state);
        }
    }
}