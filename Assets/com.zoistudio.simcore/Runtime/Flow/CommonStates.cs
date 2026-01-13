using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SimCore.Flow
{
    /// <summary>
    /// Standard state IDs used across SimCore games.
    /// Games can use these or define their own.
    /// </summary>
    public static class StandardStateIds
    {
        public const string Boot = "boot";
        public const string Loading = "loading";
        public const string MainMenu = "main_menu";
        public const string Settings = "settings";
        public const string Session = "session";      // Active gameplay
        public const string Paused = "paused";
        public const string Summary = "summary";      // End of session
        public const string Store = "store";
    }

    /// <summary>
    /// Boot state that handles initial game loading.
    /// Transitions to next state when ready.
    /// </summary>
    public class BootState : GameStateBase
    {
        public override string StateId => StandardStateIds.Boot;

        private readonly string _nextStateId;
        private readonly Func<bool> _isReadyCheck;
        private readonly Action _onBootStart;
        private float _minBootTime;
        private float _elapsedTime;

        /// <summary>
        /// Create a boot state.
        /// </summary>
        /// <param name="nextStateId">State to transition to after boot.</param>
        /// <param name="isReadyCheck">Optional check for when boot is complete.</param>
        /// <param name="onBootStart">Optional action to run on boot start.</param>
        /// <param name="minBootTime">Minimum time to show boot screen (for branding).</param>
        public BootState(
            string nextStateId = StandardStateIds.MainMenu,
            Func<bool> isReadyCheck = null,
            Action onBootStart = null,
            float minBootTime = 0f)
        {
            _nextStateId = nextStateId;
            _isReadyCheck = isReadyCheck ?? (() => true);
            _onBootStart = onBootStart;
            _minBootTime = minBootTime;
        }

        public override void Enter()
        {
            _elapsedTime = 0f;
            _onBootStart?.Invoke();
            Debug.Log("[BootState] Boot started");
        }

        public override void Tick(float deltaTime)
        {
            _elapsedTime += deltaTime;

            if (_elapsedTime >= _minBootTime && _isReadyCheck())
            {
                Debug.Log("[BootState] Boot complete, transitioning to: " + _nextStateId);
                TransitionTo(_nextStateId);
            }
        }
    }

    /// <summary>
    /// Loading state for scene transitions or asset loading.
    /// </summary>
    public class LoadingState : GameStateBase
    {
        public override string StateId => StandardStateIds.Loading;

        private string _sceneToLoad;
        private string _nextStateId;
        private AsyncOperation _loadOperation;
        private Action _onLoadComplete;

        public float Progress => _loadOperation?.progress ?? 0f;

        public override void Enter()
        {
            var data = Context.TransitionData as LoadingStateData;
            if (data == null)
            {
                Debug.LogError("[LoadingState] No LoadingStateData provided!");
                return;
            }

            _sceneToLoad = data.SceneToLoad;
            _nextStateId = data.NextStateId;
            _onLoadComplete = data.OnLoadComplete;

            if (!string.IsNullOrEmpty(_sceneToLoad))
            {
                _loadOperation = SceneManager.LoadSceneAsync(_sceneToLoad, LoadSceneMode.Single);
                _loadOperation.allowSceneActivation = true;
                Debug.Log($"[LoadingState] Loading scene: {_sceneToLoad}");
            }
            else
            {
                // No scene to load, just transition
                CompleteLoading();
            }
        }

        public override void Tick(float deltaTime)
        {
            if (_loadOperation != null && _loadOperation.isDone)
            {
                CompleteLoading();
            }
        }

        private void CompleteLoading()
        {
            _onLoadComplete?.Invoke();

            if (!string.IsNullOrEmpty(_nextStateId))
            {
                TransitionTo(_nextStateId);
            }
        }
    }

    /// <summary>
    /// Data passed to LoadingState.
    /// </summary>
    public class LoadingStateData
    {
        public string SceneToLoad;
        public string NextStateId;
        public Action OnLoadComplete;
    }

    /// <summary>
    /// Pause state that can return to previous state.
    /// </summary>
    public class PausedState : GameStateBase
    {
        public override string StateId => StandardStateIds.Paused;

        private string _returnStateId;
        private readonly Action _onPause;
        private readonly Action _onResume;

        public PausedState(Action onPause = null, Action onResume = null)
        {
            _onPause = onPause;
            _onResume = onResume;
        }

        public override void Enter()
        {
            // Get the state to return to
            _returnStateId = Context.TransitionData as string;

            Time.timeScale = 0f; // Pause game time
            _onPause?.Invoke();

            Debug.Log($"[PausedState] Game paused. Return state: {_returnStateId}");
        }

        public override void Exit()
        {
            Time.timeScale = 1f; // Resume game time
            _onResume?.Invoke();

            Debug.Log("[PausedState] Game resumed");
        }

        public override string OnBackPressed()
        {
            // Back should resume
            return _returnStateId;
        }

        /// <summary>
        /// Resume the game (return to previous state).
        /// </summary>
        public void Resume()
        {
            if (!string.IsNullOrEmpty(_returnStateId))
            {
                TransitionTo(_returnStateId);
            }
        }

        /// <summary>
        /// Quit to main menu.
        /// </summary>
        public void QuitToMenu()
        {
            TransitionTo(StandardStateIds.MainMenu);
        }
    }

    /// <summary>
    /// Abstract base for menu states (MainMenu, Settings, Store).
    /// Provides common menu functionality.
    /// </summary>
    public abstract class MenuStateBase : GameStateBase
    {
        protected readonly string _backStateId;

        protected MenuStateBase(string backStateId = null)
        {
            _backStateId = backStateId;
        }

        public override string OnBackPressed()
        {
            return _backStateId;
        }
    }

    /// <summary>
    /// Main menu state.
    /// </summary>
    public class MainMenuState : MenuStateBase
    {
        public override string StateId => StandardStateIds.MainMenu;

        private readonly Action _onEnterMainMenu;

        public MainMenuState(Action onEnterMainMenu = null) : base(null)
        {
            _onEnterMainMenu = onEnterMainMenu;
        }

        public override void Enter()
        {
            _onEnterMainMenu?.Invoke();
            Debug.Log("[MainMenuState] Entered main menu");
        }

        public override string OnBackPressed()
        {
            // At main menu, back might show quit confirmation or do nothing
            return null;
        }
    }

    /// <summary>
    /// Settings state.
    /// </summary>
    public class SettingsState : MenuStateBase
    {
        public override string StateId => StandardStateIds.Settings;

        public SettingsState() : base(StandardStateIds.MainMenu) { }

        public override void Enter()
        {
            Debug.Log("[SettingsState] Opened settings");
        }
    }

    /// <summary>
    /// Store state for IAP and in-game purchases.
    /// </summary>
    public class StoreState : MenuStateBase
    {
        public override string StateId => StandardStateIds.Store;

        private readonly string _returnStateId;

        public StoreState(string returnStateId = StandardStateIds.MainMenu)
            : base(returnStateId)
        {
            _returnStateId = returnStateId;
        }

        public override void Enter()
        {
            // Return state can be overridden by transition data
            var customReturn = Context.TransitionData as string;
            Debug.Log($"[StoreState] Opened store. Return: {customReturn ?? _returnStateId}");
        }
    }

    /// <summary>
    /// Abstract session (gameplay) state.
    /// Games should extend this for their specific gameplay.
    /// </summary>
    public abstract class SessionStateBase : GameStateBase
    {
        public override string StateId => StandardStateIds.Session;

        protected bool IsPaused { get; private set; }

        public override void Enter()
        {
            IsPaused = false;
            OnSessionStart();
        }

        public override void Exit()
        {
            OnSessionEnd();
        }

        public override void Tick(float deltaTime)
        {
            if (!IsPaused)
            {
                OnSessionTick(deltaTime);
            }
        }

        public override string OnBackPressed()
        {
            // Back during session shows pause menu
            Pause();
            return null;
        }

        /// <summary>
        /// Called when session starts.
        /// </summary>
        protected abstract void OnSessionStart();

        /// <summary>
        /// Called when session ends.
        /// </summary>
        protected abstract void OnSessionEnd();

        /// <summary>
        /// Called every frame during active session.
        /// </summary>
        protected abstract void OnSessionTick(float deltaTime);

        /// <summary>
        /// Pause the session and show pause menu.
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
            TransitionTo(StandardStateIds.Paused, StateId);
        }
    }

    /// <summary>
    /// Summary state shown at end of session.
    /// </summary>
    public abstract class SummaryStateBase : GameStateBase
    {
        public override string StateId => StandardStateIds.Summary;

        protected object SessionResults { get; private set; }

        public override void Enter()
        {
            SessionResults = Context.TransitionData;
            OnShowSummary(SessionResults);
        }

        /// <summary>
        /// Called when summary should be displayed.
        /// </summary>
        protected abstract void OnShowSummary(object results);

        /// <summary>
        /// Continue to next session or return to menu.
        /// </summary>
        public void Continue(string nextStateId, object data = null)
        {
            TransitionTo(nextStateId, data);
        }
    }
}
