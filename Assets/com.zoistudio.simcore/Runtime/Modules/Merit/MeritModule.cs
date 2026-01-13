// SimCore - Merit Module
// ═══════════════════════════════════════════════════════════════════════════════
// Implementation of multi-dimensional performance tracking.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using SimCore;
using SimCore.Signals;
using UnityEngine;

namespace SimCore.Modules.Merit
{
    /// <summary>
    /// Merit module implementation.
    /// </summary>
    public class MeritModule : IMeritModule
    {
        #region Private Fields

        private SimWorld _world;
        private SignalBus _signalBus;

        // Category definitions
        private Dictionary<string, MeritCategoryDef> _categories = new Dictionary<string, MeritCategoryDef>();
        private float _totalWeight;

        // Per-entity data
        private Dictionary<SimId, EntityMeritData> _entityData = new Dictionary<SimId, EntityMeritData>();

        // Custom evaluator
        private Func<SimId, string, string, MeritEvaluation> _evaluator;

        // Config
        private int _maxHistoryEvents = 100;
        private int _maxSnapshots = 20;

        #endregion

        #region ISimModule

        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
        }

        public void Tick(float deltaTime)
        {
            // Apply decay to all entities
            foreach (var kvp in _entityData)
            {
                ApplyDecay(kvp.Key, kvp.Value, deltaTime);
            }
        }

        public void Shutdown()
        {
            _entityData.Clear();
            _categories.Clear();
        }

        #endregion

        #region Categories

        public void RegisterCategory(MeritCategoryDef category)
        {
            if (category == null || string.IsNullOrEmpty(category.Id))
                return;

            _categories[category.Id] = category;
            RecalculateTotalWeight();
        }

        public MeritCategoryDef GetCategory(string categoryId)
        {
            return _categories.TryGetValue(categoryId, out var cat) ? cat : null;
        }

        public IEnumerable<MeritCategoryDef> GetAllCategories()
        {
            return _categories.Values;
        }

        private void RecalculateTotalWeight()
        {
            _totalWeight = 0f;
            foreach (var cat in _categories.Values)
            {
                _totalWeight += cat.Weight;
            }
        }

        #endregion

        #region Scores

        public float GetCategoryScore(SimId entityId, string categoryId)
        {
            var data = GetOrCreateEntityData(entityId);
            if (data.Scores.TryGetValue(categoryId, out var score))
                return score;

            var category = GetCategory(categoryId);
            return category?.DefaultValue ?? 50f;
        }

        public void SetCategoryScore(SimId entityId, string categoryId, float value, string reason = "")
        {
            var data = GetOrCreateEntityData(entityId);
            var category = GetCategory(categoryId);

            if (category == null)
            {
                Debug.LogWarning($"[MeritModule] Unknown category: {categoryId}");
                return;
            }

            float oldValue = GetCategoryScore(entityId, categoryId);
            float newValue = Mathf.Clamp(value, category.MinValue, category.MaxValue);

            if (Mathf.Approximately(oldValue, newValue))
                return;

            data.Scores[categoryId] = newValue;

            // Record event
            RecordEvent(data, new MeritEvent
            {
                Timestamp = DateTime.Now,
                CategoryId = categoryId,
                OldValue = oldValue,
                NewValue = newValue,
                Delta = newValue - oldValue,
                Reason = reason
            });

            // Publish signals
            _signalBus?.Publish(new MeritCategoryChangedSignal
            {
                EntityId = entityId,
                CategoryId = categoryId,
                OldValue = oldValue,
                NewValue = newValue,
                Delta = newValue - oldValue,
                Reason = reason
            });

            // Check for tier change
            CheckTierChange(entityId, data);
        }

        public void AddCategoryScore(SimId entityId, string categoryId, float delta, string reason = "")
        {
            float current = GetCategoryScore(entityId, categoryId);
            SetCategoryScore(entityId, categoryId, current + delta, reason);
        }

        public float GetOverallScore(SimId entityId)
        {
            if (_totalWeight <= 0f)
                return 50f;

            float weightedSum = 0f;

            foreach (var category in _categories.Values)
            {
                float score = GetCategoryScore(entityId, category.Id);
                weightedSum += score * category.Weight;
            }

            return weightedSum / _totalWeight;
        }

        public MeritTier GetTier(SimId entityId)
        {
            return MeritTierExtensions.GetTier(GetOverallScore(entityId));
        }

        public Dictionary<string, float> GetAllScores(SimId entityId)
        {
            var result = new Dictionary<string, float>();
            foreach (var category in _categories.Values)
            {
                result[category.Id] = GetCategoryScore(entityId, category.Id);
            }
            return result;
        }

        private void CheckTierChange(SimId entityId, EntityMeritData data)
        {
            float newScore = GetOverallScore(entityId);
            MeritTier newTier = MeritTierExtensions.GetTier(newScore);

            if (newTier != data.LastTier)
            {
                var oldTier = data.LastTier;
                data.LastTier = newTier;

                _signalBus?.Publish(new MeritTierChangedSignal
                {
                    EntityId = entityId,
                    OldTier = oldTier,
                    NewTier = newTier,
                    CurrentScore = newScore
                });
            }

            if (!Mathf.Approximately(newScore, data.LastOverallScore))
            {
                var oldScore = data.LastOverallScore;
                data.LastOverallScore = newScore;

                _signalBus?.Publish(new MeritScoreChangedSignal
                {
                    EntityId = entityId,
                    OldScore = oldScore,
                    NewScore = newScore,
                    OldTier = MeritTierExtensions.GetTier(oldScore),
                    NewTier = newTier
                });
            }
        }

        #endregion

        #region Decay

        private void ApplyDecay(SimId entityId, EntityMeritData data, float deltaTime)
        {
            foreach (var category in _categories.Values)
            {
                if (category.DecayRate <= 0f)
                    continue;

                float currentScore = GetCategoryScore(entityId, category.Id);
                float decayAmount = category.DecayRate * deltaTime;

                // Decay towards default value
                if (currentScore > category.DefaultValue)
                {
                    float newScore = Mathf.Max(category.DefaultValue, currentScore - decayAmount);
                    if (!Mathf.Approximately(currentScore, newScore))
                    {
                        data.Scores[category.Id] = newScore;

                        _signalBus?.Publish(new MeritDecaySignal
                        {
                            EntityId = entityId,
                            CategoryId = category.Id,
                            DecayAmount = currentScore - newScore,
                            NewValue = newScore
                        });
                    }
                }
                else if (currentScore < category.DefaultValue)
                {
                    float newScore = Mathf.Min(category.DefaultValue, currentScore + decayAmount);
                    if (!Mathf.Approximately(currentScore, newScore))
                    {
                        data.Scores[category.Id] = newScore;

                        _signalBus?.Publish(new MeritDecaySignal
                        {
                            EntityId = entityId,
                            CategoryId = category.Id,
                            DecayAmount = newScore - currentScore,
                            NewValue = newScore
                        });
                    }
                }
            }
        }

        #endregion

        #region Evaluation

        public MeritEvaluation Evaluate(SimId entityId, MeritEvaluation evaluation)
        {
            if (evaluation == null)
                return null;

            // Apply all impacts
            foreach (var impact in evaluation.Impacts)
            {
                AddCategoryScore(entityId, impact.CategoryId, impact.Delta, impact.Reason);
            }

            _signalBus?.Publish(new MeritEvaluatedSignal
            {
                EntityId = entityId,
                ActionId = evaluation.ActionId,
                WasCorrect = evaluation.WasCorrect,
                Feedback = evaluation.Feedback,
                ImpactCount = evaluation.Impacts.Count
            });

            return evaluation;
        }

        public Func<SimId, string, string, MeritEvaluation> Evaluator
        {
            get => _evaluator;
            set => _evaluator = value;
        }

        #endregion

        #region History

        public MeritSnapshot CreateSnapshot(SimId entityId, string context = "")
        {
            var data = GetOrCreateEntityData(entityId);

            var snapshot = new MeritSnapshot
            {
                Timestamp = DateTime.Now,
                OverallScore = GetOverallScore(entityId),
                CategoryScores = GetAllScores(entityId),
                Context = context
            };

            data.Snapshots.Add(snapshot);

            // Trim old snapshots
            while (data.Snapshots.Count > _maxSnapshots)
            {
                data.Snapshots.RemoveAt(0);
            }

            _signalBus?.Publish(new MeritSnapshotCreatedSignal
            {
                EntityId = entityId,
                OverallScore = snapshot.OverallScore,
                Tier = GetTier(entityId),
                Context = context
            });

            return snapshot;
        }

        public IEnumerable<MeritEvent> GetRecentEvents(SimId entityId, int count = 10)
        {
            if (!_entityData.TryGetValue(entityId, out var data))
                yield break;

            int startIndex = Mathf.Max(0, data.Events.Count - count);
            for (int i = data.Events.Count - 1; i >= startIndex; i--)
            {
                yield return data.Events[i];
            }
        }

        public IEnumerable<MeritSnapshot> GetSnapshotHistory(SimId entityId, int count = 10)
        {
            if (!_entityData.TryGetValue(entityId, out var data))
                yield break;

            int startIndex = Mathf.Max(0, data.Snapshots.Count - count);
            for (int i = data.Snapshots.Count - 1; i >= startIndex; i--)
            {
                yield return data.Snapshots[i];
            }
        }

        public void ClearHistory(SimId entityId)
        {
            if (_entityData.TryGetValue(entityId, out var data))
            {
                data.Events.Clear();
                data.Snapshots.Clear();
            }
        }

        private void RecordEvent(EntityMeritData data, MeritEvent evt)
        {
            data.Events.Add(evt);

            // Trim old events
            while (data.Events.Count > _maxHistoryEvents)
            {
                data.Events.RemoveAt(0);
            }
        }

        #endregion

        #region Persistence

        public MeritSaveData GetSaveData(SimId entityId)
        {
            var data = GetOrCreateEntityData(entityId);
            return new MeritSaveData
            {
                CategoryScores = new Dictionary<string, float>(data.Scores),
                SnapshotHistory = new List<MeritSnapshot>(data.Snapshots)
            };
        }

        public void RestoreFromSave(SimId entityId, MeritSaveData saveData)
        {
            if (saveData == null)
                return;

            var data = GetOrCreateEntityData(entityId);
            data.Scores.Clear();
            data.Snapshots.Clear();

            foreach (var kvp in saveData.CategoryScores)
            {
                data.Scores[kvp.Key] = kvp.Value;
            }

            data.Snapshots.AddRange(saveData.SnapshotHistory);

            // Update cached values
            data.LastOverallScore = GetOverallScore(entityId);
            data.LastTier = GetTier(entityId);
        }

        #endregion

        #region Utilities

        public void ResetToDefaults(SimId entityId)
        {
            var data = GetOrCreateEntityData(entityId);
            data.Scores.Clear();

            foreach (var category in _categories.Values)
            {
                data.Scores[category.Id] = category.DefaultValue;
            }

            data.LastOverallScore = GetOverallScore(entityId);
            data.LastTier = GetTier(entityId);
        }

        public bool MeetsRequirement(SimId entityId, MeritTier minimumTier)
        {
            return GetTier(entityId) >= minimumTier;
        }

        public bool MeetsCategoryRequirement(SimId entityId, string categoryId, float minimumScore)
        {
            return GetCategoryScore(entityId, categoryId) >= minimumScore;
        }

        #endregion

        #region Helpers

        private EntityMeritData GetOrCreateEntityData(SimId entityId)
        {
            if (!_entityData.TryGetValue(entityId, out var data))
            {
                data = new EntityMeritData();

                // Initialize with defaults
                foreach (var category in _categories.Values)
                {
                    data.Scores[category.Id] = category.DefaultValue;
                }

                data.LastOverallScore = GetOverallScore(entityId);
                data.LastTier = MeritTierExtensions.GetTier(data.LastOverallScore);

                _entityData[entityId] = data;
            }

            return data;
        }

        private class EntityMeritData
        {
            public Dictionary<string, float> Scores = new Dictionary<string, float>();
            public List<MeritEvent> Events = new List<MeritEvent>();
            public List<MeritSnapshot> Snapshots = new List<MeritSnapshot>();
            public float LastOverallScore;
            public MeritTier LastTier;
        }

        #endregion
    }
}
