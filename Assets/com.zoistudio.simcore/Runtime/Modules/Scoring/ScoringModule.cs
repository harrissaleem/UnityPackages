// SimCore - Scoring Module
// Multi-currency scoring system for game points (SP, CP, XP, reputation, etc.)

using System;
using System.Collections.Generic;
using SimCore.Signals;

namespace SimCore.Modules.Scoring
{
    /// <summary>
    /// Score changed signal
    /// </summary>
    public struct ScoreChangedSignal : ISignal
    {
        public string CurrencyId;
        public int OldValue;
        public int NewValue;
        public int Delta;
        public string Reason;
        public SimId EntityId; // Invalid if global
    }
    
    /// <summary>
    /// Score threshold reached signal
    /// </summary>
    public struct ScoreThresholdSignal : ISignal
    {
        public string CurrencyId;
        public int CurrentValue;
        public int ThresholdValue;
        public bool IsMinimum; // true = hit min, false = hit max
        public SimId EntityId;
    }
    
    /// <summary>
    /// Currency definition
    /// </summary>
    [Serializable]
    public class CurrencyDef
    {
        public string Id;
        public string DisplayName;
        public int InitialValue;
        public int MinValue = int.MinValue;
        public int MaxValue = int.MaxValue;
        public bool EmitSignalOnChange = true;
    }
    
    /// <summary>
    /// Currency state (for persistence)
    /// </summary>
    [Serializable]
    public class CurrencyState
    {
        public string CurrencyId;
        public int Value;
    }
    
    /// <summary>
    /// Scoring module interface
    /// </summary>
    public interface IScoringModule : ISimModule
    {
        void RegisterCurrency(CurrencyDef def);
        void RegisterCurrency(string id, int initialValue = 0, int minValue = int.MinValue, int maxValue = int.MaxValue);
        
        int GetScore(string currencyId, SimId entityId = default);
        void SetScore(string currencyId, int value, SimId entityId = default, string reason = null);
        void AddScore(string currencyId, int amount, SimId entityId = default, string reason = null);
        bool TryDeductScore(string currencyId, int amount, SimId entityId = default, string reason = null);
        
        bool HasCurrency(string currencyId);
        CurrencyDef GetCurrencyDef(string currencyId);
        IEnumerable<string> GetAllCurrencyIds();
        
        void ResetCurrency(string currencyId, SimId entityId = default);
        void ResetAllCurrencies(SimId entityId = default);
        
        // Persistence
        List<CurrencyState> CreateSnapshot(SimId entityId = default);
        void RestoreFromSnapshot(List<CurrencyState> snapshot, SimId entityId = default);
    }
    
    /// <summary>
    /// Scoring module implementation
    /// </summary>
    public class ScoringModule : IScoringModule
    {
        private readonly Dictionary<string, CurrencyDef> _currencyDefs = new();
        
        // Global scores (entityId = Invalid)
        private readonly Dictionary<string, int> _globalScores = new();
        
        // Per-entity scores
        private readonly Dictionary<SimId, Dictionary<string, int>> _entityScores = new();
        
        private SignalBus _signalBus;
        private SimWorld _world;
        
        public ScoringModule() { }
        
        #region ISimModule
        
        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
        }
        
        public void Tick(float deltaTime)
        {
            // Scoring doesn't need tick updates
        }
        
        public void Shutdown()
        {
            _currencyDefs.Clear();
            _globalScores.Clear();
            _entityScores.Clear();
        }
        
        #endregion
        
        #region Registration
        
        public void RegisterCurrency(CurrencyDef def)
        {
            _currencyDefs[def.Id] = def;
            _globalScores[def.Id] = def.InitialValue;
        }
        
        public void RegisterCurrency(string id, int initialValue = 0, int minValue = int.MinValue, int maxValue = int.MaxValue)
        {
            RegisterCurrency(new CurrencyDef
            {
                Id = id,
                DisplayName = id,
                InitialValue = initialValue,
                MinValue = minValue,
                MaxValue = maxValue
            });
        }
        
        public bool HasCurrency(string currencyId)
        {
            return _currencyDefs.ContainsKey(currencyId);
        }
        
        public CurrencyDef GetCurrencyDef(string currencyId)
        {
            return _currencyDefs.TryGetValue(currencyId, out var def) ? def : null;
        }
        
        public IEnumerable<string> GetAllCurrencyIds()
        {
            return _currencyDefs.Keys;
        }
        
        #endregion
        
        #region Score Operations
        
        public int GetScore(string currencyId, SimId entityId = default)
        {
            if (!entityId.IsValid)
            {
                // Global score
                return _globalScores.TryGetValue(currencyId, out var value) ? value : 0;
            }
            
            // Per-entity score
            if (_entityScores.TryGetValue(entityId, out var scores))
            {
                return scores.TryGetValue(currencyId, out var value) ? value : GetInitialValue(currencyId);
            }
            
            return GetInitialValue(currencyId);
        }
        
        public void SetScore(string currencyId, int value, SimId entityId = default, string reason = null)
        {
            var def = GetCurrencyDef(currencyId);
            int oldValue = GetScore(currencyId, entityId);
            int newValue = ClampValue(value, def);
            
            if (!entityId.IsValid)
            {
                _globalScores[currencyId] = newValue;
            }
            else
            {
                EnsureEntityScores(entityId)[currencyId] = newValue;
            }
            
            if (oldValue != newValue)
            {
                EmitChangeSignal(currencyId, oldValue, newValue, reason, entityId, def);
                CheckThresholds(currencyId, newValue, entityId, def);
            }
        }
        
        public void AddScore(string currencyId, int amount, SimId entityId = default, string reason = null)
        {
            int current = GetScore(currencyId, entityId);
            SetScore(currencyId, current + amount, entityId, reason);
        }
        
        public bool TryDeductScore(string currencyId, int amount, SimId entityId = default, string reason = null)
        {
            int current = GetScore(currencyId, entityId);
            var def = GetCurrencyDef(currencyId);
            
            int newValue = current - amount;
            
            // Check if deduction would go below minimum
            if (def != null && newValue < def.MinValue)
            {
                return false;
            }
            
            SetScore(currencyId, newValue, entityId, reason);
            return true;
        }
        
        public void ResetCurrency(string currencyId, SimId entityId = default)
        {
            int initialValue = GetInitialValue(currencyId);
            SetScore(currencyId, initialValue, entityId, "reset");
        }
        
        public void ResetAllCurrencies(SimId entityId = default)
        {
            foreach (var currencyId in _currencyDefs.Keys)
            {
                ResetCurrency(currencyId, entityId);
            }
        }
        
        #endregion
        
        #region Helpers
        
        private int GetInitialValue(string currencyId)
        {
            return _currencyDefs.TryGetValue(currencyId, out var def) ? def.InitialValue : 0;
        }
        
        private int ClampValue(int value, CurrencyDef def)
        {
            if (def == null) return value;
            return Math.Clamp(value, def.MinValue, def.MaxValue);
        }
        
        private Dictionary<string, int> EnsureEntityScores(SimId entityId)
        {
            if (!_entityScores.TryGetValue(entityId, out var scores))
            {
                scores = new Dictionary<string, int>();
                _entityScores[entityId] = scores;
            }
            return scores;
        }
        
        private void EmitChangeSignal(string currencyId, int oldValue, int newValue, string reason, SimId entityId, CurrencyDef def)
        {
            if (def?.EmitSignalOnChange != false)
            {
                _signalBus?.Publish(new ScoreChangedSignal
                {
                    CurrencyId = currencyId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Delta = newValue - oldValue,
                    Reason = reason,
                    EntityId = entityId
                });
            }
        }
        
        private void CheckThresholds(string currencyId, int newValue, SimId entityId, CurrencyDef def)
        {
            if (def == null) return;
            
            if (newValue <= def.MinValue && def.MinValue != int.MinValue)
            {
                _signalBus?.Publish(new ScoreThresholdSignal
                {
                    CurrencyId = currencyId,
                    CurrentValue = newValue,
                    ThresholdValue = def.MinValue,
                    IsMinimum = true,
                    EntityId = entityId
                });
            }
            else if (newValue >= def.MaxValue && def.MaxValue != int.MaxValue)
            {
                _signalBus?.Publish(new ScoreThresholdSignal
                {
                    CurrencyId = currencyId,
                    CurrentValue = newValue,
                    ThresholdValue = def.MaxValue,
                    IsMinimum = false,
                    EntityId = entityId
                });
            }
        }
        
        #endregion
        
        #region Persistence
        
        public List<CurrencyState> CreateSnapshot(SimId entityId = default)
        {
            var snapshot = new List<CurrencyState>();
            
            if (!entityId.IsValid)
            {
                foreach (var kvp in _globalScores)
                {
                    snapshot.Add(new CurrencyState { CurrencyId = kvp.Key, Value = kvp.Value });
                }
            }
            else if (_entityScores.TryGetValue(entityId, out var scores))
            {
                foreach (var kvp in scores)
                {
                    snapshot.Add(new CurrencyState { CurrencyId = kvp.Key, Value = kvp.Value });
                }
            }
            
            return snapshot;
        }
        
        public void RestoreFromSnapshot(List<CurrencyState> snapshot, SimId entityId = default)
        {
            if (snapshot == null) return;
            
            foreach (var state in snapshot)
            {
                if (!entityId.IsValid)
                {
                    _globalScores[state.CurrencyId] = state.Value;
                }
                else
                {
                    EnsureEntityScores(entityId)[state.CurrencyId] = state.Value;
                }
            }
        }
        
        #endregion
    }
}

