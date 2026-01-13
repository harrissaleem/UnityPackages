// SimCore - Dialogue Module
// Supports both node-based and intent-based dialogue

using System;
using System.Collections.Generic;
using SimCore.Effects;
using SimCore.Modules;
using SimCore.Signals;

namespace SimCore.Modules.Dialogue
{
    /// <summary>
    /// Dialogue node for node-based dialogue
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        public ContentId NodeId;
        public string SpeakerName;
        public string Text;
        public List<DialogueChoice> Choices = new();
        public List<Effect> Effects = new(); // Effects when reaching this node
        public ContentId NextNodeId; // For linear progression
    }
    
    /// <summary>
    /// Choice in a dialogue node
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        public string Text;
        public ContentId NextNodeId;
        public List<Condition> Conditions = new(); // Show only if conditions met
        public List<Effect> Effects = new(); // Effects when chosen
    }
    
    /// <summary>
    /// Dialogue definition
    /// </summary>
    [Serializable]
    public class DialogueDef
    {
        public ContentId Id;
        public string DisplayName;
        public ContentId StartNodeId;
        public List<DialogueNode> Nodes = new();
        public DialogueStyle Style = DialogueStyle.NodeBased;
        
        public DialogueNode GetNode(ContentId nodeId)
        {
            foreach (var node in Nodes)
            {
                if (node.NodeId == nodeId)
                    return node;
            }
            return null;
        }
    }
    
    public enum DialogueStyle
    {
        NodeBased,  // Traditional branching dialogue (horror, RPG)
        IntentBased // Context-sensitive responses (farm, casual)
    }
    
    /// <summary>
    /// Active dialogue session
    /// </summary>
    public class DialogueSession
    {
        public ContentId DialogueId;
        public SimId SpeakerId;
        public DialogueNode CurrentNode;
        public List<DialogueChoice> AvailableChoices = new();
    }
    
    /// <summary>
    /// Dialogue module implementation
    /// </summary>
    public class DialogueModule : IDialogueModule
    {
        private readonly Dictionary<ContentId, DialogueDef> _dialogues = new();
        private SignalBus _signalBus;
        private SimWorld _world;
        private DialogueSession _currentSession;
        
        public bool IsDialogueActive => _currentSession != null;
        public SimId CurrentSpeaker => _currentSession?.SpeakerId ?? SimId.Invalid;
        public ContentId CurrentDialogueId => _currentSession?.DialogueId ?? ContentId.Invalid;
        
        public DialogueModule() { }
        
        #region ISimModule
        
        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
        }
        
        public void Tick(float deltaTime)
        {
            // Dialogue is event-driven, no tick needed
        }
        
        public void Shutdown()
        {
            EndDialogue();
            _dialogues.Clear();
        }
        
        #endregion
        
        /// <summary>
        /// Register a dialogue definition
        /// </summary>
        public void RegisterDialogue(DialogueDef dialogue)
        {
            _dialogues[dialogue.Id] = dialogue;
        }
        
        public void StartDialogue(SimId speakerId, ContentId dialogueId)
        {
            if (!_dialogues.TryGetValue(dialogueId, out var def))
            {
                SimCoreLogger.LogWarning($"Dialogue not found: {dialogueId}");
                return;
            }
            
            var startNode = def.GetNode(def.StartNodeId);
            if (startNode == null)
            {
                SimCoreLogger.LogWarning($"Start node not found in dialogue: {dialogueId}");
                return;
            }
            
            _currentSession = new DialogueSession
            {
                DialogueId = dialogueId,
                SpeakerId = speakerId,
                CurrentNode = startNode
            };
            
            // Apply node effects
            ApplyNodeEffects(startNode);
            
            // Update available choices
            UpdateAvailableChoices();
            
            _signalBus.Publish(new DialogueStartedSignal
            {
                SpeakerId = speakerId,
                DialogueId = dialogueId
            });
            
            EmitCurrentLine();
        }
        
        public void SelectChoice(int choiceIndex)
        {
            if (_currentSession == null) return;
            if (choiceIndex < 0 || choiceIndex >= _currentSession.AvailableChoices.Count) return;
            
            var choice = _currentSession.AvailableChoices[choiceIndex];
            
            // Apply choice effects
            var world = _world;
            var effectCtx = new EffectContext(world)
            {
                ActorId = _currentSession.SpeakerId,
                TargetId = _currentSession.SpeakerId
            };
            
            foreach (var effect in choice.Effects)
            {
                effect.Apply(effectCtx);
            }
            
            // Navigate to next node
            if (choice.NextNodeId.IsValid)
            {
                NavigateToNode(choice.NextNodeId);
            }
            else
            {
                EndDialogue();
            }
        }
        
        public void Advance()
        {
            if (_currentSession == null) return;
            
            var currentNode = _currentSession.CurrentNode;
            
            // If no choices and has next node, advance
            if (currentNode.Choices.Count == 0 && currentNode.NextNodeId.IsValid)
            {
                NavigateToNode(currentNode.NextNodeId);
            }
            else if (currentNode.Choices.Count == 0)
            {
                EndDialogue();
            }
            // Otherwise, wait for choice selection
        }
        
        public void EndDialogue()
        {
            if (_currentSession == null) return;
            
            var dialogueId = _currentSession.DialogueId;
            var speakerId = _currentSession.SpeakerId;
            
            _currentSession = null;
            
            _signalBus.Publish(new DialogueEndedSignal
            {
                SpeakerId = speakerId,
                DialogueId = dialogueId
            });
        }
        
        /// <summary>
        /// Get current line text
        /// </summary>
        public string GetCurrentText()
        {
            return _currentSession?.CurrentNode?.Text;
        }
        
        /// <summary>
        /// Get available choice texts
        /// </summary>
        public List<string> GetChoiceTexts()
        {
            if (_currentSession == null) return new List<string>();
            
            var texts = new List<string>();
            foreach (var choice in _currentSession.AvailableChoices)
            {
                texts.Add(choice.Text);
            }
            return texts;
        }
        
        private void NavigateToNode(ContentId nodeId)
        {
            var def = _dialogues[_currentSession.DialogueId];
            var node = def.GetNode(nodeId);
            
            if (node == null)
            {
                SimCoreLogger.LogWarning($"Node not found: {nodeId}");
                EndDialogue();
                return;
            }
            
            _currentSession.CurrentNode = node;
            ApplyNodeEffects(node);
            UpdateAvailableChoices();
            EmitCurrentLine();
        }
        
        private void ApplyNodeEffects(DialogueNode node)
        {
            var world = _world;
            var effectCtx = new EffectContext(world)
            {
                ActorId = _currentSession.SpeakerId,
                TargetId = _currentSession.SpeakerId
            };
            
            foreach (var effect in node.Effects)
            {
                effect.Apply(effectCtx);
            }
        }
        
        private void UpdateAvailableChoices()
        {
            _currentSession.AvailableChoices.Clear();
            
            var world = _world;
            var condCtx = new ConditionContext(world)
            {
                ActorId = _currentSession.SpeakerId,
                TargetId = _currentSession.SpeakerId
            };
            
            foreach (var choice in _currentSession.CurrentNode.Choices)
            {
                bool available = true;
                foreach (var cond in choice.Conditions)
                {
                    if (!cond.Evaluate(condCtx))
                    {
                        available = false;
                        break;
                    }
                }
                
                if (available)
                {
                    _currentSession.AvailableChoices.Add(choice);
                }
            }
        }
        
        private void EmitCurrentLine()
        {
            var node = _currentSession.CurrentNode;
            var choices = new string[_currentSession.AvailableChoices.Count];
            for (int i = 0; i < choices.Length; i++)
            {
                choices[i] = _currentSession.AvailableChoices[i].Text;
            }
            
            _signalBus.Publish(new DialogueLineSignal
            {
                SpeakerId = _currentSession.SpeakerId,
                Text = node.Text,
                Choices = choices
            });
        }
    }
}

