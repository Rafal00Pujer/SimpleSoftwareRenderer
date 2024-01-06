using System.Numerics;

namespace SimpleSoftwareRenderer.Rasterization;

internal class ModelInstance
{
    public Model Model { get; set; }

    public Vector3 Scale { get; set; } = Vector3.One;

    public Vector3 Position { get; set; }

    public Vector3 Rotation { get; set; } = Vector3.Zero;

    public Matrix4x4 Transform
    {
        get
        {
            return Matrix4x4.CreateTranslation(Position)
                * Matrix4x4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z)
                * Matrix4x4.CreateScale(Scale);
        }
    }
}
