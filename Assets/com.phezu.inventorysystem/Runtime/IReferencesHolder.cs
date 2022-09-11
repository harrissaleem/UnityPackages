using UnityEngine;

namespace Phezu.InventorySystem
{
    public interface IReferencesHolder
    {
        /// <summary>
        /// RectTransform of the parent of all the slots of your inventory
        /// </summary>
        public RectTransform SlotsHolder { get; }

        /// <summary>
        /// Parent of the entire inventory UI
        /// </summary>
        public Transform ParentUI { get; }

        /// <summary>
        /// Transform of the player
        /// </summary>
        public Transform Player { get; }
    }
}