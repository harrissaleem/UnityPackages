// SimCore - Content Provider
// Abstraction for loading content definitions from various sources

using System;
using System.Collections.Generic;
using SimCore.Actions;
using SimCore.Entities;
using SimCore.Events;
using SimCore.Rules;

namespace SimCore.Content
{
    /// <summary>
    /// Interface for content providers
    /// </summary>
    public interface IContentProvider
    {
        /// <summary>
        /// Get all action definitions
        /// </summary>
        IEnumerable<ActionDef> GetActionDefs();
        
        /// <summary>
        /// Get all event definitions
        /// </summary>
        IEnumerable<EventDef> GetEventDefs();
        
        /// <summary>
        /// Get all rule definitions
        /// </summary>
        IEnumerable<RuleDef> GetRuleDefs();
        
        /// <summary>
        /// Get all entity archetypes
        /// </summary>
        IEnumerable<EntityArchetype> GetEntityArchetypes();
        
        /// <summary>
        /// Get all stat definitions
        /// </summary>
        IEnumerable<StatDef> GetStatDefs();
        
        /// <summary>
        /// Get all item definitions
        /// </summary>
        IEnumerable<ItemDef> GetItemDefs();
    }
    
    /// <summary>
    /// Entity archetype definition
    /// </summary>
    [Serializable]
    public class EntityArchetype
    {
        public ContentId Id;
        public string DisplayName;
        public EntityCategory Category;
        
        // Initial stats
        public Dictionary<ContentId, float> InitialStats = new();
        public Dictionary<ContentId, (float min, float max)> StatBounds = new();
        
        // Initial tags
        public List<ContentId> InitialTags = new();
        
        // Initial flags
        public List<ContentId> InitialFlags = new();
        
        // Initial inventory items
        public Dictionary<ContentId, int> InitialItems = new();
        
        // AI state set to use
        public ContentId AIStateSetId;
    }
    
    /// <summary>
    /// Stat definition
    /// </summary>
    [Serializable]
    public class StatDef
    {
        public ContentId Id;
        public string DisplayName;
        public float DefaultValue;
        public float MinValue = float.MinValue;
        public float MaxValue = float.MaxValue;
        public bool ShowInUI;
        public string Category; // "health", "resource", "skill", etc.
    }
    
    /// <summary>
    /// Item definition
    /// </summary>
    [Serializable]
    public class ItemDef
    {
        public ContentId Id;
        public string DisplayName;
        public string Description;
        public int MaxStackSize = 99;
        public float BasePrice;
        public List<ContentId> Tags = new();
        public string Category; // "weapon", "consumable", "key", etc.
    }
    
    /// <summary>
    /// Base class for code-based content providers
    /// </summary>
    public abstract class CodeContentProvider : IContentProvider
    {
        protected List<ActionDef> _actions = new();
        protected List<EventDef> _events = new();
        protected List<RuleDef> _rules = new();
        protected List<EntityArchetype> _archetypes = new();
        protected List<StatDef> _stats = new();
        protected List<ItemDef> _items = new();
        
        public IEnumerable<ActionDef> GetActionDefs() => _actions;
        public IEnumerable<EventDef> GetEventDefs() => _events;
        public IEnumerable<RuleDef> GetRuleDefs() => _rules;
        public IEnumerable<EntityArchetype> GetEntityArchetypes() => _archetypes;
        public IEnumerable<StatDef> GetStatDefs() => _stats;
        public IEnumerable<ItemDef> GetItemDefs() => _items;
        
        /// <summary>
        /// Initialize content - override in derived classes
        /// </summary>
        public abstract void Initialize();
    }
    
    /// <summary>
    /// Loads all content from a provider into the world
    /// </summary>
    public static class ContentLoader
    {
        public static void LoadContent(SimWorld world, IContentProvider provider)
        {
            // Load actions
            world.Actions.RegisterActions(provider.GetActionDefs());
            
            // Load events
            foreach (var eventDef in provider.GetEventDefs())
            {
                world.Events.RegisterEvent(eventDef);
            }
            
            // Load rules
            world.Rules.RegisterRules(provider.GetRuleDefs());
        }
        
        /// <summary>
        /// Create an entity from an archetype
        /// </summary>
        public static Entity CreateEntityFromArchetype(SimWorld world, EntityArchetype archetype, string displayName = null)
        {
            var entity = world.Entities.CreateEntity(archetype.Id, archetype.Category, displayName ?? archetype.DisplayName);
            
            // Initialize stats
            foreach (var stat in archetype.InitialStats)
            {
                var bounds = archetype.StatBounds.TryGetValue(stat.Key, out var b) ? b : (float.MinValue, float.MaxValue);
                entity.InitStat(stat.Key, stat.Value, bounds.Item1, bounds.Item2);
            }
            
            // Add tags
            foreach (var tag in archetype.InitialTags)
            {
                entity.AddTag(tag);
            }
            
            // Set flags
            foreach (var flag in archetype.InitialFlags)
            {
                entity.SetFlag(flag);
            }
            
            // Add inventory items
            var inventory = world.Inventories.GetOrCreateInventory(entity.Id);
            foreach (var item in archetype.InitialItems)
            {
                inventory.AddItem(item.Key, item.Value);
            }
            
            return entity;
        }
    }
}

