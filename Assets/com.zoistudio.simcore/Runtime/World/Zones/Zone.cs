// SimCore - Zone Base Class
// Zones are trigger areas that manage NPC interactions efficiently
// NPCs only care about the zone they're in, not other NPCs

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimCore.World.Zones
{
    /// <summary>
    /// Base class for all zones. Zones track occupancy and publish events.
    /// This allows O(1) interaction checks instead of O(n²) NPC-to-NPC checks.
    /// </summary>
    public class Zone : MonoBehaviour
    {
        [Header("Zone Configuration")]
        [SerializeField] protected ZoneType _zoneType = ZoneType.None;
        [SerializeField] protected string _zoneId;
        
        [Header("Debug")]
        [SerializeField] protected bool _showDebug = false;
        
        // Occupancy tracking - separate lists for performance
        protected HashSet<int> _pedestriansInZone = new HashSet<int>();
        protected HashSet<int> _vehiclesInZone = new HashSet<int>();
        
        // Events
        public event Action<Zone, GameObject> OnPedestrianEntered;
        public event Action<Zone, GameObject> OnPedestrianExited;
        public event Action<Zone, GameObject> OnVehicleEntered;
        public event Action<Zone, GameObject> OnVehicleExited;
        
        // Properties
        public ZoneType ZoneType => _zoneType;
        public string ZoneId => _zoneId;
        public int PedestrianCount => _pedestriansInZone.Count;
        public int VehicleCount => _vehiclesInZone.Count;
        public bool HasPedestrians => _pedestriansInZone.Count > 0;
        public bool HasVehicles => _vehiclesInZone.Count > 0;
        public bool IsOccupied => HasPedestrians || HasVehicles;
        
        private bool _isRegistered = false;
        
        protected virtual void Awake()
        {
            // Generate unique ID if not set in inspector
            // Always use instance ID to guarantee uniqueness
            if (string.IsNullOrWhiteSpace(_zoneId))
            {
                _zoneId = $"{_zoneType}_{GetInstanceID()}";
            }
            else
            {
                // User set a custom ID - append instance ID to ensure uniqueness
                // This prevents two zones with the same manual ID from conflicting
                _zoneId = $"{_zoneId}_{GetInstanceID()}";
            }
        }
        
        protected virtual void OnEnable()
        {
            if (_isRegistered) return;
            
            if (ZoneManager.Instance != null)
            {
                RegisterWithManager();
            }
            else
            {
                StartCoroutine(DeferredRegister());
            }
        }
        
        private void RegisterWithManager()
        {
            if (_isRegistered) return;
            ZoneManager.Instance?.RegisterZone(this);
            _isRegistered = true;
        }
        
        private System.Collections.IEnumerator DeferredRegister()
        {
            yield return null;
            if (!_isRegistered)
            {
                RegisterWithManager();
            }
        }
        
        protected virtual void OnDisable()
        {
            if (_isRegistered)
            {
                ZoneManager.Instance?.UnregisterZone(this);
                _isRegistered = false;
            }
        }
        
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Pedestrian") || other.CompareTag("NPC"))
            {
                int id = other.GetInstanceID();
                if (_pedestriansInZone.Add(id))
                {
                    OnPedestrianEnter(other.gameObject);
                    OnPedestrianEntered?.Invoke(this, other.gameObject);
                    
                    if (_showDebug)
                        SimCoreLogger.Log($"[Zone:{_zoneId}] Pedestrian entered. Count: {PedestrianCount}");
                }
            }
            else if (other.CompareTag("Vehicle"))
            {
                int id = other.GetInstanceID();
                if (_vehiclesInZone.Add(id))
                {
                    OnVehicleEnter(other.gameObject);
                    OnVehicleEntered?.Invoke(this, other.gameObject);
                    
                    if (_showDebug)
                        SimCoreLogger.Log($"[Zone:{_zoneId}] Vehicle entered. Count: {VehicleCount}");
                }
            }
        }
        
        protected virtual void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Pedestrian") || other.CompareTag("NPC"))
            {
                int id = other.GetInstanceID();
                if (_pedestriansInZone.Remove(id))
                {
                    OnPedestrianExit(other.gameObject);
                    OnPedestrianExited?.Invoke(this, other.gameObject);
                    
                    if (_showDebug)
                        SimCoreLogger.Log($"[Zone:{_zoneId}] Pedestrian exited. Count: {PedestrianCount}");
                }
            }
            else if (other.CompareTag("Vehicle"))
            {
                int id = other.GetInstanceID();
                if (_vehiclesInZone.Remove(id))
                {
                    OnVehicleExit(other.gameObject);
                    OnVehicleExited?.Invoke(this, other.gameObject);
                    
                    if (_showDebug)
                        SimCoreLogger.Log($"[Zone:{_zoneId}] Vehicle exited. Count: {VehicleCount}");
                }
            }
        }
        
        // Override these in subclasses for specific behavior
        protected virtual void OnPedestrianEnter(GameObject pedestrian) { }
        protected virtual void OnPedestrianExit(GameObject pedestrian) { }
        protected virtual void OnVehicleEnter(GameObject vehicle) { }
        protected virtual void OnVehicleExit(GameObject vehicle) { }
        
        /// <summary>
        /// Check if a specific entity is in this zone
        /// </summary>
        public bool ContainsEntity(GameObject entity)
        {
            int id = entity.GetInstanceID();
            return _pedestriansInZone.Contains(id) || _vehiclesInZone.Contains(id);
        }
        
        /// <summary>
        /// Force remove an entity (e.g., when despawning)
        /// </summary>
        public void ForceRemove(GameObject entity)
        {
            int id = entity.GetInstanceID();
            _pedestriansInZone.Remove(id);
            _vehiclesInZone.Remove(id);
        }
        
        protected virtual void OnDrawGizmos()
        {
            if (!_showDebug) return;
            
            Gizmos.color = _zoneType switch
            {
                ZoneType.Crossing => new Color(1f, 1f, 0f, 0.3f),      // Yellow
                ZoneType.Intersection => new Color(1f, 0.5f, 0f, 0.3f), // Orange
                ZoneType.PedestrianSpawn => new Color(0f, 1f, 0f, 0.3f), // Green
                ZoneType.VehicleSpawn => new Color(0f, 0f, 1f, 0.3f),   // Blue
                ZoneType.Despawn => new Color(1f, 0f, 0f, 0.3f),        // Red
                _ => new Color(0.5f, 0.5f, 0.5f, 0.3f)                  // Gray
            };
            
            var collider = GetComponent<BoxCollider>();
            if (collider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(collider.center, collider.size);
                Gizmos.DrawWireCube(collider.center, collider.size);
            }
        }
    }
}

