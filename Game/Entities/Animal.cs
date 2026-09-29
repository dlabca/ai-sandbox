using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
                Position.Y = groundBlock.Position.Y + 1;
            }
            else
            {
                Position.Y -= 0.1f;
            }

            Position.X = MathHelper.Clamp(Position.X, 0, 49);
            Position.Z = MathHelper.Clamp(Position.Z, 0, 29);
        }

        public void Draw(GraphicsDevice device, BasicEffect effect, Camera camera)
        {
            Color animalColor = Type switch
            {
                AnimalType.Sheep => Color.White,
                AnimalType.Pig => new Color(255, 200, 150),
                AnimalType.Cow => Color.Black,
                AnimalType.Chicken => Color.Yellow,
                _ => Color.White
            };

            DrawAnimalCube(device, effect, Position, animalColor);
        }

        private void DrawAnimalCube(GraphicsDevice device, BasicEffect effect, Vector3 position, Color color)
        {
            VertexPositionColor[] vertices = new VertexPositionColor[8];
            float s = 0.25f;

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
