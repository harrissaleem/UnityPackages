using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimCore.Input
{
    /// <summary>
    /// Virtual joystick for mobile touch controls.
    /// Supports both fixed and floating joystick modes.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Joystick Settings")]
        [SerializeField] private float _handleRange = 50f;
        [SerializeField] private float _deadZone = 0.1f;
        [SerializeField] private bool _floating = false;

        [Header("UI References")]
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _handleImage;

        [Header("Visuals")]
        [SerializeField] private float _inactiveAlpha = 0.3f;
        [SerializeField] private float _activeAlpha = 0.8f;
        [SerializeField] private bool _hideWhenInactive = false;

        private RectTransform _rectTransform;
        private Canvas _canvas;
        private Camera _uiCamera;
        private Vector2 _startPosition;
        private Vector2 _input;
        private bool _isActive;

        /// <summary>
        /// Current joystick value as normalized Vector2 (-1 to 1).
        /// </summary>
        public Vector2 Value => _input;

        /// <summary>
        /// Whether the joystick is currently being touched.
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// Horizontal input (-1 to 1).
        /// </summary>
        public float Horizontal => _input.x;

        /// <summary>
        /// Vertical input (-1 to 1).
        /// </summary>
        public float Vertical => _input.y;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();

            if (_canvas != null && _canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                _uiCamera = _canvas.worldCamera;
            }

            _startPosition = _background.anchoredPosition;
            SetVisualState(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isActive = true;
            SetVisualState(true);

            if (_floating)
            {
                // Move joystick to touch position
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform,
                    eventData.position,
                    _uiCamera,
                    out var localPoint
                );
                _background.anchoredPosition = localPoint;
            }

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background,
                eventData.position,
                _uiCamera,
                out position
            );

            // Clamp to handle range
            position = Vector2.ClampMagnitude(position, _handleRange);

            // Move handle
            _handle.anchoredPosition = position;

            // Calculate normalized input
            _input = position / _handleRange;

            // Apply dead zone
            if (_input.magnitude < _deadZone)
            {
                _input = Vector2.zero;
            }
            else
            {
                // Rescale to account for dead zone
                _input = _input.normalized * ((_input.magnitude - _deadZone) / (1f - _deadZone));
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isActive = false;
            _input = Vector2.zero;

            // Reset handle position
            _handle.anchoredPosition = Vector2.zero;

            // Reset background position if floating
            if (_floating)
            {
                _background.anchoredPosition = _startPosition;
            }

            SetVisualState(false);
        }

        private void SetVisualState(bool active)
        {
            float alpha = active ? _activeAlpha : _inactiveAlpha;

            if (_backgroundImage != null)
            {
                var color = _backgroundImage.color;
                color.a = alpha;
                _backgroundImage.color = color;
            }

            if (_handleImage != null)
            {
                var color = _handleImage.color;
                color.a = alpha;
                _handleImage.color = color;
            }

            if (_hideWhenInactive && !active)
            {
                _background.gameObject.SetActive(false);
            }
            else
            {
                _background.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Set the handle range (max distance handle can move).
        /// </summary>
        public void SetHandleRange(float range)
        {
            _handleRange = range;
        }

        /// <summary>
        /// Set the dead zone threshold.
        /// </summary>
        public void SetDeadZone(float deadZone)
        {
            _deadZone = Mathf.Clamp01(deadZone);
        }

        /// <summary>
        /// Enable or disable floating mode.
        /// </summary>
        public void SetFloating(bool floating)
        {
            _floating = floating;
        }
    }
}
