using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SimCore.Services;

namespace SimCore.Input
{
    /// <summary>
    /// Virtual button for mobile touch controls.
    /// Supports press, hold, and release events.
    /// </summary>
    public class VirtualButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Button Settings")]
        [SerializeField] private GameAction _action = GameAction.Interact;
        [SerializeField] private bool _isHoldButton = false;

        [Header("UI References")]
        [SerializeField] private Image _buttonImage;
        [SerializeField] private Image _iconImage;

        [Header("Visuals")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _pressedColor = new Color(0.8f, 0.8f, 0.8f);
        [SerializeField] private float _pressedScale = 0.9f;

        private MobileInputService _inputService;
        private RectTransform _rectTransform;
        private Vector3 _originalScale;
        private bool _isPressed;

        /// <summary>
        /// The action this button triggers.
        /// </summary>
        public GameAction Action => _action;

        /// <summary>
        /// Whether the button is currently pressed.
        /// </summary>
        public bool IsPressed => _isPressed;

        /// <summary>
        /// Whether this is a hold button (continuous action) vs tap button (single trigger).
        /// </summary>
        public bool IsHoldButton => _isHoldButton;

        /// <summary>
        /// Event fired when button is pressed.
        /// </summary>
        public event Action OnPressed;

        /// <summary>
        /// Event fired when button is released.
        /// </summary>
        public event Action OnReleased;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _originalScale = _rectTransform.localScale;

            if (_buttonImage == null)
            {
                _buttonImage = GetComponent<Image>();
            }
        }

        private void Start()
        {
            // Try to get input service
            if (ServiceLocator.TryGet<IInputService>(out var service))
            {
                _inputService = service as MobileInputService;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            SetVisualState(true);

            // Notify input service
            _inputService?.OnVirtualButtonPressed(_action);

            OnPressed?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed) return;

            _isPressed = false;
            SetVisualState(false);

            // Notify input service
            _inputService?.OnVirtualButtonReleased(_action);

            OnReleased?.Invoke();
        }

        private void SetVisualState(bool pressed)
        {
            // Color change
            if (_buttonImage != null)
            {
                _buttonImage.color = pressed ? _pressedColor : _normalColor;
            }

            // Scale change
            if (_rectTransform != null)
            {
                _rectTransform.localScale = pressed
                    ? _originalScale * _pressedScale
                    : _originalScale;
            }
        }

        /// <summary>
        /// Set the action this button triggers.
        /// </summary>
        public void SetAction(GameAction action)
        {
            _action = action;
        }

        /// <summary>
        /// Set the button icon.
        /// </summary>
        public void SetIcon(Sprite icon)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
            }
        }

        /// <summary>
        /// Set button colors.
        /// </summary>
        public void SetColors(Color normal, Color pressed)
        {
            _normalColor = normal;
            _pressedColor = pressed;

            if (!_isPressed && _buttonImage != null)
            {
                _buttonImage.color = normal;
            }
        }
    }

    /// <summary>
    /// Container for mobile controls that auto-hides on non-touch platforms.
    /// </summary>
    public class MobileControlsContainer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _autoHideOnDesktop = true;
        [SerializeField] private VirtualJoystick _moveJoystick;
        [SerializeField] private VirtualJoystick _lookJoystick;
        [SerializeField] private VirtualButton[] _buttons;

        private MobileInputService _inputService;

        private void Start()
        {
            // Get input service
            if (ServiceLocator.TryGet<IInputService>(out var service))
            {
                _inputService = service as MobileInputService;

                // Register components
                if (_moveJoystick != null)
                {
                    _inputService?.RegisterMoveJoystick(_moveJoystick);
                }
                if (_lookJoystick != null)
                {
                    _inputService?.RegisterLookJoystick(_lookJoystick);
                }
                _inputService?.RegisterMobileControlsRoot(gameObject);

                // Subscribe to mode changes
                if (_inputService != null)
                {
                    _inputService.OnInputModeChanged += OnInputModeChanged;
                    OnInputModeChanged(_inputService.CurrentMode);
                }
            }

            // Auto-hide on desktop
            #if !UNITY_IOS && !UNITY_ANDROID
            if (_autoHideOnDesktop)
            {
                gameObject.SetActive(false);
            }
            #endif
        }

        private void OnDestroy()
        {
            if (_inputService != null)
            {
                _inputService.OnInputModeChanged -= OnInputModeChanged;
            }
        }

        private void OnInputModeChanged(InputMode mode)
        {
            // Show controls only in touch mode
            bool showControls = mode == InputMode.Touch;

            #if !UNITY_IOS && !UNITY_ANDROID
            if (_autoHideOnDesktop)
            {
                showControls = false;
            }
            #endif

            gameObject.SetActive(showControls);
        }

        /// <summary>
        /// Force show/hide mobile controls.
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
