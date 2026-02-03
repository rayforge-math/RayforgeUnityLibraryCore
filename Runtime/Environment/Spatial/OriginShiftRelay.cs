using System;
using UnityEngine;

namespace Rayforge.Core.Environment.Spatial
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
        public float shiftThreshold = 100f;
        #endregion

        #region Events
        /// <summary>
        /// Fired when the total movement since the last reset exceeds the threshold.
        /// Vector3: The total delta since the last event.
        /// </summary>
        public event Action<Vector3> OnWorldShiftDetected;
        #endregion

        #region Private State
        // We track the position where the last shift was triggered (or Awake).
        private Vector3 _lastStablePosition;
        private float _sqrThreshold;
        #endregion

        private void Awake()
        {
            _lastStablePosition = transform.position;
            UpdateThreshold();
        }

        private void OnValidate() => UpdateThreshold();
        private void UpdateThreshold() => _sqrThreshold = shiftThreshold * shiftThreshold;

        private void LateUpdate()
        {
            Vector3 currentPosition = transform.position;

            Vector3 totalDeltaSinceLastShift = currentPosition - _lastStablePosition;
            float sqrDistance = totalDeltaSinceLastShift.sqrMagnitude;

            // Only trigger if the total distance traveled (accumulated or jump) is enough.
            if (sqrDistance >= _sqrThreshold)
            {
                OnWorldShiftDetected?.Invoke(totalDeltaSinceLastShift);
                _lastStablePosition = currentPosition;

#if UNITY_EDITOR
                Debug.Log($"[OriginShiftRelay] Shift triggered! Accumulated Delta: {totalDeltaSinceLastShift.magnitude:F2}");
#endif
            }
        }

        /// <summary>
        /// Manually resets the tracking origin. 
        /// Useful if you want to force the system to consider the current position as 'Zero'.
        /// </summary>
        public void ResetOrigin()
        {
            _lastStablePosition = transform.position;
        }
    }
}
