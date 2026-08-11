using Rayforge.Core.Execution.Abstractions;
using System;
using System.Runtime.CompilerServices;

namespace Rayforge.Core.Execution.Handler
{
    /// <summary>
    /// A lightweight wrapper that enables the use of lambda expressions (delegates) 
    /// with systems expecting an <see cref="IFunctionHandler{TData, TResult}"/>.
    /// <para>
    /// PERFORMANCE TIP: To avoid heap allocations, use <b>static lambdas</b> or cache the struct.
    /// </para>
    /// </summary>
    /// <typeparam name="TData">The type of data being processed.</typeparam>
    /// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
    public readonly struct FuncHandler<TData, TResult> : IFunctionHandler<TData, TResult>
    {
        private readonly Func<TData, TResult> _func;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncHandler{TData, TResult}"/> struct.
        /// </summary>
        /// <param name="func">The delegate to execute, returning a result.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FuncHandler(Func<TData, TResult> func)
        {
            _func = func;
        }

        /// <summary>
        /// Executes the underlying delegate for the provided value and returns the result.
        /// </summary>
        /// <param name="data">The input data for the operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Execute(TData data)
        {
            return _func != null ? _func.Invoke(data) : default;
        }
    }

    /// <summary>
    /// A specialized wrapper that enables the use of stateful lambda expressions 
    /// with systems expecting an <see cref="IFunctionHandler{TData, TResult}"/>.
    /// <para>
    /// This version allows passing an external context (<typeparamref name="TState"/>) 
    /// into a static delegate, effectively preventing heap allocations caused by closures.
    /// </para>
    /// </summary>
    /// <typeparam name="TData">The type of data being processed.</typeparam>
    /// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
    /// <typeparam name="TState">The type of the external state/context to pass through.</typeparam>
    public readonly struct StatefulFuncHandler<TData, TState, TResult> : IFunctionHandler<TData, TResult>
    {
        private readonly TState _state;
        private readonly Func<TData, TState, TResult> _func;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncHandler{TData, TState, TResult}"/> struct.
        /// </summary>
        /// <param name="state">The external context or state to be passed to the delegate.</param>
        /// <param name="func">The delegate to execute, accepting both the data and the state.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StatefulFuncHandler(TState state, Func<TData, TState, TResult> func)
        {
            _state = state;
            _func = func;
        }

        /// <summary>
        /// Executes the underlying delegate using the provided data and the stored state.
        /// </summary>
        /// <param name="data">The input data for the operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult Execute(TData data)
        {
            return _func != null ? _func.Invoke(data, _state) : default;
        }
    }
}