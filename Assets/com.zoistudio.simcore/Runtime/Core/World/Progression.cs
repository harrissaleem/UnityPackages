// SimCore - Progression System
// Unlocks, achievements, ranks, and progression tracking

using System;
using System.Collections.Generic;
using SimCore.Signals;

namespace SimCore.World
{
    /// <summary>
    /// Progression signal for UI - unlock
    /// </summary>
    public struct UnlockSignal : ISignal
    {
        public ContentId UnlockId;
        public string Category;
    }
    
    /// <summary>
    /// Rank changed signal
    /// </summary>
    public struct RankChangedSignal : ISignal
    {
        public string TrackId;
        public int OldRank;
        public int NewRank;
        public string NewRankName;
        public List<string> NewUnlocks;
    }
    
    /// <summary>
    /// Milestone reached signal
    /// </summary>
    public struct MilestoneReachedSignal : ISignal
    {
        public string MilestoneId;
        public string Description;
        public Dictionary<string, int> Rewards;
    }
    
    /// <summary>
    /// Stat updated signal (for tracked career stats)
    /// </summary>
    public struct ProgressionStatChangedSignal : ISignal
    {
        public string StatId;
        public long OldValue;
        public long NewValue;
    }
    
    /// <summary>
    /// Rank definition
    /// </summary>
    [Serializable]
    public class RankDef
    {
        public int Level;
        public string Name;
        public long RequiredPoints; // Points needed to reach this rank
        public List<string> Unlocks = new(); // What unlocks at this rank
    }
    
    /// <summary>
    /// Milestone definition
    /// </summary>
    [Serializable]
    public class MilestoneDef
    {
        public string Id;
        public string Description;
        public string StatId; // Which stat to track
        public long RequiredValue; // Value needed to achieve
        public Dictionary<string, int> Rewards = new(); // currency -> amount
        public bool OneTime = true; // Only award once
    }
    
    /// <summary>
    /// Progression state for persistence
    /// </summary>
    [Serializable]
    public class ProgressionState
    {
        public Dictionary<string, List<string>> Unlocks = new();
        public Dictionary<string, int> Ranks = new(); // trackId -> rank level
        public Dictionary<string, long> Points = new(); // trackId -> current points
        public Dictionary<string, long> Stats = new(); // statId -> value
        public List<string> CompletedMilestones = new();
    }
    
    /// <summary>
    /// Tracks unlocks, ranks, stats, and progression
    /// </summary>
    public class ProgressionManager
    {
        private readonly Dictionary<string, HashSet<ContentId>> _unlocks = new();
        private readonly Dictionary<string, List<RankDef>> _rankTracks = new(); // trackId -> ranks
        private readonly Dictionary<string, int> _currentRanks = new(); // trackId -> current rank
        private readonly Dictionary<string, long> _currentPoints = new(); // trackId -> points
        private readonly Dictionary<string, long> _stats = new(); // statId -> value
        private readonly Dictionary<string, MilestoneDef> _milestones = new();
        private readonly HashSet<string> _completedMilestones = new();
        
        private readonly SignalBus _signalBus;
        
        public ProgressionManager(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }
        
        #region Unlocks
        
        /// <summary>
        /// Unlock something
        /// </summary>
        public void Unlock(ContentId unlockId, string category = "default")
        {
            if (!_unlocks.TryGetValue(category, out var set))
            {
                set = new HashSet<ContentId>();
                _unlocks[category] = set;
            }
            
            if (set.Add(unlockId))
            {
                _signalBus?.Publish(new UnlockSignal
                {
                    UnlockId = unlockId,
                    Category = category
                });
            }
        }
        
        /// <summary>
        /// Check if something is unlocked
        /// </summary>
        public bool IsUnlocked(ContentId unlockId, string category = "default")
        {
            return _unlocks.TryGetValue(category, out var set) && set.Contains(unlockId);
        }
        
        /// <summary>
        /// Get all unlocks in a category
        /// </summary>
        public IEnumerable<ContentId> GetUnlocks(string category = "default")
        {
            if (_unlocks.TryGetValue(category, out var set))
            {
                foreach (var id in set)
                    yield return id;
            }
        }
        
        /// <summary>
        /// Get all categories
        /// </summary>
        public IEnumerable<string> GetCategories() => _unlocks.Keys;
        
        #endregion
        
        #region Ranks
        
        /// <summary>
        /// Register a rank track (e.g., "career", "reputation")
        /// </summary>
        public void RegisterRankTrack(string trackId, List<RankDef> ranks)
        {
            _rankTracks[trackId] = ranks;
            if (!_currentRanks.ContainsKey(trackId))
            {
                _currentRanks[trackId] = 0;
                _currentPoints[trackId] = 0;
            }
        }
        
        /// <summary>
        /// Add points to a rank track and check for rank up
        /// </summary>
        public void AddPoints(string trackId, long points)
        {
            if (!_rankTracks.ContainsKey(trackId)) return;
            
            if (!_currentPoints.ContainsKey(trackId))
                _currentPoints[trackId] = 0;
            
            _currentPoints[trackId] += points;
            
            CheckRankUp(trackId);
        }
        
        /// <summary>
        /// Get current points for a track
        /// </summary>
        public long GetPoints(string trackId)
        {
            return _currentPoints.TryGetValue(trackId, out var points) ? points : 0;
        }
        
        /// <summary>
        /// Get current rank level for a track
        /// </summary>
        public int GetRankLevel(string trackId)
        {
            return _currentRanks.TryGetValue(trackId, out var rank) ? rank : 0;
        }
        
        /// <summary>
        /// Get current rank definition
        /// </summary>
        public RankDef GetCurrentRank(string trackId)
        {
            if (!_rankTracks.TryGetValue(trackId, out var ranks)) return null;
            int level = GetRankLevel(trackId);
            return level >= 0 && level < ranks.Count ? ranks[level] : null;
        }
        
        /// <summary>
        /// Get next rank definition (null if at max)
        /// </summary>
        public RankDef GetNextRank(string trackId)
        {
            if (!_rankTracks.TryGetValue(trackId, out var ranks)) return null;
            int level = GetRankLevel(trackId);
            return level + 1 < ranks.Count ? ranks[level + 1] : null;
        }
        
        /// <summary>
        /// Get points needed for next rank
        /// </summary>
        public long GetPointsToNextRank(string trackId)
        {
            var nextRank = GetNextRank(trackId);
            if (nextRank == null) return 0;
            return Math.Max(0, nextRank.RequiredPoints - GetPoints(trackId));
        }
        
        private void CheckRankUp(string trackId)
        {
            if (!_rankTracks.TryGetValue(trackId, out var ranks)) return;
            
            int currentRank = GetRankLevel(trackId);
            long currentPoints = GetPoints(trackId);
            
            int newRank = currentRank;
            var newUnlocks = new List<string>();
            
            // Find highest rank we qualify for
            for (int i = currentRank + 1; i < ranks.Count; i++)
            {
                if (currentPoints >= ranks[i].RequiredPoints)
                {
                    newRank = i;
                    newUnlocks.AddRange(ranks[i].Unlocks);
                }
                else
                {
                    break;
                }
            }
            
            if (newRank > currentRank)
            {
                _currentRanks[trackId] = newRank;
                
                // Apply unlocks
                foreach (var unlock in newUnlocks)
                {
                    Unlock(new ContentId(unlock), trackId);
                }
                
                _signalBus?.Publish(new RankChangedSignal
                {
                    TrackId = trackId,
                    OldRank = currentRank,
                    NewRank = newRank,
                    NewRankName = ranks[newRank].Name,
                    NewUnlocks = newUnlocks
                });
            }
        }
        
        #endregion
        
        #region Stats & Milestones
        
        /// <summary>
        /// Register a milestone
        /// </summary>
        public void RegisterMilestone(MilestoneDef milestone)
        {
            _milestones[milestone.Id] = milestone;
        }
        
        /// <summary>
        /// Increment a tracked stat
        /// </summary>
        public void IncrementStat(string statId, long amount = 1)
        {
            long oldValue = GetStat(statId);
            long newValue = oldValue + amount;
            _stats[statId] = newValue;
            
            _signalBus?.Publish(new ProgressionStatChangedSignal
            {
                StatId = statId,
                OldValue = oldValue,
                NewValue = newValue
            });
            
            CheckMilestones(statId, newValue);
        }
        
        /// <summary>
        /// Get stat value
        /// </summary>
        public long GetStat(string statId)
        {
            return _stats.TryGetValue(statId, out var value) ? value : 0;
        }
        
        /// <summary>
        /// Check if a milestone is completed
        /// </summary>
        public bool IsMilestoneComplete(string milestoneId)
        {
            return _completedMilestones.Contains(milestoneId);
        }
        
        private void CheckMilestones(string statId, long newValue)
        {
            foreach (var milestone in _milestones.Values)
            {
                if (milestone.StatId != statId) continue;
                if (milestone.OneTime && _completedMilestones.Contains(milestone.Id)) continue;
                
                if (newValue >= milestone.RequiredValue)
                {
                    _completedMilestones.Add(milestone.Id);
                    
                    _signalBus?.Publish(new MilestoneReachedSignal
                    {
                        MilestoneId = milestone.Id,
                        Description = milestone.Description,
                        Rewards = milestone.Rewards
                    });
                }
            }
        }
        
        #endregion
        
        #region Persistence
        
        /// <summary>
        /// Create snapshot for persistence
        /// </summary>
        public ProgressionState CreateSnapshot()
        {
            var state = new ProgressionState();
            
            // Unlocks
            foreach (var kvp in _unlocks)
            {
                var list = new List<string>();
                foreach (var id in kvp.Value)
                    list.Add(id.Value);
                state.Unlocks[kvp.Key] = list;
            }
            
            // Ranks
            state.Ranks = new Dictionary<string, int>(_currentRanks);
            state.Points = new Dictionary<string, long>(_currentPoints);
            
            // Stats
            state.Stats = new Dictionary<string, long>(_stats);
            
            // Milestones
            state.CompletedMilestones = new List<string>(_completedMilestones);
            
            return state;
        }
        
        /// <summary>
        /// Restore from snapshot
        /// </summary>
        public void RestoreFromSnapshot(ProgressionState state)
        {
            if (state == null) return;
            
            // Unlocks
            _unlocks.Clear();
            foreach (var kvp in state.Unlocks)
            {
                var set = new HashSet<ContentId>();
                foreach (var id in kvp.Value)
                    set.Add(new ContentId(id));
                _unlocks[kvp.Key] = set;
            }
            
            // Ranks
            _currentRanks.Clear();
            foreach (var kvp in state.Ranks)
                _currentRanks[kvp.Key] = kvp.Value;
            
            _currentPoints.Clear();
            foreach (var kvp in state.Points)
                _currentPoints[kvp.Key] = kvp.Value;
            
            // Stats
            _stats.Clear();
            foreach (var kvp in state.Stats)
                _stats[kvp.Key] = kvp.Value;
            
            // Milestones
            _completedMilestones.Clear();
            foreach (var id in state.CompletedMilestones)
                _completedMilestones.Add(id);
        }
        
        // Legacy snapshot methods for backwards compatibility
        public Dictionary<string, List<string>> CreateUnlocksSnapshot()
        {
            var snapshot = new Dictionary<string, List<string>>();
            foreach (var kvp in _unlocks)
            {
                var list = new List<string>();
                foreach (var id in kvp.Value)
                    list.Add(id.Value);
                snapshot[kvp.Key] = list;
            }
            return snapshot;
        }
        
        public void RestoreFromUnlocksSnapshot(Dictionary<string, List<string>> snapshot)
        {
            _unlocks.Clear();
            foreach (var kvp in snapshot)
            {
                var set = new HashSet<ContentId>();
                foreach (var id in kvp.Value)
                    set.Add(new ContentId(id));
                _unlocks[kvp.Key] = set;
            }
        }
        
        #endregion
        
        /// <summary>
        /// Clear all progression
        /// </summary>
        public void Clear()
        {
            _unlocks.Clear();
            _currentRanks.Clear();
            _currentPoints.Clear();
            _stats.Clear();
            _completedMilestones.Clear();
        }
    }
}

