// SimCore - Rules Engine
// Condition → Effects evaluation

using System;
using System.Collections.Generic;
using SimCore.Effects;
using SimCore.Signals;
using SimCore.Actions;

namespace SimCore.Rules
{
    /// <summary>
    /// Trigger types for rules
    /// </summary>
    public enum RuleTrigger
    {
        Manual,         // Only triggered by code
        Periodic,       // Checked every tick
        OnActionComplete, // After action completes
        OnAreaEnter,    // When entity enters area
        OnAreaExit,     // When entity exits area
        OnEventStart,   // When event starts
        OnEventStageChange, // When event stage changes
        OnTimerComplete, // When timer completes
        OnTagAdded,     // When tag is added
        OnTagRemoved,   // When tag is removed
        OnStatChanged   // When stat changes
    }
    
    /// <summary>
    /// Rule definition
    /// </summary>
    [Serializable]
    public class RuleDef
    {
        public ContentId Id;
        public string Description;
        
        // When to check this rule
        public RuleTrigger Trigger = RuleTrigger.Periodic;
        
        // Filter for trigger (e.g., specific action ID, area ID)
        public ContentId TriggerFilter;
        
        // Conditions to evaluate
        public List<Condition> Conditions = new();
        
        // Effects to apply when conditions are met
        public List<Effect> Effects = new();
        
        // Priority for ordering
        public Priority Priority = Priority.Normal;
        
        // Maximum times this rule can fire (-1 = unlimited)
        public int MaxFires = -1;
        
        // Cooldown between fires
        public float CooldownSeconds;
        
        // If true, rule is disabled after firing MaxFires times
        public bool Enabled = true;
    }
    
    /// <summary>
    /// Runtime state for a rule
    /// </summary>
    public class RuleState
    {
        public ContentId RuleId;
        public int FireCount;
        public SimTime LastFireTime;
        public bool Disabled;
    }
    
    /// <summary>
    /// Context for rule evaluation
    /// </summary>
    public class RuleContext
    {
        public SimId ActorId;
        public SimId TargetId;
        public ContentId ActionId;
        public ContentId AreaId;
        public SimId EventInstanceId;
        public ContentId EventDefId;
        public ContentId TimerId;
        public ContentId TagId;
        public ContentId StatId;
    }
    
    /// <summary>
    /// Rules engine - evaluates rules and applies effects
    /// </summary>
    public class RulesEngine
    {
        private readonly List<RuleDef> _rules = new();
        private readonly Dictionary<ContentId, RuleState> _ruleStates = new();
        private readonly Dictionary<RuleTrigger, List<RuleDef>> _rulesByTrigger = new();
        private readonly SignalBus _signalBus;
        private readonly Func<SimWorld> _worldGetter;
        
        public RulesEngine(SignalBus signalBus, Func<SimWorld> worldGetter)
        {
            _signalBus = signalBus;
            _worldGetter = worldGetter;
            
            // Initialize trigger buckets
            foreach (RuleTrigger trigger in Enum.GetValues(typeof(RuleTrigger)))
            {
                _rulesByTrigger[trigger] = new List<RuleDef>();
            }
            
            // Subscribe to signals for triggered rules
            _signalBus.Subscribe<ActionCompletedSignal>(OnActionCompleted);
            _signalBus.Subscribe<AreaEnteredSignal>(OnAreaEntered);
            _signalBus.Subscribe<AreaExitedSignal>(OnAreaExited);
            _signalBus.Subscribe<EventStartedSignal>(OnEventStarted);
            _signalBus.Subscribe<EventStageChangedSignal>(OnEventStageChanged);
            _signalBus.Subscribe<TimerCompletedSignal>(OnTimerCompleted);
            _signalBus.Subscribe<TagChangedSignal>(OnTagChanged);
            _signalBus.Subscribe<StatChangedSignal>(OnStatChanged);
        }
        
        /// <summary>
        /// Register a rule
        /// </summary>
        public void RegisterRule(RuleDef rule)
        {
            _rules.Add(rule);
            _rulesByTrigger[rule.Trigger].Add(rule);
            _rulesByTrigger[rule.Trigger].Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            _ruleStates[rule.Id] = new RuleState { RuleId = rule.Id };
        }
        
        /// <summary>
        /// Register multiple rules
        /// </summary>
        public void RegisterRules(IEnumerable<RuleDef> rules)
        {
            foreach (var rule in rules)
            {
                RegisterRule(rule);
            }
        }
        
        /// <summary>
        /// Tick periodic rules
        /// </summary>
        public void Tick()
        {
            EvaluateRules(RuleTrigger.Periodic, new RuleContext());
        }
        
        /// <summary>
        /// Evaluate rules for a trigger
        /// </summary>
        public void EvaluateRules(RuleTrigger trigger, RuleContext ruleCtx)
        {
            var world = _worldGetter();
            var rules = _rulesByTrigger[trigger];
            
            foreach (var rule in rules)
            {
                if (!rule.Enabled) continue;
                
                var state = _ruleStates[rule.Id];
                if (state.Disabled) continue;
                
                // Check filter
                if (rule.TriggerFilter.IsValid)
                {
                    bool matches = trigger switch
                    {
                        RuleTrigger.OnActionComplete => rule.TriggerFilter == ruleCtx.ActionId,
                        RuleTrigger.OnAreaEnter or RuleTrigger.OnAreaExit => rule.TriggerFilter == ruleCtx.AreaId,
                        RuleTrigger.OnEventStart or RuleTrigger.OnEventStageChange => rule.TriggerFilter == ruleCtx.EventDefId,
                        RuleTrigger.OnTimerComplete => rule.TriggerFilter == ruleCtx.TimerId,
                        RuleTrigger.OnTagAdded or RuleTrigger.OnTagRemoved => rule.TriggerFilter == ruleCtx.TagId,
                        RuleTrigger.OnStatChanged => rule.TriggerFilter == ruleCtx.StatId,
                        _ => true
                    };
                    
                    if (!matches) continue;
                }
                
                // Check cooldown
                if (rule.CooldownSeconds > 0)
                {
                    float elapsed = (world.CurrentTime - state.LastFireTime).Seconds;
                    if (elapsed < rule.CooldownSeconds) continue;
                }
                
                // Check max fires
                if (rule.MaxFires >= 0 && state.FireCount >= rule.MaxFires)
                {
                    state.Disabled = true;
                    continue;
                }
                
                // Evaluate conditions
                var condCtx = new ConditionContext(world)
                {
                    ActorId = ruleCtx.ActorId,
                    TargetId = ruleCtx.TargetId,
                    EventInstanceId = ruleCtx.EventInstanceId,
                    AreaId = ruleCtx.AreaId
                };
                
                bool allConditionsMet = true;
                foreach (var cond in rule.Conditions)
                {
                    if (!cond.Evaluate(condCtx))
                    {
                        allConditionsMet = false;
                        break;
                    }
                }
                
                if (!allConditionsMet) continue;
                
                // Apply effects
                var effectCtx = new EffectContext(world)
                {
                    ActorId = ruleCtx.ActorId,
                    TargetId = ruleCtx.TargetId
                };
                
                foreach (var effect in rule.Effects)
                {
                    effect.Apply(effectCtx);
                }
                
                // Update state
                state.FireCount++;
                state.LastFireTime = world.CurrentTime;
            }
        }
        
        /// <summary>
        /// Reset rule state
        /// </summary>
        public void ResetRuleState(ContentId ruleId)
        {
            if (_ruleStates.TryGetValue(ruleId, out var state))
            {
                state.FireCount = 0;
                state.Disabled = false;
                state.LastFireTime = SimTime.Zero;
            }
        }
        
        /// <summary>
        /// Reset all rule states
        /// </summary>
        public void ResetAllStates()
        {
            foreach (var state in _ruleStates.Values)
            {
                state.FireCount = 0;
                state.Disabled = false;
                state.LastFireTime = SimTime.Zero;
            }
        }
        
        // Signal handlers
        private void OnActionCompleted(ActionCompletedSignal signal)
        {
            EvaluateRules(RuleTrigger.OnActionComplete, new RuleContext
            {
                ActorId = signal.ActorId,
                TargetId = signal.TargetId,
                ActionId = signal.ActionId
            });
        }
        
        private void OnAreaEntered(AreaEnteredSignal signal)
        {
            EvaluateRules(RuleTrigger.OnAreaEnter, new RuleContext
            {
                ActorId = signal.EntityId,
                AreaId = signal.AreaId
            });
        }
        
        private void OnAreaExited(AreaExitedSignal signal)
        {
            EvaluateRules(RuleTrigger.OnAreaExit, new RuleContext
            {
                ActorId = signal.EntityId,
                AreaId = signal.AreaId
            });
        }
        
        private void OnEventStarted(EventStartedSignal signal)
        {
            EvaluateRules(RuleTrigger.OnEventStart, new RuleContext
            {
                EventInstanceId = signal.EventInstanceId,
                EventDefId = signal.EventDefId
            });
        }
        
        private void OnEventStageChanged(EventStageChangedSignal signal)
        {
            EvaluateRules(RuleTrigger.OnEventStageChange, new RuleContext
            {
                EventInstanceId = signal.EventInstanceId,
                EventDefId = signal.EventDefId
            });
        }
        
        private void OnTimerCompleted(TimerCompletedSignal signal)
        {
            EvaluateRules(RuleTrigger.OnTimerComplete, new RuleContext
            {
                TimerId = signal.TimerId,
                ActorId = signal.OwnerId
            });
        }
        
        private void OnTagChanged(TagChangedSignal signal)
        {
            var trigger = signal.Added ? RuleTrigger.OnTagAdded : RuleTrigger.OnTagRemoved;
            EvaluateRules(trigger, new RuleContext
            {
                ActorId = signal.EntityId,
                TagId = signal.TagId
            });
        }
        
        private void OnStatChanged(StatChangedSignal signal)
        {
            EvaluateRules(RuleTrigger.OnStatChanged, new RuleContext
            {
                ActorId = signal.EntityId,
                StatId = signal.StatId
            });
        }
        
        /// <summary>
        /// Clear all rules
        /// </summary>
        public void Clear()
        {
            _rules.Clear();
            _ruleStates.Clear();
            foreach (var list in _rulesByTrigger.Values)
            {
                list.Clear();
            }
        }
    }
}

