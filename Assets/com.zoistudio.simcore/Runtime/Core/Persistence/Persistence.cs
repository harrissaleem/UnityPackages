// SimCore - Persistence System
// Light and heavy save/load implementations

using System;
using System.Collections.Generic;
using SimCore.Entities;
using SimCore.Events;
using SimCore.Inventory;
using SimCore.Timers;
using SimCore.World;

namespace SimCore.Persistence
{
    /// <summary>
    /// Persistence interface
    /// </summary>
    public interface IPersistence
    {
        /// <summary>
        /// Create a save data snapshot
        /// </summary>
        SaveData CreateSave(SimWorld world);
        
        /// <summary>
        /// Load from save data
        /// </summary>
        void LoadSave(SimWorld world, SaveData data);
        
        /// <summary>
        /// Persistence mode identifier
        /// </summary>
        PersistenceMode Mode { get; }
    }
    
    public enum PersistenceMode
    {
        Light,
        Heavy
    }
    
    /// <summary>
    /// Base save data
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string Version = "1.0";
        public float CurrentTimeSeconds;
        public float TimeOfDay;
        
        // Always saved
        public EntitySnapshot PlayerSnapshot;
        public List<InventorySnapshot> Inventories;
        public List<TimerSnapshot> Timers;
        public ProgressionState Progression;
        
        // Heavy persistence only
        public List<EntitySnapshot> EntitySnapshots;
        public List<EventSnapshot> Events;
        
        // Custom data (per-game)
        public Dictionary<string, string> CustomData = new();
    }
    
    /// <summary>
    /// Light persistence - minimal save for farm-style games
    /// </summary>
    public class LightPersistence : IPersistence
    {
        public PersistenceMode Mode => PersistenceMode.Light;
        
        public SaveData CreateSave(SimWorld world)
        {
            var data = new SaveData
            {
                CurrentTimeSeconds = world.CurrentTime.Seconds,
                TimeOfDay = world.TimeOfDay,
                Inventories = world.Inventories.CreateAllSnapshots(),
                Timers = world.Timers.CreateSnapshots(),
                Progression = world.Progression.CreateSnapshot()
            };
            
            // Save player entity only
            var player = world.Entities.GetPlayer();
            if (player != null)
            {
                data.PlayerSnapshot = player.CreateSnapshot();
            }
            
            return data;
        }
        
        public void LoadSave(SimWorld world, SaveData data)
        {
            world.Reset();
            
            // Restore time
            var currentTime = SimTime.FromSeconds(data.CurrentTimeSeconds);
            world.CurrentTime = currentTime;
            world.TimeOfDay = data.TimeOfDay;
            
            // Restore player
            if (data.PlayerSnapshot != null)
            {
                var player = world.Entities.CreateEntity(
                    data.PlayerSnapshot.ArchetypeId,
                    EntityCategory.Player,
                    data.PlayerSnapshot.DisplayName
                );
                player.RestoreFromSnapshot(data.PlayerSnapshot);
            }
            
            // Restore inventories
            world.Inventories.RestoreFromSnapshots(data.Inventories ?? new List<InventorySnapshot>());
            
            // Restore timers
            world.Timers.RestoreFromSnapshots(data.Timers ?? new List<TimerSnapshot>(), currentTime);
            
            // Restore progression
            if (data.Progression != null)
            {
                world.Progression.RestoreFromSnapshot(data.Progression);
            }
        }
    }
    
    /// <summary>
    /// Heavy persistence - full save for simulation games
    /// </summary>
    public class HeavyPersistence : IPersistence
    {
        public PersistenceMode Mode => PersistenceMode.Heavy;
        
        public SaveData CreateSave(SimWorld world)
        {
            var data = new SaveData
            {
                CurrentTimeSeconds = world.CurrentTime.Seconds,
                TimeOfDay = world.TimeOfDay,
                EntitySnapshots = world.Entities.CreateAllSnapshots(),
                Inventories = world.Inventories.CreateAllSnapshots(),
                Timers = world.Timers.CreateSnapshots(),
                Events = world.Events.CreateSnapshots(),
                Progression = world.Progression.CreateSnapshot()
            };
            
            // Also store player snapshot separately for convenience
            var player = world.Entities.GetPlayer();
            if (player != null)
            {
                data.PlayerSnapshot = player.CreateSnapshot();
            }
            
            return data;
        }
        
        public void LoadSave(SimWorld world, SaveData data)
        {
            world.Reset();
            
            // Restore time
            var currentTime = SimTime.FromSeconds(data.CurrentTimeSeconds);
            world.CurrentTime = currentTime;
            world.TimeOfDay = data.TimeOfDay;
            
            // Restore all entities
            if (data.EntitySnapshots != null)
            {
                world.Entities.RestoreFromSnapshots(data.EntitySnapshots);
            }
            
            // Restore inventories
            world.Inventories.RestoreFromSnapshots(data.Inventories ?? new List<InventorySnapshot>());
            
            // Restore timers
            world.Timers.RestoreFromSnapshots(data.Timers ?? new List<TimerSnapshot>(), currentTime);
            
            // Restore events
            if (data.Events != null)
            {
                world.Events.RestoreFromSnapshots(data.Events);
            }
            
            // Restore progression
            if (data.Progression != null)
            {
                world.Progression.RestoreFromSnapshot(data.Progression);
            }
        }
    }
    
    /// <summary>
    /// Serializer helper for save data
    /// </summary>
    public static class SaveSerializer
    {
        /// <summary>
        /// Serialize to JSON
        /// </summary>
        public static string ToJson(SaveData data)
        {
            return UnityEngine.JsonUtility.ToJson(data, true);
        }
        
        /// <summary>
        /// Deserialize from JSON
        /// </summary>
        public static SaveData FromJson(string json)
        {
            return UnityEngine.JsonUtility.FromJson<SaveData>(json);
        }
        
        /// <summary>
        /// Save to file
        /// </summary>
        public static void SaveToFile(SaveData data, string path)
        {
            var json = ToJson(data);
            System.IO.File.WriteAllText(path, json);
        }
        
        /// <summary>
        /// Load from file
        /// </summary>
        public static SaveData LoadFromFile(string path)
        {
            if (!System.IO.File.Exists(path))
                return null;
            
            var json = System.IO.File.ReadAllText(path);
            return FromJson(json);
        }
    }
}

