// SimCore - Zone Types
// Defines different zone types for NPC interaction management

using UnityEngine;

namespace SimCore.World.Zones
{
    /// <summary>
    /// Types of zones that affect NPC behavior
    /// </summary>
    public enum ZoneType
    {
        None,
        
        // Pedestrian crossings
        Crossing,           // Zebra crossing - vehicles yield to pedestrians
        
        // Traffic control
        Intersection,       // Traffic light controlled area
        StopSign,           // Stop sign controlled
        
        // Spawning
        PedestrianSpawn,    // Where pedestrians can spawn
        VehicleSpawn,       // Where vehicles can spawn
        Despawn,            // NPCs despawn when entering (edge of world)
        
        // Points of Interest
        BusStop,            // Pedestrians may wait here
        Bench,              // Pedestrians may sit
        Shop,               // Pedestrians may enter/exit
        
        // Special areas
        ParkingLot,         // Shared vehicle/pedestrian space
        Alley,              // Shortcut, risky
        Park                // Pedestrian only, calm area
    }
    
    /// <summary>
    /// Traffic light state for intersections
    /// </summary>
    public enum TrafficLightState
    {
        Green,
        Yellow,
        Red
    }
    
    /// <summary>
    /// NPC movement intent - affects pathfinding costs
    /// </summary>
    public enum MovementIntent
    {
        Calm,       // Normal behavior, follows all rules
        Hurried,    // Rushing, might take shortcuts
        Fleeing,    // Escaping, ignores rules entirely
        Drunk,      // Erratic, unpredictable
        Suspicious, // Trying to avoid attention
        Following,  // Following another entity
        Jaywalking  // Deliberately crossing road illegally - prefers roads over crossings
    }
    
    /// <summary>
    /// Area cost modifiers based on intent
    /// Lower cost = more preferred path
    /// </summary>
    public static class IntentCosts
    {
        // NavMesh area indices (must match Unity NavMesh setup)
        public const int AreaSidewalk = 3;
        public const int AreaRoad = 4;
        public const int AreaCrossing = 5;
        public const int AreaPark = 6;
        public const int AreaParkingLot = 7;
        
        /// <summary>
        /// Get area cost modifier for a given intent.
        /// NPCs with Calm intent should NEVER walk on roads.
        /// Only violations (jaywalking, fleeing, drunk) allow road access.
        /// </summary>
        public static float GetRoadCost(MovementIntent intent)
        {
            return intent switch
            {
                MovementIntent.Calm => 999f,       // NEVER use roads - effectively blocked
                MovementIntent.Hurried => 50f,     // Very reluctant, only if necessary
                MovementIntent.Fleeing => 1f,      // Roads are fine when fleeing
                MovementIntent.Drunk => 2f,        // Drunk people wander onto roads
                MovementIntent.Suspicious => 100f, // Avoid roads (don't want attention)
                MovementIntent.Following => 2f,    // Will follow target anywhere
                MovementIntent.Jaywalking => 0.5f, // PREFER roads - this is jaywalking!
                _ => 999f                          // Default: no roads
            };
        }
        
        /// <summary>
        /// Get crossing cost modifier for a given intent
        /// </summary>
        public static float GetCrossingCost(MovementIntent intent)
        {
            return intent switch
            {
                MovementIntent.Calm => 2f,        // Use crossings
                MovementIntent.Hurried => 1.5f,   // Prefer crossings but not required
                MovementIntent.Fleeing => 1f,     // Crossings are just another path
                MovementIntent.Drunk => 3f,       // Might ignore crossings
                MovementIntent.Suspicious => 1f,  // Crossings are fine
                MovementIntent.Following => 1f,   // Will use whatever path target uses
                MovementIntent.Jaywalking => 50f, // AVOID crossings - jaywalking means NOT using them
                _ => 2f
            };
        }
        
        /// <summary>
        /// Get speed multiplier for a given intent
        /// </summary>
        public static float GetSpeedMultiplier(MovementIntent intent)
        {
            return intent switch
            {
                MovementIntent.Calm => 1.0f,
                MovementIntent.Hurried => 1.3f,
                MovementIntent.Fleeing => 1.8f,
                MovementIntent.Drunk => 0.7f,
                MovementIntent.Suspicious => 0.9f,
                MovementIntent.Following => 1.2f,
                MovementIntent.Jaywalking => 1.4f, // Walking fast to cross quickly
                _ => 1.0f
            };
        }
    }
}

