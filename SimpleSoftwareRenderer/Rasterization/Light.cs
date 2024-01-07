using System.Numerics;

namespace SimpleSoftwareRenderer.Rasterization;

internal class Light
{
    public LightType Type { get; set; }

    public float Intensity { get; set; }

    public Vector3? Position { get; set; }
}

enum LightType
{
    Ambient,
    Point,
    Directional
}