// SimCore - NPC Movement
// UNIFIED component for NPC movement - replaces NPCBridge and IntentNavAgent
// Bridges SimCore AI (brain) with Unity NavMesh (body)
// Supports intent-based pathfinding for dynamic behavior

using UnityEngine;
using UnityEngine.AI;
using SimCore.Entities;
using SimCore.AI;
using SimCore.World.Zones;

namespace SimCore.Unity
{
    /// <summary>
    /// Unified NPC movement component.
    /// - Reads AI commands from Entity data (blackboard pattern)
    /// - Drives Unity NavMeshAgent
    /// - Supports intent-based area costs (calm vs fleeing)
    /// - Syncs position to SimCore WorldPartition
    /// 
    /// This REPLACES both NPCBridge and IntentNavAgent.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _baseSpeed = 2f;
        [SerializeField] private float _stoppingDistance = 0.5f;
        [SerializeField] private float _angularSpeed = 120f;
        
        [Header("Animation (Optional)")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _speedParam = "Speed";
        [SerializeField] private string _movingParam = "IsMoving";
        
        [Header("Performance (Mobile)")]
        [Tooltip("Use lower quality avoidance for better mobile performance")]
        [SerializeField] private bool _mobileOptimized = true;
        [SerializeField] private ObstacleAvoidanceType _avoidanceQuality = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
        
        [Header("LOD Settings")]
        [Tooltip("Distance from player for full updates")]
        [SerializeField] private float _lodNearDistance = 30f;
        [Tooltip("Distance from player for reduced updates")]
        [SerializeField] private float _lodMedDistance = 50f;
        [Tooltip("Frame interval for medium distance LOD")]
        [SerializeField] private int _lodMedFrameInterval = 3;
        [Tooltip("Frame interval for far distance LOD")]
        [SerializeField] private int _lodFarFrameInterval = 10;
        [Tooltip("Disable NavMeshAgent for far NPCs to save CPU")]
        [SerializeField] private bool _disableAgentWhenFar = true;
        [Tooltip("Distance at which to disable NavMeshAgent")]
        [SerializeField] private float _agentDisableDistance = 45f;
        [Tooltip("Disable Animator for far NPCs to save CPU")]
        [SerializeField] private bool _disableAnimatorWhenFar = true;
        [Tooltip("Distance at which to disable Animator")]
        [SerializeField] private float _animatorDisableDistance = 35f;
        [Tooltip("Use simpler animation update mode for medium distance")]
        [SerializeField] private bool _useAnimatorLod = true;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebug = false;
        
        // Components
        private NavMeshAgent _agent;
        private SimWorld _world;
        private SimId _entityId;
        private Entity _entity;
        private bool _isInitialized;
        
        // Movement state
        private Vector3? _currentTarget;
        private MovementIntent _currentIntent = MovementIntent.Calm;
        private bool _isMoving;
        private bool _hasArrived;
        
        // LOD state
        private Transform _playerTransform;
        private int _frameOffset; // Random offset to stagger updates across NPCs
        private int _currentLodLevel; // 0=near, 1=medium, 2=far
        private static int _frameCounter; // Global frame counter shared by all NPCs
        private bool _agentWasDisabledByLod; // Track if we disabled the agent
        private Vector3 _lastPositionBeforeDisable; // Store position when agent disabled
        private Quaternion _lastRotationBeforeDisable;
        
        // Animation LOD state
        private bool _animatorWasDisabledByLod;
        private AnimatorCullingMode _originalCullingMode;
        private AnimatorUpdateMode _originalUpdateMode;
        
        // Properties
        public SimId EntityId => _entityId;
        public bool IsInitialized => _isInitialized;
        public bool IsMoving => _isMoving;
        public bool HasArrived => _hasArrived;
        public MovementIntent CurrentIntent => _currentIntent;
        public NavMeshAgent Agent => _agent;
        public Vector3 Velocity => _agent?.velocity ?? Vector3.zero;
        public int CurrentLodLevel => _currentLodLevel;
        
        /// <summary>
        /// Get the SimCore Entity
        /// </summary>
        public Entity GetEntity() => _entity;
        
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _baseSpeed;
            _agent.stoppingDistance = _stoppingDistance;
            _agent.angularSpeed = _angularSpeed;
            _agent.autoBraking = true;
            
            // Mobile-friendly settings
            if (_mobileOptimized)
            {
                _agent.obstacleAvoidanceType = _avoidanceQuality;
                _agent.avoidancePriority = Random.Range(30, 70); // Vary to prevent deadlocks
            }
            
            // Random frame offset to stagger updates across NPCs (prevents all NPCs updating same frame)
            _frameOffset = Random.Range(0, 10);
            
            // Find player transform for LOD calculations
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
            
            // Cache original animator settings for LOD
            if (_animator != null)
            {
                _originalCullingMode = _animator.cullingMode;
                _originalUpdateMode = _animator.updateMode;
            }
        }
        
        /// <summary>
        /// Initialize with SimCore references
        /// </summary>
        public void Initialize(SimWorld world, SimId entityId)
        {
            _world = world;
            _entityId = entityId;
            _entity = world.Entities?.GetEntity(entityId);
            
            // Register initial position
            _world.Partition?.UpdatePosition(_entityId, transform.position);
            
            // Apply initial intent costs
            ApplyIntentCosts(_currentIntent);
            
            _isInitialized = true;
            
            if (_showDebug)
                SimCoreLogger.Log($"[NPCMovement] Initialized for entity {_entityId}");
        }
        
        private void Update()
        {
            if (!_isInitialized || _entity == null) return;
            
            // Increment global frame counter
            _frameCounter++;
            
            // Calculate LOD level based on distance to player
            UpdateLodLevel();
            
            // Determine if this NPC should update this frame based on LOD
            if (!ShouldUpdateThisFrame())
            {
                return;
            }
            
            // Sync position to SimCore
            _world.Partition?.UpdatePosition(_entityId, transform.position);
            
            // Read AI commands from entity data
            ReadEntityData();
            
            // Update movement state
            UpdateMovementState();
            
            // Write state back to entity
            WriteEntityData();
            
            // Update animation (only if LOD allows)
            if (_currentLodLevel < 2) // Don't animate far NPCs
            {
                UpdateAnimation();
            }
        }
        
        /// <summary>
        /// Update LOD level based on distance to player
        /// Also handles NavMeshAgent enable/disable for performance
        /// </summary>
        private void UpdateLodLevel()
        {
            if (_playerTransform == null)
            {
                _currentLodLevel = 0;
                return;
            }
            
            float distSq = (transform.position - _playerTransform.position).sqrMagnitude;
            float agentDisableDistSq = _agentDisableDistance * _agentDisableDistance;
            
            if (distSq <= _lodNearDistance * _lodNearDistance)
            {
                _currentLodLevel = 0; // Near - full updates
            }
            else if (distSq <= _lodMedDistance * _lodMedDistance)
            {
                _currentLodLevel = 1; // Medium - reduced updates
            }
            else
            {
                _currentLodLevel = 2; // Far - minimal updates
            }
            
            // Handle NavMeshAgent disable/enable based on distance
            if (_disableAgentWhenFar && _agent != null)
            {
                if (distSq > agentDisableDistSq && !_agentWasDisabledByLod)
                {
                    // Disable agent when far
                    _lastPositionBeforeDisable = transform.position;
                    _lastRotationBeforeDisable = transform.rotation;
                    
                    if (_agent.isOnNavMesh)
                    {
                        _agent.ResetPath();
                    }
                    _agent.enabled = false;
                    _agentWasDisabledByLod = true;
                    
                    if (_showDebug)
                        SimCoreLogger.Log($"[NPCMovement] {gameObject.name} NavMeshAgent DISABLED (far)");
                }
                else if (distSq <= agentDisableDistSq && _agentWasDisabledByLod)
                {
                    // Re-enable agent when close
                    _agent.enabled = true;
                    _agentWasDisabledByLod = false;
                    
                    // Warp to last known position to resync with NavMesh
                    if (_agent.isOnNavMesh)
                    {
                        _agent.Warp(_lastPositionBeforeDisable);
                    }
                    else if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                    {
                        transform.position = hit.position;
                        if (_agent.isActiveAndEnabled)
                        {
                            _agent.Warp(hit.position);
                        }
                    }
                    
                    // Reapply current target if we have one
                    if (_currentTarget.HasValue)
                    {
                        SetDestination(_currentTarget.Value);
                    }
                    
                    if (_showDebug)
                        SimCoreLogger.Log($"[NPCMovement] {gameObject.name} NavMeshAgent RE-ENABLED");
                }
            }
            
            // Handle Animator LOD
            UpdateAnimatorLod(distSq);
        }
        
        /// <summary>
        /// Update Animator state based on distance (LOD)
        /// </summary>
        private void UpdateAnimatorLod(float distSq)
        {
            if (_animator == null) return;
            
            float animDisableDistSq = _animatorDisableDistance * _animatorDisableDistance;
            float medDistSq = _lodMedDistance * _lodMedDistance;
            
            if (_disableAnimatorWhenFar)
            {
                if (distSq > animDisableDistSq && !_animatorWasDisabledByLod)
                {
                    // Disable animator completely for far NPCs
                    _animator.enabled = false;
                    _animatorWasDisabledByLod = true;
                    
                    if (_showDebug)
                        SimCoreLogger.Log($"[NPCMovement] {gameObject.name} Animator DISABLED (far)");
                }
                else if (distSq <= animDisableDistSq && _animatorWasDisabledByLod)
                {
                    // Re-enable animator
                    _animator.enabled = true;
                    _animatorWasDisabledByLod = false;
                    
                    if (_showDebug)
                        SimCoreLogger.Log($"[NPCMovement] {gameObject.name} Animator RE-ENABLED");
                }
            }
            
            // Adjust animator quality for medium distance
            if (_useAnimatorLod && _animator.enabled)
            {
                if (distSq > medDistSq)
                {
                    // Medium/Far: Use aggressive culling
                    _animator.cullingMode = AnimatorCullingMode.CullCompletely;
                }
                else
                {
                    // Near: Use original settings
                    _animator.cullingMode = _originalCullingMode;
                }
            }
        }
        
        /// <summary>
        /// Check if this NPC should update on this frame based on LOD level
        /// Uses staggered frame offsets to spread load across frames
        /// </summary>
        private bool ShouldUpdateThisFrame()
        {
            switch (_currentLodLevel)
            {
                case 0: // Near - update every frame
                    return true;
                    
                case 1: // Medium - update every N frames
                    return ((_frameCounter + _frameOffset) % _lodMedFrameInterval) == 0;
                    
                case 2: // Far - update every M frames
                    return ((_frameCounter + _frameOffset) % _lodFarFrameInterval) == 0;
                    
                default:
                    return true;
            }
        }
        
        #region Entity Data Communication
        
        private void ReadEntityData()
        {
            // Check for stop command
            bool shouldStop = _entity.GetData(IntentDataKeys.StopMovement, false);
            if (shouldStop)
            {
                Stop();
                return;
            }
            
            // Check for new intent
            int intentValue = _entity.GetData(IntentDataKeys.MovementIntent, -1);
            if (intentValue >= 0)
            {
                var newIntent = (MovementIntent)intentValue;
                if (newIntent != _currentIntent)
                {
                    SetIntent(newIntent);
                }
            }
            
            // Check for new move target
            var targetData = _entity.GetData<Vector3?>(IntentDataKeys.MoveTarget);
            if (targetData.HasValue)
            {
                var newTarget = targetData.Value;
                
                // Only update if target changed significantly
                if (!_currentTarget.HasValue || 
                    Vector3.Distance(_currentTarget.Value, newTarget) > 0.5f)
                {
                    SetDestination(newTarget);
                }
                
                // Clear so AI can set new target
                _entity.SetData<Vector3?>(IntentDataKeys.MoveTarget, null);
            }
            
            // Check for speed override
            float speedOverride = _entity.GetData(IntentDataKeys.MoveSpeed, -1f);
            if (speedOverride > 0)
            {
                _agent.speed = speedOverride * IntentCosts.GetSpeedMultiplier(_currentIntent);
            }
        }
        
        private void WriteEntityData()
        {
            _entity.SetData(IntentDataKeys.IsMoving, _isMoving);
            _entity.SetData(IntentDataKeys.HasArrived, _hasArrived);
        }
        
        #endregion
        
        #region Movement Logic
        
        private void UpdateMovementState()
        {
            if (!_agent.hasPath)
            {
                _isMoving = false;
                return;
            }
            
            _isMoving = _agent.velocity.sqrMagnitude > 0.01f;
            
            // Check if arrived
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _hasArrived = true;
                _isMoving = false;
                _currentTarget = null;
            }
            else
            {
                _hasArrived = false;
            }
            
            // NOTE: Violation detection is PROACTIVE, not reactive.
            // AI states (JaywalkingState, DrunkState, etc.) add violation tags.
            // Movement component just handles pathfinding.
        }
        
        private void UpdateAnimation()
        {
            // Skip if animator is disabled or LOD is too far
            if (_animator == null || !_animator.enabled || _animatorWasDisabledByLod) return;
            
            // For medium LOD, only update animation parameters less frequently
            if (_currentLodLevel == 1)
            {
                // Only update every 2 frames for medium distance
                if ((_frameCounter + _frameOffset) % 2 != 0) return;
            }
            
            float speed = _agent != null ? _agent.velocity.magnitude : 0f;
            
            if (!string.IsNullOrEmpty(_speedParam))
                _animator.SetFloat(_speedParam, speed);
            
            if (!string.IsNullOrEmpty(_movingParam))
                _animator.SetBool(_movingParam, _isMoving);
        }
        
        #endregion
        
        #region Public API
        
        // Reusable array for pathfinding retry - avoids allocation
        private static readonly float[] _defaultSearchRadii = { 5f, 15f, 30f };

        /// <summary>
        /// Set destination to navigate to
        /// </summary>
        public bool SetDestination(Vector3 target)
        {
            return SetDestinationWithRetry(target, _defaultSearchRadii);
        }

        /// <summary>
        /// Set destination with configurable retry radii for NavMesh sampling
        /// </summary>
        public bool SetDestinationWithRetry(Vector3 target, float[] searchRadii = null)
        {
            if (_agent == null || !_agent.enabled) return false;
            if (!_agent.isOnNavMesh)
            {
                if (_showDebug) SimCoreLogger.LogWarning($"[NPCMovement] {gameObject.name} not on NavMesh!");
                return false;
            }

            _currentTarget = target;
            _hasArrived = false;

            searchRadii ??= _defaultSearchRadii;

            // Try to find valid NavMesh position with increasing search radius
            foreach (float radius in searchRadii)
            {
                if (NavMesh.SamplePosition(target, out NavMeshHit hit, radius, NavMesh.AllAreas))
                {
                    bool result = _agent.SetDestination(hit.position);
                    _isMoving = result;

                    if (_showDebug && result)
                        SimCoreLogger.Log($"[NPCMovement] {gameObject.name} → {target} (radius: {radius}, intent: {_currentIntent})");

                    return result;
                }
            }

            // All attempts failed
            if (_showDebug)
                SimCoreLogger.LogWarning($"[NPCMovement] {gameObject.name} failed to find NavMesh position near {target}");

            return false;
        }
        
        /// <summary>
        /// Set movement intent - affects pathfinding costs
        /// </summary>
        public void SetIntent(MovementIntent intent)
        {
            if (_currentIntent == intent) return;
            
            _currentIntent = intent;
            ApplyIntentCosts(intent);
            
            // Update speed based on intent
            _agent.speed = _baseSpeed * IntentCosts.GetSpeedMultiplier(intent);
            
            if (_showDebug)
                SimCoreLogger.Log($"[NPCMovement] {gameObject.name} intent: {intent}, speed: {_agent.speed:F1}");
            
            // Recalculate path if we have a destination
            if (_currentTarget.HasValue && _agent.hasPath)
            {
                _agent.SetDestination(_currentTarget.Value);
            }
        }
        
        /// <summary>
        /// Apply NavMesh area costs based on intent
        /// </summary>
        private void ApplyIntentCosts(MovementIntent intent)
        {
            // Only set area costs if agent is on NavMesh
            if (_agent == null || !_agent.isOnNavMesh) return;
            
            _agent.SetAreaCost(IntentCosts.AreaSidewalk, 1f);
            _agent.SetAreaCost(IntentCosts.AreaRoad, IntentCosts.GetRoadCost(intent));
            _agent.SetAreaCost(IntentCosts.AreaCrossing, IntentCosts.GetCrossingCost(intent));
            _agent.SetAreaCost(IntentCosts.AreaPark, 1f);
            _agent.SetAreaCost(IntentCosts.AreaParkingLot, 2f);
        }
        
        /// <summary>
        /// Stop movement immediately
        /// </summary>
        public void Stop()
        {
            if (_agent == null) return;
            
            // Only reset path if on NavMesh
            if (_agent.isOnNavMesh)
            {
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }
            
            _currentTarget = null;
            _isMoving = false;
            _hasArrived = true;
            
            // Clear any pending move commands
            _entity?.SetData<Vector3?>(IntentDataKeys.MoveTarget, null);
        }
        
        /// <summary>
        /// Set movement speed
        /// </summary>
        public void SetSpeed(float speed)
        {
            _agent.speed = speed;
        }
        
        /// <summary>
        /// Look at a target position
        /// </summary>
        public void LookAt(Vector3 target)
        {
            Vector3 direction = (target - transform.position).normalized;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    _angularSpeed * Time.deltaTime
                );
            }
        }
        
        /// <summary>
        /// Warp to position (teleport)
        /// </summary>
        public void Warp(Vector3 position)
        {
            _agent.Warp(position);
            _world?.Partition?.UpdatePosition(_entityId, position);
        }
        
        /// <summary>
        /// Get current movement direction
        /// </summary>
        public Vector3 GetCurrentDirection()
        {
            if (_agent != null && _agent.velocity.sqrMagnitude > 0.01f)
            {
                return _agent.velocity.normalized;
            }
            return transform.forward;
        }
        
        /// <summary>
        /// Pause movement (can resume)
        /// </summary>
        public void Pause()
        {
            if (_agent == null) return;
            _agent.isStopped = true;
            _isMoving = false;
        }
        
        /// <summary>
        /// Resume movement after pause
        /// </summary>
        public void Resume()
        {
            if (_agent == null) return;
            _agent.isStopped = false;
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            if (!_showDebug || _agent == null) return;
            
            // Draw current target
            if (_currentTarget.HasValue)
            {
                Gizmos.color = _currentIntent switch
                {
                    MovementIntent.Calm => Color.green,
                    MovementIntent.Hurried => Color.yellow,
                    MovementIntent.Fleeing => Color.red,
                    MovementIntent.Drunk => Color.magenta,
                    MovementIntent.Jaywalking => new Color(1f, 0.5f, 0f), // Orange for jaywalking
                    _ => Color.white
                };
                
                Gizmos.DrawWireSphere(_currentTarget.Value, 0.3f);
                Gizmos.DrawLine(transform.position, _currentTarget.Value);
            }
            
            // Draw path
            if (_agent.hasPath)
            {
                var corners = _agent.path.corners;
                for (int i = 0; i < corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
                }
            }
            
            // Draw stopping distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _stoppingDistance);
        }
        
        #endregion
    }
}

