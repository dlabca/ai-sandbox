using System;
using Microsoft.Xna.Framework;

namespace AISandbox
{
    public sealed class Player
    {
        public const float EyeHeight = 1.6f;
        public const float Radius = 0.35f;
        public const float Height = 1.7f;

        public Vector3 Pos;
        public float VelY;
        public bool OnGround;
        public float Yaw;
        public float Pitch;

        public Player(Vector3 spawn)
        {
            Pos = spawn;
        }

        public Vector3 Eye
        {
            get { return Pos + new Vector3(0f, EyeHeight, 0f); }
        }

        public Vector3 ForwardFlat
        {
            get { return new Vector3(MathF.Sin(Yaw), 0f, MathF.Cos(Yaw)); }
        }

        public Vector3 Forward
        {
            get
            {
                float cp = MathF.Cos(Pitch);
                return new Vector3(MathF.Sin(Yaw) * cp, MathF.Sin(Pitch), MathF.Cos(Yaw) * cp);
            }
        }

        public void Step(Level lv, float dt, float vx, float vz, bool jump)
        {
            float nx = Pos.X + vx * dt;
            float nz = Pos.Z + vz * dt;
            lv.ResolveHorizontal(ref nx, ref nz, Radius, Pos.Y, Height);
            Pos.X = nx;
            Pos.Z = nz;

            if (jump && OnGround) VelY = 6.6f;

            VelY -= 18f * dt;
            if (VelY < -30f) VelY = -30f;

            float gh = lv.GroundY(Pos.X, Pos.Z, Pos.Y);
            float ny = Pos.Y + VelY * dt;
            if (OnGround && VelY <= 0f && Pos.Y - gh < 0.4f && Pos.Y - gh > -0.7f)
            {
                ny = gh;
                VelY = 0f;
                OnGround = true;
            }
            else if (ny <= gh)
            {
                ny = gh;
                VelY = 0f;
                OnGround = true;
            }
            else
            {
                OnGround = false;
            }
            Pos.Y = ny;
        }
    }
}
