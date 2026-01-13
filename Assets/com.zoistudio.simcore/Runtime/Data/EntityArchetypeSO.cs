// SimCore - Entity Archetype ScriptableObject
// ═══════════════════════════════════════════════════════════════════════════════
// Defines WHAT an entity IS - its visual representation and identity
// This is SEPARATE from behavior (how it acts) which is game-specific
//
// Use this as a base class or directly for simple entity types.
// Games can extend this for specific needs (vehicles, animals, etc.)
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using SimCore.Entities;

namespace SimCore.Data
{
    [CreateAssetMenu(fileName = "Archetype_New", menuName = "SimCore/Entity Archetype")]
    public class EntityArchetypeSO : ScriptableObject
    {
        [Header("═══ IDENTITY ═══")]
        [Tooltip("Unique ID for this archetype")]
        public string Id;
        
        [Tooltip("Display name for UI")]
        public string DisplayName;
        
        [Tooltip("Entity category (NPC, Vehicle, Object, etc.)")]
        public EntityCategory Category = EntityCategory.NPC;
        
        [Header("═══ APPEARANCE ═══")]
        [Tooltip("Prefab to instantiate for this entity")]
        public GameObject Prefab;
        
        [Tooltip("Optional variant prefabs for visual variety")]
        public List<GameObject> VariantPrefabs;
        
        [Header("═══ INITIAL STATE ═══")]
        [Tooltip("Tags this entity starts with")]
        public List<string> InitialTags;
        
        [Tooltip("Initial data values (key-value pairs)")]
        public List<InitialDataValue> InitialData;
        
        [Header("═══ NAMING ═══")]
        [Tooltip("Name pool for random name generation (leave empty to use DisplayName)")]
        public List<string> NamePool;
        
        /// <summary>
        /// Get the prefab to spawn (randomly picks variant if available)
        /// </summary>
        public GameObject GetPrefab()
        {
            if (VariantPrefabs != null && VariantPrefabs.Count > 0)
            {
                // 50% chance to use a variant
                if (Random.value > 0.5f)
                {
                    var variant = VariantPrefabs[Random.Range(0, VariantPrefabs.Count)];
                    if (variant != null) return variant;
                }
            }
            return Prefab;
        }
        
        /// <summary>
        /// Get a random name from the pool, or DisplayName if pool is empty
        /// </summary>
        public string GetRandomName()
        {
            if (NamePool == null || NamePool.Count == 0)
                return DisplayName;
            return NamePool[Random.Range(0, NamePool.Count)];
        }
        
        /// <summary>
        /// Apply initial tags and data to an entity
        /// </summary>
        public void ApplyToEntity(Entity entity)
        {
            if (entity == null) return;
            
            // Apply initial tags
            if (InitialTags != null)
            {
                foreach (var tag in InitialTags)
                {
                    if (!string.IsNullOrEmpty(tag))
                        entity.AddTag(new ContentId(tag));
                }
            }
            
            // Apply initial data
            if (InitialData != null)
            {
                foreach (var data in InitialData)
                {
                    if (!string.IsNullOrEmpty(data.Key))
                        entity.SetData<string>(data.Key, data.Value);
                }
            }
        }
    }
    
    /// <summary>
    /// Key-value pair for initial entity data
    /// </summary>
    [System.Serializable]
    public class InitialDataValue
    {
        public string Key;
        public string Value;
    }
}

