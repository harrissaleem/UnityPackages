// SimCore - Zone Manager
// Central registry for all zones, provides spatial queries

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimCore.World.Zones
{
    /// <summary>
    /// Manages all zones in the scene. Provides efficient queries.
    /// Singleton pattern for easy access from NPCs.
    /// Uses DefaultExecutionOrder(-100) to initialize before Zone components.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ZoneManager : MonoBehaviour
    {
        public static ZoneManager Instance { get; private set; }
        
        [Header("Configuration")]
        [SerializeField] private bool _logDebug = false;
        
        // Zone registries
        private Dictionary<string, Zone> _zonesById = new Dictionary<string, Zone>();
        private Dictionary<ZoneType, List<Zone>> _zonesByType = new Dictionary<ZoneType, List<Zone>>();
        
        // Cached lists for queries
        private List<Zone> _tempZoneList = new List<Zone>();
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            // Initialize type dictionary
            foreach (ZoneType type in System.Enum.GetValues(typeof(ZoneType)))
            {
                _zonesByType[type] = new List<Zone>();
            }
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        /// <summary>
        /// Register a zone with the manager
        /// </summary>
        public void RegisterZone(Zone zone)
        {
            if (zone == null) return;
            
            // Zone tracks its own registration state, but double-check here
            if (_zonesById.ContainsKey(zone.ZoneId))
            {
                if (_logDebug)
                    SimCoreLogger.LogWarning($"[ZoneManager] Zone already registered: {zone.ZoneId}");
                return;
            }
            
            _zonesById[zone.ZoneId] = zone;
            _zonesByType[zone.ZoneType].Add(zone);
            
            if (_logDebug)
            {
                SimCoreLogger.Log($"[ZoneManager] Registered zone: {zone.ZoneId} ({zone.ZoneType})");
            }
        }
        
        /// <summary>
        /// Unregister a zone
        /// </summary>
        public void UnregisterZone(Zone zone)
        {
            if (zone == null) return;
            
            _zonesById.Remove(zone.ZoneId);
            
            if (_zonesByType.TryGetValue(zone.ZoneType, out var list))
            {
                list.Remove(zone);
            }
            
            if (_logDebug)
            {
                SimCoreLogger.Log($"[ZoneManager] Unregistered zone: {zone.ZoneId}");
            }
        }
        
        /// <summary>
        /// Get a zone by ID
        /// </summary>
        public Zone GetZone(string zoneId)
        {
            _zonesById.TryGetValue(zoneId, out var zone);
            return zone;
        }
        
        /// <summary>
        /// Get all zones of a specific type
        /// </summary>
        public IReadOnlyList<Zone> GetZonesOfType(ZoneType type)
        {
            if (_zonesByType.TryGetValue(type, out var list))
            {
                return list;
            }
            return System.Array.Empty<Zone>();
        }
        
        /// <summary>
        /// Get zones within a radius of a position
        /// </summary>
        public List<Zone> GetZonesInRadius(Vector3 position, float radius, ZoneType? filterType = null)
        {
            _tempZoneList.Clear();
            float radiusSq = radius * radius;
            
            IEnumerable<Zone> zones = filterType.HasValue 
                ? GetZonesOfType(filterType.Value) 
                : (IEnumerable<Zone>)_zonesById.Values;
            
            foreach (var zone in zones)
            {
                if (zone == null) continue;
                
                float distSq = (zone.transform.position - position).sqrMagnitude;
                if (distSq <= radiusSq)
                {
                    _tempZoneList.Add(zone);
                }
            }
            
            return _tempZoneList;
        }
        
        /// <summary>
        /// Get the nearest zone of a type
        /// </summary>
        public Zone GetNearestZone(Vector3 position, ZoneType type)
        {
            var zones = GetZonesOfType(type);
            Zone nearest = null;
            float nearestDistSq = float.MaxValue;
            
            foreach (var zone in zones)
            {
                if (zone == null) continue;
                
                float distSq = (zone.transform.position - position).sqrMagnitude;
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = zone;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Get a random zone of a specific type
        /// </summary>
        public Zone GetRandomZoneOfType(ZoneType type)
        {
            var zones = GetZonesOfType(type);
            if (zones.Count == 0) return null;
            return zones[Random.Range(0, zones.Count)];
        }
        
        /// <summary>
        /// Get a random spawn zone for pedestrians
        /// </summary>
        public Zone GetRandomPedestrianSpawn(Vector3 nearPosition, float maxDistance = 100f)
        {
            var spawns = GetZonesInRadius(nearPosition, maxDistance, ZoneType.PedestrianSpawn);
            if (spawns.Count == 0) return null;
            return spawns[Random.Range(0, spawns.Count)];
        }
        
        /// <summary>
        /// Get a random spawn zone for vehicles
        /// </summary>
        public Zone GetRandomVehicleSpawn(Vector3 nearPosition, float maxDistance = 100f)
        {
            var spawns = GetZonesInRadius(nearPosition, maxDistance, ZoneType.VehicleSpawn);
            if (spawns.Count == 0) return null;
            return spawns[Random.Range(0, spawns.Count)];
        }
        
        /// <summary>
        /// Check if any crossing zone near a position has pedestrians
        /// Vehicles use this to decide whether to slow down
        /// </summary>
        public bool AnyCrossingHasPedestrians(Vector3 position, float checkRadius = 20f)
        {
            var crossings = GetZonesInRadius(position, checkRadius, ZoneType.Crossing);
            foreach (var crossing in crossings)
            {
                if (crossing.HasPedestrians)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get the closest crossing with its occupancy state
        /// </summary>
        public (CrossingZone zone, float distance) GetNearestCrossing(Vector3 position)
        {
            var crossings = GetZonesOfType(ZoneType.Crossing);
            CrossingZone nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var z in crossings)
            {
                if (z is CrossingZone crossing)
                {
                    float dist = Vector3.Distance(position, crossing.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = crossing;
                    }
                }
            }
            
            return (nearest, nearestDist);
        }
        
        /// <summary>
        /// Debug: Get zone statistics
        /// </summary>
        public string GetDebugStats()
        {
            int totalZones = _zonesById.Count;
            int crossings = GetZonesOfType(ZoneType.Crossing).Count;
            int spawns = GetZonesOfType(ZoneType.PedestrianSpawn).Count + GetZonesOfType(ZoneType.VehicleSpawn).Count;
            
            return $"Zones: {totalZones} (Crossings: {crossings}, Spawns: {spawns})";
        }
    }
}

