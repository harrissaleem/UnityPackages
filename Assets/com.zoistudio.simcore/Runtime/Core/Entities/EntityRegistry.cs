// SimCore - Entity Registry
// Central registry for all entities in the simulation

using System;
using System.Collections.Generic;
using System.Linq;
using SimCore.Signals;

namespace SimCore.Entities
{
    /// <summary>
    /// Central registry for all entities
    /// Provides lookup, creation, and querying
    /// </summary>
    public class EntityRegistry
    {
        private readonly Dictionary<SimId, Entity> _entities = new();
        private readonly Dictionary<EntityCategory, HashSet<SimId>> _byCategory = new();
        private readonly Dictionary<ContentId, HashSet<SimId>> _byArchetype = new();
        private readonly Dictionary<ContentId, HashSet<SimId>> _byTag = new();
        
        private int _nextId = 1;
        private readonly SignalBus _signalBus;
        
        public EntityRegistry(SignalBus signalBus)
        {
            _signalBus = signalBus;
            
            // Initialize category indices
            foreach (EntityCategory cat in Enum.GetValues(typeof(EntityCategory)))
            {
                _byCategory[cat] = new HashSet<SimId>();
            }
        }
        
        /// <summary>
        /// Generate a new unique ID
        /// </summary>
        public SimId GenerateId() => new SimId(_nextId++);
        
        /// <summary>
        /// Create and register a new entity
        /// </summary>
        public Entity CreateEntity(ContentId archetypeId, EntityCategory category, string displayName = null)
        {
            var id = GenerateId();
            var entity = new Entity(id, archetypeId, category, _signalBus);
            entity.DisplayName = displayName ?? archetypeId.Value;
            
            RegisterEntity(entity);
            return entity;
        }
        
        /// <summary>
        /// Register an existing entity
        /// </summary>
        public void RegisterEntity(Entity entity)
        {
            _entities[entity.Id] = entity;
            _byCategory[entity.Category].Add(entity.Id);
            
            if (!_byArchetype.TryGetValue(entity.ArchetypeId, out var archetypeSet))
            {
                archetypeSet = new HashSet<SimId>();
                _byArchetype[entity.ArchetypeId] = archetypeSet;
            }
            archetypeSet.Add(entity.Id);
            
            // Index by current tags
            foreach (var tag in entity.GetAllTags())
            {
                AddToTagIndex(entity.Id, tag);
            }
        }
        
        /// <summary>
        /// Remove an entity from the registry
        /// </summary>
        public void RemoveEntity(SimId id)
        {
            if (!_entities.TryGetValue(id, out var entity))
                return;
            
            _entities.Remove(id);
            _byCategory[entity.Category].Remove(id);
            
            if (_byArchetype.TryGetValue(entity.ArchetypeId, out var archetypeSet))
            {
                archetypeSet.Remove(id);
            }
            
            foreach (var tag in entity.GetAllTags())
            {
                RemoveFromTagIndex(id, tag);
            }
        }
        
        /// <summary>
        /// Get entity by ID
        /// </summary>
        public Entity GetEntity(SimId id)
        {
            return _entities.TryGetValue(id, out var entity) ? entity : null;
        }
        
        /// <summary>
        /// Try get entity by ID
        /// </summary>
        public bool TryGetEntity(SimId id, out Entity entity)
        {
            return _entities.TryGetValue(id, out entity);
        }
        
        /// <summary>
        /// Get all entities
        /// </summary>
        public IEnumerable<Entity> GetAllEntities() => _entities.Values;
        
        /// <summary>
        /// Get entities by category
        /// </summary>
        public IEnumerable<Entity> GetEntitiesByCategory(EntityCategory category)
        {
            if (_byCategory.TryGetValue(category, out var ids))
            {
                foreach (var id in ids)
                {
                    if (_entities.TryGetValue(id, out var entity))
                        yield return entity;
                }
            }
        }
        
        /// <summary>
        /// Get entities by archetype
        /// </summary>
        public IEnumerable<Entity> GetEntitiesByArchetype(ContentId archetypeId)
        {
            if (_byArchetype.TryGetValue(archetypeId, out var ids))
            {
                foreach (var id in ids)
                {
                    if (_entities.TryGetValue(id, out var entity))
                        yield return entity;
                }
            }
        }
        
        /// <summary>
        /// Get entities with a specific tag
        /// </summary>
        public IEnumerable<Entity> GetEntitiesWithTag(ContentId tagId)
        {
            if (_byTag.TryGetValue(tagId, out var ids))
            {
                foreach (var id in ids)
                {
                    if (_entities.TryGetValue(id, out var entity))
                        yield return entity;
                }
            }
        }
        
        /// <summary>
        /// Get the player entity (assumes single player)
        /// </summary>
        public Entity GetPlayer()
        {
            return GetEntitiesByCategory(EntityCategory.Player).FirstOrDefault();
        }
        
        /// <summary>
        /// Check if entity exists
        /// </summary>
        public bool EntityExists(SimId id) => _entities.ContainsKey(id);
        
        /// <summary>
        /// Get entity count
        /// </summary>
        public int Count => _entities.Count;
        
        // Tag index management (call when tags change)
        public void AddToTagIndex(SimId entityId, ContentId tagId)
        {
            if (!_byTag.TryGetValue(tagId, out var set))
            {
                set = new HashSet<SimId>();
                _byTag[tagId] = set;
            }
            set.Add(entityId);
        }
        
        public void RemoveFromTagIndex(SimId entityId, ContentId tagId)
        {
            if (_byTag.TryGetValue(tagId, out var set))
            {
                set.Remove(entityId);
            }
        }
        
        /// <summary>
        /// Clear all entities
        /// </summary>
        public void Clear()
        {
            _entities.Clear();
            foreach (var set in _byCategory.Values) set.Clear();
            _byArchetype.Clear();
            _byTag.Clear();
        }
        
        /// <summary>
        /// Create snapshots of all entities (for persistence)
        /// </summary>
        public List<EntitySnapshot> CreateAllSnapshots()
        {
            return _entities.Values.Select(e => e.CreateSnapshot()).ToList();
        }
        
        /// <summary>
        /// Restore from snapshots
        /// </summary>
        public void RestoreFromSnapshots(List<EntitySnapshot> snapshots)
        {
            Clear();
            _nextId = 1;
            
            foreach (var snapshot in snapshots)
            {
                var entity = new Entity(snapshot.Id, snapshot.ArchetypeId, snapshot.Category, _signalBus);
                entity.RestoreFromSnapshot(snapshot);
                RegisterEntity(entity);
                
                if (snapshot.Id.Value >= _nextId)
                    _nextId = snapshot.Id.Value + 1;
            }
        }
    }
}

