// SimCore - Bark Module
// Quick, contextual NPC dialogue reactions (one-liners, not full conversations)
// 
// USE THIS FOR:
// - NPC reactions to player actions
// - Ambient dialogue (NPCs commenting on environment)
// - Quick responses that don't need player choices
//
// USE DialogueModule FOR:
// - Full branching conversations with player choices
// - Interrogations, quests, story dialogue

using System;
using System.Collections.Generic;
using SimCore.Entities;
using SimCore.Signals;

namespace SimCore.Modules.Barks
{
    // Note: BarkMood and BarkLine are defined in SimCore.Modules (ModuleInterfaces.cs)
    // This keeps the interface and implementation separate
    
    /// <summary>
    /// Signal emitted when an NPC barks
    /// </summary>
    public struct BarkSignal : ISignal
    {
        public SimId SpeakerId;
        public string SpeakerName;
        public string Text;
        public BarkMood Mood;
        public string Context;
    }
    
    /// <summary>
    /// Interface for getting personality from an entity.
    /// Implement this in your game to define how entities have personalities.
    /// </summary>
    public interface IPersonalityProvider
    {
        /// <summary>
        /// Get the personality key for an entity (e.g., "hostile", "nervous", "polite")
        /// </summary>
        string GetPersonality(Entity entity);
    }
    
    /// <summary>
    /// Default personality provider that reads from entity tags
    /// </summary>
    public class TagBasedPersonalityProvider : IPersonalityProvider
    {
        private readonly List<string> _personalityTags;
        private readonly string _defaultPersonality;
        
        /// <summary>
        /// Create provider that checks for personality tags in order
        /// </summary>
        public TagBasedPersonalityProvider(
            IEnumerable<string> personalityTags, 
            string defaultPersonality = "default")
        {
            _personalityTags = new List<string>(personalityTags);
            _defaultPersonality = defaultPersonality;
        }
        
        public string GetPersonality(Entity entity)
        {
            foreach (var tag in _personalityTags)
            {
                if (entity.HasTag(tag))
                    return tag;
            }
            return _defaultPersonality;
        }
    }
    
    /// <summary>
    /// Bark Module Implementation
    /// Manages quick NPC dialogue reactions with personality-based selection.
    /// 
    /// USAGE:
    /// 1. Register bark lines: module.RegisterBarks("hostile", "interaction_start", lines);
    /// 2. Get a bark: var bark = module.GetBark(entity, "interaction_start");
    /// 3. Or emit directly: module.EmitBark(entity, "interaction_start");
    /// </summary>
    public class BarkModule : IBarkModule
    {
        // Storage: [personality][context] -> list of possible lines
        private readonly Dictionary<string, Dictionary<string, List<BarkLine>>> _barkDatabase = new();
        
        private SignalBus _signalBus;
        private IPersonalityProvider _personalityProvider;
        private SimWorld _world;
        private readonly Random _random = new();
        
        // Fallback personality when specific one not found
        public string FallbackPersonality { get; set; } = "default";
        
        /// <summary>
        /// Create BarkModule with optional custom personality provider
        /// </summary>
        public BarkModule(IPersonalityProvider personalityProvider = null)
        {
            // Include all common personality types
            _personalityProvider = personalityProvider ?? new TagBasedPersonalityProvider(
                new[] { 
                    "hostile", "angry",      // Hostile variants
                    "nervous", "scared",     // Nervous variants  
                    "arrogant",              // Arrogant
                    "polite", "friendly",    // Polite variants
                    "casual", "normal",      // Casual/normal variants
                    "confused",              // Confused
                    "guilty"                 // Guilty
                },
                "default"
            );
        }
        
        #region ISimModule
        
        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
        }
        
        public void Tick(float deltaTime)
        {
            // Barks are event-driven, no tick needed
        }
        
        public void Shutdown()
        {
            ClearAll();
        }
        
        #endregion
        
        #region IBarkModule - Registration
        
        public void RegisterBarks(string personality, string context, IEnumerable<BarkLine> lines)
        {
            if (!_barkDatabase.TryGetValue(personality, out var contextDict))
            {
                contextDict = new Dictionary<string, List<BarkLine>>();
                _barkDatabase[personality] = contextDict;
            }
            
            if (!contextDict.TryGetValue(context, out var lineList))
            {
                lineList = new List<BarkLine>();
                contextDict[context] = lineList;
            }
            
            lineList.AddRange(lines);
        }
        
        public void RegisterBark(string personality, string context, string text, 
            BarkMood mood = BarkMood.Neutral, float weight = 1f)
        {
            RegisterBarks(personality, context, new[] { new BarkLine(text, mood, weight) });
        }
        
        /// <summary>
        /// Bulk register from a bark set definition
        /// </summary>
        public void RegisterBarkSet(BarkSet barkSet)
        {
            foreach (var entry in barkSet.Entries)
            {
                RegisterBarks(entry.Personality, entry.Context, entry.Lines);
            }
        }
        
        public void ClearAll()
        {
            _barkDatabase.Clear();
        }
        
        #endregion
        
        #region IBarkModule - Retrieval
        
        public BarkLine GetBark(Entity entity, string context)
        {
            string personality = _personalityProvider.GetPersonality(entity);
            return GetBark(personality, context);
        }
        
        public BarkLine GetBark(string personality, string context)
        {
            // Try exact personality match
            var line = TryGetBark(personality, context);
            if (line != null) return line;
            
            // Try fallback personality
            if (personality != FallbackPersonality)
            {
                line = TryGetBark(FallbackPersonality, context);
                if (line != null) return line;
            }
            
            // Ultimate fallback
            return new BarkLine("...", BarkMood.Neutral);
        }
        
        private BarkLine TryGetBark(string personality, string context)
        {
            if (!_barkDatabase.TryGetValue(personality, out var contextDict))
                return null;
            
            if (!contextDict.TryGetValue(context, out var lines) || lines.Count == 0)
                return null;
            
            return SelectWeightedRandom(lines);
        }
        
        private BarkLine SelectWeightedRandom(List<BarkLine> lines)
        {
            if (lines.Count == 1) return lines[0];
            
            float totalWeight = 0;
            foreach (var line in lines) totalWeight += line.Weight;
            
            float roll = (float)_random.NextDouble() * totalWeight;
            float cumulative = 0;
            
            foreach (var line in lines)
            {
                cumulative += line.Weight;
                if (roll <= cumulative) return line;
            }
            
            return lines[lines.Count - 1];
        }
        
        #endregion
        
        #region IBarkModule - Emission
        
        public void EmitBark(Entity entity, string context)
        {
            if (_signalBus == null)
            {
                SimCoreLogger.LogWarning("[BarkModule] Not initialized - call Initialize(world) first");
                return;
            }
            
            string personality = _personalityProvider.GetPersonality(entity);
            var bark = GetBark(personality, context);
            
            SimCoreLogger.Log($"[BarkModule] Entity '{entity.DisplayName}' (personality: {personality}) context: {context} -> \"{bark.Text}\"");
            
            _signalBus.Publish(new BarkSignal
            {
                SpeakerId = entity.Id,
                SpeakerName = entity.DisplayName,
                Text = bark.Text,
                Mood = bark.Mood,
                Context = context
            });
        }
        
        /// <summary>
        /// Emit a specific bark line
        /// </summary>
        public void EmitBark(Entity entity, BarkLine bark, string context = null)
        {
            if (_signalBus == null) return;
            
            _signalBus.Publish(new BarkSignal
            {
                SpeakerId = entity.Id,
                SpeakerName = entity.DisplayName,
                Text = bark.Text,
                Mood = bark.Mood,
                Context = context ?? ""
            });
        }
        
        #endregion
    }
    
    #region Bark Set Definition (for bulk registration)
    
    /// <summary>
    /// A collection of bark entries for bulk registration
    /// </summary>
    [Serializable]
    public class BarkSet
    {
        public string Name;
        public List<BarkEntry> Entries = new();
    }
    
    /// <summary>
    /// A single entry in a bark set
    /// </summary>
    [Serializable]
    public class BarkEntry
    {
        public string Personality;
        public string Context;
        public List<BarkLine> Lines = new();
        
        public BarkEntry() { }
        
        public BarkEntry(string personality, string context, params BarkLine[] lines)
        {
            Personality = personality;
            Context = context;
            Lines = new List<BarkLine>(lines);
        }
    }
    
    #endregion
    
    #region Generic Bark Contexts
    
    /// <summary>
    /// GENERIC bark context names that apply to any game.
    /// Games should define their own game-specific contexts as string constants.
    /// </summary>
    public static class BarkContext
    {
        // === GENERIC GREETINGS ===
        public const string Greet = "greet";
        public const string GreetFriendly = "greet_friendly";
        public const string GreetHostile = "greet_hostile";
        public const string Farewell = "farewell";
        
        // === GENERIC REACTIONS ===
        public const string ReactHappy = "react_happy";
        public const string ReactAngry = "react_angry";
        public const string ReactScared = "react_scared";
        public const string ReactSurprised = "react_surprised";
        public const string ReactGrateful = "react_grateful";
        public const string ReactAnnoyed = "react_annoyed";
        public const string ReactConfused = "react_confused";
        public const string ReactSad = "react_sad";
        
        // === COMMERCE ===
        public const string PurchaseComplete = "purchase_complete";
        public const string PurchaseCantAfford = "purchase_cant_afford";
        public const string ItemOutOfStock = "item_out_of_stock";
        public const string BrowsingShop = "browsing_shop";
        public const string SellingItem = "selling_item";
        
        // === AMBIENT ===
        public const string Idle = "idle";
        public const string Walking = "walking";
        public const string Working = "working";
        public const string Waiting = "waiting";
        public const string Resting = "resting";
        
        // === INTERACTION ===
        public const string BeingApproached = "being_approached";
        public const string ConversationStart = "conversation_start";
        public const string ConversationEnd = "conversation_end";
        public const string ItemReceived = "item_received";
        public const string ItemGiven = "item_given";
        
        // === COMBAT/DANGER ===
        public const string TakingDamage = "taking_damage";
        public const string NearDeath = "near_death";
        public const string EnemySpotted = "enemy_spotted";
        public const string AllClear = "all_clear";
    }
    
    #endregion
}
