using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AISandbox.Game.World;
using AISandbox.Game.Inventory;
using System;

namespace AISandbox.Game.Entities
{
    public class Player
    {
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public ToolInventory Inventory { get; private set; }

        private GraphicsDevice graphicsDevice;
        private float moveSpeed = 0.15f;
        private float jumpPower = 0.5f;
        private bool isJumping = false;
        private float verticalVelocity = 0;
        private const float Gravity = -0.02f;
        private float mineDelay = 0f;

        public Player(Vector3 startPosition, GraphicsDevice device)
        {
            Position = startPosition + Vector3.Up * 10;
            Velocity = Vector3.Zero;
            graphicsDevice = device;
            Inventory = new ToolInventory();
            Inventory.AddItem(ItemType.Log, 5);
        }

        public void Update(KeyboardState keyboardState, WorldManager world)
        {
            Vector3 moveDirection = Vector3.Zero;

            if (keyboardState.IsKeyDown(Keys.W))
                moveDirection += Vector3.Forward;
            if (keyboardState.IsKeyDown(Keys.S))
                moveDirection += Vector3.Backward;
            if (keyboardState.IsKeyDown(Keys.A))
                moveDirection += Vector3.Left;
            if (keyboardState.IsKeyDown(Keys.D))
                moveDirection += Vector3.Right;

            if (moveDirection != Vector3.Zero)
            {
                moveDirection.Normalize();
                Position += moveDirection * moveSpeed;
            }

            if (keyboardState.IsKeyDown(Keys.Space) && !isJumping)
            {
                verticalVelocity = jumpPower;
                isJumping = true;
            }

            verticalVelocity += Gravity;
            Position.Y += verticalVelocity;

            Vector3 groundCheck = Position - Vector3.Up * 1.7f;
            Block groundBlock = world.GetBlock(groundCheck);
            
            if (groundBlock.IsActive)
            {
                Position.Y = groundBlock.Position.Y + 2.7f;
                verticalVelocity = 0;
                isJumping = false;
            }

            Position.X = MathHelper.Clamp(Position.X, 0, 49);
            Position.Z = MathHelper.Clamp(Position.Z, 0, 29);

            mineDelay = Math.Max(0, mineDelay - 0.016f);
        }

        public Block MineBlock(WorldManager world)
        {
            if (mineDelay > 0) return null;

            Vector3 lookDirection = Vector3.Forward;
            Vector3 checkPos = Position + lookDirection * 3;
            checkPos = new Vector3(
                (float)Math.Round(checkPos.X),
                (float)Math.Round(checkPos.Y),
                (float)Math.Round(checkPos.Z)
            );
            
            Block targetBlock = world.GetBlock(checkPos);
            if (targetBlock.IsActive && targetBlock.BlockType == BlockType.Wood)
            {
                world.SetBlock(checkPos, BlockType.Air);
                mineDelay = 0.3f;
                return targetBlock;
            }
            return null;
        }

        public void PlaceBlock(WorldManager world, BlockType blockType)
        {
            if (Inventory.GetItemCount(ItemType.Log) > 0)
            {
                Vector3 lookDirection = Vector3.Forward;
                Vector3 placePos = Position + lookDirection * 3;
                placePos = new Vector3(
                    (float)Math.Round(placePos.X),
                    (float)Math.Round(placePos.Y),
                    (float)Math.Round(placePos.Z)
                );
                
                Block targetBlock = world.GetBlock(placePos);
                if (!targetBlock.IsActive)
                {
                    world.SetBlock(placePos, BlockType.Wood);
                    Inventory.RemoveItem(ItemType.Log, 1);
                }
            }
        }

        public void Draw(GraphicsDevice device, BasicEffect effect, Camera camera)
        {
            // Draw player as a simple cube
            Color playerColor = Color.CornflowerBlue;
            DrawPlayerCube(device, effect, Position, playerColor);
        }

        private void DrawPlayerCube(GraphicsDevice device, BasicEffect effect, Vector3 position, Color color)
        {
            VertexPositionColor[] vertices = new VertexPositionColor[8];
            float s = 0.3f;

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
                0, 1, 2, 0, 2, 3,
                4, 6, 5, 4, 7, 6,
                3, 2, 6, 3, 6, 7,
                0, 5, 1, 0, 4, 5,
                0, 3, 7, 0, 7, 4,
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
