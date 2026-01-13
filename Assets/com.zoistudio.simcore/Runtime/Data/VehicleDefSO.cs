// SimCore - Vehicle Definition ScriptableObject
// ═══════════════════════════════════════════════════════════════════════════════
// Defines vehicle characteristics: performance, handling, capacity
// This is the base class - games can extend for specific vehicle types
// (e.g., PoliceVehicleDefSO adds siren, lights, etc.)
// ═══════════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;
using SimCore.Modules.Vehicle;

namespace SimCore.Data
{
    [CreateAssetMenu(fileName = "VehicleDef_New", menuName = "SimCore/Vehicle Definition")]
    public class VehicleDefSO : ScriptableObject
    {
        [Header("═══ IDENTITY ═══")]
        [Tooltip("Unique ID for this vehicle type")]
        public string Id;

        [Tooltip("Display name for UI")]
        public string DisplayName;

        [Tooltip("Vehicle physics type")]
        public VehiclePhysicsType PhysicsType = VehiclePhysicsType.FourWheel;

        [Tooltip("Vehicle category")]
        public VehicleCategory Category = VehicleCategory.Civilian;

        [Header("═══ PREFABS ═══")]
        [Tooltip("Main vehicle prefab")]
        public GameObject Prefab;

        [Tooltip("Optional destroyed/wrecked version")]
        public GameObject DestroyedPrefab;

        [Tooltip("Optional variant prefabs for visual variety")]
        public List<GameObject> VariantPrefabs;

        [Header("═══ PERFORMANCE ═══")]
        [Tooltip("Maximum speed in km/h")]
        [Range(20f, 300f)]
        public float MaxSpeed = 120f;

        [Tooltip("Acceleration power (higher = faster 0-100)")]
        [Range(1f, 20f)]
        public float Acceleration = 8f;

        [Tooltip("Brake strength multiplier")]
        [Range(0.5f, 3f)]
        public float BrakeStrength = 1f;

        [Tooltip("Handling rating (higher = tighter turns)")]
        [Range(0.1f, 1f)]
        public float HandlingRating = 0.7f;

        [Tooltip("Top speed at which steering is reduced (prevents spinning)")]
        [Range(50f, 200f)]
        public float SteeringFalloffSpeed = 80f;

        [Header("═══ PHYSICS CURVES ═══")]
        [Tooltip("Acceleration curve (x = speed %, y = torque %)")]
        public AnimationCurve AccelerationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);

        [Tooltip("Steering reduction at speed (x = speed %, y = steer %)")]
        public AnimationCurve SteeringBySpeed = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.4f);

        [Header("═══ DURABILITY ═══")]
        [Tooltip("Maximum health points")]
        [Range(10f, 500f)]
        public float MaxHealth = 100f;

        [Tooltip("Collision damage multiplier")]
        [Range(0f, 2f)]
        public float CollisionDamageMultiplier = 1f;

        [Tooltip("Whether vehicle can be destroyed")]
        public bool CanBeDestroyed = true;

        [Header("═══ FUEL ═══")]
        [Tooltip("Maximum fuel capacity (0 = infinite fuel)")]
        [Range(0f, 200f)]
        public float MaxFuel = 50f;

        [Tooltip("Fuel consumption rate (liters per km)")]
        [Range(0f, 0.5f)]
        public float FuelConsumptionRate = 0.1f;

        [Header("═══ CAPACITY ═══")]
        [Tooltip("Number of seats (including driver)")]
        [Range(1, 8)]
        public int SeatCount = 4;

        [Tooltip("Cargo capacity (for inventory)")]
        [Range(0, 100)]
        public int CargoSlots = 0;

        [Header("═══ INTERACTION ═══")]
        [Tooltip("Whether vehicle starts locked")]
        public bool StartsLocked = true;

        [Tooltip("Whether key is required to start")]
        public bool RequiresKey = true;

        [Tooltip("Whether vehicle can be searched by player")]
        public bool CanBeSearched = true;

        [Tooltip("Entry/exit animation duration")]
        [Range(0.1f, 3f)]
        public float EnterExitDuration = 1f;

        [Header("═══ AI BEHAVIOR ═══")]
        [Tooltip("Default AI speed when patrolling")]
        [Range(20f, 100f)]
        public float DefaultAISpeed = 50f;

        [Tooltip("Whether AI obeys traffic rules")]
        public bool AIObeyTraffic = true;

        [Header("═══ AUDIO ═══")]
        [Tooltip("Engine idle loop")]
        public AudioClip EngineIdleSound;

        [Tooltip("Engine running loop")]
        public AudioClip EngineRunningSound;

        [Tooltip("Horn sound")]
        public AudioClip HornSound;

        [Tooltip("Crash sound")]
        public AudioClip CrashSound;

        [Header("═══ WHEELS (For Wheeled Vehicles) ═══")]
        [Tooltip("Wheel configuration for physics")]
        public List<WheelConfig> Wheels;

        /// <summary>
        /// Get the prefab to spawn (randomly picks variant if available)
        /// </summary>
        public virtual GameObject GetPrefab()
        {
            if (VariantPrefabs != null && VariantPrefabs.Count > 0)
            {
                if (Random.value > 0.5f)
                {
                    var variant = VariantPrefabs[Random.Range(0, VariantPrefabs.Count)];
                    if (variant != null) return variant;
                }
            }
            return Prefab;
        }

        /// <summary>
        /// Get effective max speed (can be overridden)
        /// </summary>
        public virtual float GetMaxSpeed() => MaxSpeed;

        /// <summary>
        /// Get effective acceleration (can be overridden)
        /// </summary>
        public virtual float GetAcceleration() => Acceleration;

        /// <summary>
        /// Evaluate acceleration curve at normalized speed
        /// </summary>
        public float EvaluateAcceleration(float normalizedSpeed)
        {
            return AccelerationCurve?.Evaluate(normalizedSpeed) ?? 1f;
        }

        /// <summary>
        /// Evaluate steering curve at normalized speed
        /// </summary>
        public float EvaluateSteering(float normalizedSpeed)
        {
            return SteeringBySpeed?.Evaluate(normalizedSpeed) ?? 1f;
        }

        /// <summary>
        /// Check if fuel system is enabled
        /// </summary>
        public bool HasFuelSystem => MaxFuel > 0f;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure ID is set
            if (string.IsNullOrEmpty(Id))
            {
                Id = name.ToLower().Replace(" ", "_");
            }

            // Create default curves if null
            if (AccelerationCurve == null || AccelerationCurve.length == 0)
            {
                AccelerationCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);
            }
            if (SteeringBySpeed == null || SteeringBySpeed.length == 0)
            {
                SteeringBySpeed = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.4f);
            }
        }
#endif
    }

    /// <summary>
    /// Configuration for a single wheel.
    /// </summary>
    [System.Serializable]
    public class WheelConfig
    {
        [Tooltip("Reference to WheelCollider in prefab")]
        public string WheelColliderPath;

        [Tooltip("Reference to visual wheel mesh in prefab")]
        public string WheelMeshPath;

        [Tooltip("Is this a drive wheel (receives motor torque)")]
        public bool IsDriveWheel = true;

        [Tooltip("Is this a steering wheel (turns with input)")]
        public bool IsSteerWheel;

        [Tooltip("Is this a brake wheel (receives brake force)")]
        public bool IsBrakeWheel = true;
    }
}
