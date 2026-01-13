// SimCore - Stealth Module
// Detection and alert system

using System;
using System.Collections.Generic;
using SimCore.Modules;
using SimCore.Signals;
using UnityEngine;

namespace SimCore.Modules.Stealth
{
    /// <summary>
    /// Detection state for a detector
    /// </summary>
    public class DetectionState
    {
        public SimId DetectorId;
        public float AlertLevel;
        public SimId LastDetectedTarget;
        public Vector3 LastKnownPosition;
        public float TimeSinceLastSight;
    }
    
    /// <summary>
    /// Stealth module implementation
    /// </summary>
    public class StealthModule : IStealthModule
    {
        private readonly Dictionary<SimId, DetectorConfig> _detectorConfigs = new();
        private readonly Dictionary<SimId, DetectionState> _detectionStates = new();
        private SignalBus _signalBus;
        private SimWorld _world;
        private readonly int _obstacleMask;
        
        public StealthModule(int obstacleMask = ~0)
        {
            _obstacleMask = obstacleMask;
        }
        
        #region ISimModule
        
        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
        }
        
        public void Tick(float deltaTime)
        {
            Tick(_world, deltaTime);
        }
        
        public void Shutdown()
        {
            _detectorConfigs.Clear();
            _detectionStates.Clear();
        }
        
        #endregion
        
        public void RegisterDetector(SimId entityId, DetectorConfig config)
        {
            _detectorConfigs[entityId] = config;
            _detectionStates[entityId] = new DetectionState { DetectorId = entityId };
        }
        
        public void UpdateDetection(SimId detectorId, SimId targetId)
        {
            if (!_detectorConfigs.TryGetValue(detectorId, out var config)) return;
            if (!_detectionStates.TryGetValue(detectorId, out var state)) return;
        }
        
        public float GetAlertLevel(SimId detectorId)
        {
            return _detectionStates.TryGetValue(detectorId, out var state) ? state.AlertLevel : 0f;
        }
        
        public void SetAlertLevel(SimId detectorId, float level)
        {
            if (_detectionStates.TryGetValue(detectorId, out var state))
            {
                float oldLevel = state.AlertLevel;
                state.AlertLevel = Mathf.Clamp01(level);
                
                // Check threshold crossing
                var config = _detectorConfigs[detectorId];
                if (oldLevel < config.AlertThreshold && state.AlertLevel >= config.AlertThreshold)
                {
                    _signalBus.Publish(new PlayerDetectedSignal
                    {
                        DetectorId = detectorId,
                        AlertLevel = state.AlertLevel
                    });
                }
            }
        }
        
        public bool IsDetected(SimId detectorId, SimId targetId)
        {
            if (!_detectionStates.TryGetValue(detectorId, out var state)) return false;
            if (!_detectorConfigs.TryGetValue(detectorId, out var config)) return false;
            
            return state.AlertLevel >= config.AlertThreshold && state.LastDetectedTarget == targetId;
        }
        
        public void Tick(SimWorld world, float deltaTime)
        {
            var player = world.Entities.GetPlayer();
            if (player == null) return;
            
            foreach (var kvp in _detectorConfigs)
            {
                var detectorId = kvp.Key;
                var config = kvp.Value;
                var state = _detectionStates[detectorId];
                
                var detector = world.Entities.GetEntity(detectorId);
                if (detector == null || !detector.IsActive) continue;
                
                // Check visibility
                bool canSee = CanSeeTarget(world, detectorId, player.Id, config);
                bool canHear = CanHearTarget(world, detectorId, player.Id, config);
                
                if (canSee || canHear)
                {
                    // Increase alert
                    float increase = config.DetectionSpeed * deltaTime;
                    if (canSee) increase *= 2f; // Sight is more effective
                    
                    float newLevel = Mathf.Min(1f, state.AlertLevel + increase);
                    
                    state.LastDetectedTarget = player.Id;
                    state.LastKnownPosition = world.Partition.GetPosition(player.Id);
                    state.TimeSinceLastSight = 0;
                    
                    SetAlertLevel(detectorId, newLevel);
                    
                    // Update entity tags based on alert level
                    UpdateAlertTags(detector, state.AlertLevel, config.AlertThreshold);
                }
                else
                {
                    // Decay alert
                    state.TimeSinceLastSight += deltaTime;
                    
                    if (state.AlertLevel > 0)
                    {
                        float decay = config.AlertDecaySpeed * deltaTime;
                        float newLevel = Mathf.Max(0f, state.AlertLevel - decay);
                        SetAlertLevel(detectorId, newLevel);
                        
                        UpdateAlertTags(detector, state.AlertLevel, config.AlertThreshold);
                    }
                }
            }
        }
        
        /// <summary>
        /// Check if detector can see target
        /// </summary>
        public bool CanSeeTarget(SimWorld world, SimId detectorId, SimId targetId, DetectorConfig config)
        {
            var detectorPos = world.Partition.GetPosition(detectorId);
            var targetPos = world.Partition.GetPosition(targetId);
            
            // Distance check
            float distance = Vector3.Distance(detectorPos, targetPos);
            if (distance > config.ViewDistance) return false;
            
            // TODO: Angle check (requires detector facing direction)
            // For now, we assume the detector can see in all directions within view distance
            
            // Line of sight check
            return world.Partition.HasLineOfSight(detectorId, targetId, _obstacleMask);
        }
        
        /// <summary>
        /// Check if detector can hear target
        /// </summary>
        public bool CanHearTarget(SimWorld world, SimId detectorId, SimId targetId, DetectorConfig config)
        {
            var detectorPos = world.Partition.GetPosition(detectorId);
            var targetPos = world.Partition.GetPosition(targetId);
            
            float distance = Vector3.Distance(detectorPos, targetPos);
            if (distance > config.HearingDistance) return false;
            
            // Check if target is making noise (has "making_noise" tag)
            var target = world.Entities.GetEntity(targetId);
            return target?.HasTag("making_noise") ?? false;
        }
        
        /// <summary>
        /// Get last known position of target
        /// </summary>
        public Vector3 GetLastKnownPosition(SimId detectorId)
        {
            return _detectionStates.TryGetValue(detectorId, out var state) 
                ? state.LastKnownPosition 
                : Vector3.zero;
        }
        
        /// <summary>
        /// Get time since target was last seen
        /// </summary>
        public float GetTimeSinceLastSight(SimId detectorId)
        {
            return _detectionStates.TryGetValue(detectorId, out var state) 
                ? state.TimeSinceLastSight 
                : float.MaxValue;
        }
        
        private void UpdateAlertTags(Entities.Entity detector, float alertLevel, float threshold)
        {
            // Remove old alert tags
            detector.RemoveTag("alert_none");
            detector.RemoveTag("alert_suspicious");
            detector.RemoveTag("alert_detected");
            
            // Add new alert tag
            if (alertLevel < 0.3f)
            {
                detector.AddTag("alert_none");
            }
            else if (alertLevel < threshold)
            {
                detector.AddTag("alert_suspicious");
            }
            else
            {
                detector.AddTag("alert_detected");
            }
        }
        
        /// <summary>
        /// Clear all detection states
        /// </summary>
        public void Clear()
        {
            _detectorConfigs.Clear();
            _detectionStates.Clear();
        }
    }
}

