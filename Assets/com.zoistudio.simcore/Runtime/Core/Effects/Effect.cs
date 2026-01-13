// SimCore - Effects System
// Atomic world operations that can be serialized and composed

using System;
using System.Collections.Generic;

namespace SimCore.Effects
{
    /// <summary>
    /// Base class for all effects
    /// Effects are atomic operations that modify the world state
    /// </summary>
    [Serializable]
    public abstract class Effect
    {
        /// <summary>
        /// Apply this effect to the world
        /// </summary>
        public abstract void Apply(EffectContext ctx);
        
        /// <summary>
        /// Optional: Create a reversal effect (for undo support)
        /// </summary>
        public virtual Effect CreateReversal(EffectContext ctx) => null;
        
        /// <summary>
        /// Get description for logging/debugging
        /// </summary>
        public virtual string GetDescription() => GetType().Name;
    }
    
    /// <summary>
    /// Context passed to effects for world access
    /// </summary>
    public class EffectContext
    {
        public SimWorld World { get; }
        public SimId ActorId { get; set; }
        public SimId TargetId { get; set; }
        public ActionContext ActionContext { get; set; }
        
        public EffectContext(SimWorld world)
        {
            World = world;
        }
    }
    
    // ========== Stat Effects ==========
    
    /// <summary>
    /// Modify a stat on an entity
    /// </summary>
    [Serializable]
    public class ModifyStatEffect : Effect
    {
        public SimId EntityId;
        public ContentId StatId;
        public float Delta;
        public bool IsAbsolute; // If true, sets value instead of adding
        
        public ModifyStatEffect() { }
        
        public ModifyStatEffect(SimId entityId, ContentId statId, float delta, bool isAbsolute = false)
        {
            EntityId = entityId;
            StatId = statId;
            Delta = delta;
            IsAbsolute = isAbsolute;
        }
        
        public override void Apply(EffectContext ctx)
        {
            var entity = ctx.World.Entities.GetEntity(EntityId);
            if (entity == null) return;
            
            if (IsAbsolute)
                entity.SetStat(StatId, Delta);
            else
                entity.ModifyStat(StatId, Delta);
        }
        
        public override string GetDescription() => 
            IsAbsolute ? $"Set {StatId} to {Delta}" : $"Modify {StatId} by {Delta}";
    }
    
    // ========== Tag Effects ==========
    
    /// <summary>
    /// Add a tag to an entity
    /// </summary>
    [Serializable]
    public class AddTagEffect : Effect
    {
        public SimId EntityId;
        public ContentId TagId;
        
        public AddTagEffect() { }
        
        public AddTagEffect(SimId entityId, ContentId tagId)
        {
            EntityId = entityId;
            TagId = tagId;
        }
        
        public override void Apply(EffectContext ctx)
        {
            var entity = ctx.World.Entities.GetEntity(EntityId);
            entity?.AddTag(TagId);
        }
        
        public override string GetDescription() => $"Add tag {TagId}";
    }
    
    /// <summary>
    /// Remove a tag from an entity
    /// </summary>
    [Serializable]
    public class RemoveTagEffect : Effect
    {
        public SimId EntityId;
        public ContentId TagId;
        
        public RemoveTagEffect() { }
        
        public RemoveTagEffect(SimId entityId, ContentId tagId)
        {
            EntityId = entityId;
            TagId = tagId;
        }
        
        public override void Apply(EffectContext ctx)
        {
            var entity = ctx.World.Entities.GetEntity(EntityId);
            entity?.RemoveTag(TagId);
        }
        
        public override string GetDescription() => $"Remove tag {TagId}";
    }
    
    // ========== Flag Effects ==========
    
    /// <summary>
    /// Set a flag on an entity
    /// </summary>
    [Serializable]
    public class SetFlagEffect : Effect
    {
        public SimId EntityId;
        public ContentId FlagId;
        
        public SetFlagEffect() { }
        
        public SetFlagEffect(SimId entityId, ContentId flagId)
        {
            EntityId = entityId;
            FlagId = flagId;
        }
        
        public override void Apply(EffectContext ctx)
        {
            var entity = ctx.World.Entities.GetEntity(EntityId);
            entity?.SetFlag(FlagId);
        }
        
        public override string GetDescription() => $"Set flag {FlagId}";
    }
    
    /// <summary>
    /// Clear a flag on an entity
    /// </summary>
    [Serializable]
    public class ClearFlagEffect : Effect
    {
        public SimId EntityId;
        public ContentId FlagId;
        
        public ClearFlagEffect() { }
        
        public ClearFlagEffect(SimId entityId, ContentId flagId)
        {
            EntityId = entityId;
            FlagId = flagId;
        }
        
        public override void Apply(EffectContext ctx)
        {
            var entity = ctx.World.Entities.GetEntity(EntityId);
            entity?.ClearFlag(FlagId);
        }
        
        public override string GetDescription() => $"Clear flag {FlagId}";
    }
    
    // ========== Counter Effects ==========
    
    /// <summary>
    /// Increment a counter on an entity
    /// </summary>
    [Serializable]
    public class IncrementCounterEffect : Effect
    {
        public SimId EntityId;
        public ContentId CounterId;
        public int Amount = 1;
        
        public IncrementCounterEffect() { }
        
        public IncrementCounterEffect(SimId entityId, ContentId counterId, int amount = 1)
        {
            EntityId = entityId;
            CounterId = counterId;
            Amount = amount;
        }
        
        public override void Apply(EffectContext ctx)
        {
            var entity = ctx.World.Entities.GetEntity(EntityId);
            entity?.IncrementCounter(CounterId, Amount);
        }
        
        public override string GetDescription() => $"Increment {CounterId} by {Amount}";
    }
    
    /// <summary>
    /// Set a counter value
    /// </summary>
    [Serializable]
    public class SetCounterEffect : Effect
    {
        public SimId EntityId;
        public ContentId CounterId;
        public int Value;
        
        public SetCounterEffect() { }
        
        public SetCounterEffect(SimId entityId, ContentId counterId, int value)
        {
            EntityId = entityId;
            CounterId = counterId;
            Value = value;
        }
        
        public override void Apply(EffectContext ctx)
        {
            var entity = ctx.World.Entities.GetEntity(EntityId);
            entity?.SetCounter(CounterId, Value);
        }
        
        public override string GetDescription() => $"Set {CounterId} to {Value}";
    }
    
    // ========== Inventory Effects ==========
    
    /// <summary>
    /// Add item to inventory
    /// </summary>
    [Serializable]
    public class AddItemEffect : Effect
    {
        public SimId EntityId;
        public ContentId ItemId;
        public int Quantity = 1;
        
        public AddItemEffect() { }
        
        public AddItemEffect(SimId entityId, ContentId itemId, int quantity = 1)
        {
            EntityId = entityId;
            ItemId = itemId;
            Quantity = quantity;
        }
        
        public override void Apply(EffectContext ctx)
        {
            var inv = ctx.World.Inventories.GetOrCreateInventory(EntityId);
            inv.AddItem(ItemId, Quantity);
        }
        
        public override string GetDescription() => $"Add {Quantity}x {ItemId}";
    }
    
    /// <summary>
    /// Remove item from inventory
    /// </summary>
    [Serializable]
    public class RemoveItemEffect : Effect
    {
        public SimId EntityId;
        public ContentId ItemId;
        public int Quantity = 1;
        
        public RemoveItemEffect() { }
        
        public RemoveItemEffect(SimId entityId, ContentId itemId, int quantity = 1)
        {
            EntityId = entityId;
            ItemId = itemId;
            Quantity = quantity;
        }
        
        public override void Apply(EffectContext ctx)
        {
            var inv = ctx.World.Inventories.GetInventory(EntityId);
            inv?.RemoveItem(ItemId, Quantity);
        }
        
        public override string GetDescription() => $"Remove {Quantity}x {ItemId}";
    }
    
    // ========== Timer Effects ==========
    
    /// <summary>
    /// Start a timer
    /// </summary>
    [Serializable]
    public class StartTimerEffect : Effect
    {
        public ContentId TimerId;
        public SimId OwnerId;
        public float Duration;
        public List<Effect> CompletionEffects;
        
        public StartTimerEffect() { }
        
        public StartTimerEffect(ContentId timerId, SimId ownerId, float duration, List<Effect> completionEffects = null)
        {
            TimerId = timerId;
            OwnerId = ownerId;
            Duration = duration;
            CompletionEffects = completionEffects ?? new List<Effect>();
        }
        
        public override void Apply(EffectContext ctx)
        {
            ctx.World.Timers.StartTimer(TimerId, OwnerId, Duration, CompletionEffects);
        }
        
        public override string GetDescription() => $"Start timer {TimerId} for {Duration}s";
    }
    
    /// <summary>
    /// Cancel a timer
    /// </summary>
    [Serializable]
    public class CancelTimerEffect : Effect
    {
        public ContentId TimerId;
        public SimId OwnerId;
        
        public CancelTimerEffect() { }
        
        public CancelTimerEffect(ContentId timerId, SimId ownerId)
        {
            TimerId = timerId;
            OwnerId = ownerId;
        }
        
        public override void Apply(EffectContext ctx)
        {
            ctx.World.Timers.CancelTimer(TimerId, OwnerId);
        }
        
        public override string GetDescription() => $"Cancel timer {TimerId}";
    }
    
    // ========== Event Effects ==========
    
    /// <summary>
    /// Spawn an event
    /// </summary>
    [Serializable]
    public class SpawnEventEffect : Effect
    {
        public ContentId EventDefId;
        public SimId TargetEntityId;
        public Dictionary<string, object> Parameters;
        
        public SpawnEventEffect() { }
        
        public SpawnEventEffect(ContentId eventDefId, SimId targetEntityId = default)
        {
            EventDefId = eventDefId;
            TargetEntityId = targetEntityId;
            Parameters = new Dictionary<string, object>();
        }
        
        public override void Apply(EffectContext ctx)
        {
            ctx.World.Events.SpawnEvent(EventDefId, TargetEntityId, Parameters);
        }
        
        public override string GetDescription() => $"Spawn event {EventDefId}";
    }
    
    /// <summary>
    /// Advance event to next stage
    /// </summary>
    [Serializable]
    public class AdvanceEventStageEffect : Effect
    {
        public SimId EventInstanceId;
        
        public AdvanceEventStageEffect() { }
        
        public AdvanceEventStageEffect(SimId eventInstanceId)
        {
            EventInstanceId = eventInstanceId;
        }
        
        public override void Apply(EffectContext ctx)
        {
            ctx.World.Events.AdvanceStage(EventInstanceId);
        }
        
        public override string GetDescription() => $"Advance event {EventInstanceId}";
    }
    
    /// <summary>
    /// End an event
    /// </summary>
    [Serializable]
    public class EndEventEffect : Effect
    {
        public SimId EventInstanceId;
        public bool Completed;
        
        public EndEventEffect() { }
        
        public EndEventEffect(SimId eventInstanceId, bool completed = true)
        {
            EventInstanceId = eventInstanceId;
            Completed = completed;
        }
        
        public override void Apply(EffectContext ctx)
        {
            ctx.World.Events.EndEvent(EventInstanceId, Completed);
        }
        
        public override string GetDescription() => $"End event {EventInstanceId}";
    }
    
    // ========== Quest Effects ==========
    
    /// <summary>
    /// Progress a quest
    /// </summary>
    [Serializable]
    public class QuestProgressEffect : Effect
    {
        public ContentId QuestId;
        public int StepDelta = 1;
        
        public QuestProgressEffect() { }
        
        public QuestProgressEffect(ContentId questId, int stepDelta = 1)
        {
            QuestId = questId;
            StepDelta = stepDelta;
        }
        
        public override void Apply(EffectContext ctx)
        {
            ctx.World.Quests?.ProgressQuest(QuestId, StepDelta);
        }
        
        public override string GetDescription() => $"Progress quest {QuestId}";
    }
    
    /// <summary>
    /// Complete a quest step
    /// </summary>
    [Serializable]
    public class CompleteQuestStepEffect : Effect
    {
        public ContentId QuestId;
        public ContentId StepId;
        
        public CompleteQuestStepEffect() { }
        
        public CompleteQuestStepEffect(ContentId questId, ContentId stepId)
        {
            QuestId = questId;
            StepId = stepId;
        }
        
        public override void Apply(EffectContext ctx)
        {
            // Progress the quest - in the new unified QuestModule, 
            // steps are tracked via progress, not explicit step completion
            ctx.World.Quests?.ProgressQuest(QuestId.Value, 1);
        }
        
        public override string GetDescription() => $"Progress quest {QuestId}";
    }
    
    // ========== Unlock/Progression Effects ==========
    
    /// <summary>
    /// Unlock something (achievement, ability, area, etc.)
    /// </summary>
    [Serializable]
    public class UnlockEffect : Effect
    {
        public ContentId UnlockId;
        public string Category; // "achievement", "ability", "area", etc.
        
        public UnlockEffect() { }
        
        public UnlockEffect(ContentId unlockId, string category = "default")
        {
            UnlockId = unlockId;
            Category = category;
        }
        
        public override void Apply(EffectContext ctx)
        {
            ctx.World.Progression.Unlock(UnlockId, Category);
        }
        
        public override string GetDescription() => $"Unlock {Category}/{UnlockId}";
    }
    
    // ========== Composite Effects ==========
    
    /// <summary>
    /// Apply multiple effects in sequence
    /// </summary>
    [Serializable]
    public class CompositeEffect : Effect
    {
        public List<Effect> Effects = new();
        
        public CompositeEffect() { }
        
        public CompositeEffect(params Effect[] effects)
        {
            Effects = new List<Effect>(effects);
        }
        
        public override void Apply(EffectContext ctx)
        {
            foreach (var effect in Effects)
            {
                effect.Apply(ctx);
            }
        }
        
        public override string GetDescription() => $"Composite ({Effects.Count} effects)";
    }
    
    /// <summary>
    /// Conditionally apply effects
    /// </summary>
    [Serializable]
    public class ConditionalEffect : Effect
    {
        public Condition Condition;
        public List<Effect> ThenEffects = new();
        public List<Effect> ElseEffects = new();
        
        public override void Apply(EffectContext ctx)
        {
            var conditionCtx = new ConditionContext(ctx.World)
            {
                ActorId = ctx.ActorId,
                TargetId = ctx.TargetId
            };
            
            var effectsToApply = Condition.Evaluate(conditionCtx) ? ThenEffects : ElseEffects;
            foreach (var effect in effectsToApply)
            {
                effect.Apply(ctx);
            }
        }
        
        public override string GetDescription() => $"If {Condition?.GetDescription()} then ({ThenEffects.Count} effects)";
    }
    
    // ========== Notification Effect ==========
    
    /// <summary>
    /// Send a notification to UI
    /// </summary>
    [Serializable]
    public class NotifyEffect : Effect
    {
        public string Message;
        public string Category;
        public Priority Priority = Priority.Normal;
        
        public NotifyEffect() { }
        
        public NotifyEffect(string message, string category = "info", Priority priority = Priority.Normal)
        {
            Message = message;
            Category = category;
            Priority = priority;
        }
        
        public override void Apply(EffectContext ctx)
        {
            ctx.World.SignalBus.Publish(new Signals.NotificationSignal
            {
                Message = Message,
                Category = Category,
                Priority = Priority,
                RelatedEntityId = ctx.TargetId
            });
        }
        
        public override string GetDescription() => $"Notify: {Message}";
    }
}

