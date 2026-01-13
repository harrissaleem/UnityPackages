# Scenario System Design

This document explains how scenarios (incidents) work and how to add new ones.

## Architecture

```
┌──────────────────────────────────────────────────────────────────┐
│                    SCENARIO DEFINITION (SO)                       │
│  - Id: "jaywalking"                                               │
│  - ValidResolutions: ["ticket_jaywalking", "warn", "let_go"]      │
│  - CorrectResolution: "ticket_jaywalking"                         │
│  - SPReward: 10, CPPenalty: 5 (for wrong resolution)              │
└──────────────────────────────────────────────────────────────────┘
         │
         │ Triggered by AI state or world event
         ▼
┌──────────────────────────────────────────────────────────────────┐
│                    SCENARIO INSTANCE                              │
│  - Id: SimId                                                      │
│  - DefId: "jaywalking"                                            │
│  - TargetEntityId: NPC who is jaywalking                          │
│  - State: Active/Engaged/Resolved/Expired                         │
└──────────────────────────────────────────────────────────────────┘
         │
         │ Player interacts
         ▼
┌──────────────────────────────────────────────────────────────────┐
│                    RESOLUTION                                     │
│  - Player action matches ValidResolution?                         │
│  - Correct? Award SP, progress objectives                         │
│  - Wrong? Deduct CP                                               │
│  - Remove scenario, clean up NPC tags                             │
└──────────────────────────────────────────────────────────────────┘
```

## Current Scenarios (Police Sim)

| Scenario | Trigger | Correct Resolution |
|----------|---------|-------------------|
| Jaywalking | NPC crosses road illegally | ticket_jaywalking |
| Public Intoxication | NPC is drunk | ticket_intoxication or arrest |
| Littering | NPC drops item | ticket_littering |
| Parking Violation | Vehicle parked illegally | ticket_parking |
| Expired Registration | Vehicle plates expired | ticket_documentation |
| Stolen Vehicle | Vehicle flagged stolen | arrest |
| Fake ID | NPC has fake ID | arrest |
| Outstanding Warrants | NPC has warrants | arrest |

## Future Scenarios (To Be Implemented)

### Theft Scenario

```
Trigger: NPC A steals item from NPC B
Actors: Thief (NPC A), Victim (NPC B)
Player Actions:
  1. Chase thief
  2. Stop thief (interaction)
  3. Arrest thief
  4. Recover stolen item
  5. Return item to victim

Scenario Flow:
  1. ScenarioModule.SpawnScenario("theft", thiefId, position)
  2. Thief enters "fleeing" state
  3. Victim has "was_robbed" tag, waits for item return
  4. Player catches thief → standard arrest flow
  5. After arrest, new objective: "Return item to victim"
  6. Player returns to victim → interaction → "return_item"
  7. Scenario fully resolved, bonus SP
```

### Fighting Scenario

```
Trigger: NPC A and NPC B start fighting
Actors: Fighter1 (NPC A), Fighter2 (NPC B)
Player Actions:
  1. Approach fighters
  2. Break up fight (interaction with either)
  3. Check IDs of both
  4. Decide: arrest one, both, or warn
  
Scenario Flow:
  1. ScenarioModule.SpawnScenario("fighting", fighterId1, position)
     - Links fighterId2 as secondary actor
  2. Both NPCs in "fighting" state (stationary, animated)
  3. Player interacts → both stop fighting (engaged state)
  4. Resolution options: arrest_both, arrest_aggressor, warn_both
```

### Multi-Step Scenario Pattern

For complex scenarios like theft:

```csharp
// Scenario definition supports "steps"
new ScenarioDef {
    Id = "theft",
    Steps = new[] {
        new ScenarioStep { Id = "chase", Description = "Chase the thief" },
        new ScenarioStep { Id = "arrest", Description = "Arrest the thief" },
        new ScenarioStep { Id = "return", Description = "Return stolen item to victim" }
    },
    TotalSPReward = 50
}
```

## Adding a New Scenario

### 1. Create Scenario Definition (SO)

```csharp
// In SOGenerator.cs or manually create SO
CreateIncident(
    id: "theft",
    displayName: "Theft in Progress",
    description: "An NPC has stolen something from another NPC",
    category: IncidentCategory.Criminal,
    validResolutions: new[] { "arrest", "warn" },
    correctResolution: "arrest",
    spReward: 30,
    cpPenaltyWrongAction: 15
);
```

### 2. Create AI State to Trigger It

```csharp
public class ThiefFleeingState : AIState
{
    public SimId VictimId { get; set; }
    public string StolenItemId { get; set; }
    
    public override void OnEnter(AIController controller, SimWorld world)
    {
        var entity = world.Entities.GetEntity(controller.EntityId);
        
        // Set fleeing intent - will jaywalk, take shortcuts
        entity.SetData(IntentDataKeys.MovementIntent, (int)MovementIntent.Fleeing);
        
        // Store stolen item on entity
        entity.SetData("stolen_item", StolenItemId);
        entity.SetData("victim_id", VictimId.Value);
        entity.SetFlag("has_stolen_item");
        
        // Spawn scenario
        world.Scenarios?.SpawnScenario("theft", controller.EntityId, 
            world.Partition.GetPosition(controller.EntityId));
    }
}
```

### 3. Handle in InteractionController

The existing `InteractionController` already handles:
- Checking for active scenario on target
- Resolving scenario with player action
- Awarding SP / deducting CP

For theft, add special handling:

```csharp
// In InteractionController
case "arrest":
    PerformArrest();
    
    // Check if thief has stolen item
    if (_currentSession.Target.HasFlag("has_stolen_item"))
    {
        // Create follow-up objective
        var victimId = _currentSession.Target.GetData<string>("victim_id");
        CreateReturnItemObjective(victimId);
    }
    break;
```

## NPC State Integration

The new movement system integrates with scenarios via:

1. **AI State** decides to do something illegal (jaywalk, steal)
2. **AI State** calls `world.Scenarios.SpawnScenario()`
3. **Entity** gets tagged with `has_scenario`
4. **Player** sees "Talk" button when near
5. **InteractionController** shows actions based on scenario type
6. **Resolution** affects scores, objectives, NPC state

## Tags Used by Scenarios

| Tag | Meaning |
|-----|---------|
| `has_scenario` | NPC has active scenario, show Talk button |
| `jaywalking` | Currently jaywalking |
| `drunk` | Intoxicated |
| `has_fake_id` | Fake ID on person |
| `has_warrants` | Outstanding warrants |
| `has_contraband` | Found during search |
| `has_weapon` | Found during search |
| `has_drugs` | Found during search |
| `has_stolen_item` | Stole something |
| `was_robbed` | Victim of theft |
| `fighting` | Currently in a fight |
| `arrested` | Under arrest |
| `engaged` | Talking to player |

