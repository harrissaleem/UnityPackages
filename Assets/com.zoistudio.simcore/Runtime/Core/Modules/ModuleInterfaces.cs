// SimCore - Module Interfaces
// Interfaces for optional pluggable modules

using System.Collections.Generic;
using SimCore.Entities;

namespace SimCore.Modules
{
    /// <summary>
    /// Base interface for all SimCore modules.
    /// All optional modules should implement this for consistent lifecycle management.
    /// </summary>
    public interface ISimModule
    {
        /// <summary>
        /// Initialize the module with the world context
        /// </summary>
        void Initialize(SimWorld world);
        
        /// <summary>
        /// Tick/update the module each frame
        /// </summary>
        void Tick(float deltaTime);
        
        /// <summary>
        /// Shutdown and cleanup
        /// </summary>
        void Shutdown();
    }
    
    /// <summary>
    /// Dialogue module interface
    /// Supports both node-based and intent-based dialogue
    /// </summary>
    public interface IDialogueModule : ISimModule
    {
        /// <summary>
        /// Start a dialogue with an entity
        /// </summary>
        void StartDialogue(SimId speakerId, ContentId dialogueId);
        
        /// <summary>
        /// Select a choice in current dialogue
        /// </summary>
        void SelectChoice(int choiceIndex);
        
        /// <summary>
        /// Advance to next line
        /// </summary>
        void Advance();
        
        /// <summary>
        /// End current dialogue
        /// </summary>
        void EndDialogue();
        
        /// <summary>
        /// Check if dialogue is active
        /// </summary>
        bool IsDialogueActive { get; }
        
        /// <summary>
        /// Get current speaker
        /// </summary>
        SimId CurrentSpeaker { get; }
        
        /// <summary>
        /// Get current dialogue ID
        /// </summary>
        ContentId CurrentDialogueId { get; }
    }
    
    // Note: IQuestModule is now defined in SimCore.Modules.Quest.QuestModule.cs
    // It's a unified system for quests, objectives, and achievements with reset policies
    
    /// <summary>
    /// Stealth/detection module interface
    /// </summary>
    public interface IStealthModule : ISimModule
    {
        /// <summary>
        /// Update detection for an entity
        /// </summary>
        void UpdateDetection(SimId detectorId, SimId targetId);
        
        /// <summary>
        /// Get alert level for a detector
        /// </summary>
        float GetAlertLevel(SimId detectorId);
        
        /// <summary>
        /// Set alert level directly
        /// </summary>
        void SetAlertLevel(SimId detectorId, float level);
        
        /// <summary>
        /// Check if target is detected by detector
        /// </summary>
        bool IsDetected(SimId detectorId, SimId targetId);
        
        /// <summary>
        /// Register a detector
        /// </summary>
        void RegisterDetector(SimId entityId, DetectorConfig config);
    }
    
    /// <summary>
    /// Detector configuration
    /// </summary>
    public class DetectorConfig
    {
        public float ViewDistance = 10f;
        public float ViewAngle = 90f;
        public float HearingDistance = 5f;
        public float DetectionSpeed = 1f;    // How fast alert builds
        public float AlertDecaySpeed = 0.5f; // How fast alert decays
        public float AlertThreshold = 1f;    // Alert level to trigger detection
    }
    
    /// <summary>
    /// Economy module interface
    /// </summary>
    public interface IEconomyModule : ISimModule
    {
        /// <summary>
        /// Get money for an entity
        /// </summary>
        float GetMoney(SimId entityId);
        
        /// <summary>
        /// Add money to an entity
        /// </summary>
        void AddMoney(SimId entityId, float amount);
        
        /// <summary>
        /// Remove money from an entity
        /// </summary>
        bool RemoveMoney(SimId entityId, float amount);
        
        /// <summary>
        /// Transfer money between entities
        /// </summary>
        bool Transfer(SimId from, SimId to, float amount);
        
        /// <summary>
        /// Get price of an item
        /// </summary>
        float GetPrice(ContentId itemId);
    }
    
    /// <summary>
    /// Crafting/production module interface
    /// </summary>
    public interface ICraftingModule : ISimModule
    {
        /// <summary>
        /// Start crafting a recipe
        /// </summary>
        bool StartCrafting(SimId crafterId, ContentId recipeId);
        
        /// <summary>
        /// Check if can craft a recipe
        /// </summary>
        bool CanCraft(SimId crafterId, ContentId recipeId);
        
        /// <summary>
        /// Get crafting progress
        /// </summary>
        float GetProgress(SimId crafterId, ContentId recipeId);
        
        /// <summary>
        /// Cancel crafting
        /// </summary>
        void CancelCrafting(SimId crafterId, ContentId recipeId);
    }
    
    /// <summary>
    /// Bark module interface - quick NPC dialogue reactions
    /// For one-liners and reactions, not full conversations (use IDialogueModule for those)
    /// </summary>
    public interface IBarkModule : ISimModule
    {
        /// <summary>
        /// Register bark lines for a personality + context combination
        /// </summary>
        void RegisterBarks(string personality, string context, IEnumerable<BarkLine> lines);
        
        /// <summary>
        /// Register a single bark line
        /// </summary>
        void RegisterBark(string personality, string context, string text, BarkMood mood = BarkMood.Neutral, float weight = 1f);
        
        /// <summary>
        /// Get a random bark line for an entity in a given context
        /// </summary>
        BarkLine GetBark(Entity entity, string context);
        
        /// <summary>
        /// Get a random bark line for a personality + context
        /// </summary>
        BarkLine GetBark(string personality, string context);
        
        /// <summary>
        /// Get a bark and emit it as a signal
        /// </summary>
        void EmitBark(Entity entity, string context);
        
        /// <summary>
        /// Clear all registered barks
        /// </summary>
        void ClearAll();
    }
    
    /// <summary>
    /// Bark mood affects how the UI might display the line
    /// </summary>
    public enum BarkMood
    {
        Neutral,
        Cooperative,
        Nervous,
        Hostile,
        Confused,
        Happy,
        Sad,
        Scared
    }
    
    /// <summary>
    /// A single bark line with metadata
    /// </summary>
    [System.Serializable]
    public class BarkLine
    {
        public string Text;
        public BarkMood Mood;
        public float Weight;
        
        public BarkLine() : this("...", BarkMood.Neutral, 1f) { }
        
        public BarkLine(string text, BarkMood mood = BarkMood.Neutral, float weight = 1f)
        {
            Text = text;
            Mood = mood;
            Weight = weight;
        }
    }
}

