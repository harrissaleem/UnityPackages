// SimCore - World Partition
// Abstraction for spatial queries independent of world layout
// OPTIMIZED: Uses spatial hashing for O(1) range queries

using System;
using System.Collections.Generic;
using UnityEngine;
using SimCore.Signals;

namespace SimCore.World
{
    /// <summary>
    /// Spatial hash grid for efficient range queries - O(1) cell lookup vs O(n) linear scan
    /// </summary>
    public class SpatialHashGrid
    {
        private readonly float _cellSize;
        private readonly float _inverseCellSize;
        private readonly Dictionary<long, HashSet<SimId>> _cells = new Dictionary<long, HashSet<SimId>>();
        private readonly Dictionary<SimId, long> _entityCells = new Dictionary<SimId, long>();
        
        // Cached collections to avoid allocations during queries
        private readonly List<SimId> _queryResultCache = new List<SimId>(64);
        private readonly HashSet<long> _cellsToCheckCache = new HashSet<long>();
        
        // Object pool for HashSets
        private readonly Stack<HashSet<SimId>> _hashSetPool = new Stack<HashSet<SimId>>();
        
        public SpatialHashGrid(float cellSize = 10f)
        {
            _cellSize = cellSize;
            _inverseCellSize = 1f / cellSize;
        }
        
        private long GetCellKey(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x * _inverseCellSize);
            int z = Mathf.FloorToInt(position.z * _inverseCellSize);
            // Combine x and z into a single long key (supports ~2 billion cells in each direction)
            return ((long)x << 32) | (uint)z;
        }
        
        private HashSet<SimId> GetOrCreateCell(long key)
        {
            if (!_cells.TryGetValue(key, out var cell))
            {
                cell = _hashSetPool.Count > 0 ? _hashSetPool.Pop() : new HashSet<SimId>();
                cell.Clear();
                _cells[key] = cell;
            }
            return cell;
        }
        
        public void Add(SimId entityId, Vector3 position)
        {
            long key = GetCellKey(position);
            var cell = GetOrCreateCell(key);
            cell.Add(entityId);
            _entityCells[entityId] = key;
        }
        
        public void Update(SimId entityId, Vector3 newPosition)
        {
            long newKey = GetCellKey(newPosition);
            
            if (_entityCells.TryGetValue(entityId, out long oldKey))
            {
                if (oldKey == newKey) return; // Same cell, no update needed
                
                // Remove from old cell
                if (_cells.TryGetValue(oldKey, out var oldCell))
                {
                    oldCell.Remove(entityId);
                    // Return empty cells to pool
                    if (oldCell.Count == 0)
                    {
                        _cells.Remove(oldKey);
                        _hashSetPool.Push(oldCell);
                    }
                }
            }
            
            // Add to new cell
            var cell = GetOrCreateCell(newKey);
            cell.Add(entityId);
            _entityCells[entityId] = newKey;
        }
        
        public void Remove(SimId entityId)
        {
            if (_entityCells.TryGetValue(entityId, out long key))
            {
                if (_cells.TryGetValue(key, out var cell))
                {
                    cell.Remove(entityId);
                    if (cell.Count == 0)
                    {
                        _cells.Remove(key);
                        _hashSetPool.Push(cell);
                    }
                }
                _entityCells.Remove(entityId);
            }
        }
        
        /// <summary>
        /// Get all entities within radius of center position
        /// Returns cached list - do not store reference
        /// </summary>
        public List<SimId> GetEntitiesInRange(Vector3 center, float radius, Dictionary<SimId, Vector3> positions)
        {
            _queryResultCache.Clear();
            float radiusSq = radius * radius;
            
            // Calculate cell range to check
            int minX = Mathf.FloorToInt((center.x - radius) * _inverseCellSize);
            int maxX = Mathf.FloorToInt((center.x + radius) * _inverseCellSize);
            int minZ = Mathf.FloorToInt((center.z - radius) * _inverseCellSize);
            int maxZ = Mathf.FloorToInt((center.z + radius) * _inverseCellSize);
            
            // Check all cells in range
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    long key = ((long)x << 32) | (uint)z;
                    if (_cells.TryGetValue(key, out var cell))
                    {
                        foreach (var entityId in cell)
                        {
                            if (positions.TryGetValue(entityId, out var pos))
                            {
                                if ((pos - center).sqrMagnitude <= radiusSq)
                                {
                                    _queryResultCache.Add(entityId);
                                }
                            }
                        }
                    }
                }
            }
            
            return _queryResultCache;
        }
        
        public void Clear()
        {
            foreach (var cell in _cells.Values)
            {
                cell.Clear();
                _hashSetPool.Push(cell);
            }
            _cells.Clear();
            _entityCells.Clear();
        }
    }

    /// <summary>
    /// Interface for world partition systems
    /// </summary>
    public interface IWorldPartition
    {
        /// <summary>
        /// Register entity position
        /// </summary>
        void RegisterEntity(SimId entityId, Vector3 position);
        
        /// <summary>
        /// Update entity position
        /// </summary>
        void UpdatePosition(SimId entityId, Vector3 position);
        
        /// <summary>
        /// Remove entity from partition
        /// </summary>
        void RemoveEntity(SimId entityId);
        
        /// <summary>
        /// Check if entity is registered in partition
        /// </summary>
        bool IsEntityRegistered(SimId entityId);
        
        /// <summary>
        /// Get entity position
        /// </summary>
        Vector3 GetPosition(SimId entityId);
        
        /// <summary>
        /// Get distance between entities
        /// </summary>
        float GetDistance(SimId entityA, SimId entityB);
        
        /// <summary>
        /// Check if entity is in an area
        /// </summary>
        bool IsInArea(SimId entityId, ContentId areaId);
        
        /// <summary>
        /// Get current area for entity
        /// </summary>
        ContentId GetCurrentArea(SimId entityId);
        
        /// <summary>
        /// Get entities within range
        /// </summary>
        IEnumerable<SimId> GetEntitiesInRange(Vector3 center, float radius);
        
        /// <summary>
        /// Get entities within range of another entity
        /// </summary>
        IEnumerable<SimId> GetEntitiesInRange(SimId centerId, float radius);
        
        /// <summary>
        /// Get entities in an area
        /// </summary>
        IEnumerable<SimId> GetEntitiesInArea(ContentId areaId);
        
        /// <summary>
        /// Check line of sight between entities
        /// </summary>
        bool HasLineOfSight(SimId from, SimId to, int layerMask = ~0);
        
        /// <summary>
        /// Raycast from entity
        /// </summary>
        bool Raycast(SimId from, Vector3 direction, float maxDistance, out RaycastHit hit, int layerMask = ~0);
    }
    
    /// <summary>
    /// Simple grid-based world partition
    /// Works for open worlds and zone-based maps
    /// OPTIMIZED: Uses spatial hash grid for O(1) range queries
    /// </summary>
    public class SimpleWorldPartition : IWorldPartition
    {
        private readonly Dictionary<SimId, Vector3> _positions = new();
        private readonly Dictionary<SimId, ContentId> _entityAreas = new();
        private readonly Dictionary<ContentId, AreaDef> _areas = new();
        private readonly SignalBus _signalBus;
        
        // Spatial hash for efficient range queries
        private readonly SpatialHashGrid _spatialHash;
        
        // Cached list for range query results
        private readonly List<SimId> _rangeQueryCache = new List<SimId>(64);
        
        public SimpleWorldPartition(SignalBus signalBus, float spatialCellSize = 10f)
        {
            _signalBus = signalBus;
            _spatialHash = new SpatialHashGrid(spatialCellSize);
        }
        
        /// <summary>
        /// Register an area
        /// </summary>
        public void RegisterArea(AreaDef area)
        {
            _areas[area.Id] = area;
        }
        
        public void RegisterEntity(SimId entityId, Vector3 position)
        {
            _positions[entityId] = position;
            _spatialHash.Add(entityId, position);
            UpdateAreaForEntity(entityId, position);
        }
        
        public void UpdatePosition(SimId entityId, Vector3 position)
        {
            var oldPosition = GetPosition(entityId);
            _positions[entityId] = position;
            _spatialHash.Update(entityId, position);
            
            // Check area transitions
            var oldArea = _entityAreas.TryGetValue(entityId, out var area) ? area : ContentId.Invalid;
            UpdateAreaForEntity(entityId, position);
            var newArea = _entityAreas.TryGetValue(entityId, out area) ? area : ContentId.Invalid;
            
            if (oldArea != newArea)
            {
                if (oldArea.IsValid)
                {
                    _signalBus?.Publish(new AreaExitedSignal
                    {
                        EntityId = entityId,
                        AreaId = oldArea
                    });
                }
                
                if (newArea.IsValid)
                {
                    _signalBus?.Publish(new AreaEnteredSignal
                    {
                        EntityId = entityId,
                        AreaId = newArea
                    });
                }
            }
        }
        
        public void RemoveEntity(SimId entityId)
        {
            _positions.Remove(entityId);
            _entityAreas.Remove(entityId);
            _spatialHash.Remove(entityId);
        }
        
        public bool IsEntityRegistered(SimId entityId)
        {
            return _positions.ContainsKey(entityId);
        }
        
        public Vector3 GetPosition(SimId entityId)
        {
            return _positions.TryGetValue(entityId, out var pos) ? pos : Vector3.zero;
        }
        
        public float GetDistance(SimId entityA, SimId entityB)
        {
            var posA = GetPosition(entityA);
            var posB = GetPosition(entityB);
            return Vector3.Distance(posA, posB);
        }
        
        public bool IsInArea(SimId entityId, ContentId areaId)
        {
            if (!_areas.TryGetValue(areaId, out var area))
                return false;
            
            var pos = GetPosition(entityId);
            return area.Contains(pos);
        }
        
        public ContentId GetCurrentArea(SimId entityId)
        {
            return _entityAreas.TryGetValue(entityId, out var area) ? area : ContentId.Invalid;
        }
        
        /// <summary>
        /// Get entities in range - OPTIMIZED using spatial hash
        /// </summary>
        public IEnumerable<SimId> GetEntitiesInRange(Vector3 center, float radius)
        {
            // Use spatial hash for O(1) cell lookup instead of O(n) linear scan
            var results = _spatialHash.GetEntitiesInRange(center, radius, _positions);
            for (int i = 0; i < results.Count; i++)
            {
                yield return results[i];
            }
        }
        
        public IEnumerable<SimId> GetEntitiesInRange(SimId centerId, float radius)
        {
            var center = GetPosition(centerId);
            var results = _spatialHash.GetEntitiesInRange(center, radius, _positions);
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i] != centerId)
                    yield return results[i];
            }
        }
        
        /// <summary>
        /// Get entities in range as a list - avoids IEnumerable allocation overhead
        /// Returns cached list - do not store reference
        /// </summary>
        public List<SimId> GetEntitiesInRangeList(Vector3 center, float radius)
        {
            return _spatialHash.GetEntitiesInRange(center, radius, _positions);
        }
        
        public IEnumerable<SimId> GetEntitiesInArea(ContentId areaId)
        {
            foreach (var kvp in _entityAreas)
            {
                if (kvp.Value == areaId)
                    yield return kvp.Key;
            }
        }
        
        public bool HasLineOfSight(SimId from, SimId to, int layerMask = ~0)
        {
            var posFrom = GetPosition(from);
            var posTo = GetPosition(to);
            var direction = posTo - posFrom;
            float distance = direction.magnitude;
            
            return !Physics.Raycast(posFrom, direction.normalized, distance, layerMask);
        }
        
        public bool Raycast(SimId from, Vector3 direction, float maxDistance, out RaycastHit hit, int layerMask = ~0)
        {
            var origin = GetPosition(from);
            return Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);
        }
        
        private void UpdateAreaForEntity(SimId entityId, Vector3 position)
        {
            ContentId foundArea = ContentId.Invalid;
            
            foreach (var area in _areas.Values)
            {
                if (area.Contains(position))
                {
                    foundArea = area.Id;
                    break; // Take first matching area
                }
            }
            
            if (foundArea.IsValid)
                _entityAreas[entityId] = foundArea;
            else
                _entityAreas.Remove(entityId);
        }
        
        /// <summary>
        /// Clear all data
        /// </summary>
        public void Clear()
        {
            _positions.Clear();
            _entityAreas.Clear();
            _spatialHash.Clear();
        }
    }
    
    /// <summary>
    /// Definition of an area/region
    /// </summary>
    [Serializable]
    public class AreaDef
    {
        public ContentId Id;
        public string DisplayName;
        public AreaShape Shape = AreaShape.Box;
        public Vector3 Center;
        public Vector3 Size;   // For box
        public float Radius;   // For sphere
        
        public bool Contains(Vector3 point)
        {
            return Shape switch
            {
                AreaShape.Box => ContainsBox(point),
                AreaShape.Sphere => ContainsSphere(point),
                _ => false
            };
        }
        
        private bool ContainsBox(Vector3 point)
        {
            var min = Center - Size * 0.5f;
            var max = Center + Size * 0.5f;
            return point.x >= min.x && point.x <= max.x &&
                   point.y >= min.y && point.y <= max.y &&
                   point.z >= min.z && point.z <= max.z;
        }
        
        private bool ContainsSphere(Vector3 point)
        {
            return (point - Center).sqrMagnitude <= Radius * Radius;
        }
    }
    
    public enum AreaShape
    {
        Box,
        Sphere
    }
    
    /// <summary>
    /// Zone-based partition (for zone-based games)
    /// OPTIMIZED: Uses spatial hash for efficient range queries
    /// </summary>
    public class ZoneWorldPartition : IWorldPartition
    {
        private readonly Dictionary<SimId, ContentId> _entityZones = new();
        private readonly Dictionary<ContentId, HashSet<SimId>> _zoneEntities = new();
        private readonly Dictionary<SimId, Vector3> _positions = new();
        private readonly SignalBus _signalBus;
        
        // Spatial hash for efficient range queries
        private readonly SpatialHashGrid _spatialHash;
        
        public ZoneWorldPartition(SignalBus signalBus, float spatialCellSize = 10f)
        {
            _signalBus = signalBus;
            _spatialHash = new SpatialHashGrid(spatialCellSize);
        }
        
        /// <summary>
        /// Register a zone
        /// </summary>
        public void RegisterZone(ContentId zoneId)
        {
            if (!_zoneEntities.ContainsKey(zoneId))
                _zoneEntities[zoneId] = new HashSet<SimId>();
        }
        
        /// <summary>
        /// Move entity to zone
        /// </summary>
        public void MoveToZone(SimId entityId, ContentId zoneId)
        {
            var oldZone = _entityZones.TryGetValue(entityId, out var zone) ? zone : ContentId.Invalid;
            
            // Remove from old zone
            if (oldZone.IsValid && _zoneEntities.TryGetValue(oldZone, out var oldSet))
            {
                oldSet.Remove(entityId);
                _signalBus?.Publish(new AreaExitedSignal { EntityId = entityId, AreaId = oldZone });
            }
            
            // Add to new zone
            _entityZones[entityId] = zoneId;
            if (_zoneEntities.TryGetValue(zoneId, out var newSet))
            {
                newSet.Add(entityId);
                _signalBus?.Publish(new AreaEnteredSignal { EntityId = entityId, AreaId = zoneId });
            }
        }
        
        public void RegisterEntity(SimId entityId, Vector3 position)
        {
            _positions[entityId] = position;
            _spatialHash.Add(entityId, position);
        }
        
        public void UpdatePosition(SimId entityId, Vector3 position)
        {
            _positions[entityId] = position;
            _spatialHash.Update(entityId, position);
        }
        
        public void RemoveEntity(SimId entityId)
        {
            if (_entityZones.TryGetValue(entityId, out var zone))
            {
                if (_zoneEntities.TryGetValue(zone, out var set))
                    set.Remove(entityId);
                _entityZones.Remove(entityId);
            }
            _positions.Remove(entityId);
            _spatialHash.Remove(entityId);
        }

        public bool IsEntityRegistered(SimId entityId)
        {
            return _positions.ContainsKey(entityId);
        }

        public Vector3 GetPosition(SimId entityId)
        {
            return _positions.TryGetValue(entityId, out var pos) ? pos : Vector3.zero;
        }
        
        public float GetDistance(SimId entityA, SimId entityB)
        {
            // In zone-based, distance within zone uses position
            // Distance across zones is effectively infinite
            var zoneA = _entityZones.TryGetValue(entityA, out var za) ? za : ContentId.Invalid;
            var zoneB = _entityZones.TryGetValue(entityB, out var zb) ? zb : ContentId.Invalid;
            
            if (zoneA != zoneB)
                return float.MaxValue;
            
            return Vector3.Distance(GetPosition(entityA), GetPosition(entityB));
        }
        
        public bool IsInArea(SimId entityId, ContentId areaId)
        {
            return _entityZones.TryGetValue(entityId, out var zone) && zone == areaId;
        }
        
        public ContentId GetCurrentArea(SimId entityId)
        {
            return _entityZones.TryGetValue(entityId, out var zone) ? zone : ContentId.Invalid;
        }
        
        /// <summary>
        /// Get entities in range - OPTIMIZED using spatial hash
        /// </summary>
        public IEnumerable<SimId> GetEntitiesInRange(Vector3 center, float radius)
        {
            var results = _spatialHash.GetEntitiesInRange(center, radius, _positions);
            for (int i = 0; i < results.Count; i++)
            {
                yield return results[i];
            }
        }
        
        public IEnumerable<SimId> GetEntitiesInRange(SimId centerId, float radius)
        {
            // Only consider entities in same zone
            if (!_entityZones.TryGetValue(centerId, out var zone))
                yield break;
            
            var center = GetPosition(centerId);
            var results = _spatialHash.GetEntitiesInRange(center, radius, _positions);
            
            for (int i = 0; i < results.Count; i++)
            {
                var id = results[i];
                if (id == centerId) continue;
                
                // Only include entities in the same zone
                if (_entityZones.TryGetValue(id, out var entityZone) && entityZone == zone)
                {
                    yield return id;
                }
            }
        }
        
        public IEnumerable<SimId> GetEntitiesInArea(ContentId areaId)
        {
            if (_zoneEntities.TryGetValue(areaId, out var entities))
            {
                foreach (var id in entities)
                    yield return id;
            }
        }
        
        public bool HasLineOfSight(SimId from, SimId to, int layerMask = ~0)
        {
            // Only LOS within same zone
            var zoneFrom = _entityZones.TryGetValue(from, out var zf) ? zf : ContentId.Invalid;
            var zoneTo = _entityZones.TryGetValue(to, out var zt) ? zt : ContentId.Invalid;
            
            if (zoneFrom != zoneTo) return false;
            
            var posFrom = GetPosition(from);
            var posTo = GetPosition(to);
            var direction = posTo - posFrom;
            
            return !Physics.Raycast(posFrom, direction.normalized, direction.magnitude, layerMask);
        }
        
        public bool Raycast(SimId from, Vector3 direction, float maxDistance, out RaycastHit hit, int layerMask = ~0)
        {
            var origin = GetPosition(from);
            return Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);
        }
        
        public void Clear()
        {
            _entityZones.Clear();
            foreach (var set in _zoneEntities.Values) set.Clear();
            _positions.Clear();
            _spatialHash.Clear();
        }
    }
}

