// SimCore - Vehicle Input Handler
// ═══════════════════════════════════════════════════════════════════════════════
// Bridges IInputService to VehiclePhysics.
// Supports multiple input modes (joystick, tilt, buttons).
// Mobile-optimized with configurable control schemes.
// ═══════════════════════════════════════════════════════════════════════════════

using UnityEngine;
using SimCore.Input;
using SimCore.Services;

namespace SimCore.Modules.Vehicle
{
    /// <summary>
    /// Handles input for player-controlled vehicles.
    /// Reads from IInputService and outputs to VehiclePhysics.
    /// </summary>
    public class VehicleInputHandler : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// How steering input is read.
        /// </summary>
        public enum SteeringMode
        {
            Joystick,       // Use move joystick horizontal axis
            Tilt,           // Use device accelerometer
            Buttons,        // Discrete left/right buttons
            TouchWheel      // Virtual steering wheel (future)
        }

        /// <summary>
        /// How throttle/brake input is read.
        /// </summary>
        public enum ThrottleMode
        {
            JoystickVertical,   // Joystick Y axis
            Pedals,             // Separate gas/brake buttons
            AutoAccelerate      // Always accelerating, brake only
        }

        #endregion

        #region Serialized Fields

        [Header("═══ CONTROL MODES ═══")]
        [Tooltip("How steering is controlled")]
        [SerializeField] private SteeringMode _steeringMode = SteeringMode.Joystick;

        [Tooltip("How throttle/brake is controlled")]
        [SerializeField] private ThrottleMode _throttleMode = ThrottleMode.JoystickVertical;

        [Header("═══ JOYSTICK SETTINGS ═══")]
        [Tooltip("Joystick dead zone")]
        [SerializeField] [Range(0f, 0.3f)] private float _joystickDeadzone = 0.1f;

        [Tooltip("Steering sensitivity multiplier")]
        [SerializeField] [Range(0.5f, 2f)] private float _steeringSensitivity = 1f;

        [Header("═══ TILT SETTINGS ═══")]
        [Tooltip("Tilt steering sensitivity")]
        [SerializeField] [Range(0.5f, 4f)] private float _tiltSensitivity = 2f;

        [Tooltip("Tilt dead zone")]
        [SerializeField] [Range(0f, 0.3f)] private float _tiltDeadzone = 0.1f;

        [Tooltip("Invert tilt direction")]
        [SerializeField] private bool _invertTilt = false;

        [Tooltip("Tilt calibration offset (set during calibration)")]
        [SerializeField] private float _tiltCalibration = 0f;

        [Header("═══ SMOOTHING ═══")]
        [Tooltip("Steering input smoothing time")]
        [SerializeField] [Range(0f, 0.3f)] private float _steerSmoothTime = 0.1f;

        [Tooltip("Throttle input smoothing time")]
        [SerializeField] [Range(0f, 0.2f)] private float _throttleSmoothTime = 0.05f;

        [Header("═══ BUTTON ACTIONS ═══")]
        [Tooltip("GameAction for handbrake")]
        [SerializeField] private GameAction _handbrakeAction = GameAction.Sprint;

        [Tooltip("GameAction for brake (if using pedals)")]
        [SerializeField] private GameAction _brakeAction = GameAction.Custom1;

        [Tooltip("GameAction for horn")]
        [SerializeField] private GameAction _hornAction = GameAction.Custom2;

        [Header("═══ REFERENCES ═══")]
        [Tooltip("VehiclePhysics to control (auto-found if null)")]
        [SerializeField] private VehiclePhysics _vehiclePhysics;

        #endregion

        #region Private Fields

        private IInputService _inputService;

        // Raw input values
        private float _rawSteering;
        private float _rawThrottle;
        private float _rawBrake;

        // Smoothed output values
        private float _smoothedSteering;
        private float _smoothedThrottle;
        private float _smoothedBrake;
        private float _steerVelocity;
        private float _throttleVelocity;
        private float _brakeVelocity;

        // State
        private bool _isEnabled = true;
        private bool _handbrakeHeld;
        private bool _hornPressed;

        #endregion

        #region Properties

        /// <summary>Final steering output (-1 to 1)</summary>
        public float Steering => _smoothedSteering;

        /// <summary>Final throttle output (0 to 1 for forward, -1 to 0 for reverse)</summary>
        public float Throttle => _smoothedThrottle;

        /// <summary>Final brake output (0 to 1)</summary>
        public float Brake => _smoothedBrake;

        /// <summary>Is handbrake being held</summary>
        public bool Handbrake => _handbrakeHeld;

        /// <summary>Was horn pressed this frame</summary>
        public bool HornPressed => _hornPressed;

        /// <summary>Current steering mode</summary>
        public SteeringMode CurrentSteeringMode
        {
            get => _steeringMode;
            set => _steeringMode = value;
        }

        /// <summary>Current throttle mode</summary>
        public ThrottleMode CurrentThrottleMode
        {
            get => _throttleMode;
            set => _throttleMode = value;
        }

        /// <summary>Is handler enabled</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_vehiclePhysics == null)
            {
                _vehiclePhysics = GetComponent<VehiclePhysics>();
            }
        }

        private void Start()
        {
            _inputService = ServiceLocator.TryGet<IInputService>(out var service) ? service : null;
        }

        private void Update()
        {
            if (!_isEnabled)
            {
                ClearInputs();
                return;
            }

            ReadRawInputs();
            SmoothInputs();
            ApplyToPhysics();
        }

        #endregion

        #region Input Reading

        private void ReadRawInputs()
        {
            // Reset per-frame inputs
            _hornPressed = false;

            // Read steering
            _rawSteering = ReadSteering();

            // Read throttle/brake
            ReadThrottle(out _rawThrottle, out _rawBrake);

            // Read action buttons
            ReadActionButtons();
        }

        private float ReadSteering()
        {
            switch (_steeringMode)
            {
                case SteeringMode.Joystick:
                    return ReadJoystickSteering();

                case SteeringMode.Tilt:
                    return ReadTiltSteering();

                case SteeringMode.Buttons:
                    return ReadButtonSteering();

                case SteeringMode.TouchWheel:
                    // Future implementation
                    return ReadJoystickSteering();

                default:
                    return 0f;
            }
        }

        private float ReadJoystickSteering()
        {
            if (_inputService == null) return ReadFallbackKeyboardSteering();

            float horizontal = _inputService.MoveInput.x;

            // Apply deadzone
            if (Mathf.Abs(horizontal) < _joystickDeadzone)
                return 0f;

            // Normalize after deadzone
            float sign = Mathf.Sign(horizontal);
            float magnitude = (Mathf.Abs(horizontal) - _joystickDeadzone) / (1f - _joystickDeadzone);

            return sign * magnitude * _steeringSensitivity;
        }

        private float ReadTiltSteering()
        {
            // Read accelerometer
            float tilt = UnityEngine.Input.acceleration.x;

            // Apply calibration offset
            tilt -= _tiltCalibration;

            // Apply deadzone
            if (Mathf.Abs(tilt) < _tiltDeadzone)
                return 0f;

            // Normalize and apply sensitivity
            float sign = Mathf.Sign(tilt);
            float magnitude = Mathf.Min(1f, (Mathf.Abs(tilt) - _tiltDeadzone) * _tiltSensitivity);

            float result = sign * magnitude;
            return _invertTilt ? -result : result;
        }

        private float ReadButtonSteering()
        {
            // Use arrow keys or custom buttons
            float steering = 0f;

            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow) || UnityEngine.Input.GetKey(KeyCode.A))
                steering -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow) || UnityEngine.Input.GetKey(KeyCode.D))
                steering += 1f;

            return steering * _steeringSensitivity;
        }

        private float ReadFallbackKeyboardSteering()
        {
            float steering = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
                steering -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
                steering += 1f;
            return steering * _steeringSensitivity;
        }

        private void ReadThrottle(out float throttle, out float brake)
        {
            throttle = 0f;
            brake = 0f;

            switch (_throttleMode)
            {
                case ThrottleMode.JoystickVertical:
                    if (_inputService != null)
                    {
                        float vertical = _inputService.MoveInput.y;
                        if (Mathf.Abs(vertical) > _joystickDeadzone)
                        {
                            if (vertical > 0)
                                throttle = (vertical - _joystickDeadzone) / (1f - _joystickDeadzone);
                            else
                                throttle = (vertical + _joystickDeadzone) / (1f - _joystickDeadzone);
                        }
                    }
                    else
                    {
                        // Fallback to keyboard
                        if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
                            throttle = 1f;
                        if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
                            throttle = -1f;
                    }
                    break;

                case ThrottleMode.Pedals:
                    // Gas from joystick Y (positive only) or custom button
                    if (_inputService != null)
                    {
                        throttle = Mathf.Max(0f, _inputService.MoveInput.y);
                        brake = _inputService.IsHeld(_brakeAction) ? 1f : 0f;
                    }
                    break;

                case ThrottleMode.AutoAccelerate:
                    throttle = 1f;
                    if (_inputService != null)
                        brake = _inputService.IsHeld(_brakeAction) ? 1f : 0f;
                    break;
            }
        }

        private void ReadActionButtons()
        {
            if (_inputService != null)
            {
                _handbrakeHeld = _inputService.IsHeld(_handbrakeAction);
                _hornPressed = _inputService.WasPressed(_hornAction);
            }
            else
            {
                // Fallback to keyboard
                _handbrakeHeld = UnityEngine.Input.GetKey(KeyCode.Space);
                _hornPressed = UnityEngine.Input.GetKeyDown(KeyCode.H);
            }
        }

        #endregion

        #region Smoothing

        private void SmoothInputs()
        {
            float dt = Time.deltaTime;

            // Smooth steering
            _smoothedSteering = Mathf.SmoothDamp(_smoothedSteering, _rawSteering,
                ref _steerVelocity, _steerSmoothTime);

            // Smooth throttle (faster response)
            _smoothedThrottle = Mathf.SmoothDamp(_smoothedThrottle, _rawThrottle,
                ref _throttleVelocity, _throttleSmoothTime);

            // Smooth brake (fast response)
            _smoothedBrake = Mathf.SmoothDamp(_smoothedBrake, _rawBrake,
                ref _brakeVelocity, _throttleSmoothTime * 0.5f);
        }

        private void ClearInputs()
        {
            _rawSteering = 0f;
            _rawThrottle = 0f;
            _rawBrake = 0f;
            _handbrakeHeld = false;
            _hornPressed = false;

            // Quickly decay smoothed values
            _smoothedSteering = Mathf.MoveTowards(_smoothedSteering, 0f, Time.deltaTime * 5f);
            _smoothedThrottle = Mathf.MoveTowards(_smoothedThrottle, 0f, Time.deltaTime * 5f);
            _smoothedBrake = 0f;

            ApplyToPhysics();
        }

        #endregion

        #region Physics Application

        private void ApplyToPhysics()
        {
            if (_vehiclePhysics == null) return;

            _vehiclePhysics.SetInput(_smoothedThrottle, _smoothedSteering, _smoothedBrake);
            _vehiclePhysics.SetHandbrake(_handbrakeHeld);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calibrate tilt steering (call when device is in neutral position).
        /// </summary>
        public void CalibrateTilt()
        {
            _tiltCalibration = UnityEngine.Input.acceleration.x;
        }

        /// <summary>
        /// Reset tilt calibration.
        /// </summary>
        public void ResetTiltCalibration()
        {
            _tiltCalibration = 0f;
        }

        /// <summary>
        /// Set steering mode at runtime.
        /// </summary>
        public void SetSteeringMode(SteeringMode mode)
        {
            _steeringMode = mode;
        }

        /// <summary>
        /// Set throttle mode at runtime.
        /// </summary>
        public void SetThrottleMode(ThrottleMode mode)
        {
            _throttleMode = mode;
        }

        /// <summary>
        /// Get current control configuration for UI.
        /// </summary>
        public (SteeringMode steering, ThrottleMode throttle) GetCurrentConfig()
        {
            return (_steeringMode, _throttleMode);
        }

        #endregion
    }
}
