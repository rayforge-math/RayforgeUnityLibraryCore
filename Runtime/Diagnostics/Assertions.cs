using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Rayforge.Core.Diagnostics
{
    /// <summary>
    /// Provides editor/development-only runtime assertions for validating values, objects, and delegates.
    /// All calls are stripped in non-development builds if UNITY_EDITOR or DEVELOPMENT_BUILD is not defined.
    /// </summary>
    public static class Assertions
    {
        /// <summary>
        /// Provides development-only assertions to validate internal logic invariants.
        /// Stripped in Release builds; throws in Development builds; logs in Editor.
        /// </summary>
        /// <remarks>
        /// Use for: Internal logic errors or "impossible" state transitions.
        /// DO NOT use for: Public API validation, user input, or errors requiring runtime handling.
        /// Use <see cref="System.Exception"/>-based validation for public interfaces.
        /// </remarks>
        /// <param name="condition">Condition that must be true.</param>
        /// <param name="error">Message describing the logic violation.</param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Assert(
            bool condition,
            string error,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
#if UNITY_EDITOR
            if (!condition)
                UnityEngine.Debug.LogError($"{error}\n{file}:{line} ({member})");
#else
            if (!condition)
                throw new System.Exception($"{error}\n{file}:{line} ({member})");
#endif
        }

        /// <summary>
        /// Validates that an object reference is not null.
        /// Logs or throws an error if the object is null.  
        /// Caller info (<paramref name="file"/>, <paramref name="line"/>, <paramref name="member"/>) is automatically supplied by the compiler.
        /// </summary>
        /// <typeparam name="T">The type of the object being validated.</typeparam>
        /// <param name="obj">The object to validate. Must not be null.</param>
        /// <param name="error">Optional custom error message.</param>
        /// <param name="file">Compiler-supplied source file path. Supplied automatically.</param>
        /// <param name="line">Compiler-supplied line number. Supplied automatically.</param>
        /// <param name="member">Compiler-supplied calling member name. Supplied automatically.</param>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void NotNull<T>(
            T obj,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(obj != null, error ?? "Object must not be null.", file, line, member);
        }

        /// <summary>
        /// Validates that the provided object is of the expected type <typeparamref name="TExpected"/>.
        /// Throws or logs an error if the object is not of that type.  
        /// Caller info is automatically supplied by the compiler.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void IsTypeOf<TExpected>(
            object obj,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(obj is TExpected, error ?? $"Object must be of type {typeof(TExpected).Name}.", file, line, member);
        }

        /// <summary>
        /// Validates that an integer value is greater than zero.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void GreaterThanZero(
            int value,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(value > 0, error ?? "Value must be greater than 0.", file, line, member);
        }

        /// <summary>
        /// Validates that a floating-point value is greater than zero.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void GreaterThanZero(
            float value,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(value > 0f, error ?? "Value must be greater than 0.", file, line, member);
        }

        /// <summary>
        /// Validates that an integer value is greater than or equal to zero.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void AtLeastZero(
            int value,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(value >= 0, error ?? "Value must be greater than or equal to 0.", file, line, member);
        }

        /// <summary>
        /// Validates that a floating-point value is greater than or equal to zero.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void AtLeastZero(
            float value,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(value >= 0f || Mathf.Approximately(value, 0f), error ?? "Value must be greater than or equal to 0.", file, line, member);
        }

        /// <summary>
        /// Validates that an integer value is greater than zero (alias for AtLeastOne).
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void AtLeastOne(
            int value,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(value > 0, error ?? "Value must be greater than 0.", file, line, member);
        }

        /// <summary>
        /// Validates that a floating-point value is greater than zero (alias for AtLeastOne).
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void AtLeastOne(
            float value,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(value > 0f || Mathf.Approximately(value, 0f), error ?? "Value must be greater than 0.", file, line, member);
        }

        /// <summary>
        /// Validates that an integer value is not zero.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void NotZero(
            int value,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(value != 0, error ?? "Value must not be zero.", file, line, member);
        }

        /// <summary>
        /// Validates that a floating-point value is not zero.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void NotZero(
            float value,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(value != 0f, error ?? "Value must not be zero.", file, line, member);
        }

        /// <summary>
        /// Validates that a numeric value lies within a specified inclusive range.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void InRange(
            int value, int min, int max,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(value >= min && value <= max, error ?? $"Value must be in range [{min}, {max}].", file, line, member);
        }

        /// <summary>
        /// Validates that a numeric value lies within a specified inclusive range.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void InRange(
            float value, float min, float max,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(value >= min && value <= max, error ?? $"Value must be in range [{min}, {max}].", file, line, member);
        }

        /// <summary>
        /// Validates that a boolean value is true.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void IsTrue(
            bool value,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(value, error ?? "Value must be true.", file, line, member);
        }

        /// <summary>
        /// Validates that a boolean value is false.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void IsFalse(
            bool value,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(!value, error ?? "Value must be false.", file, line, member);
        }

        /// <summary>
        /// Validates that two values are equal.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void AreEqual<T>(
            T a, T b,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(Equals(a, b), error ?? $"Values must be equal: {a} != {b}", file, line, member);
        }

        /// <summary>
        /// Validates that two values are not equal.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void AreNotEqual<T>(
            T a, T b,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(!Equals(a, b), error ?? $"Values must not be equal: {a} == {b}", file, line, member);
        }

        /// <summary>
        /// Validates that a delegate reference is not null.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void NotNullDelegate<TDelegate>(
            TDelegate func,
            string error = null,
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0,
            [CallerMemberName] string member = "")
        {
            Assert(func != null, error ?? "Delegate must not be null.", file, line, member);
        }
    }
}