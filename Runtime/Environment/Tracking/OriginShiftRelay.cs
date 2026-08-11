using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Tracking
{
    /// <summary>
    /// A relay that tracks accumulated movement. 
    /// Fires an event only when the distance from the last 'stable origin' exceeds the threshold.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class OriginShiftRelay : MonoBehaviour
    {
        #region Settings

        [Header("Detection")]
        [Tooltip("The accumulated distance required to trigger a shift event.")]
        [SerializeField]
        private float shiftThreshold = 100f;

        #endregion

        #region Events

        /// <summary>
        /// Fired when the total movement since the last reset exceeds the threshold.
        /// <para>Parameter: The total delta vector (current position - last stable position).</para>
        /// </summary>
        public event Action<Vector3> OnWorldShiftDetected;

        #endregion

        #region Properties

        private Vector3 m_LastStablePosition;
        private float m_SqrThreshold;

        /// <summary>
        /// Gets the world position where the last shift was triggered.
        /// </summary>
        public Vector3 LastStablePosition => m_LastStablePosition;

        /// <summary>
        /// Gets the squared distance threshold currently in use.
        /// </summary>
        public float SqrThreshold => m_SqrThreshold;

        /// <summary>
        /// Gets or sets the linear distance threshold for triggering a shift event.
        /// </summary>
        public float ShiftThreshold => shiftThreshold;

        #endregion

        #region Monobehavior

        private void Awake()
        {
            m_LastStablePosition = transform.position;
            UpdateThreshold(shiftThreshold);
        }

        private void OnValidate()
        {
            // Ensure threshold is valid during Inspector editing
            if (shiftThreshold > 0f)
            {
                UpdateThreshold(shiftThreshold);
            }
        }

        private void LateUpdate()
        {
            Vector3 currentPosition = transform.position;
            Vector3 totalDeltaSinceLastShift = currentPosition - m_LastStablePosition;
            float sqrDistance = totalDeltaSinceLastShift.sqrMagnitude;

            // Only trigger if the total distance traveled exceeds the squared threshold.
            if (sqrDistance >= m_SqrThreshold)
            {
                OnWorldShiftDetected?.Invoke(totalDeltaSinceLastShift);
                m_LastStablePosition = currentPosition;
            }
        }

        #endregion

        #region Control

        /// <summary>
        /// Updates the movement threshold and recalculates the internal squared threshold.
        /// </summary>
        /// <param name="threshold">The new distance threshold. Must be greater than zero.</param>
        /// <exception cref="ArgumentException">Thrown when threshold is less than or equal to zero.</exception>
        public void UpdateThreshold(float threshold)
        {
            if (threshold <= 0f)
            {
                throw new ArgumentException("Threshold must be greater than zero.", nameof(threshold));
            }

            shiftThreshold = threshold;
            m_SqrThreshold = shiftThreshold * shiftThreshold;
        }

        /// <summary>
        /// Manually resets the tracking origin to the current transform position.
        /// Useful if you want to force the system to consider the current location as the new 'Zero'.
        /// </summary>
        public void ResetOrigin()
        {
            m_LastStablePosition = transform.position;
        }

        #endregion
    }
}