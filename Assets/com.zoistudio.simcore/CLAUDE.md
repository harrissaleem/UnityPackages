# SimCore - Engine-Level Framework

## Purpose
SimCore is a **game-agnostic engine framework** for building simulation games. It provides reusable systems that games extend through data and composition—never modification.

**Design Philosophy:**
- No game-specific logic in this codebase
- Modular: games cherry-pick only needed modules
- Data-driven: behavior defined through configuration, not code
- Mobile-first: zero-GC patterns, object pooling, memory budgets
- Simple mental model: 5 core catalogs (entities, actions, events, rules, quests)

## Games Using SimCore
- **PoliceSim** - Police simulation with incidents, tickets, arrests
- **WaterSim** - Water/utility simulation
- **Horror Simulation** - Stealth-based horror (Hello Neighbor style)
- **Farm Simulation** - Farming, customers, economy
- **BombBananaGame** - Turn-based multiplayer

These games are **vastly different**—simcore must remain agnostic.

---

## Architecture

### Core Systems (`Runtime/Core/`)
| System | Purpose |
|--------|---------|
| **SimWorld** | Central coordinator, ticks all systems |
| **EntityRegistry** | Entity CRUD, queries by tag/stat/area |
| **ActionPipeline** | Request → Validate → Handle → Effects → Signals |
| **SignalBus** | Zero-GC event system (struct-based) |
| **EffectsSystem** | Atomic world mutations (composable) |
| **EventManager** | Multi-stage scenarios with timers |
| **RulesEngine** | Condition → Effects triggers |
| **InventoryManager** | Item storage per entity |
| **TimerManager** | Delayed/periodic callbacks |
| **AIManager** | FSM-based NPC behaviors |
| **ProgressionManager** | Ranks, unlocks, milestones |
| **WorldPartition** | Spatial queries (grid or zone) |

### Optional Modules (`Runtime/Modules/`)
Games enable only what they need:
- **Scoring** - Multi-currency tracking (SP, CP, XP, stamina)
- **Session** - Timed sessions (shifts, days, rounds)
- **Quest** - Objectives with reset policies
- **Scenarios** - Spawnable incidents with resolutions
- **TaskQueue** - Worker pool for resource-constrained tasks
- **Barks** - Quick NPC reactions
- **Dialogue** - Branching conversations
- **Crafting** - Recipe-based production
- **Economy** - Money & transactions
- **Equipment** - Loadout management
- **Stealth** - Detection & awareness
- **Vehicle** - Driving & physics
- **Merit** - Performance tracking
- **Leaderboard** - Rankings
- **LiveOps** - Remote configuration

### Services (`Runtime/Services/`)
Platform integrations: Ads, IAP, Save system

---

## Key Patterns

### Action Pipeline
```
ActionRequest → Validators → Handler → Effects → SignalBus → UI
```
- `ActionDef`: Defines cooldowns, costs, requirements, default effects
- `IActionValidator`: Pluggable validation (range, tags, stats)
- `IActionHandler`: Custom execution logic (game-specific, lives in game layer)
- Effects: Atomic mutations applied to world state

### Entity Composition
Entities are data containers:
- **Stats**: Numeric values with min/max bounds
- **Tags**: Boolean markers (hostile, stunned, invisible)
- **Flags**: Persistent story flags (boss_defeated, has_key)
- **Counters**: Named integers (arrest_count)
- **Data**: `Dictionary<string, object>` for AI state, game-specific data
- **Inventory**: Optional item storage

### Signal Bus (Zero-GC)
```csharp
signalBus.Subscribe<MySignal>(OnMySignal);
signalBus.Publish(new MySignal { ... });
```
- Struct-based signals (no allocations)
- Direct array storage
- Safe unsubscribe during invoke

### Effects System
Composable atomic operations:
- `ModifyStatEffect`, `AddTagEffect`, `RemoveTagEffect`
- `SetFlagEffect`, `IncrementCounterEffect`
- `AddItemEffect`, `RemoveItemEffect`
- `StartTimerEffect`, `SpawnEventEffect`
- All effects are `[Serializable]` for data-driven use

### ContentProvider Pattern
Games supply content via `IContentProvider`:
```csharp
GetActionDefs()       // All action definitions
GetEventDefs()        // Scenario/event definitions
GetRuleDefs()         // Condition → Effects rules
GetEntityArchetypes() // Template entities
GetStatDefs()         // Stat definitions
GetItemDefs()         // Item definitions
```

---

## Development Rules

### NEVER Do This
- Add game-specific logic (police, farm, horror code)
- Reference game assemblies from simcore
- Hardcode content IDs, stat names, or behavior
- Break zero-GC patterns (no `new` in hot paths)
- Add Unity Editor dependencies to Runtime code
- Create non-optional tight coupling between modules

### ALWAYS Do This
- Keep systems generic and data-driven
- Use signals for cross-system communication
- Make new features optional modules
- Document public APIs with XML comments
- Consider mobile memory/CPU constraints
- Test with all game types in mind

### When Adding a New System
1. Create interface in `Core/Modules/INewModule.cs`
2. Create implementation in `Modules/NewModule/`
3. Add optional property to `SimWorld`
4. Document in `Runtime/Docs/`
5. Ensure games can opt-out completely

### When Modifying Existing Systems
1. Ensure backward compatibility
2. Don't break existing game integrations
3. Keep effects reversible when possible
4. Maintain serialization compatibility

---

## Performance Constraints (Mobile)

**Memory Budget:** 300 MB total
- 150 MB textures, 30 MB audio, 50 MB meshes
- 20 MB scripts, 50 MB overhead

**Zero-GC Rules:**
- Use struct signals, not class
- Pool all spawned objects (NPCs, vehicles, UI)
- Avoid LINQ in hot paths
- Use `StringBuilder` for string operations
- Cache component references

**Tick Order in SimWorld:**
1. Timers → 2. Stealth → 3. AI → 4. Rules → 5. Events → 6. Economy/Crafting → 7. Game Modules

---

## File Structure
```
com.zoistudio.simcore/
└── Runtime/
    ├── Core/           # Foundation (SimWorld, Entities, Actions, Signals, Effects)
    ├── Modules/        # Optional pluggable systems
    ├── Services/       # Platform integration (Ads, IAP, Save)
    ├── Input/          # Mobile input abstraction
    ├── UI/             # Screen/navigation framework
    ├── World/          # Spatial & zone management
    ├── AI/             # FSM behaviors, movement
    ├── Performance/    # Pooling, profiling
    ├── Docs/           # Documentation
    └── Test/           # Test utilities
```

---

## How Games Integrate

Each game is its own Unity project that imports SimCore as a package:
```
GameProject/
├── Packages/
│   └── com.zoistudio.simcore (this package - game-agnostic)
└── Assets/
    └── Scripts/  (game-specific code)
        ├── ContentProvider (defines game content)
        ├── Custom AI States
        ├── Custom Action Handlers
        ├── ScriptableObjects
        └── UI Implementation
```

**Game-specific code lives in the game project, not in this package.**

---

## SimCore Reuse Requirement (For Game Projects)

**Before implementing ANY system in a game project, check SimCore first.**

This applies to: UI flow, scene transitions, saving, networking wrappers, event bus, audio, analytics, input, pooling, state machines, and any common game system.

### Workflow
1. **Inspect SimCore** - Search for existing patterns/modules that solve your need
2. **Reuse if exists** - Use SimCore's implementation, extend via composition if needed
3. **Add to SimCore if engine-level** - If the feature is game-agnostic and reusable, add it to SimCore (not the game project)
4. **Keep game-specific in game** - Only truly game-specific logic stays in the game project

### SimCore Reuse Plan (Required Before Implementation)
Before implementing any system, provide a short plan:
```
## SimCore Reuse Plan: [Feature Name]

**What exists in SimCore:**
- [List relevant existing modules/patterns]

**What I'll add to SimCore:**
- [List new engine-level additions, if any]

**What stays game-specific:**
- [List game-specific implementations]
```

### Examples

**Bad:** Creating a new event system in PoliceSim
**Good:** Using SimCore's SignalBus

**Bad:** Writing save/load logic in FarmSim
**Good:** Using SimCore's persistence system

**Bad:** Building UI navigation from scratch in HorrorSim
**Good:** Extending SimCore's UINavigator and ScreenBase

**Bad:** Creating object pooling in each game
**Good:** Using SimCore's ObjectPool

---

## Existing Documentation
- `Runtime/Docs/README.md` - Quick start, API reference
- `Runtime/Docs/CreatingANewGame.md` - New game setup guide
- `Runtime/Docs/MobileGameArchitecture.md` - Architecture deep dive
- `Runtime/Docs/ScenarioDesign.md` - Event/scenario patterns
- `Runtime/Docs/NPCSetup.md` - NPC creation guide
- `Runtime/Docs/NPCMovementSetup.md` - Movement configuration
