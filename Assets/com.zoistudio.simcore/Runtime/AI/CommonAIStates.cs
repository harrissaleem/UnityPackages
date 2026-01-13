// SimCore - Common AI States
// Reusable AI states that work across all games

using System.Collections.Generic;
using UnityEngine;

namespace SimCore.AI
{
    /// <summary>
    /// Data keys for NPC movement (matches NPCMovement).
    /// Use these in your AI states to communicate with the movement component.
    /// </summary>
    public static class MovementKeys
    {
        public const string MoveTarget = "move_target";
        public const string MoveSpeed = "move_speed";
        public const string IsMoving = "is_moving";
        public const string HasArrived = "has_arrived";
        public const string DistanceToTarget = "distance_to_target";
        public const string LookTarget = "look_target";
    }
    
    /// <summary>
    /// Timed idle state - NPC stands still for a random duration then transitions.
    /// Use when you want random idle durations before next behavior.
    /// </summary>
    public class TimedIdleState : AIState
    {
        public float MinDuration = 2f;
        public float MaxDuration = 5f;
        public string NextState = null; // If null, stays idle
        
        private float _duration;
        private float _elapsed;
        
        public TimedIdleState(string stateId = "timed_idle")
        {
            StateId = stateId;
            DisplayName = "Idle";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            _elapsed = 0;
            _duration = Random.Range(MinDuration, MaxDuration);
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            _elapsed += Time.deltaTime;
            
            if (_elapsed >= _duration && !string.IsNullOrEmpty(NextState))
            {
                return AITickResult.Transition(NextState);
            }
            
            return AITickResult.Stay();
        }
    }
    
    /// <summary>
    /// Wander state - NPC walks to random nearby points.
    /// Reusable across all games.
    /// </summary>
    public class WanderState : AIState
    {
        public float WanderRadius = 10f;
        public float MoveSpeed = 2f;
        public float Timeout = 15f;
        public string NextState = "idle";
        
        private float _elapsed;
        
        public WanderState(string stateId = "wander")
        {
            StateId = stateId;
            DisplayName = "Wandering";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            _elapsed = 0;
            
            var currentPos = world.Partition.GetPosition(controller.EntityId);
            var offset = new Vector3(
                Random.Range(-WanderRadius, WanderRadius),
                0,
                Random.Range(-WanderRadius, WanderRadius)
            );
            var target = currentPos + offset;
            
            var entity = world.Entities.GetEntity(controller.EntityId);
            entity?.SetData<Vector3?>(MovementKeys.MoveTarget, target);
            entity?.SetData(MovementKeys.MoveSpeed, MoveSpeed);
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            _elapsed += Time.deltaTime;
            
            var entity = world.Entities.GetEntity(controller.EntityId);
            bool arrived = entity?.GetData<bool>(MovementKeys.HasArrived) ?? false;
            
            if (arrived || _elapsed >= Timeout)
            {
                return AITickResult.Transition(NextState);
            }
            
            return AITickResult.Stay();
        }
        
        public override void OnExit(AIController controller, SimWorld world)
        {
            var entity = world.Entities.GetEntity(controller.EntityId);
            entity?.SetData<Vector3?>(MovementKeys.MoveTarget, null);
        }
    }
    
    /// <summary>
    /// Go to a specific point.
    /// Set the target via controller.SetData("goto_target", position).
    /// </summary>
    public class GoToPointState : AIState
    {
        public float MoveSpeed = 3f;
        public float Timeout = 30f;
        public string NextState = "idle";
        public string TargetDataKey = "goto_target";
        
        private float _elapsed;
        
        public GoToPointState(string stateId = "goto")
        {
            StateId = stateId;
            DisplayName = "Going To Point";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            _elapsed = 0;
            
            var target = controller.GetData<Vector3?>(TargetDataKey);
            if (!target.HasValue)
            {
                SimCoreLogger.LogWarning($"[GoToPointState] No target set in {TargetDataKey}");
                return;
            }
            
            var entity = world.Entities.GetEntity(controller.EntityId);
            entity?.SetData<Vector3?>(MovementKeys.MoveTarget, target.Value);
            entity?.SetData(MovementKeys.MoveSpeed, MoveSpeed);
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            _elapsed += Time.deltaTime;
            
            var entity = world.Entities.GetEntity(controller.EntityId);
            bool arrived = entity?.GetData<bool>(MovementKeys.HasArrived) ?? false;
            
            if (arrived || _elapsed >= Timeout)
            {
                return AITickResult.Transition(NextState);
            }
            
            return AITickResult.Stay();
        }
        
        public override void OnExit(AIController controller, SimWorld world)
        {
            var entity = world.Entities.GetEntity(controller.EntityId);
            entity?.SetData<Vector3?>(MovementKeys.MoveTarget, null);
            controller.SetData<Vector3?>(TargetDataKey, null);
        }
    }
    
    /// <summary>
    /// Follow another entity.
    /// Set the target via controller.SetData("follow_target", entityId).
    /// </summary>
    public class FollowState : AIState
    {
        public float MoveSpeed = 3f;
        public float StopDistance = 2f;
        public float UpdateInterval = 0.5f;
        public string TargetDataKey = "follow_target";
        
        private float _updateTimer;
        
        public FollowState(string stateId = "follow")
        {
            StateId = stateId;
            DisplayName = "Following";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            _updateTimer = 0;
            UpdateTarget(controller, world);
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            _updateTimer += Time.deltaTime;
            
            if (_updateTimer >= UpdateInterval)
            {
                _updateTimer = 0;
                UpdateTarget(controller, world);
            }
            
            return AITickResult.Stay();
        }
        
        private void UpdateTarget(AIController controller, SimWorld world)
        {
            var targetId = controller.GetData<SimId>(TargetDataKey);
            if (!targetId.IsValid) return;
            
            var myPos = world.Partition.GetPosition(controller.EntityId);
            var targetPos = world.Partition.GetPosition(targetId);
            var distance = Vector3.Distance(myPos, targetPos);
            
            var entity = world.Entities.GetEntity(controller.EntityId);
            
            if (distance > StopDistance)
            {
                entity?.SetData<Vector3?>(MovementKeys.MoveTarget, targetPos);
                entity?.SetData(MovementKeys.MoveSpeed, MoveSpeed);
            }
            else
            {
                entity?.SetData<Vector3?>(MovementKeys.MoveTarget, null);
            }
        }
        
        public override void OnExit(AIController controller, SimWorld world)
        {
            var entity = world.Entities.GetEntity(controller.EntityId);
            entity?.SetData<Vector3?>(MovementKeys.MoveTarget, null);
        }
    }
    
    /// <summary>
    /// Flee from another entity.
    /// Set the threat via controller.SetData("flee_from", entityId).
    /// </summary>
    public class FleeState : AIState
    {
        public float MoveSpeed = 5f;
        public float FleeDistance = 20f;
        public float SafeDistance = 25f;
        public float Timeout = 10f;
        public string NextState = "idle";
        public string ThreatDataKey = "flee_from";
        
        private float _elapsed;
        
        public FleeState(string stateId = "flee")
        {
            StateId = stateId;
            DisplayName = "Fleeing";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            _elapsed = 0;
            UpdateFleeTarget(controller, world);
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            _elapsed += Time.deltaTime;
            
            var threatId = controller.GetData<SimId>(ThreatDataKey);
            if (threatId.IsValid)
            {
                var myPos = world.Partition.GetPosition(controller.EntityId);
                var threatPos = world.Partition.GetPosition(threatId);
                var distance = Vector3.Distance(myPos, threatPos);
                
                if (distance >= SafeDistance || _elapsed >= Timeout)
                {
                    return AITickResult.Transition(NextState);
                }
                
                // Keep updating flee direction
                UpdateFleeTarget(controller, world);
            }
            else if (_elapsed >= Timeout)
            {
                return AITickResult.Transition(NextState);
            }
            
            return AITickResult.Stay();
        }
        
        private void UpdateFleeTarget(AIController controller, SimWorld world)
        {
            var threatId = controller.GetData<SimId>(ThreatDataKey);
            if (!threatId.IsValid) return;
            
            var myPos = world.Partition.GetPosition(controller.EntityId);
            var threatPos = world.Partition.GetPosition(threatId);
            
            var direction = (myPos - threatPos).normalized;
            var fleeTarget = myPos + direction * FleeDistance;
            
            var entity = world.Entities.GetEntity(controller.EntityId);
            entity?.SetData<Vector3?>(MovementKeys.MoveTarget, fleeTarget);
            entity?.SetData(MovementKeys.MoveSpeed, MoveSpeed);
        }
        
        public override void OnExit(AIController controller, SimWorld world)
        {
            var entity = world.Entities.GetEntity(controller.EntityId);
            entity?.SetData<Vector3?>(MovementKeys.MoveTarget, null);
            controller.SetData<SimId>(ThreatDataKey, default);
        }
    }
    
    /// <summary>
    /// Patrol between Vector3 waypoints using NPCMovement.
    /// Set waypoints via controller.SetData("patrol_points", List<Vector3>).
    /// Different from AISystem.PatrolState which uses SimId waypoints.
    /// </summary>
    public class WaypointPatrolState : AIState
    {
        public float MoveSpeed = 2f;
        public float WaitAtPoint = 2f;
        public bool Loop = true;
        public string NextState = "idle";
        public string WaypointsDataKey = "patrol_points";
        
        private int _currentIndex;
        private float _waitTimer;
        private bool _waiting;
        
        public WaypointPatrolState(string stateId = "waypoint_patrol")
        {
            StateId = stateId;
            DisplayName = "Patrolling";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            _currentIndex = 0;
            _waitTimer = 0;
            _waiting = false;
            MoveToCurrentPoint(controller, world);
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            if (_waiting)
            {
                _waitTimer += Time.deltaTime;
                if (_waitTimer >= WaitAtPoint)
                {
                    _waiting = false;
                    _currentIndex++;
                    
                    var points = controller.GetData<List<Vector3>>(WaypointsDataKey);
                    if (points == null || _currentIndex >= points.Count)
                    {
                        if (Loop)
                        {
                            _currentIndex = 0;
                        }
                        else
                        {
                            return AITickResult.Transition(NextState);
                        }
                    }
                    
                    MoveToCurrentPoint(controller, world);
                }
            }
            else
            {
                var entity = world.Entities.GetEntity(controller.EntityId);
                bool arrived = entity?.GetData<bool>(MovementKeys.HasArrived) ?? false;
                
                if (arrived)
                {
                    _waiting = true;
                    _waitTimer = 0;
                }
            }
            
            return AITickResult.Stay();
        }
        
        private void MoveToCurrentPoint(AIController controller, SimWorld world)
        {
            var points = controller.GetData<List<Vector3>>(WaypointsDataKey);
            if (points == null || points.Count == 0) return;
            
            var target = points[_currentIndex % points.Count];
            var entity = world.Entities.GetEntity(controller.EntityId);
            entity?.SetData<Vector3?>(MovementKeys.MoveTarget, target);
            entity?.SetData(MovementKeys.MoveSpeed, MoveSpeed);
        }
        
        public override void OnExit(AIController controller, SimWorld world)
        {
            var entity = world.Entities.GetEntity(controller.EntityId);
            entity?.SetData<Vector3?>(MovementKeys.MoveTarget, null);
        }
    }
    
    /// <summary>
    /// Wait/pause state - freezes NPC for duration or until flag is cleared.
    /// Useful for cutscenes, interactions, etc.
    /// </summary>
    public class WaitState : AIState
    {
        public float Duration = -1f; // -1 = wait indefinitely
        public string NextState = "idle";
        public string WaitFlagKey = "is_waiting";
        
        private float _elapsed;
        
        public WaitState(string stateId = "wait")
        {
            StateId = stateId;
            DisplayName = "Waiting";
        }
        
        public override void OnEnter(AIController controller, SimWorld world)
        {
            _elapsed = 0;
            controller.SetData(WaitFlagKey, true);
            
            // Stop movement
            var entity = world.Entities.GetEntity(controller.EntityId);
            entity?.SetData<Vector3?>(MovementKeys.MoveTarget, null);
        }
        
        public override AITickResult Tick(AIController controller, SimWorld world)
        {
            _elapsed += Time.deltaTime;
            
            // Check if wait flag was cleared externally
            bool stillWaiting = controller.GetData<bool>(WaitFlagKey);
            if (!stillWaiting)
            {
                return AITickResult.Transition(NextState);
            }
            
            // Check duration
            if (Duration > 0 && _elapsed >= Duration)
            {
                return AITickResult.Transition(NextState);
            }
            
            return AITickResult.Stay();
        }
        
        public override void OnExit(AIController controller, SimWorld world)
        {
            controller.SetData(WaitFlagKey, false);
        }
    }
}

