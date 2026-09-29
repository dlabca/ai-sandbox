using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AISandbox.Game
{
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Target { get; set; }
        public Vector3 Up { get; set; }

        private GraphicsDevice graphicsDevice;

        public Matrix View { get; private set; }
        public Matrix Projection { get; private set; }

        public Camera(GraphicsDevice device)
        {
            graphicsDevice = device;
            Position = new Vector3(0, 15, -20);
            Target = Vector3.Zero;
            Up = Vector3.Up;
            UpdateMatrices();
        }

        public void Update(Vector3 playerPosition, GraphicsDevice device)
        {
            Position = playerPosition + new Vector3(5, 10, -15);
            Target = playerPosition + Vector3.Up * 3;
            UpdateMatrices();
        }

        private void UpdateMatrices()
        {
            View = Matrix.CreateLookAt(Position, Target, Up);
            float aspectRatio = (float)graphicsDevice.Viewport.Width / graphicsDevice.Viewport.Height;
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 1000f);
        }
    }
}
