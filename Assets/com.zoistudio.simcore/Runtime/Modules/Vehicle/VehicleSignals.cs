using UnityEngine;
using SimCore;
using SimCore.Signals;

namespace SimCore.Modules.Vehicle
{
    /// <summary>
    /// Published when an entity enters a vehicle.
    /// </summary>
    public struct VehicleEnteredSignal : ISignal
    {
        public SimId EntityId;          // Who entered
        public SimId VehicleId;         // Which vehicle
        public ContentId VehicleDefId;  // Vehicle definition
        public bool IsDriver;           // Driver or passenger
        public int SeatIndex;           // Which seat (0 = driver)
    }

    /// <summary>
    /// Published when an entity exits a vehicle.
    /// </summary>
    public struct VehicleExitedSignal : ISignal
    {
        public SimId EntityId;          // Who exited
        public SimId VehicleId;         // Which vehicle
        public Vector3 ExitPosition;    // Where they ended up
        public bool WasDriver;          // Were they driving
    }

    /// <summary>
    /// Published when vehicle takes damage.
    /// </summary>
    public struct VehicleDamagedSignal : ISignal
    {
        public SimId VehicleId;
        public float DamageAmount;
        public float CurrentHealth;
        public float MaxHealth;
        public VehicleDamageSource Source;
        public SimId DamageSourceId;    // Who/what caused damage
        public Vector3 ImpactPoint;
    }

    /// <summary>
    /// Published when vehicle is destroyed.
    /// </summary>
    public struct VehicleDestroyedSignal : ISignal
    {
        public SimId VehicleId;
        public ContentId VehicleDefId;
        public VehicleDamageSource FinalSource;
        public Vector3 Position;
    }

    /// <summary>
    /// Published when vehicle fuel changes.
    /// </summary>
    public struct VehicleFuelChangedSignal : ISignal
    {
        public SimId VehicleId;
        public float OldFuel;
        public float NewFuel;
        public float MaxFuel;
        public bool IsEmpty;
    }

    /// <summary>
    /// Published periodically with vehicle speed info.
    /// </summary>
    public struct VehicleSpeedChangedSignal : ISignal
    {
        public SimId VehicleId;
        public float SpeedKmh;
        public float MaxSpeedKmh;
        public float SpeedPercent;      // 0-1
        public bool IsSpeeding;         // Above speed limit
        public float SpeedLimit;        // Current zone limit
    }

    /// <summary>
    /// Published when vehicle collides with something.
    /// </summary>
    public struct VehicleCollisionSignal : ISignal
    {
        public SimId VehicleId;
        public SimId OtherEntityId;     // Invalid if static object
        public Vector3 ImpactPoint;
        public Vector3 ImpactNormal;
        public float ImpactForce;
        public bool IsVehicle;          // Collided with another vehicle
        public bool IsPedestrian;       // Collided with pedestrian
    }

    /// <summary>
    /// Published when vehicle engine state changes.
    /// </summary>
    public struct VehicleEngineSignal : ISignal
    {
        public SimId VehicleId;
        public bool EngineRunning;
        public bool StartFailed;        // True if start attempt failed
        public string FailReason;       // "no_fuel", "no_key", "damaged"
    }

    /// <summary>
    /// Published when vehicle lock state changes.
    /// </summary>
    public struct VehicleLockSignal : ISignal
    {
        public SimId VehicleId;
        public bool IsLocked;
        public SimId UnlockedBy;        // Who unlocked (if applicable)
    }

    /// <summary>
    /// Published when AI vehicle reaches destination.
    /// </summary>
    public struct VehicleAIDestinationReachedSignal : ISignal
    {
        public SimId VehicleId;
        public Vector3 Destination;
        public int WaypointIndex;
        public bool IsFinalDestination;
    }

    /// <summary>
    /// Request to spawn a vehicle (game code publishes, module handles).
    /// </summary>
    public struct VehicleSpawnRequestSignal : ISignal
    {
        public ContentId VehicleDefId;
        public Vector3 Position;
        public Quaternion Rotation;
        public VehicleState InitialState;
        public bool SpawnLocked;
        public bool SpawnWithDriver;    // Spawn with AI driver
    }

    /// <summary>
    /// Published when vehicle is spawned.
    /// </summary>
    public struct VehicleSpawnedSignal : ISignal
    {
        public SimId VehicleId;
        public ContentId VehicleDefId;
        public Vector3 Position;
        public VehicleCategory Category;
    }

    /// <summary>
    /// Published when vehicle is despawned/removed.
    /// </summary>
    public struct VehicleDespawnedSignal : ISignal
    {
        public SimId VehicleId;
        public ContentId VehicleDefId;
        public string Reason;           // "destroyed", "pooled", "out_of_range"
    }
}
