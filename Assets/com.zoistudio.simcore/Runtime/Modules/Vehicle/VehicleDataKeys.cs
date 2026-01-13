namespace SimCore.Modules.Vehicle
{
    /// <summary>
    /// Entity data keys for vehicle-related state stored on Entity objects.
    /// Use these constants with entity.SetData/GetData for vehicle state.
    /// </summary>
    public static class VehicleDataKeys
    {
        // Stats stored on vehicle Entity
        public const string Health = "vehicle_health";
        public const string MaxHealth = "vehicle_max_health";
        public const string Fuel = "vehicle_fuel";
        public const string MaxFuel = "vehicle_max_fuel";
        public const string Speed = "vehicle_speed";
        public const string MaxSpeed = "vehicle_max_speed";
        public const string Odometer = "vehicle_odometer";

        // State flags
        public const string IsLocked = "vehicle_locked";
        public const string EngineRunning = "vehicle_engine_running";
        public const string HasKey = "vehicle_has_key";
        public const string IsDestroyed = "vehicle_destroyed";
        public const string IsDamaged = "vehicle_damaged";

        // Driver/passenger tracking (stored on vehicle)
        public const string DriverId = "vehicle_driver_id";
        public const string PassengerIds = "vehicle_passenger_ids";
        public const string SeatCount = "vehicle_seat_count";

        // Stored on player/NPC entity when in vehicle
        public const string CurrentVehicleId = "current_vehicle_id";
        public const string IsDriver = "is_vehicle_driver";

        // Vehicle definition reference
        public const string VehicleDefId = "vehicle_def_id";
        public const string VehicleType = "vehicle_type";

        // Physics state
        public const string IsGrounded = "vehicle_grounded";
        public const string IsFlipped = "vehicle_flipped";
        public const string Velocity = "vehicle_velocity";

        // AI state
        public const string AIDestination = "vehicle_ai_destination";
        public const string AITargetSpeed = "vehicle_ai_target_speed";
        public const string AIPathIndex = "vehicle_ai_path_index";
    }

    /// <summary>
    /// Vehicle physics type - determines which physics model to use.
    /// </summary>
    public enum VehiclePhysicsType
    {
        FourWheel,      // Cars, trucks, SUVs
        TwoWheel,       // Motorcycles, bicycles
        Boat,           // Water vehicles
        Aircraft,       // Helicopters (future)
        Tank            // Tracked vehicles (future)
    }

    /// <summary>
    /// Vehicle category for spawning and gameplay logic.
    /// </summary>
    public enum VehicleCategory
    {
        Civilian,
        Police,         // Law enforcement vehicles
        Emergency,      // Ambulance, fire truck
        Service,        // Taxis, delivery, etc.
        Commercial,
        Motorcycle,
        Bicycle,
        Boat,
        Aircraft
    }

    /// <summary>
    /// Current vehicle operational state.
    /// </summary>
    public enum VehicleState
    {
        Parked,             // Stationary, engine off
        Idle,               // Engine running, not moving
        Moving,             // Normal driving
        Speeding,           // Above speed limit
        Fleeing,            // AI fleeing from threat
        PlayerControlled,   // Player is driving
        AIControlled,       // NPC is driving
        Disabled,           // Cannot be driven (damage/fuel)
        Destroyed           // Wrecked
    }

    /// <summary>
    /// Damage source for vehicle damage signals.
    /// </summary>
    public enum VehicleDamageSource
    {
        Collision,
        Gunfire,
        Explosion,
        Environmental,
        Wear                // Normal wear
    }
}
