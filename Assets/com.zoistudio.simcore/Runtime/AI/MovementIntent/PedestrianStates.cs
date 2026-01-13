// SimCore - Pedestrian Movement States
// AI states for pedestrian movement with intent-based pathfinding
// Communicates with IntentNavAgent via Entity data (blackboard pattern)

using UnityEngine;
using UnityEngine.AI;
using SimCore.World.Zones;
using SimCore.Entities;

namespace SimCore.AI
{
    /// <summary>
    /// Data keys for intent-based pedestrian movement.
    /// IntentNavAgent reads these to drive NavMeshAgent.
    /// </summary>
    public static class IntentDataKeys
    {
        public const string MoveTarget = "move_target";
        public const string MoveSpeed = "move_speed";
        public const string MovementIntent = "movement_intent";
        public const string IsMoving = "is_moving";
        public const string HasArrived = "has_arrived";
        public const string StopMovement = "stop_movement";
    }
    
    /// <summary>
    /// Base class for pedestrian states that use IntentNavAgent for movement.
    /// Provides helper methods for common pedestrian operations.
    /// </summary>
    public abstract class PedestrianState : AIState
    {
        /// <summary>
        /// Set movement target for IntentNavAgent to process
        /// </summary>
        protected void SetMoveTarget(Entity entity, Vector3 target)
        {
            entity?.SetData(IntentDataKeys.MoveTarget, target);
        }
        
        /// <summary>
        /// Set movement intent (affects pathfinding costs)
        /// </summary>
        protected void SetMovementIntent(Entity entity, MovementIntent intent)
        {
            entity?.SetData(IntentDataKeys.MovementIntent, (int)intent);
        }
        
        /// <summary>
        /// Set speed multiplier
        /// </summary>
        protected void SetMoveSpeed(Entity entity, float speed)
        {
            entity?.SetData(IntentDataKeys.MoveSpeed, speed);
        }
        
        /// <summary>
        /// Stop movement immediately
        /// </summary>
        protected void StopMovement(Entity entity)
        {
            entity?.SetData(IntentDataKeys.StopMovement, true);
        }
        
        /// <summary>
        /// Check if currently moving
        /// </summary>
        protected bool IsMoving(Entity entity)
        {
            return entity?.GetData(IntentDataKeys.IsMoving, false) ?? false;
        }
        
        /// <summary>
        /// Check if arrived at destination
        /// </summary>
        protected bool HasArrived(Entity entity)
        {
            return entity?.GetData(IntentDataKeys.HasArrived, false) ?? false;
        }
        
        /// <summary>
        /// Get current position from world partition
        /// </summary>
        protected Vector3 GetCurrentPosition(SimId entityId, SimWorld world)
        {
            return world.Partition?.GetPosition(entityId) ?? Vector3.zero;
        }
    }
    
    /// <summary>
    /// Pedestrian walks to a destination using sidewalks and crossings
    /// </summary>
    public class PedestrianWalkState : PedestrianState
    {
        public Vector3 Destination { get; set; }
        public float ArrivalThreshold { get; set; } = 1.5f;
        public MovementIntent Intent { get; set; } = MovementIntent.Calm;
        
        private float _stuckTimer = 0f;
        private Vector3 _lastPosition;
        
        public PedestrianWalkState()
        {
            StateId = "pedestrian_walk";
            DisplayName = "Walking";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return;
            
            // Set intent and destination on entity for IntentNavAgent to read
            entity.SetData(IntentDataKeys.MovementIntent, (int)Intent);
            entity.SetData<Vector3?>(IntentDataKeys.MoveTarget, Destination);
            entity.SetData(IntentDataKeys.StopMovement, false);
            
            _lastPosition = world.Partition?.GetPosition(controller.EntityId) ?? Vector3.zero;
            _stuckTimer = 0f;
        }
        
        public override void OnExit(AIController controller, SimWorld world)
        {
            // Keep walking unless explicitly stopped
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return AITickResult.Transition("idle");
            
            var currentPos = world.Partition?.GetPosition(controller.EntityId) ?? Vector3.zero;
            
            // Check if arrived
            float distToDest = Vector3.Distance(currentPos, Destination);
            if (distToDest < ArrivalThreshold)
            {
                return AITickResult.Transition("idle");
            }
            
            // Check if has arrived flag is set by NPCBridge/IntentNavAgent
            bool hasArrived = entity.GetData(IntentDataKeys.HasArrived, false);
            if (hasArrived)
            {
                return AITickResult.Transition("idle");
            }
            
            // Check if stuck - reduced timer for more responsive recovery
            float moved = Vector3.Distance(currentPos, _lastPosition);
            if (moved < 0.05f) // Slightly more generous threshold
            {
                _stuckTimer += Time.deltaTime;
                // Reduced from 5s to 2s for faster recovery
                if (_stuckTimer > 2f)
                {
                    SimCoreLogger.Log($"[PedestrianWalkState] NPC stuck for 2s, transitioning to idle");
                    return AITickResult.Transition("idle");
                }
            }
            else
            {
                _stuckTimer = 0f;
                _lastPosition = currentPos;
            }
            
            return AITickResult.Stay();
        }
    }
    
    /// <summary>
    /// Pedestrian wanders randomly between points
    /// </summary>
    public class PedestrianWanderState : PedestrianState
    {
        public float WanderRadius { get; set; } = 20f;
        public float MinWanderDistance { get; set; } = 5f;
        public MovementIntent Intent { get; set; } = MovementIntent.Calm;
        public float PauseDuration { get; set; } = 2f;
        public float MoveSpeed { get; set; } = 2f;
        
        private Vector3 _currentTarget;
        private bool _isPausing = false;
        private float _pauseTimer = 0f;
        private float _moveTimer = 0f;
        
        public PedestrianWanderState()
        {
            StateId = "pedestrian_wander";
            DisplayName = "Wandering";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return;
            
            entity.SetData(IntentDataKeys.MovementIntent, (int)Intent);
            entity.SetData(IntentDataKeys.MoveSpeed, MoveSpeed);
            entity.SetData(IntentDataKeys.StopMovement, false);
            
            _isPausing = false;
            _pauseTimer = 0f;
            _moveTimer = 0f;
            
            PickNewDestination(controller, world);
        }
        
        public override void OnExit(AIController controller, SimWorld world)
        {
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return AITickResult.Transition("idle");
            
            // Handle pausing
            if (_isPausing)
            {
                _pauseTimer -= Time.deltaTime;
                if (_pauseTimer <= 0)
                {
                    _isPausing = false;
                    PickNewDestination(controller, world);
                }
                return AITickResult.Stay();
            }
            
            _moveTimer += Time.deltaTime;
            
            // Check if arrived
            bool hasArrived = entity.GetData(IntentDataKeys.HasArrived, false);
            var currentPos = world.Partition?.GetPosition(controller.EntityId) ?? Vector3.zero;
            float distToTarget = Vector3.Distance(currentPos, _currentTarget);
            
            if (hasArrived || distToTarget < 1.5f || _moveTimer > 15f)
            {
                // Pause briefly, then pick new destination
                _isPausing = true;
                _pauseTimer = Random.Range(PauseDuration * 0.5f, PauseDuration * 1.5f);
                _moveTimer = 0f;
                
                // Stop movement during pause
                entity.SetData(IntentDataKeys.StopMovement, true);
            }
            
            return AITickResult.Stay();
        }
        
        private void PickNewDestination(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return;
            
            var currentPos = world.Partition?.GetPosition(controller.EntityId) ?? Vector3.zero;
            
            // Try to find a valid point
            for (int i = 0; i < 10; i++)
            {
                Vector3 randomDir = Random.insideUnitSphere * WanderRadius;
                randomDir.y = 0;
                
                Vector3 targetPos = currentPos + randomDir;
                
                if (Vector3.Distance(targetPos, currentPos) < MinWanderDistance)
                    continue;
                
                // Validate on NavMesh
                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    _currentTarget = hit.position;
                    entity.SetData<Vector3?>(IntentDataKeys.MoveTarget, _currentTarget);
                    entity.SetData(IntentDataKeys.StopMovement, false);
                    return;
                }
            }
            
            // Fallback
            _currentTarget = currentPos + Random.insideUnitSphere.normalized * MinWanderDistance;
            _currentTarget.y = currentPos.y;
            entity.SetData<Vector3?>(IntentDataKeys.MoveTarget, _currentTarget);
            entity.SetData(IntentDataKeys.StopMovement, false);
        }
    }
    
    /// <summary>
    /// Pedestrian flees from a threat (player, another NPC, etc.)
    /// Uses Fleeing intent which ignores roads/traffic rules
    /// </summary>
    public class PedestrianFleeState : PedestrianState
    {
        public SimId ThreatId { get; set; }
        public float FleeDistance { get; set; } = 30f;
        public float SafeDistance { get; set; } = 50f;
        public float FleeSpeed { get; set; } = 4f;
        
        private Vector3 _fleeTarget;
        private float _recalculateTimer = 0f;
        
        public PedestrianFleeState()
        {
            StateId = "pedestrian_flee";
            DisplayName = "Fleeing";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return;
            
            // Set fleeing intent - ignores roads, crossings, etc.
            entity.SetData(IntentDataKeys.MovementIntent, (int)MovementIntent.Fleeing);
            entity.SetData(IntentDataKeys.MoveSpeed, FleeSpeed);
            entity.SetData(IntentDataKeys.StopMovement, false);
            
            CalculateFleeTarget(controller, world);
        }
        
        public override void OnExit(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return;
            
            // Reset to calm intent
            entity.SetData(IntentDataKeys.MovementIntent, (int)MovementIntent.Calm);
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return AITickResult.Transition("idle");
            
            var currentPos = world.Partition?.GetPosition(controller.EntityId) ?? Vector3.zero;
            
            // Get threat position
            Vector3 threatPos = currentPos;
            if (ThreatId.IsValid && world.Partition != null)
            {
                threatPos = world.Partition.GetPosition(ThreatId);
            }
            
            // Check if safe
            float distToThreat = Vector3.Distance(currentPos, threatPos);
            if (distToThreat >= SafeDistance)
            {
                return AITickResult.Transition("pedestrian_wander");
            }
            
            // Recalculate flee target periodically
            _recalculateTimer += Time.deltaTime;
            bool hasArrived = entity.GetData(IntentDataKeys.HasArrived, false);
            
            if (_recalculateTimer > 2f || hasArrived)
            {
                _recalculateTimer = 0f;
                CalculateFleeTarget(controller, world);
            }
            
            return AITickResult.Stay();
        }
        
        private void CalculateFleeTarget(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return;
            
            var currentPos = world.Partition?.GetPosition(controller.EntityId) ?? Vector3.zero;
            
            Vector3 threatPos = currentPos;
            if (ThreatId.IsValid && world.Partition != null)
            {
                threatPos = world.Partition.GetPosition(ThreatId);
            }
            
            // Flee away from threat
            Vector3 fleeDir = (currentPos - threatPos).normalized;
            
            // Add some randomness
            fleeDir += Random.insideUnitSphere * 0.3f;
            fleeDir.y = 0;
            fleeDir.Normalize();
            
            Vector3 targetPos = currentPos + fleeDir * FleeDistance;
            
            // Find valid NavMesh position
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                _fleeTarget = hit.position;
            }
            else
            {
                _fleeTarget = targetPos;
            }
            
            entity.SetData<Vector3?>(IntentDataKeys.MoveTarget, _fleeTarget);
        }
    }
    
    /// <summary>
    /// Pedestrian is engaged with player (stopped, talking)
    /// </summary>
    public class PedestrianEngagedState : PedestrianState
    {
        public SimId EngagedWith { get; set; }
        
        public PedestrianEngagedState()
        {
            StateId = "engaged";
            DisplayName = "Engaged";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return;
            
            // Stop all movement
            entity.SetData(IntentDataKeys.StopMovement, true);
            entity.SetData<Vector3?>(IntentDataKeys.MoveTarget, null);
        }
        
        public override void OnExit(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return;
            
            entity.SetData(IntentDataKeys.StopMovement, false);
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            // Stay engaged until external state change
            return AITickResult.Stay();
        }
    }

    /// <summary>
    /// Pedestrian is drunk - erratic movement
    /// </summary>
    public class PedestrianDrunkWanderState : PedestrianState
    {
        public float WanderRadius { get; set; } = 10f;
        public float MoveSpeed { get; set; } = 1.2f;
        public float StaggerInterval { get; set; } = 2f;
        
        private Vector3 _currentTarget;
        private float _staggerTimer = 0f;
        
        public PedestrianDrunkWanderState()
        {
            StateId = "drunk_wander";
            DisplayName = "Drunk Wandering";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return;
            
            // Set drunk intent - ignores some rules
            entity.SetData(IntentDataKeys.MovementIntent, (int)MovementIntent.Drunk);
            entity.SetData(IntentDataKeys.MoveSpeed, MoveSpeed);
            entity.SetData(IntentDataKeys.StopMovement, false);
            
            PickNewDestination(controller, world);
            _staggerTimer = 0f;
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return AITickResult.Transition("idle");
            
            _staggerTimer += Time.deltaTime;
            
            // Stagger to new destination periodically
            if (_staggerTimer >= StaggerInterval)
            {
                _staggerTimer = 0f;
                PickNewDestination(controller, world);
            }
            
            return AITickResult.Stay();
        }
        
        private void PickNewDestination(AIController controller, SimWorld world)
        {
            var entity = world.Entities?.GetEntity(controller.EntityId);
            if (entity == null) return;
            
            var currentPos = world.Partition?.GetPosition(controller.EntityId) ?? Vector3.zero;
            
            // Drunk movement is erratic - short random distances
            Vector3 randomDir = Random.insideUnitSphere * WanderRadius;
            randomDir.y = 0;
            
            Vector3 targetPos = currentPos + randomDir;
            
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                _currentTarget = hit.position;
            }
            else
            {
                _currentTarget = targetPos;
            }
            
            entity.SetData<Vector3?>(IntentDataKeys.MoveTarget, _currentTarget);
        }
    }
}
