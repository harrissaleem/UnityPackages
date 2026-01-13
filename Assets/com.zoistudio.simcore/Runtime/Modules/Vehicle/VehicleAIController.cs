// SimCore - Vehicle AI Controller
// ═══════════════════════════════════════════════════════════════════════════════
// AI control for NPC-driven vehicles.
// Handles waypoint following, traffic behavior, and obstacle avoidance.
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using SimCore;
using SimCore.Signals;

namespace SimCore.Modules.Vehicle
{
    /// <summary>
    /// Interface for traffic light components.
    /// Implement this on your traffic light to enable AI vehicle detection.
    /// </summary>
    public interface ITrafficLight
    {
        /// <summary>True if the light is red (vehicles should stop)</summary>
        bool IsRed { get; }

        /// <summary>True if the light is yellow (vehicles should prepare to stop)</summary>
        bool IsYellow { get; }

        /// <summary>True if the light is green (vehicles can go)</summary>
        bool IsGreen { get; }
    }

    /// <summary>
    /// AI controller for NPC vehicles.
    /// Provides waypoint following and simple traffic behavior.
    /// </summary>
    public class VehicleAIController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("═══ NAVIGATION ═══")]
        [Tooltip("Distance to consider waypoint reached")]
        [SerializeField] private float _waypointReachDistance = 5f;

        [Tooltip("Look ahead distance for path smoothing")]
        [SerializeField] private float _lookAheadDistance = 10f;

        [Header("═══ SPEED CONTROL ═══")]
        [Tooltip("Target speed in km/h")]
        [SerializeField] private float _targetSpeed = 50f;

        [Tooltip("Minimum follow distance to vehicle ahead")]
        [SerializeField] private float _followDistance = 10f;

        [Tooltip("Slow down distance when approaching stop")]
        [SerializeField] private float _slowDownDistance = 20f;

        [Header("═══ TRAFFIC BEHAVIOR ═══")]
        [Tooltip("Obey traffic lights")]
        [SerializeField] private bool _obeyTrafficLights = true;

        [Tooltip("Obey speed limits")]
        [SerializeField] private bool _obeySpeedLimits = true;

        [Tooltip("Stop for pedestrians")]
        [SerializeField] private bool _stopForPedestrians = true;

        [Header("═══ DETECTION ═══")]
        [Tooltip("Forward detection range")]
        [SerializeField] private float _detectionRange = 30f;

        [Tooltip("Detection cone angle")]
        [SerializeField] private float _detectionAngle = 45f;

        [Tooltip("Obstacle layer mask")]
        [SerializeField] private LayerMask _obstacleLayer = ~0;

        [Tooltip("Pedestrian layer mask (for pedestrian detection)")]
        [SerializeField] private LayerMask _pedestrianLayer;

        [Tooltip("Tag for traffic light objects")]
        [SerializeField] private string _trafficLightTag = "TrafficLight";

        [Header("═══ STEERING ═══")]
        [Tooltip("Steering responsiveness")]
        [SerializeField] private float _steeringSensitivity = 1f;

        [Tooltip("Maximum steering angle")]
        [SerializeField] private float _maxSteerAngle = 0.8f;

        #endregion

        #region Private Fields

        private VehiclePhysics _physics;
        private SimId _entityId;
        private SignalBus _signalBus;

        // Path following
        private List<Vector3> _waypoints = new List<Vector3>();
        private int _currentWaypointIndex;
        private bool _loopPath;
        private Vector3 _currentDestination;
        private bool _hasDestination;

        // State
        private bool _isActive = true;
        private bool _isStopped;
        private float _currentSpeedLimit = 50f;

        // Obstacle detection
        private bool _obstacleAhead;
        private float _obstacleDistance;
        private Vector3 _obstaclePosition;

        // Traffic detection
        private bool _redLightAhead;
        private float _redLightDistance;
        private bool _pedestrianAhead;
        private float _pedestrianDistance;

        // Output
        private float _throttle;
        private float _steering;
        private float _brake;

        #endregion

        #region Properties

        /// <summary>Is AI active</summary>
        public bool IsActive => _isActive;

        /// <summary>Is AI stopped</summary>
        public bool IsStopped => _isStopped;

        /// <summary>Current target speed</summary>
        public float TargetSpeed => _targetSpeed;

        /// <summary>Current waypoint index</summary>
        public int CurrentWaypointIndex => _currentWaypointIndex;

        /// <summary>Has reached final destination</summary>
        public bool HasReachedDestination => _hasDestination &&
            _currentWaypointIndex >= _waypoints.Count &&
            (_waypoints.Count == 0 ||
             Vector3.Distance(transform.position, _currentDestination) < _waypointReachDistance);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _physics = GetComponent<VehiclePhysics>();
        }

        private void Update()
        {
            if (!_isActive || _physics == null) return;

            DetectObstacles();
            UpdateNavigation();
            CalculateSteering();
            CalculateThrottle();
            ApplyInputs();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize with entity reference.
        /// </summary>
        public void Initialize(SimId entityId, SignalBus signalBus)
        {
            _entityId = entityId;
            _signalBus = signalBus;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set single destination.
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            _waypoints.Clear();
            _waypoints.Add(destination);
            _currentWaypointIndex = 0;
            _currentDestination = destination;
            _hasDestination = true;
            _isStopped = false;
        }

        /// <summary>
        /// Set waypoint path.
        /// </summary>
        public void SetWaypoints(List<Vector3> waypoints, bool loop = false)
        {
            _waypoints.Clear();
            _waypoints.AddRange(waypoints);
            _currentWaypointIndex = 0;
            _loopPath = loop;
            _hasDestination = waypoints.Count > 0;

            if (_hasDestination)
            {
                _currentDestination = waypoints[waypoints.Count - 1];
            }

            _isStopped = false;
        }

        /// <summary>
        /// Set target speed.
        /// </summary>
        public void SetTargetSpeed(float speedKmh)
        {
            _targetSpeed = speedKmh;
        }

        /// <summary>
        /// Stop the vehicle.
        /// </summary>
        public void Stop()
        {
            _isStopped = true;
        }

        /// <summary>
        /// Resume movement.
        /// </summary>
        public void Resume()
        {
            _isStopped = false;
        }

        /// <summary>
        /// Clear current path.
        /// </summary>
        public void ClearPath()
        {
            _waypoints.Clear();
            _currentWaypointIndex = 0;
            _hasDestination = false;
        }

        /// <summary>
        /// Set speed limit (from traffic zone).
        /// </summary>
        public void SetSpeedLimit(float limit)
        {
            _currentSpeedLimit = limit;
        }

        #endregion

        #region Navigation

        private void UpdateNavigation()
        {
            if (_waypoints.Count == 0) return;

            // Check if reached current waypoint
            Vector3 currentWaypoint = _waypoints[_currentWaypointIndex];
            float distToWaypoint = Vector3.Distance(transform.position, currentWaypoint);

            if (distToWaypoint < _waypointReachDistance)
            {
                _currentWaypointIndex++;

                // Check if path complete
                if (_currentWaypointIndex >= _waypoints.Count)
                {
                    if (_loopPath)
                    {
                        _currentWaypointIndex = 0;
                    }
                    else
                    {
                        // Destination reached
                        _signalBus?.Publish(new VehicleAIDestinationReachedSignal
                        {
                            VehicleId = _entityId,
                            Destination = currentWaypoint,
                            WaypointIndex = _currentWaypointIndex - 1,
                            IsFinalDestination = true
                        });
                    }
                }
                else
                {
                    // Intermediate waypoint reached
                    _signalBus?.Publish(new VehicleAIDestinationReachedSignal
                    {
                        VehicleId = _entityId,
                        Destination = currentWaypoint,
                        WaypointIndex = _currentWaypointIndex - 1,
                        IsFinalDestination = false
                    });
                }
            }
        }

        #endregion

        #region Obstacle Detection

        private void DetectObstacles()
        {
            _obstacleAhead = false;
            _obstacleDistance = _detectionRange;
            _pedestrianAhead = false;
            _pedestrianDistance = _detectionRange;
            _redLightAhead = false;
            _redLightDistance = _detectionRange;

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 forward = transform.forward;

            // Center ray for obstacles
            if (Physics.Raycast(origin, forward, out RaycastHit hit, _detectionRange, _obstacleLayer))
            {
                _obstacleAhead = true;
                _obstacleDistance = hit.distance;
                _obstaclePosition = hit.point;
            }

            // Side rays for wider detection
            float angleStep = _detectionAngle / 2f;
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 dir = Quaternion.Euler(0, angleStep * i, 0) * forward;
                if (Physics.Raycast(origin, dir, out hit, _detectionRange * 0.8f, _obstacleLayer))
                {
                    if (hit.distance < _obstacleDistance)
                    {
                        _obstacleAhead = true;
                        _obstacleDistance = hit.distance;
                        _obstaclePosition = hit.point;
                    }
                }
            }

            // Detect pedestrians if configured
            if (_stopForPedestrians && _pedestrianLayer != 0)
            {
                if (Physics.Raycast(origin, forward, out hit, _detectionRange, _pedestrianLayer))
                {
                    _pedestrianAhead = true;
                    _pedestrianDistance = hit.distance;
                }
            }

            // Detect traffic lights if configured
            if (_obeyTrafficLights && !string.IsNullOrEmpty(_trafficLightTag))
            {
                DetectTrafficLights(origin, forward);
            }
        }

        // Reusable array to avoid allocations
        private static readonly RaycastHit[] _trafficLightHits = new RaycastHit[8];

        private void DetectTrafficLights(Vector3 origin, Vector3 forward)
        {
            // Non-alloc spherecast for performance
            int hitCount = Physics.SphereCastNonAlloc(origin, 2f, forward, _trafficLightHits, _detectionRange);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _trafficLightHits[i];
                if (hit.collider != null && hit.collider.CompareTag(_trafficLightTag))
                {
                    // Check if traffic light is red
                    var trafficLight = hit.collider.GetComponent<ITrafficLight>();
                    if (trafficLight != null && trafficLight.IsRed)
                    {
                        _redLightAhead = true;
                        _redLightDistance = hit.distance;
                        return;
                    }
                }
            }
        }

        #endregion

        #region Steering Calculation

        private void CalculateSteering()
        {
            if (_waypoints.Count == 0 || _currentWaypointIndex >= _waypoints.Count)
            {
                _steering = 0f;
                return;
            }

            // Get target point (with look-ahead)
            Vector3 targetPoint = GetLookAheadPoint();

            // Calculate angle to target
            Vector3 toTarget = targetPoint - transform.position;
            toTarget.y = 0;

            if (toTarget.sqrMagnitude < 0.1f)
            {
                _steering = 0f;
                return;
            }

            Vector3 forward = transform.forward;
            forward.y = 0;

            float angle = Vector3.SignedAngle(forward, toTarget.normalized, Vector3.up);

            // Convert to steering input (-1 to 1)
            _steering = Mathf.Clamp(angle / 45f * _steeringSensitivity, -_maxSteerAngle, _maxSteerAngle);
        }

        private Vector3 GetLookAheadPoint()
        {
            if (_waypoints.Count == 0) return transform.position;

            Vector3 currentWaypoint = _waypoints[_currentWaypointIndex];

            // Simple look-ahead: project ahead on path
            if (_currentWaypointIndex < _waypoints.Count - 1)
            {
                Vector3 nextWaypoint = _waypoints[_currentWaypointIndex + 1];
                Vector3 dir = (nextWaypoint - currentWaypoint).normalized;
                float distToCurrent = Vector3.Distance(transform.position, currentWaypoint);

                if (distToCurrent < _lookAheadDistance)
                {
                    float lookAheadRemaining = _lookAheadDistance - distToCurrent;
                    return currentWaypoint + dir * lookAheadRemaining;
                }
            }

            return currentWaypoint;
        }

        #endregion

        #region Throttle Calculation

        private void CalculateThrottle()
        {
            _throttle = 0f;
            _brake = 0f;

            // Stopped
            if (_isStopped || !_hasDestination || HasReachedDestination)
            {
                _brake = 1f;
                return;
            }

            // Calculate desired speed
            float desiredSpeed = _targetSpeed;

            // Apply speed limit if obeying traffic
            if (_obeySpeedLimits)
            {
                desiredSpeed = Mathf.Min(desiredSpeed, _currentSpeedLimit);
            }

            // Stop for red lights
            if (_redLightAhead)
            {
                float stopDistance = _followDistance * 1.5f;
                if (_redLightDistance < stopDistance)
                {
                    _brake = 1f;
                    return;
                }
                float speedFactor = Mathf.Clamp01((_redLightDistance - stopDistance) / _slowDownDistance);
                desiredSpeed *= speedFactor;
            }

            // Stop for pedestrians
            if (_pedestrianAhead)
            {
                float stopDistance = _followDistance;
                if (_pedestrianDistance < stopDistance)
                {
                    _brake = 1f;
                    return;
                }
                float speedFactor = Mathf.Clamp01((_pedestrianDistance - stopDistance) / _slowDownDistance);
                desiredSpeed *= speedFactor;
            }

            // Slow down for obstacles
            if (_obstacleAhead)
            {
                float speedFactor = Mathf.Clamp01((_obstacleDistance - _followDistance) / _slowDownDistance);
                desiredSpeed *= speedFactor;

                if (_obstacleDistance < _followDistance)
                {
                    _brake = 1f;
                    return;
                }
            }

            // Slow down approaching destination
            if (_currentWaypointIndex >= _waypoints.Count - 1 && _waypoints.Count > 0)
            {
                float distToEnd = Vector3.Distance(transform.position, _currentDestination);
                if (distToEnd < _slowDownDistance)
                {
                    float speedFactor = Mathf.Clamp01(distToEnd / _slowDownDistance);
                    desiredSpeed *= speedFactor;
                }
            }

            // Calculate throttle based on speed difference
            float currentSpeed = _physics.CurrentSpeedKmh;
            float speedDiff = desiredSpeed - currentSpeed;

            if (speedDiff > 5f)
            {
                _throttle = Mathf.Clamp01(speedDiff / 20f);
            }
            else if (speedDiff < -5f)
            {
                _brake = Mathf.Clamp01(-speedDiff / 20f);
            }
        }

        #endregion

        #region Apply Inputs

        private void ApplyInputs()
        {
            if (_physics == null) return;
            _physics.SetInput(_throttle, _steering, _brake);
        }

        #endregion

        #region Editor Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Waypoints
            if (_waypoints != null && _waypoints.Count > 0)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < _waypoints.Count; i++)
                {
                    Gizmos.DrawWireSphere(_waypoints[i], 1f);
                    if (i > 0)
                    {
                        Gizmos.DrawLine(_waypoints[i - 1], _waypoints[i]);
                    }
                }

                // Current target
                if (_currentWaypointIndex < _waypoints.Count)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(_waypoints[_currentWaypointIndex], 1.5f);
                    Gizmos.DrawLine(transform.position, _waypoints[_currentWaypointIndex]);
                }
            }

            // Detection cone
            Gizmos.color = _obstacleAhead ? Color.red : Color.green;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Gizmos.DrawRay(origin, transform.forward * _detectionRange);

            Vector3 leftDir = Quaternion.Euler(0, -_detectionAngle / 2f, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, _detectionAngle / 2f, 0) * transform.forward;
            Gizmos.DrawRay(origin, leftDir * _detectionRange * 0.8f);
            Gizmos.DrawRay(origin, rightDir * _detectionRange * 0.8f);

            // Obstacle
            if (_obstacleAhead)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_obstaclePosition, 0.5f);
            }
        }
#endif

        #endregion
    }
}
