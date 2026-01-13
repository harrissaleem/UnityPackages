using System;
using System.Collections.Generic;
using UnityEngine;
using SimCore.Signals;

namespace SimCore.UI
{
    /// <summary>
    /// Signal emitted when a screen is shown.
    /// </summary>
    public struct ScreenShownSignal : ISignal
    {
        public string ScreenId;
        public bool IsModal;
    }

    /// <summary>
    /// Signal emitted when a screen is hidden.
    /// </summary>
    public struct ScreenHiddenSignal : ISignal
    {
        public string ScreenId;
    }

    /// <summary>
    /// UI layer types for organization.
    /// </summary>
    public enum UILayer
    {
        Background = 0,    // Static backgrounds
        Screen = 100,      // Main screens (stack-based)
        HUD = 200,         // Gameplay HUD (always visible during gameplay)
        Modal = 300,       // Modals/popups (on top of screens)
        Tooltip = 400,     // Tooltips
        Loading = 500,     // Loading overlay
        Debug = 600        // Debug overlay (topmost)
    }

    /// <summary>
    /// Central UI navigation system with stack-based screen management.
    /// Handles screens, modals, HUD, and transitions.
    /// </summary>
    public class UINavigator : MonoBehaviour
    {
        [Header("Layer Roots")]
        [SerializeField] private Transform _screenRoot;
        [SerializeField] private Transform _hudRoot;
        [SerializeField] private Transform _modalRoot;
        [SerializeField] private Transform _loadingRoot;
        [SerializeField] private Transform _debugRoot;

        [Header("Settings")]
        [SerializeField] private bool _hideScreensWhenPushed = true;

        // Screen management
        private readonly Dictionary<string, ScreenBase> _registeredScreens = new();
        private readonly Stack<ScreenBase> _screenStack = new();
        private ScreenBase _currentScreen;

        // Modal management
        private readonly List<ModalBase> _activeModals = new();

        // HUD management
        private readonly Dictionary<string, HUDBase> _registeredHUDs = new();
        private HUDBase _activeHUD;

        // Signal bus reference
        private SignalBus _signalBus;

        /// <summary>
        /// Currently visible screen (top of stack).
        /// </summary>
        public ScreenBase CurrentScreen => _currentScreen;

        /// <summary>
        /// Currently active HUD.
        /// </summary>
        public HUDBase ActiveHUD => _activeHUD;

        /// <summary>
        /// Whether any modal is currently showing.
        /// </summary>
        public bool HasActiveModal => _activeModals.Count > 0;

        /// <summary>
        /// Number of screens in the navigation stack.
        /// </summary>
        public int StackDepth => _screenStack.Count;

        /// <summary>
        /// Initialize the navigator with a signal bus.
        /// </summary>
        public void Initialize(SignalBus signalBus)
        {
            _signalBus = signalBus;

            // Auto-discover and register screens/HUDs in children
            AutoRegisterChildren();

            Debug.Log("[UINavigator] Initialized");
        }

        /// <summary>
        /// Auto-discover and register all ScreenBase and HUDBase components in children.
        /// </summary>
        public void AutoRegisterChildren()
        {
            // Register screens
            var screens = GetComponentsInChildren<ScreenBase>(true);
            foreach (var screen in screens)
            {
                if (!_registeredScreens.ContainsKey(screen.ScreenId))
                {
                    RegisterScreen(screen);
                }
            }

            // Register HUDs
            var huds = GetComponentsInChildren<HUDBase>(true);
            foreach (var hud in huds)
            {
                if (!_registeredHUDs.ContainsKey(hud.HUDId))
                {
                    RegisterHUD(hud);
                }
            }
        }

        /// <summary>
        /// Register a screen for navigation.
        /// </summary>
        public void RegisterScreen(ScreenBase screen)
        {
            if (screen == null) return;

            var screenId = screen.ScreenId;
            if (_registeredScreens.ContainsKey(screenId))
            {
                Debug.LogWarning($"[UINavigator] Screen '{screenId}' already registered.");
                return;
            }

            _registeredScreens[screenId] = screen;
            screen.Navigator = this;
            screen.gameObject.SetActive(false);

            // Parent to screen root if specified
            if (_screenRoot != null && screen.transform.parent != _screenRoot)
            {
                screen.transform.SetParent(_screenRoot, false);
            }

            Debug.Log($"[UINavigator] Registered screen: {screenId}");
        }

        /// <summary>
        /// Register a HUD component.
        /// </summary>
        public void RegisterHUD(HUDBase hud)
        {
            if (hud == null) return;

            var hudId = hud.HUDId;
            if (_registeredHUDs.ContainsKey(hudId))
            {
                Debug.LogWarning($"[UINavigator] HUD '{hudId}' already registered.");
                return;
            }

            _registeredHUDs[hudId] = hud;
            hud.Navigator = this;
            hud.gameObject.SetActive(false);

            // Parent to HUD root if specified
            if (_hudRoot != null && hud.transform.parent != _hudRoot)
            {
                hud.transform.SetParent(_hudRoot, false);
            }

            Debug.Log($"[UINavigator] Registered HUD: {hudId}");
        }

        /// <summary>
        /// Show a screen, pushing current screen to stack.
        /// </summary>
        public void PushScreen(string screenId, object data = null)
        {
            if (!_registeredScreens.TryGetValue(screenId, out var screen))
            {
                Debug.LogError($"[UINavigator] Screen '{screenId}' not registered.");
                return;
            }

            // Hide current screen (optionally keep in stack)
            if (_currentScreen != null)
            {
                if (_hideScreensWhenPushed)
                {
                    _currentScreen.gameObject.SetActive(false);
                }
                _screenStack.Push(_currentScreen);
                _currentScreen.OnHide();
            }

            // Show new screen
            _currentScreen = screen;
            _currentScreen.gameObject.SetActive(true);
            _currentScreen.OnShow(data);

            _signalBus?.Publish(new ScreenShownSignal { ScreenId = screenId, IsModal = false });
            Debug.Log($"[UINavigator] Pushed screen: {screenId} (stack depth: {_screenStack.Count})");
        }

        /// <summary>
        /// Replace current screen without pushing to stack.
        /// </summary>
        public void ReplaceScreen(string screenId, object data = null)
        {
            if (!_registeredScreens.TryGetValue(screenId, out var screen))
            {
                Debug.LogError($"[UINavigator] Screen '{screenId}' not registered.");
                return;
            }

            // Hide current screen without pushing to stack
            if (_currentScreen != null)
            {
                var oldId = _currentScreen.ScreenId;
                _currentScreen.OnHide();
                _currentScreen.gameObject.SetActive(false);
                _signalBus?.Publish(new ScreenHiddenSignal { ScreenId = oldId });
            }

            // Show new screen
            _currentScreen = screen;
            _currentScreen.gameObject.SetActive(true);
            _currentScreen.OnShow(data);

            _signalBus?.Publish(new ScreenShownSignal { ScreenId = screenId, IsModal = false });
            Debug.Log($"[UINavigator] Replaced screen with: {screenId}");
        }

        /// <summary>
        /// Pop current screen and return to previous screen.
        /// </summary>
        public bool PopScreen()
        {
            if (_screenStack.Count == 0)
            {
                Debug.Log("[UINavigator] Screen stack is empty, cannot pop.");
                return false;
            }

            // Hide current screen
            if (_currentScreen != null)
            {
                var oldId = _currentScreen.ScreenId;
                _currentScreen.OnHide();
                _currentScreen.gameObject.SetActive(false);
                _signalBus?.Publish(new ScreenHiddenSignal { ScreenId = oldId });
            }

            // Show previous screen
            _currentScreen = _screenStack.Pop();
            _currentScreen.gameObject.SetActive(true);
            _currentScreen.OnResume();

            _signalBus?.Publish(new ScreenShownSignal { ScreenId = _currentScreen.ScreenId, IsModal = false });
            Debug.Log($"[UINavigator] Popped to screen: {_currentScreen.ScreenId} (stack depth: {_screenStack.Count})");

            return true;
        }

        /// <summary>
        /// Pop all screens except root and show specified screen.
        /// </summary>
        public void PopToRoot(string rootScreenId = null)
        {
            // Hide current
            if (_currentScreen != null)
            {
                _currentScreen.OnHide();
                _currentScreen.gameObject.SetActive(false);
            }

            // Clear stack
            while (_screenStack.Count > 0)
            {
                var screen = _screenStack.Pop();
                screen.OnHide();
                screen.gameObject.SetActive(false);
            }

            // Show root screen
            if (!string.IsNullOrEmpty(rootScreenId))
            {
                if (_registeredScreens.TryGetValue(rootScreenId, out var root))
                {
                    _currentScreen = root;
                    _currentScreen.gameObject.SetActive(true);
                    _currentScreen.OnShow(null);
                }
            }
            else
            {
                _currentScreen = null;
            }

            Debug.Log($"[UINavigator] Popped to root: {rootScreenId ?? "none"}");
        }

        /// <summary>
        /// Show a modal on top of current screen.
        /// </summary>
        public T ShowModal<T>(string modalPrefabName, object data = null) where T : ModalBase
        {
            // Load modal prefab
            var prefab = Resources.Load<T>($"UI/Modals/{modalPrefabName}");
            if (prefab == null)
            {
                Debug.LogError($"[UINavigator] Modal prefab not found: UI/Modals/{modalPrefabName}");
                return null;
            }

            var parent = _modalRoot != null ? _modalRoot : transform;
            var modal = Instantiate(prefab, parent);
            modal.Navigator = this;

            _activeModals.Add(modal);
            modal.OnShow(data);

            _signalBus?.Publish(new ScreenShownSignal { ScreenId = modalPrefabName, IsModal = true });
            Debug.Log($"[UINavigator] Showed modal: {modalPrefabName}");

            return modal;
        }

        /// <summary>
        /// Show a modal that already exists in the scene.
        /// </summary>
        public void ShowExistingModal(ModalBase modal, object data = null)
        {
            if (modal == null) return;

            modal.Navigator = this;
            modal.gameObject.SetActive(true);

            if (!_activeModals.Contains(modal))
            {
                _activeModals.Add(modal);
            }

            modal.OnShow(data);
            Debug.Log($"[UINavigator] Showed existing modal: {modal.name}");
        }

        /// <summary>
        /// Hide a modal.
        /// </summary>
        public void HideModal(ModalBase modal, bool destroy = true)
        {
            if (modal == null) return;

            modal.OnHide();
            _activeModals.Remove(modal);

            if (destroy)
            {
                Destroy(modal.gameObject);
            }
            else
            {
                modal.gameObject.SetActive(false);
            }

            Debug.Log($"[UINavigator] Hid modal: {modal.name}");
        }

        /// <summary>
        /// Hide all active modals.
        /// </summary>
        public void HideAllModals()
        {
            for (int i = _activeModals.Count - 1; i >= 0; i--)
            {
                HideModal(_activeModals[i]);
            }
        }

        /// <summary>
        /// Show a HUD.
        /// </summary>
        public void ShowHUD(string hudId)
        {
            if (!_registeredHUDs.TryGetValue(hudId, out var hud))
            {
                Debug.LogError($"[UINavigator] HUD '{hudId}' not registered.");
                return;
            }

            // Hide current HUD
            if (_activeHUD != null && _activeHUD != hud)
            {
                _activeHUD.OnHide();
                _activeHUD.gameObject.SetActive(false);
            }

            // Show new HUD
            _activeHUD = hud;
            _activeHUD.gameObject.SetActive(true);
            _activeHUD.OnShow();

            Debug.Log($"[UINavigator] Showing HUD: {hudId}");
        }

        /// <summary>
        /// Hide the active HUD.
        /// </summary>
        public void HideHUD()
        {
            if (_activeHUD != null)
            {
                _activeHUD.OnHide();
                _activeHUD.gameObject.SetActive(false);
                _activeHUD = null;
                Debug.Log("[UINavigator] Hid HUD");
            }
        }

        /// <summary>
        /// Handle back button press. Returns true if handled.
        /// </summary>
        public bool HandleBackPressed()
        {
            // First, try to close topmost modal
            if (_activeModals.Count > 0)
            {
                var topModal = _activeModals[_activeModals.Count - 1];
                if (topModal.OnBackPressed())
                {
                    HideModal(topModal);
                    return true;
                }
            }

            // Then, try current screen
            if (_currentScreen != null)
            {
                if (_currentScreen.OnBackPressed())
                {
                    return PopScreen();
                }
            }

            return false;
        }

        /// <summary>
        /// Get a registered screen by ID.
        /// </summary>
        public T GetScreen<T>(string screenId) where T : ScreenBase
        {
            return _registeredScreens.TryGetValue(screenId, out var screen) ? screen as T : null;
        }

        /// <summary>
        /// Get a registered HUD by ID.
        /// </summary>
        public T GetHUD<T>(string hudId) where T : HUDBase
        {
            return _registeredHUDs.TryGetValue(hudId, out var hud) ? hud as T : null;
        }
    }
}
