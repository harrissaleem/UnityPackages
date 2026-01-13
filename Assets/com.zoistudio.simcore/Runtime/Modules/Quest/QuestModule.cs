// SimCore - Quest Module (Unified Quests/Objectives/Achievements)
// Single system for all quest tracking with different lifecycles
// Replaces separate Quest, Objective, and Achievement systems

using System;
using System.Collections.Generic;
using SimCore.Signals;

namespace SimCore.Modules.Quest
{
    #region Signals
    
    /// <summary>
    /// Quest progress updated
    /// </summary>
    public struct QuestProgressSignal : ISignal
    {
        public string QuestId;
        public string Description;
        public int CurrentProgress;
        public int RequiredProgress;
        public float ProgressPercent;
        public bool IsComplete;
        public QuestCategory Category;
    }
    
    /// <summary>
    /// Quest completed
    /// </summary>
    public struct QuestCompletedSignal : ISignal
    {
        public string QuestId;
        public string Description;
        public QuestCategory Category;
        public Dictionary<string, int> Rewards;
        public bool WasAlreadyClaimed;
    }
    
    /// <summary>
    /// Quest failed (timed out, session ended before completion)
    /// </summary>
    public struct QuestFailedSignal : ISignal
    {
        public string QuestId;
        public string Description;
        public string Reason;
    }
    
    /// <summary>
    /// Quests reset (daily reset, session end, etc.)
    /// </summary>
    public struct QuestsResetSignal : ISignal
    {
        public ResetPolicy ResetType;
        public int QuestsReset;
    }
    
    #endregion
    
    #region Enums
    
    /// <summary>
    /// Quest lifecycle - when does this quest reset?
    /// </summary>
    public enum ResetPolicy
    {
        Never,          // Permanent achievements, story quests
        OnSessionEnd,   // "Complete 3 tasks this session"
        Daily,          // "Complete 2 sessions today"
        Weekly,         // "Earn 500 SP this week"
        Manual          // Reset only when explicitly called
    }
    
    /// <summary>
    /// Quest category for UI organization
    /// </summary>
    public enum QuestCategory
    {
        Main,           // Main story/progression
        Session,        // Current session objectives
        Daily,          // Daily challenges
        Weekly,         // Weekly challenges
        Achievement,    // One-time achievements
        Hidden          // Secret/hidden quests
    }
    
    /// <summary>
    /// Quest status
    /// </summary>
    public enum QuestStatus
    {
        Locked,         // Prerequisites not met
        Active,         // In progress
        Completed,      // Done (rewards claimed)
        Failed          // Failed (timed out, etc.)
    }
    
    #endregion
    
    #region Definitions
    
    /// <summary>
    /// Quest definition (data)
    /// </summary>
    [Serializable]
    public class QuestDef
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public QuestCategory Category = QuestCategory.Session;
        public ResetPolicy ResetPolicy = ResetPolicy.OnSessionEnd;
        
        // Progress tracking
        public int RequiredProgress = 1;
        public bool ShowProgressInUI = true;
        
        // Auto-progress triggers
        public string TriggerActionId;      // Progress when action with this ID prefix completes
        public string TriggerStatId;        // Progress when this stat changes
        public string TriggerSignalType;    // Progress when this signal type fires
        
        // Rewards
        public Dictionary<string, int> Rewards = new(); // currency -> amount
        
        // Prerequisites (optional)
        public List<string> PrerequisiteQuestIds = new();
        public int PrerequisiteRank = 0;
        
        // Multi-stage support (optional)
        public List<QuestStageDef> Stages = new();
        
        // Timing (optional)
        public float TimeoutSeconds = 0;    // 0 = no timeout
        
        // Metadata
        public Dictionary<string, object> Metadata = new();
    }
    
    /// <summary>
    /// Quest stage definition (for multi-stage quests)
    /// </summary>
    [Serializable]
    public class QuestStageDef
    {
        public string Id;
        public string Description;
        public int RequiredProgress = 1;
        public string TriggerActionId;
        public Dictionary<string, int> StageRewards = new();
    }
    
    #endregion
    
    #region State
    
    /// <summary>
    /// Quest instance (runtime state)
    /// </summary>
    [Serializable]
    public class QuestState
    {
        public string Id;
        public QuestDef Definition;
        public QuestStatus Status;
        public int CurrentProgress;
        public int CurrentStage;        // For multi-stage quests
        public float StartTime;
        public float CompletionTime;
        public bool RewardsClaimed;
        
        // Computed
        public float ProgressPercent => Definition.RequiredProgress > 0 
            ? Math.Clamp((float)CurrentProgress / Definition.RequiredProgress, 0f, 1f) 
            : 0f;
        public bool IsComplete => Status == QuestStatus.Completed;
        public bool IsActive => Status == QuestStatus.Active;
    }
    
    /// <summary>
    /// Persistence snapshot
    /// </summary>
    [Serializable]
    public class QuestSnapshot
    {
        public string Id;
        public int CurrentProgress;
        public int CurrentStage;
        public int Status; // enum as int
        public float StartTime;
        public float CompletionTime;
        public bool RewardsClaimed;
    }
    
    #endregion
    
    /// <summary>
    /// Quest module interface
    /// </summary>
    public interface IQuestModule : ISimModule
    {
        // Registration
        void RegisterQuest(QuestDef def);
        void RegisterQuests(IEnumerable<QuestDef> defs);
        QuestDef GetQuestDef(string questId);
        
        // Activation
        void ActivateQuest(string questId);
        void DeactivateQuest(string questId);
        
        // Progress
        void ProgressQuest(string questId, int amount = 1);
        void SetProgress(string questId, int progress);
        void CompleteQuest(string questId);
        void FailQuest(string questId, string reason = null);
        
        // Queries
        QuestState GetQuest(string questId);
        IEnumerable<QuestState> GetAllQuests();
        IEnumerable<QuestState> GetQuestsByCategory(QuestCategory category);
        IEnumerable<QuestState> GetActiveQuests();
        IEnumerable<QuestState> GetCompletedQuests();
        
        int GetActiveCount();
        int GetCompletedCount();
        int GetCompletedCount(QuestCategory category);
        
        // Reset
        void ResetQuests(ResetPolicy policy);
        void ResetAll();
        
        // Persistence
        List<QuestSnapshot> CreateSnapshot();
        void RestoreFromSnapshot(List<QuestSnapshot> snapshot);
    }
    
    /// <summary>
    /// Quest module implementation
    /// </summary>
    public class QuestModule : IQuestModule
    {
        private readonly Dictionary<string, QuestDef> _definitions = new();
        private readonly Dictionary<string, QuestState> _quests = new();
        
        private SignalBus _signalBus;
        private SimWorld _world;
        
        public QuestModule() { }
        
        #region ISimModule
        
        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
            
            // Subscribe to action signals for auto-progress
            _signalBus.Subscribe<ActionCompletedSignal>(OnActionCompleted);
        }
        
        public void Tick(float deltaTime)
        {
            // Check for timed-out quests
            float currentTime = UnityEngine.Time.time;
            
            foreach (var quest in _quests.Values)
            {
                if (quest.Status != QuestStatus.Active) continue;
                if (quest.Definition.TimeoutSeconds <= 0) continue;
                
                if (currentTime - quest.StartTime >= quest.Definition.TimeoutSeconds)
                {
                    FailQuest(quest.Id, "Timed out");
                }
            }
        }
        
        public void Shutdown()
        {
            _definitions.Clear();
            _quests.Clear();
        }
        
        #endregion
        
        #region Registration
        
        public void RegisterQuest(QuestDef def)
        {
            _definitions[def.Id] = def;
        }
        
        public void RegisterQuests(IEnumerable<QuestDef> defs)
        {
            foreach (var def in defs) RegisterQuest(def);
        }
        
        public QuestDef GetQuestDef(string questId)
        {
            return _definitions.TryGetValue(questId, out var def) ? def : null;
        }
        
        #endregion
        
        #region Activation
        
        public void ActivateQuest(string questId)
        {
            if (!_definitions.TryGetValue(questId, out var def)) return;
            
            // Check if already exists
            if (_quests.TryGetValue(questId, out var existing))
            {
                if (existing.Status == QuestStatus.Active) return;
                if (existing.Status == QuestStatus.Completed && def.ResetPolicy == ResetPolicy.Never)
                    return; // Already permanently completed
            }
            
            // Check prerequisites
            if (!CheckPrerequisites(def)) return;
            
            _quests[questId] = new QuestState
            {
                Id = questId,
                Definition = def,
                Status = QuestStatus.Active,
                CurrentProgress = 0,
                CurrentStage = 0,
                StartTime = UnityEngine.Time.time,
                RewardsClaimed = false
            };
            
            SimCoreLogger.Log($"[QuestModule] Quest activated: {def.DisplayName}");
        }
        
        public void DeactivateQuest(string questId)
        {
            if (_quests.TryGetValue(questId, out var quest))
            {
                if (quest.Status == QuestStatus.Active)
                {
                    _quests.Remove(questId);
                }
            }
        }
        
        private bool CheckPrerequisites(QuestDef def)
        {
            // Check prerequisite quests
            foreach (var prereqId in def.PrerequisiteQuestIds)
            {
                if (!_quests.TryGetValue(prereqId, out var prereq) || 
                    prereq.Status != QuestStatus.Completed)
                {
                    return false;
                }
            }
            
            // Check rank prerequisite
            if (def.PrerequisiteRank > 0)
            {
                int currentRank = _world.Progression.GetRankLevel("career");
                if (currentRank < def.PrerequisiteRank)
                    return false;
            }
            
            return true;
        }
        
        #endregion
        
        #region Progress
        
        public void ProgressQuest(string questId, int amount = 1)
        {
            if (!_quests.TryGetValue(questId, out var quest)) return;
            if (quest.Status != QuestStatus.Active) return;
            
            quest.CurrentProgress += amount;
            
            _signalBus?.Publish(new QuestProgressSignal
            {
                QuestId = questId,
                Description = quest.Definition.Description,
                CurrentProgress = quest.CurrentProgress,
                RequiredProgress = quest.Definition.RequiredProgress,
                ProgressPercent = quest.ProgressPercent,
                IsComplete = quest.CurrentProgress >= quest.Definition.RequiredProgress,
                Category = quest.Definition.Category
            });
            
            // Auto-complete check
            if (quest.CurrentProgress >= quest.Definition.RequiredProgress)
            {
                CompleteQuest(questId);
            }
        }
        
        public void SetProgress(string questId, int progress)
        {
            if (!_quests.TryGetValue(questId, out var quest)) return;
            if (quest.Status != QuestStatus.Active) return;
            
            int oldProgress = quest.CurrentProgress;
            quest.CurrentProgress = Math.Max(0, progress);
            
            if (quest.CurrentProgress != oldProgress)
            {
                _signalBus?.Publish(new QuestProgressSignal
                {
                    QuestId = questId,
                    Description = quest.Definition.Description,
                    CurrentProgress = quest.CurrentProgress,
                    RequiredProgress = quest.Definition.RequiredProgress,
                    ProgressPercent = quest.ProgressPercent,
                    IsComplete = quest.CurrentProgress >= quest.Definition.RequiredProgress,
                    Category = quest.Definition.Category
                });
                
                if (quest.CurrentProgress >= quest.Definition.RequiredProgress)
                {
                    CompleteQuest(questId);
                }
            }
        }
        
        public void CompleteQuest(string questId)
        {
            if (!_quests.TryGetValue(questId, out var quest)) return;
            if (quest.Status == QuestStatus.Completed && quest.RewardsClaimed) return;
            
            bool wasAlreadyCompleted = quest.Status == QuestStatus.Completed;
            
            quest.Status = QuestStatus.Completed;
            quest.CompletionTime = UnityEngine.Time.time;
            quest.CurrentProgress = quest.Definition.RequiredProgress;
            
            bool claimRewards = !quest.RewardsClaimed;
            quest.RewardsClaimed = true;
            
            _signalBus?.Publish(new QuestCompletedSignal
            {
                QuestId = questId,
                Description = quest.Definition.Description,
                Category = quest.Definition.Category,
                Rewards = claimRewards ? quest.Definition.Rewards : new Dictionary<string, int>(),
                WasAlreadyClaimed = wasAlreadyCompleted
            });
            
            SimCoreLogger.Log($"[QuestModule] Quest completed: {quest.Definition.DisplayName}");
        }
        
        public void FailQuest(string questId, string reason = null)
        {
            if (!_quests.TryGetValue(questId, out var quest)) return;
            if (quest.Status != QuestStatus.Active) return;
            
            quest.Status = QuestStatus.Failed;
            
            _signalBus?.Publish(new QuestFailedSignal
            {
                QuestId = questId,
                Description = quest.Definition.Description,
                Reason = reason ?? "Failed"
            });
        }
        
        #endregion
        
        #region Auto-Progress
        
        private void OnActionCompleted(ActionCompletedSignal signal)
        {
            foreach (var quest in _quests.Values)
            {
                if (quest.Status != QuestStatus.Active) continue;
                
                var triggerAction = quest.Definition.TriggerActionId;
                if (string.IsNullOrEmpty(triggerAction)) continue;
                
                // Match exact or prefix
                if (signal.ActionId.Value == triggerAction ||
                    signal.ActionId.Value.StartsWith(triggerAction))
                {
                    ProgressQuest(quest.Id, 1);
                }
            }
        }
        
        #endregion
        
        #region Queries
        
        public QuestState GetQuest(string questId)
        {
            return _quests.TryGetValue(questId, out var quest) ? quest : null;
        }
        
        public IEnumerable<QuestState> GetAllQuests()
        {
            return _quests.Values;
        }
        
        public IEnumerable<QuestState> GetQuestsByCategory(QuestCategory category)
        {
            foreach (var quest in _quests.Values)
            {
                if (quest.Definition.Category == category)
                    yield return quest;
            }
        }
        
        public IEnumerable<QuestState> GetActiveQuests()
        {
            foreach (var quest in _quests.Values)
            {
                if (quest.Status == QuestStatus.Active)
                    yield return quest;
            }
        }
        
        public IEnumerable<QuestState> GetCompletedQuests()
        {
            foreach (var quest in _quests.Values)
            {
                if (quest.Status == QuestStatus.Completed)
                    yield return quest;
            }
        }
        
        public int GetActiveCount()
        {
            int count = 0;
            foreach (var quest in _quests.Values)
            {
                if (quest.Status == QuestStatus.Active) count++;
            }
            return count;
        }
        
        public int GetCompletedCount()
        {
            int count = 0;
            foreach (var quest in _quests.Values)
            {
                if (quest.Status == QuestStatus.Completed) count++;
            }
            return count;
        }
        
        public int GetCompletedCount(QuestCategory category)
        {
            int count = 0;
            foreach (var quest in _quests.Values)
            {
                if (quest.Status == QuestStatus.Completed && quest.Definition.Category == category) 
                    count++;
            }
            return count;
        }
        
        #endregion
        
        #region Reset
        
        public void ResetQuests(ResetPolicy policy)
        {
            var toReset = new List<string>();
            
            foreach (var quest in _quests.Values)
            {
                if (quest.Definition.ResetPolicy == policy)
                {
                    toReset.Add(quest.Id);
                }
            }
            
            foreach (var questId in toReset)
            {
                _quests.Remove(questId);
            }
            
            if (toReset.Count > 0)
            {
                _signalBus?.Publish(new QuestsResetSignal
                {
                    ResetType = policy,
                    QuestsReset = toReset.Count
                });
                
                SimCoreLogger.Log($"[QuestModule] Reset {toReset.Count} quests with policy {policy}");
            }
        }
        
        public void ResetAll()
        {
            _quests.Clear();
        }
        
        #endregion
        
        #region Persistence
        
        public List<QuestSnapshot> CreateSnapshot()
        {
            var snapshot = new List<QuestSnapshot>();
            
            foreach (var quest in _quests.Values)
            {
                snapshot.Add(new QuestSnapshot
                {
                    Id = quest.Id,
                    CurrentProgress = quest.CurrentProgress,
                    CurrentStage = quest.CurrentStage,
                    Status = (int)quest.Status,
                    StartTime = quest.StartTime,
                    CompletionTime = quest.CompletionTime,
                    RewardsClaimed = quest.RewardsClaimed
                });
            }
            
            return snapshot;
        }
        
        public void RestoreFromSnapshot(List<QuestSnapshot> snapshot)
        {
            foreach (var data in snapshot)
            {
                if (!_definitions.TryGetValue(data.Id, out var def)) continue;
                
                _quests[data.Id] = new QuestState
                {
                    Id = data.Id,
                    Definition = def,
                    CurrentProgress = data.CurrentProgress,
                    CurrentStage = data.CurrentStage,
                    Status = (QuestStatus)data.Status,
                    StartTime = data.StartTime,
                    CompletionTime = data.CompletionTime,
                    RewardsClaimed = data.RewardsClaimed
                };
            }
        }
        
        #endregion
    }
}

