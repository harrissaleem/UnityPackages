using System.Collections.Generic;
using UnityEngine;
using SimCore;
using SimCore.Data;
using SimCore.Modules;

namespace SimCore.Modules.Vehicle
{
    /// <summary>
    /// Vehicle module interface for managing all vehicle-related functionality.
    /// Handles vehicle registration, player vehicle operations, and AI vehicles.
    /// </summary>
    public interface IVehicleModule : ISimModule
    {
        #region Vehicle Registration

        /// <summary>
        /// Register a vehicle entity with its definition.
        /// Call when spawning a vehicle.
        /// </summary>
        void RegisterVehicle(SimId vehicleId, VehicleDefSO definition, GameObject gameObject);

        /// <summary>
        /// Unregister a vehicle (when despawning).
        /// </summary>
        void UnregisterVehicle(SimId vehicleId);

        /// <summary>
        /// Check if a vehicle is registered.
        /// </summary>
        bool IsVehicleRegistered(SimId vehicleId);

        /// <summary>
        /// Get all registered vehicle IDs.
        /// </summary>
        IReadOnlyList<SimId> GetAllVehicles();

        #endregion

        #region Enter/Exit Operations

        /// <summary>
        /// Attempt to enter a vehicle.
        /// Returns false if vehicle is locked, full, or entity is already in a vehicle.
        /// </summary>
        bool TryEnterVehicle(SimId entityId, SimId vehicleId, bool asDriver = true);

        /// <summary>
        /// Attempt to exit current vehicle.
        /// Returns false if exit is blocked or entity is not in a vehicle.
        /// </summary>
        bool TryExitVehicle(SimId entityId);

        /// <summary>
        /// Force exit from vehicle (teleport, emergency).
        /// </summary>
        void ForceExitVehicle(SimId entityId);

        /// <summary>
        /// Check if entity is currently in a vehicle.
        /// </summary>
        bool IsInVehicle(SimId entityId);

        /// <summary>
        /// Get the vehicle an entity is currently in.
        /// Returns SimId.Invalid if not in vehicle.
        /// </summary>
        SimId GetCurrentVehicle(SimId entityId);

        /// <summary>
        /// Check if entity is the driver of their current vehicle.
        /// </summary>
        bool IsDriver(SimId entityId);

        #endregion

        #region Vehicle State

        /// <summary>
        /// Get vehicle runtime data.
        /// </summary>
        VehicleRuntimeData GetVehicleData(SimId vehicleId);

        /// <summary>
        /// Set vehicle state (parked, moving, etc).
        /// </summary>
        void SetVehicleState(SimId vehicleId, VehicleState state);

        /// <summary>
        /// Get current vehicle state.
        /// </summary>
        VehicleState GetVehicleState(SimId vehicleId);

        /// <summary>
        /// Lock/unlock vehicle.
        /// </summary>
        void SetVehicleLocked(SimId vehicleId, bool locked);

        /// <summary>
        /// Check if vehicle is locked.
        /// </summary>
        bool IsVehicleLocked(SimId vehicleId);

        /// <summary>
        /// Start/stop engine.
        /// </summary>
        bool TryStartEngine(SimId vehicleId);
        void StopEngine(SimId vehicleId);
        bool IsEngineRunning(SimId vehicleId);

        /// <summary>
        /// Apply damage to vehicle.
        /// </summary>
        void DamageVehicle(SimId vehicleId, float damage, VehicleDamageSource source, Vector3 impactPoint);

        /// <summary>
        /// Repair vehicle.
        /// </summary>
        void RepairVehicle(SimId vehicleId, float amount);

        /// <summary>
        /// Add/remove fuel.
        /// </summary>
        void AddFuel(SimId vehicleId, float amount);
        bool TryConsumeFuel(SimId vehicleId, float amount);

        #endregion

        #region Player Vehicle

        /// <summary>
        /// Get the player's current vehicle (convenience for common case).
        /// </summary>
        SimId PlayerVehicle { get; }

        /// <summary>
        /// Is player currently driving.
        /// </summary>
        bool IsPlayerDriving { get; }

        #endregion

        #region AI Vehicles

        /// <summary>
        /// Set AI destination for a vehicle.
        /// </summary>
        void SetAIDestination(SimId vehicleId, Vector3 destination);

        /// <summary>
        /// Set AI waypoint path.
        /// </summary>
        void SetAIWaypoints(SimId vehicleId, List<Vector3> waypoints, bool loop = false);

        /// <summary>
        /// Set AI target speed.
        /// </summary>
        void SetAITargetSpeed(SimId vehicleId, float speedKmh);

        /// <summary>
        /// Stop AI vehicle.
        /// </summary>
        void StopAIVehicle(SimId vehicleId);

        /// <summary>
        /// Resume AI vehicle after stop.
        /// </summary>
        void ResumeAIVehicle(SimId vehicleId);

        #endregion

        #region Queries

        /// <summary>
        /// Find nearby vehicles within radius.
        /// </summary>
        List<SimId> FindNearbyVehicles(Vector3 position, float radius);

        /// <summary>
        /// Find nearest enterable vehicle.
        /// </summary>
        SimId FindNearestEnterableVehicle(Vector3 position, float maxDistance);

        /// <summary>
        /// Get driver of a vehicle.
        /// </summary>
        SimId GetDriver(SimId vehicleId);

        /// <summary>
        /// Get all passengers (including driver).
        /// </summary>
        List<SimId> GetPassengers(SimId vehicleId);

        #endregion
    }

    /// <summary>
    /// Runtime data for a registered vehicle.
    /// </summary>
    public class VehicleRuntimeData
    {
        public SimId VehicleId;
        public ContentId DefinitionId;
        public VehicleDefSO Definition;
        public GameObject GameObject;
        public VehiclePhysics Physics;
        public VehicleInteraction Interaction;
        public VehicleAIController AIController;

        public VehicleState State;
        public float Health;
        public float MaxHealth;
        public float Fuel;
        public float MaxFuel;
        public float CurrentSpeed;      // km/h
        public float Odometer;          // Total km traveled

        public SimId DriverId;
        public List<SimId> PassengerIds = new List<SimId>();

        public bool IsLocked;
        public bool EngineRunning;
        public bool IsDestroyed;
        public bool IsPlayerControlled;

        public Vector3 Position => GameObject != null ? GameObject.transform.position : Vector3.zero;
        public Quaternion Rotation => GameObject != null ? GameObject.transform.rotation : Quaternion.identity;
    }
}
