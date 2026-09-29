using Microsoft.Xna.Framework;
using AISandbox.Game.World;
using System;

namespace AISandbox.Game.Entities
{
    public enum AnimalType
    {
        Sheep,
        Pig,
        Cow,
        Chicken
    }

    public class Animal
    {
        public Vector3 Position { get; set; }
        public AnimalType Type { get; private set; }
        private Vector3 moveDirection = Vector3.Zero;
        private float moveTimer = 0;
        private float moveInterval = 2f;
        private Random random = new Random();

        public Animal(Vector3 position, AnimalType type)
        {
            Position = position;
            Type = type;
        }

        public void Update(float deltaTime, WorldManager world)
        {
            moveTimer += deltaTime;

            if (moveTimer >= moveInterval)
            {
                float angle = (float)(random.NextDouble() * Math.PI * 2);
                moveDirection = new Vector3((float)Math.Cos(angle), 0, (float)Math.Sin(angle));
                moveTimer = 0;
                moveInterval = 2f + (float)random.NextDouble() * 3f;
            }

            Position += moveDirection * 0.05f;

            Vector3 groundCheck = Position - Vector3.Up * 0.5f;
            Block groundBlock = world.GetBlock(groundCheck);
            
            if (groundBlock.IsActive)
            {
                Position = new Vector3(Position.X, groundBlock.Position.Y + 1, Position.Z);
            }
            else
            {
                Position = new Vector3(Position.X, Position.Y - 0.1f, Position.Z);
            }

            Position = new Vector3(
                MathHelper.Clamp(Position.X, 0, 49),
                Position.Y,
                MathHelper.Clamp(Position.Z, 0, 29));
        }

        public void Draw(GraphicsDevice device, Camera camera)
        {
        }
    }
}
