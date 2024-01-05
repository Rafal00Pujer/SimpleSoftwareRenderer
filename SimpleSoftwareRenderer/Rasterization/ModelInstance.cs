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
            return Matrix4x4.CreateFromYawPitchRoll(Rotation.Z, Rotation.Y, Rotation.X)
                * Matrix4x4.CreateScale(Scale)
                * Matrix4x4.CreateTranslation(Position);
        }
    }
}
