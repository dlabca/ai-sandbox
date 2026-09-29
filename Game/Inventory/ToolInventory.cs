using System.Collections.Generic;

namespace AISandbox.Game.Inventory
{
    public enum ItemType
    {
        Log,
        WoodPlank,
        Stick,
        WoodenAxe,
        Stone,
        Pickaxe
    }

    public class ToolInventory
    {
        private Dictionary<ItemType, int> items;
        public const int MaxSlots = 9;

        public ToolInventory()
        {
            items = new Dictionary<ItemType, int>();
            foreach (ItemType item in System.Enum.GetValues(typeof(ItemType)))
            {
                items[item] = 0;
            }
        }

        public void AddItem(ItemType itemType, int count)
        {
            if (items.ContainsKey(itemType))
                items[itemType] += count;
            else
                items[itemType] = count;
        }

        public void RemoveItem(ItemType itemType, int count)
        {
            if (items.ContainsKey(itemType))
            {
                items[itemType] -= count;
                if (items[itemType] < 0)
                    items[itemType] = 0;
            }
        }

        public int GetItemCount(ItemType itemType)
        {
            return items.ContainsKey(itemType) ? items[itemType] : 0;
        }

        public Dictionary<ItemType, int> GetAllItems()
        {
            return new Dictionary<ItemType, int>(items);
        }
    }
}
