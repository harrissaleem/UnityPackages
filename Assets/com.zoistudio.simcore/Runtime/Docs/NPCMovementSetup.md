# NPC Movement System Setup

This document explains how to set up the zone-based, intent-driven NPC movement system.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        ZONE MANAGER                              │
│  - Tracks all zones in scene                                     │
│  - Provides spatial queries                                      │
│  - No per-NPC checks needed                                      │
└─────────────────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────────┐
│                          ZONES                                   │
│  CrossingZone  │  IntersectionZone  │  SpawnZone  │  DespawnZone │
│  - Tracks occupancy (pedestrians/vehicles)                       │
│  - Publishes events when NPCs enter/exit                         │
└─────────────────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────────┐
│                     INTENT NAV AGENT                             │
│  - Wraps NavMeshAgent                                            │
│  - Adjusts area costs based on intent                            │
│  - Calm → uses sidewalks/crossings                               │
│  - Fleeing → ignores roads, takes shortcuts                      │
└─────────────────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────────┐
│                     PEDESTRIAN STATES                            │
│  Walk  │  Wander  │  WaitAtCrossing  │  Flee  │  Engaged         │
└─────────────────────────────────────────────────────────────────┘
```

## Step 1: NavMesh Area Setup

### In Unity Navigation Window:

1. **Open Navigation**: Window → AI → Navigation
2. **Go to Areas tab**
3. **Configure areas** (indices must match ZoneTypes.cs):

| Index | Name       | Default Cost |
|-------|------------|--------------|
| 0     | Walkable   | 1            |
| 1     | Not Walkable | - |
| 2     | Jump       | 2            |
| 3     | Sidewalk   | 1            |
| 4     | Road       | 10           |
| 5     | Crossing   | 2            |
| 6     | Park       | 1            |
| 7     | ParkingLot | 2            |

### Mark Surfaces:

1. Select your sidewalk meshes
2. In Navigation window → Object tab
3. Set "Navigation Area" to "Sidewalk"
4. Repeat for roads (Road), crossings (Crossing), etc.
5. Bake NavMesh

## Step 2: Create Zone Manager

1. Create empty GameObject: `ZoneManager`
2. Add component: `ZoneManager`
3. This will auto-register all zones in scene

## Step 3: Set Up Crossing Zones

### Create a Crossing Zone:

1. Create empty GameObject at zebra crossing location
2. Add BoxCollider (set as Trigger)
3. Size it to cover the crossing area
4. Add `CrossingZone` component
5. Configure:
   - Zone Type: Crossing
   - Vehicle Slowdown Distance: 15
   - Vehicle Stop Distance: 8

### Prefab Recommendation:

Create a prefab: `Prefabs/Zones/CrossingZone`
- BoxCollider (Trigger)
- CrossingZone component
- Tag: "Zone" (create if needed)

## Step 4: Set Up Spawn Zones

### Pedestrian Spawn Zone:

1. Create empty GameObject where pedestrians should spawn
2. Add BoxCollider (Trigger) covering spawn area
3. Add `Zone` component
4. Set Zone Type: PedestrianSpawn

### Vehicle Spawn Zone:

Same as above but Zone Type: VehicleSpawn

## Step 5: NPC Prefab Setup

### Pedestrian Prefab:

```
NPC_Pedestrian (root)
├── Model (your character model)
├── Collider (CapsuleCollider)
│   - Tag: "Pedestrian" or "NPC"
└── Components:
    - NavMeshAgent
    - IntentNavAgent
    - NPCBridge (if using SimCore entity system)
    - AIController
```

### Configure IntentNavAgent:

- Base Speed: 2
- Base Angular Speed: 120
- Stopping Distance: 0.5

## Step 6: Scene Hierarchy

Recommended structure:

```
Scene
├── Environment
│   ├── Ground
│   ├── Buildings
│   ├── Roads (marked as NavMesh "Road")
│   ├── Sidewalks (marked as NavMesh "Sidewalk")
│   └── Parks (marked as NavMesh "Park")
├── Zones
│   ├── ZoneManager
│   ├── Crossings
│   │   ├── CrossingZone_01
│   │   ├── CrossingZone_02
│   │   └── ...
│   ├── Spawns
│   │   ├── PedestrianSpawn_01
│   │   ├── VehicleSpawn_01
│   │   └── ...
│   └── Despawns
│       └── DespawnZone_EdgeOfWorld
├── NPCs (runtime spawned)
└── Player
```

## How It Works

### Intent-Driven Pathfinding

When an NPC's intent changes, the NavMesh area costs change:

**Calm Intent (normal citizen):**
- Sidewalk: Cost 1 (preferred)
- Road: Cost 10 (avoided)
- Crossing: Cost 2 (acceptable)

**Fleeing Intent (escaping suspect):**
- Sidewalk: Cost 1
- Road: Cost 1 (same as sidewalk!)
- Crossing: Cost 1

This means a fleeing NPC will jaywalk naturally without any special logic.

### Zone-Based Interactions

**Crossing Zone Flow:**

1. Pedestrian approaches crossing
2. If vehicles in zone or nearby → wait
3. If clear → cross
4. Vehicles check zone occupancy, not individual NPCs
5. O(1) instead of O(pedestrians × vehicles)

### Mobile Performance

- **No per-NPC distance checks** between pedestrians and vehicles
- **Zones track occupancy** with HashSets (O(1) lookup)
- **Staggered updates** possible in tick functions
- **Object pooling** for NPC spawning

## Usage Examples

### Spawn a Calm Pedestrian

```csharp
var npc = Instantiate(pedestrianPrefab, spawnPos, Quaternion.identity);
var navAgent = npc.GetComponent<IntentNavAgent>();
navAgent.SetIntent(MovementIntent.Calm);
navAgent.SetDestination(targetPos);
```

### Make NPC Jaywalk

```csharp
// Just change intent - pathfinding handles the rest
navAgent.SetIntent(MovementIntent.Hurried);
navAgent.SetDestination(targetPos); // Will take shortcuts through roads
```

### Make NPC Flee

```csharp
// Set fleeing intent - ignores all traffic rules
navAgent.SetIntent(MovementIntent.Fleeing);

// Or use the flee state
var ai = npc.GetComponent<AIController>();
var fleeState = new PedestrianFleeState 
{ 
    ThreatTransform = player.transform,
    FleeDistance = 30f
};
ai.TransitionTo("pedestrian_flee", world);
```

## Validation Checklist

After setup, verify:

- [ ] NavMesh baked with all area types
- [ ] ZoneManager in scene
- [ ] At least one CrossingZone set up
- [ ] Spawn zones configured
- [ ] NPC prefab has NavMeshAgent + IntentNavAgent
- [ ] NPC prefab tagged "Pedestrian" or "NPC"
- [ ] Zones have BoxCollider (IsTrigger = true)

## Next Steps

1. **Vehicle System**: Add spline-based vehicle movement
2. **Traffic Lights**: Add IntersectionZone with light states
3. **LOD System**: Despawn far NPCs, simplify medium-distance ones
4. **POIs**: Add bus stops, benches for idle behavior

