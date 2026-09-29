using System.Collections.Generic;
using AISandbox.Game.Inventory;

namespace AISandbox.Game.Crafting
{
    public class CraftRecipe
    {
        public ItemType OutputItem { get; set; }
        public int OutputCount { get; set; }
        public Dictionary<ItemType, int> Requirements { get; set; }

        public CraftRecipe(ItemType output, int count, Dictionary<ItemType, int> requirements)
        {
            OutputItem = output;
            OutputCount = count;
            Requirements = requirements;
        }
    }

    public class CraftingSystem
    {
        private List<CraftRecipe> recipes;

        public CraftingSystem()
        {
            recipes = new List<CraftRecipe>();
            InitializeRecipes();
        }

        private void InitializeRecipes()
        {
            recipes.Add(new CraftRecipe(ItemType.WoodPlank, 4, new Dictionary<ItemType, int>
            {
                { ItemType.Log, 1 }
            }));

            recipes.Add(new CraftRecipe(ItemType.Stick, 2, new Dictionary<ItemType, int>
            {
                { ItemType.WoodPlank, 1 }
            }));

            recipes.Add(new CraftRecipe(ItemType.WoodenAxe, 1, new Dictionary<ItemType, int>
            {
                { ItemType.Log, 2 },
                { ItemType.Stick, 3 }
            }));

            recipes.Add(new CraftRecipe(ItemType.Pickaxe, 1, new Dictionary<ItemType, int>
            {
                { ItemType.Stone, 3 },
                { ItemType.Stick, 3 },
                { ItemType.Log, 2 }
            }));
        }

        public CraftRecipe GetRecipe(ItemType itemType)
        {
            foreach (var recipe in recipes)
            {
                if (recipe.OutputItem == itemType)
                    return recipe;
            }
            return null;
        }

        public bool CanCraft(ToolInventory inventory, CraftRecipe recipe)
        {
            if (recipe == null) return false;

            foreach (var requirement in recipe.Requirements)
            {
                if (inventory.GetItemCount(requirement.Key) < requirement.Value)
                    return false;
            }
            return true;
        }

        public void Craft(ToolInventory inventory, CraftRecipe recipe)
        {
            if (!CanCraft(inventory, recipe)) return;

            foreach (var requirement in recipe.Requirements)
            {
                inventory.RemoveItem(requirement.Key, requirement.Value);
            }

            inventory.AddItem(recipe.OutputItem, recipe.OutputCount);
        }

        public List<CraftRecipe> GetAllRecipes()
        {
            return new List<CraftRecipe>(recipes);
        }
    }
}
