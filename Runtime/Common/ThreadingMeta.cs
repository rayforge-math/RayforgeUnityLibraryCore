using UnityEngine;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Rayforge.Core.Common
{
    /// <summary>
    /// Provides a global registry for thread-related engine context.
    /// This class ensures the Main Thread ID is captured during the early 
    /// initialization phase of the Unity runtime.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class ThreadingMeta
    {
        /// <summary>
        /// Gets the unique identifier of the Unity Main Thread.
        /// Used by thread-safe utilities to verify execution context.
        /// </summary>
        public static int MainThreadId { get; private set; } = -1;

        /// <summary>
        /// Checks if the current thread is the Unity Main Thread.
        /// </summary>
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

#if UNITY_EDITOR
        static ThreadingMeta()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }
    }
}