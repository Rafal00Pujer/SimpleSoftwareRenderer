using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace SimpleSoftwareRenderer;

internal class Light
{
    public LightType Type { get; set; }

    public float Intensity { get; set; }

    public Vector3? Position { get; set; }

    public Vector3? Direction { get; set; }
}

enum LightType
{
    Ambient,
    Point,
    Directional
}