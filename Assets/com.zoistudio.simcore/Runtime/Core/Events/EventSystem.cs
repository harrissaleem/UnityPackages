// SimCore - Event System
// Multi-stage events and scenarios

using System;
using System.Collections.Generic;
using SimCore.Effects;
using SimCore.Signals;

namespace SimCore.Events
{
    /// <summary>
    /// Definition of an event stage
    /// </summary>
    [Serializable]
    public class EventStageDef
    {
        public ContentId StageId;
        public string DisplayName;
        public string Description;
        
        // Effects when entering this stage
        public List<Effect> EnterEffects = new();
        
        // Effects when exiting this stage
        public List<Effect> ExitEffects = new();
        
        // Auto-advance conditions (any of these can advance)
        public List<Condition> AdvanceConditions = new();
        
        // Auto-advance after duration (0 = no auto-advance)
        public float DurationSeconds;
        
        // If true, this is the final stage and completes the event
        public bool IsFinal;
    }
    
    /// <summary>
    /// Definition of an event
    /// </summary>
    [Serializable]
    public class EventDef
    {
        public ContentId Id;
        public string DisplayName;
        public string Description;
        
        // Stages (ordered)
        public List<EventStageDef> Stages = new();
        
        // Conditions required to spawn this event
        public List<Condition> SpawnConditions = new();
        
        // Effects when event starts
        public List<Effect> StartEffects = new();
        
        // Effects when event ends (completed or cancelled)
        public List<Effect> EndEffects = new();
        
        // Priority for event queue ordering
        public Priority Priority = Priority.Normal;
        
        // Can only have one instance at a time
        public bool Unique = true;
        
        // Tags to apply to target entity during event
        public List<ContentId> ApplyTags = new();
    }
    
    /// <summary>
    /// Active instance of an event
    /// </summary>
    [Serializable]
    public class EventInstance
    {
        public SimId InstanceId;
        public ContentId EventDefId;
        public SimId TargetEntityId;
        public int CurrentStage;
        public SimTime StageStartTime;
        public bool IsActive = true;
        public Dictionary<string, object> Parameters = new();
        
        public EventStageDef GetCurrentStageDef(EventDef def)
        {
            if (CurrentStage >= 0 && CurrentStage < def.Stages.Count)
                return def.Stages[CurrentStage];
            return null;
        }
    }
    
    /// <summary>
    /// Event snapshot for persistence
    /// </summary>
    [Serializable]
    public class EventSnapshot
    {
        public SimId InstanceId;
        public ContentId EventDefId;
        public SimId TargetEntityId;
        public int CurrentStage;
        public float StageStartTimeSeconds;
        public Dictionary<string, object> Parameters;
    }
    
    /// <summary>
    /// Manages all events in the simulation
    /// </summary>
    public class EventManager
    {
        private readonly Dictionary<ContentId, EventDef> _eventDefs = new();
        private readonly List<EventInstance> _activeEvents = new();
        private readonly SignalBus _signalBus;
        private readonly Func<SimWorld> _worldGetter;
        private int _nextInstanceId = 1;
        
        public EventManager(SignalBus signalBus, Func<SimWorld> worldGetter)
        {
            _signalBus = signalBus;
            _worldGetter = worldGetter;
        }
        
        /// <summary>
        /// Register an event definition
        /// </summary>
        public void RegisterEvent(EventDef def)
        {
            _eventDefs[def.Id] = def;
        }
        
        /// <summary>
        /// Get event definition
        /// </summary>
        public EventDef GetEventDef(ContentId eventDefId)
        {
            return _eventDefs.TryGetValue(eventDefId, out var def) ? def : null;
        }
        
        /// <summary>
        /// Spawn a new event instance
        /// </summary>
        public EventInstance SpawnEvent(ContentId eventDefId, SimId targetEntityId = default, 
            Dictionary<string, object> parameters = null)
        {
            if (!_eventDefs.TryGetValue(eventDefId, out var def))
            {
                SimCoreLogger.LogWarning($"Unknown event: {eventDefId}");
                return null;
            }
            
            // Check unique constraint
            if (def.Unique && IsEventActive(eventDefId))
            {
                return null;
            }
            
            var world = _worldGetter();
            
            // Check spawn conditions
            var condCtx = new ConditionContext(world)
            {
                ActorId = targetEntityId,
                TargetId = targetEntityId
            };
            
            foreach (var cond in def.SpawnConditions)
            {
                if (!cond.Evaluate(condCtx))
                {
                    return null;
                }
            }
            
            var instance = new EventInstance
            {
                InstanceId = new SimId(_nextInstanceId++),
                EventDefId = eventDefId,
                TargetEntityId = targetEntityId,
                CurrentStage = 0,
                StageStartTime = world.CurrentTime,
                Parameters = parameters ?? new Dictionary<string, object>()
            };
            
            _activeEvents.Add(instance);
            
            // Apply tags to target
            if (targetEntityId.IsValid)
            {
                var target = world.Entities.GetEntity(targetEntityId);
                foreach (var tag in def.ApplyTags)
                {
                    target?.AddTag(tag);
                }
            }
            
            // Apply start effects
            var effectCtx = new EffectContext(world)
            {
                ActorId = targetEntityId,
                TargetId = targetEntityId
            };
            
            foreach (var effect in def.StartEffects)
            {
                effect.Apply(effectCtx);
            }
            
            // Apply first stage enter effects
            if (def.Stages.Count > 0)
            {
                foreach (var effect in def.Stages[0].EnterEffects)
                {
                    effect.Apply(effectCtx);
                }
            }
            
            _signalBus.Publish(new EventStartedSignal
            {
                EventInstanceId = instance.InstanceId,
                EventDefId = eventDefId
            });
            
            return instance;
        }
        
        /// <summary>
        /// Advance an event to the next stage
        /// </summary>
        public bool AdvanceStage(SimId instanceId)
        {
            var instance = GetEvent(instanceId);
            if (instance == null || !instance.IsActive) return false;
            
            var def = GetEventDef(instance.EventDefId);
            if (def == null) return false;
            
            var world = _worldGetter();
            var effectCtx = new EffectContext(world)
            {
                ActorId = instance.TargetEntityId,
                TargetId = instance.TargetEntityId
            };
            
            // Exit current stage
            var currentStageDef = instance.GetCurrentStageDef(def);
            if (currentStageDef != null)
            {
                foreach (var effect in currentStageDef.ExitEffects)
                {
                    effect.Apply(effectCtx);
                }
            }
            
            int oldStage = instance.CurrentStage;
            instance.CurrentStage++;
            instance.StageStartTime = world.CurrentTime;
            
            // Check if event completed
            if (instance.CurrentStage >= def.Stages.Count)
            {
                EndEvent(instanceId, true);
                return true;
            }
            
            // Enter new stage
            var newStageDef = instance.GetCurrentStageDef(def);
            if (newStageDef != null)
            {
                foreach (var effect in newStageDef.EnterEffects)
                {
                    effect.Apply(effectCtx);
                }
                
                // Check if final
                if (newStageDef.IsFinal)
                {
                    EndEvent(instanceId, true);
                    return true;
                }
            }
            
            _signalBus.Publish(new EventStageChangedSignal
            {
                EventInstanceId = instanceId,
                EventDefId = instance.EventDefId,
                OldStage = oldStage,
                NewStage = instance.CurrentStage
            });
            
            return true;
        }
        
        /// <summary>
        /// End an event
        /// </summary>
        public void EndEvent(SimId instanceId, bool completed)
        {
            var instance = GetEvent(instanceId);
            if (instance == null || !instance.IsActive) return;
            
            instance.IsActive = false;
            
            var def = GetEventDef(instance.EventDefId);
            if (def == null) return;
            
            var world = _worldGetter();
            var effectCtx = new EffectContext(world)
            {
                ActorId = instance.TargetEntityId,
                TargetId = instance.TargetEntityId
            };
            
            // Exit current stage
            var currentStageDef = instance.GetCurrentStageDef(def);
            if (currentStageDef != null)
            {
                foreach (var effect in currentStageDef.ExitEffects)
                {
                    effect.Apply(effectCtx);
                }
            }
            
            // Apply end effects
            foreach (var effect in def.EndEffects)
            {
                effect.Apply(effectCtx);
            }
            
            // Remove tags from target
            if (instance.TargetEntityId.IsValid)
            {
                var target = world.Entities.GetEntity(instance.TargetEntityId);
                foreach (var tag in def.ApplyTags)
                {
                    target?.RemoveTag(tag);
                }
            }
            
            _signalBus.Publish(new EventEndedSignal
            {
                EventInstanceId = instanceId,
                EventDefId = instance.EventDefId,
                Completed = completed
            });
        }
        
        /// <summary>
        /// Get an event instance
        /// </summary>
        public EventInstance GetEvent(SimId instanceId)
        {
            foreach (var evt in _activeEvents)
            {
                if (evt.InstanceId == instanceId)
                    return evt;
            }
            return null;
        }
        
        /// <summary>
        /// Get active events by definition
        /// </summary>
        public IEnumerable<EventInstance> GetEventsByDef(ContentId eventDefId)
        {
            foreach (var evt in _activeEvents)
            {
                if (evt.EventDefId == eventDefId && evt.IsActive)
                    yield return evt;
            }
        }
        
        /// <summary>
        /// Get active events for an entity
        /// </summary>
        public IEnumerable<EventInstance> GetEventsForEntity(SimId entityId)
        {
            foreach (var evt in _activeEvents)
            {
                if (evt.TargetEntityId == entityId && evt.IsActive)
                    yield return evt;
            }
        }
        
        /// <summary>
        /// Check if an event type is currently active
        /// </summary>
        public bool IsEventActive(ContentId eventDefId)
        {
            foreach (var evt in _activeEvents)
            {
                if (evt.EventDefId == eventDefId && evt.IsActive)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get all active events
        /// </summary>
        public IReadOnlyList<EventInstance> GetAllActiveEvents() => _activeEvents.FindAll(e => e.IsActive);
        
        /// <summary>
        /// Tick events - check auto-advance conditions and durations
        /// </summary>
        public void Tick()
        {
            var world = _worldGetter();
            var currentTime = world.CurrentTime;
            var toAdvance = new List<SimId>();
            
            foreach (var evt in _activeEvents)
            {
                if (!evt.IsActive) continue;
                
                var def = GetEventDef(evt.EventDefId);
                if (def == null) continue;
                
                var stageDef = evt.GetCurrentStageDef(def);
                if (stageDef == null) continue;
                
                bool shouldAdvance = false;
                
                // Check duration
                if (stageDef.DurationSeconds > 0)
                {
                    float elapsed = (currentTime - evt.StageStartTime).Seconds;
                    if (elapsed >= stageDef.DurationSeconds)
                    {
                        shouldAdvance = true;
                    }
                }
                
                // Check conditions
                if (!shouldAdvance && stageDef.AdvanceConditions.Count > 0)
                {
                    var condCtx = new ConditionContext(world)
                    {
                        ActorId = evt.TargetEntityId,
                        TargetId = evt.TargetEntityId,
                        EventInstanceId = evt.InstanceId
                    };
                    
                    foreach (var cond in stageDef.AdvanceConditions)
                    {
                        if (cond.Evaluate(condCtx))
                        {
                            shouldAdvance = true;
                            break;
                        }
                    }
                }
                
                if (shouldAdvance)
                {
                    toAdvance.Add(evt.InstanceId);
                }
            }
            
            foreach (var id in toAdvance)
            {
                AdvanceStage(id);
            }
            
            // Clean up ended events
            _activeEvents.RemoveAll(e => !e.IsActive);
        }
        
        /// <summary>
        /// Create snapshots for persistence
        /// </summary>
        public List<EventSnapshot> CreateSnapshots()
        {
            var snapshots = new List<EventSnapshot>();
            foreach (var evt in _activeEvents)
            {
                if (!evt.IsActive) continue;
                
                snapshots.Add(new EventSnapshot
                {
                    InstanceId = evt.InstanceId,
                    EventDefId = evt.EventDefId,
                    TargetEntityId = evt.TargetEntityId,
                    CurrentStage = evt.CurrentStage,
                    StageStartTimeSeconds = evt.StageStartTime.Seconds,
                    Parameters = new Dictionary<string, object>(evt.Parameters)
                });
            }
            return snapshots;
        }
        
        /// <summary>
        /// Restore from snapshots
        /// </summary>
        public void RestoreFromSnapshots(List<EventSnapshot> snapshots)
        {
            _activeEvents.Clear();
            _nextInstanceId = 1;
            
            foreach (var snapshot in snapshots)
            {
                var instance = new EventInstance
                {
                    InstanceId = snapshot.InstanceId,
                    EventDefId = snapshot.EventDefId,
                    TargetEntityId = snapshot.TargetEntityId,
                    CurrentStage = snapshot.CurrentStage,
                    StageStartTime = SimTime.FromSeconds(snapshot.StageStartTimeSeconds),
                    Parameters = snapshot.Parameters ?? new Dictionary<string, object>(),
                    IsActive = true
                };
                
                _activeEvents.Add(instance);
                
                if (snapshot.InstanceId.Value >= _nextInstanceId)
                    _nextInstanceId = snapshot.InstanceId.Value + 1;
            }
        }
        
        /// <summary>
        /// Clear all events
        /// </summary>
        public void Clear()
        {
            _activeEvents.Clear();
            _nextInstanceId = 1;
        }
    }
}

