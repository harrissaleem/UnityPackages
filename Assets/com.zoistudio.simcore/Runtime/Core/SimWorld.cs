// SimCore - SimWorld
// Central coordinator for all simulation systems

using System;
using SimCore.Actions;
using SimCore.AI;
using SimCore.Entities;
using SimCore.Events;
using SimCore.Inventory;
using SimCore.Modules;
using SimCore.Modules.Equipment;
using SimCore.Modules.Leaderboard;
using SimCore.Modules.LiveOps;
using SimCore.Modules.Merit;
using SimCore.Modules.Quest;
using SimCore.Modules.Scenarios;
using SimCore.Modules.Scoring;
using SimCore.Modules.Session;
using SimCore.Modules.TaskQueue;
using SimCore.Modules.Vehicle;
using SimCore.Rules;
using SimCore.Signals;
using SimCore.Timers;
using SimCore.World;

namespace SimCore
{
    /// <summary>
    /// Central simulation world - coordinates all systems
    /// </summary>
    public class SimWorld
    {
        // Core systems
        public SignalBus SignalBus { get; }
        public EntityRegistry Entities { get; }
        public InventoryManager Inventories { get; }
        public ActionPipeline Actions { get; }
        public TimerManager Timers { get; }
        public EventManager Events { get; }
        public RulesEngine Rules { get; }
        public AIManager AI { get; }
        public ProgressionManager Progression { get; }
        
        // World partition (replaceable)
        private IWorldPartition _partition;
        public IWorldPartition Partition
        {
            get => _partition;
            set => _partition = value ?? new SimpleWorldPartition(SignalBus);
        }
        
        // Time
        public SimTime CurrentTime { get; set; }
        public float TimeOfDay { get; set; } // 0-24 hours
        public float TimeScale { get; set; } = 1f;
        public bool IsPaused { get; set; }
        
        // Optional modules (set by game)
        public IDialogueModule Dialogue { get; set; }
        public IStealthModule Stealth { get; set; }
        public IEconomyModule Economy { get; set; }
        public ICraftingModule Crafting { get; set; }
        
        // Game pattern modules
        public IScoringModule Scoring { get; set; }
        public ISessionModule Session { get; set; }
        public IQuestModule Quests { get; set; }         // Unified quests/objectives/achievements
        public IScenarioModule Scenarios { get; set; }
        public ITaskQueueModule TaskQueues { get; set; }
        public IVehicleModule Vehicles { get; set; }     // Vehicle management
        public IEquipmentModule Equipment { get; set; }  // Equipment/loadout management
        public IMeritModule Merit { get; set; }          // Performance tracking
        public ILeaderboardModule Leaderboards { get; set; } // Rankings
        public ILiveOpsModule LiveOps { get; set; }      // Live events/config
        
        public SimWorld()
        {
            SignalBus = new SignalBus();
            Entities = new EntityRegistry(SignalBus);
            Inventories = new InventoryManager(SignalBus);
            Actions = new ActionPipeline(this);
            Timers = new TimerManager(SignalBus, () => this);
            Events = new EventManager(SignalBus, () => this);
            Rules = new RulesEngine(SignalBus, () => this);
            AI = new AIManager(SignalBus, Actions);
            Progression = new ProgressionManager(SignalBus);
            _partition = new SimpleWorldPartition(SignalBus);
        }
        
        /// <summary>
        /// Main simulation tick - call from MonoBehaviour.Update
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (IsPaused) return;
            
            float scaledDelta = deltaTime * TimeScale;
            CurrentTime = CurrentTime + SimTime.FromSeconds(scaledDelta);
            
            // Update time of day
            TimeOfDay = (TimeOfDay + scaledDelta / 3600f) % 24f;
            
            // 1. Tick timers
            Timers.Tick();
            
            // 2. Tick optional modules
            Stealth?.Tick(scaledDelta);
            
            // 3. Tick AI (outputs action requests)
            AI.Tick(this);
            
            // 4. Tick rules (periodic)
            Rules.Tick();
            
            // 5. Tick events (auto-advance stages)
            Events.Tick();
            
            // 6. Tick optional modules
            Economy?.Tick(scaledDelta);
            Crafting?.Tick(scaledDelta);
            
            // 7. Tick game pattern modules
            Session?.Tick(scaledDelta);
            Scoring?.Tick(scaledDelta);
            Quests?.Tick(scaledDelta);
            Scenarios?.Tick(scaledDelta);
            TaskQueues?.Tick(scaledDelta);
            Vehicles?.Tick(scaledDelta);
            Equipment?.Tick(scaledDelta);
            Merit?.Tick(scaledDelta);
            Leaderboards?.Tick(scaledDelta);
            LiveOps?.Tick(scaledDelta);
        }
        
        /// <summary>
        /// Submit an action request (from UI or other systems)
        /// </summary>
        public ActionResponse SubmitAction(ActionRequest request)
        {
            return Actions.ProcessAction(request);
        }
        
        /// <summary>
        /// Reset the world
        /// </summary>
        public void Reset()
        {
            Entities.Clear();
            Inventories.Clear();
            Timers.Clear();
            Events.Clear();
            Rules.ResetAllStates();
            AI.Clear();
            Progression.Clear();
            Actions.ClearCooldowns();
            
            if (_partition is SimpleWorldPartition simple)
                simple.Clear();
            else if (_partition is ZoneWorldPartition zone)
                zone.Clear();
            
            // Reset game pattern modules
            Scoring?.Shutdown();
            Session?.Shutdown();
            Quests?.Shutdown();
            Scenarios?.Shutdown();
            TaskQueues?.Shutdown();
            Vehicles?.Shutdown();
            Equipment?.Shutdown();
            Merit?.Shutdown();
            Leaderboards?.Shutdown();
            LiveOps?.Shutdown();
            
            CurrentTime = SimTime.Zero;
            TimeOfDay = 8f; // Default 8am start
        }
    }
}
