// SimCore - Scenario Module
// Unified system for all scenarios - from simple (jaywalking) to complex (bank heist with car chase)
// Uses orchestrators as plugins for game-specific logic

using System;
using System.Collections.Generic;
using UnityEngine;
using SimCore.Signals;

namespace SimCore.Modules.Scenarios
{
    #region Signals
    
    public struct ScenarioSpawnedSignal : ISignal
    {
        public SimId ScenarioId;
        public string TypeId;
        public Vector3 Location;
        public SimId[] ParticipantIds;
    }
    
    public struct ScenarioPhaseChangedSignal : ISignal
    {
        public SimId ScenarioId;
        public string TypeId;
        public string OldPhase;
        public string NewPhase;
    }
    
    public struct ScenarioEngagedSignal : ISignal
    {
        public SimId ScenarioId;
        public string TypeId;
        public SimId ActorId;
    }
    
    public struct ScenarioResolvedSignal : ISignal
    {
        public SimId ScenarioId;
        public string TypeId;
        public string Resolution;
        public bool Success;
    }
    
    public struct ScenarioExpiredSignal : ISignal
    {
        public SimId ScenarioId;
        public string TypeId;
    }
    
    #endregion
    
    #region Data Structures
    
    /// <summary>
    /// A participant in a scenario (NPC, vehicle, etc.)
    /// </summary>
    [Serializable]
    public struct ScenarioParticipant
    {
        public SimId EntityId;
        public string Role;  // Game defines roles via constants
        
        public ScenarioParticipant(SimId entityId, string role)
        {
            EntityId = entityId;
            Role = role;
        }
        
        public bool IsValid => EntityId.IsValid;
    }
    
    /// <summary>
    /// Active scenario instance - universal container for any scenario type
    /// </summary>
    public class ScenarioInstance
    {
        public SimId Id;
        public string TypeId;                    // "theft", "robbery", "hostage", etc.
        public string Phase;                     // Current phase - orchestrator defines valid phases
        public Vector3 Location;                 // Can update for moving scenarios (car chase)
        public float StartTime;
        public float ExpirationTime;             // 0 = never expires
        
        // Participants - flexible for any scenario size
        public List<ScenarioParticipant> Participants = new();
        
        // Custom data per scenario type
        public Dictionary<string, object> Data = new();
        
        // State
        public bool PlayerEngaged;
        public SimId EngagedByActorId;
        public bool IsResolved;
        public string Resolution;
        
        // Convenience accessors
        public bool IsActive => !IsResolved;
        
        public SimId GetParticipant(string role)
        {
            foreach (var p in Participants)
            {
                if (p.Role == role) return p.EntityId;
            }
            return SimId.Invalid;
        }
        
        public SimId[] GetParticipants(string role)
        {
            var result = new List<SimId>();
            foreach (var p in Participants)
            {
                if (p.Role == role) result.Add(p.EntityId);
            }
            return result.ToArray();
        }
        
        public void AddParticipant(SimId entityId, string role)
        {
            Participants.Add(new ScenarioParticipant(entityId, role));
        }
        
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (Data.TryGetValue(key, out var value) && value is T typed)
                return typed;
            return defaultValue;
        }
        
        public void SetData<T>(string key, T value)
        {
            Data[key] = value;
        }
    }
    
    #endregion
    
    #region Orchestrator Interface
    
    /// <summary>
    /// Orchestrator plugin interface - game-specific logic for scenario types.
    /// Implement this for complex scenarios that need custom spawn/tick/resolve logic.
    /// Simple scenarios (no orchestrator) just use default behavior.
    /// </summary>
    public interface IScenarioOrchestrator
    {
        /// <summary>
        /// The scenario type this orchestrator handles (matches TypeId)
        /// </summary>
        string TypeId { get; }
        
        /// <summary>
        /// Called when scenario is being spawned. Set up participants, AI, initial phase.
        /// Return false to cancel the spawn.
        /// </summary>
        bool OnSpawn(ScenarioInstance instance, Vector3 location, bool isDispatch, SimWorld world);
        
        /// <summary>
        /// Called every frame for active scenarios. Update phase, check conditions.
        /// </summary>
        void OnTick(ScenarioInstance instance, float deltaTime, Vector3 playerPosition, SimWorld world);
        
        /// <summary>
        /// Called when player engages with a participant in this scenario.
        /// </summary>
        void OnPlayerEngaged(ScenarioInstance instance, SimId participantId, SimWorld world);
        
        /// <summary>
        /// Called when phase changes. Handle transitions, spawn new participants, etc.
        /// </summary>
        void OnPhaseChanged(ScenarioInstance instance, string oldPhase, string newPhase, SimWorld world);
        
        /// <summary>
        /// Called when scenario is being resolved. Handle rewards, cleanup.
        /// </summary>
        void OnResolve(ScenarioInstance instance, string resolution, SimWorld world);
        
        /// <summary>
        /// Called when scenario expires without resolution.
        /// </summary>
        void OnExpire(ScenarioInstance instance, SimWorld world);
        
        /// <summary>
        /// Called when scenario is cleaned up. Despawn NPCs if needed.
        /// </summary>
        void OnCleanup(ScenarioInstance instance, SimWorld world);
    }
    
    /// <summary>
    /// Base class for orchestrators with default empty implementations
    /// </summary>
    public abstract class ScenarioOrchestratorBase : IScenarioOrchestrator
    {
        public abstract string TypeId { get; }
        
        public virtual bool OnSpawn(ScenarioInstance instance, Vector3 location, bool isDispatch, SimWorld world) => true;
        public virtual void OnTick(ScenarioInstance instance, float deltaTime, Vector3 playerPosition, SimWorld world) { }
        public virtual void OnPlayerEngaged(ScenarioInstance instance, SimId participantId, SimWorld world) 
        {
            instance.PlayerEngaged = true;
        }
        public virtual void OnPhaseChanged(ScenarioInstance instance, string oldPhase, string newPhase, SimWorld world) { }
        public virtual void OnResolve(ScenarioInstance instance, string resolution, SimWorld world) { }
        public virtual void OnExpire(ScenarioInstance instance, SimWorld world) { }
        public virtual void OnCleanup(ScenarioInstance instance, SimWorld world) { }
    }
    
    #endregion
    
    #region Module Interface
    
    public interface IScenarioModule : ISimModule
    {
        // Orchestrator registration
        void RegisterOrchestrator(IScenarioOrchestrator orchestrator);
        IScenarioOrchestrator GetOrchestrator(string typeId);
        T GetOrchestrator<T>() where T : class, IScenarioOrchestrator;
        
        // Player position for orchestrator ticks
        void SetPlayerPosition(Vector3 position);
        
        // Scenario lifecycle
        ScenarioInstance SpawnScenario(string typeId, Vector3 location, bool isDispatch = false);
        ScenarioInstance SpawnSimpleScenario(string typeId, Vector3 location, SimId participantId, string role = "subject", float expirationSeconds = 120f);
        void SetPhase(SimId scenarioId, string phase);
        void EngageScenario(SimId scenarioId, SimId actorId);
        void DisengageScenario(SimId scenarioId);
        void ResolveScenario(SimId scenarioId, string resolution, bool success = true);
        
        // Queries
        ScenarioInstance GetScenario(SimId scenarioId);
        ScenarioInstance[] GetScenariosForEntity(SimId entityId);
        ScenarioInstance[] GetActiveScenarios();
        ScenarioInstance[] GetScenariosByType(string typeId);
        
        // Cleanup
        void Clear();
    }
    
    #endregion
    
    #region Module Implementation
    
    public class ScenarioModule : IScenarioModule
    {
        private readonly Dictionary<string, IScenarioOrchestrator> _orchestrators = new();
        private readonly Dictionary<SimId, ScenarioInstance> _scenarios = new();
        private readonly Dictionary<SimId, List<SimId>> _entityToScenarios = new(); // Multiple scenarios per entity
        private readonly List<SimId> _toRemove = new();
        
        private int _nextId = 1;
        private SignalBus _signalBus;
        private SimWorld _world;
        private Vector3 _playerPosition;
        
        /// <summary>
        /// Set the player position for orchestrator ticks.
        /// Call this from game code each frame.
        /// </summary>
        public void SetPlayerPosition(Vector3 position)
        {
            _playerPosition = position;
        }
        
        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
        }
        
        public void Tick(float deltaTime)
        {
            float currentTime = Time.time;
            _toRemove.Clear();
            
            // Player position is set externally via SetPlayerPosition()
            
            // Use a temporary list of IDs to avoid InvalidOperationException if scenarios are added/removed during tick
            // This can happen if an orchestrator spawns another scenario or resolves itself during OnTick
            var scenarioIds = new List<SimId>(_scenarios.Keys);
            
            foreach (var id in scenarioIds)
            {
                // Check if it still exists (in case a previous scenario in this loop cleared everything)
                if (!_scenarios.TryGetValue(id, out var instance)) continue;
                
                if (!instance.IsActive)
                {
                    _toRemove.Add(id);
                    continue;
                }
                
                // Check expiration
                if (instance.ExpirationTime > 0 && currentTime >= instance.ExpirationTime && !instance.PlayerEngaged)
                {
                    ExpireScenario(instance);
                    _toRemove.Add(id);
                    continue;
                }
                
                // Tick orchestrator
                if (_orchestrators.TryGetValue(instance.TypeId, out var orchestrator))
                {
                    orchestrator.OnTick(instance, deltaTime, _playerPosition, _world);
                }
                
                // Check if resolved during tick
                if (instance.IsResolved)
                {
                    _toRemove.Add(id);
                }
            }
            
            // Cleanup resolved scenarios
            foreach (var id in _toRemove)
            {
                if (_scenarios.TryGetValue(id, out var instance))
                {
                    CleanupScenario(instance);
                    _scenarios.Remove(id);
                }
            }
        }
        
        public void Shutdown()
        {
            Clear();
            _orchestrators.Clear();
        }
        
        #region Orchestrator Registration
        
        public void RegisterOrchestrator(IScenarioOrchestrator orchestrator)
        {
            _orchestrators[orchestrator.TypeId] = orchestrator;
            SimCoreLogger.Log($"[ScenarioModule] Registered orchestrator: {orchestrator.TypeId}");
        }
        
        public IScenarioOrchestrator GetOrchestrator(string typeId)
        {
            return _orchestrators.TryGetValue(typeId, out var orch) ? orch : null;
        }
        
        public T GetOrchestrator<T>() where T : class, IScenarioOrchestrator
        {
            foreach (var orch in _orchestrators.Values)
            {
                if (orch is T typed) return typed;
            }
            return null;
        }
        
        #endregion
        
        #region Scenario Lifecycle
        
        public ScenarioInstance SpawnScenario(string typeId, Vector3 location, bool isDispatch = false)
        {
            var id = new SimId(_nextId++);
            
            var instance = new ScenarioInstance
            {
                Id = id,
                TypeId = typeId,
                Phase = "spawning",
                Location = location,
                StartTime = Time.time,
                ExpirationTime = 0  // Orchestrator can set this
            };
            
            // Let orchestrator set up the scenario
            if (_orchestrators.TryGetValue(typeId, out var orchestrator))
            {
                if (!orchestrator.OnSpawn(instance, location, isDispatch, _world))
                {
                    SimCoreLogger.Log($"[ScenarioModule] Orchestrator cancelled spawn: {typeId}");
                    return null;
                }
            }
            
            _scenarios[id] = instance;
            
            // Map all participants to this scenario (supports multiple scenarios per entity)
            foreach (var participant in instance.Participants)
            {
                if (participant.IsValid)
                {
                    if (!_entityToScenarios.TryGetValue(participant.EntityId, out var scenarioList))
                    {
                        scenarioList = new List<SimId>();
                        _entityToScenarios[participant.EntityId] = scenarioList;
                    }
                    scenarioList.Add(id);
                    
                    // Tag entity
                    var entity = _world.Entities?.GetEntity(participant.EntityId);
                    entity?.AddTag("has_scenario");
                    entity?.AddTag(new ContentId($"scenario_{typeId}"));
                }
            }
            
            _signalBus?.Publish(new ScenarioSpawnedSignal
            {
                ScenarioId = id,
                TypeId = typeId,
                Location = location,
                ParticipantIds = GetParticipantIds(instance)
            });
            
            SimCoreLogger.Log($"[ScenarioModule] Spawned: {typeId} with {instance.Participants.Count} participants");
            
            return instance;
        }
        
        /// <summary>
        /// Spawn a simple scenario with a single participant and expiration time.
        /// Use this for scenarios that don't need an orchestrator (e.g., jaywalking, drunk).
        /// </summary>
        public ScenarioInstance SpawnSimpleScenario(string typeId, Vector3 location, SimId participantId, string role = "subject", float expirationSeconds = 120f)
        {
            var id = new SimId(_nextId++);
            
            var instance = new ScenarioInstance
            {
                Id = id,
                TypeId = typeId,
                Phase = "active",
                Location = location,
                StartTime = Time.time,
                ExpirationTime = expirationSeconds > 0 ? Time.time + expirationSeconds : 0
            };
            
            // Add the participant
            instance.AddParticipant(participantId, role);
            
            _scenarios[id] = instance;
            
            // Map participant to this scenario
            if (!_entityToScenarios.TryGetValue(participantId, out var scenarioList))
            {
                scenarioList = new List<SimId>();
                _entityToScenarios[participantId] = scenarioList;
            }
            scenarioList.Add(id);
            
            // Tag entity
            var entity = _world.Entities?.GetEntity(participantId);
            entity?.AddTag("has_scenario");
            entity?.AddTag(new ContentId($"scenario_{typeId}"));
            
            _signalBus?.Publish(new ScenarioSpawnedSignal
            {
                ScenarioId = id,
                TypeId = typeId,
                Location = location,
                ParticipantIds = new[] { participantId }
            });
            
            SimCoreLogger.Log($"[ScenarioModule] Spawned simple scenario: {typeId} for entity {participantId.Value}");
            
            return instance;
        }
        
        public void SetPhase(SimId scenarioId, string phase)
        {
            if (!_scenarios.TryGetValue(scenarioId, out var instance)) return;
            if (instance.Phase == phase) return;
            
            string oldPhase = instance.Phase;
            instance.Phase = phase;
            
            if (_orchestrators.TryGetValue(instance.TypeId, out var orchestrator))
            {
                orchestrator.OnPhaseChanged(instance, oldPhase, phase, _world);
            }
            
            _signalBus?.Publish(new ScenarioPhaseChangedSignal
            {
                ScenarioId = scenarioId,
                TypeId = instance.TypeId,
                OldPhase = oldPhase,
                NewPhase = phase
            });
        }
        
        public void EngageScenario(SimId scenarioId, SimId actorId)
        {
            if (!_scenarios.TryGetValue(scenarioId, out var instance)) return;
            if (!instance.IsActive) return;
            
            instance.PlayerEngaged = true;
            instance.EngagedByActorId = actorId;
            
            if (_orchestrators.TryGetValue(instance.TypeId, out var orchestrator))
            {
                orchestrator.OnPlayerEngaged(instance, actorId, _world);
            }
            
            _signalBus?.Publish(new ScenarioEngagedSignal
            {
                ScenarioId = scenarioId,
                TypeId = instance.TypeId,
                ActorId = actorId
            });
        }
        
        public void DisengageScenario(SimId scenarioId)
        {
            if (!_scenarios.TryGetValue(scenarioId, out var instance)) return;
            
            instance.PlayerEngaged = false;
            instance.EngagedByActorId = SimId.Invalid;
        }
        
        public void ResolveScenario(SimId scenarioId, string resolution, bool success = true)
        {
            if (!_scenarios.TryGetValue(scenarioId, out var instance)) return;
            if (instance.IsResolved) return;
            
            instance.IsResolved = true;
            instance.Resolution = resolution;
            
            if (_orchestrators.TryGetValue(instance.TypeId, out var orchestrator))
            {
                orchestrator.OnResolve(instance, resolution, _world);
            }
            
            _signalBus?.Publish(new ScenarioResolvedSignal
            {
                ScenarioId = scenarioId,
                TypeId = instance.TypeId,
                Resolution = resolution,
                Success = success
            });
            
            SimCoreLogger.Log($"[ScenarioModule] Resolved: {instance.TypeId} - {resolution}");
        }
        
        private void ExpireScenario(ScenarioInstance instance)
        {
            instance.IsResolved = true;
            instance.Resolution = "expired";
            
            if (_orchestrators.TryGetValue(instance.TypeId, out var orchestrator))
            {
                orchestrator.OnExpire(instance, _world);
            }
            
            _signalBus?.Publish(new ScenarioExpiredSignal
            {
                ScenarioId = instance.Id,
                TypeId = instance.TypeId
            });
            
            SimCoreLogger.Log($"[ScenarioModule] Expired: {instance.TypeId}");
        }
        
        private void CleanupScenario(ScenarioInstance instance)
        {
            // Remove entity mappings
            foreach (var participant in instance.Participants)
            {
                if (participant.IsValid)
                {
                    // Remove this scenario from entity's list
                    if (_entityToScenarios.TryGetValue(participant.EntityId, out var scenarioList))
                    {
                        scenarioList.Remove(instance.Id);
                        
                        // If no more scenarios, remove the entry and tags
                        if (scenarioList.Count == 0)
                        {
                            _entityToScenarios.Remove(participant.EntityId);
                            
                            var entity = _world.Entities?.GetEntity(participant.EntityId);
                            if (entity != null)
                            {
                                entity.RemoveTag("has_scenario");
                            }
                        }
                    }
                    
                    // Always remove the specific scenario tag
                    var ent = _world.Entities?.GetEntity(participant.EntityId);
                    ent?.RemoveTag(new ContentId($"scenario_{instance.TypeId}"));
                }
            }
            
            // Notify orchestrator
            if (_orchestrators.TryGetValue(instance.TypeId, out var orchestrator))
            {
                orchestrator.OnCleanup(instance, _world);
            }
        }
        
        #endregion
        
        #region Queries
        
        public ScenarioInstance GetScenario(SimId scenarioId)
        {
            return _scenarios.TryGetValue(scenarioId, out var instance) ? instance : null;
        }
        
        public ScenarioInstance GetScenarioForEntity(SimId entityId)
        {
            // Returns first active scenario for backwards compatibility
            if (_entityToScenarios.TryGetValue(entityId, out var scenarioIds))
            {
                foreach (var scenarioId in scenarioIds)
                {
                    var scenario = GetScenario(scenarioId);
                    if (scenario != null && scenario.IsActive)
                        return scenario;
                }
            }
            return null;
        }
        
        public ScenarioInstance[] GetScenariosForEntity(SimId entityId)
        {
            var result = new List<ScenarioInstance>();
            if (_entityToScenarios.TryGetValue(entityId, out var scenarioIds))
            {
                foreach (var scenarioId in scenarioIds)
                {
                    var scenario = GetScenario(scenarioId);
                    if (scenario != null && scenario.IsActive)
                        result.Add(scenario);
                }
            }
            return result.ToArray();
        }
        
        public ScenarioInstance[] GetActiveScenarios()
        {
            var result = new List<ScenarioInstance>();
            foreach (var instance in _scenarios.Values)
            {
                if (instance.IsActive) result.Add(instance);
            }
            return result.ToArray();
        }
        
        public ScenarioInstance[] GetScenariosByType(string typeId)
        {
            var result = new List<ScenarioInstance>();
            foreach (var instance in _scenarios.Values)
            {
                if (instance.IsActive && instance.TypeId == typeId)
                    result.Add(instance);
            }
            return result.ToArray();
        }
        
        public void Clear()
        {
            foreach (var instance in _scenarios.Values)
            {
                CleanupScenario(instance);
            }
            _scenarios.Clear();
            _entityToScenarios.Clear();
            _nextId = 1;
        }
        
        #endregion
        
        #region Helpers
        
        private SimId[] GetParticipantIds(ScenarioInstance instance)
        {
            var ids = new SimId[instance.Participants.Count];
            for (int i = 0; i < instance.Participants.Count; i++)
            {
                ids[i] = instance.Participants[i].EntityId;
            }
            return ids;
        }
        
        #endregion
    }
    
    #endregion
}
