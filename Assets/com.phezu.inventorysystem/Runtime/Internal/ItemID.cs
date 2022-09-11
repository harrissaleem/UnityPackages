namespace Phezu.InventorySystem.Internal
{
    [System.Serializable]
    public class ItemID
    {
        public int itemID;

        public static implicit operator int(ItemID itemID)
        {
            return itemID.itemID;
        }

        public static implicit operator ItemID(int itemID)
        {
            ItemID ans = new();
            ans.itemID = itemID;
            return ans;
        }
    }
}