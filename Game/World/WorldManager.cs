using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace AISandbox.Game.World
{
    public enum BlockType
    {
        Air,
        Stone,
        Dirt,
        Grass,
        Wood,
        Leaves
    }

    public class Block
    {
        public BlockType BlockType { get; set; }
        public Vector3 Position { get; set; }
        public bool IsActive { get; set; }

        public Block(Vector3 position, BlockType blockType)
        {
            Position = position;
            BlockType = blockType;
            IsActive = blockType != BlockType.Air;
        }
    }

    public class WorldManager
    {
        private Dictionary<Vector3, Block> blocks;
        private int width, height, depth;

        public WorldManager(int w, int h, int d)
        {
            width = w;
            height = h;
            depth = d;
            blocks = new Dictionary<Vector3, Block>();
            GenerateWorld();
        }

        private void GenerateWorld()
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < 5; y++)
                    {
                        var pos = new Vector3(x, y, z);
                        blocks[pos] = new Block(pos, BlockType.Stone);
                    }

                    for (int y = 5; y < 8; y++)
                    {
                        var pos = new Vector3(x, y, z);
                        blocks[pos] = new Block(pos, BlockType.Dirt);
                    }

                    var grassPos = new Vector3(x, 8, z);
                    blocks[grassPos] = new Block(grassPos, BlockType.Grass);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                int x = 5 + i * 4;
                int z = 5 + (i % 3) * 3;
                int y = 9;

                if (x < width && z < depth)
                {
                    for (int ty = 0; ty < 4; ty++)
                    {
                        var pos = new Vector3(x, y + ty, z);
                        blocks[pos] = new Block(pos, BlockType.Wood);
                    }

                    for (int dx = -2; dx <= 2; dx++)
                        for (int dz = -2; dz <= 2; dz++)
                            for (int dy = 0; dy < 3; dy++)
                            {
                                var pos = new Vector3(x + dx, y + 4 + dy, z + dz);
                                if (!blocks.ContainsKey(pos))
                                    blocks[pos] = new Block(pos, BlockType.Leaves);
                            }
                }
            }
        }

        public Block GetBlock(Vector3 position)
        {
            if (blocks.TryGetValue(position, out var block))
                return block;
            return new Block(position, BlockType.Air);
        }

        public void SetBlock(Vector3 position, BlockType blockType)
        {
            if (blockType == BlockType.Air)
            {
                blocks.Remove(position);
            }
            else
            {
                blocks[position] = new Block(position, blockType);
            }
        }

        public void Draw(GraphicsDevice device)
        {
            // Simple block rendering
        }
    }
}
