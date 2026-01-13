// SimCore - World Simulator Base
// Generic world simulation framework for open-world games
// Extend this for game-specific implementations (Police, Farm, Horror, etc.)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using SimCore.Entities;
using SimCore.AI;
using SimCore.Unity;
using SimCore.World.Zones;

namespace SimCore.World
{
    /// <summary>
    /// Interface for world zones/areas.
    /// Implement in your game for districts, regions, etc.
    /// </summary>
    public interface IWorldZone
    {
        string Id { get; }
        string DisplayName { get; }
        Vector3 Center { get; }
        float Radius { get; }
        
        /// <summary>
        /// Check if a position is within this zone
        /// </summary>
        bool Contains(Vector3 position);
    }
    
    /// <summary>
    /// Spawn density configuration
    /// </summary>
    [Serializable]
    public class SpawnDensityConfig
    {
        [Header("Population Limits")]
        public int MaxNPCs = 20;
        public int MaxVehicles = 10;
        
        [Header("Spawn Radii")]
        public float SpawnRadius = 40f;
        public float DespawnRadius = 60f;
        public float MinSpawnDistance = 15f;
        
        [Header("Timing")]
        public float SpawnInterval = 2f;
        public float InitialBurstRatio = 0.5f; // Spawn this ratio on start
    }
    
    /// <summary>
    /// Time of day affects spawning
    /// </summary>
    [Serializable]
    public class TimeOfDayModifier
    {
        public string Name = "Default";
        [Range(0f, 24f)] public float StartHour = 0f;
        [Range(0f, 24f)] public float EndHour = 24f;
        
        [Header("Density Multipliers")]
        [Range(0f, 3f)] public float NPCDensity = 1f;
        [Range(0f, 3f)] public float VehicleDensity = 1f;
        
        [Header("Event Modifiers")]
        [Range(0f, 3f)] public float EventRate = 1f;  // Generic event frequency modifier
        
        public bool IsActiveAtHour(float hour)
        {
            if (StartHour <= EndHour)
                return hour >= StartHour && hour < EndHour;
            else // Wraps around midnight
                return hour >= StartHour || hour < EndHour;
        }
        
        public static TimeOfDayModifier Default => new TimeOfDayModifier
        {
            Name = "Default",
            StartHour = 0f,
            EndHour = 24f,
            NPCDensity = 1f,
            VehicleDensity = 1f,
            EventRate = 1f
        };
    }
    
    /// <summary>
    /// Tracked spawned entity
    /// </summary>
    public class SpawnedEntity
    {
        public SimId EntityId;
        public NPCMovement Movement;  // Unified movement component
        public AIController AI;
        public string ProfileId;
        public string ZoneId;
        public float SpawnTime;
        public bool IsProtected; // Don't despawn (e.g., in conversation, in cutscene)
        
        // Backwards compatibility alias
        [Obsolete("Use Movement instead")]
        public NPCMovement Bridge => Movement;
    }
    
    /// <summary>
    /// Base class for world simulation.
    /// Handles spawn/despawn logic, zone awareness, time modifiers.
    /// Extend for game-specific behavior.
    /// 
    /// CONFIG: Set _config in derived class from your game's config source.
    /// </summary>
    public abstract class WorldSimulator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected Transform _playerTransform;
        
        [Header("Debug")]
        [SerializeField] protected bool _showDebug = true;
        [SerializeField] protected bool _logSpawns = false;
        
        // Config - set by derived class
        protected WorldSimConfigSO _config;
        
        // Config accessors with safe fallbacks
        protected int MaxNPCs => _config?.MaxNPCs ?? 20;
        protected int MaxVehicles => _config?.MaxVehicles ?? 10;
        protected float SpawnRadius => _config?.SpawnRadius ?? 40f;
        protected float DespawnRadius => _config?.DespawnRadius ?? 60f;
        protected float MinSpawnDistance => _config?.MinSpawnDistance ?? 15f;
        protected float SpawnInterval => _config?.SpawnInterval ?? 2f;
        protected float InitialBurstRatio => _config?.InitialBurstRatio ?? 0.5f;
        protected List<TimeOfDayModifier> TimeModifiers => _config?.TimeModifiers ?? new();
        
        // Core references
        protected SimWorld _world;
        protected bool _isActive;
        protected float _currentHour = 12f; // Noon by default
        
        // Spawn timers
        protected float _npcSpawnTimer;
        protected float _vehicleSpawnTimer;
        
        // Tracked entities
        protected readonly Dictionary<SimId, SpawnedEntity> _spawnedNPCs = new();
        protected readonly Dictionary<SimId, SpawnedEntity> _spawnedVehicles = new();
        protected readonly List<SimId> _toRemove = new();
        
        // Zones
        protected readonly List<IWorldZone> _zones = new();
        
        #region Lifecycle
        
        /// <summary>
        /// Initialize the world simulator
        /// </summary>
        public virtual void Initialize(SimWorld world)
        {
            _world = world;
            
            if (_playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    _playerTransform = player.transform;
            }
            
            // Let subclass register zones and content
            RegisterZones();
            RegisterContent();
        }
        
        /// <summary>
        /// Start world simulation
        /// </summary>
        public virtual void StartSimulation()
        {
            _isActive = true;
            _npcSpawnTimer = 0;
            _vehicleSpawnTimer = 0;
            
            // Initial burst spawn
            int initialNPCs = Mathf.RoundToInt(MaxNPCs * InitialBurstRatio);
            for (int i = 0; i < initialNPCs; i++)
            {
                TrySpawnNPC();
            }
            
            int initialVehicles = Mathf.RoundToInt(MaxVehicles * InitialBurstRatio);
            for (int i = 0; i < initialVehicles; i++)
            {
                TrySpawnVehicle();
            }
            
            if (_logSpawns)
                SimCoreLogger.Log($"[WorldSim] Started with {_spawnedNPCs.Count} NPCs, {_spawnedVehicles.Count} vehicles");
        }
        
        /// <summary>
        /// Stop world simulation and clear all spawned entities
        /// </summary>
        public virtual void StopSimulation()
        {
            _isActive = false;
            ClearAllEntities();
        }
        
        /// <summary>
        /// Set current time of day (0-24)
        /// </summary>
        public void SetTimeOfDay(float hour)
        {
            _currentHour = Mathf.Repeat(hour, 24f);
        }
        
        protected virtual void Update()
        {
            if (!_isActive || _world == null || _playerTransform == null) return;
            
            // Get current time modifier
            var timeModifier = GetCurrentTimeModifier();
            
            // Spawn NPCs
            int effectiveMaxNPCs = Mathf.RoundToInt(MaxNPCs * timeModifier.NPCDensity);
            _npcSpawnTimer += Time.deltaTime;
            if (_npcSpawnTimer >= SpawnInterval && _spawnedNPCs.Count < effectiveMaxNPCs)
            {
                _npcSpawnTimer = 0;
                TrySpawnNPC();
            }
            
            // Spawn Vehicles
            int effectiveMaxVehicles = Mathf.RoundToInt(MaxVehicles * timeModifier.VehicleDensity);
            _vehicleSpawnTimer += Time.deltaTime;
            if (_vehicleSpawnTimer >= SpawnInterval && _spawnedVehicles.Count < effectiveMaxVehicles)
            {
                _vehicleSpawnTimer = 0;
                TrySpawnVehicle();
            }
            
            // Update entities
            UpdateNPCs();
            UpdateVehicles();
            
            // Cleanup
            CleanupFarEntities();
        }
        
        #endregion
        
        #region Zone Management
        
        /// <summary>
        /// Register a zone
        /// </summary>
        public void RegisterZone(IWorldZone zone)
        {
            _zones.Add(zone);
        }
        
        /// <summary>
        /// Get zone at position
        /// </summary>
        public IWorldZone GetZoneAt(Vector3 position)
        {
            foreach (var zone in _zones)
            {
                if (zone.Contains(position))
                    return zone;
            }
            return null;
        }
        
        /// <summary>
        /// Get all registered zones
        /// </summary>
        public IReadOnlyList<IWorldZone> Zones => _zones;
        
        #endregion
        
        #region Spawn Logic
        
        protected virtual void TrySpawnNPC()
        {
            Vector3? spawnPos = FindSpawnPosition();
            if (!spawnPos.HasValue) return;
            
            var zone = GetZoneAt(spawnPos.Value);
            SpawnNPCAt(spawnPos.Value, zone);
        }
        
        protected virtual void TrySpawnVehicle()
        {
            Vector3? spawnPos = FindVehicleSpawnPosition();
            if (!spawnPos.HasValue) return;
            
            var zone = GetZoneAt(spawnPos.Value);
            SpawnVehicleAt(spawnPos.Value, zone);
        }
        
        protected virtual Vector3? FindSpawnPosition()
        {
            Vector3 playerPos = _playerTransform.position;
            
            // REQUIRED: Use Spawn Zones - NO FALLBACK
            var spawnZones = ZoneManager.Instance?.GetZonesOfType(ZoneType.PedestrianSpawn);
            
            if (spawnZones == null || spawnZones.Count == 0)
            {
                SimCoreLogger.LogError("[WorldSimulator] NO SPAWN ZONES FOUND! Add Zone components with Type=PedestrianSpawn to your scene.");
                return null;
            }
            
            // Get spawn zones within spawn radius
            var validZones = new List<Zone>();
            foreach (var zone in spawnZones)
            {
                float dist = Vector3.Distance(playerPos, zone.transform.position);
                if (dist >= MinSpawnDistance && dist <= SpawnRadius)
                {
                    validZones.Add(zone);
                }
            }
            
            if (validZones.Count == 0)
            {
                SimCoreLogger.LogWarning($"[WorldSimulator] No spawn zones within range ({MinSpawnDistance}-{SpawnRadius}m). Total zones: {spawnZones.Count}");
                return null;
            }
            
            // Pick random spawn zone
            var chosenZone = validZones[UnityEngine.Random.Range(0, validZones.Count)];
            
            // Get random point within zone bounds (from collider)
            var collider = chosenZone.GetComponent<Collider>();
            if (collider == null)
            {
                SimCoreLogger.LogError($"[WorldSimulator] Spawn zone {chosenZone.ZoneId} has no Collider!");
                return null;
            }
            
            var bounds = collider.bounds;
            for (int i = 0; i < 5; i++)
            {
                Vector3 randomPoint = new Vector3(
                    UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                    bounds.center.y,
                    UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
                );
                
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    if (!IsTooCloseToEntity(hit.position, _spawnedNPCs.Values, 3f))
                    {
                        if (_logSpawns)
                            SimCoreLogger.Log($"[WorldSimulator] Spawning at zone: {chosenZone.ZoneId}");
                        return hit.position;
                    }
                }
            }
            
            SimCoreLogger.LogWarning($"[WorldSimulator] Could not find valid NavMesh position in zone {chosenZone.ZoneId}");
            return null;
        }
        
        protected virtual Vector3? FindVehicleSpawnPosition()
        {
            // Default: same as NPC but with larger spacing
            // Override in subclass for road-based spawning
            Vector3 playerPos = _playerTransform.position;
            
            for (int i = 0; i < 10; i++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = UnityEngine.Random.Range(MinSpawnDistance, SpawnRadius);
                
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0,
                    Mathf.Sin(angle) * distance
                );
                
                Vector3 testPos = playerPos + offset;
                
                // Vehicles need more space
                if (!IsTooCloseToEntity(testPos, _spawnedVehicles.Values, 8f))
                    return testPos;
            }
            
            return null;
        }
        
        protected bool IsTooCloseToEntity(Vector3 position, IEnumerable<SpawnedEntity> entities, float minDistance)
        {
            foreach (var entity in entities)
            {
                if (entity.Movement != null)
                {
                    float dist = Vector3.Distance(position, entity.Movement.transform.position);
                    if (dist < minDistance)
                        return true;
                }
            }
            return false;
        }
        
        protected virtual void CleanupFarEntities()
        {
            Vector3 playerPos = _playerTransform.position;
            _toRemove.Clear();
            
            // NPCs
            foreach (var kvp in _spawnedNPCs)
            {
                var entity = kvp.Value;
                if (entity.IsProtected) continue;
                
                if (entity.Movement == null)
                {
                    _toRemove.Add(kvp.Key);
                    continue;
                }
                
                float distance = Vector3.Distance(playerPos, entity.Movement.transform.position);
                if (distance > DespawnRadius)
                {
                    _toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var id in _toRemove)
            {
                DespawnNPC(id);
            }
            
            // Vehicles
            _toRemove.Clear();
            foreach (var kvp in _spawnedVehicles)
            {
                var entity = kvp.Value;
                if (entity.IsProtected) continue;
                
                if (entity.Movement == null)
                {
                    _toRemove.Add(kvp.Key);
                    continue;
                }
                
                float distance = Vector3.Distance(playerPos, entity.Movement.transform.position);
                if (distance > DespawnRadius)
                {
                    _toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var id in _toRemove)
            {
                DespawnVehicle(id);
            }
        }
        
        protected virtual void DespawnNPC(SimId entityId)
        {
            if (_spawnedNPCs.TryGetValue(entityId, out var entity))
            {
                OnBeforeNPCDespawn(entity);
                
                if (entity.Movement != null && entity.Movement.gameObject != null)
                {
                    Destroy(entity.Movement.gameObject);
                }
                
                _spawnedNPCs.Remove(entityId);
                _world.Partition.RemoveEntity(entityId);
                _world.Entities.RemoveEntity(entityId);
                
                if (_logSpawns)
                    SimCoreLogger.Log($"[WorldSim] Despawned NPC {entityId}");
            }
        }
        
        protected virtual void DespawnVehicle(SimId entityId)
        {
            if (_spawnedVehicles.TryGetValue(entityId, out var entity))
            {
                OnBeforeVehicleDespawn(entity);
                
                if (entity.Movement != null && entity.Movement.gameObject != null)
                {
                    Destroy(entity.Movement.gameObject);
                }
                
                _spawnedVehicles.Remove(entityId);
                _world.Partition.RemoveEntity(entityId);
                _world.Entities.RemoveEntity(entityId);
                
                if (_logSpawns)
                    SimCoreLogger.Log($"[WorldSim] Despawned vehicle {entityId}");
            }
        }
        
        protected virtual void ClearAllEntities()
        {
            var npcIds = new List<SimId>(_spawnedNPCs.Keys);
            foreach (var id in npcIds)
            {
                DespawnNPC(id);
            }
            
            var vehicleIds = new List<SimId>(_spawnedVehicles.Keys);
            foreach (var id in vehicleIds)
            {
                DespawnVehicle(id);
            }
        }
        
        #endregion
        
        #region Time of Day
        
        protected TimeOfDayModifier GetCurrentTimeModifier()
        {
            foreach (var modifier in TimeModifiers)
            {
                if (IsTimeInRange(_currentHour, modifier.StartHour, modifier.EndHour))
                    return modifier;
            }
            
            // Default modifier
            return new TimeOfDayModifier();
        }
        
        private bool IsTimeInRange(float hour, float start, float end)
        {
            if (start <= end)
                return hour >= start && hour < end;
            else
                return hour >= start || hour < end; // Wraps midnight
        }
        
        #endregion
        
        #region Accessors
        
        public SpawnedEntity GetNPC(SimId entityId)
        {
            return _spawnedNPCs.TryGetValue(entityId, out var entity) ? entity : null;
        }
        
        public SpawnedEntity GetVehicle(SimId entityId)
        {
            return _spawnedVehicles.TryGetValue(entityId, out var entity) ? entity : null;
        }
        
        /// <summary>
        /// Mark entity as protected (won't despawn)
        /// </summary>
        public void ProtectEntity(SimId entityId, bool protect)
        {
            if (_spawnedNPCs.TryGetValue(entityId, out var npc))
            {
                npc.IsProtected = protect;
            }
            else if (_spawnedVehicles.TryGetValue(entityId, out var vehicle))
            {
                vehicle.IsProtected = protect;
            }
        }
        
        public int ActiveNPCCount => _spawnedNPCs.Count;
        public int ActiveVehicleCount => _spawnedVehicles.Count;
        
        #endregion
        
        #region Abstract - Implement in Game
        
        /// <summary>
        /// Register zones (districts, regions, etc.)
        /// </summary>
        protected abstract void RegisterZones();
        
        /// <summary>
        /// Register content (profiles, prefabs, etc.)
        /// </summary>
        protected abstract void RegisterContent();
        
        /// <summary>
        /// Spawn NPC at position with zone context
        /// </summary>
        protected abstract void SpawnNPCAt(Vector3 position, IWorldZone zone);
        
        /// <summary>
        /// Spawn vehicle at position with zone context
        /// </summary>
        protected abstract void SpawnVehicleAt(Vector3 position, IWorldZone zone);
        
        /// <summary>
        /// Update NPCs (tick AI, check behaviors)
        /// </summary>
        protected abstract void UpdateNPCs();
        
        /// <summary>
        /// Update vehicles
        /// </summary>
        protected abstract void UpdateVehicles();
        
        /// <summary>
        /// Called before NPC despawns
        /// </summary>
        protected virtual void OnBeforeNPCDespawn(SpawnedEntity entity) { }
        
        /// <summary>
        /// Called before vehicle despawns
        /// </summary>
        protected virtual void OnBeforeVehicleDespawn(SpawnedEntity entity) { }
        
        #endregion
        
        #region Debug
        
        protected virtual void OnDrawGizmosSelected()
        {
            if (!_showDebug) return;
            
            Vector3 center = _playerTransform != null ? _playerTransform.position : transform.position;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, SpawnRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, DespawnRadius);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, MinSpawnDistance);
            
            // Draw zones
            Gizmos.color = Color.cyan;
            foreach (var zone in _zones)
            {
                Gizmos.DrawWireSphere(zone.Center, zone.Radius);
            }
        }
        
        #endregion
    }
}

