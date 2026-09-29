using System.Collections.Generic;

namespace AISandbox.Game.Inventory
{
    public enum ItemType { Log, WoodPlank, Stick, WoodenAxe, Stone, Pickaxe }

    public class ToolInventory
    {
        private readonly Dictionary<ItemType, int> items = new();
        public const int MaxSlots = 9;

        public ToolInventory()
        {
            foreach (ItemType item in System.Enum.GetValues(typeof(ItemType))) items[item] = 0;
        }

        public void AddItem(ItemType itemType, int count) => items[itemType] = GetItemCount(itemType) + count;
        public void RemoveItem(ItemType itemType, int count) => items[itemType] = System.Math.Max(0, GetItemCount(itemType) - count);
        public int GetItemCount(ItemType itemType) => items.TryGetValue(itemType, out int count) ? count : 0;
        public Dictionary<ItemType, int> GetAllItems() => new(items);
    }
}
