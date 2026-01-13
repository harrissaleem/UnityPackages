# Creating a New Game with SimCore

This guide documents the pattern for creating a new simulation game using the SimCore framework.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Step 1: Create Game Assembly](#step-1-create-game-assembly)
4. [Step 2: Define Game Signals](#step-2-define-game-signals)
5. [Step 3: Create Data ScriptableObjects](#step-3-create-data-scriptableobjects)
6. [Step 4: Create Core Game Systems](#step-4-create-core-game-systems)
7. [Step 5: Create GameDatabase SO](#step-5-create-gamedatabase-so)
8. [Step 6: Create Content Assets](#step-6-create-content-assets)
9. [Step 7: Wire Up Unity Scene](#step-7-wire-up-unity-scene)
10. [City Patrol Example](#city-patrol-example)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                         YOUR GAME                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │ Game Systems │  │ ScriptableObjects │  │ Unity Components │   │
│  │ (C# Logic)   │  │ (Data/Content)    │  │ (Scene/UI)       │   │
│  └──────┬───────┘  └────────┬─────────┘  └─────────┬────────┘   │
│         │                   │                      │             │
│         └───────────────────┼──────────────────────┘             │
│                             │                                    │
├─────────────────────────────┼────────────────────────────────────┤
│                      SIMCORE FRAMEWORK                           │
│  ┌─────────┐ ┌───────────┐ ┌────────┐ ┌────────┐ ┌─────────┐    │
│  │Entities │ │SignalBus  │ │Actions │ │Effects │ │ Modules │    │
│  │Registry │ │(Events)   │ │Pipeline│ │System  │ │(Opt.)   │    │
│  └─────────┘ └───────────┘ └────────┘ └────────┘ └─────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

### Key Principles

1. **SimCore is game-agnostic** - No game-specific logic in SimCore
2. **Data-driven content** - All content defined via ScriptableObjects
3. **Signal-based communication** - UI subscribes to signals, submits ActionRequests
4. **Game systems orchestrate** - Your game code coordinates SimCore systems

---

## Project Structure

```
Assets/
├── SimCore/                    # Framework (DO NOT MODIFY for game-specific logic)
│   ├── Core/
│   │   ├── Entities/
│   │   ├── Actions/
│   │   ├── Effects/
│   │   ├── Signals/
│   │   └── ...
│   └── Modules/
│       ├── Dialogue/
│       ├── Barks/
│       ├── Quest/
│       └── ...
│
├── YourGame/                   # Your game assembly
│   ├── Core/                   # Game-specific systems
│   │   ├── YourGameSignals.cs
│   │   ├── YourGameGame.cs     # Main orchestrator
│   │   ├── LevelLoader.cs
│   │   └── [GameSpecificManagers].cs
│   ├── Data/                   # ScriptableObject definitions
│   │   ├── EntityArchetypeSO.cs
│   │   ├── [ContentType]SO.cs
│   │   └── GameDatabaseSO.cs
│   ├── Content/                # Game-specific content logic
│   │   ├── YourGameBarks.cs    # Bark registrations
│   │   └── YourGameContent.cs  # (Optional) Code-based fallbacks
│   ├── AI/                     # AI behaviors
│   │   └── [NPC]AI.cs
│   ├── UI/                     # UI controllers
│   │   └── YourGameUIController.cs
│   └── YourGame.asmdef         # Assembly definition
│
└── Resources/                  # Or use Addressables
    └── YourGame/
        ├── Database/
        │   └── GameDatabase.asset
        ├── Entities/
        │   ├── Player.asset
        │   ├── NPC_Type1.asset
        │   └── ...
        ├── [ContentType]/
        │   └── [Content].asset
        └── Levels/
            ├── Level_01.asset
            └── ...
```

---

## Step 1: Create Game Assembly

Create an Assembly Definition file to isolate your game code.

**File: `YourGame/YourGame.asmdef`**
```json
{
    "name": "YourGame",
    "rootNamespace": "YourGame",
    "references": [
        "SimCore",
        "Unity.TextMeshPro",
        "Unity.InputSystem"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

---

## Step 2: Define Game Signals

Create signals for game-specific events that UI needs to respond to.

**File: `YourGame/Core/YourGameSignals.cs`**
```csharp
using SimCore;
using SimCore.Signals;

namespace YourGame
{
    // ========== Game State Signals ==========
    
    public struct GameStartedSignal : ISignal
    {
        public string LevelName;
    }
    
    public struct GameEndedSignal : ISignal
    {
        public bool Success;
        public int Score;
    }
    
    // ========== Gameplay Signals ==========
    
    public struct InteractionAvailableSignal : ISignal
    {
        public SimId TargetId;
        public string TargetName;
        public bool IsInteractable;
    }
    
    // ========== Enums (shared across game) ==========
    
    public enum YourGameCategory
    {
        TypeA,
        TypeB,
        TypeC
    }
}
```

### Signal Naming Convention

| Signal Type | Naming Pattern | Example |
|------------|----------------|---------|
| State Change | `[State]Signal` | `ShiftStartedSignal` |
| Available Action | `[Action]AvailableSignal` | `InteractionAvailableSignal` |
| Action Result | `[Action]CompletedSignal` | `TicketIssuedSignal` |
| UI Request | `[UI]RequestSignal` | `ActionOptionsSignal` |
| Progress | `[System]UpdateSignal` | `ShiftTimeUpdateSignal` |

---

## Step 3: Create Data ScriptableObjects

Define ScriptableObjects for each type of game content.

### 3.1 Entity Archetype SO

Defines entity templates (player, NPCs, objects).

**File: `YourGame/Data/EntityArchetypeSO.cs`**
```csharp
using System.Collections.Generic;
using UnityEngine;
using SimCore;

namespace YourGame.Data
{
    [CreateAssetMenu(fileName = "Entity_New", menuName = "YourGame/Entity Archetype")]
    public class EntityArchetypeSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string DisplayName;
        public EntityCategory Category;
        
        [Header("Visual")]
        public GameObject Prefab;
        public Sprite Icon;
        
        [Header("Stats")]
        public List<StatValue> InitialStats;
        
        [Header("Tags")]
        public List<string> InitialTags;
    }
    
    [System.Serializable]
    public class StatValue
    {
        public string StatId;
        public float Value;
    }
}
```

### 3.2 Content-Specific SOs

Create SOs for each content type your game needs.

**Pattern:**
```csharp
using UnityEngine;

namespace YourGame.Data
{
    [CreateAssetMenu(fileName = "Content_New", menuName = "YourGame/Content Type")]
    public class ContentTypeSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string DisplayName;
        [TextArea(2, 4)]
        public string Description;
        
        [Header("Configuration")]
        // Add content-specific fields
        
        [Header("Points/Rewards")]
        public int RewardPoints;
        public int PenaltyPoints;
        
        /// <summary>
        /// Convert to runtime definition for game systems
        /// </summary>
        public ContentTypeDef ToRuntime()
        {
            return new ContentTypeDef
            {
                Id = Id,
                DisplayName = DisplayName,
                // ... map all fields
            };
        }
    }
    
    /// <summary>
    /// Runtime definition used by game systems
    /// </summary>
    [System.Serializable]
    public class ContentTypeDef
    {
        public string Id;
        public string DisplayName;
        // ... runtime fields
    }
}
```

### 3.3 Level/Shift Configuration SO

Defines a complete level setup.

**File: `YourGame/Data/LevelConfigSO.cs`**
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace YourGame.Data
{
    [CreateAssetMenu(fileName = "Level_New", menuName = "YourGame/Level Configuration")]
    public class LevelConfigSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string DisplayName;
        
        [Header("Settings")]
        public float DurationMinutes = 5f;
        public int StartingPoints = 100;
        
        [Header("Entities to Spawn")]
        public List<SpawnedEntity> Entities;
        
        [Header("Objectives")]
        public List<ObjectiveSO> Objectives;
    }
    
    [System.Serializable]
    public class SpawnedEntity
    {
        public EntityArchetypeSO Archetype;
        public Vector3 Position;
        public float Rotation;
        
        [Header("Entity-Specific Data")]
        public string CustomId;  // Optional override
        public List<string> AdditionalTags;
    }
}
```

---

## Step 4: Create Core Game Systems

### 4.1 Main Game Orchestrator

**File: `YourGame/Core/YourGameGame.cs`**
```csharp
using UnityEngine;
using SimCore;
using SimCore.Signals;
using YourGame.Data;

namespace YourGame
{
    public class YourGameGame : MonoBehaviour
    {
        [Header("Database")]
        [SerializeField] private GameDatabaseSO _database;
        [SerializeField] private LevelConfigSO _defaultLevel;
        
        // SimCore systems
        private SimWorld _world;
        private SignalBus _signalBus;
        
        // Game systems
        private LevelLoader _levelLoader;
        // ... other managers
        
        // Properties for external access
        public SimWorld World => _world;
        public SignalBus SignalBus => _signalBus;
        
        private void Awake()
        {
            InitializeSimCore();
            LoadContent();
            CreatePlayer();
        }
        
        private void InitializeSimCore()
        {
            _signalBus = new SignalBus();
            _world = new SimWorld(_signalBus);
            
            // Initialize game systems
            _levelLoader = new LevelLoader(_world, _signalBus);
        }
        
        private void LoadContent()
        {
            // Register content from database
            foreach (var entitySO in _database.EntityArchetypes)
            {
                // Register with your systems
            }
        }
        
        private void CreatePlayer()
        {
            var playerArchetype = _database.PlayerArchetype;
            // Create player entity
        }
        
        private void Update()
        {
            if (!_isActive) return;
            _world.Tick(Time.deltaTime);
        }
        
        public void StartLevel(LevelConfigSO config = null)
        {
            var levelConfig = config ?? _defaultLevel;
            _levelLoader.LoadLevel(levelConfig);
            
            _signalBus.Publish(new GameStartedSignal
            {
                LevelName = levelConfig.DisplayName
            });
        }
    }
}
```

### 4.2 Level Loader

**File: `YourGame/Core/LevelLoader.cs`**
```csharp
using System.Collections.Generic;
using UnityEngine;
using SimCore;
using SimCore.Signals;
using YourGame.Data;

namespace YourGame
{
    public class LevelLoader
    {
        private readonly SimWorld _world;
        private readonly SignalBus _signalBus;
        private readonly List<SimId> _spawnedEntities = new();
        private readonly List<GameObject> _spawnedObjects = new();
        
        public LevelLoader(SimWorld world, SignalBus signalBus)
        {
            _world = world;
            _signalBus = signalBus;
        }
        
        public void LoadLevel(LevelConfigSO config)
        {
            ClearLevel();
            
            foreach (var spawn in config.Entities)
            {
                SpawnEntity(spawn);
            }
        }
        
        private void SpawnEntity(SpawnedEntity spawn)
        {
            var archetype = spawn.Archetype;
            
            // Create entity in SimCore
            var entity = _world.Entities.CreateEntity(
                new ContentId(archetype.Id),
                archetype.Category,
                spawn.CustomId ?? archetype.DisplayName
            );
            
            // Apply initial stats
            foreach (var stat in archetype.InitialStats)
            {
                entity.SetStat(new ContentId(stat.StatId), stat.Value);
            }
            
            // Apply initial tags
            foreach (var tag in archetype.InitialTags)
            {
                entity.AddTag(new ContentId(tag));
            }
            
            // Apply additional tags
            foreach (var tag in spawn.AdditionalTags)
            {
                entity.AddTag(new ContentId(tag));
            }
            
            // Register position
            _world.Partition.RegisterEntity(entity.Id, spawn.Position);
            
            // Spawn visual prefab if provided
            if (archetype.Prefab != null)
            {
                var go = Object.Instantiate(archetype.Prefab, spawn.Position, 
                    Quaternion.Euler(0, spawn.Rotation, 0));
                _spawnedObjects.Add(go);
            }
            
            _spawnedEntities.Add(entity.Id);
        }
        
        public void ClearLevel()
        {
            foreach (var entityId in _spawnedEntities)
            {
                _world.Partition.UnregisterEntity(entityId);
                // Optionally destroy entity
            }
            _spawnedEntities.Clear();
            
            foreach (var go in _spawnedObjects)
            {
                Object.Destroy(go);
            }
            _spawnedObjects.Clear();
        }
    }
}
```

---

## Step 5: Create GameDatabase SO

Central registry for all game content.

**File: `YourGame/Data/GameDatabaseSO.cs`**
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace YourGame.Data
{
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "YourGame/Game Database")]
    public class GameDatabaseSO : ScriptableObject
    {
        [Header("Player")]
        public EntityArchetypeSO PlayerArchetype;
        
        [Header("Entity Archetypes")]
        public List<EntityArchetypeSO> EntityArchetypes;
        
        [Header("Content Types")]
        public List<ContentTypeSO> ContentTypes;
        
        [Header("Levels")]
        public List<LevelConfigSO> Levels;
        
        // Helper methods for lookups
        public EntityArchetypeSO GetEntity(string id)
        {
            return EntityArchetypes.Find(e => e.Id == id);
        }
        
        public LevelConfigSO GetLevel(string id)
        {
            return Levels.Find(l => l.Id == id);
        }
    }
}
```

---

## Step 6: Create Content Assets

Now create the actual ScriptableObject assets in Unity.

### Folder Structure for Assets

```
Assets/Resources/YourGame/
├── Database/
│   └── GameDatabase.asset          ← Master database
│
├── Entities/
│   ├── Player.asset                ← Player archetype
│   ├── NPC_TypeA.asset
│   ├── NPC_TypeB.asset
│   └── Object_TypeA.asset
│
├── ContentTypes/
│   ├── Action_TypeA.asset
│   ├── Action_TypeB.asset
│   └── ...
│
└── Levels/
    ├── Level_Tutorial.asset
    ├── Level_01.asset
    ├── Level_02.asset
    └── ...
```

### Creating Assets in Unity

1. **Right-click in Project window** → Create → YourGame → [Asset Type]
2. **Fill in the Inspector fields**
3. **Add to GameDatabase.asset** references

---

## Step 7: Wire Up Unity Scene

### Scene Hierarchy

```
Scene
├── GameManager                     ← YourGameGame.cs component
│   └── [Assign GameDatabase SO]
│
├── Canvas (UI)
│   └── [UI Elements]
│       └── YourGameUIController.cs
│
├── Player                          ← Created at runtime or pre-placed
│   └── PlayerController.cs
│
└── Environment
    └── [Static scene objects]
```

### UI Controller Pattern

```csharp
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SimCore.Signals;

namespace YourGame
{
    public class YourGameUIController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private YourGameGame _game;
        
        [Header("HUD")]
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _timerText;
        
        [Header("Interaction")]
        [SerializeField] private Button _actionButton;
        [SerializeField] private GameObject _actionPanel;
        
        private void Start()
        {
            if (_game == null)
                _game = FindFirstObjectByType<YourGameGame>();
            
            SubscribeToSignals();
            SetupButtons();
        }
        
        private void SubscribeToSignals()
        {
            _game.SignalBus.Subscribe<GameStartedSignal>(OnGameStarted);
            _game.SignalBus.Subscribe<GameEndedSignal>(OnGameEnded);
            _game.SignalBus.Subscribe<InteractionAvailableSignal>(OnInteractionAvailable);
        }
        
        private void SetupButtons()
        {
            _actionButton.onClick.AddListener(OnActionButtonPressed);
        }
        
        // Signal handlers update UI
        private void OnGameStarted(GameStartedSignal signal)
        {
            // Update UI
        }
        
        // Button handlers submit action requests
        private void OnActionButtonPressed()
        {
            // Submit action to game systems
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from all signals
            if (_game?.SignalBus != null)
            {
                _game.SignalBus.Unsubscribe<GameStartedSignal>(OnGameStarted);
                // ... unsubscribe all
            }
        }
    }
}
```

---

## City Patrol Example

Here's how City Patrol implements this pattern:

### ScriptableObject Types

| SO Type | Purpose | Menu Path |
|---------|---------|-----------|
| `EntityArchetypeSO` | Player, Civilians, Vehicles | CityPatrol/Entity Archetype |
| `IncidentDefSO` | Jaywalking, Expired Meter, etc. | CityPatrol/Incident Definition |
| `TicketTypeDefSO` | Ticket types with fines | CityPatrol/Ticket Type |
| `ShiftObjectiveSO` | "Issue 3 tickets", etc. | CityPatrol/Shift Objective |
| `DistrictSO` | Downtown, Suburbs, etc. | CityPatrol/District |
| `RankDefSO` | Rookie, Officer, Sergeant | CityPatrol/Rank Definition |
| `ShiftConfigSO` | Complete shift setup | CityPatrol/Shift Configuration |
| `GameDatabaseSO` | Master registry | CityPatrol/Game Database |

### Creating City Patrol Assets

#### 1. Create Entity Archetypes

**Player.asset:**
- Id: `player`
- DisplayName: `Officer`
- Category: `Player`
- InitialStats: `speed: 5`, `stamina: 100`
- Prefab: Player capsule prefab

**Civilian_Normal.asset:**
- Id: `civilian_normal`
- DisplayName: `Civilian`
- Category: `NPC`
- InitialTags: `civilian`

**Vehicle_Parked.asset:**
- Id: `vehicle_parked`
- DisplayName: `Parked Vehicle`
- Category: `Object`
- InitialTags: `vehicle`, `parked`

#### 2. Create Incident Definitions

**Incident_Jaywalking.asset:**
- Id: `jaywalking`
- DisplayName: `Jaywalking`
- Category: `Pedestrian`
- TargetType: `Pedestrian`
- DefaultTicketId: `ticket_jaywalking`
- SPCorrect: `10`
- CPWrong: `10`

**Incident_ExpiredMeter.asset:**
- Id: `expired_meter`
- DisplayName: `Expired Parking Meter`
- Category: `Parking`
- TargetType: `Vehicle`
- DefaultTicketId: `ticket_parking`
- SPCorrect: `12`
- CPWrong: `10`

#### 3. Create Ticket Types

**Ticket_Jaywalking.asset:**
- Id: `ticket_jaywalking`
- DisplayName: `Jaywalking Violation`
- FineAmount: `50`
- SP: `10`
- WrongCP: `10`
- ApplicableTo: `Pedestrian`

#### 4. Create Shift Objectives

**Objective_Issue3Tickets.asset:**
- Id: `issue_3_tickets`
- Description: `Issue 3 tickets`
- Type: `IssueTickets`
- RequiredCount: `3`
- BonusSP: `20`
- ShowProgress: `true`

#### 5. Create Shift Configuration

**Shift_Day1_Tutorial.asset:**
- Id: `day1_tutorial`
- DisplayName: `Day 1 - Training`
- District: [Reference to Downtown district]
- PatrolType: `Foot`
- DurationMinutes: `5`
- StartingCP: `100`
- Objectives: [Issue3Tickets, NoMistakes]
- PrePlacedIncidents:
  - Incident: Jaywalking, Position: (10, 0, 5)
  - Incident: ExpiredMeter, Position: (15, 0, 10)

#### 6. Create GameDatabase

**GameDatabase.asset:**
- PlayerArchetype: [Player.asset]
- EntityArchetypes: [Civilian_Normal, Vehicle_Parked, ...]
- IncidentDefinitions: [Jaywalking, ExpiredMeter, ...]
- TicketTypes: [Ticket_Jaywalking, Ticket_Parking, ...]
- Ranks: [Rookie, Officer, ...]
- Shifts: [Day1_Tutorial, Day2_Downtown, ...]

---

## Quick Reference: Data Flow

```
┌─────────────────┐
│ GameDatabaseSO  │ ← References all content
└────────┬────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│ YourGameGame    │────▶│ LevelLoader     │
│ (MonoBehaviour) │     │ Spawns entities │
└────────┬────────┘     └─────────────────┘
         │
         ▼
┌─────────────────┐     ┌─────────────────┐
│ SimWorld        │◀───▶│ YourManagers    │
│ (Entity/Action) │     │ (Game Logic)    │
└────────┬────────┘     └────────┬────────┘
         │                       │
         ▼                       ▼
┌─────────────────┐     ┌─────────────────┐
│ SignalBus       │────▶│ UI Controller   │
│ (Events)        │     │ (Updates UI)    │
└─────────────────┘     └─────────────────┘
```

---

## Checklist for New Game

- [ ] Create assembly definition (.asmdef)
- [ ] Define game signals (YourGameSignals.cs)
- [ ] Define enums in signals file
- [ ] Create EntityArchetypeSO.cs
- [ ] Create content-specific SO classes
- [ ] Create LevelConfigSO.cs
- [ ] Create GameDatabaseSO.cs
- [ ] Create LevelLoader.cs
- [ ] Create main game orchestrator (YourGameGame.cs)
- [ ] Create UI controller
- [ ] Create SO assets in Unity
- [ ] Set up GameDatabase.asset with all references
- [ ] Set up scene with GameManager
- [ ] Test with a simple level

---

## Tips

1. **Start Simple**: Create minimal SOs first, add fields as needed
2. **Use Prefabs**: Entity archetypes should reference prefabs for visuals
3. **Test Incrementally**: Create one level, test, then expand
4. **Keep IDs Consistent**: Use lowercase_snake_case for IDs
5. **Document Your Content**: Add descriptions to SO fields
6. **Use Folders**: Organize SOs by type in Resources/YourGame/

