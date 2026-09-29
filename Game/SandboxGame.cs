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
        private BasicEffect basicEffect;

        private WorldManager world;
        private Player player;
        private CraftingSystem craftingSystem;
        private List<Animal> animals;

        private Camera camera;
        private float gameTime = 0f;
        private KeyboardState previousKeyboardState;
        private bool showCraftingUI = false;
        private int selectedRecipeIndex = 0;

        public SandboxGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.IsFullScreen = false;
            graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            world = new WorldManager(50, 20, 30);
            player = new Player(Vector3.Zero, this.GraphicsDevice);
            craftingSystem = new CraftingSystem();
            animals = new List<Animal>();
            camera = new Camera(this.GraphicsDevice);
            previousKeyboardState = Keyboard.GetState();

            // Spawn animals
            for (int i = 0; i < 5; i++)
            {
                animals.Add(new Animal(new Vector3(10 + i * 5, 9, 10), AnimalType.Sheep));
                animals.Add(new Animal(new Vector3(15 + i * 5, 9, -10), AnimalType.Pig));
            }
            animals.Add(new Animal(new Vector3(25, 9, 20), AnimalType.Cow));
            animals.Add(new Animal(new Vector3(10, 9, 25), AnimalType.Chicken));

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            basicEffect = new BasicEffect(GraphicsDevice)
            {
                TextureEnabled = false,
                VertexColorEnabled = true
            };
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            var keyboardState = Keyboard.GetState();

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

            // Crafting UI toggle with C key
            if (keyboardState.IsKeyDown(Keys.C) && !previousKeyboardState.IsKeyDown(Keys.C))
            {
                showCraftingUI = !showCraftingUI;
                selectedRecipeIndex = 0;
            }

            // Navigate crafting recipes
            if (showCraftingUI)
            {
                var recipes = craftingSystem.GetAllRecipes();
                if (keyboardState.IsKeyDown(Keys.Up) && !previousKeyboardState.IsKeyDown(Keys.Up))
                {
                    selectedRecipeIndex = (selectedRecipeIndex - 1 + recipes.Count) % recipes.Count;
                }
                if (keyboardState.IsKeyDown(Keys.Down) && !previousKeyboardState.IsKeyDown(Keys.Down))
                {
                    selectedRecipeIndex = (selectedRecipeIndex + 1) % recipes.Count;
                }

                // Craft with Enter
                if (keyboardState.IsKeyDown(Keys.Enter) && !previousKeyboardState.IsKeyDown(Keys.Enter))
                {
                    if (selectedRecipeIndex < recipes.Count)
                    {
                        var recipe = recipes[selectedRecipeIndex];
                        if (craftingSystem.CanCraft(player.Inventory, recipe))
                        {
                            craftingSystem.Craft(player.Inventory, recipe);
                        }
                    }
                }
            }

            // Building with B key (hold)
            if (keyboardState.IsKeyDown(Keys.B))
            {
                player.PlaceBlock(world, BlockType.Wood);
            }

            // Toggle fullscreen with F11
            if (keyboardState.IsKeyDown(Keys.F11) && !previousKeyboardState.IsKeyDown(Keys.F11))
            {
                graphics.IsFullScreen = !graphics.IsFullScreen;
                graphics.ApplyChanges();
            }

            previousKeyboardState = keyboardState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(135, 206, 235)); // Sky blue
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;

            // Draw 3D world
            basicEffect.View = camera.View;
            basicEffect.Projection = camera.Projection;

            world.Draw(GraphicsDevice, basicEffect);
            player.Draw(GraphicsDevice, basicEffect, camera);

            foreach (var animal in animals)
            {
                animal.Draw(GraphicsDevice, basicEffect, camera);
            }

            // Draw 2D UI
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            DrawUI();
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawUI()
        {
            var font = CreateSimpleFont();

            // Title
            spriteBatch.DrawString(font, "AI SANDBOX 3D", new Vector2(10, 10), Color.White);

            // Position
            spriteBatch.DrawString(font, $"Position: {player.Position.X:F1}, {player.Position.Y:F1}, {player.Position.Z:F1}", 
                new Vector2(10, 40), Color.LimeGreen);

            // Inventory
            int invY = 80;
            spriteBatch.DrawString(font, "=== INVENTORY ===", new Vector2(10, invY), Color.Yellow);
            invY += 25;
            spriteBatch.DrawString(font, $"Logs: {player.Inventory.GetItemCount(ItemType.Log)}", new Vector2(10, invY), Color.White);
            invY += 20;
            spriteBatch.DrawString(font, $"Planks: {player.Inventory.GetItemCount(ItemType.WoodPlank)}", new Vector2(10, invY), Color.White);
            invY += 20;
            spriteBatch.DrawString(font, $"Sticks: {player.Inventory.GetItemCount(ItemType.Stick)}", new Vector2(10, invY), Color.White);
            invY += 20;
            spriteBatch.DrawString(font, $"Wooden Axe: {player.Inventory.GetItemCount(ItemType.WoodenAxe)}", new Vector2(10, invY), Color.White);
            invY += 20;
            spriteBatch.DrawString(font, $"Pickaxe: {player.Inventory.GetItemCount(ItemType.Pickaxe)}", new Vector2(10, invY), Color.White);
            invY += 20;
            spriteBatch.DrawString(font, $"Stone: {player.Inventory.GetItemCount(ItemType.Stone)}", new Vector2(10, invY), Color.White);

            // Controls
            int ctrlY = 450;
            spriteBatch.DrawString(font, "=== CONTROLS ===", new Vector2(10, ctrlY), Color.Cyan);
            ctrlY += 25;
            spriteBatch.DrawString(font, "WASD - Move | Space - Jump", new Vector2(10, ctrlY), Color.White);
            ctrlY += 20;
            spriteBatch.DrawString(font, "E - Mine | B - Build | C - Craft", new Vector2(10, ctrlY), Color.White);
            ctrlY += 20;
            spriteBatch.DrawString(font, "F11 - Toggle Fullscreen", new Vector2(10, ctrlY), Color.White);

            // Crafting UI
            if (showCraftingUI)
            {
                DrawCraftingUI(font);
            }

            // Animals count
            spriteBatch.DrawString(font, $"Animals: {animals.Count}", new Vector2(GraphicsDevice.Viewport.Width - 200, 10), Color.Magenta);
        }

        private void DrawCraftingUI(SpriteFont font)
        {
            var recipes = craftingSystem.GetAllRecipes();
            int centerX = GraphicsDevice.Viewport.Width / 2 - 150;
            int centerY = GraphicsDevice.Viewport.Height / 2 - 100;

            // Background
            var bgRect = new Rectangle(centerX - 20, centerY - 20, 340, 280);
            DrawFilledRectangle(new Color(0, 0, 0, 200), bgRect);

            spriteBatch.DrawString(font, "=== CRAFTING MENU ===", new Vector2(centerX + 30, centerY), Color.Yellow);
            centerY += 40;

            for (int i = 0; i < recipes.Count; i++)
            {
                var recipe = recipes[i];
                var color = i == selectedRecipeIndex ? Color.Lime : Color.White;
                var canCraft = craftingSystem.CanCraft(player.Inventory, recipe);
                if (!canCraft) color = Color.Red;

                string reqStr = "";
                foreach (var req in recipe.Requirements)
                {
                    reqStr += $"{req.Value}x {req.Key} ";
                }

                spriteBatch.DrawString(font, $"{recipe.OutputItem} ({recipe.OutputCount}x)", 
                    new Vector2(centerX, centerY), color);
                spriteBatch.DrawString(font, $"Needs: {reqStr}", 
                    new Vector2(centerX + 10, centerY + 18), color * 0.7f);

                centerY += 50;
            }

            centerY += 10;
            spriteBatch.DrawString(font, "UP/DOWN - Select | ENTER - Craft", new Vector2(centerX, centerY), Color.Cyan);
        }

        private void DrawFilledRectangle(Color color, Rectangle rect)
        {
            var pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            spriteBatch.Draw(pixel, rect, color);
            pixel.Dispose();
        }

        private SpriteFont CreateSimpleFont()
        {
            // Create a simple monospace font using texture
            return new SpriteFont(
                new Texture2D(GraphicsDevice, 1, 1),
                new List<Rectangle>(),
                new List<Rectangle>(),
                new List<char>(),
                10, 0, new List<Vector3>(), null
            ) ?? null;
        }
    }
}
