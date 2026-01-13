// SimCore - Core Types
// Fundamental types used across all SimCore systems

using System;
using System.Collections.Generic;

namespace SimCore
{
    /// <summary>
    /// Unique identifier for entities, events, etc.
    /// </summary>
    [Serializable]
    public struct SimId : IEquatable<SimId>
    {
        public readonly int Value;
        
        public SimId(int value) => Value = value;
        
        public bool IsValid => Value > 0;
        public static SimId Invalid => new SimId(0);
        
        public bool Equals(SimId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is SimId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => $"SimId({Value})";
        
        public static bool operator ==(SimId a, SimId b) => a.Value == b.Value;
        public static bool operator !=(SimId a, SimId b) => a.Value != b.Value;
    }
    
    /// <summary>
    /// String identifier for content definitions (actions, events, stats, etc.)
    /// </summary>
    [Serializable]
    public struct ContentId : IEquatable<ContentId>
    {
        public readonly string Value;
        
        public ContentId(string value) => Value = value ?? string.Empty;
        
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public static ContentId Invalid => new ContentId(null);
        
        public bool Equals(ContentId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ContentId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? "(invalid)";
        
        public static bool operator ==(ContentId a, ContentId b) => a.Value == b.Value;
        public static bool operator !=(ContentId a, ContentId b) => a.Value != b.Value;
        
        public static implicit operator ContentId(string s) => new ContentId(s);
        public static implicit operator string(ContentId id) => id.Value;
    }
    
    /// <summary>
    /// Time in the simulation (ticks or real-time depending on config)
    /// </summary>
    [Serializable]
    public struct SimTime : IEquatable<SimTime>, IComparable<SimTime>
    {
        public readonly float Seconds;
        
        public SimTime(float seconds) => Seconds = seconds;
        
        public static SimTime Zero => new SimTime(0);
        public static SimTime FromSeconds(float s) => new SimTime(s);
        public static SimTime FromMinutes(float m) => new SimTime(m * 60f);
        
        public bool Equals(SimTime other) => Math.Abs(Seconds - other.Seconds) < 0.0001f;
        public override bool Equals(object obj) => obj is SimTime other && Equals(other);
        public override int GetHashCode() => Seconds.GetHashCode();
        public int CompareTo(SimTime other) => Seconds.CompareTo(other.Seconds);
        public override string ToString() => $"{Seconds:F2}s";
        
        public static SimTime operator +(SimTime a, SimTime b) => new SimTime(a.Seconds + b.Seconds);
        public static SimTime operator -(SimTime a, SimTime b) => new SimTime(a.Seconds - b.Seconds);
        public static bool operator <(SimTime a, SimTime b) => a.Seconds < b.Seconds;
        public static bool operator >(SimTime a, SimTime b) => a.Seconds > b.Seconds;
        public static bool operator <=(SimTime a, SimTime b) => a.Seconds <= b.Seconds;
        public static bool operator >=(SimTime a, SimTime b) => a.Seconds >= b.Seconds;
    }
    
    /// <summary>
    /// Result of an action attempt
    /// </summary>
    public enum ActionResult
    {
        Success,
        Failed,
        Blocked,       // Validation failed
        Interrupted,   // Action was cancelled mid-execution
        Pending        // Action is ongoing (multi-frame)
    }
    
    /// <summary>
    /// Context data passed through the action pipeline
    /// </summary>
    [Serializable]
    public class ActionContext
    {
        public Dictionary<string, object> Data { get; } = new Dictionary<string, object>();
        
        public T Get<T>(string key, T defaultValue = default)
        {
            if (Data.TryGetValue(key, out var value) && value is T typed)
                return typed;
            return defaultValue;
        }
        
        public void Set<T>(string key, T value) => Data[key] = value;
        public bool Has(string key) => Data.ContainsKey(key);
    }
    
    /// <summary>
    /// Priority levels for rules and events
    /// </summary>
    public enum Priority
    {
        Lowest = 0,
        Low = 25,
        Normal = 50,
        High = 75,
        Highest = 100,
        Critical = 200
    }
    
    /// <summary>
    /// Entity categories
    /// </summary>
    public enum EntityCategory
    {
        Player,
        NPC,
        Object,
        Vehicle,
        Trigger,
        Other
    }
}

