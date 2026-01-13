# SimCore Mobile Game Architecture

## Overview

SimCore is a **reusable simulation framework** designed for mobile-first game development. This document outlines the architecture patterns, module boundaries, and best practices for building production-ready mobile simulation games.

---

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           GAME LAYER (CityPatrol, FarmSim, etc.)            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │ Game Config  │  │ Game Systems │  │ Game UI      │  │ Game Content │    │
│  │ (SOs)        │  │ (C# Logic)   │  │ (Screens)    │  │ (Assets)     │    │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘    │
├─────────────────────────────────────────────────────────────────────────────┤
│                           SIMCORE FRAMEWORK                                  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │ Core        │  │ Modules     │  │ Services    │  │ UI Framework│        │
│  │ (Entity,    │  │ (Quest,     │  │ (IAP, Ads,  │  │ (Navigation,│        │
│  │  Actions,   │  │  Session,   │  │  Save,      │  │  Screens,   │        │
│  │  Signals)   │  │  Scoring)   │  │  Analytics) │  │  Binding)   │        │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘        │
├─────────────────────────────────────────────────────────────────────────────┤
│                           UNITY ENGINE                                       │
│  Input System │ UI Toolkit/UGUI │ NavMesh │ Addressables │ Services         │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## SimCore vs Game Layer Responsibilities

### SimCore Owns (Reusable, Game-Agnostic)

| Category | Systems | Description |
|----------|---------|-------------|
| **Core** | Entity, Actions, Effects, Conditions, Signals | Foundation systems |
| **Modules** | Scoring, Session, Quest, Scenario, Dialogue, Barks | Game pattern modules |
| **Services** | IAPService, AdService, SaveService, AnalyticsService | Platform integration |
| **UI** | UINavigator, ScreenBase, ModalSystem, DataBinding | UI framework |
| **Input** | InputService, VirtualJoystick, TouchButton | Mobile input |
| **Flow** | GameFlowStateMachine, GameStateBase | Game flow management |
| **Performance** | ObjectPool, DebugOverlay, Profiler hooks | Optimization tools |

### Game Layer Owns (Game-Specific)

| Category | Examples (CityPatrol) | Description |
|----------|----------------------|-------------|
| **Config** | MasterGameConfigSO, ShiftConfigSO | Game configuration |
| **Systems** | InteractionController, ArrestController | Game mechanics |
| **Scenarios** | TheftOrchestrator, FightOrchestrator | Specific scenarios |
| **Content** | Incidents, Tickets, NPCProfiles | Game data |
| **UI Screens** | ShiftSelectScreen, PatrolHUD | Concrete screens |
| **Flow States** | PatrolState, PrecinctState | Game-specific states |

---

## Module Patterns

### 1. Service Pattern (SimCore Services)

Services are singleton-like systems that provide cross-cutting functionality.

```csharp
// SimCore/Services/IService.cs
public interface IService
{
    void Initialize();
    void Shutdown();
}

// SimCore/Services/ServiceLocator.cs
public static class ServiceLocator
{
    public static T Get<T>() where T : class, IService;
    public static void Register<T>(T service) where T : class, IService;
}
```

**Services in SimCore:**
- `IIAPService` - In-app purchases
- `IAdService` - Advertisements
- `ISaveService` - Persistence
- `IAnalyticsService` - Analytics events
- `IInputService` - Input abstraction
- `IAudioService` - Sound management

### 2. Module Pattern (SimCore Modules)

Modules are systems that plug into SimWorld and participate in the game tick.

```csharp
// SimCore/Core/ISimModule.cs
public interface ISimModule
{
    void Initialize(SimWorld world);
    void Tick(float deltaTime);
    void Shutdown();
}
```

### 3. Screen Pattern (UI Framework)

Screens are self-contained UI views with standardized lifecycle.

```csharp
// SimCore/UI/ScreenBase.cs
public abstract class ScreenBase : MonoBehaviour
{
    public virtual void OnShow(object data) { }
    public virtual void OnHide() { }
    public virtual bool OnBackPressed() => true; // Allow back?
}
```

---

## Game Flow State Machine

SimCore provides a generic state machine for game flow management.

```
┌──────────────────────────────────────────────────────────────────┐
│                      GAME FLOW STATES                             │
│                                                                   │
│   ┌─────────┐    ┌─────────┐    ┌──────────┐    ┌─────────┐     │
│   │  Boot   │───▶│  Menu   │───▶│ Session  │───▶│ Summary │     │
│   │         │    │         │    │ (Patrol) │    │         │     │
│   └─────────┘    └────┬────┘    └────┬─────┘    └────┬────┘     │
│                       │              │               │           │
│                       │         ┌────▼────┐         │           │
│                       │         │  Pause  │         │           │
│                       │         └─────────┘         │           │
│                       │                             │           │
│                       └─────────────────────────────┘           │
│                           (Back to Menu)                         │
└──────────────────────────────────────────────────────────────────┘
```

### State Machine Implementation

```csharp
// SimCore provides base classes
public abstract class GameStateBase
{
    public abstract void Enter(GameFlowContext context);
    public abstract void Exit(GameFlowContext context);
    public virtual void Tick(float deltaTime) { }
}

// Games implement concrete states
public class PatrolState : GameStateBase
{
    public override void Enter(GameFlowContext context)
    {
        // Start shift, enable HUD, spawn NPCs
    }
}
```

---

## UI Navigation System

Stack-based navigation with modal support.

```
┌─────────────────────────────────────────────────────────────────┐
│                        UI LAYER STACK                            │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Modal Layer (Popups, Dialogs, Confirmations)            │ ←─ Top
│  │ ┌─────────────┐  ┌─────────────┐                        │    │
│  │ │ConfirmPopup │  │ RewardPopup │                        │    │
│  │ └─────────────┘  └─────────────┘                        │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ Screen Stack (Main screens, navigated via push/pop)     │    │
│  │ ┌─────────────────────────────────────────────────────┐ │    │
│  │ │ Current Screen (e.g., ShiftSelectScreen)            │ │ ←─ Visible
│  │ └─────────────────────────────────────────────────────┘ │    │
│  │ ┌─────────────────────────────────────────────────────┐ │    │
│  │ │ Previous Screen (e.g., MainMenuScreen)              │ │ ←─ Hidden
│  │ └─────────────────────────────────────────────────────┘ │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │ HUD Layer (Always visible during gameplay)              │ ←─ Bottom
│  │ ┌─────────────┐  ┌─────────────┐  ┌─────────────┐      │    │
│  │ │ Timer       │  │ Score       │  │ Minimap     │      │    │
│  │ └─────────────┘  └─────────────┘  └─────────────┘      │    │
│  └─────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Mobile Input Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     INPUT ABSTRACTION                            │
│                                                                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                    IInputService                         │    │
│  │  Vector2 MoveInput { get; }                             │    │
│  │  Vector2 LookInput { get; }                             │    │
│  │  bool InteractPressed { get; }                          │    │
│  │  bool SprintHeld { get; }                               │    │
│  │  event Action<GameAction> OnActionTriggered;            │    │
│  └─────────────────────────────────────────────────────────┘    │
│                            │                                     │
│           ┌────────────────┼────────────────┐                   │
│           ▼                ▼                ▼                   │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │
│  │MobileInput  │  │DesktopInput │  │ GamepadInput│             │
│  │Service      │  │Service      │  │ Service     │             │
│  └─────────────┘  └─────────────┘  └─────────────┘             │
│        │                │                │                      │
│        ▼                ▼                ▼                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐             │
│  │Virtual      │  │Keyboard+    │  │ Gamepad     │             │
│  │Joystick+    │  │Mouse        │  │ Buttons     │             │
│  │Touch Buttons│  │             │  │             │             │
│  └─────────────┘  └─────────────┘  └─────────────┘             │
└─────────────────────────────────────────────────────────────────┘
```

---

## Economy & Monetization Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    ECONOMY SYSTEM                                │
│                                                                  │
│  ┌─────────────────┐    ┌─────────────────┐                     │
│  │ Soft Currency   │    │ Hard Currency   │                     │
│  │ (Earned)        │    │ (Purchased)     │                     │
│  │                 │    │                 │                     │
│  │ • Cash ($)      │    │ • Gold/Gems     │                     │
│  │ • XP            │    │                 │                     │
│  │ • Reputation    │    │                 │                     │
│  └────────┬────────┘    └────────┬────────┘                     │
│           │                      │                               │
│           └──────────┬───────────┘                               │
│                      ▼                                           │
│           ┌─────────────────────┐                               │
│           │   IEconomyService   │                               │
│           │                     │                               │
│           │ • GetBalance()      │                               │
│           │ • TrySpend()        │                               │
│           │ • Grant()           │                               │
│           │ • GetPriceFor()     │                               │
│           └──────────┬──────────┘                               │
│                      │                                           │
│      ┌───────────────┼───────────────┐                          │
│      ▼               ▼               ▼                          │
│  ┌───────┐      ┌───────┐      ┌───────┐                       │
│  │ Store │      │ IAP   │      │ Ads   │                       │
│  │       │      │       │      │       │                       │
│  │Soft $ │      │Hard $ │      │Rewarded│                       │
│  │Items  │      │Bundles│      │Currency│                       │
│  └───────┘      └───────┘      └───────┘                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## Save System Architecture

```csharp
// Save data is structured hierarchically
public class GameSaveData
{
    public int Version;
    public DateTime LastSaved;

    // Core progress
    public PlayerProgressData Progress;
    public InventoryData Inventory;
    public CurrencyData Currencies;

    // Module snapshots
    public List<QuestState> Quests;
    public List<AchievementState> Achievements;

    // Game-specific
    public object GameSpecificData; // Cast to game type
}
```

**Save Triggers:**
- Auto-save at shift end
- Auto-save at checkpoint
- Manual save from menu
- Background save on app pause

**Anti-Corruption:**
- Write to temp file first
- Validate before overwrite
- Keep one backup
- Version migrations

---

## Performance Guidelines

### Zero-GC Hot Paths

1. **Signals** - Use struct signals, no boxing
2. **Pooling** - Pool all frequently spawned objects
3. **Update loops** - Avoid allocations in Tick()
4. **String operations** - Use StringBuilder or cached strings

### Mobile Optimization Checklist

- [ ] Object pooling for NPCs, vehicles, UI popups
- [ ] LOD system for distant objects
- [ ] Texture atlasing for UI
- [ ] Audio compression
- [ ] Quality presets (Low/Med/High)
- [ ] Frame rate cap options (30/60)
- [ ] Battery saver mode

### Memory Budget (Mobile)

| Category | Budget |
|----------|--------|
| Textures | 150 MB |
| Audio | 30 MB |
| Meshes | 50 MB |
| Scripts | 20 MB |
| Overhead | 50 MB |
| **Total** | **300 MB** |

---

## Assembly Structure

```
SimCore.asmdef
├── Core/
├── Modules/
├── Services/
├── UI/
├── Input/
└── Utils/

CityPatrol.asmdef (references SimCore)
├── Core/
├── Data/
├── Scenarios/
├── AI/
├── UI/
└── Content/

CityPatrol.Editor.asmdef (references CityPatrol, UnityEditor)
└── Editor tools
```

---

## Quick Start: Adding a New System

### 1. Decide: SimCore or Game Layer?

- **SimCore**: Is it reusable for FarmSim? HorrorSim? → Yes = SimCore
- **Game Layer**: Is it police/crime specific? → Yes = CityPatrol

### 2. Create the Interface (SimCore)

```csharp
// SimCore/Services/IMyService.cs
public interface IMyService : IService
{
    void DoSomething();
    event Action<MyEvent> OnSomethingHappened;
}
```

### 3. Implement (SimCore or Game)

```csharp
// Implementation can be in SimCore (default) or Game (override)
public class MyService : IMyService
{
    public void Initialize() { }
    public void Shutdown() { }
    public void DoSomething() { }
}
```

### 4. Register at Startup

```csharp
// In game initialization
ServiceLocator.Register<IMyService>(new MyService());
```

### 5. Use Anywhere

```csharp
var myService = ServiceLocator.Get<IMyService>();
myService.DoSomething();
```

---

## Related Documentation

- [Creating A New Game](CreatingANewGame.md)
- [Scenario Design](ScenarioDesign.md)
- [NPC Setup](NPCSetup.md)
- [CityPatrol Architecture](../../CityPatrol/Docs/ARCHITECTURE.md)
