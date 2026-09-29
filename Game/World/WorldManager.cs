using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;

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
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private List<VertexPositionColor> vertices;
        private List<int> indices;

        public WorldManager(int w, int h, int d)
        {
            width = w;
            height = h;
            depth = d;
            blocks = new Dictionary<Vector3, Block>();
            vertices = new List<VertexPositionColor>();
            indices = new List<int>();
            GenerateWorld();
        }

        private void GenerateWorld()
        {
            // Base terrain
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

            // Trees
            for (int i = 0; i < 12; i++)
            {
                int x = 5 + i * 4;
                int z = 5 + (i % 4) * 3;
                int y = 9;

                if (x < width && z < depth)
                {
                    // Trunk
                    for (int ty = 0; ty < 5; ty++)
                    {
                        var pos = new Vector3(x, y + ty, z);
                        blocks[pos] = new Block(pos, BlockType.Wood);
                    }

                    // Canopy/Leaves
                    for (int dx = -3; dx <= 3; dx++)
                        for (int dz = -3; dz <= 3; dz++)
                            for (int dy = 0; dy < 4; dy++)
                            {
                                var pos = new Vector3(x + dx, y + 4 + dy, z + dz);
                                if (!blocks.ContainsKey(pos) && (dx * dx + dz * dz) <= 9)
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

        public void Draw(GraphicsDevice device, BasicEffect effect)
        {
            // Draw all active blocks
            foreach (var block in blocks.Values.Where(b => b.IsActive))
            {
                DrawBlock(device, effect, block);
            }
        }

        private void DrawBlock(GraphicsDevice device, BasicEffect effect, Block block)
        {
            var pos = block.Position;
            Color blockColor = block.BlockType switch
            {
                BlockType.Stone => Color.Gray,
                BlockType.Dirt => new Color(139, 69, 19),
                BlockType.Grass => Color.Green,
                BlockType.Wood => new Color(139, 90, 43),
                BlockType.Leaves => new Color(34, 139, 34),
                _ => Color.White
            };

            DrawCube(device, effect, pos, blockColor);
        }

        private void DrawCube(GraphicsDevice device, BasicEffect effect, Vector3 position, Color color)
        {
            VertexPositionColor[] vertices = new VertexPositionColor[8];
            float s = 0.5f;

            vertices[0] = new VertexPositionColor(position + new Vector3(-s, -s, -s), color);
            vertices[1] = new VertexPositionColor(position + new Vector3(s, -s, -s), color);
            vertices[2] = new VertexPositionColor(position + new Vector3(s, s, -s), color);
            vertices[3] = new VertexPositionColor(position + new Vector3(-s, s, -s), color);
            vertices[4] = new VertexPositionColor(position + new Vector3(-s, -s, s), color);
            vertices[5] = new VertexPositionColor(position + new Vector3(s, -s, s), color);
            vertices[6] = new VertexPositionColor(position + new Vector3(s, s, s), color);
            vertices[7] = new VertexPositionColor(position + new Vector3(-s, s, s), color);

            short[] indices = new short[]
            {
                // Front face
                0, 1, 2, 0, 2, 3,
                // Back face
                4, 6, 5, 4, 7, 6,
                // Top face
                3, 2, 6, 3, 6, 7,
                // Bottom face
                0, 5, 1, 0, 4, 5,
                // Left face
                0, 3, 7, 0, 7, 4,
                // Right face
                1, 5, 6, 1, 6, 2
            };

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 8, indices, 0, 12);
            }
        }
    }
}
