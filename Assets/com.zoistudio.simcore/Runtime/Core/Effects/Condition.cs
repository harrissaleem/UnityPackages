// SimCore - Conditions
// Boolean expressions for rules and conditional effects

using System;
using System.Collections.Generic;

namespace SimCore.Effects
{
    /// <summary>
    /// Context for evaluating conditions
    /// </summary>
    public class ConditionContext
    {
        public SimWorld World { get; }
        public SimId ActorId { get; set; }
        public SimId TargetId { get; set; }
        public SimId EventInstanceId { get; set; }
        public ContentId AreaId { get; set; }
        
        public ConditionContext(SimWorld world)
        {
            World = world;
        }
    }
    
    /// <summary>
    /// Base class for all conditions
    /// </summary>
    [Serializable]
    public abstract class Condition
    {
        /// <summary>
        /// Evaluate this condition
        /// </summary>
        public abstract bool Evaluate(ConditionContext ctx);
        
        /// <summary>
        /// Get description for debugging
        /// </summary>
        public virtual string GetDescription() => GetType().Name;
    }
    
    // ========== Tag Conditions ==========
    
    /// <summary>
    /// Check if entity has a tag
    /// </summary>
    [Serializable]
    public class HasTagCondition : Condition
    {
        public SimId EntityId;
        public ContentId TagId;
        public bool UseTarget; // If true, use ctx.TargetId instead of EntityId
        
        public HasTagCondition() { }
        
        public HasTagCondition(ContentId tagId, SimId entityId = default, bool useTarget = false)
        {
            TagId = tagId;
            EntityId = entityId;
            UseTarget = useTarget;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var id = UseTarget ? ctx.TargetId : (EntityId.IsValid ? EntityId : ctx.ActorId);
            var entity = ctx.World.Entities.GetEntity(id);
            return entity?.HasTag(TagId) ?? false;
        }
        
        public override string GetDescription() => $"HasTag({TagId})";
    }
    
    /// <summary>
    /// Check if entity is missing a tag
    /// </summary>
    [Serializable]
    public class MissingTagCondition : Condition
    {
        public SimId EntityId;
        public ContentId TagId;
        public bool UseTarget;
        
        public MissingTagCondition() { }
        
        public MissingTagCondition(ContentId tagId, SimId entityId = default, bool useTarget = false)
        {
            TagId = tagId;
            EntityId = entityId;
            UseTarget = useTarget;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var id = UseTarget ? ctx.TargetId : (EntityId.IsValid ? EntityId : ctx.ActorId);
            var entity = ctx.World.Entities.GetEntity(id);
            return entity == null || !entity.HasTag(TagId);
        }
        
        public override string GetDescription() => $"MissingTag({TagId})";
    }
    
    // ========== Stat Conditions ==========
    
    /// <summary>
    /// Check if stat is above a value
    /// </summary>
    [Serializable]
    public class StatAboveCondition : Condition
    {
        public SimId EntityId;
        public ContentId StatId;
        public float Value;
        public bool UseTarget;
        
        public StatAboveCondition() { }
        
        public StatAboveCondition(ContentId statId, float value, SimId entityId = default, bool useTarget = false)
        {
            StatId = statId;
            Value = value;
            EntityId = entityId;
            UseTarget = useTarget;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var id = UseTarget ? ctx.TargetId : (EntityId.IsValid ? EntityId : ctx.ActorId);
            var entity = ctx.World.Entities.GetEntity(id);
            return entity != null && entity.GetStat(StatId) > Value;
        }
        
        public override string GetDescription() => $"{StatId} > {Value}";
    }
    
    /// <summary>
    /// Check if stat is below a value
    /// </summary>
    [Serializable]
    public class StatBelowCondition : Condition
    {
        public SimId EntityId;
        public ContentId StatId;
        public float Value;
        public bool UseTarget;
        
        public StatBelowCondition() { }
        
        public StatBelowCondition(ContentId statId, float value, SimId entityId = default, bool useTarget = false)
        {
            StatId = statId;
            Value = value;
            EntityId = entityId;
            UseTarget = useTarget;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var id = UseTarget ? ctx.TargetId : (EntityId.IsValid ? EntityId : ctx.ActorId);
            var entity = ctx.World.Entities.GetEntity(id);
            return entity != null && entity.GetStat(StatId) < Value;
        }
        
        public override string GetDescription() => $"{StatId} < {Value}";
    }
    
    /// <summary>
    /// Check if stat equals a value (with tolerance)
    /// </summary>
    [Serializable]
    public class StatEqualsCondition : Condition
    {
        public SimId EntityId;
        public ContentId StatId;
        public float Value;
        public float Tolerance = 0.0001f;
        public bool UseTarget;
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var id = UseTarget ? ctx.TargetId : (EntityId.IsValid ? EntityId : ctx.ActorId);
            var entity = ctx.World.Entities.GetEntity(id);
            return entity != null && Math.Abs(entity.GetStat(StatId) - Value) <= Tolerance;
        }
        
        public override string GetDescription() => $"{StatId} == {Value}";
    }
    
    // ========== Flag Conditions ==========
    
    /// <summary>
    /// Check if flag is set
    /// </summary>
    [Serializable]
    public class FlagSetCondition : Condition
    {
        public SimId EntityId;
        public ContentId FlagId;
        public bool UseTarget;
        
        public FlagSetCondition() { }
        
        public FlagSetCondition(ContentId flagId, SimId entityId = default, bool useTarget = false)
        {
            FlagId = flagId;
            EntityId = entityId;
            UseTarget = useTarget;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var id = UseTarget ? ctx.TargetId : (EntityId.IsValid ? EntityId : ctx.ActorId);
            var entity = ctx.World.Entities.GetEntity(id);
            return entity?.HasFlag(FlagId) ?? false;
        }
        
        public override string GetDescription() => $"FlagSet({FlagId})";
    }
    
    // ========== Counter Conditions ==========
    
    /// <summary>
    /// Check if counter is at least a value
    /// </summary>
    [Serializable]
    public class CounterAtLeastCondition : Condition
    {
        public SimId EntityId;
        public ContentId CounterId;
        public int Value;
        public bool UseTarget;
        
        public CounterAtLeastCondition() { }
        
        public CounterAtLeastCondition(ContentId counterId, int value, SimId entityId = default, bool useTarget = false)
        {
            CounterId = counterId;
            Value = value;
            EntityId = entityId;
            UseTarget = useTarget;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var id = UseTarget ? ctx.TargetId : (EntityId.IsValid ? EntityId : ctx.ActorId);
            var entity = ctx.World.Entities.GetEntity(id);
            return entity != null && entity.GetCounter(CounterId) >= Value;
        }
        
        public override string GetDescription() => $"{CounterId} >= {Value}";
    }
    
    // ========== Inventory Conditions ==========
    
    /// <summary>
    /// Check if entity has item
    /// </summary>
    [Serializable]
    public class HasItemCondition : Condition
    {
        public SimId EntityId;
        public ContentId ItemId;
        public int MinQuantity = 1;
        public bool UseTarget;
        
        public HasItemCondition() { }
        
        public HasItemCondition(ContentId itemId, int minQuantity = 1, SimId entityId = default, bool useTarget = false)
        {
            ItemId = itemId;
            MinQuantity = minQuantity;
            EntityId = entityId;
            UseTarget = useTarget;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var id = UseTarget ? ctx.TargetId : (EntityId.IsValid ? EntityId : ctx.ActorId);
            var inv = ctx.World.Inventories.GetInventory(id);
            return inv?.HasItem(ItemId, MinQuantity) ?? false;
        }
        
        public override string GetDescription() => $"HasItem({ItemId}, {MinQuantity})";
    }
    
    /// <summary>
    /// Check item count
    /// </summary>
    [Serializable]
    public class ItemCountAtLeastCondition : Condition
    {
        public SimId EntityId;
        public ContentId ItemId;
        public int Count;
        public bool UseTarget;
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var id = UseTarget ? ctx.TargetId : (EntityId.IsValid ? EntityId : ctx.ActorId);
            var inv = ctx.World.Inventories.GetInventory(id);
            return inv != null && inv.GetItemCount(ItemId) >= Count;
        }
        
        public override string GetDescription() => $"ItemCount({ItemId}) >= {Count}";
    }
    
    // ========== Area/Range Conditions ==========
    
    /// <summary>
    /// Check if entity is in an area
    /// </summary>
    [Serializable]
    public class InAreaCondition : Condition
    {
        public SimId EntityId;
        public ContentId AreaId;
        public bool UseTarget;
        
        public InAreaCondition() { }
        
        public InAreaCondition(ContentId areaId, SimId entityId = default, bool useTarget = false)
        {
            AreaId = areaId;
            EntityId = entityId;
            UseTarget = useTarget;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var id = UseTarget ? ctx.TargetId : (EntityId.IsValid ? EntityId : ctx.ActorId);
            return ctx.World.Partition.IsInArea(id, AreaId);
        }
        
        public override string GetDescription() => $"InArea({AreaId})";
    }
    
    /// <summary>
    /// Check if two entities are within range
    /// </summary>
    [Serializable]
    public class InRangeCondition : Condition
    {
        public SimId EntityA;
        public SimId EntityB;
        public float MaxDistance;
        public bool UseActorAndTarget; // If true, use ctx.ActorId and ctx.TargetId
        
        public InRangeCondition() { }
        
        public InRangeCondition(float maxDistance, bool useActorAndTarget = true)
        {
            MaxDistance = maxDistance;
            UseActorAndTarget = useActorAndTarget;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var idA = UseActorAndTarget ? ctx.ActorId : EntityA;
            var idB = UseActorAndTarget ? ctx.TargetId : EntityB;
            return ctx.World.Partition.GetDistance(idA, idB) <= MaxDistance;
        }
        
        public override string GetDescription() => $"InRange({MaxDistance})";
    }
    
    // ========== Time Conditions ==========
    
    /// <summary>
    /// Check if current time is within a window
    /// </summary>
    [Serializable]
    public class TimeInWindowCondition : Condition
    {
        public float StartHour; // 0-24
        public float EndHour;   // 0-24
        
        public TimeInWindowCondition() { }
        
        public TimeInWindowCondition(float startHour, float endHour)
        {
            StartHour = startHour;
            EndHour = endHour;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            float currentHour = ctx.World.TimeOfDay;
            
            if (StartHour <= EndHour)
            {
                // Normal range (e.g., 9-17)
                return currentHour >= StartHour && currentHour <= EndHour;
            }
            else
            {
                // Overnight range (e.g., 22-6)
                return currentHour >= StartHour || currentHour <= EndHour;
            }
        }
        
        public override string GetDescription() => $"Time({StartHour:F1}-{EndHour:F1})";
    }
    
    // ========== Event Conditions ==========
    
    /// <summary>
    /// Check if event is at a specific stage
    /// </summary>
    [Serializable]
    public class EventStageIsCondition : Condition
    {
        public SimId EventInstanceId;
        public int Stage;
        public bool UseContextEvent; // If true, use ctx.EventInstanceId
        
        public EventStageIsCondition() { }
        
        public EventStageIsCondition(int stage, bool useContextEvent = true)
        {
            Stage = stage;
            UseContextEvent = useContextEvent;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            var eventId = UseContextEvent ? ctx.EventInstanceId : EventInstanceId;
            var evt = ctx.World.Events.GetEvent(eventId);
            return evt != null && evt.CurrentStage == Stage;
        }
        
        public override string GetDescription() => $"EventStage == {Stage}";
    }
    
    /// <summary>
    /// Check if an event is active
    /// </summary>
    [Serializable]
    public class EventActiveCondition : Condition
    {
        public ContentId EventDefId;
        
        public EventActiveCondition() { }
        
        public EventActiveCondition(ContentId eventDefId)
        {
            EventDefId = eventDefId;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            return ctx.World.Events.IsEventActive(EventDefId);
        }
        
        public override string GetDescription() => $"EventActive({EventDefId})";
    }
    
    // ========== Logical Conditions ==========
    
    /// <summary>
    /// AND multiple conditions
    /// </summary>
    [Serializable]
    public class AndCondition : Condition
    {
        public List<Condition> Conditions = new();
        
        public AndCondition() { }
        
        public AndCondition(params Condition[] conditions)
        {
            Conditions = new List<Condition>(conditions);
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            foreach (var cond in Conditions)
            {
                if (!cond.Evaluate(ctx))
                    return false;
            }
            return true;
        }
        
        public override string GetDescription() => $"AND({Conditions.Count})";
    }
    
    /// <summary>
    /// OR multiple conditions
    /// </summary>
    [Serializable]
    public class OrCondition : Condition
    {
        public List<Condition> Conditions = new();
        
        public OrCondition() { }
        
        public OrCondition(params Condition[] conditions)
        {
            Conditions = new List<Condition>(conditions);
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            foreach (var cond in Conditions)
            {
                if (cond.Evaluate(ctx))
                    return true;
            }
            return false;
        }
        
        public override string GetDescription() => $"OR({Conditions.Count})";
    }
    
    /// <summary>
    /// NOT a condition
    /// </summary>
    [Serializable]
    public class NotCondition : Condition
    {
        public Condition Inner;
        
        public NotCondition() { }
        
        public NotCondition(Condition inner)
        {
            Inner = inner;
        }
        
        public override bool Evaluate(ConditionContext ctx)
        {
            return !Inner.Evaluate(ctx);
        }
        
        public override string GetDescription() => $"NOT({Inner?.GetDescription()})";
    }
    
    /// <summary>
    /// Always true
    /// </summary>
    [Serializable]
    public class AlwaysTrueCondition : Condition
    {
        public override bool Evaluate(ConditionContext ctx) => true;
        public override string GetDescription() => "true";
    }
    
    /// <summary>
    /// Always false
    /// </summary>
    [Serializable]
    public class AlwaysFalseCondition : Condition
    {
        public override bool Evaluate(ConditionContext ctx) => false;
        public override string GetDescription() => "false";
    }
}

