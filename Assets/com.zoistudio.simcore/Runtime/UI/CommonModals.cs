using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SimCore.UI
{
    /// <summary>
    /// Data for confirmation dialogs.
    /// </summary>
    public class ConfirmationData
    {
        public string Title;
        public string Message;
        public string ConfirmText = "OK";
        public string CancelText = "Cancel";
        public Action OnConfirm;
        public Action OnCancel;
        public bool ShowCancel = true;
    }

    /// <summary>
    /// Simple confirmation modal.
    /// </summary>
    public class ConfirmationModal : ModalBase<ConfirmationData>
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TMP_Text _confirmButtonText;
        [SerializeField] private TMP_Text _cancelButtonText;

        private Action _onConfirm;
        private Action _onCancel;

        protected override bool CloseOnBack => Data?.ShowCancel ?? true;

        protected override void OnBind(ConfirmationData data)
        {
            if (data == null) return;

            _onConfirm = data.OnConfirm;
            _onCancel = data.OnCancel;

            if (_titleText != null)
                _titleText.text = data.Title ?? "";

            if (_messageText != null)
                _messageText.text = data.Message ?? "";

            if (_confirmButtonText != null)
                _confirmButtonText.text = data.ConfirmText;

            if (_cancelButtonText != null)
                _cancelButtonText.text = data.CancelText;

            if (_cancelButton != null)
                _cancelButton.gameObject.SetActive(data.ShowCancel);
        }

        private void Awake()
        {
            _confirmButton?.onClick.AddListener(OnConfirmClicked);
            _cancelButton?.onClick.AddListener(OnCancelClicked);
        }

        private void OnConfirmClicked()
        {
            _onConfirm?.Invoke();
            Close();
        }

        private void OnCancelClicked()
        {
            _onCancel?.Invoke();
            Close();
        }

        public override bool OnBackPressed()
        {
            if (CloseOnBack)
            {
                _onCancel?.Invoke();
            }
            return CloseOnBack;
        }
    }

    /// <summary>
    /// Data for reward/result popups.
    /// </summary>
    public class RewardPopupData
    {
        public string Title;
        public string Description;
        public Sprite Icon;
        public string RewardText;
        public Action OnClaim;
        public Action OnDoubleReward; // For rewarded ad
        public bool ShowDoubleButton = false;
    }

    /// <summary>
    /// Reward/result display modal.
    /// </summary>
    public class RewardModal : ModalBase<RewardPopupData>
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _rewardText;
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _doubleButton;

        private Action _onClaim;
        private Action _onDouble;

        protected override void OnBind(RewardPopupData data)
        {
            if (data == null) return;

            _onClaim = data.OnClaim;
            _onDouble = data.OnDoubleReward;

            if (_titleText != null)
                _titleText.text = data.Title ?? "";

            if (_descriptionText != null)
                _descriptionText.text = data.Description ?? "";

            if (_iconImage != null && data.Icon != null)
            {
                _iconImage.sprite = data.Icon;
                _iconImage.gameObject.SetActive(true);
            }
            else if (_iconImage != null)
            {
                _iconImage.gameObject.SetActive(false);
            }

            if (_rewardText != null)
                _rewardText.text = data.RewardText ?? "";

            if (_doubleButton != null)
                _doubleButton.gameObject.SetActive(data.ShowDoubleButton);
        }

        private void Awake()
        {
            _claimButton?.onClick.AddListener(OnClaimClicked);
            _doubleButton?.onClick.AddListener(OnDoubleClicked);
        }

        private void OnClaimClicked()
        {
            _onClaim?.Invoke();
            Close();
        }

        private void OnDoubleClicked()
        {
            _onDouble?.Invoke();
            // Don't close - let the rewarded ad callback handle it
        }
    }

    /// <summary>
    /// Loading overlay data.
    /// </summary>
    public class LoadingData
    {
        public string Message = "Loading...";
        public bool ShowProgress = false;
        public float Progress = 0f;
    }

    /// <summary>
    /// Loading overlay modal.
    /// </summary>
    public class LoadingModal : ModalBase<LoadingData>
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private GameObject _spinnerObject;

        protected override bool CloseOnOutsideClick => false;
        protected override bool CloseOnBack => false;

        protected override void OnBind(LoadingData data)
        {
            if (data == null) return;

            if (_messageText != null)
                _messageText.text = data.Message;

            if (_progressSlider != null)
            {
                _progressSlider.gameObject.SetActive(data.ShowProgress);
                _progressSlider.value = data.Progress;
            }

            if (_progressText != null)
            {
                _progressText.gameObject.SetActive(data.ShowProgress);
                _progressText.text = $"{(data.Progress * 100):0}%";
            }

            if (_spinnerObject != null)
            {
                _spinnerObject.SetActive(!data.ShowProgress);
            }
        }

        /// <summary>
        /// Update loading progress.
        /// </summary>
        public void SetProgress(float progress, string message = null)
        {
            if (_progressSlider != null)
                _progressSlider.value = progress;

            if (_progressText != null)
                _progressText.text = $"{(progress * 100):0}%";

            if (message != null && _messageText != null)
                _messageText.text = message;
        }
    }

    /// <summary>
    /// Toast notification data.
    /// </summary>
    public class ToastData
    {
        public string Message;
        public float Duration = 2f;
        public ToastType Type = ToastType.Info;
    }

    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// Toast notification popup.
    /// </summary>
    public class ToastNotification : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Colors")]
        [SerializeField] private Color _infoColor = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color _successColor = new Color(0.2f, 0.7f, 0.3f);
        [SerializeField] private Color _warningColor = new Color(0.8f, 0.6f, 0.2f);
        [SerializeField] private Color _errorColor = new Color(0.8f, 0.2f, 0.2f);

        [Header("Animation")]
        [SerializeField] private float _fadeInTime = 0.3f;
        [SerializeField] private float _fadeOutTime = 0.3f;

        private float _duration;
        private float _elapsed;
        private bool _isFadingIn;
        private bool _isFadingOut;

        public void Show(ToastData data)
        {
            if (data == null) return;

            _messageText.text = data.Message;
            _duration = data.Duration;
            _elapsed = 0f;
            _isFadingIn = _fadeInTime > 0f;
            _isFadingOut = false;

            // Set color based on type
            if (_backgroundImage != null)
            {
                _backgroundImage.color = data.Type switch
                {
                    ToastType.Success => _successColor,
                    ToastType.Warning => _warningColor,
                    ToastType.Error => _errorColor,
                    _ => _infoColor
                };
            }

            // Start transparent if fading in, otherwise visible
            if (_canvasGroup != null)
                _canvasGroup.alpha = _isFadingIn ? 0f : 1f;

            gameObject.SetActive(true);
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;

            // Fade in phase
            if (_isFadingIn)
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = Mathf.Clamp01(_elapsed / _fadeInTime);
                }

                if (_elapsed >= _fadeInTime)
                {
                    _isFadingIn = false;
                    _elapsed = 0f;
                    if (_canvasGroup != null)
                        _canvasGroup.alpha = 1f;
                }
                return;
            }

            // Display phase - wait for duration
            if (!_isFadingOut && _elapsed >= _duration)
            {
                _isFadingOut = true;
                _elapsed = 0f;
            }

            // Fade out phase
            if (_isFadingOut)
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 1f - (_elapsed / _fadeOutTime);
                }

                if (_elapsed >= _fadeOutTime)
                {
                    gameObject.SetActive(false);
                    Destroy(gameObject);
                }
            }
        }
    }
}
