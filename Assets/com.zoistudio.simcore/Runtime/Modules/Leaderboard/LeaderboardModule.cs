// SimCore - Leaderboard Module
// ═══════════════════════════════════════════════════════════════════════════════
// Local implementation of leaderboard system with server-ready hooks.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using SimCore;
using SimCore.Signals;

namespace SimCore.Modules.Leaderboard
{
    /// <summary>
    /// Local leaderboard module implementation.
    /// </summary>
    public class LeaderboardModule : ILeaderboardModule
    {
        #region Private Fields

        private SimWorld _world;
        private SignalBus _signalBus;

        // Definitions
        private Dictionary<string, LeaderboardDef> _definitions = new Dictionary<string, LeaderboardDef>();

        // Local entries (for offline/local play)
        private Dictionary<string, List<LeaderboardEntry>> _localEntries = new Dictionary<string, List<LeaderboardEntry>>();

        // Personal bests
        private Dictionary<string, PersonalBest> _personalBests = new Dictionary<string, PersonalBest>();

        // Friends
        private Dictionary<string, string> _friends = new Dictionary<string, string>(); // playerId -> name

        // Player info
        private string _playerId = "local_player";
        private string _playerName = "Player";

        // Provider
        private ILeaderboardProvider _provider;

        private const string SaveKey = "Leaderboards_";

        #endregion

        #region ISimModule

        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
            LoadLocal();
        }

        public void Tick(float deltaTime)
        {
            // Could check for leaderboard resets here
        }

        public void Shutdown()
        {
            SaveLocal();
        }

        #endregion

        #region Definitions

        public void RegisterLeaderboard(LeaderboardDef definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id))
                return;

            _definitions[definition.Id] = definition;

            // Initialize local entries if needed
            if (!_localEntries.ContainsKey(definition.Id))
            {
                _localEntries[definition.Id] = GenerateFakeEntries(definition);
            }
        }

        public LeaderboardDef GetDefinition(string leaderboardId)
        {
            return _definitions.TryGetValue(leaderboardId, out var def) ? def : null;
        }

        public IEnumerable<LeaderboardDef> GetAllDefinitions()
        {
            return _definitions.Values;
        }

        #endregion

        #region Score Submission

        public ScoreSubmitResult SubmitScore(string leaderboardId, long score)
        {
            return SubmitScore(leaderboardId, score, null);
        }

        public ScoreSubmitResult SubmitScore(string leaderboardId, long score, Dictionary<string, string> metadata)
        {
            var def = GetDefinition(leaderboardId);
            if (def == null)
            {
                return new ScoreSubmitResult
                {
                    Success = false,
                    LeaderboardId = leaderboardId,
                    ErrorMessage = "Leaderboard not found"
                };
            }

            // Get or create entries list
            if (!_localEntries.TryGetValue(leaderboardId, out var entries))
            {
                entries = new List<LeaderboardEntry>();
                _localEntries[leaderboardId] = entries;
            }

            // Check for existing player entry
            var existingEntry = entries.FirstOrDefault(e => e.PlayerId == _playerId);
            int previousRank = existingEntry?.Rank ?? -1;
            long previousScore = existingEntry?.Score ?? 0;

            bool isNewHighScore = false;

            // Check if this is a new high score
            if (def.SortOrder == LeaderboardSortOrder.HighestFirst)
            {
                isNewHighScore = existingEntry == null || score > existingEntry.Score;
            }
            else
            {
                isNewHighScore = existingEntry == null || score < existingEntry.Score;
            }

            if (isNewHighScore || existingEntry == null)
            {
                // Update or create entry
                if (existingEntry != null)
                {
                    existingEntry.Score = score;
                    existingEntry.SubmittedAt = DateTime.UtcNow;
                    if (metadata != null)
                    {
                        foreach (var kvp in metadata)
                        {
                            existingEntry.Metadata[kvp.Key] = kvp.Value;
                        }
                    }
                }
                else
                {
                    var newEntry = new LeaderboardEntry
                    {
                        PlayerId = _playerId,
                        PlayerName = _playerName,
                        Score = score,
                        SubmittedAt = DateTime.UtcNow,
                        Metadata = metadata ?? new Dictionary<string, string>()
                    };
                    entries.Add(newEntry);
                }

                // Re-sort and recalculate ranks
                SortAndRankEntries(leaderboardId, def);

                // Update personal best
                UpdatePersonalBest(leaderboardId, score);

                SaveLocal();
            }

            // Get new rank
            var playerEntry = entries.FirstOrDefault(e => e.PlayerId == _playerId);
            int newRank = playerEntry?.Rank ?? entries.Count;
            var division = playerEntry?.Division ?? LeaderboardDivision.Bronze;

            // Publish signals
            _signalBus?.Publish(new ScoreSubmittedSignal
            {
                LeaderboardId = leaderboardId,
                Score = score,
                NewRank = newRank,
                IsNewHighScore = isNewHighScore,
                Division = division
            });

            if (previousRank > 0 && newRank != previousRank)
            {
                var previousDivision = existingEntry?.Division ?? LeaderboardDivision.Bronze;
                if (division != previousDivision)
                {
                    _signalBus?.Publish(new DivisionChangedSignal
                    {
                        LeaderboardId = leaderboardId,
                        OldDivision = previousDivision,
                        NewDivision = division,
                        IsPromotion = division > previousDivision
                    });
                }

                _signalBus?.Publish(new RankChangedSignal
                {
                    LeaderboardId = leaderboardId,
                    OldRank = previousRank,
                    NewRank = newRank,
                    OldDivision = previousDivision,
                    NewDivision = division
                });
            }

            return new ScoreSubmitResult
            {
                Success = true,
                LeaderboardId = leaderboardId,
                Score = score,
                NewRank = newRank,
                PreviousRank = previousRank,
                IsNewHighScore = isNewHighScore,
                Division = division
            };
        }

        public async Task<ScoreSubmitResult> SubmitScoreAsync(string leaderboardId, long score)
        {
            if (_provider != null)
            {
                return await _provider.SubmitScoreAsync(leaderboardId, _playerId, _playerName, score, null);
            }

            // Fall back to local
            return SubmitScore(leaderboardId, score);
        }

        #endregion

        #region Queries

        public LeaderboardResult GetTopEntries(string leaderboardId, int count = 10)
        {
            var result = new LeaderboardResult
            {
                LeaderboardId = leaderboardId,
                QueryTime = DateTime.UtcNow,
                FromCache = true
            };

            if (!_localEntries.TryGetValue(leaderboardId, out var entries))
            {
                return result;
            }

            result.Entries = entries.Take(count).Select(e => e.Clone()).ToList();
            result.TotalPlayers = entries.Count;
            result.PlayerEntry = entries.FirstOrDefault(e => e.PlayerId == _playerId)?.Clone();

            return result;
        }

        public LeaderboardResult GetEntriesAroundPlayer(string leaderboardId, int count = 5)
        {
            var result = new LeaderboardResult
            {
                LeaderboardId = leaderboardId,
                QueryTime = DateTime.UtcNow,
                FromCache = true
            };

            if (!_localEntries.TryGetValue(leaderboardId, out var entries))
            {
                return result;
            }

            var playerEntry = entries.FirstOrDefault(e => e.PlayerId == _playerId);
            if (playerEntry == null)
            {
                return GetTopEntries(leaderboardId, count);
            }

            int playerIndex = entries.IndexOf(playerEntry);
            int startIndex = Math.Max(0, playerIndex - count / 2);
            int endIndex = Math.Min(entries.Count, startIndex + count);

            result.Entries = entries.Skip(startIndex).Take(endIndex - startIndex).Select(e => e.Clone()).ToList();
            result.TotalPlayers = entries.Count;
            result.PlayerEntry = playerEntry.Clone();

            return result;
        }

        public LeaderboardEntry GetPlayerEntry(string leaderboardId)
        {
            if (!_localEntries.TryGetValue(leaderboardId, out var entries))
                return null;

            return entries.FirstOrDefault(e => e.PlayerId == _playerId)?.Clone();
        }

        public LeaderboardResult GetFriendsEntries(string leaderboardId)
        {
            var result = new LeaderboardResult
            {
                LeaderboardId = leaderboardId,
                QueryTime = DateTime.UtcNow,
                FromCache = true
            };

            if (!_localEntries.TryGetValue(leaderboardId, out var entries))
            {
                return result;
            }

            var friendIds = new HashSet<string>(_friends.Keys) { _playerId };
            result.Entries = entries.Where(e => friendIds.Contains(e.PlayerId)).Select(e => e.Clone()).ToList();
            result.TotalPlayers = result.Entries.Count;
            result.PlayerEntry = entries.FirstOrDefault(e => e.PlayerId == _playerId)?.Clone();

            return result;
        }

        public async Task<LeaderboardResult> GetTopEntriesAsync(string leaderboardId, int count = 10)
        {
            if (_provider != null)
            {
                return await _provider.FetchTopEntriesAsync(leaderboardId, count);
            }

            return GetTopEntries(leaderboardId, count);
        }

        #endregion

        #region Personal Bests

        public PersonalBest GetPersonalBest(string leaderboardId)
        {
            return _personalBests.TryGetValue(leaderboardId, out var pb) ? pb : null;
        }

        public IEnumerable<PersonalBest> GetAllPersonalBests()
        {
            return _personalBests.Values;
        }

        private void UpdatePersonalBest(string leaderboardId, long score)
        {
            var def = GetDefinition(leaderboardId);
            if (def == null) return;

            bool isNewBest = false;

            if (_personalBests.TryGetValue(leaderboardId, out var existing))
            {
                if (def.SortOrder == LeaderboardSortOrder.HighestFirst)
                {
                    isNewBest = score > existing.Score;
                }
                else
                {
                    isNewBest = score < existing.Score;
                }

                if (isNewBest)
                {
                    long oldBest = existing.Score;
                    existing.Score = score;
                    existing.AchievedAt = DateTime.UtcNow;
                    existing.BestRank = GetPlayerRank(leaderboardId);

                    _signalBus?.Publish(new PersonalBestSignal
                    {
                        LeaderboardId = leaderboardId,
                        OldBest = oldBest,
                        NewBest = score,
                        BestRank = existing.BestRank
                    });
                }
            }
            else
            {
                _personalBests[leaderboardId] = new PersonalBest
                {
                    LeaderboardId = leaderboardId,
                    Score = score,
                    AchievedAt = DateTime.UtcNow,
                    BestRank = GetPlayerRank(leaderboardId)
                };
            }
        }

        #endregion

        #region Player Info

        public void SetPlayerId(string playerId)
        {
            _playerId = playerId;
        }

        public void SetPlayerName(string playerName)
        {
            _playerName = playerName;
        }

        public LeaderboardDivision GetPlayerDivision(string leaderboardId)
        {
            var entry = GetPlayerEntry(leaderboardId);
            return entry?.Division ?? LeaderboardDivision.Bronze;
        }

        public int GetPlayerRank(string leaderboardId)
        {
            var entry = GetPlayerEntry(leaderboardId);
            return entry?.Rank ?? -1;
        }

        #endregion

        #region Friends

        public void AddFriend(string friendPlayerId, string friendName)
        {
            _friends[friendPlayerId] = friendName;
        }

        public void RemoveFriend(string friendPlayerId)
        {
            _friends.Remove(friendPlayerId);
        }

        public void SetFriends(IEnumerable<(string playerId, string name)> friends)
        {
            _friends.Clear();
            foreach (var (playerId, name) in friends)
            {
                _friends[playerId] = name;
            }
        }

        #endregion

        #region Provider

        public void SetProvider(ILeaderboardProvider provider)
        {
            _provider = provider;
        }

        public bool HasProvider => _provider != null;

        #endregion

        #region Persistence

        public void SaveLocal()
        {
            // Save personal bests
            foreach (var pb in _personalBests)
            {
                PlayerPrefs.SetString($"{SaveKey}{pb.Key}_score", pb.Value.Score.ToString());
                PlayerPrefs.SetInt($"{SaveKey}{pb.Key}_rank", pb.Value.BestRank);
            }

            PlayerPrefs.SetString($"{SaveKey}PlayerId", _playerId);
            PlayerPrefs.SetString($"{SaveKey}PlayerName", _playerName);
            PlayerPrefs.Save();
        }

        public void LoadLocal()
        {
            _playerId = PlayerPrefs.GetString($"{SaveKey}PlayerId", "local_player");
            _playerName = PlayerPrefs.GetString($"{SaveKey}PlayerName", "Player");
        }

        #endregion

        #region Helpers

        private void SortAndRankEntries(string leaderboardId, LeaderboardDef def)
        {
            if (!_localEntries.TryGetValue(leaderboardId, out var entries))
                return;

            // Sort
            if (def.SortOrder == LeaderboardSortOrder.HighestFirst)
            {
                entries.Sort((a, b) => b.Score.CompareTo(a.Score));
            }
            else
            {
                entries.Sort((a, b) => a.Score.CompareTo(b.Score));
            }

            // Assign ranks and divisions
            int totalPlayers = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].Rank = i + 1;

                if (def.EnableDivisions && totalPlayers > 0)
                {
                    float percentile = ((float)i / totalPlayers) * 100f;
                    entries[i].Division = LeaderboardDivisionExtensions.GetDivision(percentile);
                }
            }

            // Trim if needed
            if (def.MaxEntries > 0 && entries.Count > def.MaxEntries)
            {
                entries.RemoveRange(def.MaxEntries, entries.Count - def.MaxEntries);
            }
        }

        private List<LeaderboardEntry> GenerateFakeEntries(LeaderboardDef def)
        {
            var entries = new List<LeaderboardEntry>();
            var names = new[] { "Player_Alpha", "Player_Beta", "Player_Gamma", "Player_Delta",
                "Player_Echo", "Player_Foxtrot", "Player_Golf", "Player_Hotel", "Player_India", "Player_Juliet" };

            for (int i = 0; i < Mathf.Min(def.MaxEntries, 50); i++)
            {
                long score = def.SortOrder == LeaderboardSortOrder.HighestFirst
                    ? UnityEngine.Random.Range(1000, 50000)
                    : UnityEngine.Random.Range(60, 600); // For time-based

                entries.Add(new LeaderboardEntry
                {
                    PlayerId = $"npc_{i}",
                    PlayerName = names[i % names.Length] + (i >= names.Length ? $" {i / names.Length + 1}" : ""),
                    Score = score,
                    SubmittedAt = DateTime.UtcNow.AddDays(-UnityEngine.Random.Range(0, 30))
                });
            }

            SortAndRankEntries(def.Id, def);
            return entries;
        }

        #endregion
    }
}
