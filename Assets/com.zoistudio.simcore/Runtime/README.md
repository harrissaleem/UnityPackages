# SimCore - Reusable Simulation Core for Unity

SimCore is a modular, data-driven simulation framework for Unity that enables rapid development of simulation games by swapping theme packs.

## Core Concepts

Every simulation is built on these fundamental concepts:

| Concept | Description |
|---------|-------------|
| **Entities** | Player, NPCs, and objects with stats, tags, flags, and counters |
| **Actions** | Operations that go through the Action Pipeline (validate → handle → effects → signals) |
| **Effects** | Atomic world mutations (modify stat, add tag, spawn event, etc.) |
| **Events** | Multi-stage scenarios that can be spawned, advanced, and completed |
| **Rules** | Condition → Effects triggers (periodic or event-driven) |
| **Modules** | Optional systems (Dialogue, Barks, Quests, Stealth, Economy, Crafting) |

## Architecture Overview

```
SimWorld (Central Coordinator)
├── SignalBus (Event-based UI communication)
├── EntityRegistry (Entity management)
├── InventoryManager (Item storage)
├── ActionPipeline (Action processing)
├── TimerManager (Time-based triggers)
├── EventManager (Multi-stage events)
├── RulesEngine (Condition → Effects)
├── AIManager (FSM-based AI)
├── ProgressionManager (Unlocks/achievements/ranks)
├── Game Pattern Modules
│   ├── ScoringModule (scores/gauges: SP, CP, XP, stamina)
│   ├── SessionModule (timed sessions: shifts, rounds, days)
│   ├── QuestModule (unified quests/objectives/achievements)
│   ├── ScenarioModule (incidents/events with resolutions)
│   └── TaskQueueModule (worker pools: backup, delivery, etc.)
└── Optional Modules
    ├── Dialogue (branching conversations)
    ├── Barks (quick NPC reactions)
    ├── Stealth (detection/awareness)
    ├── Economy (money transactions)
    └── Crafting (production recipes)
```

## Quick Start

### 1. Create a SimWorld

```csharp
var world = new SimWorld();
```

### 2. Load Theme Content

```csharp
var contentProvider = new MyThemeContentProvider();
contentProvider.Initialize();
ContentLoader.LoadContent(world, contentProvider);
```

### 3. Register Custom Handlers

```csharp
world.Actions.AddHandler(new MyActionHandler());
```

### 4. Create Entities

```csharp
var player = ContentLoader.CreateEntityFromArchetype(world, playerArchetype, "Player");
```

### 5. Tick the Simulation

```csharp
void Update() {
    world.Tick(Time.deltaTime);
}
```

### 6. Submit Actions

```csharp
var request = new ActionRequest(playerId, "interact", targetId);
var response = world.SubmitAction(request);
```

## Action Pipeline

All actions flow through:

```
ActionRequest → Validators → Handler → Effects → Signals
```

- **Validators**: Check if action is allowed (cooldown, requirements, range)
- **Handler**: Execute logic, return Effects (custom or data-driven)
- **Effects**: Atomic operations applied to world
- **Signals**: Notifications emitted for UI

## Effects System

Built-in effects:

| Effect | Description |
|--------|-------------|
| `ModifyStatEffect` | Change a stat value |
| `AddTagEffect` / `RemoveTagEffect` | Manage tags |
| `SetFlagEffect` / `ClearFlagEffect` | Manage flags |
| `IncrementCounterEffect` | Modify counters |
| `AddItemEffect` / `RemoveItemEffect` | Inventory changes |
| `StartTimerEffect` / `CancelTimerEffect` | Timer management |
| `SpawnEventEffect` | Spawn an event |
| `AdvanceEventStageEffect` | Progress event |
| `QuestProgressEffect` | Quest tracking |
| `NotifyEffect` | UI notifications |

## Conditions

Built-in conditions for rules and validation:

- `HasTagCondition` / `MissingTagCondition`
- `StatAboveCondition` / `StatBelowCondition`
- `FlagSetCondition`
- `CounterAtLeastCondition`
- `HasItemCondition` / `ItemCountAtLeastCondition`
- `InAreaCondition` / `InRangeCondition`
- `TimeInWindowCondition`
- `EventStageIsCondition` / `EventActiveCondition`
- `AndCondition` / `OrCondition` / `NotCondition`

## Events/Scenarios

Events support multiple stages with:

- Enter/exit effects per stage
- Auto-advance by duration or conditions
- Tags applied during event

```csharp
var eventDef = new EventDef {
    Id = "my_event",
    Stages = new List<EventStageDef> {
        new EventStageDef {
            StageId = "stage1",
            DurationSeconds = 10f,
            EnterEffects = new List<Effect> { ... }
        },
        new EventStageDef {
            StageId = "stage2",
            IsFinal = true
        }
    }
};
```

## AI System

Simple FSM that outputs actions only:

```csharp
public class MyAIState : AIState {
    public override AITickResult Tick(AIController controller, SimWorld world) {
        // Return action or transition
        return AITickResult.DoAction(new ActionRequest(...));
        // or
        return AITickResult.Transition("other_state");
    }
}
```

## Persistence

Two modes:

- **Light Persistence**: Player, inventory, timers, unlocks (Farm-style)
- **Heavy Persistence**: All entities, events, full world state (Police-style)

```csharp
var persistence = new LightPersistence(); // or HeavyPersistence
var saveData = persistence.CreateSave(world);
persistence.LoadSave(world, saveData);
```

## Game Pattern Modules

These new modules provide higher-level game patterns built on SimCore primitives. They bridge the gap between low-level systems and game-specific needs.

### ScoringModule

Multi-score tracking system for game points, gauges, and meters (SP, CP, XP, stamina, etc.).

**Types of scores:**
| Type | Example | Behavior |
|------|---------|----------|
| **Accumulator** | SP (Service Points) | Starts at 0, no upper bound |
| **Gauge** | CP (Conduct Points) | Has min/max, emits threshold signals |
| **Resource** | Energy, Stamina | Depletes, regenerates |

```csharp
// Register score types
var scoring = new ScoringModule();
scoring.Initialize(world);
scoring.RegisterCurrency(new CurrencyDef {
    Id = "SP",
    DisplayName = "Service Points",
    InitialValue = 0
});
scoring.RegisterCurrency("CP", initialValue: 100, minValue: 0, maxValue: 100);
world.Scoring = scoring;

// Use
scoring.AddScore("SP", 25, reason: "Ticket issued");
scoring.AddScore("CP", -10, reason: "Wrong action");
int sp = scoring.GetScore("SP");
```

**Difference from EconomyModule**: 
- EconomyModule = money, prices, buy/sell transactions
- ScoringModule = game performance metrics with bounds and threshold signals

### SessionModule

Timed game sessions (shifts, days, rounds, missions).

```csharp
var session = new SessionModule();
session.Initialize(world);
world.Session = session;

// Start a session
session.StartSession(new SessionConfig {
    Id = "day1_tutorial",
    DisplayName = "Day 1 - Training",
    DurationSeconds = 300f
});

// Monitor
if (session.IsSessionActive) {
    Debug.Log($"Remaining: {session.RemainingSeconds}s");
}

// End early
session.EndSession(SessionEndReason.EndedEarly);
```

**Signals**: `SessionStartedSignal`, `SessionTimeUpdateSignal`, `SessionPausedSignal`, `SessionEndedSignal`

### QuestModule (Unified Quests/Objectives/Achievements)

Single system for ALL goal tracking - just use different **reset policies**. Inspired by WoW-style quests but simplified for mobile games where objectives and achievements are also "quests" with different lifecycles.

| Reset Policy | Use Case | Example |
|--------------|----------|---------|
| `Never` | Permanent achievements | "Arrest 100 criminals" |
| `OnSessionEnd` | Shift objectives | "Issue 3 tickets this shift" |
| `Daily` | Daily challenges | "Complete 2 shifts today" |
| `Weekly` | Weekly challenges | "Earn 500 SP this week" |
| `Manual` | Story quests | "Reach Sergeant rank" |

```csharp
var quests = new QuestModule();
quests.Initialize(world);
world.Quests = quests;

// Register quest definitions (all types in one system)
quests.RegisterQuest(new QuestDef {
    Id = "shift_issue_3_tickets",
    DisplayName = "Issue 3 Tickets",
    Description = "Issue 3 tickets this shift",
    Category = QuestCategory.Session,
    ResetPolicy = ResetPolicy.OnSessionEnd,  // Clears when shift ends
    RequiredProgress = 3,
    TriggerActionId = "ticket_",  // Auto-progress on actions starting with "ticket_"
    Rewards = new Dictionary<string, int> { ["SP"] = 20 }
});

quests.RegisterQuest(new QuestDef {
    Id = "achievement_100_arrests",
    DisplayName = "Veteran Officer",
    Description = "Make 100 arrests",
    Category = QuestCategory.Achievement,
    ResetPolicy = ResetPolicy.Never,  // Permanent, persisted forever
    RequiredProgress = 100,
    TriggerActionId = "arrest",
    Rewards = new Dictionary<string, int> { ["SP"] = 500 }
});

quests.RegisterQuest(new QuestDef {
    Id = "daily_2_shifts",
    DisplayName = "Daily Duty",
    Description = "Complete 2 shifts today",
    Category = QuestCategory.Daily,
    ResetPolicy = ResetPolicy.Daily,  // Resets at midnight
    RequiredProgress = 2,
    Rewards = new Dictionary<string, int> { ["SP"] = 50 }
});

// Activate quests
quests.ActivateQuest("shift_issue_3_tickets");
quests.ActivateQuest("achievement_100_arrests");

// Manual progress (or auto via TriggerActionId)
quests.ProgressQuest("shift_issue_3_tickets", 1);

// Query by category
var sessionQuests = quests.GetQuestsByCategory(QuestCategory.Session);
var achievements = quests.GetQuestsByCategory(QuestCategory.Achievement);

// End of shift - reset session quests only
quests.ResetQuests(ResetPolicy.OnSessionEnd);
// Achievements stay intact!

// Daily reset (call at midnight)
quests.ResetQuests(ResetPolicy.Daily);
```

**All quests are persisted**: If player quits mid-shift, progress is saved. Only `ResetQuests()` clears quests.

**Signals**: `QuestProgressSignal`, `QuestCompletedSignal`, `QuestFailedSignal`, `QuestsResetSignal`

### ScenarioModule

Spawnable situations with resolutions (incidents, events, encounters).

```csharp
var scenarios = new ScenarioModule();
scenarios.Initialize(world);
world.Scenarios = scenarios;

// Register scenario definitions
scenarios.RegisterScenarioDef(new ScenarioDef {
    Id = "jaywalking",
    DisplayName = "Jaywalking",
    Category = "pedestrian",
    ExpirationSeconds = 120f,
    Resolutions = new List<ResolutionDef> {
        new ResolutionDef {
            Id = "ticket_jaywalking",
            DisplayName = "Issue Ticket",
            IsCorrect = true,
            Rewards = new Dictionary<string, int> { ["SP"] = 10 }
        },
        new ResolutionDef {
            Id = "warn",
            DisplayName = "Warning",
            IsCorrect = true,
            Rewards = new Dictionary<string, int> { ["SP"] = 5 }
        }
    },
    ExpiredPenalties = new Dictionary<string, int> { ["CP"] = 5 }
});

// Spawn scenario
SimId scenarioId = scenarios.SpawnScenario("jaywalking", npcEntityId, position);

// Engage (player interacting)
scenarios.EngageScenario(scenarioId, playerId);

// Resolve
scenarios.ResolveScenario(scenarioId, "ticket_jaywalking");
```

**Signals**: `ScenarioSpawnedSignal`, `ScenarioEngagedSignal`, `ScenarioResolvedSignal`, `ScenarioExpiredSignal`

### ProgressionManager (Enhanced)

Now supports ranks, stats, and milestones in addition to unlocks.

```csharp
// Register rank track
world.Progression.RegisterRankTrack("career", new List<RankDef> {
    new RankDef { Level = 0, Name = "Rookie", RequiredPoints = 0 },
    new RankDef { Level = 1, Name = "Officer", RequiredPoints = 500, Unlocks = new List<string> { "district_suburbs" } },
    new RankDef { Level = 2, Name = "Sergeant", RequiredPoints = 1500 }
});

// Add points (auto-checks for rank up)
world.Progression.AddPoints("career", 250);

// Track stats
world.Progression.IncrementStat("total_tickets");
world.Progression.IncrementStat("total_arrests");

// Register milestones
world.Progression.RegisterMilestone(new MilestoneDef {
    Id = "first_ticket",
    Description = "Issue your first ticket",
    StatId = "total_tickets",
    RequiredValue = 1,
    Rewards = new Dictionary<string, int> { ["SP"] = 10 }
});
```

**Signals**: `RankChangedSignal`, `MilestoneReachedSignal`, `ProgressionStatChangedSignal`

### TaskQueueModule

Generic task/job queue with worker pool for resource-constrained processing.

**Use cases:**
| Game | Tasks | Workers | Process |
|------|-------|---------|---------|
| Police Sim | Arrests | Backup vehicles | Transport to station |
| Farm Sim | Harvests | Delivery trucks | Transport to market |
| Restaurant | Orders | Waiters | Deliver to table |
| Hospital | Patients | Ambulances | Transport to ER |

```csharp
var taskQueue = new TaskQueueModule();
taskQueue.Initialize(world);
world.TaskQueues = taskQueue;

// Register a worker pool (e.g., 2 backup vehicles)
taskQueue.RegisterQueue("backup", new WorkerPoolDef {
    DisplayName = "Backup Units",
    WorkerCount = 2,
    HomePosition = policeStationPosition
});

// Submit a task (arrest needs transport)
var taskId = taskQueue.SubmitTask(new TaskDef {
    QueueId = "backup",
    TaskType = "arrest_transport",
    TargetEntityId = suspectId,
    Position = arrestPosition,
    TravelTimeSeconds = 15f,  // Worker travels to location
    ProcessTimeSeconds = 5f,   // Load suspect
    ReturnTimeSeconds = 20f,   // Return to station
    CompletionRewards = new Dictionary<string, int> { ["SP"] = 10 }
});

// Query state
int pending = taskQueue.GetPendingCount("backup");
int available = taskQueue.GetAvailableWorkerCount("backup");
```

**Worker lifecycle:**
```
Available → EnRoute → Working → Returning → Available
           (travel)   (process)  (return)
```

**Signals**: `TaskSubmittedSignal`, `WorkerAssignedSignal`, `WorkerArrivedSignal`, `TaskCompletedSignal`, `TaskCancelledSignal`

## Bark Module (Quick NPC Reactions)

The Bark Module provides personality-based NPC dialogue reactions - quick one-liners that don't require player choices.

### When to Use

| Use Bark Module | Use Dialogue Module |
|----------------|---------------------|
| Quick reactions (ticket received, arrested) | Full conversations with choices |
| Ambient NPC comments | Interrogations, quests |
| Personality-based one-liners | Branching story dialogue |

### Setup

```csharp
// Create module
var barkModule = new BarkModule(world.SignalBus);

// Register barks for personality + context combinations
barkModule.RegisterBark("hostile", "received_ticket", 
    "This is BULL****! I'm fighting this!", BarkMood.Hostile);

barkModule.RegisterBarks("polite", "received_ticket", new[] {
    new BarkLine("I understand. It won't happen again.", BarkMood.Cooperative),
    new BarkLine("Fair enough. I've learned my lesson.", BarkMood.Cooperative)
});
```

### Usage

```csharp
// Get a random bark based on entity's personality
var bark = barkModule.GetBark(npcEntity, BarkContext.ReceivedTicket);

// Or emit directly as a signal
barkModule.EmitBark(npcEntity, BarkContext.ReceivedTicket);
```

### Personality Detection

By default, reads personality from entity tags (`hostile`, `nervous`, `polite`, etc.). 
Customize by implementing `IPersonalityProvider`.

### Built-in Generic Contexts

SimCore provides only GENERIC contexts that apply to any game:

```csharp
// Greetings
BarkContext.Greet, BarkContext.GreetFriendly, BarkContext.Farewell

// Reactions
BarkContext.ReactHappy, BarkContext.ReactAngry, BarkContext.ReactScared

// Commerce
BarkContext.PurchaseComplete, BarkContext.ItemOutOfStock

// Ambient
BarkContext.Idle, BarkContext.Walking, BarkContext.Working
```

### Game-Specific Contexts

Games define their own context constants. Example from City Patrol:

```csharp
// In CityPatrol/Content/CityPatrolBarks.cs
public static class PatrolBarkContext
{
    public const string IdRequested = "id_requested";
    public const string ReceivedTicket = "received_ticket";
    public const string BeingArrested = "being_arrested";
    // ... police-specific contexts
}
```

## World Partition

Abstraction for spatial queries:

- `SimpleWorldPartition`: Open/grid-based worlds
- `ZoneWorldPartition`: Zone-based games

## Creating a New Theme Pack

1. Create a content provider:

```csharp
public class MyContentProvider : CodeContentProvider {
    public override void Initialize() {
        // Define stats, items, archetypes, actions, events, rules
    }
}
```

2. Create custom action handlers (optional):

```csharp
public class MyHandler : IActionHandler {
    public bool CanHandle(ContentId actionId) => actionId == "my_action";
    public List<Effect> Execute(...) { ... }
}
```

3. Load and register:

```csharp
ContentLoader.LoadContent(world, myProvider);
world.Actions.AddHandler(new MyHandler());
```

## Sample Theme Packs

### Police Sim
- Jaywalking → ID check → Fake ID detection → Arrest → Backup
- Heavy persistence, dialogue, hints from real world state

### Farm Sim  
- Plant → Grow (timer) → Harvest → Stock → Customer purchase
- Light persistence, production timers, economy module

### Horror Sim
- Patrol → Detect → Chase → Search → Return
- Stealth module, FSM AI, multi-stage chase event

## UI Independence

SimCore is completely UI-agnostic. UI systems should:

1. Subscribe to signals: `world.SignalBus.Subscribe<NotificationSignal>(...)`
2. Submit actions: `world.SubmitAction(new ActionRequest(...))`
3. Query state: `world.Entities.GetEntity(id).GetStat("health")`

## Directory Structure

```
Assets/
├── SimCore/
│   ├── Core/
│   │   ├── Actions/
│   │   ├── AI/
│   │   ├── Content/
│   │   ├── Effects/
│   │   ├── Entities/
│   │   ├── Events/
│   │   ├── Inventory/
│   │   ├── Modules/
│   │   ├── Persistence/
│   │   ├── Rules/
│   │   ├── Signals/
│   │   ├── Timers/
│   │   └── World/
│   └── Modules/
│       ├── Barks/
│       ├── Crafting/
│       ├── Dialogue/
│       ├── Economy/
│       ├── Quest/
│       └── Stealth/
├── ThemePacks/
│   ├── Police/
│   ├── Farm/
│   └── Horror/
└── Samples/
    ├── PoliceDemo/
    ├── FarmDemo/
    └── HorrorDemo/
```

## Design Priorities

- ✅ Simple mental model
- ✅ Modular + optional systems
- ✅ UI-agnostic core
- ✅ Data-driven with custom hook support
- ✅ Single-player focused
- ✅ Easy to remember (5 core catalogs: entities, actions, events, rules, quests)

