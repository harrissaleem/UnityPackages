using System;
using System.Collections.Generic;
using UnityEngine;
using SimCore.Signals;

namespace SimCore.Flow
{
    /// <summary>
    /// Context object passed to game states, providing access to shared resources.
    /// Games should extend this with game-specific context data.
    /// </summary>
    public class GameFlowContext
    {
        public SignalBus SignalBus { get; set; }
        public SimWorld World { get; set; }
        public object GameData { get; set; } // Game-specific data

        // Navigation data (set before state transition)
        public object TransitionData { get; set; }
    }

    /// <summary>
    /// Base class for all game flow states.
    /// States handle enter/exit logic and can trigger transitions.
    /// </summary>
    public abstract class GameStateBase
    {
        /// <summary>
        /// Unique identifier for this state.
        /// </summary>
        public abstract string StateId { get; }

        /// <summary>
        /// Reference to the state machine (set automatically).
        /// </summary>
        protected GameFlowStateMachine StateMachine { get; private set; }

        /// <summary>
        /// Reference to the flow context (set automatically).
        /// </summary>
        protected GameFlowContext Context { get; private set; }

        internal void SetReferences(GameFlowStateMachine stateMachine, GameFlowContext context)
        {
            StateMachine = stateMachine;
            Context = context;
        }

        /// <summary>
        /// Called when entering this state. TransitionData may contain data from previous state.
        /// </summary>
        public virtual void Enter() { }

        /// <summary>
        /// Called when exiting this state.
        /// </summary>
        public virtual void Exit() { }

        /// <summary>
        /// Called every frame while this state is active.
        /// </summary>
        public virtual void Tick(float deltaTime) { }

        /// <summary>
        /// Called when a back button/gesture is pressed.
        /// Return the state ID to transition to, or null to ignore.
        /// </summary>
        public virtual string OnBackPressed() => null;

        /// <summary>
        /// Request a transition to another state.
        /// </summary>
        protected void TransitionTo(string stateId, object data = null)
        {
            StateMachine.TransitionTo(stateId, data);
        }
    }

    /// <summary>
    /// Signal emitted when game state changes.
    /// </summary>
    public struct GameStateChangedSignal : ISignal
    {
        public string PreviousStateId;
        public string NewStateId;
    }

    /// <summary>
    /// Game flow state machine that manages high-level game states.
    /// Examples: Boot -> MainMenu -> ShiftSelect -> Patrol -> ShiftEnd -> MainMenu
    /// </summary>
    public class GameFlowStateMachine
    {
        private readonly Dictionary<string, GameStateBase> _states = new();
        private readonly GameFlowContext _context;
        private GameStateBase _currentState;
        private GameStateBase _pendingState;
        private object _pendingData;
        private bool _isTransitioning;

        /// <summary>
        /// The currently active state.
        /// </summary>
        public GameStateBase CurrentState => _currentState;

        /// <summary>
        /// ID of the currently active state.
        /// </summary>
        public string CurrentStateId => _currentState?.StateId;

        /// <summary>
        /// Event fired when state changes (for non-signal subscribers).
        /// </summary>
        public event Action<string, string> OnStateChanged;

        public GameFlowStateMachine(GameFlowContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Register a state with the state machine.
        /// </summary>
        public void RegisterState(GameStateBase state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (_states.ContainsKey(state.StateId))
            {
                Debug.LogWarning($"[GameFlow] State '{state.StateId}' already registered. Replacing.");
            }

            state.SetReferences(this, _context);
            _states[state.StateId] = state;
            Debug.Log($"[GameFlow] Registered state: {state.StateId}");
        }

        /// <summary>
        /// Register multiple states at once.
        /// </summary>
        public void RegisterStates(params GameStateBase[] states)
        {
            foreach (var state in states)
            {
                RegisterState(state);
            }
        }

        /// <summary>
        /// Start the state machine with the given initial state.
        /// </summary>
        public void Start(string initialStateId, object data = null)
        {
            if (!_states.TryGetValue(initialStateId, out var state))
            {
                Debug.LogError($"[GameFlow] Initial state '{initialStateId}' not registered.");
                return;
            }

            _context.TransitionData = data;
            _currentState = state;
            _currentState.Enter();

            Debug.Log($"[GameFlow] Started with state: {initialStateId}");
            _context.SignalBus?.Publish(new GameStateChangedSignal
            {
                PreviousStateId = null,
                NewStateId = initialStateId
            });
            OnStateChanged?.Invoke(null, initialStateId);
        }

        /// <summary>
        /// Transition to a new state.
        /// </summary>
        public void TransitionTo(string stateId, object data = null)
        {
            if (!_states.TryGetValue(stateId, out var state))
            {
                Debug.LogError($"[GameFlow] State '{stateId}' not registered.");
                return;
            }

            if (_isTransitioning)
            {
                // Queue transition for next frame
                _pendingState = state;
                _pendingData = data;
                Debug.Log($"[GameFlow] Queued transition to: {stateId}");
                return;
            }

            ExecuteTransition(state, data);
        }

        private void ExecuteTransition(GameStateBase newState, object data)
        {
            _isTransitioning = true;

            var previousStateId = _currentState?.StateId;

            // Exit current state
            if (_currentState != null)
            {
                try
                {
                    _currentState.Exit();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameFlow] Error in {_currentState.StateId}.Exit(): {e}");
                }
            }

            // Set transition data
            _context.TransitionData = data;

            // Enter new state
            _currentState = newState;
            try
            {
                _currentState.Enter();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameFlow] Error in {_currentState.StateId}.Enter(): {e}");
            }

            Debug.Log($"[GameFlow] Transitioned: {previousStateId} -> {newState.StateId}");

            _context.SignalBus?.Publish(new GameStateChangedSignal
            {
                PreviousStateId = previousStateId,
                NewStateId = newState.StateId
            });
            OnStateChanged?.Invoke(previousStateId, newState.StateId);

            _isTransitioning = false;
        }

        /// <summary>
        /// Call every frame to tick the current state and process pending transitions.
        /// </summary>
        public void Tick(float deltaTime)
        {
            // Process pending transition
            if (_pendingState != null)
            {
                var state = _pendingState;
                var data = _pendingData;
                _pendingState = null;
                _pendingData = null;
                ExecuteTransition(state, data);
            }

            // Tick current state
            if (_currentState != null)
            {
                try
                {
                    _currentState.Tick(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameFlow] Error in {_currentState.StateId}.Tick(): {e}");
                }
            }
        }

        /// <summary>
        /// Handle back button press. Returns true if handled.
        /// </summary>
        public bool HandleBackPressed()
        {
            if (_currentState == null) return false;

            var targetState = _currentState.OnBackPressed();
            if (!string.IsNullOrEmpty(targetState))
            {
                TransitionTo(targetState);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a state is registered.
        /// </summary>
        public bool HasState(string stateId)
        {
            return _states.ContainsKey(stateId);
        }

        /// <summary>
        /// Get a registered state by ID.
        /// </summary>
        public T GetState<T>(string stateId) where T : GameStateBase
        {
            return _states.TryGetValue(stateId, out var state) ? state as T : null;
        }
    }
}
