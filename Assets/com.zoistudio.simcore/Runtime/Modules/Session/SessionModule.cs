// SimCore - Session Module
// Timed game sessions (shifts, days, rounds, missions, etc.)

using System;
using System.Collections.Generic;
using UnityEngine;
using SimCore.Signals;

namespace SimCore.Modules.Session
{
    /// <summary>
    /// Session started signal
    /// </summary>
    public struct SessionStartedSignal : ISignal
    {
        public string SessionId;
        public string DisplayName;
        public float DurationSeconds;
    }
    
    /// <summary>
    /// Session time update signal (emitted once per second)
    /// </summary>
    public struct SessionTimeUpdateSignal : ISignal
    {
        public string SessionId;
        public float ElapsedSeconds;
        public float RemainingSeconds;
        public float TotalSeconds;
        public float Progress; // 0-1
    }
    
    /// <summary>
    /// Session paused/resumed signal
    /// </summary>
    public struct SessionPausedSignal : ISignal
    {
        public string SessionId;
        public bool IsPaused;
    }
    
    /// <summary>
    /// Session ended signal
    /// </summary>
    public struct SessionEndedSignal : ISignal
    {
        public string SessionId;
        public bool Completed; // true = time ran out or ended normally, false = failed/cancelled
        public float FinalElapsedSeconds;
        public SessionEndReason Reason;
    }
    
    public enum SessionEndReason
    {
        TimeExpired,
        Completed,
        Failed,
        Cancelled,
        EndedEarly
    }
    
    /// <summary>
    /// Session configuration
    /// </summary>
    [Serializable]
    public class SessionConfig
    {
        public string Id;
        public string DisplayName;
        public float DurationSeconds;
        public bool EndOnTimeExpired = true;
        public bool PauseOnStart = false;
        
        // Optional metadata (game-specific)
        public Dictionary<string, object> Metadata = new();
    }
    
    /// <summary>
    /// Session state (for persistence)
    /// </summary>
    [Serializable]
    public class SessionState
    {
        public string SessionId;
        public string DisplayName;
        public float ElapsedSeconds;
        public float TotalSeconds;
        public bool IsActive;
        public bool IsPaused;
        public string MetadataJson;
    }
    
    /// <summary>
    /// Session module interface
    /// </summary>
    public interface ISessionModule : ISimModule
    {
        void StartSession(SessionConfig config);
        void EndSession(SessionEndReason reason = SessionEndReason.Completed);
        void PauseSession();
        void ResumeSession();
        void SetPaused(bool paused);
        
        bool IsSessionActive { get; }
        bool IsPaused { get; }
        string CurrentSessionId { get; }
        float ElapsedSeconds { get; }
        float RemainingSeconds { get; }
        float TotalDuration { get; }
        float Progress { get; } // 0-1
        
        // Access metadata
        T GetMetadata<T>(string key, T defaultValue = default);
        void SetMetadata<T>(string key, T value);
        
        // Persistence
        SessionState GetState();
        void RestoreState(SessionState state);
    }
    
    /// <summary>
    /// Session module implementation
    /// </summary>
    public class SessionModule : ISessionModule
    {
        private SignalBus _signalBus;
        private SimWorld _world;
        
        private SessionConfig _config;
        private float _elapsedSeconds;
        private bool _isActive;
        private bool _isPaused;
        private float _lastTimeUpdate;
        
        public bool IsSessionActive => _isActive;
        public bool IsPaused => _isPaused;
        public string CurrentSessionId => _config?.Id;
        public float ElapsedSeconds => _elapsedSeconds;
        public float TotalDuration => _config?.DurationSeconds ?? 0;
        public float RemainingSeconds => Math.Max(0, TotalDuration - _elapsedSeconds);
        public float Progress => TotalDuration > 0 ? Math.Clamp(_elapsedSeconds / TotalDuration, 0f, 1f) : 0f;
        
        public SessionModule() { }
        
        #region ISimModule
        
        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
        }
        
        public void Tick(float deltaTime)
        {
            if (!_isActive || _isPaused) return;
            
            _elapsedSeconds += deltaTime;
            
            // Emit time update roughly once per second
            if (Mathf.Floor(_elapsedSeconds) > Mathf.Floor(_lastTimeUpdate))
            {
                _lastTimeUpdate = _elapsedSeconds;
                
                _signalBus?.Publish(new SessionTimeUpdateSignal
                {
                    SessionId = _config.Id,
                    ElapsedSeconds = _elapsedSeconds,
                    RemainingSeconds = RemainingSeconds,
                    TotalSeconds = TotalDuration,
                    Progress = Progress
                });
            }
            
            // Check if time expired
            if (_config.EndOnTimeExpired && _elapsedSeconds >= TotalDuration)
            {
                EndSession(SessionEndReason.TimeExpired);
            }
        }
        
        public void Shutdown()
        {
            if (_isActive)
            {
                EndSession(SessionEndReason.Cancelled);
            }
        }
        
        #endregion
        
        #region Session Lifecycle
        
        public void StartSession(SessionConfig config)
        {
            if (_isActive)
            {
                SimCoreLogger.LogWarning("[SessionModule] Session already active. End it first.");
                return;
            }
            
            _config = config;
            _elapsedSeconds = 0;
            _lastTimeUpdate = 0;
            _isActive = true;
            _isPaused = config.PauseOnStart;
            
            _signalBus?.Publish(new SessionStartedSignal
            {
                SessionId = config.Id,
                DisplayName = config.DisplayName,
                DurationSeconds = config.DurationSeconds
            });
            
            SimCoreLogger.Log($"[SessionModule] Session started: {config.DisplayName} ({config.DurationSeconds}s)");
        }
        
        public void EndSession(SessionEndReason reason = SessionEndReason.Completed)
        {
            if (!_isActive) return;
            
            _isActive = false;
            
            bool completed = reason == SessionEndReason.TimeExpired || 
                           reason == SessionEndReason.Completed ||
                           reason == SessionEndReason.EndedEarly;
            
            _signalBus?.Publish(new SessionEndedSignal
            {
                SessionId = _config.Id,
                Completed = completed,
                FinalElapsedSeconds = _elapsedSeconds,
                Reason = reason
            });
            
            SimCoreLogger.Log($"[SessionModule] Session ended: {_config.Id}, Reason: {reason}, Elapsed: {_elapsedSeconds:F1}s");
        }
        
        public void PauseSession()
        {
            if (!_isActive || _isPaused) return;
            
            _isPaused = true;
            
            _signalBus?.Publish(new SessionPausedSignal
            {
                SessionId = _config.Id,
                IsPaused = true
            });
        }
        
        public void ResumeSession()
        {
            if (!_isActive || !_isPaused) return;
            
            _isPaused = false;
            
            _signalBus?.Publish(new SessionPausedSignal
            {
                SessionId = _config.Id,
                IsPaused = false
            });
        }
        
        public void SetPaused(bool paused)
        {
            if (paused)
                PauseSession();
            else
                ResumeSession();
        }
        
        #endregion
        
        #region Metadata
        
        public T GetMetadata<T>(string key, T defaultValue = default)
        {
            if (_config?.Metadata == null) return defaultValue;
            
            if (_config.Metadata.TryGetValue(key, out var value) && value is T typed)
            {
                return typed;
            }
            
            return defaultValue;
        }
        
        public void SetMetadata<T>(string key, T value)
        {
            if (_config == null)
            {
                SimCoreLogger.LogWarning("[SessionModule] Cannot set metadata when no session is active.");
                return;
            }
            
            _config.Metadata ??= new Dictionary<string, object>();
            _config.Metadata[key] = value;
        }
        
        #endregion
        
        #region Persistence
        
        public SessionState GetState()
        {
            if (!_isActive) return null;
            
            string metadataJson = null;
            if (_config.Metadata != null && _config.Metadata.Count > 0)
            {
                try
                {
                    // Simple JSON serialization for metadata
                    metadataJson = JsonUtility.ToJson(new MetadataWrapper { Data = _config.Metadata });
                }
                catch
                {
                    metadataJson = null;
                }
            }
            
            return new SessionState
            {
                SessionId = _config.Id,
                DisplayName = _config.DisplayName,
                ElapsedSeconds = _elapsedSeconds,
                TotalSeconds = TotalDuration,
                IsActive = _isActive,
                IsPaused = _isPaused,
                MetadataJson = metadataJson
            };
        }
        
        public void RestoreState(SessionState state)
        {
            if (state == null || !state.IsActive) return;
            
            _config = new SessionConfig
            {
                Id = state.SessionId,
                DisplayName = state.DisplayName,
                DurationSeconds = state.TotalSeconds
            };
            
            _elapsedSeconds = state.ElapsedSeconds;
            _lastTimeUpdate = state.ElapsedSeconds;
            _isActive = true;
            _isPaused = state.IsPaused;
            
            SimCoreLogger.Log($"[SessionModule] Session restored: {state.SessionId}, Elapsed: {state.ElapsedSeconds:F1}s");
        }
        
        [Serializable]
        private class MetadataWrapper
        {
            public Dictionary<string, object> Data;
        }
        
        #endregion
    }
}

