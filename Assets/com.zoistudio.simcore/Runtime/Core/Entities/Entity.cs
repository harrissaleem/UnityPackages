// SimCore - Entity System
// Core entity with stats, tags, flags, and counters

using System;
using System.Collections.Generic;
using SimCore.Signals;

namespace SimCore.Entities
{
    /// <summary>
    /// Core entity in the simulation
    /// Has stats, tags, flags, counters, and inventory
    /// </summary>
    [Serializable]
    public class Entity
    {
        public SimId Id { get; private set; }
        public ContentId ArchetypeId { get; private set; }
        public EntityCategory Category { get; private set; }
        public string DisplayName { get; set; }
        public bool IsActive { get; set; } = true;
        
        // Stats (numeric values with min/max)
        private readonly Dictionary<ContentId, float> _stats = new();
        private readonly Dictionary<ContentId, (float min, float max)> _statBounds = new();
        
        // Tags (boolean markers)
        private readonly HashSet<ContentId> _tags = new();
        
        // Flags (named booleans, slightly different from tags - more persistent/story-related)
        private readonly HashSet<ContentId> _flags = new();
        
        // Counters (named integers for tracking)
        private readonly Dictionary<ContentId, int> _counters = new();
        
        // Generic data storage (for AI, movement, game-specific data)
        private readonly Dictionary<string, object> _data = new();
        
        // Reference to signal bus for notifications
        private SignalBus _signalBus;
        
        public Entity(SimId id, ContentId archetypeId, EntityCategory category, SignalBus signalBus = null)
        {
            Id = id;
            ArchetypeId = archetypeId;
            Category = category;
            _signalBus = signalBus;
        }
        
        public void SetSignalBus(SignalBus bus) => _signalBus = bus;
        
        // ========== Stats ==========
        
        public void InitStat(ContentId statId, float value, float min = float.MinValue, float max = float.MaxValue)
        {
            _stats[statId] = Math.Clamp(value, min, max);
            _statBounds[statId] = (min, max);
        }
        
        public float GetStat(ContentId statId, float defaultValue = 0f)
        {
            return _stats.TryGetValue(statId, out var value) ? value : defaultValue;
        }
        
        public void SetStat(ContentId statId, float value)
        {
            var oldValue = GetStat(statId);
            if (_statBounds.TryGetValue(statId, out var bounds))
            {
                value = Math.Clamp(value, bounds.min, bounds.max);
            }
            _stats[statId] = value;
            
            if (Math.Abs(oldValue - value) > 0.0001f)
            {
                _signalBus?.Publish(new StatChangedSignal
                {
                    EntityId = Id,
                    StatId = statId,
                    OldValue = oldValue,
                    NewValue = value
                });
            }
        }
        
        public void ModifyStat(ContentId statId, float delta)
        {
            SetStat(statId, GetStat(statId) + delta);
        }
        
        public bool HasStat(ContentId statId) => _stats.ContainsKey(statId);
        
        public IEnumerable<KeyValuePair<ContentId, float>> GetAllStats() => _stats;
        
        // ========== Tags ==========
        
        public bool HasTag(ContentId tagId) => _tags.Contains(tagId);
        
        public void AddTag(ContentId tagId)
        {
            if (_tags.Add(tagId))
            {
                _signalBus?.Publish(new TagChangedSignal
                {
                    EntityId = Id,
                    TagId = tagId,
                    Added = true
                });
            }
        }
        
        public void RemoveTag(ContentId tagId)
        {
            if (_tags.Remove(tagId))
            {
                _signalBus?.Publish(new TagChangedSignal
                {
                    EntityId = Id,
                    TagId = tagId,
                    Added = false
                });
            }
        }
        
        public IEnumerable<ContentId> GetAllTags() => _tags;
        
        // ========== Flags ==========
        
        public bool HasFlag(ContentId flagId) => _flags.Contains(flagId);
        
        public void SetFlag(ContentId flagId)
        {
            _flags.Add(flagId);
        }
        
        public void ClearFlag(ContentId flagId)
        {
            _flags.Remove(flagId);
        }
        
        public IEnumerable<ContentId> GetAllFlags() => _flags;
        
        // ========== Counters ==========
        
        public int GetCounter(ContentId counterId) => 
            _counters.TryGetValue(counterId, out var value) ? value : 0;
        
        public void SetCounter(ContentId counterId, int value)
        {
            _counters[counterId] = value;
        }
        
        public void IncrementCounter(ContentId counterId, int amount = 1)
        {
            _counters[counterId] = GetCounter(counterId) + amount;
        }
        
        public IEnumerable<KeyValuePair<ContentId, int>> GetAllCounters() => _counters;
        
        // ========== Generic Data Storage ==========
        // For AI, movement, and game-specific data
        
        /// <summary>
        /// Set arbitrary data on the entity
        /// </summary>
        public void SetData<T>(string key, T value)
        {
            if (value == null)
                _data.Remove(key);
            else
                _data[key] = value;
        }
        
        /// <summary>
        /// Get arbitrary data from the entity
        /// </summary>
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out var value) && value is T typed)
                return typed;
            return defaultValue;
        }
        
        /// <summary>
        /// Check if entity has data with key
        /// </summary>
        public bool HasData(string key) => _data.ContainsKey(key);
        
        /// <summary>
        /// Clear data with key
        /// </summary>
        public void ClearData(string key) => _data.Remove(key);
        
        // ========== Serialization Support ==========
        
        public EntitySnapshot CreateSnapshot()
        {
            return new EntitySnapshot
            {
                Id = Id,
                ArchetypeId = ArchetypeId,
                Category = Category,
                DisplayName = DisplayName,
                IsActive = IsActive,
                Stats = new Dictionary<ContentId, float>(_stats),
                StatBounds = new Dictionary<ContentId, (float, float)>(_statBounds),
                Tags = new HashSet<ContentId>(_tags),
                Flags = new HashSet<ContentId>(_flags),
                Counters = new Dictionary<ContentId, int>(_counters)
            };
        }
        
        public void RestoreFromSnapshot(EntitySnapshot snapshot)
        {
            DisplayName = snapshot.DisplayName;
            IsActive = snapshot.IsActive;
            
            _stats.Clear();
            foreach (var kvp in snapshot.Stats) _stats[kvp.Key] = kvp.Value;
            
            _statBounds.Clear();
            foreach (var kvp in snapshot.StatBounds) _statBounds[kvp.Key] = kvp.Value;
            
            _tags.Clear();
            foreach (var tag in snapshot.Tags) _tags.Add(tag);
            
            _flags.Clear();
            foreach (var flag in snapshot.Flags) _flags.Add(flag);
            
            _counters.Clear();
            foreach (var kvp in snapshot.Counters) _counters[kvp.Key] = kvp.Value;
        }
    }
    
    /// <summary>
    /// Serializable snapshot of entity state
    /// </summary>
    [Serializable]
    public class EntitySnapshot
    {
        public SimId Id;
        public ContentId ArchetypeId;
        public EntityCategory Category;
        public string DisplayName;
        public bool IsActive;
        public Dictionary<ContentId, float> Stats;
        public Dictionary<ContentId, (float min, float max)> StatBounds;
        public HashSet<ContentId> Tags;
        public HashSet<ContentId> Flags;
        public Dictionary<ContentId, int> Counters;
    }
}

