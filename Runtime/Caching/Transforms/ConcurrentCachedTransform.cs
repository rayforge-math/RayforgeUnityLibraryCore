using Rayforge.Core.Caching.Abstractions;
using UnityEngine;

namespace Rayforge.Core.Caching.Transforms
{
    /// <summary>
    /// Thread-safe variant of <see cref="CachedTransform"/>.
    /// Wraps all cache and Transform accessors with a synchronization lock,
    /// allowing safe multi-threaded reads/writes to cached transform data.
    ///
    /// Note: Unity's Transform API is not thread-safe — only cached values are safe
    /// to access from background threads. Direct UnityEngine.Transform operations
    /// (through <see cref="Self"/>) must still occur on the main thread.
    /// </summary>
    public class ConcurrentCachedTransform : CachedTransform
    {
        /// <summary>
        /// Lock object used to synchronize access to cached state and Unity Transform operations.
        /// </summary>
        private readonly object m_Lock = new();

        /// <summary>
        /// Initializes a new instance of <see cref="ConcurrentCachedTransform"/> using the given GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to wrap with caching and locking.</param>
        public ConcurrentCachedTransform(GameObject gameObject)
            : base(gameObject)
        { }

        /// <summary>
        /// Creates a new <see cref="CachedTransform"/> by instantiating a new <see cref="GameObject"/> with the given name.
        /// </summary>
        /// <param name="name">The name of the new GameObject.</param>
        /// <returns>A new <see cref="CachedTransform"/> instance.</returns>
        public new static ConcurrentCachedTransform Create(string name)
        {
            var gameObject = new GameObject(name);
            return new ConcurrentCachedTransform(gameObject);
        }

        /// <summary>
        /// Creates a new <see cref="CachedTransform"/> by instantiating a new <see cref="GameObject"/> 
        /// and linking it to a parent <see cref="ICachedTransform"/>.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent, which must implement <see cref="ICachedTransform"/>.</typeparam>
        /// <param name="name">The name of the new GameObject.</param>
        /// <param name="parent">The parent instance to attach to. If not null, the GameObject's transform is parented in Unity.</param>
        /// <returns>A new <see cref="CachedTransform"/> instance with the specified parent.</returns>
        public new static ConcurrentCachedTransform Create<TParent>(string name, TParent parent)
            where TParent : ICachedTransform
        {
            var gameObject = new GameObject(name);
            var t = gameObject.transform;

            if (parent != null)
            {
                t.SetParent(parent.Self);
            }

            return new ConcurrentCachedTransform(gameObject) { m_Parent = parent };
        }

        /// <inheritdoc/>
        public override Vector3 Position
        {
            get
            {
                lock (m_Lock)
                    return base.Position;
            }
            set
            {
                lock (m_Lock)
                    base.Position = value;
            }
        }

        /// <inheritdoc/>
        public override Quaternion Rotation
        {
            get
            {
                lock (m_Lock)
                    return base.Rotation;
            }
            set
            {
                lock (m_Lock)
                    base.Rotation = value;
            }
        }

        /// <inheritdoc/>
        public override Vector3 Scale
        {
            get
            {
                lock (m_Lock)
                    return base.Scale;
            }
            set
            {
                lock (m_Lock)
                    base.Scale = value;
            }
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            var t = Self;
            lock (m_Lock)
            {
                m_CachedPosition = t.position;
                m_CachedRotation = t.rotation;
                m_CachedScale = t.localScale;
            }
        }
    }
}