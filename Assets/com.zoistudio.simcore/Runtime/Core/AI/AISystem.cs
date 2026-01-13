// SimCore - AI System
// Simple FSM-based AI that outputs actions only

using System;
using System.Collections.Generic;
using SimCore.Actions;
using SimCore.Effects;
using SimCore.Signals;

namespace SimCore.AI
{
    /// <summary>
    /// Base class for AI states
    /// </summary>
    [Serializable]
    public abstract class AIState
    {
        public ContentId StateId;
        public string DisplayName;
        
        /// <summary>
        /// Called when entering this state
        /// </summary>
        public virtual void OnEnter(AIController controller, SimWorld world) { }
        
        /// <summary>
        /// Called when exiting this state
        /// </summary>
        public virtual void OnExit(AIController controller, SimWorld world) { }
        
        /// <summary>
        /// Called every tick - returns action requests or transitions
        /// </summary>
        public abstract AITickResult Tick(AIController controller, SimWorld world);
        
        /// <summary>
        /// Get description for debugging
        /// </summary>
        public virtual string GetDescription() => DisplayName ?? StateId.Value;
    }
    
    /// <summary>
    /// Result of an AI state tick - STRUCT to avoid allocations
    /// Uses a pooled list for action requests to eliminate GC pressure
    /// </summary>
    public struct AITickResult
    {
        public ContentId TransitionTo; // If valid, transition to this state
        public bool HasAction;
        public ActionRequest Action; // Single action (most common case)
        
        // Static cached instance for Stay() - no allocation
        private static readonly AITickResult _stayResult = new AITickResult();
        
        public static AITickResult Stay() => _stayResult;
        
        public static AITickResult DoAction(ActionRequest request)
        {
            return new AITickResult 
            { 
                HasAction = true, 
                Action = request 
            };
        }
        
        public static AITickResult Transition(ContentId stateId)
        {
            return new AITickResult { TransitionTo = stateId };
        }
        
        public static AITickResult ActionThenTransition(ActionRequest request, ContentId stateId)
        {
            return new AITickResult 
            { 
                TransitionTo = stateId,
                HasAction = true,
                Action = request
            };
        }
    }
    
    /// <summary>
    /// AI controller for an entity
    /// </summary>
    public class AIController
    {
        public SimId EntityId { get; private set; }
        public ContentId CurrentStateId { get; private set; }
        public string PreviousStateId { get; private set; } // Track previous state for returning after interrupts
        public bool IsActive { get; set; } = true;
        
        private readonly Dictionary<ContentId, AIState> _states = new();
        private AIState _currentState;
        private readonly SignalBus _signalBus;
        
        // Blackboard for storing AI-specific data
        private readonly Dictionary<string, object> _blackboard = new();
        
        public AIController(SimId entityId, SignalBus signalBus)
        {
            EntityId = entityId;
            _signalBus = signalBus;
        }
        
        /// <summary>
        /// Register a state
        /// </summary>
        public void RegisterState(AIState state)
        {
            _states[state.StateId] = state;
        }
        
        /// <summary>
        /// Register multiple states
        /// </summary>
        public void RegisterStates(IEnumerable<AIState> states)
        {
            foreach (var state in states)
            {
                RegisterState(state);
            }
        }
        
        /// <summary>
        /// Set initial state
        /// </summary>
        public void SetInitialState(ContentId stateId, SimWorld world)
        {
            if (_states.TryGetValue(stateId, out var state))
            {
                CurrentStateId = stateId;
                _currentState = state;
                _currentState.OnEnter(this, world);
                
                _signalBus?.Publish(new AIStateChangedSignal
                {
                    EntityId = EntityId,
                    OldState = null,
                    NewState = stateId.Value
                });
            }
        }
        
        /// <summary>
        /// Transition to a new state
        /// </summary>
        public void TransitionTo(ContentId stateId, SimWorld world)
        {
            if (!_states.TryGetValue(stateId, out var newState))
            {
                SimCoreLogger.LogWarning($"AI state not found: {stateId}");
                return;
            }
            
            var oldStateId = CurrentStateId;
            
            // Track previous state so we can return to it after interrupts (e.g., jaywalking)
            PreviousStateId = oldStateId.Value;
            
            // Exit current
            _currentState?.OnExit(this, world);
            
            // Enter new
            CurrentStateId = stateId;
            _currentState = newState;
            _currentState.OnEnter(this, world);
            
            _signalBus?.Publish(new AIStateChangedSignal
            {
                EntityId = EntityId,
                OldState = oldStateId.Value,
                NewState = stateId.Value
            });
        }
        
        // Cached list for tick results - reused to avoid allocations
        private readonly List<ActionRequest> _tickResultCache = new List<ActionRequest>(4);
        
        /// <summary>
        /// Tick the AI and return action requests
        /// </summary>
        public List<ActionRequest> Tick(SimWorld world)
        {
            _tickResultCache.Clear();
            
            if (!IsActive || _currentState == null)
                return _tickResultCache;
            
            var result = _currentState.Tick(this, world);
            
            // Handle transition
            if (result.TransitionTo.IsValid)
            {
                TransitionTo(result.TransitionTo, world);
            }
            
            // Add action to cached list if present
            if (result.HasAction)
            {
                _tickResultCache.Add(result.Action);
            }
            
            return _tickResultCache;
        }
        
        // Blackboard access
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (_blackboard.TryGetValue(key, out var value) && value is T typed)
                return typed;
            return defaultValue;
        }
        
        public void SetData<T>(string key, T value) => _blackboard[key] = value;
        public bool HasData(string key) => _blackboard.ContainsKey(key);
        public void ClearData(string key) => _blackboard.Remove(key);
        public void ClearAllData() => _blackboard.Clear();
        
        /// <summary>
        /// Get current state
        /// </summary>
        public AIState GetCurrentState() => _currentState;
    }
    
    /// <summary>
    /// Manages all AI controllers
    /// </summary>
    public class AIManager
    {
        private readonly Dictionary<SimId, AIController> _controllers = new();
        private readonly SignalBus _signalBus;
        private readonly ActionPipeline _actionPipeline;
        
        public AIManager(SignalBus signalBus, ActionPipeline actionPipeline)
        {
            _signalBus = signalBus;
            _actionPipeline = actionPipeline;
        }
        
        /// <summary>
        /// Create or get AI controller for an entity
        /// </summary>
        public AIController GetOrCreateController(SimId entityId)
        {
            if (!_controllers.TryGetValue(entityId, out var controller))
            {
                controller = new AIController(entityId, _signalBus);
                _controllers[entityId] = controller;
            }
            return controller;
        }
        
        /// <summary>
        /// Get controller if exists
        /// </summary>
        public AIController GetController(SimId entityId)
        {
            return _controllers.TryGetValue(entityId, out var controller) ? controller : null;
        }
        
        /// <summary>
        /// Remove controller
        /// </summary>
        public void RemoveController(SimId entityId) => _controllers.Remove(entityId);
        
        /// <summary>
        /// Tick all AI controllers
        /// </summary>
        public void Tick(SimWorld world)
        {
            foreach (var controller in _controllers.Values)
            {
                if (!controller.IsActive) continue;
                
                var requests = controller.Tick(world);
                
                // Submit action requests to pipeline
                foreach (var request in requests)
                {
                    _actionPipeline.ProcessAction(request);
                }
            }
        }
        
        /// <summary>
        /// Get all controllers
        /// </summary>
        public IEnumerable<AIController> GetAllControllers() => _controllers.Values;
        
        /// <summary>
        /// Clear all controllers
        /// </summary>
        public void Clear() => _controllers.Clear();
    }
    
    // ========== Common AI States ==========
    
    /// <summary>
    /// Idle state - does nothing
    /// </summary>
    public class IdleState : AIState
    {
        public float IdleDuration = -1; // -1 = forever
        public ContentId NextState;
        
        private float _elapsed;
        
        public IdleState()
        {
            StateId = "idle";
            DisplayName = "Idle";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            _elapsed = 0;
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            if (IdleDuration > 0)
            {
                _elapsed += UnityEngine.Time.deltaTime;
                if (_elapsed >= IdleDuration && NextState.IsValid)
                {
                    return AITickResult.Transition(NextState);
                }
            }
            return AITickResult.Stay();
        }
    }
    
    /// <summary>
    /// Follow target state
    /// </summary>
    public class FollowTargetState : AIState
    {
        public ContentId MoveActionId = "move";
        public float StopDistance = 1f;
        public float UpdateInterval = 0.5f;
        
        private float _lastUpdate;
        
        public FollowTargetState()
        {
            StateId = "follow";
            DisplayName = "Following";
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            var targetId = controller.GetData<SimId>("target");
            if (!targetId.IsValid) return AITickResult.Stay();
            
            float distance = world.Partition.GetDistance(controller.EntityId, targetId);
            if (distance <= StopDistance) return AITickResult.Stay();
            
            _lastUpdate += UnityEngine.Time.deltaTime;
            if (_lastUpdate < UpdateInterval) return AITickResult.Stay();
            _lastUpdate = 0;
            
            var request = new ActionRequest(controller.EntityId, MoveActionId, targetId);
            return AITickResult.DoAction(request);
        }
    }
    
    /// <summary>
    /// Patrol between waypoints
    /// </summary>
    public class PatrolState : AIState
    {
        public ContentId MoveActionId = "move";
        public List<SimId> Waypoints = new();
        public float WaypointReachDistance = 0.5f;
        public float WaitTimeAtWaypoint;
        
        private int _currentWaypointIndex;
        private float _waitTimer;
        
        public PatrolState()
        {
            StateId = "patrol";
            DisplayName = "Patrolling";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            _currentWaypointIndex = 0;
            _waitTimer = 0;
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            if (Waypoints.Count == 0) return AITickResult.Stay();
            
            // Waiting at waypoint
            if (_waitTimer > 0)
            {
                _waitTimer -= UnityEngine.Time.deltaTime;
                return AITickResult.Stay();
            }
            
            var targetWaypoint = Waypoints[_currentWaypointIndex];
            float distance = world.Partition.GetDistance(controller.EntityId, targetWaypoint);
            
            if (distance <= WaypointReachDistance)
            {
                // Reached waypoint, move to next
                _currentWaypointIndex = (_currentWaypointIndex + 1) % Waypoints.Count;
                _waitTimer = WaitTimeAtWaypoint;
                return AITickResult.Stay();
            }
            
            var request = new ActionRequest(controller.EntityId, MoveActionId, targetWaypoint);
            return AITickResult.DoAction(request);
        }
    }
    
    /// <summary>
    /// Chase target state
    /// </summary>
    public class ChaseState : AIState
    {
        public ContentId MoveActionId = "move";
        public float CatchDistance = 1.5f;
        public float LoseDistance = 20f;
        public ContentId CatchState;
        public ContentId LoseState;
        
        public ChaseState()
        {
            StateId = "chase";
            DisplayName = "Chasing";
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            var targetId = controller.GetData<SimId>("target");
            if (!targetId.IsValid)
            {
                if (LoseState.IsValid)
                    return AITickResult.Transition(LoseState);
                return AITickResult.Stay();
            }
            
            float distance = world.Partition.GetDistance(controller.EntityId, targetId);
            
            if (distance <= CatchDistance && CatchState.IsValid)
            {
                return AITickResult.Transition(CatchState);
            }
            
            if (distance >= LoseDistance && LoseState.IsValid)
            {
                return AITickResult.Transition(LoseState);
            }
            
            var request = new ActionRequest(controller.EntityId, MoveActionId, targetId);
            return AITickResult.DoAction(request);
        }
    }
    
    /// <summary>
    /// Search/investigate state
    /// </summary>
    public class SearchState : AIState
    {
        public float SearchDuration = 10f;
        public ContentId IdleState;
        
        private float _elapsed;
        
        public SearchState()
        {
            StateId = "search";
            DisplayName = "Searching";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            _elapsed = 0;
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            _elapsed += UnityEngine.Time.deltaTime;
            
            if (_elapsed >= SearchDuration && IdleState.IsValid)
            {
                return AITickResult.Transition(IdleState);
            }
            
            return AITickResult.Stay();
        }
    }
}

