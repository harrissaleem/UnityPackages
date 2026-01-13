using System;
using System.Collections.Generic;
using UnityEngine;
using SimCore.Signals;
using SimCore.Services;

namespace SimCore.Tutorial
{
    /// <summary>
    /// Tutorial step definition.
    /// </summary>
    [Serializable]
    public class TutorialStep
    {
        /// <summary>
        /// Unique ID for this step.
        /// </summary>
        public string StepId;

        /// <summary>
        /// Message to display.
        /// </summary>
        public string Message;

        /// <summary>
        /// Optional title for the step.
        /// </summary>
        public string Title;

        /// <summary>
        /// World-space position to highlight (optional).
        /// </summary>
        public Vector3? HighlightPosition;

        /// <summary>
        /// UI element name to highlight (optional).
        /// </summary>
        public string HighlightUIElement;

        /// <summary>
        /// Hand/pointer position (screen space, optional).
        /// </summary>
        public Vector2? PointerPosition;

        /// <summary>
        /// Required action to complete this step.
        /// </summary>
        public TutorialAction RequiredAction;

        /// <summary>
        /// Action parameter (e.g., button name, area name).
        /// </summary>
        public string ActionParameter;

        /// <summary>
        /// Time to auto-advance (0 = manual advance only).
        /// </summary>
        public float AutoAdvanceDelay;

        /// <summary>
        /// Whether to pause game time during this step.
        /// </summary>
        public bool PauseGame;

        /// <summary>
        /// Whether to show skip button.
        /// </summary>
        public bool CanSkip = true;

        /// <summary>
        /// Custom callback when step is shown.
        /// </summary>
        public Action OnShow;

        /// <summary>
        /// Custom callback when step is completed.
        /// </summary>
        public Action OnComplete;
    }

    /// <summary>
    /// Actions that can complete a tutorial step.
    /// </summary>
    public enum TutorialAction
    {
        None,                    // Manual advance (tap anywhere or button)
        TapAnywhere,            // Tap anywhere on screen
        TapButton,              // Tap specific button
        MoveTo,                 // Move to location
        Interact,               // Perform any interaction
        InteractWith,           // Interact with specific target
        PerformAction,          // Perform specific action
        Wait,                   // Wait for duration
        Signal,                 // Wait for specific signal
        Custom                  // Custom condition
    }

    /// <summary>
    /// Tutorial sequence definition.
    /// </summary>
    [Serializable]
    public class TutorialSequence
    {
        public string SequenceId;
        public string DisplayName;
        public List<TutorialStep> Steps = new();
        public bool AllowSkip = true;
        public Action OnComplete;
    }

    /// <summary>
    /// Signal emitted when tutorial step changes.
    /// </summary>
    public struct TutorialStepSignal : ISignal
    {
        public string SequenceId;
        public string StepId;
        public int StepIndex;
        public int TotalSteps;
        public bool IsComplete;
    }

    /// <summary>
    /// Signal requesting tutorial UI update.
    /// </summary>
    public struct TutorialUIRequestSignal : ISignal
    {
        public TutorialStep Step;
        public bool ShouldShow;
    }

    /// <summary>
    /// Tutorial system service interface.
    /// </summary>
    public interface ITutorialService : IService
    {
        /// <summary>
        /// Whether a tutorial is currently active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Current sequence being played.
        /// </summary>
        TutorialSequence CurrentSequence { get; }

        /// <summary>
        /// Current step index.
        /// </summary>
        int CurrentStepIndex { get; }

        /// <summary>
        /// Register a tutorial sequence.
        /// </summary>
        void RegisterSequence(TutorialSequence sequence);

        /// <summary>
        /// Start a tutorial sequence.
        /// </summary>
        void StartSequence(string sequenceId);

        /// <summary>
        /// Advance to next step (if manual advance).
        /// </summary>
        void AdvanceStep();

        /// <summary>
        /// Skip the current tutorial sequence.
        /// </summary>
        void SkipSequence();

        /// <summary>
        /// Mark a sequence as completed (persisted).
        /// </summary>
        void MarkSequenceCompleted(string sequenceId);

        /// <summary>
        /// Check if a sequence has been completed.
        /// </summary>
        bool IsSequenceCompleted(string sequenceId);

        /// <summary>
        /// Notify that a tutorial action was performed.
        /// </summary>
        void NotifyAction(TutorialAction action, string parameter = null);

        /// <summary>
        /// Event fired when tutorial state changes.
        /// </summary>
        event Action<string, int, bool> OnTutorialStateChanged;
    }

    /// <summary>
    /// Tutorial system implementation.
    /// </summary>
    public class TutorialService : ITutorialService, ITickableService
    {
        private readonly Dictionary<string, TutorialSequence> _sequences = new();
        private readonly HashSet<string> _completedSequences = new();

        private TutorialSequence _currentSequence;
        private int _currentStepIndex;
        private float _stepElapsedTime;
        private bool _isWaitingForAction;

        private SignalBus _signalBus;

        public bool IsActive => _currentSequence != null;
        public TutorialSequence CurrentSequence => _currentSequence;
        public int CurrentStepIndex => _currentStepIndex;

        public event Action<string, int, bool> OnTutorialStateChanged;

        /// <summary>
        /// Set signal bus for publishing events.
        /// </summary>
        public void SetSignalBus(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            LoadCompletedSequences();
            Debug.Log("[TutorialService] Initialized");
        }

        public void Shutdown()
        {
            SaveCompletedSequences();
            Debug.Log("[TutorialService] Shutdown");
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive) return;

            var currentStep = GetCurrentStep();
            if (currentStep == null) return;

            _stepElapsedTime += deltaTime;

            // Check for auto-advance
            if (currentStep.AutoAdvanceDelay > 0 && _stepElapsedTime >= currentStep.AutoAdvanceDelay)
            {
                AdvanceStep();
            }

            // Check for wait action
            if (currentStep.RequiredAction == TutorialAction.Wait &&
                _stepElapsedTime >= float.Parse(currentStep.ActionParameter ?? "0"))
            {
                AdvanceStep();
            }
        }

        public void RegisterSequence(TutorialSequence sequence)
        {
            if (sequence == null || string.IsNullOrEmpty(sequence.SequenceId))
            {
                Debug.LogError("[TutorialService] Invalid sequence");
                return;
            }

            _sequences[sequence.SequenceId] = sequence;
            Debug.Log($"[TutorialService] Registered sequence: {sequence.SequenceId} ({sequence.Steps.Count} steps)");
        }

        public void StartSequence(string sequenceId)
        {
            if (!_sequences.TryGetValue(sequenceId, out var sequence))
            {
                Debug.LogError($"[TutorialService] Sequence not found: {sequenceId}");
                return;
            }

            if (sequence.Steps.Count == 0)
            {
                Debug.LogWarning($"[TutorialService] Sequence has no steps: {sequenceId}");
                return;
            }

            _currentSequence = sequence;
            _currentStepIndex = 0;
            _stepElapsedTime = 0f;
            _isWaitingForAction = false;

            Debug.Log($"[TutorialService] Starting sequence: {sequenceId}");
            ShowCurrentStep();
        }

        public void AdvanceStep()
        {
            if (!IsActive) return;

            var currentStep = GetCurrentStep();
            currentStep?.OnComplete?.Invoke();

            _currentStepIndex++;
            _stepElapsedTime = 0f;
            _isWaitingForAction = false;

            // Check if sequence is complete
            if (_currentStepIndex >= _currentSequence.Steps.Count)
            {
                CompleteSequence();
            }
            else
            {
                ShowCurrentStep();
            }
        }

        public void SkipSequence()
        {
            if (!IsActive) return;

            Debug.Log($"[TutorialService] Skipping sequence: {_currentSequence.SequenceId}");
            CompleteSequence();
        }

        public void MarkSequenceCompleted(string sequenceId)
        {
            _completedSequences.Add(sequenceId);
            SaveCompletedSequences();
            Debug.Log($"[TutorialService] Marked complete: {sequenceId}");
        }

        public bool IsSequenceCompleted(string sequenceId)
        {
            return _completedSequences.Contains(sequenceId);
        }

        public void NotifyAction(TutorialAction action, string parameter = null)
        {
            if (!IsActive || !_isWaitingForAction) return;

            var currentStep = GetCurrentStep();
            if (currentStep == null) return;

            bool actionMatches = false;

            switch (currentStep.RequiredAction)
            {
                case TutorialAction.TapAnywhere:
                    actionMatches = action == TutorialAction.TapAnywhere ||
                                   action == TutorialAction.TapButton;
                    break;

                case TutorialAction.TapButton:
                    actionMatches = action == TutorialAction.TapButton &&
                                   parameter == currentStep.ActionParameter;
                    break;

                case TutorialAction.MoveTo:
                    actionMatches = action == TutorialAction.MoveTo &&
                                   parameter == currentStep.ActionParameter;
                    break;

                case TutorialAction.Interact:
                    actionMatches = action == TutorialAction.Interact ||
                                   action == TutorialAction.InteractWith;
                    break;

                case TutorialAction.InteractWith:
                    actionMatches = action == TutorialAction.InteractWith &&
                                   parameter == currentStep.ActionParameter;
                    break;

                case TutorialAction.PerformAction:
                    actionMatches = action == TutorialAction.PerformAction &&
                                   parameter == currentStep.ActionParameter;
                    break;

                case TutorialAction.Signal:
                    actionMatches = action == TutorialAction.Signal &&
                                   parameter == currentStep.ActionParameter;
                    break;

                case TutorialAction.Custom:
                    // Custom actions require explicit parameter match
                    actionMatches = action == TutorialAction.Custom &&
                                   parameter == currentStep.ActionParameter;
                    break;
            }

            if (actionMatches)
            {
                Debug.Log($"[TutorialService] Action matched: {action} ({parameter})");
                AdvanceStep();
            }
        }

        private TutorialStep GetCurrentStep()
        {
            if (!IsActive || _currentStepIndex >= _currentSequence.Steps.Count)
                return null;

            return _currentSequence.Steps[_currentStepIndex];
        }

        private void ShowCurrentStep()
        {
            var step = GetCurrentStep();
            if (step == null) return;

            // Handle game pausing
            if (step.PauseGame)
            {
                Time.timeScale = 0f;
            }

            // Set waiting for action
            _isWaitingForAction = step.RequiredAction != TutorialAction.None &&
                                  step.RequiredAction != TutorialAction.Wait;

            // Invoke callbacks
            step.OnShow?.Invoke();

            // Emit events
            OnTutorialStateChanged?.Invoke(_currentSequence.SequenceId, _currentStepIndex, false);

            _signalBus?.Publish(new TutorialStepSignal
            {
                SequenceId = _currentSequence.SequenceId,
                StepId = step.StepId,
                StepIndex = _currentStepIndex,
                TotalSteps = _currentSequence.Steps.Count,
                IsComplete = false
            });

            _signalBus?.Publish(new TutorialUIRequestSignal
            {
                Step = step,
                ShouldShow = true
            });

            Debug.Log($"[TutorialService] Showing step {_currentStepIndex + 1}/{_currentSequence.Steps.Count}: {step.StepId}");
        }

        private void CompleteSequence()
        {
            var sequenceId = _currentSequence.SequenceId;
            var onComplete = _currentSequence.OnComplete;

            // Resume game time
            Time.timeScale = 1f;

            // Hide tutorial UI
            _signalBus?.Publish(new TutorialUIRequestSignal
            {
                Step = null,
                ShouldShow = false
            });

            // Mark as completed
            MarkSequenceCompleted(sequenceId);

            // Clear current sequence
            _currentSequence = null;
            _currentStepIndex = 0;

            // Emit events
            OnTutorialStateChanged?.Invoke(sequenceId, -1, true);

            _signalBus?.Publish(new TutorialStepSignal
            {
                SequenceId = sequenceId,
                StepId = null,
                StepIndex = -1,
                TotalSteps = 0,
                IsComplete = true
            });

            // Invoke completion callback
            onComplete?.Invoke();

            Debug.Log($"[TutorialService] Sequence complete: {sequenceId}");
        }

        private void LoadCompletedSequences()
        {
            var completed = PlayerPrefs.GetString("tutorial_completed", "");
            if (!string.IsNullOrEmpty(completed))
            {
                var ids = completed.Split(',');
                foreach (var id in ids)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        _completedSequences.Add(id);
                    }
                }
            }
            Debug.Log($"[TutorialService] Loaded {_completedSequences.Count} completed sequences");
        }

        private void SaveCompletedSequences()
        {
            var completed = string.Join(",", _completedSequences);
            PlayerPrefs.SetString("tutorial_completed", completed);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Reset all tutorial progress (for testing).
        /// </summary>
        public void ResetAllProgress()
        {
            _completedSequences.Clear();
            PlayerPrefs.DeleteKey("tutorial_completed");
            PlayerPrefs.Save();
            Debug.Log("[TutorialService] Reset all tutorial progress");
        }
    }
}
