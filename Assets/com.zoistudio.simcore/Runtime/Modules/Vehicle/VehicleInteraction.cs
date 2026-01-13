// SimCore - Vehicle Interaction Component
// ═══════════════════════════════════════════════════════════════════════════════
// Handles vehicle entry/exit logic, door positions, and occupant tracking.
// Works with VehicleModule to coordinate enter/exit across the game.
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using UnityEngine;
using SimCore;
using SimCore.Data;
using SimCore.Signals;

namespace SimCore.Modules.Vehicle
{
    /// <summary>
    /// Manages vehicle entry/exit points and occupant tracking.
    /// Attach to vehicle prefab root.
    /// </summary>
    public class VehicleInteraction : MonoBehaviour
    {
        #region Serialized Fields

        [Header("═══ ENTRY POINTS ═══")]
        [Tooltip("Driver door position (relative to vehicle)")]
        [SerializeField] private Transform _driverDoor;

        [Tooltip("Driver seat position (where player sits)")]
        [SerializeField] private Transform _driverSeat;

        [Tooltip("Passenger door positions")]
        [SerializeField] private Transform[] _passengerDoors;

        [Tooltip("Passenger seat positions")]
        [SerializeField] private Transform[] _passengerSeats;

        [Header("═══ EXIT POINTS ═══")]
        [Tooltip("Preferred exit point (defaults to driver door)")]
        [SerializeField] private Transform _exitPoint;

        [Tooltip("Fallback exit points if preferred is blocked")]
        [SerializeField] private Transform[] _fallbackExitPoints;

        [Header("═══ DETECTION ═══")]
        [Tooltip("Radius for detecting nearby players/NPCs")]
        [SerializeField] private float _interactionRadius = 3f;

        [Tooltip("Layer mask for entities that can enter")]
        [SerializeField] private LayerMask _entityLayer = ~0;

        [Header("═══ TIMING ═══")]
        [Tooltip("Time to complete enter animation")]
        [SerializeField] private float _enterDuration = 1f;

        [Tooltip("Time to complete exit animation")]
        [SerializeField] private float _exitDuration = 0.8f;

        [Tooltip("Cooldown between enter/exit")]
        [SerializeField] private float _interactionCooldown = 0.5f;

        [Header("═══ BLOCKING ═══")]
        [Tooltip("Radius to check for exit blocking obstacles")]
        [SerializeField] private float _exitBlockCheckRadius = 0.5f;

        [Tooltip("Layer mask for obstacles that block exit")]
        [SerializeField] private LayerMask _obstacleLayer = ~0;

        #endregion

        #region Private Fields

        private SimId _vehicleEntityId;
        private VehicleDefSO _definition;
        private SignalBus _signalBus;

        // Occupants
        private SimId _driverId = SimId.Invalid;
        private List<SimId> _passengerIds = new List<SimId>();

        // State
        private bool _isLocked;
        private bool _requiresKey;
        private float _lastInteractionTime;

        // Transition tracking
        private Dictionary<SimId, TransitionState> _activeTransitions = new Dictionary<SimId, TransitionState>();

        #endregion

        #region Properties

        /// <summary>Is vehicle currently locked</summary>
        public bool IsLocked => _isLocked;

        /// <summary>Does vehicle require a key to operate</summary>
        public bool RequiresKey => _requiresKey;

        /// <summary>Is there a driver</summary>
        public bool HasDriver => _driverId.IsValid;

        /// <summary>Current driver ID</summary>
        public SimId DriverId => _driverId;

        /// <summary>Number of current occupants</summary>
        public int OccupantCount => (_driverId.IsValid ? 1 : 0) + _passengerIds.Count;

        /// <summary>Total seat count</summary>
        public int TotalSeats => 1 + (_passengerSeats?.Length ?? 0);

        /// <summary>Are there empty seats</summary>
        public bool HasEmptySeats => OccupantCount < TotalSeats;

        /// <summary>Is driver seat empty</summary>
        public bool IsDriverSeatEmpty => !_driverId.IsValid;

        /// <summary>Interaction radius for detection</summary>
        public float InteractionRadius => _interactionRadius;

        /// <summary>Vehicle entity ID</summary>
        public SimId VehicleEntityId => _vehicleEntityId;

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the interaction component.
        /// </summary>
        public void Initialize(SimId vehicleId, VehicleDefSO definition, SignalBus signalBus)
        {
            _vehicleEntityId = vehicleId;
            _definition = definition;
            _signalBus = signalBus;

            if (_definition != null)
            {
                _isLocked = _definition.StartsLocked;
                _requiresKey = _definition.RequiresKey;
                _enterDuration = _definition.EnterExitDuration;
                _exitDuration = _definition.EnterExitDuration * 0.8f;
            }

            SetupDefaultTransforms();
        }

        private void SetupDefaultTransforms()
        {
            // Create default positions if not set
            if (_driverDoor == null)
            {
                var doorGO = new GameObject("DriverDoor");
                doorGO.transform.SetParent(transform);
                doorGO.transform.localPosition = new Vector3(-1.2f, 0f, 0.5f);
                _driverDoor = doorGO.transform;
            }

            if (_driverSeat == null)
            {
                var seatGO = new GameObject("DriverSeat");
                seatGO.transform.SetParent(transform);
                seatGO.transform.localPosition = new Vector3(-0.5f, 0.5f, 0.3f);
                _driverSeat = seatGO.transform;
            }

            if (_exitPoint == null)
            {
                _exitPoint = _driverDoor;
            }
        }

        #endregion

        #region Can Enter/Exit Checks

        /// <summary>
        /// Check if entity can enter this vehicle.
        /// </summary>
        public bool CanEnter(SimId entityId, out string reason)
        {
            reason = null;

            // Check cooldown
            if (Time.time - _lastInteractionTime < _interactionCooldown)
            {
                reason = "cooldown";
                return false;
            }

            // Check if already in transition
            if (_activeTransitions.ContainsKey(entityId))
            {
                reason = "in_transition";
                return false;
            }

            // Check if locked
            if (_isLocked)
            {
                reason = "locked";
                return false;
            }

            // Check if full
            if (!HasEmptySeats)
            {
                reason = "full";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if entity can exit.
        /// </summary>
        public bool CanExit(SimId entityId, out string reason)
        {
            reason = null;

            // Check cooldown
            if (Time.time - _lastInteractionTime < _interactionCooldown)
            {
                reason = "cooldown";
                return false;
            }

            // Check if in transition
            if (_activeTransitions.ContainsKey(entityId))
            {
                reason = "in_transition";
                return false;
            }

            // Check if entity is actually in vehicle
            if (!IsOccupant(entityId))
            {
                reason = "not_in_vehicle";
                return false;
            }

            // Check if exit is blocked
            if (IsExitBlocked())
            {
                reason = "exit_blocked";
                return false;
            }

            return true;
        }

        #endregion

        #region Enter/Exit Operations

        /// <summary>
        /// Start entering the vehicle.
        /// </summary>
        public bool TryEnter(SimId entityId, bool asDriver, out Vector3 seatPosition)
        {
            seatPosition = Vector3.zero;

            if (!CanEnter(entityId, out _))
                return false;

            _lastInteractionTime = Time.time;

            // Determine seat
            Transform seat;
            int seatIndex;

            if (asDriver && IsDriverSeatEmpty)
            {
                seat = _driverSeat;
                seatIndex = 0;
                _driverId = entityId;
            }
            else
            {
                // Find first empty passenger seat
                seatIndex = FindEmptyPassengerSeat();
                if (seatIndex < 0)
                    return false;

                seat = _passengerSeats[seatIndex];
                _passengerIds.Add(entityId);
                seatIndex += 1; // Offset for driver seat
            }

            seatPosition = seat.position;

            // Track transition
            _activeTransitions[entityId] = new TransitionState
            {
                EntityId = entityId,
                IsEntering = true,
                SeatIndex = seatIndex,
                StartTime = Time.time,
                Duration = _enterDuration
            };

            // Publish signal
            _signalBus?.Publish(new VehicleEnteredSignal
            {
                EntityId = entityId,
                VehicleId = _vehicleEntityId,
                VehicleDefId = _definition != null ? new ContentId(_definition.Id) : default,
                IsDriver = seatIndex == 0,
                SeatIndex = seatIndex
            });

            return true;
        }

        /// <summary>
        /// Start exiting the vehicle.
        /// </summary>
        public bool TryExit(SimId entityId, out Vector3 exitPosition)
        {
            exitPosition = Vector3.zero;

            if (!CanExit(entityId, out _))
                return false;

            _lastInteractionTime = Time.time;

            // Find exit position
            exitPosition = FindBestExitPosition();

            // Track transition
            bool wasDriver = entityId.Equals(_driverId);
            int seatIndex = wasDriver ? 0 : _passengerIds.IndexOf(entityId) + 1;

            _activeTransitions[entityId] = new TransitionState
            {
                EntityId = entityId,
                IsEntering = false,
                SeatIndex = seatIndex,
                StartTime = Time.time,
                Duration = _exitDuration
            };

            // Remove from occupants
            if (wasDriver)
            {
                _driverId = SimId.Invalid;
            }
            else
            {
                _passengerIds.Remove(entityId);
            }

            // Publish signal
            _signalBus?.Publish(new VehicleExitedSignal
            {
                EntityId = entityId,
                VehicleId = _vehicleEntityId,
                ExitPosition = exitPosition,
                WasDriver = wasDriver
            });

            return true;
        }

        /// <summary>
        /// Force an entity out immediately (for emergencies/death).
        /// </summary>
        public void ForceExit(SimId entityId)
        {
            bool wasDriver = entityId.Equals(_driverId);

            if (wasDriver)
            {
                _driverId = SimId.Invalid;
            }
            else
            {
                _passengerIds.Remove(entityId);
            }

            _activeTransitions.Remove(entityId);

            _signalBus?.Publish(new VehicleExitedSignal
            {
                EntityId = entityId,
                VehicleId = _vehicleEntityId,
                ExitPosition = FindBestExitPosition(),
                WasDriver = wasDriver
            });
        }

        /// <summary>
        /// Check if transition is complete.
        /// </summary>
        public bool IsTransitionComplete(SimId entityId)
        {
            if (!_activeTransitions.TryGetValue(entityId, out var state))
                return true;

            if (Time.time >= state.StartTime + state.Duration)
            {
                _activeTransitions.Remove(entityId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get transition progress (0-1).
        /// </summary>
        public float GetTransitionProgress(SimId entityId)
        {
            if (!_activeTransitions.TryGetValue(entityId, out var state))
                return 1f;

            float elapsed = Time.time - state.StartTime;
            return Mathf.Clamp01(elapsed / state.Duration);
        }

        #endregion

        #region Lock/Unlock

        /// <summary>
        /// Lock the vehicle.
        /// </summary>
        public void Lock()
        {
            _isLocked = true;
            _signalBus?.Publish(new VehicleLockSignal
            {
                VehicleId = _vehicleEntityId,
                IsLocked = true
            });
        }

        /// <summary>
        /// Unlock the vehicle.
        /// </summary>
        public void Unlock(SimId unlockedBy = default)
        {
            _isLocked = false;
            _signalBus?.Publish(new VehicleLockSignal
            {
                VehicleId = _vehicleEntityId,
                IsLocked = false,
                UnlockedBy = unlockedBy
            });
        }

        /// <summary>
        /// Toggle lock state.
        /// </summary>
        public void ToggleLock()
        {
            if (_isLocked)
                Unlock();
            else
                Lock();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check if entity is an occupant.
        /// </summary>
        public bool IsOccupant(SimId entityId)
        {
            return entityId.Equals(_driverId) || _passengerIds.Contains(entityId);
        }

        /// <summary>
        /// Get all occupant IDs.
        /// </summary>
        public List<SimId> GetOccupants()
        {
            var list = new List<SimId>();
            if (_driverId.IsValid)
                list.Add(_driverId);
            list.AddRange(_passengerIds);
            return list;
        }

        /// <summary>
        /// Get the door position for entry.
        /// </summary>
        public Vector3 GetEntryPosition(bool asDriver)
        {
            if (asDriver && _driverDoor != null)
                return _driverDoor.position;

            if (_passengerDoors != null && _passengerDoors.Length > 0 && _passengerDoors[0] != null)
                return _passengerDoors[0].position;

            return _driverDoor != null ? _driverDoor.position : transform.position;
        }

        /// <summary>
        /// Get seat world position.
        /// </summary>
        public Vector3 GetSeatPosition(int seatIndex)
        {
            if (seatIndex == 0 && _driverSeat != null)
                return _driverSeat.position;

            int passengerIndex = seatIndex - 1;
            if (_passengerSeats != null && passengerIndex < _passengerSeats.Length)
                return _passengerSeats[passengerIndex].position;

            return _driverSeat != null ? _driverSeat.position : transform.position;
        }

        private int FindEmptyPassengerSeat()
        {
            if (_passengerSeats == null)
                return -1;

            for (int i = 0; i < _passengerSeats.Length; i++)
            {
                // Check if this seat index is taken
                // (passenger index = seat index, driver is 0)
                bool taken = false;
                // Simple approach: count passengers
                if (i >= _passengerIds.Count)
                    return i;
            }

            return -1;
        }

        private Vector3 FindBestExitPosition()
        {
            // Try preferred exit
            if (_exitPoint != null && !IsPositionBlocked(_exitPoint.position))
                return _exitPoint.position;

            // Try fallbacks
            if (_fallbackExitPoints != null)
            {
                foreach (var point in _fallbackExitPoints)
                {
                    if (point != null && !IsPositionBlocked(point.position))
                        return point.position;
                }
            }

            // Try driver door
            if (_driverDoor != null && !IsPositionBlocked(_driverDoor.position))
                return _driverDoor.position;

            // Last resort: offset from vehicle
            return transform.position + transform.right * -2f;
        }

        private bool IsExitBlocked()
        {
            return IsPositionBlocked(FindBestExitPosition());
        }

        private bool IsPositionBlocked(Vector3 position)
        {
            return Physics.CheckSphere(position + Vector3.up * 0.5f, _exitBlockCheckRadius, _obstacleLayer);
        }

        #endregion

        #region Editor Gizmos

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Interaction radius
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);

            // Driver door
            if (_driverDoor != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_driverDoor.position, 0.3f);
                Gizmos.DrawLine(transform.position, _driverDoor.position);
            }

            // Driver seat
            if (_driverSeat != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(_driverSeat.position, Vector3.one * 0.4f);
            }

            // Passenger doors/seats
            if (_passengerDoors != null)
            {
                Gizmos.color = Color.green;
                foreach (var door in _passengerDoors)
                {
                    if (door != null)
                        Gizmos.DrawWireSphere(door.position, 0.25f);
                }
            }

            if (_passengerSeats != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var seat in _passengerSeats)
                {
                    if (seat != null)
                        Gizmos.DrawWireCube(seat.position, Vector3.one * 0.3f);
                }
            }

            // Exit point
            if (_exitPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_exitPoint.position, 0.4f);
            }
        }
#endif

        #endregion
    }

    /// <summary>
    /// Tracks in-progress enter/exit transitions.
    /// </summary>
    internal struct TransitionState
    {
        public SimId EntityId;
        public bool IsEntering;
        public int SeatIndex;
        public float StartTime;
        public float Duration;
    }
}
