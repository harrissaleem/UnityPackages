using System;
using UnityEngine;
using SimCore.Services;

namespace SimCore.Input
{
    /// <summary>
    /// Standard game actions that can be triggered.
    /// Games can extend this with additional actions.
    /// </summary>
    public enum GameAction
    {
        // Movement
        Move,
        Sprint,
        Jump,
        Crouch,

        // Interaction
        Interact,
        Attack,
        Cancel,

        // UI
        Pause,
        Back,
        Menu,

        // Selection
        Next,
        Previous,

        // Custom (game-specific)
        Custom1,
        Custom2,
        Custom3,
        Custom4
    }

    /// <summary>
    /// Input mode for different control schemes.
    /// </summary>
    public enum InputMode
    {
        Touch,          // Mobile touch controls
        Keyboard,       // Keyboard + Mouse
        Gamepad         // Controller
    }

    /// <summary>
    /// Abstraction layer for input handling.
    /// Supports touch, keyboard, and gamepad seamlessly.
    /// </summary>
    public interface IInputService : IService
    {
        /// <summary>
        /// Current input mode (touch, keyboard, gamepad).
        /// </summary>
        InputMode CurrentMode { get; }

        /// <summary>
        /// Movement input as normalized Vector2 (-1 to 1).
        /// </summary>
        Vector2 MoveInput { get; }

        /// <summary>
        /// Look/camera input as Vector2.
        /// </summary>
        Vector2 LookInput { get; }

        /// <summary>
        /// Whether sprint is being held.
        /// </summary>
        bool SprintHeld { get; }

        /// <summary>
        /// Whether interact was pressed this frame.
        /// </summary>
        bool InteractPressed { get; }

        /// <summary>
        /// Whether attack was pressed this frame.
        /// </summary>
        bool AttackPressed { get; }

        /// <summary>
        /// Whether cancel/back was pressed this frame.
        /// </summary>
        bool CancelPressed { get; }

        /// <summary>
        /// Event fired when an action is triggered.
        /// </summary>
        event Action<GameAction> OnActionTriggered;

        /// <summary>
        /// Event fired when input mode changes.
        /// </summary>
        event Action<InputMode> OnInputModeChanged;

        /// <summary>
        /// Check if a specific action was pressed this frame.
        /// </summary>
        bool WasPressed(GameAction action);

        /// <summary>
        /// Check if a specific action is being held.
        /// </summary>
        bool IsHeld(GameAction action);

        /// <summary>
        /// Enable or disable input processing.
        /// </summary>
        void SetEnabled(bool enabled);

        /// <summary>
        /// Show/hide mobile touch controls.
        /// </summary>
        void SetMobileControlsVisible(bool visible);

        /// <summary>
        /// Trigger haptic feedback (mobile).
        /// </summary>
        void TriggerHaptic(HapticType type);
    }

    /// <summary>
    /// Haptic feedback types for mobile.
    /// </summary>
    public enum HapticType
    {
        Light,
        Medium,
        Heavy,
        Selection,
        Success,
        Warning,
        Error
    }
}
