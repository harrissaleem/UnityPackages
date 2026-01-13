using System;
using System.Text;
using UnityEngine;

namespace SimCore.Performance
{
    /// <summary>
    /// Debug overlay showing FPS, memory, and game stats.
    /// Enable with DEBUG_OVERLAY define or at runtime.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private bool _showOnStart = false;
        [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
        [SerializeField] private int _fontSize = 14;
        [SerializeField] private Color _backgroundColor = new Color(0, 0, 0, 0.7f);
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _warningColor = Color.yellow;
        [SerializeField] private Color _errorColor = Color.red;

        [Header("Performance Thresholds")]
        [SerializeField] private float _fpsWarning = 30f;
        [SerializeField] private float _fpsError = 15f;
        [SerializeField] private float _memoryWarning = 200f; // MB
        [SerializeField] private float _memoryError = 400f;   // MB

        // FPS calculation
        private float _deltaTime;
        private float _fps;
        private float _fpsMin = float.MaxValue;
        private float _fpsMax;
        private float _fpsUpdateInterval = 0.5f;
        private float _fpsAccum;
        private int _fpsFrames;
        private float _fpsTimer;

        // Memory tracking
        private float _memoryMB;
        private float _memoryUpdateInterval = 1f;
        private float _memoryTimer;

        // Display
        private bool _isVisible;
        private GUIStyle _backgroundStyle;
        private GUIStyle _textStyle;
        private StringBuilder _stringBuilder = new StringBuilder(256);
        private Rect _windowRect = new Rect(10, 10, 250, 200);

        // Custom stats callback
        private Func<string> _customStatsCallback;

        /// <summary>
        /// Whether the overlay is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Current FPS.
        /// </summary>
        public float FPS => _fps;

        /// <summary>
        /// Current memory usage in MB.
        /// </summary>
        public float MemoryMB => _memoryMB;

        private void Awake()
        {
            _isVisible = _showOnStart;

            #if !DEBUG && !DEVELOPMENT_BUILD
            // Disable in release builds by default
            if (!_showOnStart)
            {
                enabled = false;
            }
            #endif
        }

        private void Update()
        {
            // Toggle visibility
            if (UnityEngine.Input.GetKeyDown(_toggleKey))
            {
                _isVisible = !_isVisible;
            }

            // Three-finger tap to toggle on mobile
            #if UNITY_IOS || UNITY_ANDROID
            if (UnityEngine.Input.touchCount == 3 &&
                UnityEngine.Input.GetTouch(0).phase == TouchPhase.Began)
            {
                _isVisible = !_isVisible;
            }
            #endif

            if (!_isVisible) return;

            UpdateFPS();
            UpdateMemory();
        }

        private void UpdateFPS()
        {
            _deltaTime += Time.unscaledDeltaTime;
            _fpsAccum += Time.unscaledDeltaTime;
            _fpsFrames++;
            _fpsTimer += Time.unscaledDeltaTime;

            if (_fpsTimer >= _fpsUpdateInterval)
            {
                _fps = _fpsFrames / _fpsAccum;
                _fpsMin = Mathf.Min(_fpsMin, _fps);
                _fpsMax = Mathf.Max(_fpsMax, _fps);

                _fpsAccum = 0f;
                _fpsFrames = 0;
                _fpsTimer = 0f;
            }
        }

        private void UpdateMemory()
        {
            _memoryTimer += Time.unscaledDeltaTime;

            if (_memoryTimer >= _memoryUpdateInterval)
            {
                _memoryMB = (float)GC.GetTotalMemory(false) / (1024 * 1024);
                _memoryTimer = 0f;
            }
        }

        private void OnGUI()
        {
            if (!_isVisible) return;

            InitStyles();

            _windowRect = GUILayout.Window(0, _windowRect, DrawWindow, "Debug", _backgroundStyle);
        }

        private void InitStyles()
        {
            if (_backgroundStyle == null)
            {
                _backgroundStyle = new GUIStyle(GUI.skin.window);
                _backgroundStyle.normal.background = MakeTexture(2, 2, _backgroundColor);

                _textStyle = new GUIStyle(GUI.skin.label);
                _textStyle.fontSize = _fontSize;
                _textStyle.normal.textColor = _textColor;
            }
        }

        private void DrawWindow(int windowID)
        {
            _stringBuilder.Clear();

            // FPS
            var fpsColor = _textColor;
            if (_fps < _fpsError) fpsColor = _errorColor;
            else if (_fps < _fpsWarning) fpsColor = _warningColor;

            _textStyle.normal.textColor = fpsColor;
            _stringBuilder.AppendFormat("FPS: {0:0.0} (min: {1:0.0}, max: {2:0.0})\n", _fps, _fpsMin, _fpsMax);
            GUILayout.Label(_stringBuilder.ToString(), _textStyle);
            _stringBuilder.Clear();

            // Frame time
            _textStyle.normal.textColor = _textColor;
            _stringBuilder.AppendFormat("Frame: {0:0.00} ms\n", _deltaTime * 1000f);
            GUILayout.Label(_stringBuilder.ToString(), _textStyle);
            _stringBuilder.Clear();

            // Memory
            var memColor = _textColor;
            if (_memoryMB > _memoryError) memColor = _errorColor;
            else if (_memoryMB > _memoryWarning) memColor = _warningColor;

            _textStyle.normal.textColor = memColor;
            _stringBuilder.AppendFormat("Memory: {0:0.0} MB\n", _memoryMB);
            GUILayout.Label(_stringBuilder.ToString(), _textStyle);
            _stringBuilder.Clear();

            // Time
            _textStyle.normal.textColor = _textColor;
            _stringBuilder.AppendFormat("Time Scale: {0:0.0}x\n", Time.timeScale);
            _stringBuilder.AppendFormat("Play Time: {0:0.0}s\n", Time.time);
            GUILayout.Label(_stringBuilder.ToString(), _textStyle);
            _stringBuilder.Clear();

            // Custom stats
            if (_customStatsCallback != null)
            {
                GUILayout.Space(10);
                var customStats = _customStatsCallback();
                if (!string.IsNullOrEmpty(customStats))
                {
                    GUILayout.Label(customStats, _textStyle);
                }
            }

            // Reset button
            GUILayout.Space(10);
            if (GUILayout.Button("Reset Stats"))
            {
                _fpsMin = float.MaxValue;
                _fpsMax = 0f;
            }

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }

        /// <summary>
        /// Show the debug overlay.
        /// </summary>
        public void Show()
        {
            _isVisible = true;
        }

        /// <summary>
        /// Hide the debug overlay.
        /// </summary>
        public void Hide()
        {
            _isVisible = false;
        }

        /// <summary>
        /// Toggle visibility.
        /// </summary>
        public void Toggle()
        {
            _isVisible = !_isVisible;
        }

        /// <summary>
        /// Set callback for custom stats display.
        /// </summary>
        public void SetCustomStatsCallback(Func<string> callback)
        {
            _customStatsCallback = callback;
        }

        /// <summary>
        /// Get performance stats as string.
        /// </summary>
        public string GetStatsString()
        {
            return $"FPS: {_fps:0.0}, Memory: {_memoryMB:0.0} MB";
        }
    }
}
