// SimCore - Economy Module
// Money and pricing system

using System;
using System.Collections.Generic;
using SimCore.Modules;
using SimCore.Signals;

namespace SimCore.Modules.Economy
{
    /// <summary>
    /// Money changed signal
    /// </summary>
    public struct MoneyChangedSignal : ISignal
    {
        public SimId EntityId;
        public float OldAmount;
        public float NewAmount;
        public float Delta;
    }
    
    /// <summary>
    /// Purchase signal
    /// </summary>
    public struct PurchaseSignal : ISignal
    {
        public SimId BuyerId;
        public SimId SellerId;
        public ContentId ItemId;
        public int Quantity;
        public float TotalPrice;
    }
    
    /// <summary>
    /// Economy module implementation
    /// </summary>
    public class EconomyModule : IEconomyModule
    {
        private readonly Dictionary<SimId, float> _money = new();
        private readonly Dictionary<ContentId, float> _basePrices = new();
        private readonly Dictionary<ContentId, float> _currentPrices = new();
        private SignalBus _signalBus;
        private SimWorld _world;
        
        public EconomyModule() { }
        
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
            _money.Clear();
            _basePrices.Clear();
            _currentPrices.Clear();
        }
        
        #endregion
        
        /// <summary>
        /// Set base price for an item
        /// </summary>
        public void SetBasePrice(ContentId itemId, float price)
        {
            _basePrices[itemId] = price;
            _currentPrices[itemId] = price;
        }
        
        public float GetMoney(SimId entityId)
        {
            return _money.TryGetValue(entityId, out var amount) ? amount : 0f;
        }
        
        public void AddMoney(SimId entityId, float amount)
        {
            float oldAmount = GetMoney(entityId);
            float newAmount = oldAmount + amount;
            _money[entityId] = newAmount;
            
            _signalBus?.Publish(new MoneyChangedSignal
            {
                EntityId = entityId,
                OldAmount = oldAmount,
                NewAmount = newAmount,
                Delta = amount
            });
        }
        
        public bool RemoveMoney(SimId entityId, float amount)
        {
            float current = GetMoney(entityId);
            if (current < amount) return false;
            
            _money[entityId] = current - amount;
            
            _signalBus?.Publish(new MoneyChangedSignal
            {
                EntityId = entityId,
                OldAmount = current,
                NewAmount = current - amount,
                Delta = -amount
            });
            
            return true;
        }
        
        public bool Transfer(SimId from, SimId to, float amount)
        {
            if (GetMoney(from) < amount) return false;
            
            RemoveMoney(from, amount);
            AddMoney(to, amount);
            return true;
        }
        
        public float GetPrice(ContentId itemId)
        {
            return _currentPrices.TryGetValue(itemId, out var price) ? price : 0f;
        }
        
        /// <summary>
        /// Buy item from seller
        /// </summary>
        public bool BuyItem(SimWorld world, SimId buyerId, SimId sellerId, ContentId itemId, int quantity = 1)
        {
            float totalPrice = GetPrice(itemId) * quantity;
            
            if (!RemoveMoney(buyerId, totalPrice))
                return false;
            
            AddMoney(sellerId, totalPrice);
            
            // Transfer item
            var sellerInv = world.Inventories.GetInventory(sellerId);
            var buyerInv = world.Inventories.GetOrCreateInventory(buyerId);
            
            if (sellerInv != null && sellerInv.HasItem(itemId, quantity))
            {
                sellerInv.RemoveItem(itemId, quantity);
                buyerInv.AddItem(itemId, quantity);
            }
            else
            {
                // Seller doesn't have item, just give to buyer (unlimited stock)
                buyerInv.AddItem(itemId, quantity);
            }
            
            _signalBus?.Publish(new PurchaseSignal
            {
                BuyerId = buyerId,
                SellerId = sellerId,
                ItemId = itemId,
                Quantity = quantity,
                TotalPrice = totalPrice
            });
            
            return true;
        }
        
        /// <summary>
        /// Sell item to buyer
        /// </summary>
        public bool SellItem(SimWorld world, SimId sellerId, SimId buyerId, ContentId itemId, int quantity = 1)
        {
            var sellerInv = world.Inventories.GetInventory(sellerId);
            if (sellerInv == null || !sellerInv.HasItem(itemId, quantity))
                return false;
            
            float totalPrice = GetPrice(itemId) * quantity * 0.5f; // Sell for half price
            
            AddMoney(sellerId, totalPrice);
            RemoveMoney(buyerId, totalPrice);
            
            sellerInv.RemoveItem(itemId, quantity);
            
            var buyerInv = world.Inventories.GetOrCreateInventory(buyerId);
            buyerInv.AddItem(itemId, quantity);
            
            return true;
        }
        
        public void Tick(SimWorld world, float deltaTime)
        {
            // Could implement price fluctuations here
        }
        
        /// <summary>
        /// Create snapshot for persistence
        /// </summary>
        public Dictionary<string, float> CreateMoneySnapshot()
        {
            var snapshot = new Dictionary<string, float>();
            foreach (var kvp in _money)
            {
                snapshot[kvp.Key.Value.ToString()] = kvp.Value;
            }
            return snapshot;
        }
        
        /// <summary>
        /// Restore from snapshot
        /// </summary>
        public void RestoreMoneyFromSnapshot(Dictionary<string, float> snapshot)
        {
            _money.Clear();
            foreach (var kvp in snapshot)
            {
                if (int.TryParse(kvp.Key, out var id))
                {
                    _money[new SimId(id)] = kvp.Value;
                }
            }
        }
    }
}

