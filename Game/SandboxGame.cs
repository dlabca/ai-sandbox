using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AISandbox.Game.World;
using AISandbox.Game.Crafting;
using AISandbox.Game.Inventory;
using AISandbox.Game.Entities;
using System.Collections.Generic;

namespace AISandbox.Game
{
    public class SandboxGame : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private SpriteFont font;

        private WorldManager world;
        private Player player;
        private CraftingSystem craftingSystem;
        private List<Animal> animals;

        private Camera camera;
        private float gameTime = 0f;
        private KeyboardState previousKeyboardState;

        public SandboxGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
        }

        protected override void Initialize()
        {
            world = new WorldManager(50, 20, 30);
            player = new Player(Vector3.Zero, this.GraphicsDevice);
            craftingSystem = new CraftingSystem();
            animals = new List<Animal>();
            camera = new Camera(this.GraphicsDevice);
            previousKeyboardState = Keyboard.GetState();

            for (int i = 0; i < 5; i++)
            {
                animals.Add(new Animal(new Vector3(10 + i * 5, 5, 10), AnimalType.Sheep));
                animals.Add(new Animal(new Vector3(-10 - i * 5, 5, -10), AnimalType.Pig));
            }
            animals.Add(new Animal(new Vector3(0, 5, 20), AnimalType.Cow));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            var keyboardState = Keyboard.GetState();
            var mouseState = Mouse.GetState();

            this.gameTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

            player.Update(keyboardState, this.world);
            camera.Update(player.Position, GraphicsDevice);

            foreach (var animal in animals)
            {
                animal.Update((float)gameTime.ElapsedGameTime.TotalSeconds, world);
            }

            // Mining with E key
            if (keyboardState.IsKeyDown(Keys.E) && !previousKeyboardState.IsKeyDown(Keys.E))
            {
                var minedBlock = player.MineBlock(world);
                if (minedBlock != null && minedBlock.BlockType == BlockType.Wood)
                {
                    player.Inventory.AddItem(ItemType.Log, 1);
                }
            }

            // Crafting with C key
            if (keyboardState.IsKeyDown(Keys.C) && !previousKeyboardState.IsKeyDown(Keys.C))
            {
                HandleCrafting();
            }

            // Building with B key
            if (keyboardState.IsKeyDown(Keys.B))
            {
                player.PlaceBlock(world, BlockType.Wood);
            }

            previousKeyboardState = keyboardState;
            base.Update(gameTime);
        }

        private void HandleCrafting()
        {
            var recipes = craftingSystem.GetAllRecipes();
            foreach (var recipe in recipes)
            {
                if (craftingSystem.CanCraft(player.Inventory, recipe))
                {
                    craftingSystem.Craft(player.Inventory, recipe);
                    break;
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            DrawWorld();

            spriteBatch.Begin();
            DrawUI();
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawWorld()
        {
            world.Draw(GraphicsDevice);
            player.Draw(GraphicsDevice, camera);

            foreach (var animal in animals)
            {
                animal.Draw(GraphicsDevice, camera);
            }
        }

        private void DrawUI()
        {
            spriteBatch.DrawString(GetDefaultFont(), "AI Sandbox 3D", new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(GetDefaultFont(), $"Pos: {player.Position.X:F1}, {player.Position.Y:F1}, {player.Position.Z:F1}", new Vector2(10, 40), Color.White);
            spriteBatch.DrawString(GetDefaultFont(), $"Wood: {player.Inventory.GetItemCount(ItemType.Log)}", new Vector2(10, 70), Color.White);
            spriteBatch.DrawString(GetDefaultFont(), $"Planks: {player.Inventory.GetItemCount(ItemType.WoodPlank)}", new Vector2(10, 100), Color.White);
            spriteBatch.DrawString(GetDefaultFont(), "E-Mine | C-Craft | B-Build | WASD-Move | Space-Jump", new Vector2(10, 130), Color.Yellow);
        }

        private SpriteFont GetDefaultFont()
        {
            if (font == null)
            {
                font = Content.Load<SpriteFont>("Arial");
            }
            return font;
        }
    }
}
