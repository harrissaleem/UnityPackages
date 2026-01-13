// SimCore - Action Pipeline
// Central processing for all actions in the simulation

using System;
using System.Collections.Generic;
using SimCore.Effects;
using SimCore.Signals;

namespace SimCore.Actions
{
    /// <summary>
    /// Request to perform an action
    /// </summary>
    [Serializable]
    public class ActionRequest
    {
        public SimId ActorId;
        public ContentId ActionId;
        public SimId TargetId;
        public ActionContext Context;
        
        public ActionRequest(SimId actorId, ContentId actionId, SimId targetId = default)
        {
            ActorId = actorId;
            ActionId = actionId;
            TargetId = targetId;
            Context = new ActionContext();
        }
    }
    
    /// <summary>
    /// Result of processing an action
    /// </summary>
    public class ActionResponse
    {
        public ActionResult Result;
        public string BlockReason;
        public List<Effect> ProducedEffects = new();
        public ActionContext Context;
    }
    
    /// <summary>
    /// Definition of an action
    /// </summary>
    [Serializable]
    public class ActionDef
    {
        public ContentId Id;
        public string DisplayName;
        public string Description;
        
        // Validation conditions
        public List<Condition> RequiredConditions = new();
        
        // Default effects when no custom handler
        public List<Effect> DefaultEffects = new();
        
        // Cost (stats to deduct)
        public Dictionary<ContentId, float> StatCosts = new();
        
        // Cooldown
        public float CooldownSeconds;
        
        // If true, requires a target
        public bool RequiresTarget;
        
        // Tags required on actor
        public List<ContentId> RequiredActorTags = new();
        
        // Tags required on target
        public List<ContentId> RequiredTargetTags = new();
        
        // Max range to target
        public float MaxRange = -1; // -1 = no range check
    }
    
    /// <summary>
    /// Interface for action validators
    /// </summary>
    public interface IActionValidator
    {
        /// <summary>
        /// Validate an action request
        /// </summary>
        /// <param name="request">The action request</param>
        /// <param name="def">The action definition</param>
        /// <param name="world">The simulation world</param>
        /// <param name="reason">Output reason if blocked</param>
        /// <returns>True if valid, false if blocked</returns>
        bool Validate(ActionRequest request, ActionDef def, SimWorld world, out string reason);
        
        /// <summary>
        /// Priority (higher runs first)
        /// </summary>
        int Priority => 0;
    }
    
    /// <summary>
    /// Interface for action handlers (custom per-game logic)
    /// </summary>
    public interface IActionHandler
    {
        /// <summary>
        /// Check if this handler can process the action
        /// </summary>
        bool CanHandle(ContentId actionId);
        
        /// <summary>
        /// Execute the action and return effects
        /// </summary>
        List<Effect> Execute(ActionRequest request, ActionDef def, SimWorld world);
    }
    
    /// <summary>
    /// Central action processing pipeline
    /// </summary>
    public class ActionPipeline
    {
        private readonly SimWorld _world;
        private readonly Dictionary<ContentId, ActionDef> _actionDefs = new();
        private readonly List<IActionValidator> _validators = new();
        private readonly List<IActionHandler> _handlers = new();
        private readonly Dictionary<(SimId, ContentId), SimTime> _cooldowns = new();
        
        public ActionPipeline(SimWorld world)
        {
            _world = world;
            
            // Add default validators
            _validators.Add(new DefaultActionValidator());
        }
        
        /// <summary>
        /// Register an action definition
        /// </summary>
        public void RegisterAction(ActionDef def)
        {
            _actionDefs[def.Id] = def;
        }
        
        /// <summary>
        /// Register multiple action definitions
        /// </summary>
        public void RegisterActions(IEnumerable<ActionDef> defs)
        {
            foreach (var def in defs)
            {
                RegisterAction(def);
            }
        }
        
        /// <summary>
        /// Add a custom validator
        /// </summary>
        public void AddValidator(IActionValidator validator)
        {
            _validators.Add(validator);
            _validators.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
        
        /// <summary>
        /// Add a custom handler
        /// </summary>
        public void AddHandler(IActionHandler handler)
        {
            _handlers.Add(handler);
        }
        
        /// <summary>
        /// Get an action definition
        /// </summary>
        public ActionDef GetActionDef(ContentId actionId)
        {
            return _actionDefs.TryGetValue(actionId, out var def) ? def : null;
        }
        
        /// <summary>
        /// Process an action request
        /// </summary>
        public ActionResponse ProcessAction(ActionRequest request)
        {
            var response = new ActionResponse
            {
                Context = request.Context ?? new ActionContext()
            };
            
            // Get action definition
            if (!_actionDefs.TryGetValue(request.ActionId, out var def))
            {
                response.Result = ActionResult.Failed;
                response.BlockReason = $"Unknown action: {request.ActionId}";
                return response;
            }
            
            // Run validators
            foreach (var validator in _validators)
            {
                if (!validator.Validate(request, def, _world, out var reason))
                {
                    response.Result = ActionResult.Blocked;
                    response.BlockReason = reason;
                    
                    _world.SignalBus.Publish(new ActionBlockedSignal
                    {
                        ActorId = request.ActorId,
                        ActionId = request.ActionId,
                        TargetId = request.TargetId,
                        Reason = reason
                    });
                    
                    return response;
                }
            }
            
            // Check cooldown
            var cooldownKey = (request.ActorId, request.ActionId);
            if (_cooldowns.TryGetValue(cooldownKey, out var cooldownEnd))
            {
                if (_world.CurrentTime < cooldownEnd)
                {
                    response.Result = ActionResult.Blocked;
                    response.BlockReason = "Action on cooldown";
                    return response;
                }
            }
            
            // Find handler or use default effects
            List<Effect> effects = null;
            foreach (var handler in _handlers)
            {
                if (handler.CanHandle(request.ActionId))
                {
                    effects = handler.Execute(request, def, _world);
                    break;
                }
            }
            
            // Use default effects if no handler
            effects ??= new List<Effect>(def.DefaultEffects);
            
            // Apply stat costs as effects
            foreach (var cost in def.StatCosts)
            {
                effects.Insert(0, new ModifyStatEffect(request.ActorId, cost.Key, -cost.Value));
            }
            
            // Apply effects
            var effectCtx = new EffectContext(_world)
            {
                ActorId = request.ActorId,
                TargetId = request.TargetId,
                ActionContext = response.Context
            };
            
            foreach (var effect in effects)
            {
                effect.Apply(effectCtx);
            }
            
            response.ProducedEffects = effects;
            response.Result = ActionResult.Success;
            
            // Set cooldown
            if (def.CooldownSeconds > 0)
            {
                _cooldowns[cooldownKey] = _world.CurrentTime + SimTime.FromSeconds(def.CooldownSeconds);
            }
            
            // Emit signal
            _world.SignalBus.Publish(new ActionCompletedSignal
            {
                ActorId = request.ActorId,
                ActionId = request.ActionId,
                TargetId = request.TargetId,
                Result = ActionResult.Success,
                Context = response.Context
            });
            
            return response;
        }
        
        /// <summary>
        /// Check if an action can be performed (without executing)
        /// </summary>
        public bool CanPerformAction(ActionRequest request, out string reason)
        {
            reason = null;
            
            if (!_actionDefs.TryGetValue(request.ActionId, out var def))
            {
                reason = $"Unknown action: {request.ActionId}";
                return false;
            }
            
            foreach (var validator in _validators)
            {
                if (!validator.Validate(request, def, _world, out reason))
                {
                    return false;
                }
            }
            
            // Check cooldown
            var cooldownKey = (request.ActorId, request.ActionId);
            if (_cooldowns.TryGetValue(cooldownKey, out var cooldownEnd))
            {
                if (_world.CurrentTime < cooldownEnd)
                {
                    reason = "Action on cooldown";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Get available actions for an actor
        /// </summary>
        public List<ContentId> GetAvailableActions(SimId actorId, SimId targetId = default)
        {
            var available = new List<ContentId>();
            
            foreach (var def in _actionDefs.Values)
            {
                var request = new ActionRequest(actorId, def.Id, targetId);
                if (CanPerformAction(request, out _))
                {
                    available.Add(def.Id);
                }
            }
            
            return available;
        }
        
        /// <summary>
        /// Clear cooldowns (for testing/reset)
        /// </summary>
        public void ClearCooldowns() => _cooldowns.Clear();
    }
    
    /// <summary>
    /// Default validator that checks action definition requirements
    /// </summary>
    public class DefaultActionValidator : IActionValidator
    {
        public int Priority => 100;
        
        public bool Validate(ActionRequest request, ActionDef def, SimWorld world, out string reason)
        {
            reason = null;
            
            var actor = world.Entities.GetEntity(request.ActorId);
            if (actor == null)
            {
                reason = "Invalid actor";
                return false;
            }
            
            // Check target requirement
            if (def.RequiresTarget && !request.TargetId.IsValid)
            {
                reason = "Action requires a target";
                return false;
            }
            
            // Check target exists
            if (request.TargetId.IsValid && world.Entities.GetEntity(request.TargetId) == null)
            {
                reason = "Invalid target";
                return false;
            }
            
            // Check actor tags
            foreach (var tag in def.RequiredActorTags)
            {
                if (!actor.HasTag(tag))
                {
                    reason = $"Missing required tag: {tag}";
                    return false;
                }
            }
            
            // Check target tags
            if (request.TargetId.IsValid && def.RequiredTargetTags.Count > 0)
            {
                var target = world.Entities.GetEntity(request.TargetId);
                foreach (var tag in def.RequiredTargetTags)
                {
                    if (!target.HasTag(tag))
                    {
                        reason = $"Target missing required tag: {tag}";
                        return false;
                    }
                }
            }
            
            // Check range
            if (def.MaxRange >= 0 && request.TargetId.IsValid)
            {
                float distance = world.Partition.GetDistance(request.ActorId, request.TargetId);
                if (distance > def.MaxRange)
                {
                    reason = "Target out of range";
                    return false;
                }
            }
            
            // Check stat costs
            foreach (var cost in def.StatCosts)
            {
                if (actor.GetStat(cost.Key) < cost.Value)
                {
                    reason = $"Insufficient {cost.Key}";
                    return false;
                }
            }
            
            // Check custom conditions
            var condCtx = new ConditionContext(world)
            {
                ActorId = request.ActorId,
                TargetId = request.TargetId
            };
            
            foreach (var condition in def.RequiredConditions)
            {
                if (!condition.Evaluate(condCtx))
                {
                    reason = $"Condition failed: {condition.GetDescription()}";
                    return false;
                }
            }
            
            return true;
        }
    }
}

