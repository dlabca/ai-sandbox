using Microsoft.Xna.Framework;
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
            Position = new Vector3(Position.X, Position.Y + verticalVelocity, Position.Z);

            Vector3 groundCheck = Position - Vector3.Up * 1.7f;
            Block groundBlock = world.GetBlock(groundCheck);
            
            if (groundBlock.IsActive)
            {
                Position = new Vector3(Position.X, groundBlock.Position.Y + 2.7f, Position.Z);
                verticalVelocity = 0;
                isJumping = false;
            }

            Position = new Vector3(
                MathHelper.Clamp(Position.X, 0, 49),
                Position.Y,
                MathHelper.Clamp(Position.Z, 0, 29));
        }

        public Block MineBlock(WorldManager world)
        {
            Vector3 lookDirection = Vector3.Forward;
            Vector3 checkPos = Position + lookDirection * 3;
            
            Block targetBlock = world.GetBlock(checkPos);
            if (targetBlock.IsActive && targetBlock.BlockType == BlockType.Wood)
            {
                world.SetBlock(checkPos, BlockType.Air);
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
                placePos = new Vector3((float)System.Math.Round(placePos.X), (float)System.Math.Round(placePos.Y), (float)System.Math.Round(placePos.Z));
                
                world.SetBlock(placePos, BlockType.Wood);
                Inventory.RemoveItem(ItemType.Log, 1);
            }
        }

        public void Draw(GraphicsDevice device, Camera camera)
        {
        }
    }
}
