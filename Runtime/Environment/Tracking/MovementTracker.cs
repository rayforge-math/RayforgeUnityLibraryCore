using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rayforge.Core.Environment.Tracking
{
    /// <summary>
    /// Central hub to track movement of multiple entities and trigger LOD or Shift events.
    /// Implements a Singleton pattern for easy access and automatic lifecycle management.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class MovementTracker : MonoBehaviour
    {
        #region Singleton Logic

        private static MovementTracker _instance;
        private static bool _isApplicationQuitting = false;

        /// <summary>
        /// Access the global instance of the MovementTracker. 
        /// Automatically creates one if it doesn't exist in the scene.
        /// </summary>
        public static MovementTracker Instance
        {
            get
            {
                if (_isApplicationQuitting) return null;

                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<MovementTracker>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject("Central Movement Tracker (Auto-Generated)");
                        _instance = go.AddComponent<MovementTracker>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        /// <summary>
        /// Internal data structure for optimized tracking. 
        /// Using a struct ensures data is stored contiguously in memory.
        /// </summary>
        private struct TrackedEntity
        {
            public Transform Transform;
            public Vector3 LastStablePosition;
            public float SqrThreshold;
            public Action<Vector3> OnShiftAction;

            /// <summary>
            /// Initializes a new tracking data point.
            /// </summary>
            /// <param name="t">The transform to monitor.</param>
            /// <param name="threshold">The distance threshold for events.</param>
            /// <param name="onShift">Callback triggered when threshold is exceeded.</param>
            public TrackedEntity(Transform t, float threshold, Action<Vector3> onShift)
            {
                Transform = t;
                LastStablePosition = t.position;
                SqrThreshold = threshold * threshold;
                OnShiftAction = onShift;
            }
        }

        /// <summary>
        /// List of all entities currently being monitored.
        /// </summary>
        private readonly List<TrackedEntity> _entities = new List<TrackedEntity>();

        #region API Methods

        /// <summary>
        /// Registers a new transform for movement tracking.
        /// </summary>
        /// <param name="target">The transform to track.</param>
        /// <param name="threshold">Distance to travel before firing the event.</param>
        /// <param name="onShift">Action to execute on threshold breach (receives delta vector).</param>
        public void RegisterEntity(Transform target, float threshold, Action<Vector3> onShift)
        {
            if (target == null) return;

            _entities.Add(new TrackedEntity(target, threshold, onShift));
        }

        /// <summary>
        /// Removes a transform from tracking. 
        /// Should be called in OnDisable to prevent memory leaks.
        /// </summary>
        /// <param name="target">The transform to stop tracking.</param>
        public void UnregisterEntity(Transform target)
        {
            if (_isApplicationQuitting) return;

            for (int i = _entities.Count - 1; i >= 0; i--)
            {
                if (_entities[i].Transform == target)
                {
                    _entities.RemoveAt(i);
                }
            }
        }

        #endregion

        #region Engine Callbacks

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// LateUpdate ensures all physical or script-based movement is completed
        /// before we calculate the distance deltas.
        /// </summary>
        private void LateUpdate()
        {
            int count = _entities.Count;

            for (int i = 0; i < count; i++)
            {
                var entity = _entities[i];

                if (entity.Transform == null)
                {
                    continue;
                }

                Vector3 currentPos = entity.Transform.position;
                Vector3 delta = currentPos - entity.LastStablePosition;

                if (delta.sqrMagnitude >= entity.SqrThreshold)
                {
                    entity.OnShiftAction?.Invoke(delta);

                    entity.LastStablePosition = currentPos;
                    _entities[i] = entity;
                }
            }
        }

        private void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }

        #endregion
    }
}