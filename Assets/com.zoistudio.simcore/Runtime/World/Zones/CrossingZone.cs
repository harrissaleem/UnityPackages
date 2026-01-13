// SimCore - Crossing Zone
// Zebra crossing that coordinates pedestrian/vehicle interaction

using System.Collections.Generic;
using UnityEngine;
using SimCore.Unity;

namespace SimCore.World.Zones
{
    /// <summary>
    /// Zebra crossing zone. Vehicles should yield when pedestrians are present.
    /// Pedestrians may wait for vehicles to clear before crossing.
    /// </summary>
    public class CrossingZone : Zone
    {
        [Header("Crossing Configuration")]
        [Tooltip("How long a pedestrian will wait for traffic before attempting to cross anyway")]
        [SerializeField] private float _maxWaitTime = 5f;
        
        [Tooltip("Distance at which vehicles start to slow down")]
        [SerializeField] private float _vehicleSlowdownDistance = 15f;
        
        [Tooltip("Distance at which vehicles must stop")]
        [SerializeField] private float _vehicleStopDistance = 8f;
        
        [Header("Crossing State")]
        [SerializeField] private bool _isPedestrianCrossing = false;
        [SerializeField] private bool _isVehiclePassing = false;
        
        // Track waiting pedestrians (not yet crossing)
        private Dictionary<int, float> _waitingPedestrians = new Dictionary<int, float>();
        
        // Track crossing direction for animation purposes
        private Dictionary<int, Vector3> _crossingDirections = new Dictionary<int, Vector3>();
        
        // Properties
        public bool IsPedestrianCrossing => _isPedestrianCrossing;
        public bool IsVehiclePassing => _isVehiclePassing;
        public bool IsClear => !_isPedestrianCrossing && !_isVehiclePassing;
        public float VehicleSlowdownDistance => _vehicleSlowdownDistance;
        public float VehicleStopDistance => _vehicleStopDistance;
        public int WaitingPedestrianCount => _waitingPedestrians.Count;
        
        protected override void Awake()
        {
            _zoneType = ZoneType.Crossing;
            base.Awake();
        }
        
        private void Update()
        {
            // Update waiting times
            UpdateWaitingPedestrians();
            
            // Update crossing state
            _isPedestrianCrossing = _pedestriansInZone.Count > 0;
            _isVehiclePassing = _vehiclesInZone.Count > 0;
        }
        
        private void UpdateWaitingPedestrians()
        {
            if (_waitingPedestrians.Count == 0) return;
            
            // Update wait times and check if any should force cross
            var toRemove = new List<int>();
            foreach (var kvp in _waitingPedestrians)
            {
                // Wait time is tracked by the pedestrian AI, we just track who's waiting
            }
        }
        
        protected override void OnPedestrianEnter(GameObject pedestrian)
        {
            // Remove from waiting list when actually crossing
            _waitingPedestrians.Remove(pedestrian.GetInstanceID());
            
            // Store crossing direction
            var movement = pedestrian.GetComponent<NPCMovement>();
            if (movement != null)
            {
                Vector3 dir = movement.GetCurrentDirection();
                _crossingDirections[pedestrian.GetInstanceID()] = dir;
            }
        }
        
        protected override void OnPedestrianExit(GameObject pedestrian)
        {
            _crossingDirections.Remove(pedestrian.GetInstanceID());
        }
        
        protected override void OnVehicleEnter(GameObject vehicle)
        {
            // Vehicle entering crossing - this shouldn't happen if system works correctly
            // But we track it for edge cases
            if (_showDebug && _isPedestrianCrossing)
            {
                SimCoreLogger.LogWarning($"[CrossingZone:{_zoneId}] Vehicle entered while pedestrians crossing!");
            }
        }
        
        /// <summary>
        /// Called by pedestrian AI when approaching the crossing
        /// </summary>
        public bool CanCross(GameObject pedestrian, MovementIntent intent)
        {
            // Fleeing or following pedestrians don't wait
            if (intent == MovementIntent.Fleeing || intent == MovementIntent.Following)
            {
                return true;
            }
            
            // If no vehicles, can cross
            if (!_isVehiclePassing && _vehiclesInZone.Count == 0)
            {
                return true;
            }
            
            // Drunk pedestrians might just go
            if (intent == MovementIntent.Drunk && Random.value < 0.3f)
            {
                return true;
            }
            
            // Start waiting
            int id = pedestrian.GetInstanceID();
            if (!_waitingPedestrians.ContainsKey(id))
            {
                _waitingPedestrians[id] = 0f;
            }
            
            _waitingPedestrians[id] += Time.deltaTime;
            
            // If waited too long, cross anyway
            if (_waitingPedestrians[id] >= _maxWaitTime)
            {
                _waitingPedestrians.Remove(id);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Called by pedestrian AI when they give up waiting
        /// </summary>
        public void StopWaiting(GameObject pedestrian)
        {
            _waitingPedestrians.Remove(pedestrian.GetInstanceID());
        }
        
        /// <summary>
        /// Get the recommended speed for a vehicle approaching this crossing
        /// </summary>
        public float GetVehicleSpeedMultiplier(float distanceToCrossing)
        {
            if (!_isPedestrianCrossing && _waitingPedestrians.Count == 0)
            {
                return 1f; // No pedestrians, full speed
            }
            
            if (distanceToCrossing <= _vehicleStopDistance)
            {
                return 0f; // Must stop
            }
            
            if (distanceToCrossing <= _vehicleSlowdownDistance)
            {
                // Gradually slow down
                float t = (distanceToCrossing - _vehicleStopDistance) / (_vehicleSlowdownDistance - _vehicleStopDistance);
                return Mathf.Lerp(0f, 0.5f, t);
            }
            
            return 1f; // Full speed
        }
        
        /// <summary>
        /// Should a vehicle stop for this crossing?
        /// </summary>
        public bool ShouldVehicleStop(float distanceToCrossing)
        {
            // Stop if pedestrians are crossing or waiting
            if (_isPedestrianCrossing || _waitingPedestrians.Count > 0)
            {
                return distanceToCrossing <= _vehicleStopDistance;
            }
            return false;
        }
        
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            
            if (!_showDebug) return;
            
            // Draw slowdown radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _vehicleSlowdownDistance);
            
            // Draw stop radius
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _vehicleStopDistance);
            
            // Draw crossing state
            Gizmos.color = _isPedestrianCrossing ? Color.green : (_isVehiclePassing ? Color.red : Color.white);
            Gizmos.DrawSphere(transform.position + Vector3.up * 2f, 0.5f);
        }
    }
}

