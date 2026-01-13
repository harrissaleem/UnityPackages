// SimCore - Vehicle Module
// ═══════════════════════════════════════════════════════════════════════════════
// Central coordinator for all vehicle operations.
// Implements IVehicleModule to provide vehicle management to the game.
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using SimCore;
using SimCore.Data;
using SimCore.Entities;
using SimCore.Signals;

namespace SimCore.Modules.Vehicle
{
    /// <summary>
    /// Core vehicle module implementation.
    /// Manages all registered vehicles and coordinates enter/exit operations.
    /// </summary>
    public class VehicleModule : IVehicleModule
    {
        #region Private Fields

        private SimWorld _world;
        private SignalBus _signalBus;

        // Registered vehicles
        private readonly Dictionary<SimId, VehicleRuntimeData> _vehicles = new Dictionary<SimId, VehicleRuntimeData>();
        private readonly List<SimId> _vehicleList = new List<SimId>();

        // Entity -> Vehicle mapping (for entities currently in vehicles)
        private readonly Dictionary<SimId, SimId> _entityToVehicle = new Dictionary<SimId, SimId>();

        // Player tracking
        private SimId _playerId;
        private SimId _playerVehicle = SimId.Invalid;

        // Speed update tracking
        private float _speedUpdateTimer;
        private const float SpeedUpdateInterval = 0.25f;

        #endregion

        #region IVehicleModule Properties

        public SimId PlayerVehicle => _playerVehicle;
        public bool IsPlayerDriving => _playerVehicle.IsValid;

        #endregion

        #region ISimModule Lifecycle

        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;

            // Subscribe to relevant signals
            _signalBus.Subscribe<EntityDestroyedSignal>(OnEntityDestroyed);
        }

        public void Tick(float deltaTime)
        {
            // Update speed signals periodically
            _speedUpdateTimer += deltaTime;
            if (_speedUpdateTimer >= SpeedUpdateInterval)
            {
                _speedUpdateTimer = 0f;
                UpdateVehicleSpeeds();
            }

            // Update fuel consumption
            UpdateFuelConsumption(deltaTime);

            // Cleanup completed transitions
            CleanupTransitions();
        }

        public void Shutdown()
        {
            _signalBus?.Unsubscribe<EntityDestroyedSignal>(OnEntityDestroyed);
            _vehicles.Clear();
            _vehicleList.Clear();
            _entityToVehicle.Clear();
        }

        #endregion

        #region Vehicle Registration

        public void RegisterVehicle(SimId vehicleId, VehicleDefSO definition, GameObject gameObject)
        {
            if (_vehicles.ContainsKey(vehicleId))
            {
                Debug.LogWarning($"[VehicleModule] Vehicle {vehicleId} already registered");
                return;
            }

            var data = new VehicleRuntimeData
            {
                VehicleId = vehicleId,
                DefinitionId = definition != null ? new ContentId(definition.Id) : default,
                Definition = definition,
                GameObject = gameObject,
                State = VehicleState.Parked,
                MaxHealth = definition?.MaxHealth ?? 100f,
                Health = definition?.MaxHealth ?? 100f,
                MaxFuel = definition?.MaxFuel ?? 50f,
                Fuel = definition?.MaxFuel ?? 50f,
                IsLocked = definition?.StartsLocked ?? false
            };

            // Get components
            if (gameObject != null)
            {
                data.Physics = gameObject.GetComponent<VehiclePhysics>();
                data.Interaction = gameObject.GetComponent<VehicleInteraction>();
                data.AIController = gameObject.GetComponent<VehicleAIController>();

                // Initialize physics
                data.Physics?.Initialize(vehicleId, definition);

                // Initialize interaction
                data.Interaction?.Initialize(vehicleId, definition, _signalBus);
            }

            _vehicles[vehicleId] = data;
            _vehicleList.Add(vehicleId);

            _signalBus?.Publish(new VehicleSpawnedSignal
            {
                VehicleId = vehicleId,
                VehicleDefId = data.DefinitionId,
                Position = data.Position,
                Category = definition?.Category ?? VehicleCategory.Civilian
            });
        }

        public void UnregisterVehicle(SimId vehicleId)
        {
            if (!_vehicles.TryGetValue(vehicleId, out var data))
                return;

            // Force exit any occupants
            if (data.Interaction != null)
            {
                foreach (var occupant in data.Interaction.GetOccupants())
                {
                    data.Interaction.ForceExit(occupant);
                    _entityToVehicle.Remove(occupant);
                }
            }

            // Update player tracking
            if (vehicleId.Equals(_playerVehicle))
            {
                _playerVehicle = SimId.Invalid;
            }

            _vehicles.Remove(vehicleId);
            _vehicleList.Remove(vehicleId);

            _signalBus?.Publish(new VehicleDespawnedSignal
            {
                VehicleId = vehicleId,
                VehicleDefId = data.DefinitionId,
                Reason = "unregistered"
            });
        }

        public bool IsVehicleRegistered(SimId vehicleId)
        {
            return _vehicles.ContainsKey(vehicleId);
        }

        public IReadOnlyList<SimId> GetAllVehicles()
        {
            return _vehicleList;
        }

        #endregion

        #region Enter/Exit Operations

        public bool TryEnterVehicle(SimId entityId, SimId vehicleId, bool asDriver = true)
        {
            // Check if entity is already in a vehicle
            if (_entityToVehicle.ContainsKey(entityId))
            {
                Debug.LogWarning($"[VehicleModule] Entity {entityId} is already in a vehicle");
                return false;
            }

            if (!_vehicles.TryGetValue(vehicleId, out var data))
            {
                Debug.LogWarning($"[VehicleModule] Vehicle {vehicleId} not registered");
                return false;
            }

            if (data.Interaction == null)
            {
                Debug.LogWarning($"[VehicleModule] Vehicle {vehicleId} has no interaction component");
                return false;
            }

            // Try to enter
            if (!data.Interaction.TryEnter(entityId, asDriver, out Vector3 seatPos))
                return false;

            // Track entity -> vehicle mapping
            _entityToVehicle[entityId] = vehicleId;

            // Update driver tracking
            if (asDriver)
            {
                data.DriverId = entityId;
                data.State = VehicleState.Idle;

                // Check if this is the player
                if (IsPlayer(entityId))
                {
                    _playerId = entityId;
                    _playerVehicle = vehicleId;
                    data.IsPlayerControlled = true;
                }
            }

            return true;
        }

        public bool TryExitVehicle(SimId entityId)
        {
            if (!_entityToVehicle.TryGetValue(entityId, out var vehicleId))
                return false;

            if (!_vehicles.TryGetValue(vehicleId, out var data))
                return false;

            if (data.Interaction == null)
                return false;

            // Try to exit
            if (!data.Interaction.TryExit(entityId, out Vector3 exitPos))
                return false;

            // Remove mapping
            _entityToVehicle.Remove(entityId);

            // Update driver tracking
            if (entityId.Equals(data.DriverId))
            {
                data.DriverId = SimId.Invalid;
                data.State = VehicleState.Parked;

                if (entityId.Equals(_playerId))
                {
                    _playerVehicle = SimId.Invalid;
                    data.IsPlayerControlled = false;
                }
            }

            return true;
        }

        public void ForceExitVehicle(SimId entityId)
        {
            if (!_entityToVehicle.TryGetValue(entityId, out var vehicleId))
                return;

            if (_vehicles.TryGetValue(vehicleId, out var data))
            {
                data.Interaction?.ForceExit(entityId);

                if (entityId.Equals(data.DriverId))
                {
                    data.DriverId = SimId.Invalid;
                    data.State = VehicleState.Parked;
                    data.IsPlayerControlled = false;
                }

                if (entityId.Equals(_playerId))
                {
                    _playerVehicle = SimId.Invalid;
                }
            }

            _entityToVehicle.Remove(entityId);
        }

        public bool IsInVehicle(SimId entityId)
        {
            return _entityToVehicle.ContainsKey(entityId);
        }

        public SimId GetCurrentVehicle(SimId entityId)
        {
            return _entityToVehicle.TryGetValue(entityId, out var vehicleId) ? vehicleId : SimId.Invalid;
        }

        public bool IsDriver(SimId entityId)
        {
            if (!_entityToVehicle.TryGetValue(entityId, out var vehicleId))
                return false;

            if (!_vehicles.TryGetValue(vehicleId, out var data))
                return false;

            return entityId.Equals(data.DriverId);
        }

        #endregion

        #region Vehicle State

        public VehicleRuntimeData GetVehicleData(SimId vehicleId)
        {
            return _vehicles.TryGetValue(vehicleId, out var data) ? data : null;
        }

        public void SetVehicleState(SimId vehicleId, VehicleState state)
        {
            if (_vehicles.TryGetValue(vehicleId, out var data))
            {
                data.State = state;
            }
        }

        public VehicleState GetVehicleState(SimId vehicleId)
        {
            return _vehicles.TryGetValue(vehicleId, out var data) ? data.State : VehicleState.Parked;
        }

        public void SetVehicleLocked(SimId vehicleId, bool locked)
        {
            if (_vehicles.TryGetValue(vehicleId, out var data))
            {
                data.IsLocked = locked;
                if (locked)
                    data.Interaction?.Lock();
                else
                    data.Interaction?.Unlock();
            }
        }

        public bool IsVehicleLocked(SimId vehicleId)
        {
            return _vehicles.TryGetValue(vehicleId, out var data) && data.IsLocked;
        }

        public bool TryStartEngine(SimId vehicleId)
        {
            if (!_vehicles.TryGetValue(vehicleId, out var data))
                return false;

            // Check fuel
            if (data.Definition != null && data.Definition.HasFuelSystem && data.Fuel <= 0)
            {
                _signalBus?.Publish(new VehicleEngineSignal
                {
                    VehicleId = vehicleId,
                    EngineRunning = false,
                    StartFailed = true,
                    FailReason = "no_fuel"
                });
                return false;
            }

            data.EngineRunning = true;
            data.State = VehicleState.Idle;

            _signalBus?.Publish(new VehicleEngineSignal
            {
                VehicleId = vehicleId,
                EngineRunning = true,
                StartFailed = false
            });

            return true;
        }

        public void StopEngine(SimId vehicleId)
        {
            if (_vehicles.TryGetValue(vehicleId, out var data))
            {
                data.EngineRunning = false;
                data.State = VehicleState.Parked;

                _signalBus?.Publish(new VehicleEngineSignal
                {
                    VehicleId = vehicleId,
                    EngineRunning = false,
                    StartFailed = false
                });
            }
        }

        public bool IsEngineRunning(SimId vehicleId)
        {
            return _vehicles.TryGetValue(vehicleId, out var data) && data.EngineRunning;
        }

        public void DamageVehicle(SimId vehicleId, float damage, VehicleDamageSource source, Vector3 impactPoint)
        {
            if (!_vehicles.TryGetValue(vehicleId, out var data))
                return;

            float oldHealth = data.Health;
            data.Health = Mathf.Max(0f, data.Health - damage);

            _signalBus?.Publish(new VehicleDamagedSignal
            {
                VehicleId = vehicleId,
                DamageAmount = damage,
                CurrentHealth = data.Health,
                MaxHealth = data.MaxHealth,
                Source = source,
                ImpactPoint = impactPoint
            });

            // Check for destruction
            if (data.Health <= 0f && !data.IsDestroyed && (data.Definition?.CanBeDestroyed ?? true))
            {
                data.IsDestroyed = true;
                data.State = VehicleState.Destroyed;

                // Force everyone out
                if (data.Interaction != null)
                {
                    foreach (var occupant in data.Interaction.GetOccupants())
                    {
                        ForceExitVehicle(occupant);
                    }
                }

                _signalBus?.Publish(new VehicleDestroyedSignal
                {
                    VehicleId = vehicleId,
                    VehicleDefId = data.DefinitionId,
                    FinalSource = source,
                    Position = data.Position
                });
            }
        }

        public void RepairVehicle(SimId vehicleId, float amount)
        {
            if (_vehicles.TryGetValue(vehicleId, out var data))
            {
                data.Health = Mathf.Min(data.MaxHealth, data.Health + amount);
                if (data.IsDestroyed && data.Health > 0)
                {
                    data.IsDestroyed = false;
                    data.State = VehicleState.Parked;
                }
            }
        }

        public void AddFuel(SimId vehicleId, float amount)
        {
            if (_vehicles.TryGetValue(vehicleId, out var data))
            {
                float oldFuel = data.Fuel;
                data.Fuel = Mathf.Min(data.MaxFuel, data.Fuel + amount);

                _signalBus?.Publish(new VehicleFuelChangedSignal
                {
                    VehicleId = vehicleId,
                    OldFuel = oldFuel,
                    NewFuel = data.Fuel,
                    MaxFuel = data.MaxFuel,
                    IsEmpty = false
                });
            }
        }

        public bool TryConsumeFuel(SimId vehicleId, float amount)
        {
            if (!_vehicles.TryGetValue(vehicleId, out var data))
                return false;

            if (data.Fuel < amount)
                return false;

            float oldFuel = data.Fuel;
            data.Fuel -= amount;

            if (data.Fuel <= 0)
            {
                _signalBus?.Publish(new VehicleFuelChangedSignal
                {
                    VehicleId = vehicleId,
                    OldFuel = oldFuel,
                    NewFuel = 0,
                    MaxFuel = data.MaxFuel,
                    IsEmpty = true
                });
            }

            return true;
        }

        #endregion

        #region AI Vehicles

        public void SetAIDestination(SimId vehicleId, Vector3 destination)
        {
            if (_vehicles.TryGetValue(vehicleId, out var data) && data.AIController != null)
            {
                data.AIController.SetDestination(destination);
            }
        }

        public void SetAIWaypoints(SimId vehicleId, List<Vector3> waypoints, bool loop = false)
        {
            if (_vehicles.TryGetValue(vehicleId, out var data) && data.AIController != null)
            {
                data.AIController.SetWaypoints(waypoints, loop);
            }
        }

        public void SetAITargetSpeed(SimId vehicleId, float speedKmh)
        {
            if (_vehicles.TryGetValue(vehicleId, out var data) && data.AIController != null)
            {
                data.AIController.SetTargetSpeed(speedKmh);
            }
        }

        public void StopAIVehicle(SimId vehicleId)
        {
            if (_vehicles.TryGetValue(vehicleId, out var data) && data.AIController != null)
            {
                data.AIController.Stop();
            }
        }

        public void ResumeAIVehicle(SimId vehicleId)
        {
            if (_vehicles.TryGetValue(vehicleId, out var data) && data.AIController != null)
            {
                data.AIController.Resume();
            }
        }

        #endregion

        #region Queries

        public List<SimId> FindNearbyVehicles(Vector3 position, float radius)
        {
            var result = new List<SimId>();
            float radiusSqr = radius * radius;

            foreach (var kvp in _vehicles)
            {
                if ((kvp.Value.Position - position).sqrMagnitude <= radiusSqr)
                {
                    result.Add(kvp.Key);
                }
            }

            return result;
        }

        public SimId FindNearestEnterableVehicle(Vector3 position, float maxDistance)
        {
            SimId nearest = SimId.Invalid;
            float nearestDist = maxDistance * maxDistance;

            foreach (var kvp in _vehicles)
            {
                var data = kvp.Value;

                // Skip destroyed or locked
                if (data.IsDestroyed || data.IsLocked)
                    continue;

                // Skip full vehicles
                if (data.Interaction != null && !data.Interaction.HasEmptySeats)
                    continue;

                float distSqr = (data.Position - position).sqrMagnitude;
                if (distSqr < nearestDist)
                {
                    nearestDist = distSqr;
                    nearest = kvp.Key;
                }
            }

            return nearest;
        }

        public SimId GetDriver(SimId vehicleId)
        {
            return _vehicles.TryGetValue(vehicleId, out var data) ? data.DriverId : SimId.Invalid;
        }

        public List<SimId> GetPassengers(SimId vehicleId)
        {
            if (_vehicles.TryGetValue(vehicleId, out var data) && data.Interaction != null)
            {
                return data.Interaction.GetOccupants();
            }
            return new List<SimId>();
        }

        #endregion

        #region Player Management

        /// <summary>
        /// Set the player entity ID for tracking.
        /// </summary>
        public void SetPlayer(SimId playerId)
        {
            _playerId = playerId;
        }

        private bool IsPlayer(SimId entityId)
        {
            return entityId.Equals(_playerId);
        }

        #endregion

        #region Private Methods

        private void UpdateVehicleSpeeds()
        {
            foreach (var kvp in _vehicles)
            {
                var data = kvp.Value;
                if (data.Physics == null) continue;

                float speed = data.Physics.CurrentSpeedKmh;
                data.CurrentSpeed = speed;

                // Only publish for moving vehicles
                if (speed > 1f)
                {
                    float maxSpeed = data.Definition?.MaxSpeed ?? 120f;

                    _signalBus?.Publish(new VehicleSpeedChangedSignal
                    {
                        VehicleId = kvp.Key,
                        SpeedKmh = speed,
                        MaxSpeedKmh = maxSpeed,
                        SpeedPercent = speed / maxSpeed,
                        IsSpeeding = speed > 50f, // TODO: Get from zone
                        SpeedLimit = 50f
                    });
                }
            }
        }

        private void UpdateFuelConsumption(float deltaTime)
        {
            foreach (var kvp in _vehicles)
            {
                var data = kvp.Value;
                if (data.Definition == null || !data.Definition.HasFuelSystem)
                    continue;

                if (!data.EngineRunning || data.CurrentSpeed < 1f)
                    continue;

                // Calculate fuel consumption based on distance
                float distanceKm = data.CurrentSpeed * deltaTime / 3600f; // km traveled
                float fuelUsed = distanceKm * data.Definition.FuelConsumptionRate;

                if (fuelUsed > 0)
                {
                    TryConsumeFuel(kvp.Key, fuelUsed);

                    // Update odometer
                    data.Odometer += distanceKm;
                }
            }
        }

        private void CleanupTransitions()
        {
            // Check for completed transitions in all vehicles
            foreach (var data in _vehicles.Values)
            {
                // Interaction handles its own transition cleanup
            }
        }

        private void OnEntityDestroyed(EntityDestroyedSignal signal)
        {
            // If entity was in a vehicle, force them out
            if (_entityToVehicle.ContainsKey(signal.EntityId))
            {
                ForceExitVehicle(signal.EntityId);
            }
        }

        #endregion
    }
}
