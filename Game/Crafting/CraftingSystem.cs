using System.Collections.Generic;
using AISandbox.Game.Inventory;

namespace AISandbox.Game.Crafting
{
    public sealed class CraftRecipe
    {
        public ItemType OutputItem { get; }
        public int OutputCount { get; }
        public Dictionary<ItemType, int> Requirements { get; }

        public CraftRecipe(ItemType output, int count, Dictionary<ItemType, int> requirements)
        {
            OutputItem = output;
            OutputCount = count;
            Requirements = requirements;
        }
    }

    public sealed class CraftingSystem
    {
        private readonly List<CraftRecipe> recipes = new()
        {
            new CraftRecipe(ItemType.WoodPlank, 4, new() { [ItemType.Log] = 1 }),
            new CraftRecipe(ItemType.Stick, 2, new() { [ItemType.WoodPlank] = 1 }),
            new CraftRecipe(ItemType.WoodenAxe, 1, new() { [ItemType.Log] = 2, [ItemType.Stick] = 3 }),
            new CraftRecipe(ItemType.Pickaxe, 1, new() { [ItemType.Stone] = 3, [ItemType.Stick] = 3 })
        };

        public List<CraftRecipe> GetAllRecipes() => new(recipes);
        public CraftRecipe GetRecipe(ItemType item) => recipes.Find(r => r.OutputItem == item);

        public bool CanCraft(ToolInventory inventory, CraftRecipe recipe)
        {
            if (recipe == null) return false;
            foreach (var requirement in recipe.Requirements)
                if (inventory.GetItemCount(requirement.Key) < requirement.Value) return false;
            return true;
        }

        public bool Craft(ToolInventory inventory, CraftRecipe recipe)
        {
            if (!CanCraft(inventory, recipe)) return false;
            foreach (var requirement in recipe.Requirements) inventory.RemoveItem(requirement.Key, requirement.Value);
            inventory.AddItem(recipe.OutputItem, recipe.OutputCount);
            return true;
        }
    }
}
