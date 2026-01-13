// SimCore - World Simulator Configuration
// Base SO for world simulation settings
// Games can extend this or use it directly

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimCore.World
{
    /// <summary>
    /// Base configuration for WorldSimulator.
    /// Extend this in your game for additional settings.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldSimConfig", menuName = "SimCore/World Sim Config")]
    public class WorldSimConfigSO : ScriptableObject
    {
        [Header("═══ POPULATION ═══")]
        [Tooltip("Maximum NPCs that can exist at once")]
        [Range(5, 100)] public int MaxNPCs = 20;
        
        [Tooltip("Maximum vehicles that can exist at once")]
        [Range(0, 50)] public int MaxVehicles = 10;
        
        [Header("═══ SPAWN DISTANCES ═══")]
        [Tooltip("Distance from player where entities spawn")]
        [Range(20f, 100f)] public float SpawnRadius = 40f;
        
        [Tooltip("Distance from player where entities despawn")]
        [Range(50f, 200f)] public float DespawnRadius = 60f;
        
        [Tooltip("Minimum distance from player to spawn")]
        [Range(10f, 50f)] public float MinSpawnDistance = 15f;
        
        [Header("═══ TIMING ═══")]
        [Tooltip("Seconds between spawn attempts")]
        [Range(0.5f, 10f)] public float SpawnInterval = 2f;
        
        [Tooltip("Ratio of max to spawn at simulation start")]
        [Range(0f, 1f)] public float InitialBurstRatio = 0.5f;
        
        [Header("═══ TIME OF DAY ═══")]
        [Tooltip("How time of day affects the world")]
        public List<TimeOfDayModifier> TimeModifiers = new();
        
        /// <summary>
        /// Get the time modifier active at a given hour
        /// </summary>
        public TimeOfDayModifier GetModifierForHour(float hour)
        {
            foreach (var mod in TimeModifiers)
            {
                if (mod.IsActiveAtHour(hour))
                    return mod;
            }
            return TimeOfDayModifier.Default;
        }
        
        /// <summary>
        /// Validate config values
        /// </summary>
        public virtual void Validate()
        {
            if (SpawnRadius >= DespawnRadius)
                SimCoreLogger.LogWarning($"[WorldSimConfig] SpawnRadius ({SpawnRadius}) should be < DespawnRadius ({DespawnRadius})");
            
            if (MinSpawnDistance >= SpawnRadius)
                SimCoreLogger.LogWarning($"[WorldSimConfig] MinSpawnDistance ({MinSpawnDistance}) should be < SpawnRadius ({SpawnRadius})");
            
            if (TimeModifiers == null || TimeModifiers.Count == 0)
            {
                TimeModifiers = new List<TimeOfDayModifier> { TimeOfDayModifier.Default };
            }
        }
    }
}

