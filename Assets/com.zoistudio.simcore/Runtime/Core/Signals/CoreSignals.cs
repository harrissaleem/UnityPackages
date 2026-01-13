namespace SimCore.Signals
{
    // ========== Common Signals ==========
    
    /// <summary>
    /// Fired when an action completes
    /// </summary>
    public struct ActionCompletedSignal : ISignal
    {
        public SimId ActorId;
        public ContentId ActionId;
        public SimId TargetId;
        public ActionResult Result;
        public ActionContext Context;
    }
    
    /// <summary>
    /// Fired when an action is blocked by validation
    /// </summary>
    public struct ActionBlockedSignal : ISignal
    {
        public SimId ActorId;
        public ContentId ActionId;
        public SimId TargetId;
        public string Reason;
    }
    
    /// <summary>
    /// Fired when a stat changes
    /// </summary>
    public struct StatChangedSignal : ISignal
    {
        public SimId EntityId;
        public ContentId StatId;
        public float OldValue;
        public float NewValue;
    }
    
    /// <summary>
    /// Fired when a tag is added/removed
    /// </summary>
    public struct TagChangedSignal : ISignal
    {
        public SimId EntityId;
        public ContentId TagId;
        public bool Added;
    }
    
    /// <summary>
    /// Fired when an item is added to inventory
    /// </summary>
    public struct ItemAddedSignal : ISignal
    {
        public SimId EntityId;
        public ContentId ItemId;
        public int Quantity;
    }
    
    /// <summary>
    /// Fired when an item is removed from inventory
    /// </summary>
    public struct ItemRemovedSignal : ISignal
    {
        public SimId EntityId;
        public ContentId ItemId;
        public int Quantity;
    }
    
    /// <summary>
    /// Fired when an event starts
    /// </summary>
    public struct EventStartedSignal : ISignal
    {
        public SimId EventInstanceId;
        public ContentId EventDefId;
    }
    
    /// <summary>
    /// Fired when an event stage changes
    /// </summary>
    public struct EventStageChangedSignal : ISignal
    {
        public SimId EventInstanceId;
        public ContentId EventDefId;
        public int OldStage;
        public int NewStage;
    }
    
    /// <summary>
    /// Fired when an event ends
    /// </summary>
    public struct EventEndedSignal : ISignal
    {
        public SimId EventInstanceId;
        public ContentId EventDefId;
        public bool Completed;
    }
    
    /// <summary>
    /// Fired when a timer completes
    /// </summary>
    public struct TimerCompletedSignal : ISignal
    {
        public ContentId TimerId;
        public SimId OwnerId;
    }
    
    /// <summary>
    /// Fired when entering a world area
    /// </summary>
    public struct AreaEnteredSignal : ISignal
    {
        public SimId EntityId;
        public ContentId AreaId;
    }
    
    /// <summary>
    /// Fired when exiting a world area
    /// </summary>
    public struct AreaExitedSignal : ISignal
    {
        public SimId EntityId;
        public ContentId AreaId;
    }
    
    /// <summary>
    /// Fired for UI notifications/hints
    /// </summary>
    public struct NotificationSignal : ISignal
    {
        public string Message;
        public string Category;
        public Priority Priority;
        public SimId RelatedEntityId;
    }
    
    /// <summary>
    /// Fired when dialogue starts
    /// </summary>
    public struct DialogueStartedSignal : ISignal
    {
        public SimId SpeakerId;
        public ContentId DialogueId;
    }
    
    /// <summary>
    /// Fired for dialogue lines
    /// </summary>
    public struct DialogueLineSignal : ISignal
    {
        public SimId SpeakerId;
        public string Text;
        public string[] Choices;
    }
    
    /// <summary>
    /// Fired when dialogue ends
    /// </summary>
    public struct DialogueEndedSignal : ISignal
    {
        public SimId SpeakerId;
        public ContentId DialogueId;
    }
    
    /// <summary>
    /// Fired when AI state changes (for debug/UI)
    /// </summary>
    public struct AIStateChangedSignal : ISignal
    {
        public SimId EntityId;
        public string OldState;
        public string NewState;
    }
    
    /// <summary>
    /// Fired when player is detected (stealth)
    /// </summary>
    public struct PlayerDetectedSignal : ISignal
    {
        public SimId DetectorId;
        public float AlertLevel;
    }

    /// <summary>
    /// Fired when an entity is created/spawned.
    /// </summary>
    public struct EntityCreatedSignal : ISignal
    {
        public SimId EntityId;
        public ContentId ArchetypeId;
    }

    /// <summary>
    /// Fired when an entity is destroyed/despawned.
    /// </summary>
    public struct EntityDestroyedSignal : ISignal
    {
        public SimId EntityId;
        public ContentId ArchetypeId;
    }
}