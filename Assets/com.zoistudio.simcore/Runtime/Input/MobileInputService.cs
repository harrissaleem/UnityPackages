using System;
using UnityEngine;
using UnityEngine.InputSystem;
using SimCore.Services;

namespace SimCore.Input
{
    /// <summary>
    /// Mobile-first input service supporting touch, keyboard, and gamepad.
    /// Uses Unity's new Input System for cross-platform support.
    /// </summary>
    public class MobileInputService : IInputService, ITickableService
    {
        private InputMode _currentMode = InputMode.Touch;
        private bool _isEnabled = true;

        // Input state
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _sprintHeld;

        // Button states (current frame)
        private bool _interactPressed;
        private bool _attackPressed;
        private bool _cancelPressed;

        // Reference to virtual controls
        private VirtualJoystick _moveJoystick;
        private VirtualJoystick _lookJoystick;
        private GameObject _mobileControlsRoot;

        // Input actions (Unity Input System)
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private InputAction _interactAction;
        private InputAction _attackAction;
        private InputAction _cancelAction;

        public InputMode CurrentMode => _currentMode;
        public Vector2 MoveInput => _isEnabled ? _moveInput : Vector2.zero;
        public Vector2 LookInput => _isEnabled ? _lookInput : Vector2.zero;
        public bool SprintHeld => _isEnabled && _sprintHeld;
        public bool InteractPressed => _isEnabled && _interactPressed;
        public bool AttackPressed => _isEnabled && _attackPressed;
        public bool CancelPressed => _isEnabled && _cancelPressed;

        public event Action<GameAction> OnActionTriggered;
        public event Action<InputMode> OnInputModeChanged;

        public void Initialize()
        {
            // Try to find existing input actions asset
            var inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
            if (inputActions != null)
            {
                SetupFromInputActions(inputActions);
            }
            else
            {
                // Create default actions if no asset found
                SetupDefaultActions();
            }

            // Detect initial input mode
            DetectInputMode();

            Debug.Log($"[MobileInputService] Initialized. Mode: {_currentMode}");
        }

        private void SetupFromInputActions(InputActionAsset asset)
        {
            var playerMap = asset.FindActionMap("Player");
            if (playerMap != null)
            {
                _moveAction = playerMap.FindAction("Move");
                _lookAction = playerMap.FindAction("Look");
                _sprintAction = playerMap.FindAction("Sprint");
                _interactAction = playerMap.FindAction("Interact");
                _attackAction = playerMap.FindAction("Attack");

                playerMap.Enable();
            }

            var uiMap = asset.FindActionMap("UI");
            if (uiMap != null)
            {
                _cancelAction = uiMap.FindAction("Cancel");
                uiMap.Enable();
            }
        }

        private void SetupDefaultActions()
        {
            // Create simple default actions
            _moveAction = new InputAction("Move", InputActionType.Value);
            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveAction.AddBinding("<Gamepad>/leftStick");
            _moveAction.Enable();

            _lookAction = new InputAction("Look", InputActionType.Value);
            _lookAction.AddBinding("<Pointer>/delta");
            _lookAction.AddBinding("<Gamepad>/rightStick");
            _lookAction.Enable();

            _sprintAction = new InputAction("Sprint", InputActionType.Button);
            _sprintAction.AddBinding("<Keyboard>/leftShift");
            _sprintAction.AddBinding("<Gamepad>/leftStickPress");
            _sprintAction.Enable();

            _interactAction = new InputAction("Interact", InputActionType.Button);
            _interactAction.AddBinding("<Keyboard>/e");
            _interactAction.AddBinding("<Gamepad>/buttonNorth");
            _interactAction.Enable();

            _attackAction = new InputAction("Attack", InputActionType.Button);
            _attackAction.AddBinding("<Mouse>/leftButton");
            _attackAction.AddBinding("<Keyboard>/enter");
            _attackAction.AddBinding("<Gamepad>/buttonWest");
            _attackAction.Enable();

            _cancelAction = new InputAction("Cancel", InputActionType.Button);
            _cancelAction.AddBinding("<Keyboard>/escape");
            _cancelAction.AddBinding("<Gamepad>/buttonEast");
            _cancelAction.Enable();
        }

        public void Shutdown()
        {
            _moveAction?.Disable();
            _lookAction?.Disable();
            _sprintAction?.Disable();
            _interactAction?.Disable();
            _attackAction?.Disable();
            _cancelAction?.Disable();

            Debug.Log("[MobileInputService] Shutdown");
        }

        public void Tick(float deltaTime)
        {
            // Clear per-frame states
            _interactPressed = false;
            _attackPressed = false;
            _cancelPressed = false;

            // Read input based on mode
            if (_currentMode == InputMode.Touch)
            {
                ReadTouchInput();
            }
            else
            {
                ReadSystemInput();
            }

            // Check for mode changes
            CheckInputModeChange();
        }

        private void ReadTouchInput()
        {
            // Movement from virtual joystick
            if (_moveJoystick != null && _moveJoystick.IsActive)
            {
                _moveInput = _moveJoystick.Value;
            }
            else
            {
                _moveInput = Vector2.zero;
            }

            // Look from virtual joystick or swipe
            if (_lookJoystick != null && _lookJoystick.IsActive)
            {
                _lookInput = _lookJoystick.Value;
            }
            else
            {
                _lookInput = Vector2.zero;
            }

            // Sprint and other buttons handled by virtual button callbacks
        }

        private void ReadSystemInput()
        {
            // Movement
            if (_moveAction != null)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();
            }

            // Look (scale mouse delta)
            if (_lookAction != null)
            {
                var rawLook = _lookAction.ReadValue<Vector2>();
                if (_currentMode == InputMode.Keyboard)
                {
                    rawLook *= 0.1f; // Scale mouse delta
                }
                _lookInput = rawLook;
            }

            // Sprint
            _sprintHeld = _sprintAction?.IsPressed() ?? false;

            // Buttons
            if (_interactAction?.WasPressedThisFrame() ?? false)
            {
                _interactPressed = true;
                OnActionTriggered?.Invoke(GameAction.Interact);
            }

            if (_attackAction?.WasPressedThisFrame() ?? false)
            {
                _attackPressed = true;
                OnActionTriggered?.Invoke(GameAction.Attack);
            }

            if (_cancelAction?.WasPressedThisFrame() ?? false)
            {
                _cancelPressed = true;
                OnActionTriggered?.Invoke(GameAction.Cancel);
            }
        }

        private void DetectInputMode()
        {
            #if UNITY_ANDROID || UNITY_IOS
                _currentMode = InputMode.Touch;
            #else
                // Check for gamepad
                if (Gamepad.current != null)
                {
                    _currentMode = InputMode.Gamepad;
                }
                else
                {
                    _currentMode = InputMode.Keyboard;
                }
            #endif
        }

        private void CheckInputModeChange()
        {
            var newMode = _currentMode;

            // Check for gamepad input
            if (Gamepad.current != null &&
                (Gamepad.current.leftStick.ReadValue().magnitude > 0.1f ||
                 Gamepad.current.aButton.wasPressedThisFrame))
            {
                newMode = InputMode.Gamepad;
            }
            // Check for keyboard/mouse input
            else if (Keyboard.current != null &&
                     (Keyboard.current.anyKey.wasPressedThisFrame ||
                      (Mouse.current != null && Mouse.current.delta.ReadValue().magnitude > 0.1f)))
            {
                newMode = InputMode.Keyboard;
            }
            // Check for touch input
            else if (Touchscreen.current != null &&
                     Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                newMode = InputMode.Touch;
            }

            if (newMode != _currentMode)
            {
                _currentMode = newMode;
                OnInputModeChanged?.Invoke(_currentMode);
                Debug.Log($"[MobileInputService] Input mode changed to: {_currentMode}");
            }
        }

        public bool WasPressed(GameAction action)
        {
            if (!_isEnabled) return false;

            return action switch
            {
                GameAction.Interact => _interactPressed,
                GameAction.Attack => _attackPressed,
                GameAction.Cancel => _cancelPressed,
                GameAction.Back => _cancelPressed,
                _ => false
            };
        }

        public bool IsHeld(GameAction action)
        {
            if (!_isEnabled) return false;

            return action switch
            {
                GameAction.Sprint => _sprintHeld,
                _ => false
            };
        }

        public void SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            Debug.Log($"[MobileInputService] Input enabled: {enabled}");
        }

        public void SetMobileControlsVisible(bool visible)
        {
            if (_mobileControlsRoot != null)
            {
                _mobileControlsRoot.SetActive(visible);
            }
        }

        public void TriggerHaptic(HapticType type)
        {
            #if UNITY_IOS || UNITY_ANDROID
            // Trigger haptic feedback
            switch (type)
            {
                case HapticType.Light:
                    Handheld.Vibrate(); // Basic vibration
                    break;
                case HapticType.Medium:
                    Handheld.Vibrate();
                    break;
                case HapticType.Heavy:
                    Handheld.Vibrate();
                    break;
                default:
                    // Use selection feedback for other types
                    break;
            }
            #endif
        }

        // === Virtual Control Integration ===

        /// <summary>
        /// Register the move joystick.
        /// </summary>
        public void RegisterMoveJoystick(VirtualJoystick joystick)
        {
            _moveJoystick = joystick;
        }

        /// <summary>
        /// Register the look joystick.
        /// </summary>
        public void RegisterLookJoystick(VirtualJoystick joystick)
        {
            _lookJoystick = joystick;
        }

        /// <summary>
        /// Register the mobile controls root object.
        /// </summary>
        public void RegisterMobileControlsRoot(GameObject root)
        {
            _mobileControlsRoot = root;
        }

        /// <summary>
        /// Called by virtual buttons when pressed.
        /// </summary>
        public void OnVirtualButtonPressed(GameAction action)
        {
            switch (action)
            {
                case GameAction.Interact:
                    _interactPressed = true;
                    break;
                case GameAction.Attack:
                    _attackPressed = true;
                    break;
                case GameAction.Sprint:
                    _sprintHeld = true;
                    break;
            }
            OnActionTriggered?.Invoke(action);
        }

        /// <summary>
        /// Called by virtual buttons when released.
        /// </summary>
        public void OnVirtualButtonReleased(GameAction action)
        {
            switch (action)
            {
                case GameAction.Sprint:
                    _sprintHeld = false;
                    break;
            }
        }
    }
}
