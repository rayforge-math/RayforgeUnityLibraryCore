using System;
using UnityEngine;

using Rayforge.Core.Caching.Abstractions;

namespace Rayforge.Core.Caching.Transforms
{
    /// <summary>
    /// A cached wrapper around a Unity <see cref="Transform"/> that stores position, rotation, and scale locally
    /// for efficient access, while keeping them synchronized with the underlying Unity object.
    ///
    /// This wrapper allows systems to access and modify transform data without repeated engine calls,
    /// and provides an abstraction layer suitable for multi-threaded or data-oriented code.
    ///
    /// The associated <see cref="GameObject"/> is automatically destroyed when this instance is disposed.
    /// </summary>
    public class CachedTransform : ICachedTransform
    {
        private const string Tag = "CachedTransform";

        private GameObject m_GameObject;
        protected ICachedTransform m_Parent;

        protected Vector3 m_CachedPosition;
        protected Quaternion m_CachedRotation;
        protected Vector3 m_CachedScale;

        /// <summary>
        /// Gets the underlying Unity <see cref="Transform"/> instance associated with this cached transform.
        /// Use this property only when direct Unity API access is required.
        /// </summary>
        public Transform Self => m_GameObject != null ? m_GameObject.transform : null;

        /// <summary>
        /// Initializes a new <see cref="CachedTransform"/> that wraps the specified <see cref="GameObject"/>.
        /// </summary>
        /// <param name="gameObject">The GameObject to wrap and cache transform data from.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="gameObject"/> is null.</exception>
        public CachedTransform(GameObject gameObject)
        {
            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject), $"{Tag}: GameObject cannot be null.");
            }

            m_GameObject = gameObject;
            var t = m_GameObject.transform;
            m_CachedPosition = t.position;
            m_CachedRotation = t.rotation;
            m_CachedScale = t.localScale;
        }

        /// <summary>
        /// Finalizer ensures cleanup if <see cref="Dispose"/> was not called manually.
        /// </summary>
        ~CachedTransform()
        {
            Dispose();
        }

        /// <summary>
        /// Creates a new <see cref="CachedTransform"/> by instantiating a new <see cref="GameObject"/> with the given name.
        /// </summary>
        /// <param name="name">The name of the new GameObject.</param>
        /// <returns>A new <see cref="CachedTransform"/> instance.</returns>
        public static CachedTransform Create(string name)
        {
            var gameObject = new GameObject(name);
            return new CachedTransform(gameObject);
        }

        /// <summary>
        /// Creates a new <see cref="CachedTransform"/> by instantiating a new <see cref="GameObject"/> 
        /// and linking it to a parent <see cref="ICachedTransform"/>.
        /// </summary>
        /// <typeparam name="TParent">The type of the parent, which must implement <see cref="ICachedTransform"/>.</typeparam>
        /// <param name="name">The name of the new GameObject.</param>
        /// <param name="parent">The parent instance to attach to. If not null, the GameObject's transform is parented in Unity.</param>
        /// <returns>A new <see cref="CachedTransform"/> instance with the specified parent.</returns>
        public static CachedTransform Create<TParent>(string name, TParent parent)
            where TParent : ICachedTransform
        {
            var gameObject = new GameObject(name);
            var t = gameObject.transform;

            if (parent != null)
            {
                t.SetParent(parent.Self);
            }

            return new CachedTransform(gameObject) { m_Parent = parent };
        }

        /// <inheritdoc/>
        /// <remarks>Set can only be called from Main Thread.</remarks>
        public virtual Vector3 Position
        {
            get => m_CachedPosition;
            set
            {
                if (m_CachedPosition != value)
                {
                    m_CachedPosition = value;
                    Self.position = value;
                }
            }
        }

        /// <inheritdoc/>
        /// <remarks>Set can only be called from Main Thread.</remarks>
        public virtual Quaternion Rotation
        {
            get => m_CachedRotation;
            set
            {
                if (m_CachedRotation != value)
                {
                    m_CachedRotation = value;
                    Self.rotation = value;
                }
            }
        }

        /// <inheritdoc/>
        /// <remarks>Set can only be called from Main Thread.</remarks>
        public virtual Vector3 Scale
        {
            get => m_CachedScale;
            set
            {
                if (m_CachedScale != value)
                {
                    m_CachedScale = value;
                    Self.localScale = value;
                }
            }
        }

        /// <inheritdoc/>
        /// <remarks>Set can only be called from Main Thread.</remarks>
        public ICachedTransform Parent
        {
            get => m_Parent;
            set
            {
                SetParent(value);
            }
        }

        /// <inheritdoc/>
        /// <remarks>Only call from Main Thread.</remarks>
        public void SetParent(ICachedTransform parent, bool worldPositionStays = false)
        {
            Self.SetParent(parent?.Self, worldPositionStays);
            m_Parent = parent;
            Refresh();
        }

        /// <summary>
        /// Updates the cached position, rotation, and scale from the underlying Unity transform.
        /// Call this if the transform was externally modified.
        /// Only call from Main Thread.
        /// </summary>
        public virtual void Refresh()
        {
            var t = Self;
            m_CachedPosition = t.position;
            m_CachedRotation = t.rotation;
            m_CachedScale = t.localScale;
        }

        /// <inheritdoc/>
        /// <remarks>Only call from Main Thread.</remarks>
        public Tcomp AddComponent<Tcomp>() where Tcomp : Component
            => m_GameObject.AddComponent<Tcomp>();

        /// <summary>
        /// Destroys the underlying GameObject and releases references.
        /// Only call from Main Thread.
        /// </summary>
        public void Dispose()
        {
            if (m_GameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(m_GameObject);
                m_GameObject = null;
                GC.SuppressFinalize(this);
            }
        }
    }
}