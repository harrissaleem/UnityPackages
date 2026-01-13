// SimCore - Vehicle Physics Component
// ═══════════════════════════════════════════════════════════════════════════════
// Handles vehicle movement physics using Rigidbody.
// Supports both WheelCollider-based (realistic) and arcade physics modes.
// Mobile-optimized with smooth input handling.
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using SimCore;
using SimCore.Data;

namespace SimCore.Modules.Vehicle
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehiclePhysics : MonoBehaviour
    {
        #region Serialized Fields

        [Header("═══ PHYSICS MODE ═══")]
        [Tooltip("Use WheelColliders for realistic physics, or arcade for simpler handling")]
        [SerializeField] private PhysicsMode _physicsMode = PhysicsMode.Arcade;

        [Header("═══ ARCADE PHYSICS ═══")]
        [Tooltip("Motor force for arcade mode")]
        [SerializeField] private float _arcadeMotorForce = 2000f;

        [Tooltip("Steering speed for arcade mode")]
        [SerializeField] private float _arcadeSteerSpeed = 100f;

        [Tooltip("Drag when not accelerating")]
        [SerializeField] private float _arcadeDrag = 2f;

        [Tooltip("Angular drag for stability")]
        [SerializeField] private float _arcadeAngularDrag = 5f;

        [Header("═══ WHEEL PHYSICS ═══")]
        [Tooltip("Wheel colliders (if using realistic mode)")]
        [SerializeField] private WheelCollider[] _wheelColliders;

        [Tooltip("Wheel mesh transforms (for visual rotation)")]
        [SerializeField] private Transform[] _wheelMeshes;

        [Tooltip("Which wheels are drive wheels")]
        [SerializeField] private bool[] _isDriveWheel;

        [Tooltip("Which wheels are steer wheels")]
        [SerializeField] private bool[] _isSteerWheel;

        [Tooltip("Max motor torque per wheel")]
        [SerializeField] private float _maxMotorTorque = 1500f;

        [Tooltip("Max brake torque")]
        [SerializeField] private float _maxBrakeTorque = 3000f;

        [Tooltip("Max steer angle")]
        [SerializeField] private float _maxSteerAngle = 35f;

        [Header("═══ HANDLING ═══")]
        [Tooltip("Steering smoothing (higher = smoother)")]
        [SerializeField] private float _steerSmoothing = 8f;

        [Tooltip("Throttle smoothing")]
        [SerializeField] private float _throttleSmoothing = 6f;

        [Tooltip("Counter-steer assist (helps prevent spinning)")]
        [SerializeField] [Range(0f, 1f)] private float _counterSteerAssist = 0.3f;

        [Tooltip("Downforce at speed")]
        [SerializeField] private float _downforce = 100f;

        [Header("═══ GROUND DETECTION ═══")]
        [Tooltip("Ground check raycast distance")]
        [SerializeField] private float _groundCheckDistance = 1f;

        [Tooltip("Ground layer mask")]
        [SerializeField] private LayerMask _groundLayer = ~0;

        #endregion

        #region Private Fields

        private Rigidbody _rb;
        private VehicleDefSO _definition;
        private SimId _entityId;

        // Input state (set externally)
        private float _inputThrottle;
        private float _inputSteer;
        private float _inputBrake;
        private bool _inputHandbrake;

        // Smoothed values
        private float _currentThrottle;
        private float _currentSteer;
        private float _currentBrake;

        // State
        private bool _isGrounded;
        private bool _isFlipped;
        private float _currentSpeedKmh;
        private Vector3 _groundNormal;

        #endregion

        #region Properties

        /// <summary>Current speed in km/h</summary>
        public float CurrentSpeedKmh => _currentSpeedKmh;

        /// <summary>Normalized speed (0-1 relative to max)</summary>
        public float NormalizedSpeed => _definition != null ? Mathf.Clamp01(_currentSpeedKmh / _definition.MaxSpeed) : 0f;

        /// <summary>Is vehicle on the ground</summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>Is vehicle flipped over</summary>
        public bool IsFlipped => _isFlipped;

        /// <summary>Current velocity magnitude in m/s</summary>
        public float VelocityMagnitude => _rb != null ? _rb.linearVelocity.magnitude : 0f;

        /// <summary>Forward velocity (positive = forward, negative = reverse)</summary>
        public float ForwardVelocity => _rb != null ? Vector3.Dot(_rb.linearVelocity, transform.forward) : 0f;

        /// <summary>Entity ID this vehicle belongs to</summary>
        public SimId EntityId => _entityId;

        /// <summary>Definition this vehicle uses</summary>
        public VehicleDefSO Definition => _definition;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            SetupRigidbody();
        }

        private void FixedUpdate()
        {
            if (_definition == null) return;

            UpdateGroundCheck();
            SmoothInputs();

            if (_physicsMode == PhysicsMode.Arcade)
            {
                ApplyArcadePhysics();
            }
            else
            {
                ApplyWheelPhysics();
            }

            ApplyDownforce();
            UpdateSpeed();
            UpdateFlipCheck();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the vehicle physics with a definition.
        /// </summary>
        public void Initialize(SimId entityId, VehicleDefSO definition)
        {
            _entityId = entityId;
            _definition = definition;

            // Apply definition settings
            if (_definition != null)
            {
                _maxMotorTorque = _definition.Acceleration * 200f;
                _maxSteerAngle = Mathf.Lerp(25f, 45f, _definition.HandlingRating);
            }
        }

        private void SetupRigidbody()
        {
            if (_rb == null) return;

            _rb.mass = 1500f;
            _rb.linearDamping = 0.05f;
            _rb.angularDamping = 0.5f;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            // Lower center of mass for stability
            _rb.centerOfMass = new Vector3(0f, -0.5f, 0f);
        }

        #endregion

        #region Input

        /// <summary>
        /// Set input values. Call each frame from input handler.
        /// </summary>
        public void SetInput(float throttle, float steer, float brake)
        {
            _inputThrottle = Mathf.Clamp(throttle, -1f, 1f);
            _inputSteer = Mathf.Clamp(steer, -1f, 1f);
            _inputBrake = Mathf.Clamp01(brake);
        }

        /// <summary>
        /// Set handbrake state.
        /// </summary>
        public void SetHandbrake(bool active)
        {
            _inputHandbrake = active;
        }

        private void SmoothInputs()
        {
            float dt = Time.fixedDeltaTime;

            _currentThrottle = Mathf.MoveTowards(_currentThrottle, _inputThrottle, _throttleSmoothing * dt * 2f);
            _currentBrake = Mathf.MoveTowards(_currentBrake, _inputBrake, _throttleSmoothing * dt * 3f);

            // Speed-sensitive steering
            float steerMultiplier = _definition != null ? _definition.EvaluateSteering(NormalizedSpeed) : 1f;
            float targetSteer = _inputSteer * steerMultiplier;
            _currentSteer = Mathf.MoveTowards(_currentSteer, targetSteer, _steerSmoothing * dt);
        }

        #endregion

        #region Arcade Physics

        private void ApplyArcadePhysics()
        {
            if (!_isGrounded) return;

            // Motor force
            float motorForce = _currentThrottle * _arcadeMotorForce;
            if (_definition != null)
            {
                motorForce *= _definition.EvaluateAcceleration(NormalizedSpeed);
            }

            // Apply forward force
            Vector3 forwardForce = transform.forward * motorForce;
            _rb.AddForce(forwardForce, ForceMode.Force);

            // Steering (only when moving)
            if (Mathf.Abs(ForwardVelocity) > 0.5f)
            {
                float turnSign = Mathf.Sign(ForwardVelocity);
                float steerTorque = _currentSteer * _arcadeSteerSpeed * turnSign;

                // Counter-steer assist
                float sidewaysVelocity = Vector3.Dot(_rb.linearVelocity, transform.right);
                steerTorque -= sidewaysVelocity * _counterSteerAssist * 10f;

                _rb.AddTorque(Vector3.up * steerTorque, ForceMode.Force);
            }

            // Braking
            if (_currentBrake > 0.1f || _inputHandbrake)
            {
                float brakeMultiplier = _inputHandbrake ? 1.5f : _currentBrake;
                _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, Vector3.zero,
                    brakeMultiplier * 10f * Time.fixedDeltaTime);
            }

            // Drag when not accelerating
            if (Mathf.Abs(_currentThrottle) < 0.1f)
            {
                _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, Vector3.zero,
                    _arcadeDrag * Time.fixedDeltaTime);
            }

            // Speed cap
            if (_definition != null && _currentSpeedKmh > _definition.MaxSpeed)
            {
                float targetSpeedMs = _definition.MaxSpeed / 3.6f;
                _rb.linearVelocity = _rb.linearVelocity.normalized * targetSpeedMs;
            }
        }

        #endregion

        #region Wheel Physics

        private void ApplyWheelPhysics()
        {
            if (_wheelColliders == null || _wheelColliders.Length == 0) return;

            for (int i = 0; i < _wheelColliders.Length; i++)
            {
                var wheel = _wheelColliders[i];
                if (wheel == null) continue;

                // Motor torque (drive wheels only)
                if (_isDriveWheel != null && i < _isDriveWheel.Length && _isDriveWheel[i])
                {
                    float torque = _currentThrottle * _maxMotorTorque;
                    if (_definition != null)
                    {
                        torque *= _definition.EvaluateAcceleration(NormalizedSpeed);
                    }
                    wheel.motorTorque = torque;
                }

                // Steering (steer wheels only)
                if (_isSteerWheel != null && i < _isSteerWheel.Length && _isSteerWheel[i])
                {
                    float steerAngle = _currentSteer * _maxSteerAngle;
                    wheel.steerAngle = steerAngle;
                }

                // Brakes
                float brakeTorque = (_currentBrake + (_inputHandbrake ? 1f : 0f)) * _maxBrakeTorque;
                wheel.brakeTorque = brakeTorque;

                // Update visual wheel
                UpdateWheelMesh(i);
            }
        }

        private void UpdateWheelMesh(int wheelIndex)
        {
            if (_wheelMeshes == null || wheelIndex >= _wheelMeshes.Length) return;
            if (_wheelColliders == null || wheelIndex >= _wheelColliders.Length) return;

            var wheel = _wheelColliders[wheelIndex];
            var mesh = _wheelMeshes[wheelIndex];

            if (wheel == null || mesh == null) return;

            wheel.GetWorldPose(out Vector3 pos, out Quaternion rot);
            mesh.position = pos;
            mesh.rotation = rot;
        }

        #endregion

        #region Physics Helpers

        private void ApplyDownforce()
        {
            if (!_isGrounded || _downforce <= 0f) return;

            float speedFactor = NormalizedSpeed;
            Vector3 downForce = -_groundNormal * _downforce * speedFactor * speedFactor;
            _rb.AddForce(downForce, ForceMode.Force);
        }

        private void UpdateGroundCheck()
        {
            // Raycast down from center
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down,
                out RaycastHit hit, _groundCheckDistance + 0.5f, _groundLayer))
            {
                _isGrounded = true;
                _groundNormal = hit.normal;
            }
            else
            {
                _isGrounded = false;
                _groundNormal = Vector3.up;
            }
        }

        private void UpdateSpeed()
        {
            // Convert m/s to km/h
            _currentSpeedKmh = _rb.linearVelocity.magnitude * 3.6f;
        }

        private void UpdateFlipCheck()
        {
            // Check if vehicle is upside down
            float upDot = Vector3.Dot(transform.up, Vector3.up);
            _isFlipped = upDot < 0.1f;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reset vehicle to upright position.
        /// </summary>
        public void ResetToUpright()
        {
            Vector3 pos = transform.position;
            pos.y += 1f;
            transform.position = pos;

            Quaternion targetRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            transform.rotation = targetRot;

            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Apply impulse force (collisions, explosions).
        /// </summary>
        public void ApplyImpulse(Vector3 force, Vector3 point)
        {
            _rb.AddForceAtPosition(force, point, ForceMode.Impulse);
        }

        /// <summary>
        /// Freeze/unfreeze physics.
        /// </summary>
        public void SetFrozen(bool frozen)
        {
            _rb.isKinematic = frozen;
        }

        /// <summary>
        /// Get forward direction relative to ground.
        /// </summary>
        public Vector3 GetGroundForward()
        {
            return Vector3.ProjectOnPlane(transform.forward, _groundNormal).normalized;
        }

        #endregion

        #region Editor Helpers

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Ground check ray
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f,
                transform.position + Vector3.up * 0.5f + Vector3.down * (_groundCheckDistance + 0.5f));

            // Center of mass
            if (_rb != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.TransformPoint(_rb.centerOfMass), 0.2f);
            }

            // Velocity
            Gizmos.color = Color.blue;
            if (_rb != null)
            {
                Gizmos.DrawLine(transform.position, transform.position + _rb.linearVelocity);
            }
        }
#endif

        #endregion
    }

    public enum PhysicsMode
    {
        Arcade,     // Simple, forgiving physics
        Realistic   // WheelCollider-based physics
    }
}
