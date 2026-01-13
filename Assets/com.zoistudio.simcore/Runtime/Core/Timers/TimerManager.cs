// SimCore - Timer Manager
// Manages time-based triggers and delays

using System;
using System.Collections.Generic;
using SimCore.Effects;
using SimCore.Signals;

namespace SimCore.Timers
{
    /// <summary>
    /// A timer instance
    /// </summary>
    [Serializable]
    public class SimTimer
    {
        public ContentId TimerId;
        public SimId OwnerId;
        public SimTime StartTime;
        public SimTime EndTime;
        public float Duration;
        public List<Effect> CompletionEffects = new();
        public bool Repeating;
        public bool IsActive = true;
        
        public float Progress(SimTime currentTime)
        {
            if (Duration <= 0) return 1f;
            float elapsed = (currentTime - StartTime).Seconds;
            return Math.Clamp(elapsed / Duration, 0f, 1f);
        }
        
        public float RemainingTime(SimTime currentTime)
        {
            return Math.Max(0, (EndTime - currentTime).Seconds);
        }
        
        public bool IsCompleted(SimTime currentTime) => currentTime >= EndTime;
    }
    
    /// <summary>
    /// Timer snapshot for persistence
    /// </summary>
    [Serializable]
    public class TimerSnapshot
    {
        public ContentId TimerId;
        public SimId OwnerId;
        public float EndTimeSeconds; // Absolute end time
        public float Duration;
        public List<Effect> CompletionEffects;
        public bool Repeating;
    }
    
    /// <summary>
    /// Manages all timers in the simulation
    /// </summary>
    public class TimerManager
    {
        private readonly List<SimTimer> _timers = new();
        private readonly SignalBus _signalBus;
        private readonly Func<SimWorld> _worldGetter;
        
        public TimerManager(SignalBus signalBus, Func<SimWorld> worldGetter)
        {
            _signalBus = signalBus;
            _worldGetter = worldGetter;
        }
        
        /// <summary>
        /// Start a new timer
        /// </summary>
        public SimTimer StartTimer(ContentId timerId, SimId ownerId, float durationSeconds, 
            List<Effect> completionEffects = null, bool repeating = false)
        {
            var world = _worldGetter();
            var timer = new SimTimer
            {
                TimerId = timerId,
                OwnerId = ownerId,
                StartTime = world.CurrentTime,
                EndTime = world.CurrentTime + SimTime.FromSeconds(durationSeconds),
                Duration = durationSeconds,
                CompletionEffects = completionEffects ?? new List<Effect>(),
                Repeating = repeating
            };
            
            _timers.Add(timer);
            return timer;
        }
        
        /// <summary>
        /// Cancel a timer
        /// </summary>
        public bool CancelTimer(ContentId timerId, SimId ownerId)
        {
            for (int i = _timers.Count - 1; i >= 0; i--)
            {
                var timer = _timers[i];
                if (timer.TimerId == timerId && timer.OwnerId == ownerId)
                {
                    _timers.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Cancel all timers for an owner
        /// </summary>
        public void CancelAllForOwner(SimId ownerId)
        {
            _timers.RemoveAll(t => t.OwnerId == ownerId);
        }
        
        /// <summary>
        /// Check if a timer is active
        /// </summary>
        public bool IsTimerActive(ContentId timerId, SimId ownerId)
        {
            foreach (var timer in _timers)
            {
                if (timer.TimerId == timerId && timer.OwnerId == ownerId && timer.IsActive)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get a timer
        /// </summary>
        public SimTimer GetTimer(ContentId timerId, SimId ownerId)
        {
            foreach (var timer in _timers)
            {
                if (timer.TimerId == timerId && timer.OwnerId == ownerId)
                    return timer;
            }
            return null;
        }
        
        /// <summary>
        /// Get all active timers
        /// </summary>
        public IReadOnlyList<SimTimer> GetAllTimers() => _timers;
        
        /// <summary>
        /// Tick all timers and fire completed ones
        /// </summary>
        public void Tick()
        {
            var world = _worldGetter();
            var currentTime = world.CurrentTime;
            var completedTimers = new List<SimTimer>();
            
            foreach (var timer in _timers)
            {
                if (!timer.IsActive) continue;
                
                if (timer.IsCompleted(currentTime))
                {
                    completedTimers.Add(timer);
                }
            }
            
            foreach (var timer in completedTimers)
            {
                // Apply completion effects
                var effectCtx = new EffectContext(world)
                {
                    ActorId = timer.OwnerId,
                    TargetId = timer.OwnerId
                };
                
                foreach (var effect in timer.CompletionEffects)
                {
                    effect.Apply(effectCtx);
                }
                
                // Emit signal
                _signalBus.Publish(new TimerCompletedSignal
                {
                    TimerId = timer.TimerId,
                    OwnerId = timer.OwnerId
                });
                
                if (timer.Repeating)
                {
                    // Reset for next cycle
                    timer.StartTime = currentTime;
                    timer.EndTime = currentTime + SimTime.FromSeconds(timer.Duration);
                }
                else
                {
                    timer.IsActive = false;
                }
            }
            
            // Remove inactive timers
            _timers.RemoveAll(t => !t.IsActive);
        }
        
        /// <summary>
        /// Create snapshots for persistence
        /// </summary>
        public List<TimerSnapshot> CreateSnapshots()
        {
            var snapshots = new List<TimerSnapshot>();
            foreach (var timer in _timers)
            {
                if (!timer.IsActive) continue;
                
                snapshots.Add(new TimerSnapshot
                {
                    TimerId = timer.TimerId,
                    OwnerId = timer.OwnerId,
                    EndTimeSeconds = timer.EndTime.Seconds,
                    Duration = timer.Duration,
                    CompletionEffects = timer.CompletionEffects,
                    Repeating = timer.Repeating
                });
            }
            return snapshots;
        }
        
        /// <summary>
        /// Restore from snapshots
        /// </summary>
        public void RestoreFromSnapshots(List<TimerSnapshot> snapshots, SimTime currentTime)
        {
            _timers.Clear();
            
            foreach (var snapshot in snapshots)
            {
                var endTime = SimTime.FromSeconds(snapshot.EndTimeSeconds);
                var startTime = endTime - SimTime.FromSeconds(snapshot.Duration);
                
                var timer = new SimTimer
                {
                    TimerId = snapshot.TimerId,
                    OwnerId = snapshot.OwnerId,
                    StartTime = startTime,
                    EndTime = endTime,
                    Duration = snapshot.Duration,
                    CompletionEffects = snapshot.CompletionEffects ?? new List<Effect>(),
                    Repeating = snapshot.Repeating,
                    IsActive = true
                };
                
                _timers.Add(timer);
            }
        }
        
        /// <summary>
        /// Clear all timers
        /// </summary>
        public void Clear() => _timers.Clear();
    }
}

