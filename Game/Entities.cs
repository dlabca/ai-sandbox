using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AISandbox
{
    public enum Item { Stick = 0, Stone = 1, Bark = 2, CopperOre = 3, IronOre = 4, Copper = 5, Iron = 6 }
    public enum OreType { Stone, Copper, Iron }
    public enum MachineType { Smelter = 0, Debarker = 1, Generator = 2 }
    public enum TreeState { Standing, Falling, Stump }
    public enum ToolKind { Hand, Axe, Pickaxe }

    public static class ItemInfo
    {
        public static string Name(Item i)
        {
            switch (i)
            {
                case Item.Stick: return "STICK";
                case Item.Stone: return "STONE";
                case Item.Bark: return "BARK";
                case Item.CopperOre: return "COPPER ORE";
                case Item.IronOre: return "IRON ORE";
                case Item.Copper: return "COPPER";
                case Item.Iron: return "IRON";
                default: return "?";
            }
        }

        public static float FuelSeconds(Item i)
        {
            if (i == Item.Bark) return 30f;
            if (i == Item.Stick) return 20f;
            return 0f;
        }
    }

    public sealed class Tool
    {
        public readonly string Name;
        public readonly string Label;
        public readonly ToolKind Kind;
        public readonly float Power;

        public Tool(string name, string label, ToolKind kind, float power)
        {
            Name = name;
            Label = label;
            Kind = kind;
            Power = power;
        }
    }

    public static class ToolDefs
    {
        public static readonly Tool Hand = new Tool("HAND", "HAND", ToolKind.Hand, 0f);
        public static readonly Tool StoneAxe = new Tool("STONE AXE", "S AXE", ToolKind.Axe, 1f);
        public static readonly Tool StonePickaxe = new Tool("STONE PICKAXE", "S PICK", ToolKind.Pickaxe, 1f);
        public static readonly Tool IronAxe = new Tool("IRON AXE", "I AXE", ToolKind.Axe, 2.2f);
        public static readonly Tool IronPickaxe = new Tool("IRON PICKAXE", "I PICK", ToolKind.Pickaxe, 2.2f);
    }

    public sealed class Tree
    {
        public Vector3 Pos;
        public float Height;
        public int Variant;
        public int Seed;
        public float Hp = 6f;
        public const float MaxHp = 6f;
        public TreeState State = TreeState.Standing;
        public float FallT;
        public Vector3 FallDir;

        public float TrunkHeight
        {
            get { return Height * 0.6f; }
        }
    }

    public sealed class OreNode
    {
        public Vector3 Pos;
        public OreType Type;
        public float Radius;
        public int Seed;
        public float Hp = 5f;
        public const float MaxHp = 5f;
        public bool Alive = true;

        public string Name
        {
            get { return Type == OreType.Copper ? "COPPER ORE" : (Type == OreType.Iron ? "IRON ORE" : "STONE"); }
        }
    }

    public sealed class Pickup
    {
        public Item Type;
        public Vector3 Pos;
        public int Seed;
        public bool Alive = true;
    }

    /// <summary>Skutecna klada lezici ve svete - da se zvednout, nest, polozit nebo vlozit do stroje.</summary>
    public sealed class LogItem
    {
        public Vector3 Pos;
        public float Yaw;
        public bool Peeled;
        public float VelY;
        public bool Settled;
        public const float Length = 2.6f;
        public const float Radius = 0.24f;

        public Vector3 Dir
        {
            get { return new Vector3(MathF.Sin(Yaw), 0f, MathF.Cos(Yaw)); }
        }
    }

    public sealed class Machine
    {
        public MachineType Type;
        public Vector3 Pos;
        public float Yaw;
        public float Fuel;
        public float Progress;
        public bool Running;
        public bool Powered;
        public float Anim;

        // pec
        public int OreCount;
        public Item OreKind = Item.IronOre;
        public int OutCount;
        public Item OutKind = Item.Iron;

        // odkurovac
        public int Queue;
        public int OutPeeled;
        public int OutBark;

        public float Radius
        {
            get
            {
                switch (Type)
                {
                    case MachineType.Smelter: return 0.85f;
                    case MachineType.Generator: return 0.9f;
                    default: return 1.1f;
                }
            }
        }

        public string Name
        {
            get
            {
                switch (Type)
                {
                    case MachineType.Smelter: return "SMELTER";
                    case MachineType.Generator: return "GENERATOR";
                    default: return "DEBARKER";
                }
            }
        }
    }

    public enum TargetKind { None, Tree, Ore, Pickup, Log, Machine }

    public sealed class Target
    {
        public TargetKind Kind;
        public Tree Tree;
        public OreNode Ore;
        public Pickup Pickup;
        public LogItem Log;
        public Machine Machine;
        public float Dist;

        public void Clear(float reach)
        {
            Kind = TargetKind.None;
            Tree = null;
            Ore = null;
            Pickup = null;
            Log = null;
            Machine = null;
            Dist = reach;
        }
    }

    // ------------------------------------------------------------------ inventar a recepty

    public sealed class Cost
    {
        public readonly Item Item;
        public readonly int Count;

        public Cost(Item item, int count)
        {
            Item = item;
            Count = count;
        }
    }

    public sealed class Recipe
    {
        public readonly string Name;
        public readonly Cost[] Costs;
        public readonly Tool ResultTool;
        public readonly int ResultMachine;

        public Recipe(string name, Tool tool, Cost[] costs)
        {
            Name = name;
            ResultTool = tool;
            ResultMachine = -1;
            Costs = costs;
        }

        public Recipe(string name, MachineType machine, Cost[] costs)
        {
            Name = name;
            ResultTool = null;
            ResultMachine = (int)machine;
            Costs = costs;
        }

        public string CostText
        {
            get
            {
                string s = "";
                for (int i = 0; i < Costs.Length; i++)
                {
                    if (i > 0) s += "  ";
                    s += Costs[i].Count + " " + ItemInfo.Name(Costs[i].Item);
                }
                return s;
            }
        }
    }

    public static class Recipes
    {
        public static readonly Recipe[] All =
        {
            new Recipe("STONE AXE", ToolDefs.StoneAxe, new[] { new Cost(Item.Stick, 2), new Cost(Item.Stone, 2) }),
            new Recipe("STONE PICKAXE", ToolDefs.StonePickaxe, new[] { new Cost(Item.Stick, 2), new Cost(Item.Stone, 3) }),
            new Recipe("SMELTER", MachineType.Smelter, new[] { new Cost(Item.Stone, 8) }),
            new Recipe("IRON AXE", ToolDefs.IronAxe, new[] { new Cost(Item.Iron, 3), new Cost(Item.Stick, 2) }),
            new Recipe("IRON PICKAXE", ToolDefs.IronPickaxe, new[] { new Cost(Item.Iron, 3), new Cost(Item.Stick, 2) }),
            new Recipe("DEBARKER", MachineType.Debarker, new[] { new Cost(Item.Iron, 5), new Cost(Item.Copper, 2) }),
            new Recipe("GENERATOR", MachineType.Generator, new[] { new Cost(Item.Iron, 4), new Cost(Item.Copper, 6) })
        };
    }

    public sealed class Inventory
    {
        public const int CarryMax = 4;

        private readonly int[] counts = new int[8];

        public readonly List<Tool> Tools = new List<Tool>();
        public int SelectedSlot;
        public int Logs;          // neodkorene kladdy v naruci
        public int Peeled;        // odkorene kladdy v naruci
        public bool UsePeeled;    // jaky material se pouziva na stavbu
        public readonly int[] MachineItems = new int[3];

        public Inventory()
        {
            Tools.Add(ToolDefs.Hand);
        }

        public Tool SelectedTool
        {
            get
            {
                if (SelectedSlot < 0 || SelectedSlot >= Tools.Count) SelectedSlot = 0;
                return Tools[SelectedSlot];
            }
        }

        public int Carry
        {
            get { return Logs + Peeled; }
        }

        public int Count(Item i)
        {
            return counts[(int)i];
        }

        public void Add(Item i, int n)
        {
            counts[(int)i] += n;
        }

        public bool Remove(Item i, int n)
        {
            if (counts[(int)i] < n) return false;
            counts[(int)i] -= n;
            return true;
        }

        public bool HasTool(Tool t)
        {
            return Tools.Contains(t);
        }

        public bool CanAfford(Recipe r)
        {
            for (int i = 0; i < r.Costs.Length; i++)
                if (Count(r.Costs[i].Item) < r.Costs[i].Count) return false;
            return true;
        }

        public void Pay(Recipe r)
        {
            for (int i = 0; i < r.Costs.Length; i++)
                counts[(int)r.Costs[i].Item] -= r.Costs[i].Count;
        }
    }
}
