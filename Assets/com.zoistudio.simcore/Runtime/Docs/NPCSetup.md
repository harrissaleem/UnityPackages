# SimCore NPC & World Simulation Setup

## Architecture Overview

SimCore provides **generic base classes** for world simulation. Games extend these with **game-specific implementations**.

```
SimCore (Reusable)                    Game-Specific
├── WorldSimulator.cs          →      PoliceWorldSimController.cs
├── IWorldZone interface       →      DistrictSO (via adapter)
├── SpawnDensityConfig         →      (uses as-is)
├── TimeOfDayModifier          →      (uses as-is)
├── NPCBridge.cs               →      (uses as-is)
├── NPCSpawner.cs              →      (uses as-is)
└── AIController + AIState     →      CivilianAI.cs (game-specific states)
```

## What Goes Where?

### SimCore (Core - Reusable)

| Component | Purpose |
|-----------|---------|
| `WorldSimulator` | Base class for world population |
| `IWorldZone` | Interface for zones/areas |
| `SpawnDensityConfig` | Generic spawn settings |
| `TimeOfDayModifier` | Time-based density changes |
| `NPCBridge` | Unity ↔ SimCore Entity sync |
| `NPCSpawner` | Generic entity spawning |
| `CommonAIStates` | Reusable AI states (Wander, Follow, Flee) |
| `AIController` | State machine controller |

### Game-Specific (CityPatrol)

| Component | Purpose |
|-----------|---------|
| `PoliceWorldSimController` | Extends WorldSimulator |
| `NPCBehaviorProfile` | Police-specific NPC profiles |
| `NPCPersonality` | Personality enum for barks |
| `DistrictSO` | District definition |
| `CivilianAI.cs` | Police-specific AI states |
| `CityPatrolBarks.cs` | Police dialogue |

## Creating a New Game (e.g., Farm Sim)

### 1. Create Zone Definition

```csharp
// FarmSim/Data/FarmZoneSO.cs
[CreateAssetMenu(menuName = "FarmSim/Zone")]
public class FarmZoneSO : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public Vector3 Center;
    public float Radius;
    public float CustomerRate = 1f;  // Game-specific
    public float DeliveryRate = 1f;  // Game-specific
}

// Adapter to make it IWorldZone
public class FarmZoneAdapter : IWorldZone
{
    public FarmZoneSO Zone { get; }
    public FarmZoneAdapter(FarmZoneSO zone) { Zone = zone; }
    
    public string Id => Zone.Id;
    public string DisplayName => Zone.DisplayName;
    public Vector3 Center => Zone.Center;
    public float Radius => Zone.Radius;
    public bool Contains(Vector3 pos) => Vector3.Distance(pos, Center) <= Radius;
}
```

### 2. Create Behavior Profiles

```csharp
// FarmSim/Data/NPCTypes.cs
public enum CustomerType { Regular, VIP, Impatient, Haggler }

[Serializable]
public class CustomerProfile
{
    public string Name = "Regular";
    [Range(0f, 1f)] public float SpawnWeight = 1f;
    public CustomerType Type = CustomerType.Regular;
    public float PatienceMultiplier = 1f;
    public float TipChance = 0.1f;
}
```

### 3. Extend WorldSimulator

```csharp
// FarmSim/Core/FarmWorldSimController.cs
public class FarmWorldSimController : WorldSimulator
{
    [SerializeField] private List<CustomerProfile> _customerProfiles;
    [SerializeField] private GameObject _customerPrefab;
    [SerializeField] private GameObject _deliveryTruckPrefab;
    
    protected override void RegisterZones()
    {
        // Register farm zones from your database
        foreach (var zone in _database.Zones)
            RegisterZone(new FarmZoneAdapter(zone));
    }
    
    protected override void RegisterContent() { }
    
    protected override void SpawnNPCAt(Vector3 position, IWorldZone zone)
    {
        var profile = SelectCustomerProfile(zone);
        
        // Create entity
        var entity = _world.Entities.CreateEntity(
            new ContentId("customer"),
            EntityCategory.NPC,
            GenerateName()
        );
        
        // Apply profile
        entity.AddTag("customer");
        entity.SetData("customer_type", profile.Type.ToString());
        entity.SetData("patience", profile.PatienceMultiplier);
        
        // Spawn Unity GameObject and setup
        // ...
    }
    
    protected override void SpawnVehicleAt(Vector3 position, IWorldZone zone)
    {
        // Spawn delivery trucks
    }
    
    protected override void UpdateNPCs() { /* Tick AI */ }
    protected override void UpdateVehicles() { /* Tick vehicles */ }
}
```

### 4. Create Game-Specific AI States

```csharp
// FarmSim/AI/CustomerAI.cs
public class CustomerIdleState : AIState { /* Look at products */ }
public class CustomerBrowsingState : AIState { /* Walk around */ }
public class CustomerWaitingState : AIState { /* Wait at register */ }
public class CustomerLeavingState : AIState { /* Exit shop */ }
```

## NPCBridge Usage

The `NPCBridge` component syncs SimCore Entity data with Unity NavMesh:

```csharp
// AI states communicate via Entity data
entity.SetData(MovementKeys.MoveTarget, targetPosition);
entity.SetData(MovementKeys.MoveSpeed, 2f);

// NPCBridge reads this and drives NavMeshAgent
// On arrival, NPCBridge sets:
entity.SetData(MovementKeys.HasArrived, true);
```

### Movement Keys

| Key | Type | Description |
|-----|------|-------------|
| `move_target` | `Vector3?` | Target position (null = stop) |
| `move_speed` | `float` | Movement speed |
| `is_moving` | `bool` | Currently moving |
| `has_arrived` | `bool` | Reached destination |

## Time of Day

WorldSimulator supports time-based spawning:

```csharp
_timeModifiers = new List<TimeOfDayModifier>
{
    new() { Name = "Morning", StartHour = 6, EndHour = 12, NPCDensity = 1.2f },
    new() { Name = "Afternoon", StartHour = 12, EndHour = 18, NPCDensity = 1.0f },
    new() { Name = "Evening", StartHour = 18, EndHour = 22, NPCDensity = 0.8f },
    new() { Name = "Night", StartHour = 22, EndHour = 6, NPCDensity = 0.3f }
};
```

## Best Practices

1. **Keep SimCore generic** - Don't add police/farm-specific code to SimCore
2. **Use Entity data** - Store game state on Entity with `SetData<T>()`
3. **Extend, don't modify** - Override abstract methods in your game's controller
4. **Zone awareness** - Use zones for difficulty/density variation
5. **Profile-based spawning** - Define behavior profiles as data, not code
