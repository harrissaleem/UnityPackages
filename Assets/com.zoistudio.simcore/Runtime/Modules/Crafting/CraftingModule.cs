// SimCore - Crafting Module
// Production and crafting system

using System;
using System.Collections.Generic;
using SimCore.Effects;
using SimCore.Modules;
using SimCore.Signals;

namespace SimCore.Modules.Crafting
{
    /// <summary>
    /// Recipe definition
    /// </summary>
    [Serializable]
    public class RecipeDef
    {
        public ContentId Id;
        public string DisplayName;
        public Dictionary<ContentId, int> Ingredients = new();
        public Dictionary<ContentId, int> Results = new();
        public float CraftTime; // Seconds
        public List<Condition> Conditions = new(); // Requirements to craft
    }
    
    /// <summary>
    /// Active crafting operation
    /// </summary>
    public class CraftingOperation
    {
        public SimId CrafterId;
        public ContentId RecipeId;
        public float StartTime;
        public float Duration;
        public bool IsComplete;
    }
    
    /// <summary>
    /// Crafting started signal
    /// </summary>
    public struct CraftingStartedSignal : ISignal
    {
        public SimId CrafterId;
        public ContentId RecipeId;
        public float Duration;
    }
    
    /// <summary>
    /// Crafting completed signal
    /// </summary>
    public struct CraftingCompletedSignal : ISignal
    {
        public SimId CrafterId;
        public ContentId RecipeId;
    }
    
    /// <summary>
    /// Crafting module implementation
    /// </summary>
    public class CraftingModule : ICraftingModule
    {
        private readonly Dictionary<ContentId, RecipeDef> _recipes = new();
        private readonly List<CraftingOperation> _operations = new();
        private SignalBus _signalBus;
        private SimWorld _world;
        
        public CraftingModule() { }
        
        #region ISimModule
        
        public void Initialize(SimWorld world)
        {
            _world = world;
            _signalBus = world.SignalBus;
        }
        
        public void Tick(float deltaTime)
        {
            Tick(_world, deltaTime);
        }
        
        public void Shutdown()
        {
            _recipes.Clear();
            _operations.Clear();
        }
        
        #endregion
        
        /// <summary>
        /// Register a recipe
        /// </summary>
        public void RegisterRecipe(RecipeDef recipe)
        {
            _recipes[recipe.Id] = recipe;
        }
        
        public bool CanCraft(SimId crafterId, ContentId recipeId)
        {
            if (!_recipes.TryGetValue(recipeId, out var recipe))
                return false;
            
            var world = _world;
            var inv = world.Inventories.GetInventory(crafterId);
            if (inv == null) return false;
            
            // Check ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                if (!inv.HasItem(ingredient.Key, ingredient.Value))
                    return false;
            }
            
            // Check conditions
            var condCtx = new ConditionContext(world) { ActorId = crafterId };
            foreach (var cond in recipe.Conditions)
            {
                if (!cond.Evaluate(condCtx))
                    return false;
            }
            
            return true;
        }
        
        public bool StartCrafting(SimId crafterId, ContentId recipeId)
        {
            if (!CanCraft(crafterId, recipeId))
                return false;
            
            var recipe = _recipes[recipeId];
            var world = _world;
            var inv = world.Inventories.GetInventory(crafterId);
            
            // Consume ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                inv.RemoveItem(ingredient.Key, ingredient.Value);
            }
            
            // Start operation
            var operation = new CraftingOperation
            {
                CrafterId = crafterId,
                RecipeId = recipeId,
                StartTime = world.CurrentTime.Seconds,
                Duration = recipe.CraftTime
            };
            _operations.Add(operation);
            
            _signalBus?.Publish(new CraftingStartedSignal
            {
                CrafterId = crafterId,
                RecipeId = recipeId,
                Duration = recipe.CraftTime
            });
            
            // If instant craft
            if (recipe.CraftTime <= 0)
            {
                CompleteCrafting(operation);
            }
            
            return true;
        }
        
        public float GetProgress(SimId crafterId, ContentId recipeId)
        {
            foreach (var op in _operations)
            {
                if (op.CrafterId == crafterId && op.RecipeId == recipeId && !op.IsComplete)
                {
                    var world = _world;
                    float elapsed = world.CurrentTime.Seconds - op.StartTime;
                    return Math.Clamp(elapsed / op.Duration, 0f, 1f);
                }
            }
            return 0f;
        }
        
        public void CancelCrafting(SimId crafterId, ContentId recipeId)
        {
            for (int i = _operations.Count - 1; i >= 0; i--)
            {
                var op = _operations[i];
                if (op.CrafterId == crafterId && op.RecipeId == recipeId && !op.IsComplete)
                {
                    // Refund ingredients (partial)
                    var recipe = _recipes[recipeId];
                    var world = _world;
                    var inv = world.Inventories.GetInventory(crafterId);
                    
                    float progress = GetProgress(crafterId, recipeId);
                    float refundRate = 1f - progress;
                    
                    foreach (var ingredient in recipe.Ingredients)
                    {
                        int refund = (int)(ingredient.Value * refundRate);
                        if (refund > 0)
                        {
                            inv.AddItem(ingredient.Key, refund);
                        }
                    }
                    
                    _operations.RemoveAt(i);
                    break;
                }
            }
        }
        
        public void Tick(SimWorld world, float deltaTime)
        {
            var completed = new List<CraftingOperation>();
            
            foreach (var op in _operations)
            {
                if (op.IsComplete) continue;
                
                float elapsed = world.CurrentTime.Seconds - op.StartTime;
                if (elapsed >= op.Duration)
                {
                    completed.Add(op);
                }
            }
            
            foreach (var op in completed)
            {
                CompleteCrafting(op);
            }
            
            // Remove completed operations
            _operations.RemoveAll(op => op.IsComplete);
        }
        
        private void CompleteCrafting(CraftingOperation operation)
        {
            operation.IsComplete = true;
            
            var recipe = _recipes[operation.RecipeId];
            var world = _world;
            var inv = world.Inventories.GetOrCreateInventory(operation.CrafterId);
            
            // Give results
            foreach (var result in recipe.Results)
            {
                inv.AddItem(result.Key, result.Value);
            }
            
            _signalBus?.Publish(new CraftingCompletedSignal
            {
                CrafterId = operation.CrafterId,
                RecipeId = operation.RecipeId
            });
        }
        
        /// <summary>
        /// Get all active operations for a crafter
        /// </summary>
        public IEnumerable<CraftingOperation> GetActiveOperations(SimId crafterId)
        {
            foreach (var op in _operations)
            {
                if (op.CrafterId == crafterId && !op.IsComplete)
                    yield return op;
            }
        }
    }
}

